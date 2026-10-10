using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MiningUpgradeSystem
    {
        public bool AutoUpgradeUnlocked { get; private set; }
        public bool AutoUpgradeEnabled { get; private set; }
        public event System.Action AutoUpgradeChanged;
        private float nextAutoUpgradeTime;
        public bool IsAutoUpgradePurchase { get; private set; }
        private System.Collections.Generic.IReadOnlyList<MiningUpgradeType> panelUpgradeOrder;

        // Use the actual card order, including buttons appended by the designer.
        public void SetAutoUpgradeOrder(System.Collections.Generic.IReadOnlyList<MiningUpgradeType> order)
        {
            panelUpgradeOrder = order == null ? null : new System.Collections.Generic.List<MiningUpgradeType>(order);
        }

        private void LoadAutoUpgrade()
        {
            if (upgradeData == null || string.IsNullOrWhiteSpace(upgradeData.AutoUpgradeSaveKey)) return;
            AutoUpgradeUnlocked = GameSave.GetInt(upgradeData.AutoUpgradeSaveKey, 0) == 1;
            AutoUpgradeEnabled = AutoUpgradeUnlocked && GameSave.GetInt(upgradeData.AutoUpgradeSaveKey + ".Enabled", 0) == 1;
        }

        public bool TryUnlockAutoUpgrade()
        {
            if (AutoUpgradeUnlocked) return true;
            if (upgradeData != null && (float.IsNaN(upgradeData.AutoUpgradeGemCost) || float.IsInfinity(upgradeData.AutoUpgradeGemCost))) return false;
            if (upgradeData == null || wallet == null || !wallet.TrySpendGems(upgradeData.AutoUpgradeGemCost)) return false;
            AutoUpgradeUnlocked = true;
            SetAutoUpgradeEnabled(true);
            return true;
        }

        public void SetAutoUpgradeEnabled(bool enabled)
        {
            AutoUpgradeEnabled = enabled && AutoUpgradeUnlocked;
            if (upgradeData != null && !string.IsNullOrWhiteSpace(upgradeData.AutoUpgradeSaveKey))
            {
                GameSave.SetInt(upgradeData.AutoUpgradeSaveKey, AutoUpgradeUnlocked ? 1 : 0);
                GameSave.SetInt(upgradeData.AutoUpgradeSaveKey + ".Enabled", AutoUpgradeEnabled ? 1 : 0);
                GameSave.Save();
            }
            AutoUpgradeChanged?.Invoke();
        }

        public void ResetAutoUpgrade()
        {
            AutoUpgradeUnlocked = AutoUpgradeEnabled = false;
            if (upgradeData != null && !string.IsNullOrWhiteSpace(upgradeData.AutoUpgradeSaveKey))
            {
                GameSave.DeleteKey(upgradeData.AutoUpgradeSaveKey);
                GameSave.DeleteKey(upgradeData.AutoUpgradeSaveKey + ".Enabled");
                GameSave.Save();
            }
            AutoUpgradeChanged?.Invoke();
        }

        private void Update()
        {
            if (!AutoUpgradeEnabled || upgradeData == null || Time.timeScale <= 0f || Time.time < nextAutoUpgradeTime) return;
            nextAutoUpgradeTime = Time.time + upgradeData.AutoUpgradeInterval;
            TryAutoUpgradeOnce();
        }

        // One purchase per tick. Always start at the top; skip unaffordable/maxed cards.
        public bool TryAutoUpgradeOnce()
        {
            if (!AutoUpgradeEnabled || upgradeData == null) return false;
            var order = panelUpgradeOrder ?? upgradeData.AutoUpgradeOrder;
            if (order == null || order.Count == 0) return false;
            for (int i = 0; i < order.Count; i++)
            {
                var type = order[i];
                if (!System.Enum.IsDefined(typeof(MiningUpgradeType), type)) continue;
                // These upgrades belong to the card choice system, never the Money panel.
                if (type == MiningUpgradeType.RegenIntervalReduction || type == MiningUpgradeType.HealingEffectiveness) continue;
                IsAutoUpgradePurchase = true;
                try
                {
                    if (TryPurchase(type)) return true;
                }
                finally
                {
                    IsAutoUpgradePurchase = false;
                }
            }
            return false;
        }
    }
}
