using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns the player's runtime money balance.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerWallet : MonoBehaviour
    {
        [Header("Money")]
        [Tooltip("The single authoritative balance. You can change it in the Inspector during Play Mode.")]
        [Min(0), SerializeField] private int currentMoney = 100;

        public int CurrentMoney => currentMoney;
        public event Action<int> MoneyChanged;

        private void Awake()
        {
            currentMoney = Mathf.Max(0, currentMoney);
            MoneyChanged?.Invoke(currentMoney);
        }

        private void OnValidate()
        {
            currentMoney = Mathf.Max(0, currentMoney);
            if (Application.isPlaying)
            {
                MoneyChanged?.Invoke(currentMoney);
            }
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
