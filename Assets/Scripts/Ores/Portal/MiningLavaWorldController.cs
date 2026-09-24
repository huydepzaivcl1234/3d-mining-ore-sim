using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>One-scene world swap. Ground, managers, NPCs and their save data stay intact.</summary>
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

        private readonly Dictionary<GameObject, bool> groundTreeStates = new();
        private readonly Dictionary<Terrain, bool> terrainTreeStates = new();
        private readonly Dictionary<GameObject, bool> initialLavaStates = new();
        public bool IsInLavaWorld => oreSpawner != null && oreSpawner.LavaWorldActive;

        private void Awake()
        {
            foreach (GameObject root in surroundingTrees)
                if (root != null && !groundTreeStates.ContainsKey(root))
                    groundTreeStates.Add(root, root.activeSelf);
            foreach (GameObject root in lavaDecorations)
                if (root != null && !initialLavaStates.ContainsKey(root))
                    initialLavaStates.Add(root, root.activeSelf);
            foreach (Terrain terrain in groundTerrains)
                if (terrain != null && !terrainTreeStates.ContainsKey(terrain))
                    terrainTreeStates.Add(terrain, terrain.drawTreesAndFoliage);
        }

        public void Travel()
        {
            if (!isActiveAndEnabled || transition == null || transition.IsPlaying || oreSpawner == null) return;
            if (!IsInLavaWorld && !oreSpawner.HasSpawnableLavaOre())
            {
                Debug.LogWarning("Lava World needs an unlocked Lava ore. Configure Ore System > Lava World.", this);
                return;
            }
            transition.PlayTransition(SwapCovered);
        }

        private void SwapCovered()
        {
            bool entering = !IsInLavaWorld;
            if (!oreSpawner.SetLavaWorld(entering)) return;
            foreach (var pair in groundTreeStates)
                if (pair.Key != null) pair.Key.SetActive(entering ? false : pair.Value);
            foreach (var pair in terrainTreeStates)
                if (pair.Key != null) pair.Key.drawTreesAndFoliage = entering ? false : pair.Value;
            foreach (var pair in initialLavaStates)
                if (pair.Key != null) pair.Key.SetActive(entering || pair.Value);
        }
    }
}
