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
        public GameObject prefab, projectile;
        public Sprite icon;
        public MiningItemData inventoryItem;
        public string Summary => $"LV {level} • DMG {damage:0.##} • Range {range:0.#}m • AS {attackSpeed:0.##}\nHP {health:0.#} • Armor {armor:0.#} • MR {magicResistance:0.#}";
    }
}
