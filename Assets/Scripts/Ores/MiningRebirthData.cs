using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Designer-owned progression values for permanent rebirth bonuses.</summary>
    [CreateAssetMenu(fileName = "MiningRebirthData", menuName = "Mining Simulator/Game Data/Rebirth")]
    public sealed class MiningRebirthData : ScriptableObject
    {
        [Min(1), SerializeField] private int startingMoneyRequirement = 1000;
        [Min(1f), SerializeField] private float requirementGrowthMultiplier = 2f;
        [Min(0f), SerializeField] private float moneyBoostPercentPerRebirth = 10f;
        [SerializeField] private string rebirthCountSaveKey = "MiningSimulator.RebirthCount.v1";

        public int StartingMoneyRequirement => startingMoneyRequirement;
        public float RequirementGrowthMultiplier => requirementGrowthMultiplier;
        public float MoneyBoostPercentPerRebirth => moneyBoostPercentPerRebirth;
        public string RebirthCountSaveKey => rebirthCountSaveKey;

        public int GetRequirement(int completedRebirths)
        {
            double requirement = startingMoneyRequirement *
                                 System.Math.Pow(requirementGrowthMultiplier,
                                     Mathf.Max(0, completedRebirths));
            return requirement >= int.MaxValue ? int.MaxValue :
                Mathf.Max(1, (int)System.Math.Ceiling(requirement));
        }

        public float GetMoneyMultiplier(int completedRebirths)
        {
            return 1f + Mathf.Max(0, completedRebirths) * moneyBoostPercentPerRebirth * 0.01f;
        }

        private void OnValidate()
        {
            startingMoneyRequirement = Mathf.Max(1, startingMoneyRequirement);
            requirementGrowthMultiplier = Mathf.Max(1f, requirementGrowthMultiplier);
            moneyBoostPercentPerRebirth = Mathf.Max(0f, moneyBoostPercentPerRebirth);
            if (string.IsNullOrWhiteSpace(rebirthCountSaveKey))
            {
                rebirthCountSaveKey = "MiningSimulator.RebirthCount.v1";
            }
        }
    }
}
