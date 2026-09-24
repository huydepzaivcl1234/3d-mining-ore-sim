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

        [Header("Portal Unlock")]
        [Min(0), SerializeField] private int requiredRebirths = 3;
        [Min(0f), SerializeField] private float requiredCoins = 1000000f;

        [Header("Runtime References")]
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MiningRebirthSystem rebirthSystem;
        [SerializeField] private OreSpawner groundSpawner;
        [SerializeField] private OreSpawner undergroundSpawner;
        [SerializeField] private GameObject groundEnvironment;
        [SerializeField] private GameObject undergroundEnvironment;
        [Tooltip("Authored camera arrival for the surface. If empty, the camera position before the first descent is remembered.")]
        [SerializeField] private Transform groundCameraFocus;
        [Tooltip("Required scene-authored arrival focus above the underground platform.")]
        [SerializeField] private Transform undergroundCameraFocus;
        [Tooltip("Optional marker where newly purchased miners appear Underground; falls back to the Underground Camera Focus.")]
        [SerializeField] private Transform undergroundNpcSpawnPoint;
        [Header("Shared Miners")]
        [Tooltip("Move miners outside both authored world roots with the camera when worlds are at different heights.")]
        [SerializeField] private bool moveSharedMinersWithWorld = true;
        [Header("Underground Lighting")]
        [SerializeField] private bool undergroundFog = true;
        [SerializeField] private Color undergroundFogColor = new(0.025f, 0.03f, 0.05f);
        [Min(0f), SerializeField] private float undergroundFogDensity = 0.018f;
        [SerializeField] private AmbientMode undergroundAmbientMode = AmbientMode.Flat;
        [SerializeField] private Color undergroundAmbientLight = new(0.08f, 0.095f, 0.14f);

        private bool undergroundUnlocked;
        private bool groundFog;
        private Color groundFogColor;
        private float groundFogDensity;
        private AmbientMode groundAmbientMode;
        private Color groundAmbientLight;
        private MiningPortalSlideTransition slideTransition;
        private Transform pendingPortalAnchor;
        private Vector3 groundReturnFocus;
        private bool hasGroundReturnFocus;

        public MiningWorldArea CurrentArea { get; private set; } = MiningWorldArea.Ground;
        public bool UndergroundUnlocked => undergroundUnlocked;
        public int RequiredRebirths => requiredRebirths;
        public float RequiredCoins => requiredCoins;
        public int CompletedRebirths => rebirthSystem != null ? rebirthSystem.CompletedRebirths : 0;
        public float CurrentCoins => wallet != null ? wallet.CurrentMoney : 0f;
        public OreSpawner ActiveSpawner => CurrentArea == MiningWorldArea.Underground
            ? undergroundSpawner
            : groundSpawner;
        public bool IsTransitioning => slideTransition != null && slideTransition.IsPlaying;
        public bool WorldsConfigured => groundSpawner != null && undergroundSpawner != null &&
            groundSpawner != undergroundSpawner && groundSpawner.SpawnData != null &&
            undergroundSpawner.SpawnData != null && groundEnvironment != null &&
            undergroundEnvironment != null && groundEnvironment != undergroundEnvironment &&
            undergroundCameraFocus != null;
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
            if (!WorldsConfigured)
            {
                Debug.LogError("Portal requires distinct Ground/UnderGround roots, two OreSpawners with SpawnData, and an Underground Camera Focus assigned in the scene.", this);
                return false;
            }
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

        public void SetPortalAnchor(Transform anchor)
        {
            pendingPortalAnchor = anchor;
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
                if (groundCameraFocus != null) return groundCameraFocus.position;
                if (hasGroundReturnFocus) return groundReturnFocus;
                return groundFallback;
            }

            if (undergroundCameraFocus != null)
            {
                return undergroundCameraFocus.position;
            }

            return groundFallback;
        }

        private bool RequestAreaChange(MiningWorldArea area)
        {
            if (CurrentArea == area || IsTransitioning ||
                (area == MiningWorldArea.Underground && !undergroundUnlocked))
            {
                return false;
            }

            if (!WorldsConfigured)
            {
                Debug.LogError("World swap stopped: assign both world roots, separate OreSpawners/SpawnData, and Underground Camera Focus in the scene.", this);
                return false;
            }

            MiningOrbitCamera camera = FindFirstObjectByType<MiningOrbitCamera>(FindObjectsInactive.Include);
            if (area == MiningWorldArea.Underground && camera != null)
            {
                groundReturnFocus = camera.FocusPoint;
                hasGroundReturnFocus = true;
            }
            slideTransition ??= MiningPortalSlideTransition.EnsureRuntime(this);
            Transform portalAnchor = pendingPortalAnchor;
            pendingPortalAnchor = null;
            if (slideTransition != null &&
                slideTransition.TryPlay(area, portalAnchor))
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

            if (moveSharedMinersWithWorld && !force)
            {
                Vector3 from = GetCameraFocus(underground ? MiningWorldArea.Ground :
                    MiningWorldArea.Underground, Vector3.zero);
                Vector3 to = GetCameraFocus(area, Vector3.zero);
                MoveSharedMiners(to - from);
            }
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

        }

        private void EnsureUndergroundWorld()
        {
            // Underground content is authored in the scene. Never create geometry, clone a
            // spawner, reparent objects, or overwrite the designer's platform configuration.
            if (undergroundEnvironment != null) undergroundEnvironment.SetActive(false);
            if (undergroundSpawner != null) undergroundSpawner.gameObject.SetActive(false);
            if (Application.isPlaying && undergroundSpawner == null)
            {
                Debug.LogWarning("Assign your existing Underground OreSpawner on MiningWorldAreaController. Nothing is generated automatically.", this);
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

            ResolveReferences();
        }
#endif


        private void RebindSharedAreaSystems(OreSpawner spawner)
        {
            if (spawner == null) return;

            // World-owned miners keep their own authored spawner. Only shared miners are
            // re-bound; the NPC shop can create new miners against this same active spawner.
            foreach (MiningNpc miner in FindObjectsByType<MiningNpc>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (miner != null && !IsWithinWorldRoot(miner.transform))
                    miner.SetOreSpawner(spawner);
            }
            NpcShop shop = FindFirstObjectByType<NpcShop>(FindObjectsInactive.Include);
            if (shop != null)
            {
                shop.SetOreSpawner(spawner);
                shop.SetWorldSpawnPoint(CurrentArea == MiningWorldArea.Underground
                    ? undergroundNpcSpawnPoint != null ? undergroundNpcSpawnPoint : undergroundCameraFocus
                    : null);
            }
            FindFirstObjectByType<NpcProgressionSystem>(FindObjectsInactive.Include)?.SetOreSpawner(spawner);
            FindFirstObjectByType<MiningUnlockNotifier>(FindObjectsInactive.Include)?.SetOreSpawner(spawner);
            foreach (JuicyMinerProgress progress in FindObjectsByType<JuicyMinerProgress>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                progress?.SetOreSpawner(spawner);
            }
        }

        private bool IsWithinWorldRoot(Transform miner)
        {
            return (groundEnvironment != null && miner.IsChildOf(groundEnvironment.transform)) ||
                   (undergroundEnvironment != null && miner.IsChildOf(undergroundEnvironment.transform));
        }

        private void MoveSharedMiners(Vector3 delta)
        {
            if (delta.sqrMagnitude < 0.0001f) return;
            foreach (MiningNpc miner in FindObjectsByType<MiningNpc>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (miner == null || IsWithinWorldRoot(miner.transform)) continue;
                Rigidbody body = miner.GetComponent<Rigidbody>();
                if (body != null)
                {
                    body.position += delta;
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                else miner.transform.position += delta;
            }
            Physics.SyncTransforms();
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
                RenderSettings.fog = undergroundFog;
                RenderSettings.fogColor = undergroundFogColor;
                RenderSettings.fogDensity = undergroundFogDensity;
                RenderSettings.ambientMode = undergroundAmbientMode;
                RenderSettings.ambientLight = undergroundAmbientLight;
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
            undergroundFogDensity = Mathf.Max(0f, undergroundFogDensity);
        }
    }
}
