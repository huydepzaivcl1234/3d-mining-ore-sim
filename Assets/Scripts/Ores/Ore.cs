using System;
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
        [SerializeField] private PlayerWallet wallet;
        [SerializeField, Min(0)] private int currentDurability;

        private bool rewardGranted;

        public OreData Data => data;
        public int CurrentDurability => currentDurability;
        public int MaxDurability => data != null ? data.Durability : 0;
        public bool IsDepleted => currentDurability <= 0;
        public event Action<Ore> Depleted;
        public event Action<int, int> DurabilityChanged;

        private void Awake()
        {
            ResetDurability();
        }

        public void SetData(OreData oreData)
        {
            data = oreData;
            ResetDurability();
        }

        public void Initialize(OreData oreData, PlayerWallet playerWallet)
        {
            data = oreData;
            wallet = playerWallet;
            ResetDurability();
        }

        public bool MineOnce()
        {
            return data != null && ApplyDamage(data.ClickDamage);
        }

        public bool ApplyDamage(int damage)
        {
            if (data == null || IsDepleted || damage <= 0)
            {
                return false;
            }

            currentDurability = Mathf.Max(0, currentDurability - damage);
            DurabilityChanged?.Invoke(currentDurability, MaxDurability);
            if (currentDurability == 0)
            {
                Deplete();
            }

            return true;
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
            rewardGranted = false;
            DurabilityChanged?.Invoke(currentDurability, MaxDurability);
        }

        private void Deplete()
        {
            if (rewardGranted)
            {
                return;
            }

            rewardGranted = true;
            wallet?.AddMoney(data.BaseSellValue);
            Depleted?.Invoke(this);
            Destroy(gameObject, data.DestroyDelay);
        }
    }
}
