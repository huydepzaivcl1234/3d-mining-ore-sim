using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns runtime upgrade stacks, purchases, and mining modifiers.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningUpgradeSystem : MonoBehaviour
    {
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MiningUpgradeData upgradeData;
        [SerializeField] private MiningItemSystem itemSystem;

        [SerializeField, Min(0)] private int moneyRewardStacks;
        [SerializeField, Min(0)] private int rareOreSpawnStacks;
        [SerializeField, Min(0)] private int oreDamageStacks;
        [SerializeField, Min(0)] private int oreSpawnSpeedStacks;
        [SerializeField, Min(0)] private int npcMoveSpeedStacks;
        [SerializeField, Min(0)] private int npcCapacityStacks;
        [SerializeField, Min(0)] private int luckyBlockRewardStacks;
        [SerializeField, Min(0)] private int luckyBlockDropChanceStacks;
        [SerializeField, Min(0)] private int npcExperienceStacks;
        [SerializeField, Min(0)] private int itemDropChanceStacks;

        private float permanentMoneyMultiplier = 1f;
        private float permanentExperienceMultiplier = 1f;
        private float achievementMoneyMultiplier = 1f;
        private float achievementExperienceMultiplier = 1f;

        public MiningUpgradeData UpgradeData => upgradeData;
        public event Action UpgradesChanged;
        public event Action<MiningUpgradeType> UpgradePurchased;
        public float PermanentMoneyMultiplier => permanentMoneyMultiplier * achievementMoneyMultiplier;
        public float PermanentExperienceMultiplier =>
            permanentExperienceMultiplier * achievementExperienceMultiplier;
        public float AchievementMoneyMultiplier => achievementMoneyMultiplier;
        public float AchievementExperienceMultiplier => achievementExperienceMultiplier;

        private void Awake()
        {
            if (itemSystem == null)
            {
                itemSystem = FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
            }
        }

        public int GetStacks(MiningUpgradeType type)
        {
            return type switch
            {
                MiningUpgradeType.MoneyReward => moneyRewardStacks,
                MiningUpgradeType.RareOreSpawn => rareOreSpawnStacks,
                MiningUpgradeType.OreDamage => oreDamageStacks,
                MiningUpgradeType.OreSpawnSpeed => oreSpawnSpeedStacks,
                MiningUpgradeType.NpcMoveSpeed => npcMoveSpeedStacks,
                MiningUpgradeType.NpcCapacity => npcCapacityStacks,
                MiningUpgradeType.LuckyBlockReward => luckyBlockRewardStacks,
                MiningUpgradeType.LuckyBlockDropChance => luckyBlockDropChanceStacks,
                MiningUpgradeType.NpcExperience => npcExperienceStacks,
                MiningUpgradeType.ItemDropChance => itemDropChanceStacks,
                _ => 0
            };
        }

        public int GetCost(MiningUpgradeType type)
        {
            MiningUpgradeDefinition definition = upgradeData != null
                ? upgradeData.GetDefinition(type)
                : null;
            return definition != null ? definition.GetCost(GetStacks(type)) : 0;
        }

        public bool IsMaximum(MiningUpgradeType type)
        {
            MiningUpgradeDefinition definition = upgradeData != null
                ? upgradeData.GetDefinition(type)
                : null;
            return definition == null || GetStacks(type) >= definition.MaximumStacks;
        }

        public bool CanPurchase(MiningUpgradeType type)
        {
            return wallet != null && upgradeData != null && !IsMaximum(type) &&
                   wallet.CurrentMoney >= GetCost(type);
        }

        public bool TryPurchase(MiningUpgradeType type)
        {
            if (!CanPurchase(type) || !wallet.TrySpend(GetCost(type)))
            {
                return false;
            }

            switch (type)
            {
                case MiningUpgradeType.MoneyReward:
                    moneyRewardStacks++;
                    break;
                case MiningUpgradeType.RareOreSpawn:
                    rareOreSpawnStacks++;
                    break;
                case MiningUpgradeType.OreDamage:
                    oreDamageStacks++;
                    break;
                case MiningUpgradeType.OreSpawnSpeed:
                    oreSpawnSpeedStacks++;
                    break;
                case MiningUpgradeType.NpcMoveSpeed:
                    npcMoveSpeedStacks++;
                    break;
                case MiningUpgradeType.NpcCapacity:
                    npcCapacityStacks++;
                    break;
                case MiningUpgradeType.LuckyBlockReward:
                    luckyBlockRewardStacks++;
                    break;
                case MiningUpgradeType.LuckyBlockDropChance:
                    luckyBlockDropChanceStacks++;
                    break;
                case MiningUpgradeType.NpcExperience:
                    npcExperienceStacks++;
                    break;
                case MiningUpgradeType.ItemDropChance:
                    itemDropChanceStacks++;
                    break;
            }

            UpgradesChanged?.Invoke();
            UpgradePurchased?.Invoke(type);
            return true;
        }

        public float GetMultiplier(MiningUpgradeType type)
        {
            if (upgradeData == null)
            {
                return 1f;
            }

            MiningUpgradeDefinition definition = upgradeData.GetDefinition(type);
            return 1f + definition.PercentPerStack * GetStacks(type) * 0.01f;
        }

        public float GetAddedPercent(MiningUpgradeType type)
        {
            if (upgradeData == null)
            {
                return 0f;
            }

            MiningUpgradeDefinition definition = upgradeData.GetDefinition(type);
            return definition != null
                ? definition.PercentPerStack * GetStacks(type)
                : 0f;
        }

        public float CalculateMiningReward(int baseReward)
        {
            if (baseReward <= 0)
            {
                return 0f;
            }

            return baseReward * GetMultiplier(MiningUpgradeType.MoneyReward) *
                   PermanentMoneyMultiplier *
                   (itemSystem != null ? itemSystem.MoneyRewardMultiplier : 1f);
        }

        public float CalculateLuckyBlockReward(int baseReward)
        {
            if (baseReward <= 0)
            {
                return 0f;
            }

            return baseReward * GetMultiplier(MiningUpgradeType.LuckyBlockReward) *
                   PermanentMoneyMultiplier *
                   (itemSystem != null ? itemSystem.MoneyRewardMultiplier : 1f);
        }

        public void SetPermanentMoneyMultiplier(float multiplier)
        {
            permanentMoneyMultiplier = Mathf.Max(1f, multiplier);
        }

        /// <summary>Permanent, Rebirth-granted multiplier applied to NPC experience gains,
        /// on top of the purchasable NPC Experience upgrade.</summary>
        public void SetPermanentExperienceMultiplier(float multiplier)
        {
            permanentExperienceMultiplier = Mathf.Max(1f, multiplier);
        }

        /// <summary>Applies the cumulative permanent rewards earned from achievements.</summary>
        public void SetAchievementRewardMultipliers(float moneyMultiplier,
            float experienceMultiplier)
        {
            achievementMoneyMultiplier = Mathf.Max(1f, moneyMultiplier);
            achievementExperienceMultiplier = Mathf.Max(1f, experienceMultiplier);
            UpgradesChanged?.Invoke();
        }

        public void ResetAllUpgrades()
        {
            moneyRewardStacks = 0;
            rareOreSpawnStacks = 0;
            oreDamageStacks = 0;
            oreSpawnSpeedStacks = 0;
            npcMoveSpeedStacks = 0;
            npcCapacityStacks = 0;
            luckyBlockRewardStacks = 0;
            luckyBlockDropChanceStacks = 0;
            npcExperienceStacks = 0;
            itemDropChanceStacks = 0;
            UpgradesChanged?.Invoke();
        }

        private void OnValidate()
        {
            if (upgradeData == null)
            {
                return;
            }

            moneyRewardStacks = Mathf.Clamp(moneyRewardStacks, 0, upgradeData.MoneyReward.MaximumStacks);
            rareOreSpawnStacks = Mathf.Clamp(rareOreSpawnStacks, 0, upgradeData.RareOreSpawn.MaximumStacks);
            oreDamageStacks = Mathf.Clamp(oreDamageStacks, 0, upgradeData.OreDamage.MaximumStacks);
            oreSpawnSpeedStacks = Mathf.Clamp(oreSpawnSpeedStacks, 0,
                upgradeData.OreSpawnSpeed.MaximumStacks);
            npcMoveSpeedStacks = Mathf.Clamp(npcMoveSpeedStacks, 0,
                upgradeData.NpcMoveSpeed.MaximumStacks);
            npcCapacityStacks = Mathf.Clamp(npcCapacityStacks, 0,
                upgradeData.NpcCapacity.MaximumStacks);
            luckyBlockRewardStacks = Mathf.Clamp(luckyBlockRewardStacks, 0,
                upgradeData.LuckyBlockReward.MaximumStacks);
            luckyBlockDropChanceStacks = Mathf.Clamp(luckyBlockDropChanceStacks, 0,
                upgradeData.LuckyBlockDropChance.MaximumStacks);
            npcExperienceStacks = Mathf.Clamp(npcExperienceStacks, 0,
                upgradeData.NpcExperience.MaximumStacks);
            itemDropChanceStacks = Mathf.Clamp(itemDropChanceStacks, 0,
                upgradeData.ItemDropChance.MaximumStacks);
        }
    }
}
