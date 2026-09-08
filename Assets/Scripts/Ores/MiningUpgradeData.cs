using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum MiningUpgradeType
    {
        MoneyReward = 0,
        RareOreSpawn = 1,
        OreDamage = 2,
        OreSpawnSpeed = 3,
        NpcMoveSpeed = 4,
        NpcCapacity = 5,
        LuckyBlockReward = 6,
        LuckyBlockDropChance = 7
    }

    [Serializable]
    public sealed class MiningUpgradeDefinition
    {
        [SerializeField] private string displayName = "Nâng cấp";
        [InspectorName("Value Per Stack"), Min(0f), SerializeField]
        private float percentPerStack = 1f;
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
        public float ValuePerStack => percentPerStack;
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

    /// <summary>Designer-owned values for mining upgrades.</summary>
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

        [Header("Ore Spawn Speed")]
        [SerializeField] private MiningUpgradeDefinition oreSpawnSpeed =
            new("Tăng tốc độ spawn quặng", 1f);

        [Header("NPC Move Speed")]
        [SerializeField] private MiningUpgradeDefinition npcMoveSpeed =
            new("Tăng tốc độ di chuyển NPC", 1f);

        [Header("NPC Capacity")]
        [SerializeField] private MiningUpgradeDefinition npcCapacity =
            new("Tăng giới hạn thợ mỏ", 1f, 25, 50, 25);

        [Header("Lucky Block Reward")]
        [SerializeField] private MiningUpgradeDefinition luckyBlockReward =
            new("Tăng tiền Lucky Block", 1f);

        [Header("Lucky Block Drop Chance")]
        [SerializeField] private MiningUpgradeDefinition luckyBlockDropChance =
            new("Tăng tỉ lệ Lucky Block", 1f);

        public MiningUpgradeDefinition MoneyReward => moneyReward;
        public MiningUpgradeDefinition RareOreSpawn => rareOreSpawn;
        public MiningUpgradeDefinition OreDamage => oreDamage;
        public MiningUpgradeDefinition OreSpawnSpeed => oreSpawnSpeed;
        public MiningUpgradeDefinition NpcMoveSpeed => npcMoveSpeed;
        public MiningUpgradeDefinition NpcCapacity => npcCapacity;
        public MiningUpgradeDefinition LuckyBlockReward => luckyBlockReward ??=
            new MiningUpgradeDefinition("Tăng tiền Lucky Block", 1f);
        public MiningUpgradeDefinition LuckyBlockDropChance => luckyBlockDropChance ??=
            new MiningUpgradeDefinition("Tăng tỉ lệ Lucky Block", 1f);

        public MiningUpgradeDefinition GetDefinition(MiningUpgradeType type)
        {
            return type switch
            {
                MiningUpgradeType.MoneyReward => moneyReward,
                MiningUpgradeType.RareOreSpawn => rareOreSpawn,
                MiningUpgradeType.OreDamage => oreDamage,
                MiningUpgradeType.OreSpawnSpeed => oreSpawnSpeed,
                MiningUpgradeType.NpcMoveSpeed => npcMoveSpeed,
                MiningUpgradeType.NpcCapacity => npcCapacity,
                MiningUpgradeType.LuckyBlockReward => LuckyBlockReward,
                MiningUpgradeType.LuckyBlockDropChance => LuckyBlockDropChance,
                _ => moneyReward
            };
        }

        private void OnValidate()
        {
            moneyReward?.Validate();
            rareOreSpawn?.Validate();
            oreDamage?.Validate();
            oreSpawnSpeed?.Validate();
            npcMoveSpeed?.Validate();
            npcCapacity?.Validate();
            LuckyBlockReward.Validate();
            LuckyBlockDropChance.Validate();
        }
    }
}
