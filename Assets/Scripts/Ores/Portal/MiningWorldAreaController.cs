using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiningSimulator.Ores
{
    public enum MiningWorldArea
    {
        Ground = 0,
        Underground = 1
    }

    /// <summary>
    /// Owns the active mining area. Ground and Underground occupy the same gameplay footprint,
    /// but only one environment and ore spawner is active at a time. This lets the existing orbit
    /// camera, interaction raycasts and miners keep their coordinates while the portal swaps worlds.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MiningWorldAreaController : MonoBehaviour
    {
        public const string UndergroundUnlockSaveKey = "MiningSimulator.UndergroundUnlocked.v1";
        private const string UndergroundSpawnDataResourcePath =
            "MiningSimulator/UndergroundOreSpawnData";

        [Header("Portal Unlock")]
        [Min(0), SerializeField] private int requiredRebirths = 3;
        [Min(0f), SerializeField] private float requiredCoins = 1000000f;

        [Header("Underground Ore Area")]
        [Tooltip("Independent spawn table. Only entries in this asset can spawn Underground.")]
        [SerializeField] private OreSpawnData undergroundSpawnData;
        [SerializeField] private Vector3 undergroundAreaCenter;
        [Min(1f), SerializeField] private Vector3 undergroundAreaSize = new(20f, 0f, 20f);

        [Header("Runtime References")]
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MiningRebirthSystem rebirthSystem;
        [SerializeField] private OreSpawner groundSpawner;
        [SerializeField] private OreSpawner undergroundSpawner;
        [SerializeField] private GameObject groundEnvironment;
        [SerializeField] private GameObject undergroundEnvironment;
        [Tooltip("Optional scene-authored camera destination. When empty, the Underground Ore " +
                 "System or Underground Platform position is used automatically.")]
        [SerializeField] private Transform undergroundCameraFocus;

        private bool undergroundUnlocked;
        private bool groundFog;
        private Color groundFogColor;
        private float groundFogDensity;
        private AmbientMode groundAmbientMode;
        private Color groundAmbientLight;
        private MiningPortalSlideTransition slideTransition;

        public MiningWorldArea CurrentArea { get; private set; } = MiningWorldArea.Ground;
        public bool UndergroundUnlocked => undergroundUnlocked;
        public int RequiredRebirths => requiredRebirths;
        public float RequiredCoins => requiredCoins;
        public OreSpawnData UndergroundSpawnData => undergroundSpawnData;
        public Vector3 UndergroundAreaCenter => undergroundAreaCenter;
        public Vector3 UndergroundAreaSize => undergroundAreaSize;
        public int CompletedRebirths => rebirthSystem != null ? rebirthSystem.CompletedRebirths : 0;
        public float CurrentCoins => wallet != null ? wallet.CurrentMoney : 0f;
        public OreSpawner ActiveSpawner => CurrentArea == MiningWorldArea.Underground
            ? undergroundSpawner
            : groundSpawner;
        public bool IsTransitioning => slideTransition != null && slideTransition.IsPlaying;
        public event Action AreaChanged;
        public event Action RequirementsChanged;

        public static MiningWorldAreaController EnsureRuntime()
        {
            MiningWorldAreaController existing = FindFirstObjectByType<MiningWorldAreaController>(
                FindObjectsInactive.Include);
            if (existing != null)
            {
                return existing;
            }

            GameObject owner = new("Mining World Areas");
            return owner.AddComponent<MiningWorldAreaController>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapRuntime()
        {
            EnsureRuntime();
        }

        public static void ResetSavedUnlock()
        {
            PlayerPrefs.DeleteKey(UndergroundUnlockSaveKey);
            PlayerPrefs.Save();
            MiningWorldAreaController controller = FindFirstObjectByType<MiningWorldAreaController>(
                FindObjectsInactive.Include);
            if (controller != null)
            {
                controller.undergroundUnlocked = false;
                controller.SwitchTo(MiningWorldArea.Ground);
                controller.RequirementsChanged?.Invoke();
            }
        }

        private void Awake()
        {
            undergroundSpawnData ??= Resources.Load<OreSpawnData>(UndergroundSpawnDataResourcePath);
            ResolveReferences();
            undergroundUnlocked = PlayerPrefs.GetInt(UndergroundUnlockSaveKey, 0) == 1;
            CaptureGroundLighting();
            EnsureUndergroundWorld();
            SwitchTo(MiningWorldArea.Ground, true);
            slideTransition = MiningPortalSlideTransition.EnsureRuntime(this);
        }

        private void OnEnable()
        {
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
                wallet.MoneyChanged += HandleMoneyChanged;
            }
            if (rebirthSystem != null)
            {
                rebirthSystem.StateChanged -= HandleRebirthChanged;
                rebirthSystem.StateChanged += HandleRebirthChanged;
            }
        }

        private void OnDisable()
        {
            if (wallet != null) wallet.MoneyChanged -= HandleMoneyChanged;
            if (rebirthSystem != null) rebirthSystem.StateChanged -= HandleRebirthChanged;
        }

        public bool CanPurchaseUnlock => !undergroundUnlocked &&
                                         CompletedRebirths >= requiredRebirths &&
                                         CurrentCoins >= requiredCoins;

        public bool TryUnlockAndEnter()
        {
            if (undergroundUnlocked)
            {
                return RequestAreaChange(MiningWorldArea.Underground);
            }

            if (!CanPurchaseUnlock || wallet == null || !wallet.TrySpend(requiredCoins))
            {
                RequirementsChanged?.Invoke();
                return false;
            }

            undergroundUnlocked = true;
            PlayerPrefs.SetInt(UndergroundUnlockSaveKey, 1);
            PlayerPrefs.Save();
            RequirementsChanged?.Invoke();
            return RequestAreaChange(MiningWorldArea.Underground);
        }

        public void ReturnToGround()
        {
            RequestAreaChange(MiningWorldArea.Ground);
        }

        public void ToggleArea()
        {
            if (CurrentArea == MiningWorldArea.Underground)
            {
                ReturnToGround();
            }
            else if (undergroundUnlocked)
            {
                RequestAreaChange(MiningWorldArea.Underground);
            }
        }

        public Vector3 GetCameraFocus(MiningWorldArea area, Vector3 groundFallback)
        {
            if (area != MiningWorldArea.Underground)
            {
                return groundFallback;
            }

            if (undergroundCameraFocus != null)
            {
                return undergroundCameraFocus.position;
            }

            if (undergroundSpawner != null)
            {
                return undergroundSpawner.transform.position;
            }

            if (undergroundEnvironment != null &&
                TryGetRendererBoundsCenter(undergroundEnvironment, out Vector3 platformCenter))
            {
                return platformCenter;
            }

            return undergroundAreaCenter;
        }

        private bool RequestAreaChange(MiningWorldArea area)
        {
            if (CurrentArea == area || IsTransitioning ||
                (area == MiningWorldArea.Underground && !undergroundUnlocked))
            {
                return false;
            }

            slideTransition ??= MiningPortalSlideTransition.EnsureRuntime(this);
            MiningPortalGate portal = FindFirstObjectByType<MiningPortalGate>(
                FindObjectsInactive.Include);
            if (slideTransition != null &&
                slideTransition.TryPlay(area, portal != null ? portal.transform : null))
            {
                return true;
            }

            // A missing camera should never make travel unusable. The world swap still works;
            // only the presentation is skipped.
            SwitchTo(area);
            return CurrentArea == area;
        }

        internal void CompleteAnimatedSwitch(MiningWorldArea area)
        {
            SwitchTo(area);
        }

        private void SwitchTo(MiningWorldArea area, bool force = false)
        {
            if (!force && CurrentArea == area)
            {
                return;
            }
            if (area == MiningWorldArea.Underground && !undergroundUnlocked)
            {
                return;
            }

            CurrentArea = area;
            bool underground = area == MiningWorldArea.Underground;

            if (groundSpawner != null) groundSpawner.gameObject.SetActive(!underground);
            if (undergroundSpawner != null) undergroundSpawner.gameObject.SetActive(underground);
            if (groundEnvironment != null) groundEnvironment.SetActive(!underground);
            if (undergroundEnvironment != null) undergroundEnvironment.SetActive(underground);

            ApplyLighting(underground);
            RebindSharedAreaSystems(ActiveSpawner);

            MiningNavMeshBuilder navMeshBuilder = MiningNavMeshBuilder.Instance;
            if (navMeshBuilder != null)
            {
                if (underground)
                {
                    navMeshBuilder.RequestRebuildForRoot(undergroundEnvironment);
                }
                else
                {
                    navMeshBuilder.RequestRebuild();
                }
            }
            AreaChanged?.Invoke();
        }

        private void ResolveReferences()
        {
            wallet ??= FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            rebirthSystem ??= FindFirstObjectByType<MiningRebirthSystem>(FindObjectsInactive.Include);

            foreach (OreSpawner candidate in FindObjectsByType<OreSpawner>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate == null)
                {
                    continue;
                }

                if (undergroundSpawner == null && IsUndergroundName(candidate.gameObject.name))
                {
                    undergroundSpawner = candidate;
                    continue;
                }

                if (groundSpawner == null && candidate != undergroundSpawner &&
                    !IsUndergroundName(candidate.gameObject.name))
                {
                    groundSpawner = candidate;
                }
            }

            if (groundEnvironment == null)
            {
                groundEnvironment = GameObject.Find("Ground");
            }

            undergroundEnvironment ??= FindSceneObjectByName(
                "UnderGround", "Underground",
                "Underground Environment", "Under Ground Environment",
                "Underground Platform", "Under Ground Platform",
                "Underground World", "Under Ground World");

            if (undergroundCameraFocus == null)
            {
                GameObject focus = FindSceneObjectByName(
                    "Underground Camera Focus", "Under Ground Camera Focus",
                    "Underground Focus", "Under Ground Focus");
                undergroundCameraFocus = focus != null ? focus.transform : null;
            }
        }

        private void EnsureUndergroundWorld()
        {
            // Underground content is authored in the scene. Never create geometry, clone a
            // spawner, reparent objects, or overwrite the designer's platform configuration.
            if (undergroundEnvironment != null) undergroundEnvironment.SetActive(false);
            if (undergroundSpawner != null) undergroundSpawner.gameObject.SetActive(false);
            if (Application.isPlaying && undergroundSpawner == null)
            {
                Debug.LogWarning("No scene-authored Underground Ore System was found. " +
                                 "Assign it on MiningWorldAreaController or name the object " +
                                 "'Under Ground Ore System'. Nothing will be generated.", this);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Called by the editor setup menu. It only resolves designer-authored scene objects and
        /// never creates, reparents, resizes, or configures Underground content.
        /// </summary>
        public void PrepareEditorHierarchy()
        {
            if (Application.isPlaying)
            {
                return;
            }

            undergroundSpawnData ??= Resources.Load<OreSpawnData>(UndergroundSpawnDataResourcePath);
            ResolveReferences();
        }
#endif

        private static bool IsUndergroundName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            string compact = objectName.Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty);
            return compact.IndexOf("underground", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static GameObject FindSceneObjectByName(params string[] acceptedNames)
        {
            foreach (Transform candidate in FindObjectsByType<Transform>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate == null || !candidate.gameObject.scene.IsValid())
                {
                    continue;
                }

                foreach (string acceptedName in acceptedNames)
                {
                    if (string.Equals(candidate.name, acceptedName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return candidate.gameObject;
                    }
                }
            }

            return null;
        }

        private static bool TryGetRendererBoundsCenter(GameObject root, out Vector3 center)
        {
            center = root != null ? root.transform.position : Vector3.zero;
            if (root == null)
            {
                return false;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return false;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            center = bounds.center;
            return true;
        }

        private void RebindSharedAreaSystems(OreSpawner spawner)
        {
            if (spawner == null) return;

            // Miners are scene-authored per area. Never move, duplicate, or overwrite their
            // OreSpawner assignment here; the user can duplicate a miner under UnderGround and
            // assign the existing underground spawner in the Inspector.
            FindFirstObjectByType<NpcShop>(FindObjectsInactive.Include)?.SetOreSpawner(spawner);
            FindFirstObjectByType<NpcProgressionSystem>(FindObjectsInactive.Include)?.SetOreSpawner(spawner);
            FindFirstObjectByType<MiningUnlockNotifier>(FindObjectsInactive.Include)?.SetOreSpawner(spawner);
            foreach (JuicyMinerProgress progress in FindObjectsByType<JuicyMinerProgress>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                progress?.SetOreSpawner(spawner);
            }
        }

        private void CaptureGroundLighting()
        {
            groundFog = RenderSettings.fog;
            groundFogColor = RenderSettings.fogColor;
            groundFogDensity = RenderSettings.fogDensity;
            groundAmbientMode = RenderSettings.ambientMode;
            groundAmbientLight = RenderSettings.ambientLight;
        }

        private void ApplyLighting(bool underground)
        {
            if (underground)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(0.025f, 0.03f, 0.05f);
                RenderSettings.fogDensity = 0.018f;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.08f, 0.095f, 0.14f);
                return;
            }
            RenderSettings.fog = groundFog;
            RenderSettings.fogColor = groundFogColor;
            RenderSettings.fogDensity = groundFogDensity;
            RenderSettings.ambientMode = groundAmbientMode;
            RenderSettings.ambientLight = groundAmbientLight;
        }

        private void HandleMoneyChanged(float _) => RequirementsChanged?.Invoke();
        private void HandleRebirthChanged() => RequirementsChanged?.Invoke();

        private void OnValidate()
        {
            requiredRebirths = Mathf.Max(0, requiredRebirths);
            requiredCoins = Mathf.Max(0f, requiredCoins);
            undergroundAreaSize = new Vector3(
                Mathf.Max(1f, Mathf.Abs(undergroundAreaSize.x)),
                Mathf.Max(0f, Mathf.Abs(undergroundAreaSize.y)),
                Mathf.Max(1f, Mathf.Abs(undergroundAreaSize.z)));
        }
    }
}
