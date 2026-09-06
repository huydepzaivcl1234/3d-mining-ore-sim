using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns the player's runtime money balance.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerWallet : MonoBehaviour
    {
        [Min(0), SerializeField] private int startingMoney = 100;
        [SerializeField] private int currentMoney;

        public int CurrentMoney => currentMoney;
        public event Action<int> MoneyChanged;

        private void Awake()
        {
            currentMoney = Mathf.Max(0, startingMoney);
            MoneyChanged?.Invoke(currentMoney);
        }

        public void AddMoney(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currentMoney += amount;
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
    }
}
