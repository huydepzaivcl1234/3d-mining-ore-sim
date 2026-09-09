using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns saved rebirth progression and applies its permanent money multiplier.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningRebirthSystem : MonoBehaviour
    {
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MiningUpgradeSystem upgradeSystem;
        [SerializeField] private MiningRebirthData rebirthData;
        [SerializeField] private NpcProgressionSystem npcProgressionSystem;
        private int completedRebirths;

        public int CompletedRebirths => completedRebirths;
        public int CurrentRequirement => rebirthData != null
            ? rebirthData.GetRequirement(completedRebirths)
            : int.MaxValue;
        public float PermanentMoneyMultiplier => rebirthData != null
            ? rebirthData.GetMoneyMultiplier(completedRebirths)
            : 1f;
        public float NextMoneyMultiplier => rebirthData != null
            ? rebirthData.GetMoneyMultiplier(completedRebirths + 1)
            : 1f;
        public bool CanRebirth => wallet != null && rebirthData != null &&
                                  wallet.CurrentMoney >= CurrentRequirement;
        public float Progress01 => wallet == null || rebirthData == null
            ? 0f
            : Mathf.Clamp01((float)wallet.CurrentMoney / CurrentRequirement);

        public event Action StateChanged;
        public event Action<int> RebirthCompleted;

        private void Awake()
        {
            LoadProgress();
            ApplyPermanentBoost();
        }

        private void OnEnable()
        {
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
                wallet.MoneyChanged += HandleMoneyChanged;
            }
        }

        private void OnDisable()
        {
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
            }
        }

        public bool TryRebirth()
        {
            if (!CanRebirth)
            {
                return false;
            }

            completedRebirths++;
            SaveProgress();
            ApplyPermanentBoost();
            upgradeSystem?.ResetAllUpgrades();
            npcProgressionSystem?.ResetProgression();
            wallet.ResetMoney();
            RebirthCompleted?.Invoke(completedRebirths);
            StateChanged?.Invoke();
            return true;
        }

        private void LoadProgress()
        {
            completedRebirths = rebirthData == null
                ? 0
                : Mathf.Max(0, PlayerPrefs.GetInt(rebirthData.RebirthCountSaveKey, 0));
        }

        private void SaveProgress()
        {
            if (rebirthData == null)
            {
                return;
            }

            PlayerPrefs.SetInt(rebirthData.RebirthCountSaveKey, completedRebirths);
            PlayerPrefs.Save();
        }

        private void ApplyPermanentBoost()
        {
            upgradeSystem?.SetPermanentMoneyMultiplier(PermanentMoneyMultiplier);
        }

        private void HandleMoneyChanged(float money)
        {
            StateChanged?.Invoke();
        }
    }
}
