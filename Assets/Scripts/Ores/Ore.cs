using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Runtime identity and durability for one spawned ore prefab.
    /// Mining rewards are returned only when this instance is fully depleted.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Ore : MonoBehaviour
    {
        [SerializeField] private OreData data;
        [SerializeField, Min(0)] private int currentDurability;

        public OreData Data => data;
        public int CurrentDurability => currentDurability;
        public bool IsDepleted => currentDurability <= 0;

        private void Awake()
        {
            ResetDurability();
        }

        public void SetData(OreData oreData)
        {
            data = oreData;
            ResetDurability();
        }

        public bool TryMine(int miningPower, out int moneyEarned)
        {
            moneyEarned = 0;
            if (data == null || IsDepleted || miningPower < data.MiningPowerRequired)
            {
                return false;
            }

            currentDurability = Mathf.Max(0, currentDurability - Mathf.Max(1, miningPower));
            if (currentDurability == 0)
            {
                moneyEarned = data.BaseSellValue;
            }

            return true;
        }

        public void ResetDurability()
        {
            currentDurability = data != null ? data.Durability : 0;
        }
    }
}
