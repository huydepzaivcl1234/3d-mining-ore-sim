using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns runtime upgrade stacks, purchases, and mining modifiers.</summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true,"MiningSimulator.Ores","Assembly-CSharp","MiningUpgradeSystem")]
    public sealed partial class EconomyUpgradeSystem : MonoBehaviour
    {
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private EconomyUpgradeData upgradeData;
        [SerializeField] private MiningItemSystem itemSystem;

        [SerializeField, Min(0)] private int moneyRewardStacks;
        [SerializeField, Min(0)] private int luckyBlockRewardStacks;
        [SerializeField, Min(0)] private int luckyBlockDropChanceStacks;
        [SerializeField, Min(0)] private int itemDropChanceStacks;
        [SerializeField, Min(0)] private int regenIntervalReductionStacks;
        [SerializeField, Min(0)] private int healingEffectivenessStacks;

        private float permanentMoneyMultiplier = 1f;
        private float permanentExperienceMultiplier = 1f;
        private float achievementMoneyMultiplier = 1f;
        private float achievementExperienceMultiplier = 1f;

        public EconomyUpgradeData UpgradeData => upgradeData;
        public event Action UpgradesChanged;
        public event Action<EconomyUpgradeType> UpgradePurchased;
        public float PermanentMoneyMultiplier => permanentMoneyMultiplier * achievementMoneyMultiplier;
        public float PermanentExperienceMultiplier =>
            permanentExperienceMultiplier * achievementExperienceMultiplier;
        public float AchievementMoneyMultiplier => achievementMoneyMultiplier;
        public float AchievementExperienceMultiplier => achievementExperienceMultiplier;

        private void Awake()
        {
            LoadAutoUpgrade();
            if (itemSystem == null)
            {
                itemSystem = FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
            }
        }

        public int GetStacks(EconomyUpgradeType type)
        {
            return type switch
            {
                EconomyUpgradeType.MoneyReward => moneyRewardStacks,
                EconomyUpgradeType.LuckyBlockReward => luckyBlockRewardStacks,
                EconomyUpgradeType.LuckyBlockDropChance => luckyBlockDropChanceStacks,
                EconomyUpgradeType.ItemDropChance => itemDropChanceStacks,
                EconomyUpgradeType.RegenIntervalReduction => regenIntervalReductionStacks,
                EconomyUpgradeType.HealingEffectiveness => healingEffectivenessStacks,
                _ => 0
            };
        }

        public float GetCost(EconomyUpgradeType type)
        {
            EconomyUpgradeDefinition definition = upgradeData != null
                ? upgradeData.GetDefinition(type)
                : null;
            return definition != null ? definition.GetCost(GetStacks(type)) : 0;
        }

        public bool IsMaximum(EconomyUpgradeType type)
        {
            EconomyUpgradeDefinition definition = upgradeData != null
                ? upgradeData.GetDefinition(type)
                : null;
            return definition == null || GetStacks(type) >= definition.MaximumStacks;
        }

        public bool CanPurchase(EconomyUpgradeType type)
        {
            return wallet != null && upgradeData != null && !IsMaximum(type) &&
                   wallet.CurrentMoney >= GetCost(type);
        }

        public bool TryPurchase(EconomyUpgradeType type)
        {
            if (!CanPurchase(type) || !wallet.TrySpend(GetCost(type)))
            {
                return false;
            }

            switch (type)
            {
                case EconomyUpgradeType.MoneyReward:
                    moneyRewardStacks++;
                    break;
                case EconomyUpgradeType.LuckyBlockReward:
                    luckyBlockRewardStacks++;
                    break;
                case EconomyUpgradeType.LuckyBlockDropChance:
                    luckyBlockDropChanceStacks++;
                    break;
                case EconomyUpgradeType.ItemDropChance:
                    itemDropChanceStacks++;
                    break;
                case EconomyUpgradeType.RegenIntervalReduction:
                    regenIntervalReductionStacks++;
                    break;
                case EconomyUpgradeType.HealingEffectiveness:
                    healingEffectivenessStacks++;
                    break;
            }

            UpgradesChanged?.Invoke();
            UpgradePurchased?.Invoke(type);
            return true;
        }

        public float GetMultiplier(EconomyUpgradeType type)
        {
            if (upgradeData == null)
            {
                return 1f;
            }

            EconomyUpgradeDefinition definition = upgradeData.GetDefinition(type);
            if (definition == null) return 1f;
            // Compounding the reduction keeps intervals positive, even at high upgrade levels.
            if (type == EconomyUpgradeType.RegenIntervalReduction)
                return Mathf.Pow(1f - Mathf.Clamp(definition.PercentPerStack, 0f, 100f) * 0.01f,
                    GetStacks(type));
            if (definition == null) return 1f;
            float multiplier = 1f + definition.PercentPerStack * GetStacks(type) * 0.01f;
            return multiplier;
        }

        public float GetAddedPercent(EconomyUpgradeType type)
        {
            if (upgradeData == null)
            {
                return 0f;
            }

            EconomyUpgradeDefinition definition = upgradeData.GetDefinition(type);
            return definition != null
                ? definition.PercentPerStack * GetStacks(type)
                : 0f;
        }




        public float CalculateMonsterMoneyReward(float baseReward)
        {
            if (baseReward <= 0f) return 0f;
            return baseReward * GetMultiplier(EconomyUpgradeType.MoneyReward) *
                PermanentMoneyMultiplier * (itemSystem != null ? itemSystem.MoneyRewardMultiplier : 1f);
        }

        public float CalculateLuckyBlockReward(int baseReward)
        {
            if (baseReward <= 0)
            {
                return 0f;
            }

            return baseReward * GetMultiplier(EconomyUpgradeType.LuckyBlockReward) *
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

        public float CalculatePlayerExperienceReward(float baseExperience)
        {
            if (baseExperience <= 0f) return 0f;
            return baseExperience *
                PermanentExperienceMultiplier;
        }

        /// <summary>Permanent, Rebirth-granted multiplier applied to every NPC mining hit.</summary>


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
            luckyBlockRewardStacks = 0;
            luckyBlockDropChanceStacks = 0;
            itemDropChanceStacks = 0;
            regenIntervalReductionStacks = 0;
            healingEffectivenessStacks = 0;
            UpgradesChanged?.Invoke();
        }

        private void OnValidate()
        {
            if (upgradeData == null)
            {
                return;
            }

            moneyRewardStacks = Mathf.Clamp(moneyRewardStacks, 0, upgradeData.MoneyReward.MaximumStacks);
            luckyBlockRewardStacks = Mathf.Clamp(luckyBlockRewardStacks, 0,
                upgradeData.LuckyBlockReward.MaximumStacks);
            luckyBlockDropChanceStacks = Mathf.Clamp(luckyBlockDropChanceStacks, 0,
                upgradeData.LuckyBlockDropChance.MaximumStacks);
            itemDropChanceStacks = Mathf.Clamp(itemDropChanceStacks, 0,
                upgradeData.ItemDropChance.MaximumStacks);
            regenIntervalReductionStacks = Mathf.Clamp(regenIntervalReductionStacks, 0,
                upgradeData.RegenIntervalReduction.MaximumStacks);
            healingEffectivenessStacks = Mathf.Clamp(healingEffectivenessStacks, 0,
                upgradeData.HealingEffectiveness.MaximumStacks);
        }
    }
}
