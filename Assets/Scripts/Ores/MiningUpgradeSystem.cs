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

        [SerializeField, Min(0)] private int moneyRewardStacks;
        [SerializeField, Min(0)] private int rareOreSpawnStacks;
        [SerializeField, Min(0)] private int oreDamageStacks;
        [SerializeField, Min(0)] private int oreSpawnSpeedStacks;
        [SerializeField, Min(0)] private int npcMoveSpeedStacks;
        [SerializeField, Min(0)] private int npcCapacityStacks;

        private float rewardRemainder;
        private float permanentMoneyMultiplier = 1f;

        public MiningUpgradeData UpgradeData => upgradeData;
        public event Action UpgradesChanged;
        public event Action<MiningUpgradeType> UpgradePurchased;
        public float PermanentMoneyMultiplier => permanentMoneyMultiplier;

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

        public int CalculateMiningReward(int baseReward)
        {
            if (baseReward <= 0)
            {
                return 0;
            }

            float upgradedReward = baseReward * GetMultiplier(MiningUpgradeType.MoneyReward) *
                                   permanentMoneyMultiplier + rewardRemainder;
            int wholeReward = Mathf.FloorToInt(upgradedReward);
            rewardRemainder = upgradedReward - wholeReward;
            return wholeReward;
        }

        public void SetPermanentMoneyMultiplier(float multiplier)
        {
            permanentMoneyMultiplier = Mathf.Max(1f, multiplier);
        }

        public void ResetAllUpgrades()
        {
            moneyRewardStacks = 0;
            rareOreSpawnStacks = 0;
            oreDamageStacks = 0;
            oreSpawnSpeedStacks = 0;
            npcMoveSpeedStacks = 0;
            npcCapacityStacks = 0;
            rewardRemainder = 0f;
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
        }
    }
}
