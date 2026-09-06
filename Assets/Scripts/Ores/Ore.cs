using System;
using System.Collections.Generic;
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
        private readonly Dictionary<MiningNpc, int> reservedMiners = new();

        public OreData Data => data;
        public int CurrentDurability => currentDurability;
        public int MaxDurability => data != null ? data.Durability : 0;
        public bool IsDepleted => currentDurability <= 0;
        public event Action<Ore> Depleted;
        public event Action<int, int> DurabilityChanged;

        public bool CanAcceptMiner(MiningNpc miner, int miningPower, int maximumMiners)
        {
            RemoveMissingReservations();
            if (miner == null || data == null || IsDepleted ||
                miningPower < data.MiningPowerRequired || maximumMiners <= 0)
            {
                return false;
            }

            return reservedMiners.ContainsKey(miner) || reservedMiners.Count < maximumMiners;
        }

        public bool TryReserveMiner(MiningNpc miner, int miningPower, int maximumMiners,
            out int slotIndex)
        {
            slotIndex = -1;
            if (!CanAcceptMiner(miner, miningPower, maximumMiners))
            {
                return false;
            }

            if (reservedMiners.TryGetValue(miner, out slotIndex))
            {
                return true;
            }

            bool[] usedSlots = new bool[maximumMiners];
            foreach (int usedSlot in reservedMiners.Values)
            {
                if (usedSlot >= 0 && usedSlot < usedSlots.Length)
                {
                    usedSlots[usedSlot] = true;
                }
            }

            for (int index = 0; index < usedSlots.Length; index++)
            {
                if (usedSlots[index])
                {
                    continue;
                }

                reservedMiners.Add(miner, index);
                slotIndex = index;
                return true;
            }

            return false;
        }

        public void ReleaseMiner(MiningNpc miner)
        {
            if (miner != null)
            {
                reservedMiners.Remove(miner);
            }
        }

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
            reservedMiners.Clear();
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
            reservedMiners.Clear();
            wallet?.AddMoney(data.BaseSellValue);
            Depleted?.Invoke(this);
            Destroy(gameObject, data.DestroyDelay);
        }

        private void OnDisable()
        {
            reservedMiners.Clear();
        }

        private void RemoveMissingReservations()
        {
            List<MiningNpc> missing = null;
            foreach (MiningNpc miner in reservedMiners.Keys)
            {
                if (miner != null)
                {
                    continue;
                }

                missing ??= new List<MiningNpc>();
                missing.Add(miner);
            }

            if (missing == null)
            {
                return;
            }

            foreach (MiningNpc miner in missing)
            {
                reservedMiners.Remove(miner);
            }
        }
    }
}
