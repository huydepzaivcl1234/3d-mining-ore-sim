using System;
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
        [SerializeField] private MiningUiPanelCoordinator panelCoordinator;
        [SerializeField] private Button openButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button moneyRewardButton;
        [SerializeField] private Button rareOreSpawnButton;
        [SerializeField] private Button oreDamageButton;
        [SerializeField] private Button oreSpawnSpeedButton;
        [SerializeField] private Button npcMoveSpeedButton;
        [SerializeField] private Button npcCapacityButton;
        [SerializeField] private Button luckyBlockRewardButton;
        [SerializeField] private Button luckyBlockDropChanceButton;
        [SerializeField] private TextMeshProUGUI moneyRewardLabel;
        [SerializeField] private TextMeshProUGUI rareOreSpawnLabel;
        [SerializeField] private TextMeshProUGUI oreDamageLabel;
        [SerializeField] private TextMeshProUGUI oreSpawnSpeedLabel;
        [SerializeField] private TextMeshProUGUI npcMoveSpeedLabel;
        [SerializeField] private TextMeshProUGUI npcCapacityLabel;
        [SerializeField] private TextMeshProUGUI luckyBlockRewardLabel;
        [SerializeField] private TextMeshProUGUI luckyBlockDropChanceLabel;
        [SerializeField] private bool openOnPlay;

        [Header("Editable Text")]
        [SerializeField] private string upgradeFormat = "{0}\n+{1:0.##}%  [{2}/{3}]  -  {4} tiền";
        [SerializeField] private string maximumFormat = "{0}\n+{1:0.##}%  [{2}/{3}]  -  TỐI ĐA";
        [SerializeField] private string capacityFormat = "{0}\n+{1:0} thợ mỏ  [{2}/{3}]  -  {4} tiền";
        [SerializeField] private string capacityMaximumFormat = "{0}\n+{1:0} thợ mỏ  [{2}/{3}]  -  TỐI ĐA";

        public event Action PanelOpened;
        public event Action PanelClosed;

        private void Awake()
        {
            if (openOnPlay)
            {
                shopPanel?.SetActive(false);
                upgradePanel?.SetActive(true);
            }
            else
            {
                upgradePanel?.SetActive(false);
                shopPanel?.SetActive(true);
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
            oreSpawnSpeedButton?.onClick.AddListener(BuyOreSpawnSpeed);
            npcMoveSpeedButton?.onClick.AddListener(BuyNpcMoveSpeed);
            npcCapacityButton?.onClick.AddListener(BuyNpcCapacity);
            luckyBlockRewardButton?.onClick.AddListener(BuyLuckyBlockReward);
            luckyBlockDropChanceButton?.onClick.AddListener(BuyLuckyBlockDropChance);
        }

        private void RemoveListeners()
        {
            openButton?.onClick.RemoveListener(OpenPanel);
            backButton?.onClick.RemoveListener(ClosePanel);
            closeButton?.onClick.RemoveListener(ClosePanel);
            moneyRewardButton?.onClick.RemoveListener(BuyMoneyReward);
            rareOreSpawnButton?.onClick.RemoveListener(BuyRareOreSpawn);
            oreDamageButton?.onClick.RemoveListener(BuyOreDamage);
            oreSpawnSpeedButton?.onClick.RemoveListener(BuyOreSpawnSpeed);
            npcMoveSpeedButton?.onClick.RemoveListener(BuyNpcMoveSpeed);
            npcCapacityButton?.onClick.RemoveListener(BuyNpcCapacity);
            luckyBlockRewardButton?.onClick.RemoveListener(BuyLuckyBlockReward);
            luckyBlockDropChanceButton?.onClick.RemoveListener(BuyLuckyBlockDropChance);
        }

        private void OpenPanel()
        {
            if (panelCoordinator != null)
            {
                panelCoordinator.OpenPanel(upgradePanel != null
                    ? upgradePanel.GetComponent<RectTransform>()
                    : null);
            }
            else
            {
                shopPanel?.SetActive(false);
                upgradePanel?.SetActive(true);
            }
            Refresh();
            PanelOpened?.Invoke();
        }

        private void ClosePanel()
        {
            if (panelCoordinator != null)
            {
                panelCoordinator.ClosePanel(upgradePanel != null
                    ? upgradePanel.GetComponent<RectTransform>()
                    : null);
            }
            else
            {
                upgradePanel?.SetActive(false);
                shopPanel?.SetActive(true);
            }
            PanelClosed?.Invoke();
        }

        private void BuyMoneyReward() => Buy(MiningUpgradeType.MoneyReward);
        private void BuyRareOreSpawn() => Buy(MiningUpgradeType.RareOreSpawn);
        private void BuyOreDamage() => Buy(MiningUpgradeType.OreDamage);
        private void BuyOreSpawnSpeed() => Buy(MiningUpgradeType.OreSpawnSpeed);
        private void BuyNpcMoveSpeed() => Buy(MiningUpgradeType.NpcMoveSpeed);
        private void BuyNpcCapacity() => Buy(MiningUpgradeType.NpcCapacity);
        private void BuyLuckyBlockReward() => Buy(MiningUpgradeType.LuckyBlockReward);
        private void BuyLuckyBlockDropChance() => Buy(MiningUpgradeType.LuckyBlockDropChance);

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
            RefreshUpgrade(MiningUpgradeType.OreSpawnSpeed, oreSpawnSpeedButton, oreSpawnSpeedLabel);
            RefreshUpgrade(MiningUpgradeType.NpcMoveSpeed, npcMoveSpeedButton, npcMoveSpeedLabel);
            RefreshCapacityUpgrade();
            RefreshUpgrade(MiningUpgradeType.LuckyBlockReward, luckyBlockRewardButton,
                luckyBlockRewardLabel);
            RefreshUpgrade(MiningUpgradeType.LuckyBlockDropChance, luckyBlockDropChanceButton,
                luckyBlockDropChanceLabel);
        }

        private void RefreshCapacityUpgrade()
        {
            if (upgradeSystem == null || upgradeSystem.UpgradeData == null)
            {
                if (npcCapacityButton != null) npcCapacityButton.interactable = false;
                return;
            }

            MiningUpgradeDefinition definition =
                upgradeSystem.UpgradeData.GetDefinition(MiningUpgradeType.NpcCapacity);
            int stacks = upgradeSystem.GetStacks(MiningUpgradeType.NpcCapacity);
            bool maximum = upgradeSystem.IsMaximum(MiningUpgradeType.NpcCapacity);
            if (npcCapacityLabel != null)
            {
                npcCapacityLabel.text = string.Format(maximum ? capacityMaximumFormat : capacityFormat,
                    definition.DisplayName, definition.ValuePerStack, stacks,
                    definition.MaximumStacks, MiningMoneyFormatter.Format(
                        upgradeSystem.GetCost(MiningUpgradeType.NpcCapacity)));
            }
            if (npcCapacityButton != null)
            {
                npcCapacityButton.interactable =
                    upgradeSystem.CanPurchase(MiningUpgradeType.NpcCapacity);
            }
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
                    definition.MaximumStacks, MiningMoneyFormatter.Format(
                        upgradeSystem.GetCost(type)));
            }
            if (button != null)
            {
                button.interactable = upgradeSystem.CanPurchase(type);
            }
        }
    }
}
