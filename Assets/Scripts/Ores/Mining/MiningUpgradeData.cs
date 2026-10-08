using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum MiningUpgradeType
    {
        MoneyReward = 0,
        // Reserved IDs keep old serialized data readable. These entries cannot be purchased.
        [Obsolete("Removed mining upgrade")] RetiredRareOreSpawn = 1,
        [Obsolete("Removed mining upgrade")] RetiredOreDamage = 2,
        [Obsolete("Removed mining upgrade")] RetiredOreSpawnSpeed = 3,
        [Obsolete("Removed miner upgrade")] RetiredNpcMoveSpeed = 4,
        [Obsolete("Removed miner upgrade")] RetiredNpcCapacity = 5,
        [Obsolete("Removed miner upgrade")] RetiredNpcExperience = 8,
        LuckyBlockReward = 6,
        LuckyBlockDropChance = 7,
        ItemDropChance = 9,
        RegenIntervalReduction = 10,
        HealingEffectiveness = 11
    }

    [Serializable]
    public sealed class MiningUpgradeDefinition
    {
        [SerializeField] private string displayName = "Nâng cấp";
        [InspectorName("Value Per Stack"), Min(0f), SerializeField]
        private float percentPerStack = 1f;
        [Min(1), SerializeField] private int maximumStacks = 100;
        [Min(0), SerializeField] private int startingCost = 25;
        [Tooltip("Exponential price growth. Cost = Starting Cost × Growth^Current Stacks.")]
        [Min(1.01f), SerializeField] private float costGrowthMultiplier = 1.20f;
        // Kept serialized so existing assets do not lose their old authoring value when opened.
        // The exponential formula intentionally no longer uses this linear increment.
        [HideInInspector, SerializeField] private int costIncreasePerPurchase = 10;

        public MiningUpgradeDefinition(string name = "Nâng cấp", float percent = 1f,
            int maxStacks = 100, int baseCost = 25, float growthMultiplier = 1.20f)
        {
            displayName = name;
            percentPerStack = percent;
            maximumStacks = maxStacks;
            startingCost = baseCost;
            costGrowthMultiplier = growthMultiplier;
        }

        public string DisplayName => displayName;
        public float PercentPerStack => percentPerStack;
        public float ValuePerStack => percentPerStack;
        public int MaximumStacks => maximumStacks;
        public int StartingCost => startingCost;
        public float CostGrowthMultiplier => costGrowthMultiplier;
        [Obsolete("Linear upgrade pricing was replaced by CostGrowthMultiplier.")]
        public int CostIncreasePerPurchase => costIncreasePerPurchase;

        public float GetCost(int currentStacks)
        {
            int safeStacks = Mathf.Max(0, currentStacks);
            double growth = float.IsNaN(costGrowthMultiplier) ||
                            float.IsInfinity(costGrowthMultiplier) ||
                            costGrowthMultiplier < 1.01f
                ? 1.20d
                : costGrowthMultiplier;
            double cost = startingCost * Math.Pow(growth, safeStacks);
            if (double.IsNaN(cost) || cost <= 0d)
            {
                return 0f;
            }

            return cost >= float.MaxValue
                ? float.MaxValue
                : (float)Math.Ceiling(cost);
        }

        public void Validate()
        {
            percentPerStack = Mathf.Max(0f, percentPerStack);
            maximumStacks = Mathf.Max(1, maximumStacks);
            startingCost = Mathf.Max(0, startingCost);
            costIncreasePerPurchase = Mathf.Max(0, costIncreasePerPurchase);
            if (float.IsNaN(costGrowthMultiplier) || float.IsInfinity(costGrowthMultiplier) ||
                costGrowthMultiplier < 1.01f)
            {
                costGrowthMultiplier = 1.20f;
            }
        }
    }

    /// <summary>Designer-owned values for mining upgrades.</summary>
    [CreateAssetMenu(fileName = "MiningUpgradeData", menuName = "Mining Simulator/Game Data/Upgrades")]
    public sealed class MiningUpgradeData : ScriptableObject
    {
        [Header("Auto upgrade - Gem unlock, Money purchases")]
        [Min(0), SerializeField] private float autoUpgradeGemCost = 25f;
        [Min(0.05f), SerializeField] private float autoUpgradeInterval = 0.5f;
        [SerializeField] private string autoUpgradeSaveKey = "MiningSimulator.AutoUpgrade.v1";
        [SerializeField] private MiningUpgradeType[] autoUpgradeOrder = {
            MiningUpgradeType.MoneyReward, 
            
            MiningUpgradeType.LuckyBlockReward, MiningUpgradeType.LuckyBlockDropChance,
            MiningUpgradeType.ItemDropChance };
        public float AutoUpgradeGemCost => Mathf.Max(0f, autoUpgradeGemCost);
        public float AutoUpgradeInterval => Mathf.Max(0.05f, autoUpgradeInterval);
        public string AutoUpgradeSaveKey => autoUpgradeSaveKey;
        public System.Collections.Generic.IReadOnlyList<MiningUpgradeType> AutoUpgradeOrder => autoUpgradeOrder;
        [Header("Money Reward")]
        [SerializeField] private MiningUpgradeDefinition moneyReward =
            new("Tăng tiền nhận được", 1f);






        [Header("Lucky Block Reward")]
        [SerializeField] private MiningUpgradeDefinition luckyBlockReward =
            new("Tăng tiền Lucky Block", 1f);

        [Header("Lucky Block Drop Chance")]
        [SerializeField] private MiningUpgradeDefinition luckyBlockDropChance =
            new("Tăng tỉ lệ Lucky Block", 1f);


        [Header("Item Drop Chance")]
        [SerializeField] private MiningUpgradeDefinition itemDropChance =
            new("Tăng tỉ lệ rơi vật phẩm", 1f);

        public MiningUpgradeDefinition MoneyReward => moneyReward;
        [Header("Player regeneration")]
        [Tooltip("Percent removed from the current regeneration interval per level (multiplicative).")]
        [SerializeField] private MiningUpgradeDefinition regenIntervalReduction =
            new("Giảm thời gian hồi máu", 1f);
        [SerializeField] private MiningUpgradeDefinition healingEffectiveness =
            new("Tăng hiệu quả hồi máu", 1f);
        public MiningUpgradeDefinition RegenIntervalReduction => regenIntervalReduction ??=
            new MiningUpgradeDefinition("Giảm thời gian hồi máu", 1f);
        public MiningUpgradeDefinition HealingEffectiveness => healingEffectiveness ??=
            new MiningUpgradeDefinition("Tăng hiệu quả hồi máu", 1f);
        public MiningUpgradeDefinition LuckyBlockReward => luckyBlockReward ??=
            new MiningUpgradeDefinition("Tăng tiền Lucky Block", 1f);
        public MiningUpgradeDefinition LuckyBlockDropChance => luckyBlockDropChance ??=
            new MiningUpgradeDefinition("Tăng tỉ lệ Lucky Block", 1f);
        public MiningUpgradeDefinition ItemDropChance => itemDropChance ??=
            new MiningUpgradeDefinition("Tăng tỉ lệ rơi vật phẩm", 1f);

        public MiningUpgradeDefinition GetDefinition(MiningUpgradeType type)
        {
            return type switch
            {
                MiningUpgradeType.MoneyReward => moneyReward,
                MiningUpgradeType.LuckyBlockReward => LuckyBlockReward,
                MiningUpgradeType.LuckyBlockDropChance => LuckyBlockDropChance,
                MiningUpgradeType.ItemDropChance => ItemDropChance,
                MiningUpgradeType.RegenIntervalReduction => RegenIntervalReduction,
                MiningUpgradeType.HealingEffectiveness => HealingEffectiveness,
                _ => null
            };
        }

        private void OnValidate()
        {
            moneyReward?.Validate();
            LuckyBlockReward.Validate();
            LuckyBlockDropChance.Validate();
            ItemDropChance.Validate();
            RegenIntervalReduction.Validate();
            HealingEffectiveness.Validate();
        }
    }
}
