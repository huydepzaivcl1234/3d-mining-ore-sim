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
        [Min(0f), SerializeField] private float currentMoney = 100f;

        public float CurrentMoney => currentMoney;
        public event Action<float> MoneyChanged;
        public event Action<float> MoneySpent;

        private void Awake()
        {
            currentMoney = Mathf.Max(0f, currentMoney);
            MoneyChanged?.Invoke(currentMoney);
        }

        private void OnValidate()
        {
            currentMoney = Mathf.Max(0f, currentMoney);
            if (Application.isPlaying)
            {
                MoneyChanged?.Invoke(currentMoney);
            }
        }

        public void AddMoney(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            currentMoney = Mathf.Min(float.MaxValue, currentMoney + amount);
            MoneyChanged?.Invoke(currentMoney);
        }

        public bool TrySpend(float amount)
        {
            if (amount < 0f || amount > currentMoney)
            {
                return false;
            }

            currentMoney -= amount;
            MoneyChanged?.Invoke(currentMoney);
            if (amount > 0f)
            {
                MoneySpent?.Invoke(amount);
            }
            return true;
        }

        public void SetMoney(float amount)
        {
            float safeAmount = Mathf.Max(0f, amount);
            if (Mathf.Approximately(currentMoney, safeAmount))
            {
                return;
            }

            currentMoney = safeAmount;
            MoneyChanged?.Invoke(currentMoney);
        }

        public void ResetMoney()
        {
            if (currentMoney == 0f)
            {
                return;
            }

            SetMoney(0f);
        }
    }
}
