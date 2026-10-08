using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>One-scene world swap; the shared surface, managers and progression stay intact.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningLavaWorldController : MonoBehaviour
    {
        [Header("Existing scene references")]
        [SerializeField] private OreSpawner oreSpawner;
        [SerializeField] private MiningDynamicRadialMaskTransition transition;
        [Tooltip("Assign scene tree/forest roots. Hidden in Lava and restored on Ground.")]
        [SerializeField] private List<GameObject> surroundingTrees = new();
        [Tooltip("Optional scene Terrains whose tree and foliage rendering is hidden in Lava World.")]
        [SerializeField] private List<Terrain> groundTerrains = new();
        [Tooltip("Optional scene-authored Lava decoration roots. Disable them in the scene until travelling.")]
        [SerializeField] private List<GameObject> lavaDecorations = new();
        [Header("World-specific scene content")]
        [Tooltip("Optional extra Ground-only roots (props, enemies, shops). Restored to their original state on return. Ground surface, player, Ore System, UI and portal must stay shared.")]
        [SerializeField] private List<GameObject> groundOnlyRoots = new();
        [Tooltip("Optional Lava-only roots. Keep these inactive in the authored Ground scene.")]
        [SerializeField] private List<GameObject> lavaOnlyRoots = new();
        [Header("Existing ground surface")]
        [Tooltip("Assign the existing Ground mesh renderer; no new floor is created.")]
        [SerializeField] private Renderer groundRenderer;
        [Tooltip("Use only if Ground is a Terrain instead of a mesh.")]
        [SerializeField] private Terrain groundTerrain;
        [Tooltip("Assign a scene-editable Lava material. Leave empty to keep the current ground surface.")]
        [SerializeField] private Material lavaGroundMaterial;
        [Header("Lava World entry requirement")]
        [Tooltip("The shared miner progression used by the Ground HUD. Required to enter Lava World.")]
        [SerializeField] private NpcProgressionSystem minerProgression;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MiningAudioManager audioManager;
        [Tooltip("Minimum miner level required. Set to 0 to disable this requirement.")]
        [Min(0), SerializeField] private int requiredMinerLevel = 2;
        [Tooltip("Minimum mining power needed to enter. Set to 0 to allow entry immediately.")]
        [Min(0), SerializeField] private int requiredMiningPower = 8;
        [Tooltip("Minimum coin balance to enter; this is a threshold, not a purchase. Set 0 to disable.")]
        [Min(0f), SerializeField] private float requiredMoney = 1000f;

        private readonly Dictionary<GameObject, bool> groundTreeStates = new();
        private readonly Dictionary<Terrain, bool> terrainTreeStates = new();
        private readonly Dictionary<GameObject, bool> initialLavaStates = new();
        private readonly Dictionary<GameObject, bool> groundContentStates = new();
        private readonly Dictionary<GameObject, bool> lavaContentStates = new();
        private Material[] originalGroundMaterials;
        private Material originalTerrainMaterial;
        private Material runtimeTerrainLavaMaterial;
        private MaterialPropertyBlock originalGroundProperties;
        private static readonly int MapCenterId = Shader.PropertyToID("_MapCenter");
        private static readonly int MapSizeId = Shader.PropertyToID("_MapSize");
        public bool IsInLavaWorld => oreSpawner != null && oreSpawner.LavaWorldActive;
        public int RequiredMiningPower => requiredMiningPower;
        public int CurrentMiningPower => minerProgression != null ? minerProgression.CurrentMiningPower : 0;

        private void Awake()
        {
            if (minerProgression == null)
                minerProgression = FindFirstObjectByType<NpcProgressionSystem>(FindObjectsInactive.Include);
            if (wallet == null)
                wallet = FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            if (audioManager == null)
                audioManager = FindFirstObjectByType<MiningAudioManager>(FindObjectsInactive.Include);
            if (groundRenderer != null)
            {
                originalGroundMaterials = groundRenderer.sharedMaterials;
                originalGroundProperties = new MaterialPropertyBlock();
                groundRenderer.GetPropertyBlock(originalGroundProperties);
            }
            if (groundTerrain != null)
                originalTerrainMaterial = groundTerrain.materialTemplate;
            foreach (GameObject root in surroundingTrees)
                if (root != null && !groundTreeStates.ContainsKey(root))
                    groundTreeStates.Add(root, root.activeSelf);
            // Older portal scenes have an empty tree list. Discover only clearly
            // named forest roots, using the same rule as the editor setup menu.
            if (surroundingTrees.Count == 0)
            {
                foreach (Transform candidate in FindObjectsByType<Transform>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (candidate.gameObject.scene != gameObject.scene ||
                        !IsTreeName(candidate.name)) continue;
                    bool hasTreeAncestor = false;
                    for (Transform parent = candidate.parent; parent != null; parent = parent.parent)
                        if (IsTreeName(parent.name)) { hasTreeAncestor = true; break; }
                    if (!hasTreeAncestor && !transform.IsChildOf(candidate) &&
                        !groundTreeStates.ContainsKey(candidate.gameObject))
                        groundTreeStates.Add(candidate.gameObject, candidate.gameObject.activeSelf);
                }
            }
            foreach (GameObject root in lavaDecorations)
                if (root != null && !initialLavaStates.ContainsKey(root))
                    initialLavaStates.Add(root, root.activeSelf);
            foreach (Terrain terrain in groundTerrains)
                if (terrain != null && !terrainTreeStates.ContainsKey(terrain))
                    terrainTreeStates.Add(terrain, terrain.drawTreesAndFoliage);
            foreach (GameObject root in groundOnlyRoots) RememberGroundRoot(root);
            // Existing scene systems are Ground content even when the portal's new lists
            // have not yet been filled in the Inspector. Their own OnDisable cleans up drops.
            foreach (MiningChestSpawner system in FindObjectsByType<MiningChestSpawner>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                RememberGroundRoot(system.gameObject);
            foreach (LuckyBlockDropSystem system in FindObjectsByType<LuckyBlockDropSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                RememberGroundRoot(system.gameObject);
            foreach (MonsterSpawnZone system in FindObjectsByType<MonsterSpawnZone>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                RememberGroundRoot(system.gameObject);
            foreach (WanderingTraderSystem system in FindObjectsByType<WanderingTraderSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                RememberGroundRoot(system.gameObject);
            foreach (GameObject root in lavaOnlyRoots)
                if (root != null && root.scene == gameObject.scene &&
                    !lavaContentStates.ContainsKey(root))
                    lavaContentStates.Add(root, root.activeSelf);
        }

        private void RememberGroundRoot(GameObject root)
        {
            if (root == null || root.scene != gameObject.scene || root == gameObject ||
                transform.IsChildOf(root.transform) ||
                (oreSpawner != null && oreSpawner.transform.IsChildOf(root.transform)) ||
                groundContentStates.ContainsKey(root)) return;
            groundContentStates.Add(root, root.activeSelf);
        }

        private static bool IsTreeName(string value)
        {
            string name = value.ToLowerInvariant();
            return name.StartsWith("tree") || name.StartsWith("pine") ||
                   name.Contains(" trees") || name.Contains("forest");
        }

        public bool CanTravel(out string englishReason, out string vietnameseReason)
        {
            englishReason = null;
            vietnameseReason = null;
            if (oreSpawner == null || transition == null)
            {
                englishReason = "Portal is not set up. Assign the spawner and radial transition.";
                vietnameseReason = "Cổng chưa được thiết lập. Gán spawner và hiệu ứng chuyển cảnh.";
                return false;
            }
            if (IsInLavaWorld) return true; // Return trip must always remain available.
            var missingEnglish = new List<string>();
            var missingVietnamese = new List<string>();
            if (requiredMinerLevel > 0 && (minerProgression == null ||
                minerProgression.CurrentLevel < requiredMinerLevel))
            {
                int level = minerProgression != null ? minerProgression.CurrentLevel : 0;
                missingEnglish.Add("Level " + level + "/" + requiredMinerLevel);
                missingVietnamese.Add("Cấp " + level + "/" + requiredMinerLevel);
            }
            if (requiredMiningPower > 0 && (minerProgression == null ||
                minerProgression.CurrentMiningPower < requiredMiningPower))
            {
                missingEnglish.Add("Power " + CurrentMiningPower + "/" + requiredMiningPower);
                missingVietnamese.Add("Lực đào " + CurrentMiningPower + "/" + requiredMiningPower);
            }
            if (requiredMoney > 0f && (wallet == null || wallet.CurrentMoney < requiredMoney))
            {
                string balance = MiningMoneyFormatter.Format(wallet != null ? wallet.CurrentMoney : 0f);
                string cost = MiningMoneyFormatter.Format(requiredMoney);
                missingEnglish.Add("Coins " + balance + "/" + cost);
                missingVietnamese.Add("Xu " + balance + "/" + cost);
            }
            if (!oreSpawner.HasSpawnableLavaOre())
            {
                missingEnglish.Add("Lava ore unavailable");
                missingVietnamese.Add("Thiếu quặng Lava");
            }
            if (missingEnglish.Count == 0) return true;
            englishReason = "To enter Lava World: " + string.Join(" | ", missingEnglish);
            vietnameseReason = "Điều kiện vào Lava: " + string.Join(" | ", missingVietnamese);
            return false;
        }

        public void Travel()
        {
            if (!isActiveAndEnabled || transition == null || transition.IsPlaying || oreSpawner == null) return;
            if (!CanTravel(out string reason, out _))
            {
                Debug.LogWarning(reason, this);
                return;
            }
            transition.PlayTransition(SwapCovered);
        }

        private void SwapCovered()
        {
            bool entering = !IsInLavaWorld;
            if (entering) SetGroundContent(false);
            if (!oreSpawner.SetLavaWorld(entering))
            {
                if (entering) SetGroundContent(true);
                return;
            }
            if (!entering) SetGroundContent(true);
            foreach (var pair in groundTreeStates)
                if (pair.Key != null) pair.Key.SetActive(entering ? false : pair.Value);
            foreach (var pair in terrainTreeStates)
                if (pair.Key != null) pair.Key.drawTreesAndFoliage = entering ? false : pair.Value;
            foreach (var pair in initialLavaStates)
                if (pair.Key != null) pair.Key.SetActive(entering || pair.Value);
            foreach (var pair in lavaContentStates)
                if (pair.Key != null) pair.Key.SetActive(entering || pair.Value);
            ApplyGroundMaterial(entering);
            if (audioManager != null) audioManager.SetLavaWorldAmbience(entering);
        }

        private void SetGroundContent(bool ground)
        {
            foreach (var pair in groundContentStates)
                if (pair.Key != null) pair.Key.SetActive(ground && pair.Value);
        }

        private void ApplyGroundMaterial(bool lava)
        {
            if (groundRenderer != null && lavaGroundMaterial != null &&
                originalGroundMaterials != null && originalGroundMaterials.Length > 0)
            {
                Material[] materials = (Material[])originalGroundMaterials.Clone();
                if (lava) materials[0] = lavaGroundMaterial;
                groundRenderer.sharedMaterials = materials;
                if (lava)
                {
                    Bounds bounds = groundRenderer.bounds;
                    var properties = new MaterialPropertyBlock();
                    groundRenderer.GetPropertyBlock(properties);
                    properties.SetVector(MapCenterId, new Vector4(bounds.center.x, bounds.center.z, 0, 0));
                    properties.SetVector(MapSizeId, new Vector4(
                        Mathf.Max(0.01f, bounds.size.x), Mathf.Max(0.01f, bounds.size.z), 0, 0));
                    groundRenderer.SetPropertyBlock(properties);
                }
                else groundRenderer.SetPropertyBlock(originalGroundProperties);
            }
            if (groundTerrain != null && lavaGroundMaterial != null)
            {
                if (lava && runtimeTerrainLavaMaterial == null)
                {
                    runtimeTerrainLavaMaterial = new Material(lavaGroundMaterial);
                    Vector3 origin = groundTerrain.transform.position;
                    Vector3 size = groundTerrain.terrainData.size;
                    runtimeTerrainLavaMaterial.SetVector(MapCenterId,
                        new Vector4(origin.x + size.x * 0.5f, origin.z + size.z * 0.5f, 0, 0));
                    runtimeTerrainLavaMaterial.SetVector(MapSizeId,
                        new Vector4(size.x, size.z, 0, 0));
                }
                groundTerrain.materialTemplate = lava ? runtimeTerrainLavaMaterial : originalTerrainMaterial;
            }
        }

        private void OnDestroy()
        {
            if (runtimeTerrainLavaMaterial != null) Destroy(runtimeTerrainLavaMaterial);
        }
    }
}
