using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns the player's runtime money balance.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerWallet : MonoBehaviour
    {
        [Header("Starting Balance")]
        [Tooltip("Money applied every time Play Mode starts.")]
        [Min(0), SerializeField] private int startingMoney = 100;

        [Header("Runtime Balance")]
        [Tooltip("Current Play Mode balance. Change Starting Money to set the next game's initial balance.")]
        [Min(0), SerializeField] private int currentMoney;

        public int StartingMoney => startingMoney;
        public int CurrentMoney => currentMoney;
        public event Action<int> MoneyChanged;

        private void Awake()
        {
            currentMoney = startingMoney;
            MoneyChanged?.Invoke(currentMoney);
        }

        private void OnValidate()
        {
            startingMoney = Mathf.Max(0, startingMoney);
            currentMoney = Mathf.Max(0, currentMoney);
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

        public void SetMoney(int amount)
        {
            int safeAmount = Mathf.Max(0, amount);
            if (currentMoney == safeAmount)
            {
                return;
            }

            currentMoney = safeAmount;
            MoneyChanged?.Invoke(currentMoney);
        }

        public void ResetMoney()
        {
            if (currentMoney == 0)
            {
                return;
            }

            SetMoney(0);
        }
    }
}
