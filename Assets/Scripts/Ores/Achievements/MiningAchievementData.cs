using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum MiningAchievementTrigger
    {
        MinedOreKind = 0,
        ConcurrentNpcCount = 1,
        AllConfiguredOresUnlocked = 2,
        RebirthCount = 3
    }

    [Serializable]
    public sealed class MiningAchievementDefinition
    {
        [Header("Identity")]
        [SerializeField] private string achievementId;
        [SerializeField] private string englishTitle;
        [SerializeField] private string vietnameseTitle;
        [TextArea, SerializeField] private string englishDescription;
        [TextArea, SerializeField] private string vietnameseDescription;

        [Header("Goal")]
        [SerializeField] private MiningAchievementTrigger trigger;
        [SerializeField] private OreKind oreKind;
        [Min(1), SerializeField] private long targetAmount = 1;
        [Tooltip("Used by All Configured Ores Unlocked. This explicit list also supports day/night ores that are not in the normal spawn table.")]
        [SerializeField] private List<OreData> requiredOres = new();

        [Header("Permanent Reward")]
        [Min(0f), SerializeField] private float moneyMultiplierPercent = 1f;
        [Min(0f), SerializeField] private float experienceMultiplierPercent = 1f;

        public string AchievementId => achievementId;
        public string Title => MiningLocalization.Text(englishTitle, vietnameseTitle);
        public string Description => MiningLocalization.Text(englishDescription, vietnameseDescription);
        public MiningAchievementTrigger Trigger => trigger;
        public OreKind OreKind => oreKind;
        public long TargetAmount => trigger == MiningAchievementTrigger.AllConfiguredOresUnlocked
            ? Math.Max(1, CountUniqueRequiredOres())
            : Math.Max(1, targetAmount);
        public IReadOnlyList<OreData> RequiredOres => requiredOres;
        public float MoneyMultiplierPercent => Mathf.Max(0f, moneyMultiplierPercent);
        public float ExperienceMultiplierPercent => Mathf.Max(0f, experienceMultiplierPercent);

        private int CountUniqueRequiredOres()
        {
            var kinds = new HashSet<OreKind>();
            foreach (OreData ore in requiredOres)
            {
                if (ore != null)
                {
                    kinds.Add(ore.Kind);
                }
            }
            return kinds.Count;
        }
    }

    /// <summary>Designer-owned achievement goals, permanent rewards, and save key.</summary>
    [CreateAssetMenu(fileName = "MiningAchievementData",
        menuName = "Mining Simulator/Game Data/Achievements")]
    public sealed class MiningAchievementData : ScriptableObject
    {
        [SerializeField] private string saveKey = "MiningSimulator.Achievements.v1";
        [SerializeField] private List<MiningAchievementDefinition> achievements = new();

        public string SaveKey => saveKey;
        public IReadOnlyList<MiningAchievementDefinition> Achievements => achievements;

        public MiningAchievementDefinition FindById(string achievementId)
        {
            if (string.IsNullOrWhiteSpace(achievementId))
            {
                return null;
            }

            foreach (MiningAchievementDefinition achievement in achievements)
            {
                if (achievement != null && achievement.AchievementId == achievementId)
                {
                    return achievement;
                }
            }
            return null;
        }
    }
}
