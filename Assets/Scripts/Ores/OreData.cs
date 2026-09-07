using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum OreKind
    {
        Stone = 0,
        Coal = 1,
        Copper = 2
    }

    /// <summary>
    /// Authoring data for one mineable ore type. Create one asset per ore so balancing
    /// and prefab references stay independent.
    /// </summary>
    [CreateAssetMenu(fileName = "NewOreData", menuName = "Mining Simulator/Ore Data")]
    public sealed class OreData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private OreKind kind;
        [SerializeField] private string displayName = "New Ore";
        [TextArea, SerializeField] private string description;

        [Header("Progression")]
        [Min(1), SerializeField] private int tier = 1;
        [Min(1), SerializeField] private int miningPowerRequired = 1;
        [Min(1), SerializeField] private int durability = 10;
        [Min(1), SerializeField] private int clickDamage = 1;

        [Header("Economy")]
        [Min(0), SerializeField] private int baseSellValue = 1;

        [Header("Depletion")]
        [Min(0f), SerializeField] private float destroyDelay;

        [Header("NPC Mining Slots")]
        [Min(1), SerializeField] private int maximumMiningNpcs = 3;
        [Min(0.1f), SerializeField] private float npcStandDistance = 1.4f;

        [Header("Presentation")]
        [SerializeField] private bool rareOre;
        [SerializeField] private Vector3 spawnRotationOffset;
        [Tooltip("Extra world-space height applied after the collider is placed above the ground.")]
        [SerializeField] private float spawnHeightOffset;
        [Tooltip("World-space offset from the top-center of the ore collider bounds.")]
        [SerializeField] private Vector3 healthBarWorldOffset = new(0f, 0.25f, 0f);
        [Min(0.01f), SerializeField] private float healthBarScale = 0.65f;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Color mapColor = Color.gray;

        public OreKind Kind => kind;
        public string DisplayName => displayName;
        public string Description => description;
        public int Tier => tier;
        public int MiningPowerRequired => miningPowerRequired;
        public int Durability => durability;
        public int MaxHealth => durability;
        public int ClickDamage => clickDamage;
        public int BaseSellValue => baseSellValue;
        public float DestroyDelay => destroyDelay;
        public int MaximumMiningNpcs => maximumMiningNpcs;
        public float NpcStandDistance => npcStandDistance;
        public bool RareOre => rareOre;
        public Vector3 SpawnRotationOffset => spawnRotationOffset;
        public float SpawnHeightOffset => spawnHeightOffset;
        public Vector3 HealthBarWorldOffset => healthBarWorldOffset;
        public float HealthBarScale => healthBarScale;
        public GameObject Prefab => prefab;
        public Color MapColor => mapColor;

        private void OnValidate()
        {
            tier = Mathf.Max(1, tier);
            miningPowerRequired = Mathf.Max(1, miningPowerRequired);
            durability = Mathf.Max(1, durability);
            clickDamage = Mathf.Max(1, clickDamage);
            baseSellValue = Mathf.Max(0, baseSellValue);
            destroyDelay = Mathf.Max(0f, destroyDelay);
            maximumMiningNpcs = Mathf.Max(1, maximumMiningNpcs);
            npcStandDistance = Mathf.Max(0.1f, npcStandDistance);
            healthBarScale = Mathf.Max(0.01f, healthBarScale);
        }
    }
}
