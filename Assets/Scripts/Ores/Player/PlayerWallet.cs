using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns the player's runtime money balance.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerWallet : MonoBehaviour
    {
        private const string DefaultGemSaveKey = "MiningSimulator.Gems.v1";

        [Header("Game Data")]
        [SerializeField] private MiningGameData gameData;

        [Header("Money")]
        [Tooltip("The single authoritative balance. You can change it in the Inspector during Play Mode.")]
        [Min(0f), SerializeField] private float currentMoney = 100f;

        [Header("Gem")]
        [Tooltip("The saved secondary currency. Its earning and spending uses can be added later.")]
        [Min(0f), SerializeField] private float currentGems;

        public float CurrentMoney => currentMoney;
        public float CurrentGems => currentGems;
        public event Action<float> MoneyChanged;
        public event Action<float> MoneySpent;
        public event Action<float> GemsChanged;
        public event Action<float> GemsSpent;

        private void Awake()
        {
            currentMoney = Mathf.Max(0f, currentMoney);
            currentGems = LoadGems();
            MoneyChanged?.Invoke(currentMoney);
            GemsChanged?.Invoke(currentGems);
        }

        private void OnValidate()
        {
            currentMoney = Mathf.Max(0f, currentMoney);
            currentGems = Mathf.Max(0f, currentGems);
            if (Application.isPlaying)
            {
                MoneyChanged?.Invoke(currentMoney);
                GemsChanged?.Invoke(currentGems);
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

        public void AddGems(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            SetGems(Mathf.Min(float.MaxValue, currentGems + amount));
        }

        public bool TrySpendGems(float amount)
        {
            if (amount < 0f || amount > currentGems)
            {
                return false;
            }

            SetGems(currentGems - amount);
            if (amount > 0f)
            {
                GemsSpent?.Invoke(amount);
            }
            return true;
        }

        public void SetGems(float amount)
        {
            float safeAmount = Mathf.Max(0f, amount);
            if (Mathf.Approximately(currentGems, safeAmount))
            {
                return;
            }

            currentGems = safeAmount;
            SaveGems();
            GemsChanged?.Invoke(currentGems);
        }

        /// <summary>Clears Gem only during a full data reset. A normal Rebirth keeps Gem.</summary>
        public void ResetGems()
        {
            PlayerPrefs.DeleteKey(GemSaveKey);
            currentGems = gameData != null ? gameData.StartingGems : 0f;
            PlayerPrefs.Save();
            GemsChanged?.Invoke(currentGems);
        }

        private string GemSaveKey => gameData != null ? gameData.GemSaveKey : DefaultGemSaveKey;

        private float LoadGems()
        {
            float startingAmount = gameData != null ? gameData.StartingGems : 0f;
            return Mathf.Max(0f, PlayerPrefs.GetFloat(GemSaveKey, startingAmount));
        }

        private void SaveGems()
        {
            PlayerPrefs.SetFloat(GemSaveKey, currentGems);
            PlayerPrefs.Save();
        }
    }
}
