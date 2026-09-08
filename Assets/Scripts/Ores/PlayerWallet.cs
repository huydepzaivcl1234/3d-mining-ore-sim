using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns the player's runtime money balance.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerWallet : MonoBehaviour
    {
        [SerializeField] private MiningGameData gameData;
        [SerializeField] private int currentMoney;

        public int CurrentMoney => currentMoney;
        public event Action<int> MoneyChanged;

        private void Awake()
        {
            currentMoney = gameData != null ? gameData.StartingMoney : 0;
            MoneyChanged?.Invoke(currentMoney);
        }

        public void AddMoney(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            long updatedMoney = (long)currentMoney + amount;
            currentMoney = (int)Math.Min(int.MaxValue, updatedMoney);
            MoneyChanged?.Invoke(currentMoney);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || amount > currentMoney)
            {
                return false;
            }

            currentMoney -= amount;
            MoneyChanged?.Invoke(currentMoney);
            return true;
        }

        public void ResetMoney()
        {
            if (currentMoney == 0)
            {
                return;
            }

            currentMoney = 0;
            MoneyChanged?.Invoke(currentMoney);
        }
    }
}
