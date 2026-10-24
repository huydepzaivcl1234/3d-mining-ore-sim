using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum MiningQuestPeriod
    {
        Daily = 0,
        Weekly = 1
    }

    public enum MiningQuestObjective
    {
        MineOre = 0,
        PurchaseNpc = 1,
        Rebirth = 2,
        KillMonster = 3,
        KillBoss = 4
    }

    public enum QuestMonsterSpecies { None, Mushroom, Golem, Bat, Mushranon, ForestGolem, Skullclaw }
    public enum QuestDifficulty { Easy, Medium, Hard }

    public enum MiningQuestRewardType
    {
        Money = 0,
        RandomMoney = 1
    }

    [Serializable]
    public sealed class MiningQuestDefinition
    {
        [SerializeField] private string questId;
        [SerializeField] private MiningQuestPeriod period;
        [SerializeField] private MiningQuestObjective objective;
        [SerializeField] private QuestMonsterSpecies species;
        [SerializeField] private QuestDifficulty difficulty;
        [Min(1), SerializeField] private int targetAmount = 1;
        [SerializeField] private string englishName;
        [SerializeField] private string vietnameseName;
        [SerializeField] private MiningQuestRewardType rewardType;
        [Min(0f), SerializeField] private float moneyReward = 100f;
        [Min(0f), SerializeField] private float randomMoneyMinimum = 100f;
        [Min(0f), SerializeField] private float randomMoneyMaximum = 200f;

        public MiningQuestDefinition(string id, MiningQuestPeriod questPeriod,
            MiningQuestObjective questObjective, int target, string english, string vietnamese,
            MiningQuestRewardType type, float reward, float randomMinimum = 0f,
            float randomMaximum = 0f, QuestMonsterSpecies monsterSpecies = QuestMonsterSpecies.None,
            QuestDifficulty questDifficulty = QuestDifficulty.Easy)
        {
            questId = id;
            period = questPeriod;
            objective = questObjective;
            species = monsterSpecies;
            difficulty = questDifficulty;
            targetAmount = target;
            englishName = english;
            vietnameseName = vietnamese;
            rewardType = type;
            moneyReward = reward;
            randomMoneyMinimum = randomMinimum;
            randomMoneyMaximum = randomMaximum;
        }

        public string QuestId => questId?.Trim();
        public MiningQuestPeriod Period => period;
        public MiningQuestObjective Objective => objective;
        public int TargetAmount => Mathf.Max(1, targetAmount);
        public MiningQuestRewardType RewardType => rewardType;
        public float MoneyReward => Mathf.Max(0f, moneyReward);
        public float RandomMoneyMinimum => Mathf.Max(0f,
            Mathf.Min(randomMoneyMinimum, randomMoneyMaximum));
        public float RandomMoneyMaximum => Mathf.Max(RandomMoneyMinimum,
            Mathf.Max(randomMoneyMinimum, randomMoneyMaximum));
        public QuestDifficulty Difficulty => difficulty;
        public bool IsValid => !string.IsNullOrWhiteSpace(QuestId) &&
            (objective == MiningQuestObjective.Rebirth || objective == MiningQuestObjective.KillBoss ||
             objective == MiningQuestObjective.KillMonster && species != QuestMonsterSpecies.None);

        public bool MatchesKill(MonsterRewardData monster, bool boss)
        {
            if (objective == MiningQuestObjective.KillBoss) return boss;
            if (objective != MiningQuestObjective.KillMonster || monster == null) return false;
            return species switch
            {
                QuestMonsterSpecies.Mushroom => monster is MushroomData,
                QuestMonsterSpecies.Golem => monster is GolemData,
                QuestMonsterSpecies.Bat => monster is BatData,
                QuestMonsterSpecies.Mushranon => monster is MushranonData,
                QuestMonsterSpecies.ForestGolem => monster is ForestGolemData,
                QuestMonsterSpecies.Skullclaw => monster is SkullclawData,
                _ => false
            };
        }

        public string GetLocalizedName()
        {
            return MiningLocalization.Text(
                string.IsNullOrWhiteSpace(englishName) ? QuestId : englishName,
                string.IsNullOrWhiteSpace(vietnameseName) ? englishName : vietnameseName)
                .Replace("{0}", TargetAmount.ToString());
        }

        public string GetRewardPreview()
        {
            return rewardType == MiningQuestRewardType.RandomMoney
                ? string.Format(MiningLocalization.Text("{0} - {1} MONEY"),
                    MiningMoneyFormatter.Format(RandomMoneyMinimum),
                    MiningMoneyFormatter.Format(RandomMoneyMaximum))
                : string.Format(MiningLocalization.Text("{0} MONEY"),
                    MiningMoneyFormatter.Format(MoneyReward));
        }

        public float RollReward()
        {
            if (rewardType != MiningQuestRewardType.RandomMoney)
            {
                return MoneyReward;
            }

            float minimum = RandomMoneyMinimum;
            float maximum = RandomMoneyMaximum;
            return Mathf.Approximately(minimum, maximum)
                ? minimum
                : UnityEngine.Random.Range(minimum, maximum);
        }

        public void Validate()
        {
            questId = questId?.Trim();
            targetAmount = Mathf.Max(1, targetAmount);
            moneyReward = Mathf.Max(0f, moneyReward);
            randomMoneyMinimum = Mathf.Max(0f, randomMoneyMinimum);
            randomMoneyMaximum = Mathf.Max(randomMoneyMinimum, randomMoneyMaximum);
        }
    }

    [CreateAssetMenu(fileName = "MiningQuestData",
        menuName = "Mining Simulator/Game Data/Daily And Weekly Quests")]
    public sealed class MiningQuestData : ScriptableObject
    {
        [SerializeField] private string saveKey = "MiningSimulator.Quests.v1";
        [SerializeField] private List<MiningQuestDefinition> quests = new()
        {
            new MiningQuestDefinition("weekly_rebirth_1", MiningQuestPeriod.Weekly,
                MiningQuestObjective.Rebirth, 1, "Rebirth once this week",
                "Rebirth 1 lần trong tuần", MiningQuestRewardType.Money, 5000f)
        };

        public string SaveKey => string.IsNullOrWhiteSpace(saveKey)
            ? "MiningSimulator.Quests.v1"
            : saveKey.Trim();
        public IReadOnlyList<MiningQuestDefinition> Quests => quests ??
            (IReadOnlyList<MiningQuestDefinition>)Array.Empty<MiningQuestDefinition>();

        public MiningQuestDefinition GetDefinition(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId)) return null;
            foreach (MiningQuestDefinition quest in Quests)
            {
                if (quest != null && string.Equals(quest.QuestId, questId,
                        StringComparison.Ordinal))
                {
                    return quest;
                }
            }
            return null;
        }

        // Explicit authoring operation; never runs on load or resets existing progress/definitions.
        public void AddDefenseQuests()
        {
            AddKill("daily_mushroom", "Mushroom", 10, QuestMonsterSpecies.Mushroom, QuestDifficulty.Easy, 100);
            AddKill("daily_golem", "Golem", 5, QuestMonsterSpecies.Golem, QuestDifficulty.Medium, 200);
            AddKill("daily_bat", "Bat", 5, QuestMonsterSpecies.Bat, QuestDifficulty.Medium, 200);
            AddKill("daily_mushranon", "Mushranon", 5, QuestMonsterSpecies.Mushranon, QuestDifficulty.Easy, 100);
            AddKill("daily_forest_golem", "Forest Golem", 2, QuestMonsterSpecies.ForestGolem, QuestDifficulty.Hard, 400);
            AddKill("daily_skullclaw", "Skullclaw", 1, QuestMonsterSpecies.Skullclaw, QuestDifficulty.Hard, 500);
            if (GetDefinition("weekly_boss") == null)
                quests.Add(new MiningQuestDefinition("weekly_boss", MiningQuestPeriod.Weekly,
                    MiningQuestObjective.KillBoss, 5, "Defeat {0} bosses", "Hạ {0} boss",
                    MiningQuestRewardType.Money, 1500, questDifficulty: QuestDifficulty.Hard));
        }

        private void AddKill(string id, string name, int amount, QuestMonsterSpecies kind,
            QuestDifficulty grade, float reward)
        {
            quests ??= new List<MiningQuestDefinition>();
            if (GetDefinition(id) != null) return;
            quests.Add(new MiningQuestDefinition(id, MiningQuestPeriod.Daily,
                MiningQuestObjective.KillMonster, amount, "Defeat {0} " + name, "Hạ {0} " + name,
                MiningQuestRewardType.Money, reward, monsterSpecies: kind, questDifficulty: grade));
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(saveKey))
            {
                saveKey = "MiningSimulator.Quests.v1";
            }
            quests ??= new List<MiningQuestDefinition>();
            foreach (MiningQuestDefinition quest in quests)
            {
                quest?.Validate();
            }
        }
    }
}
