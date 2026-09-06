using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum MiningUpgradeType
    {
        MoneyReward = 0,
        RareOreSpawn = 1,
        OreDamage = 2
    }

    [Serializable]
    public sealed class MiningUpgradeDefinition
    {
        [SerializeField] private string displayName = "Nâng cấp";
        [Min(0f), SerializeField] private float percentPerStack = 1f;
        [Min(1), SerializeField] private int maximumStacks = 100;
        [Min(0), SerializeField] private int startingCost = 25;
        [Min(0), SerializeField] private int costIncreasePerPurchase = 10;

        public MiningUpgradeDefinition(string name = "Nâng cấp", float percent = 1f,
            int maxStacks = 100, int baseCost = 25, int addedCost = 10)
        {
            displayName = name;
            percentPerStack = percent;
            maximumStacks = maxStacks;
            startingCost = baseCost;
            costIncreasePerPurchase = addedCost;
        }

        public string DisplayName => displayName;
        public float PercentPerStack => percentPerStack;
        public int MaximumStacks => maximumStacks;
        public int StartingCost => startingCost;
        public int CostIncreasePerPurchase => costIncreasePerPurchase;

        public int GetCost(int currentStacks)
        {
            long cost = startingCost + (long)costIncreasePerPurchase * Mathf.Max(0, currentStacks);
            return (int)Math.Min(int.MaxValue, cost);
        }

        public void Validate()
        {
            percentPerStack = Mathf.Max(0f, percentPerStack);
            maximumStacks = Mathf.Max(1, maximumStacks);
            startingCost = Mathf.Max(0, startingCost);
            costIncreasePerPurchase = Mathf.Max(0, costIncreasePerPurchase);
        }
    }

    /// <summary>Designer-owned values for the three mining upgrades.</summary>
    [CreateAssetMenu(fileName = "MiningUpgradeData", menuName = "Mining Simulator/Game Data/Upgrades")]
    public sealed class MiningUpgradeData : ScriptableObject
    {
        [Header("Money Reward")]
        [SerializeField] private MiningUpgradeDefinition moneyReward =
            new("Tăng tiền nhận được", 1f);

        [Header("Rare Ore Spawn")]
        [SerializeField] private MiningUpgradeDefinition rareOreSpawn =
            new("Tăng tỉ lệ quặng hiếm", 0.5f);

        [Header("Ore Damage")]
        [SerializeField] private MiningUpgradeDefinition oreDamage =
            new("Tăng sát thương lên quặng", 1f);

        public MiningUpgradeDefinition MoneyReward => moneyReward;
        public MiningUpgradeDefinition RareOreSpawn => rareOreSpawn;
        public MiningUpgradeDefinition OreDamage => oreDamage;

        public MiningUpgradeDefinition GetDefinition(MiningUpgradeType type)
        {
            return type switch
            {
                MiningUpgradeType.MoneyReward => moneyReward,
                MiningUpgradeType.RareOreSpawn => rareOreSpawn,
                MiningUpgradeType.OreDamage => oreDamage,
                _ => moneyReward
            };
        }

        private void OnValidate()
        {
            moneyReward?.Validate();
            rareOreSpawn?.Validate();
            oreDamage?.Validate();
        }
    }
}
