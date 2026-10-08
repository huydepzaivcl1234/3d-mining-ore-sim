using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class MonsterBossSettings
    {
        public bool enabled = true;
        [Range(0, 100)] public float chancePercent = 5f;
        [Min(1)] public int minimumPlayerLevel = 5;
        [Min(0)] public float sizeIncreasePercent = 35f;
        [Min(1)] public float healthMultiplier = 3f;
        [Min(0)] public float damageMultiplier = 1.5f;
        [Min(0)] public float goldMultiplier = 5f;
        [Min(0)] public float experienceMultiplier = 3f;
        [Tooltip("Optional separate loot, level and status-effect settings for this species' boss.")]
        public MonsterRewardData rewardOverride;
        public Color effectColor = new Color(1f, .2f, .06f, 1f);
        [Min(1)] public float combatSeconds = 120f;
        [Header("Species boss skill (normal monsters do not use this)")]
        public BossSkillKind skill;
        [Range(0, 100)] public float healTriggerHealthPercent = 20f;
        [Range(0, 100)] public float healMaxHealthPercent = 50f;
        [Min(.1f)] public float healSeconds = 5f;
        [Min(1)] public int slowEveryStrikes = 3;
        [Min(0)] public float slowRadius = 5f;
        [Range(0, 100)] public float slowPercent = 30f;
        [Min(.1f)] public float slowSeconds = 3f;
        [Range(.1f, 100)] public float hasteDamageThresholdPercent = 5f;
        [Min(0)] public float hasteAttackSpeedPercent = 100f;
        public bool hasteStacks;
        public bool CanSpawn(int playerLevel, bool ignoreLevel = false) => enabled && (ignoreLevel || playerLevel >= Mathf.Max(1, minimumPlayerLevel));
        public bool Roll(int playerLevel, float sample, bool ignoreLevel = false) => CanSpawn(playerLevel, ignoreLevel) &&
            (chancePercent >= 100f || Mathf.Clamp01(sample) < Mathf.Clamp(chancePercent, 0f, 100f) * .01f);
        public float ScaleMultiplier => 1f + Mathf.Max(0f, sizeIncreasePercent) * .01f;
    }
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
    /// <summary>Common stats/rewards only. Author a concrete species GameData from the Monsters menu.</summary>
    public class MonsterRewardData : ScriptableObject
    {
        [Tooltip("Species combat loadout. Null only for legacy assets not yet migrated.")]
        public MonsterCombatSettings combat = new MonsterCombatSettings();
        [HideInInspector] public int combatSettingsVersion;
        public bool HasCombatData => combatSettingsVersion > 0 && combat != null;
        [Header("Boss variant (same species)")]
        public MonsterBossSettings boss = new();
        [Header("Encounter expiry")]
        [Tooltip("Normal monster lifetime after spawning. 0 means unlimited. Bosses use Boss Combat Seconds.")]
        [Min(0)] public float combatSeconds;
        [Min(.1f)] public float dissolveSeconds = 2f;
        public Color dissolveColor = new Color(1f, .6f, .1f, 1f);
        public Shader dissolveShader;
        [Header("Spawn level relative to player (normal and boss)")]
        public bool usePlayerRelativeLevels = true;
        [Min(1)] public int minimumLevelsBelowPlayer = 1;
        [Min(1)] public int maximumLevelsBelowPlayer = 4;
        [Min(1)] public int maximumLevelsAbovePlayer = 1;
        [Tooltip("Relative spawn weights, normalized together. At player level 1, weaker rolls clamp to level 1. Daily growth applies only to the legacy roll below.")]
        [Range(0f, 1f)] public float weakerLevelWeight = .2f;
        [Range(0f, 1f)] public float equalLevelWeight = .7f;
        [Range(0f, 1f)] public float strongerLevelWeight = .1f;
        [Header("Legacy level roll (used only when relative levels are disabled)")]
        [Min(1)] public int minimumLevel = 1;
        [Tooltip("Optional legacy rule. Off keeps level 1 possible regardless of player level.")]
        public bool addPlayerLevelAtSpawn;
        [Tooltip("Used only when Add Player Level At Spawn is enabled: 1 = roll + player level.")]
        [Min(0f)] public float playerLevelContribution = 1f;
        [Range(0, 0.99f)] public float higherLevelChance = 0.35f;
        [Tooltip("Percentage points added to the higher-level roll for each completed day. 0.01 = +0.01% per day, not +1%.")]
        [Min(0f)] public float higherLevelIncreasePerDayPercent = 0.01f;
        [Tooltip("Maximum higher-level probability in percent. Caps the probability, not the monster level.")]
        [Range(0f, 99f)] public float maximumHigherLevelChancePercent = 50f;
        [Tooltip("Linear growth: level 2 = 1.75x, level 3 = 2.5x at 0.75.")]
        [Min(0)] public float statGrowthPerLevel = 0.75f;
        [Min(0)] public float goldGrowthPerLevel = 0.75f;
        public Vector2 randomStatMultiplier = new Vector2(0.9f, 1.1f);
        public float HigherLevelProbability(int dayNumber)
        {
            double completedDays = System.Math.Max(0L, (long)dayNumber - 1L);
            double probability = Mathf.Clamp(higherLevelChance, 0f, 0.99f) +
                completedDays * System.Math.Max(0d, higherLevelIncreasePerDayPercent) * 0.01d;
            return (float)System.Math.Min(probability,
                Mathf.Clamp(maximumHigherLevelChancePercent, 0f, 99f) * 0.01d);
        }
        public int RollLevel(float sample) => RollLevel(sample, 1);
        public int RollSpawnLevel(float sample, int playerLevel, int dayNumber)
        {
            if (!usePlayerRelativeLevels)
                return GetSpawnLevel(RollLevel(sample, dayNumber), playerLevel);

            sample = float.IsNaN(sample) ? 0f : Mathf.Clamp01(sample);
            int player = Mathf.Max(1, playerLevel);
            double lowerWeight = LevelWeight(weakerLevelWeight);
            double equalWeight = LevelWeight(equalLevelWeight);
            double higherWeight = LevelWeight(strongerLevelWeight);
            double total = lowerWeight + equalWeight + higherWeight;
            if (total <= 0d) return player;
            double lowerChance = lowerWeight / total;
            double higherChance = higherWeight / total;
            double equalEnd = (lowerWeight + equalWeight) / total;
            long level;
            if (higherChance > 0d && sample >= equalEnd)
            {
                int maximum = Mathf.Max(1, maximumLevelsAbovePlayer);
                double t = (sample - equalEnd) / higherChance;
                long offset = 1L + System.Math.Min(maximum - 1L, (long)System.Math.Floor(t * maximum));
                level = player + offset;
            }
            else if (lowerChance > 0d && sample < lowerChance)
            {
                int minimum = Mathf.Max(1, Mathf.Min(minimumLevelsBelowPlayer, maximumLevelsBelowPlayer));
                int maximum = Mathf.Max(minimum, Mathf.Max(minimumLevelsBelowPlayer, maximumLevelsBelowPlayer));
                long count = (long)maximum - minimum + 1L;
                double t = sample / lowerChance;
                long offset = minimum + System.Math.Min(count - 1L, (long)System.Math.Floor(t * count));
                level = player - offset;
            }
            else return player;
            return (int)System.Math.Max(1L, System.Math.Min(int.MaxValue, level));
        }
        private static double LevelWeight(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) ? 0d : Mathf.Clamp01(value);
        public int RollLevel(float sample, int dayNumber)
        {
            double chance = HigherLevelProbability(dayNumber);
            double extra = chance <= 0 ? 0 : System.Math.Floor(System.Math.Log(System.Math.Max(1e-12, 1 - Mathf.Clamp01(sample))) / System.Math.Log(chance));
            return (int)System.Math.Min(int.MaxValue, System.Math.Max(1, minimumLevel) + extra);
        }
        public int GetSpawnLevel(int rolledLevel, int playerLevel)
        {
            double contribution = addPlayerLevelAtSpawn
                ? System.Math.Floor(System.Math.Max(0, playerLevelContribution) * (double)Mathf.Max(1, playerLevel)) : 0d;
            return (int)System.Math.Min(int.MaxValue, Mathf.Max(1, rolledLevel) + contribution);
        }
        public float StatMultiplier(int level) => 1 + Mathf.Max(0, statGrowthPerLevel) * (Mathf.Max(1, level) - 1);
        public float GoldMultiplier(int level) => 1 + Mathf.Max(0, goldGrowthPerLevel) * (Mathf.Max(1, level) - 1);
        [Min(0)] public float experience = 25;
        [Min(0)] public float gold = 50;
        [Header("Defenses and direct attack damage type")]
        [Min(0f)] public float armor;
        [Min(0f)] public float magicResistance;
        [Min(0f)] public float armorPerLevel;
        [Min(0f)] public float magicResistancePerLevel;
        [Tooltip("Rating that halves incoming damage. 100 uses the LoL-style curve.")]
        [Min(.01f)] public float resistanceScale = 100f;
        public CombatDamageType attackDamageType = CombatDamageType.Physical;
        public float ArmorAtLevel(int level) => CombatDamage.NonNegative(armor) +
            CombatDamage.NonNegative(armorPerLevel) * (Mathf.Max(1, level) - 1);
        public float MagicResistanceAtLevel(int level) => CombatDamage.NonNegative(magicResistance) +
            CombatDamage.NonNegative(magicResistancePerLevel) * (Mathf.Max(1, level) - 1);
        [Header("Monster attack / healing effects (0 disables an effect)")]
        [Min(0)] public float burnDamagePerTick;
        [Min(0.1f)] public float burnTickSeconds = 1f;
        [Min(0)] public float burnDurationSeconds;
        [Range(0f, 100f)] public float lifeStealPercent;
        [Min(0)] public float healingBonusPercent;
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
