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
        Rebirth = 2
    }

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
            float randomMaximum = 0f)
        {
            questId = id;
            period = questPeriod;
            objective = questObjective;
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
        public bool IsValid => !string.IsNullOrWhiteSpace(QuestId) && TargetAmount > 0;

        public string GetLocalizedName()
        {
            return MiningLocalization.Text(
                string.IsNullOrWhiteSpace(englishName) ? QuestId : englishName,
                string.IsNullOrWhiteSpace(vietnameseName) ? englishName : vietnameseName);
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
            new MiningQuestDefinition("daily_mine_500", MiningQuestPeriod.Daily,
                MiningQuestObjective.MineOre, 500, "Mine 500 ores today",
                "Đào 500 quặng hôm nay", MiningQuestRewardType.Money, 1000f),
            new MiningQuestDefinition("daily_buy_3_npc", MiningQuestPeriod.Daily,
                MiningQuestObjective.PurchaseNpc, 3, "Hire 3 miners today",
                "Mua thêm 3 NPC hôm nay", MiningQuestRewardType.RandomMoney, 0f, 750f, 1500f),
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
