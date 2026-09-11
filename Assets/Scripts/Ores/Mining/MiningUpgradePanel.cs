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
        [SerializeField] private Button npcExperienceButton;
        [SerializeField] private Button itemDropChanceButton;
        [SerializeField] private TextMeshProUGUI moneyRewardLabel;
        [SerializeField] private TextMeshProUGUI rareOreSpawnLabel;
        [SerializeField] private TextMeshProUGUI oreDamageLabel;
        [SerializeField] private TextMeshProUGUI oreSpawnSpeedLabel;
        [SerializeField] private TextMeshProUGUI npcMoveSpeedLabel;
        [SerializeField] private TextMeshProUGUI npcCapacityLabel;
        [SerializeField] private TextMeshProUGUI luckyBlockRewardLabel;
        [SerializeField] private TextMeshProUGUI luckyBlockDropChanceLabel;
        [SerializeField] private TextMeshProUGUI npcExperienceLabel;
        [SerializeField] private TextMeshProUGUI itemDropChanceLabel;
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
            // The 10 purchase buttons get their own distinct feedback (UpgradePurchasedSfx,
            // fired once per successful buy via upgradeSystem.UpgradePurchased — see
            // MiningAudioManager.HandleUpgradePurchased). Strip the generic per-click SFX
            // component from just those buttons so a buy doesn't also play the shared button
            // click sound; Open/Back/Close keep it untouched.
            StripGenericClickSfx(moneyRewardButton, rareOreSpawnButton, oreDamageButton,
                oreSpawnSpeedButton, npcMoveSpeedButton, npcCapacityButton,
                luckyBlockRewardButton, luckyBlockDropChanceButton, npcExperienceButton,
                itemDropChanceButton);

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

        private static void StripGenericClickSfx(params Button[] purchaseButtons)
        {
            foreach (Button button in purchaseButtons)
            {
                if (button == null)
                {
                    continue;
                }

                MiningButtonSfx genericSfx = button.GetComponent<MiningButtonSfx>();
                if (genericSfx != null)
                {
                    Destroy(genericSfx);
                }
            }
        }

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
            MiningLocalization.LanguageChanged += HandleLanguageChanged;
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
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
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
            npcExperienceButton?.onClick.AddListener(BuyNpcExperience);
            itemDropChanceButton?.onClick.AddListener(BuyItemDropChance);
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
            npcExperienceButton?.onClick.RemoveListener(BuyNpcExperience);
            itemDropChanceButton?.onClick.RemoveListener(BuyItemDropChance);
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
        private void BuyNpcExperience() => Buy(MiningUpgradeType.NpcExperience);
        private void BuyItemDropChance() => Buy(MiningUpgradeType.ItemDropChance);

        private void Buy(MiningUpgradeType type)
        {
            upgradeSystem?.TryPurchase(type);
            Refresh();
        }

        private void HandleMoneyChanged(float money) => Refresh();

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
            RefreshUpgrade(MiningUpgradeType.NpcExperience, npcExperienceButton,
                npcExperienceLabel);
            RefreshUpgrade(MiningUpgradeType.ItemDropChance, itemDropChanceButton,
                itemDropChanceLabel);
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
                string format = maximum
                    ? MiningLocalization.Text(
                        "{0}\n+{1:0} miners  [{2}/{3}]  -  MAX", capacityMaximumFormat)
                    : MiningLocalization.Text(
                        "{0}\n+{1:0} miners  [{2}/{3}]  -  {4} money", capacityFormat);
                npcCapacityLabel.text = string.Format(format,
                    MiningLocalization.GetUpgradeName(MiningUpgradeType.NpcCapacity,
                        definition.DisplayName), definition.ValuePerStack, stacks,
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
                string format = maximum
                    ? MiningLocalization.Text(
                        "{0}\n+{1:0.##}%  [{2}/{3}]  -  MAX", maximumFormat)
                    : MiningLocalization.Text(
                        "{0}\n+{1:0.##}%  [{2}/{3}]  -  {4} money", upgradeFormat);
                label.text = string.Format(format,
                    MiningLocalization.GetUpgradeName(type, definition.DisplayName),
                    definition.PercentPerStack, stacks,
                    definition.MaximumStacks, MiningMoneyFormatter.Format(
                        upgradeSystem.GetCost(type)));
            }
            if (button != null)
            {
                button.interactable = upgradeSystem.CanPurchase(type);
            }
        }

        private void HandleLanguageChanged()
        {
            Refresh();
        }
    }
}