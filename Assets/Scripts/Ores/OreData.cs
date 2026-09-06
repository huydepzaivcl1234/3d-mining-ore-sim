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

        [Header("Presentation")]
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
        }
    }
}
