using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum OreKind
    {
        Stone = 0,
        Coal = 1,
        Copper = 2,
        Iron = 3,
        Gold = 4,
        Diamond = 5,
        LightStone = 6,
        DarkStone = 7
    }

    public enum OreRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
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
        [HideInInspector, SerializeField] private bool rareOre;
        [SerializeField] private OreRarity rarity;
        [SerializeField] private Vector3 spawnRotationOffset;
        [Tooltip("Extra world-space height applied after the collider is placed above the ground.")]
        [SerializeField] private float spawnHeightOffset;
        [Tooltip("World-space offset from the top-center of the ore collider bounds.")]
        [SerializeField] private Vector3 healthBarWorldOffset = new(0f, 0.25f, 0f);
        [Min(0.01f), SerializeField] private float healthBarScale = 0.65f;
        [Header("Hit Feedback")]
        [Range(0f, 0.5f), SerializeField] private float hitPunchScale = 0.08f;
        [Min(0f), SerializeField] private float hitPunchLift = 0.12f;
        [Min(0.01f), SerializeField] private float hitPunchDuration = 0.16f;
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
        public OreRarity Rarity => rarity;
        public bool RareOre => rarity >= OreRarity.Rare;
        public Vector3 SpawnRotationOffset => spawnRotationOffset;
        public float SpawnHeightOffset => spawnHeightOffset;
        public Vector3 HealthBarWorldOffset => healthBarWorldOffset;
        public float HealthBarScale => healthBarScale;
        public float HitPunchScale => hitPunchScale;
        public float HitPunchLift => hitPunchLift;
        public float HitPunchDuration => hitPunchDuration;
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
            hitPunchScale = Mathf.Clamp(hitPunchScale, 0f, 0.5f);
            hitPunchLift = Mathf.Max(0f, hitPunchLift);
            hitPunchDuration = Mathf.Max(0.01f, hitPunchDuration);
        }
    }
}
