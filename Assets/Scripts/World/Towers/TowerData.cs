using UnityEngine;
namespace MiningSimulator.Ores
{
    public abstract class TowerData : ScriptableObject
    {
        public string towerId = "cannon";
        public string displayName = "Cannon";
        [Min(0)] public float price = 100f;
        [Min(0)] public float damage = 5f;
        [Min(.1f)] public float range = 10f;
        [Min(.01f)] public float attackSpeed = 1f;
        [Min(1)] public float health = 100f;
        [Min(0)] public float magicResistance;
        [Min(0)] public float armor;
        [Min(.1f)] public float projectileSpeed = 12f;
        [Min(1)] public int level = 1;
        [Header("Per placed tower upgrades")]
        [Min(1)] public int maximumLevel = 10;
        [Min(0)] public float upgradePrice = 75f;
        [Min(1)] public float upgradeCostMultiplier = 1.5f;
        [Min(0)] public float damageGrowth = .2f, healthGrowth = .25f;
        [Min(0)] public float attackSpeedGrowth = .05f, rangeGrowth = .05f;
        [Min(0)] public float armorPerLevel = 2f, magicResistancePerLevel = 2f;
        public int ClampLevel(int value) => Mathf.Clamp(value, Mathf.Max(1, level), Mathf.Max(level, maximumLevel));
        public float UpgradeCost(int currentLevel) => upgradePrice * Mathf.Pow(upgradeCostMultiplier, ClampLevel(currentLevel) - Mathf.Max(1, level));
        public float Scaled(float value, float growth, int currentLevel) => value * (1 + growth * (ClampLevel(currentLevel) - Mathf.Max(1, level)));
        public GameObject prefab, projectile;
        [Header("Projectile reuse (0 destroys returned shots)")]
        [Min(0)] public int projectilePoolCapacity = 64;
        public Sprite icon;
        public MiningItemData inventoryItem;
        public string Summary => $"LV {level} • DMG {damage:0.##} • Range {range:0.#}m • AS {attackSpeed:0.##}\nHP {health:0.#} • Armor {armor:0.#} • MR {magicResistance:0.#}";
    }
}
