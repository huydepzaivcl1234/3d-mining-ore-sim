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
        [SerializeField] private NpcShop npcShop;
        [SerializeField] private MiningItemSystem itemSystem;
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
        // Rebirth's permanent boost now applies to both money and NPC experience, at the
        // same multiplier value.
        public float PermanentExperienceMultiplier => PermanentMoneyMultiplier;
        public float NextExperienceMultiplier => NextMoneyMultiplier;
        public bool CanRebirth => wallet != null && rebirthData != null &&
                                  wallet.CurrentMoney >= CurrentRequirement;
        public float Progress01 => wallet == null || rebirthData == null
            ? 0f
            : Mathf.Clamp01((float)wallet.CurrentMoney / CurrentRequirement);

        public event Action StateChanged;
        public event Action<int> RebirthCompleted;

        private void Awake()
        {
            FindResetTargetsIfMissing();
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
            npcShop?.ResetAllNpcs();
            wallet.ResetMoney();
            RebirthCompleted?.Invoke(completedRebirths);
            StateChanged?.Invoke();
            return true;
        }

        /// <summary>Clears all gameplay progression, including the saved Rebirth count.</summary>
        public void ResetAllProgress()
        {
            FindResetTargetsIfMissing();
            completedRebirths = 0;
            if (rebirthData != null)
            {
                PlayerPrefs.DeleteKey(rebirthData.RebirthCountSaveKey);
                PlayerPrefs.Save();
            }

            upgradeSystem?.ResetAllUpgrades();
            npcProgressionSystem?.ResetProgression();
            npcShop?.ResetAllNpcs();
            itemSystem?.ResetAllData();
            wallet?.ResetMoney();
            ApplyPermanentBoost();
            StateChanged?.Invoke();
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
            upgradeSystem?.SetPermanentExperienceMultiplier(PermanentExperienceMultiplier);
        }

        private void FindResetTargetsIfMissing()
        {
            if (npcProgressionSystem == null)
            {
                npcProgressionSystem = FindFirstObjectByType<NpcProgressionSystem>(
                    FindObjectsInactive.Include);
            }
            if (npcShop == null)
            {
                npcShop = FindFirstObjectByType<NpcShop>(FindObjectsInactive.Include);
            }
            if (itemSystem == null)
            {
                itemSystem = FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
            }
        }

        private void HandleMoneyChanged(float money)
        {
            StateChanged?.Invoke();
        }
    }
}