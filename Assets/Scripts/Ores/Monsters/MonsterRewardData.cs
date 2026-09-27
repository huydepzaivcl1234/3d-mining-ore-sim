using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class MonsterItemDrop
    {
        public MiningItemData item;
        [Range(0, 100)] public float chancePercent = 25;
        [Min(1)] public int minimumAmount = 1;
        [Min(1)] public int maximumAmount = 1;
        [Tooltip("Optional override. Otherwise use the item's inventory icon.")]
        public Sprite icon;
    }
    [CreateAssetMenu(menuName = "Mining Simulator/Game Data/Monster Rewards")]
    public sealed class MonsterRewardData : ScriptableObject
    {
        [Header("Random level - geometric rarity, no gameplay level cap")]
        [Min(1)] public int minimumLevel = 1;
        [Range(0, 0.99f)] public float higherLevelChance = 0.35f;
        [Tooltip("Linear growth: level 2 = 1.75x, level 3 = 2.5x at 0.75.")]
        [Min(0)] public float statGrowthPerLevel = 0.75f;
        [Min(0)] public float goldGrowthPerLevel = 0.75f;
        public Vector2 randomStatMultiplier = new Vector2(0.9f, 1.1f);
        public int RollLevel(float sample)
        {
            double chance = Mathf.Clamp(higherLevelChance, 0, 0.99f);
            double extra = chance <= 0 ? 0 : System.Math.Floor(System.Math.Log(System.Math.Max(1e-12, 1 - Mathf.Clamp01(sample))) / System.Math.Log(chance));
            return (int)System.Math.Min(int.MaxValue, System.Math.Max(1, minimumLevel) + extra);
        }
        public float StatMultiplier(int level) => 1 + Mathf.Max(0, statGrowthPerLevel) * (Mathf.Max(1, level) - 1);
        public float GoldMultiplier(int level) => 1 + Mathf.Max(0, goldGrowthPerLevel) * (Mathf.Max(1, level) - 1);
        [Min(0)] public float experience = 25;
        [Min(0)] public float gold = 50;
        public List<MonsterItemDrop> drops = new();
        [Header("Icon drop motion")]
        [Min(0.01f)] public float iconSize = 0.45f;
        [Min(0)] public float launchUpSpeed = 4;
        [Min(0)] public float launchOutSpeed = 2;
        [Min(0)] public float groundWaitSeconds = 0.6f;
        [Min(0.1f)] public float attractionRadius = 30;
        [Min(0.1f)] public float attractionSpeed = 2;
        [Min(0)] public float attractionAcceleration = 3;
    }
}
