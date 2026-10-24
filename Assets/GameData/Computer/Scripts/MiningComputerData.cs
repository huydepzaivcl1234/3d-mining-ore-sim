using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Designer-owned economy and animation settings for the Computer 1 coin machine.</summary>
    [CreateAssetMenu(fileName = "MiningComputerData",
        menuName = "Mining Simulator/Game Data/Computer Coin Machine")]
    public sealed class MiningComputerData : ScriptableObject
    {
        [Header("Purchase And Production")]
        [Min(0), SerializeField] private int purchaseCost = 500;
        [Min(1), SerializeField] private int baseCoinsPerTick = 10;
        [Min(0.05f), SerializeField] private float secondsPerTick = 2f;
        [SerializeField] private string purchaseSaveKey =
            "MiningSimulator.ComputerCoinMachine.Purchased.v1";

        [Header("Machine Upgrade")]
        [Min(1), SerializeField] private int maximumLevel = 10;
        [Min(0), SerializeField] private int bonusCoinsPerLevel = 50;
        [Min(0f), SerializeField] private float upgradeBaseCost = 500f;
        [Min(1.01f), SerializeField] private float upgradeCostGrowthMultiplier = 1.6f;

        [Header("Ghost Preview")]
        [SerializeField] private Color ghostColor = new(0.1f, 0.85f, 1f, 0.28f);
        [SerializeField] private Color focusedGhostColor = new(0.35f, 1f, 0.75f, 0.48f);
        [Min(0f), SerializeField] private float ghostEmissionIntensity = 1.25f;

        [Header("Purchase Assembly")]
        [Min(0f), SerializeField] private float partDropHeight = 2.5f;
        [Min(0.01f), SerializeField] private float partDropDuration = 0.55f;
        [Min(0f), SerializeField] private float delayBetweenParts = 0.09f;

        [Header("Coin Tick Punch")]
        [Range(0f, 0.4f), SerializeField] private float punchScaleAmount = 0.08f;
        [Min(0f), SerializeField] private float punchDropAmount = 0.08f;
        [Min(0.02f), SerializeField] private float punchDuration = 0.18f;

        public int PurchaseCost => Mathf.Max(0, purchaseCost);
        public int BaseCoinsPerTick => Mathf.Max(1, baseCoinsPerTick);
        public float SecondsPerTick => Mathf.Max(0.05f, secondsPerTick);
        public string PurchaseSaveKey => string.IsNullOrWhiteSpace(purchaseSaveKey)
            ? "MiningSimulator.ComputerCoinMachine.Purchased.v1"
            : purchaseSaveKey;
        public int MaximumLevel => Mathf.Max(1, maximumLevel);
        public int BonusCoinsPerLevel => Mathf.Max(0, bonusCoinsPerLevel);
        public float UpgradeBaseCost => Mathf.Max(0f, upgradeBaseCost);
        public float UpgradeCostGrowthMultiplier => Mathf.Max(1.01f,
            upgradeCostGrowthMultiplier);
        public Color GhostColor => ghostColor;
        public Color FocusedGhostColor => focusedGhostColor;
        public float GhostEmissionIntensity => Mathf.Max(0f, ghostEmissionIntensity);
        public float PartDropHeight => Mathf.Max(0f, partDropHeight);
        public float PartDropDuration => Mathf.Max(0.01f, partDropDuration);
        public float DelayBetweenParts => Mathf.Max(0f, delayBetweenParts);
        public float PunchScaleAmount => Mathf.Clamp(punchScaleAmount, 0f, 0.4f);
        public float PunchDropAmount => Mathf.Max(0f, punchDropAmount);
        public float PunchDuration => Mathf.Max(0.02f, punchDuration);

        public int GetBaseCoinsPerTick(int level)
        {
            int safeLevel = Mathf.Clamp(level, 1, MaximumLevel);
            long coins = (long)BaseCoinsPerTick + (long)BonusCoinsPerLevel * (safeLevel - 1);
            return coins >= int.MaxValue ? int.MaxValue : (int)coins;
        }

        public float GetUpgradeCost(int currentLevel)
        {
            int safeLevel = Mathf.Clamp(currentLevel, 1, MaximumLevel);
            if (safeLevel >= MaximumLevel)
            {
                return float.MaxValue;
            }

            float cost = UpgradeBaseCost * Mathf.Pow(UpgradeCostGrowthMultiplier,
                safeLevel - 1);
            return float.IsNaN(cost) || float.IsInfinity(cost)
                ? float.MaxValue
                : Mathf.Ceil(cost);
        }

        private void OnValidate()
        {
            purchaseCost = Mathf.Max(0, purchaseCost);
            baseCoinsPerTick = Mathf.Max(1, baseCoinsPerTick);
            secondsPerTick = Mathf.Max(0.05f, secondsPerTick);
            maximumLevel = Mathf.Max(1, maximumLevel);
            bonusCoinsPerLevel = Mathf.Max(0, bonusCoinsPerLevel);
            upgradeBaseCost = Mathf.Max(0f, upgradeBaseCost);
            upgradeCostGrowthMultiplier = Mathf.Max(1.01f, upgradeCostGrowthMultiplier);
            ghostEmissionIntensity = Mathf.Max(0f, ghostEmissionIntensity);
            partDropHeight = Mathf.Max(0f, partDropHeight);
            partDropDuration = Mathf.Max(0.01f, partDropDuration);
            delayBetweenParts = Mathf.Max(0f, delayBetweenParts);
            punchScaleAmount = Mathf.Clamp(punchScaleAmount, 0f, 0.4f);
            punchDropAmount = Mathf.Max(0f, punchDropAmount);
            punchDuration = Mathf.Max(0.02f, punchDuration);
            if (string.IsNullOrWhiteSpace(purchaseSaveKey))
            {
                purchaseSaveKey = "MiningSimulator.ComputerCoinMachine.Purchased.v1";
            }
        }
    }
}
