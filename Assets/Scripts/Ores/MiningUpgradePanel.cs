using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Connects the editable upgrade panel to MiningUpgradeSystem.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningUpgradePanel : MonoBehaviour
    {
        [SerializeField] private MiningUpgradeSystem upgradeSystem;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button moneyRewardButton;
        [SerializeField] private Button rareOreSpawnButton;
        [SerializeField] private Button oreDamageButton;
        [SerializeField] private TextMeshProUGUI moneyRewardLabel;
        [SerializeField] private TextMeshProUGUI rareOreSpawnLabel;
        [SerializeField] private TextMeshProUGUI oreDamageLabel;
        [SerializeField] private bool openOnPlay;

        [Header("Editable Text")]
        [SerializeField] private string upgradeFormat = "{0}\n+{1:0.##}%  [{2}/{3}]  -  {4} tiền";
        [SerializeField] private string maximumFormat = "{0}\n+{1:0.##}%  [{2}/{3}]  -  TỐI ĐA";

        private void Awake()
        {
            if (openOnPlay)
            {
                OpenPanel();
            }
            else
            {
                ClosePanel();
            }
        }

        private void OnEnable()
        {
            AddListeners();
            if (upgradeSystem != null)
            {
                upgradeSystem.UpgradesChanged -= Refresh;
                upgradeSystem.UpgradesChanged += Refresh;
            }
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
                wallet.MoneyChanged += HandleMoneyChanged;
            }
            Refresh();
        }

        private void OnDisable()
        {
            RemoveListeners();
            if (upgradeSystem != null)
            {
                upgradeSystem.UpgradesChanged -= Refresh;
            }
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
            }
        }

        private void AddListeners()
        {
            openButton?.onClick.AddListener(OpenPanel);
            backButton?.onClick.AddListener(ClosePanel);
            closeButton?.onClick.AddListener(ClosePanel);
            moneyRewardButton?.onClick.AddListener(BuyMoneyReward);
            rareOreSpawnButton?.onClick.AddListener(BuyRareOreSpawn);
            oreDamageButton?.onClick.AddListener(BuyOreDamage);
        }

        private void RemoveListeners()
        {
            openButton?.onClick.RemoveListener(OpenPanel);
            backButton?.onClick.RemoveListener(ClosePanel);
            closeButton?.onClick.RemoveListener(ClosePanel);
            moneyRewardButton?.onClick.RemoveListener(BuyMoneyReward);
            rareOreSpawnButton?.onClick.RemoveListener(BuyRareOreSpawn);
            oreDamageButton?.onClick.RemoveListener(BuyOreDamage);
        }

        private void OpenPanel()
        {
            shopPanel?.SetActive(false);
            upgradePanel?.SetActive(true);
            Refresh();
        }

        private void ClosePanel()
        {
            upgradePanel?.SetActive(false);
            shopPanel?.SetActive(true);
        }

        private void BuyMoneyReward() => Buy(MiningUpgradeType.MoneyReward);
        private void BuyRareOreSpawn() => Buy(MiningUpgradeType.RareOreSpawn);
        private void BuyOreDamage() => Buy(MiningUpgradeType.OreDamage);

        private void Buy(MiningUpgradeType type)
        {
            upgradeSystem?.TryPurchase(type);
            Refresh();
        }

        private void HandleMoneyChanged(int money) => Refresh();

        private void Refresh()
        {
            RefreshUpgrade(MiningUpgradeType.MoneyReward, moneyRewardButton, moneyRewardLabel);
            RefreshUpgrade(MiningUpgradeType.RareOreSpawn, rareOreSpawnButton, rareOreSpawnLabel);
            RefreshUpgrade(MiningUpgradeType.OreDamage, oreDamageButton, oreDamageLabel);
        }

        private void RefreshUpgrade(MiningUpgradeType type, Button button, TextMeshProUGUI label)
        {
            if (upgradeSystem == null || upgradeSystem.UpgradeData == null)
            {
                if (button != null) button.interactable = false;
                return;
            }

            MiningUpgradeDefinition definition = upgradeSystem.UpgradeData.GetDefinition(type);
            int stacks = upgradeSystem.GetStacks(type);
            bool maximum = upgradeSystem.IsMaximum(type);
            if (label != null)
            {
                label.text = string.Format(maximum ? maximumFormat : upgradeFormat,
                    definition.DisplayName, definition.PercentPerStack, stacks,
                    definition.MaximumStacks, upgradeSystem.GetCost(type));
            }
            if (button != null)
            {
                button.interactable = upgradeSystem.CanPurchase(type);
            }
        }
    }
}
