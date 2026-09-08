using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class OreSpawnEntry
    {
        [FormerlySerializedAs("data"), SerializeField] private OreData ore;
        [FormerlySerializedAs("weight"), Min(0f), SerializeField] private float spawnWeight = 1f;

        public OreData Ore => ore;
        public float SpawnWeight => spawnWeight;
    }

    [Serializable]
    public sealed class OreRaritySpawnRule
    {
        [SerializeField] private OreRarity rarity;
        [Min(0f), SerializeField] private float baseWeightMultiplier = 1f;
        [SerializeField] private bool affectedByRareUpgrade;
        [Min(0f), SerializeField] private float zeroWeightUnlockAtOneHundredPercentBonus;

        public OreRarity Rarity => rarity;
        public float BaseWeightMultiplier => baseWeightMultiplier;
        public bool AffectedByRareUpgrade => affectedByRareUpgrade;
        public float ZeroWeightUnlockAtOneHundredPercentBonus =>
            zeroWeightUnlockAtOneHundredPercentBonus;
    }

    /// <summary>Designer-owned ore spawn ratios, timing, limits, and placement.</summary>
    [CreateAssetMenu(fileName = "OreSpawnData", menuName = "Mining Simulator/Game Data/Ore Spawn")]
    public sealed class OreSpawnData : ScriptableObject
    {
        [Header("Spawn Table")]
        [SerializeField] private List<OreSpawnEntry> oreSpawnTable = new();

        [Header("Rarity Rules")]
        [Tooltip("Controls rarity weight and how zero-weight rare ores unlock through the rare-spawn upgrade.")]
        [SerializeField] private List<OreRaritySpawnRule> rarityRules = new();

        [Header("Timing And Population")]
        [SerializeField] private bool spawnOnEnable = true;
        [Min(0), SerializeField] private int initialSpawnCount = 8;
        [Min(0), SerializeField] private int maximumAliveOres = 12;
        [Tooltip("Maximum inactive ore instances retained for reuse across all ore types. Set to 0 to disable caching.")]
        [Min(0), SerializeField] private int maximumPooledOres = 12;
        [Min(0.05f), SerializeField] private float secondsPerSpawn = 2f;

        [Header("Spawn Area")]
        [SerializeField] private Vector3 areaCenter;
        [SerializeField] private Vector3 areaSize = new(16f, 0f, 16f);
        [SerializeField] private float heightOffset;
        [SerializeField] private bool randomYRotation = true;
        [SerializeField] private Vector2 randomYRotationRange = new(0f, 360f);
        [SerializeField] private Vector2 uniformScaleRange = Vector2.one;

        [Header("Surface Placement")]
        [SerializeField] private bool keepOreAboveSurface = true;
        [Min(0f), SerializeField] private float surfaceClearance = 0.05f;

        [Header("Optional Ground Placement")]
        [SerializeField] private bool alignToGround;
        [SerializeField] private LayerMask groundLayers = ~0;
        [Min(0.1f), SerializeField] private float groundRayStartHeight = 20f;
        [Min(0.1f), SerializeField] private float groundRayDistance = 50f;

        [Header("Scene Editing")]
        [SerializeField] private Color spawnAreaGizmoColor = new(0.95f, 0.65f, 0.12f, 0.65f);

        public IReadOnlyList<OreSpawnEntry> OreSpawnTable => oreSpawnTable;
        public IReadOnlyList<OreRaritySpawnRule> RarityRules => rarityRules;
        public bool SpawnOnEnable => spawnOnEnable;
        public int InitialSpawnCount => initialSpawnCount;
        public int MaximumAliveOres => maximumAliveOres;
        public int MaximumPooledOres => maximumPooledOres;
        public float SecondsPerSpawn => secondsPerSpawn;
        public Vector3 AreaCenter => areaCenter;
        public Vector3 AreaSize => areaSize;
        public float HeightOffset => heightOffset;
        public bool RandomYRotation => randomYRotation;
        public Vector2 RandomYRotationRange => randomYRotationRange;
        public Vector2 UniformScaleRange => uniformScaleRange;
        public bool KeepOreAboveSurface => keepOreAboveSurface;
        public float SurfaceClearance => surfaceClearance;
        public bool AlignToGround => alignToGround;
        public LayerMask GroundLayers => groundLayers;
        public float GroundRayStartHeight => groundRayStartHeight;
        public float GroundRayDistance => groundRayDistance;
        public Color SpawnAreaGizmoColor => spawnAreaGizmoColor;

        public OreRaritySpawnRule GetRarityRule(OreRarity rarity)
        {
            foreach (OreRaritySpawnRule rule in rarityRules)
            {
                if (rule != null && rule.Rarity == rarity)
                {
                    return rule;
                }
            }
            return null;
        }

        private void OnValidate()
        {
            initialSpawnCount = Mathf.Max(0, initialSpawnCount);
            maximumAliveOres = Mathf.Max(0, maximumAliveOres);
            maximumPooledOres = Mathf.Max(0, maximumPooledOres);
            secondsPerSpawn = Mathf.Max(0.05f, secondsPerSpawn);
            areaSize = new Vector3(Mathf.Abs(areaSize.x), Mathf.Abs(areaSize.y), Mathf.Abs(areaSize.z));
            surfaceClearance = Mathf.Max(0f, surfaceClearance);
            groundRayStartHeight = Mathf.Max(0.1f, groundRayStartHeight);
            groundRayDistance = Mathf.Max(0.1f, groundRayDistance);
        }
    }
}
