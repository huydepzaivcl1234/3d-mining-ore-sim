using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum MonsterDataMode { PerSpecies, AllMonsters }

    [CreateAssetMenu(menuName = "Survivor/Monster Loot and Rewards")]
    public sealed class MonsterLootSettings : ScriptableObject
    {
        [Tooltip("Cards and item drops. Does not change gold or XP.")]
        public MonsterDataMode dropMode;
        [Tooltip("Gold, XP and boss reward multipliers. Does not change drops.")]
        public MonsterDataMode rewardMode;
        public MonsterRewardData shared;

        private static MonsterLootSettings current;
        public static MonsterLootSettings Current => current ? current :
            current = Resources.Load<MonsterLootSettings>("MonsterLootSettings");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => current = null;

        public static MonsterRewardData DropsFor(MonsterRewardData species)
        {
            var settings = Current;
            return settings && settings.dropMode == MonsterDataMode.AllMonsters && settings.shared
                ? settings.shared : species;
        }

        public static MonsterRewardData RewardsFor(MonsterRewardData species)
        {
            var settings = Current;
            return settings && settings.rewardMode == MonsterDataMode.AllMonsters && settings.shared
                ? settings.shared : species;
        }

        public static float BossGold(float original)
        {
            var settings = Current;
            return settings && settings.rewardMode == MonsterDataMode.AllMonsters && settings.shared
                ? settings.shared.boss.goldMultiplier : original;
        }

        public static float BossExperience(float original)
        {
            var settings = Current;
            return settings && settings.rewardMode == MonsterDataMode.AllMonsters && settings.shared
                ? settings.shared.boss.experienceMultiplier : original;
        }
    }
}
