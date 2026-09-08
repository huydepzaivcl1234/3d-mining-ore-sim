using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class MiningDrillLevel
    {
        [Min(0), SerializeField] private int upgradeCost = 250;
        [Min(0), SerializeField] private int moneyPerCycle = 10;
        [Min(0.05f), SerializeField] private float secondsPerCycle = 2f;
        [SerializeField] private float drillRotationDegreesPerSecond = 180f;

        public MiningDrillLevel(int cost, int money, float seconds, float rotationSpeed)
        {
            upgradeCost = cost;
            moneyPerCycle = money;
            secondsPerCycle = seconds;
            drillRotationDegreesPerSecond = rotationSpeed;
        }

        public int UpgradeCost => upgradeCost;
        public int MoneyPerCycle => moneyPerCycle;
        public float SecondsPerCycle => secondsPerCycle;
        public float DrillRotationPerSecond => drillRotationDegreesPerSecond;

        public void Validate()
        {
            upgradeCost = Mathf.Max(0, upgradeCost);
            moneyPerCycle = Mathf.Max(0, moneyPerCycle);
            secondsPerCycle = Mathf.Max(0.05f, secondsPerCycle);
        }
    }

    /// <summary>Designer-owned purchase, production, visuals, and five drill levels.</summary>
    [CreateAssetMenu(fileName = "MiningDrillData", menuName = "Mining Simulator/Game Data/Drill")]
    public sealed class MiningDrillData : ScriptableObject
    {
        public const int LevelCount = 5;

        [Header("Purchase")]
        [Min(0), SerializeField] private int purchaseCost = 500;
        [SerializeField] private bool resetOnRebirth = true;
        [SerializeField] private bool applyMoneyRewardBoosts = true;

        [Header("Exactly Five Levels")]
        [SerializeField] private MiningDrillLevel[] levels =
        {
            new(250, 10, 2f, 180f),
            new(750, 25, 1.8f, 220f),
            new(1500, 60, 1.6f, 260f),
            new(3500, 140, 1.35f, 310f),
            new(0, 350, 1f, 380f)
        };

        [Header("Affordable Glow")]
        [SerializeField] private Color lockedColor = new(0.35f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color affordableColor = new(0.2f, 1f, 0.25f, 1f);
        [SerializeField] private Color activeColor = Color.white;
        [Min(0f), SerializeField] private float affordableLightIntensity = 4f;
        [Min(0f), SerializeField] private float activeLightIntensity = 0.8f;
        [Min(0f), SerializeField] private float glowPulseSpeed = 3f;
        [Min(0f), SerializeField] private float glowPulseAmount = 0.3f;

        public int PurchaseCost => purchaseCost;
        public bool ResetOnRebirth => resetOnRebirth;
        public bool ApplyMoneyRewardBoosts => applyMoneyRewardBoosts;
        public int MaximumLevel => LevelCount;
        public Color LockedColor => lockedColor;
        public Color AffordableColor => affordableColor;
        public Color ActiveColor => activeColor;
        public float AffordableLightIntensity => affordableLightIntensity;
        public float ActiveLightIntensity => activeLightIntensity;
        public float GlowPulseSpeed => glowPulseSpeed;
        public float GlowPulseAmount => glowPulseAmount;

        public MiningDrillLevel GetLevel(int oneBasedLevel)
        {
            if (levels == null || levels.Length == 0)
            {
                return null;
            }

            int index = Mathf.Clamp(oneBasedLevel - 1, 0, levels.Length - 1);
            return levels[index];
        }

        private void OnValidate()
        {
            purchaseCost = Mathf.Max(0, purchaseCost);
            affordableLightIntensity = Mathf.Max(0f, affordableLightIntensity);
            activeLightIntensity = Mathf.Max(0f, activeLightIntensity);
            glowPulseSpeed = Mathf.Max(0f, glowPulseSpeed);
            glowPulseAmount = Mathf.Max(0f, glowPulseAmount);

            if (levels == null)
            {
                levels = new MiningDrillLevel[LevelCount];
            }
            else if (levels.Length != LevelCount)
            {
                Array.Resize(ref levels, LevelCount);
            }

            for (int index = 0; index < levels.Length; index++)
            {
                levels[index] ??= new MiningDrillLevel(0, 0, 1f, 180f);
                levels[index].Validate();
            }
        }
    }
}
