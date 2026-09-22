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

        private bool undergroundUnlocked;
        private bool groundFog;
        private Color groundFogColor;
        private float groundFogDensity;
        private AmbientMode groundAmbientMode;
        private Color groundAmbientLight;

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
                SwitchTo(MiningWorldArea.Underground);
                return true;
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
            SwitchTo(MiningWorldArea.Underground);
            return true;
        }

        public void ReturnToGround()
        {
            SwitchTo(MiningWorldArea.Ground);
        }

        public void ToggleArea()
        {
            if (CurrentArea == MiningWorldArea.Underground)
            {
                ReturnToGround();
            }
            else if (undergroundUnlocked)
            {
                SwitchTo(MiningWorldArea.Underground);
            }
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
            RebindAreaSystems(ActiveSpawner);
            MiningNavMeshBuilder.Instance?.RequestRebuild();
            AreaChanged?.Invoke();
        }

        private void ResolveReferences()
        {
            wallet ??= FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            rebirthSystem ??= FindFirstObjectByType<MiningRebirthSystem>(FindObjectsInactive.Include);
            if (groundSpawner == null)
            {
                foreach (OreSpawner candidate in FindObjectsByType<OreSpawner>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (candidate == null || candidate == undergroundSpawner ||
                        candidate.gameObject.name == "Underground Ore System")
                    {
                        continue;
                    }

                    groundSpawner = candidate;
                    break;
                }
            }
            if (groundEnvironment == null)
            {
                groundEnvironment = GameObject.Find("Ground");
            }
        }

        private void EnsureUndergroundWorld()
        {
            EnsureUndergroundHierarchy();

            // The editor setup creates the named environment root so designers can see and
            // select the Underground hierarchy. Its generated cave children are still created
            // lazily in Play Mode to avoid serializing runtime-created materials into a scene.
            if (undergroundEnvironment != null && undergroundEnvironment.transform.childCount == 0)
            {
                BuildCave(undergroundEnvironment.transform);
            }

            if (groundSpawner != null)
            {
                undergroundSpawner.ConfigureAsAreaClone(groundSpawner, undergroundSpawnData,
                    undergroundAreaCenter, undergroundAreaSize);
            }

            undergroundEnvironment.SetActive(false);
            if (undergroundSpawner != null) undergroundSpawner.gameObject.SetActive(false);
        }

        private void EnsureUndergroundHierarchy()
        {
            if (undergroundEnvironment == null)
            {
                Transform existingEnvironment = transform.Find("Underground Environment");
                undergroundEnvironment = existingEnvironment != null
                    ? existingEnvironment.gameObject
                    : new GameObject("Underground Environment");
            }
            if (undergroundEnvironment.transform.parent != transform)
            {
                undergroundEnvironment.transform.SetParent(transform, false);
            }

            if (undergroundSpawner == null)
            {
                Transform existingSpawner = transform.Find("Underground Ore System");
                if (existingSpawner != null)
                {
                    undergroundSpawner = existingSpawner.GetComponent<OreSpawner>();
                }

                if (undergroundSpawner == null && groundSpawner != null)
                {
                    GameObject spawnerObject = new("Underground Ore System");
                    spawnerObject.SetActive(false);
                    spawnerObject.transform.SetParent(transform, false);
                    undergroundSpawner = spawnerObject.AddComponent<OreSpawner>();
                }

                if (undergroundSpawner != null)
                {
                    undergroundSpawner.gameObject.SetActive(false);
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Called by the editor setup menu for a designer-authored gameplay scene. It creates only
        /// the named hierarchy and serialized references; the cave geometry remains runtime-built.
        /// </summary>
        public void PrepareEditorHierarchy()
        {
            if (Application.isPlaying)
            {
                return;
            }

            undergroundSpawnData ??= Resources.Load<OreSpawnData>(UndergroundSpawnDataResourcePath);
            ResolveReferences();
            EnsureUndergroundHierarchy();
            undergroundEnvironment.SetActive(false);
            if (undergroundSpawner != null)
            {
                undergroundSpawner.ConfigureAsAreaClone(groundSpawner, undergroundSpawnData,
                    undergroundAreaCenter, undergroundAreaSize);
                undergroundSpawner.gameObject.SetActive(false);
            }
        }
#endif

        private void BuildCave(Transform root)
        {
            Vector3 center = undergroundAreaCenter;
            Vector3 size = undergroundAreaSize;
            float width = Mathf.Max(28f, size.x + 12f);
            float depth = Mathf.Max(28f, size.z + 12f);
            float wallHeight = 14f;

            Material floorMaterial = CreateMaterial("Underground Basalt", new Color(0.10f, 0.105f, 0.12f));
            Material wallMaterial = CreateMaterial("Underground Rock", new Color(0.075f, 0.065f, 0.08f));
            Material[] rockMaterials =
            {
                CreateMaterial("Slate Rock", new Color(0.16f, 0.17f, 0.20f)),
                CreateMaterial("Iron Rock", new Color(0.23f, 0.16f, 0.13f)),
                CreateMaterial("Moss Rock", new Color(0.12f, 0.20f, 0.16f)),
                CreateMaterial("Crystal Rock", new Color(0.20f, 0.12f, 0.28f))
            };

            CreateBlock("Cave Floor", root, center + Vector3.down * 0.5f,
                new Vector3(width, 1f, depth), floorMaterial, true);
            CreateBlock("North Wall", root, center + new Vector3(0f, wallHeight * 0.5f, depth * 0.5f),
                new Vector3(width, wallHeight, 1.5f), wallMaterial, true);
            CreateBlock("South Wall", root, center + new Vector3(0f, wallHeight * 0.5f, -depth * 0.5f),
                new Vector3(width, wallHeight, 1.5f), wallMaterial, true);
            CreateBlock("East Wall", root, center + new Vector3(width * 0.5f, wallHeight * 0.5f, 0f),
                new Vector3(1.5f, wallHeight, depth), wallMaterial, true);
            CreateBlock("West Wall", root, center + new Vector3(-width * 0.5f, wallHeight * 0.5f, 0f),
                new Vector3(1.5f, wallHeight, depth), wallMaterial, true);
            CreateBlock("Cave Ceiling", root, center + Vector3.up * wallHeight,
                new Vector3(width, 1.5f, depth), wallMaterial, false);

            System.Random random = new(73021);
            for (int index = 0; index < 36; index++)
            {
                float angle = index / 36f * Mathf.PI * 2f;
                float edgeX = Mathf.Cos(angle) * (width * 0.43f);
                float edgeZ = Mathf.Sin(angle) * (depth * 0.43f);
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = $"Decorative Rock Variant {index + 1:00}";
                rock.transform.SetParent(root, false);
                rock.transform.position = center + new Vector3(edgeX, 0.35f, edgeZ);
                rock.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f,
                    (float)random.NextDouble() * 18f - 9f);
                float scale = 0.7f + (float)random.NextDouble() * 1.5f;
                rock.transform.localScale = new Vector3(scale * 1.35f, scale * 0.75f, scale);
                Renderer renderer = rock.GetComponent<Renderer>();
                renderer.sharedMaterial = rockMaterials[index % rockMaterials.Length];
                Collider collider = rock.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
            }

            for (int index = 0; index < 8; index++)
            {
                GameObject lightObject = new($"Cave Crystal Light {index + 1:00}");
                lightObject.transform.SetParent(root, false);
                float angle = index / 8f * Mathf.PI * 2f;
                lightObject.transform.position = center + new Vector3(
                    Mathf.Cos(angle) * width * 0.34f, 3f, Mathf.Sin(angle) * depth * 0.34f);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 11f;
                light.intensity = 2.2f;
                light.color = index % 2 == 0
                    ? new Color(0.30f, 0.65f, 1f)
                    : new Color(0.72f, 0.30f, 1f);
            }
        }

        private static void CreateBlock(string name, Transform parent, Vector3 position,
            Vector3 scale, Material material, bool walkableOrBlocking)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = block.GetComponent<Collider>();
            if (collider != null) collider.enabled = walkableOrBlocking;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new(shader) { name = name, color = color };
            return material;
        }

        private void RebindAreaSystems(OreSpawner spawner)
        {
            if (spawner == null) return;
            foreach (MiningNpc npc in FindObjectsByType<MiningNpc>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                npc?.SetOreSpawner(spawner);
            }
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
