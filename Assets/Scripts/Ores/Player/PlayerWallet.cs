using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns the player's runtime money balance.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerWallet : MonoBehaviour
    {
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
        public MiningGameData GameData => gameData;
        public event Action<float> MoneyChanged;
        public event Action<float> MoneySpent;
        public event Action<float> GemsChanged;
        public event Action<float> GemsSpent;

        private void Awake()
        {
            currentMoney = Mathf.Max(0f, GameSave.GetFloat(MoneySaveKey, currentMoney));
            MoneyChanged += PersistMoney;
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
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f)
            {
                return;
            }

            currentMoney = Mathf.Min(float.MaxValue, currentMoney + amount);
            MoneyChanged?.Invoke(currentMoney);
        }

        public bool TrySpend(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f || amount > currentMoney)
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

        /// <summary>Theft changes the balance, but is not a purchase/quest MoneySpent event.</summary>
        public float TakeStolenMoney(float requested)
        {
            if (float.IsNaN(requested) || float.IsInfinity(requested) || requested <= 0f) return 0f;
            float taken = Mathf.Min(requested, currentMoney);
            if (taken <= 0f) return 0f;
            currentMoney -= taken;
            MoneyChanged?.Invoke(currentMoney);
            return taken;
        }

        public void SetMoney(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount)) return;
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
                PersistMoney(0f);
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
            GameSave.DeleteKey(GemSaveKey);
            currentGems = gameData != null ? gameData.StartingGems : 0f;
            GameSave.Save();
            GemsChanged?.Invoke(currentGems);
        }

        private string GemSaveKey => gameData != null
            ? gameData.GemSaveKey
            : MiningGameData.DefaultGemSaveKey;

        private float LoadGems()
        {
            float startingAmount = gameData != null ? gameData.StartingGems : 0f;
            return Mathf.Max(0f, GameSave.GetFloat(GemSaveKey, startingAmount));
        }

        private void SaveGems()
        {
            GameSave.SetFloat(GemSaveKey, currentGems);
            GameSave.Save();
        }

        public const string MoneySaveKey = "ChestDefense.Wallet.Money.v1";
        private void PersistMoney(float amount)
        {
            if (!Application.isPlaying) return;
            GameSave.SetFloat(MoneySaveKey, amount);
            GameSave.Save();
        }
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            PersistMoney(currentMoney);
            SaveGems();
        }
    }
}
