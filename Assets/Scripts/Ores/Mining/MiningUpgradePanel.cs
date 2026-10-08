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
        [SerializeField] private Button luckyBlockRewardButton;
        [SerializeField] private Button luckyBlockDropChanceButton;
        [SerializeField] private Button itemDropChanceButton;
        [SerializeField] private TextMeshProUGUI moneyRewardLabel;
        [SerializeField] private TextMeshProUGUI luckyBlockRewardLabel;
        [SerializeField] private TextMeshProUGUI luckyBlockDropChanceLabel;
        [SerializeField] private TextMeshProUGUI itemDropChanceLabel;
        [SerializeField] private bool openOnPlay;
        private MiningUpgradeStation worldStation;
        private bool usesCardPurchases;

        private void Start()
        {
            // Recover the authored view when scene references were cleared, without making a second panel.
            if (upgradePanel == null)
            {
                var item = FindFirstObjectByType<JuicyUpgradeItem>(FindObjectsInactive.Include);
                if (item != null) upgradePanel = item.transform.parent.gameObject;
            }
            worldStation = FindFirstObjectByType<MiningUpgradeStation>(FindObjectsInactive.Include);
            if (worldStation == null)
            {
                var prefab = Resources.Load<MiningUpgradeStation>("MiningUpgradeStation");
                if (prefab != null) worldStation = Instantiate(prefab);
            }
            if (worldStation == null || upgradePanel == null) return;
            RemoveListeners();
            usesCardPurchases = true;
            var carousel = upgradePanel.GetComponent<MiningUpgradeCarousel>();
            if (carousel == null) carousel = upgradePanel.AddComponent<MiningUpgradeCarousel>();
            carousel.Initialize(upgradeSystem, wallet, worldStation.UpgradeOrder, worldStation.Close);
            var autoButton = upgradePanel.GetComponent<MiningAutoUpgradeButton>() ?? upgradePanel.AddComponent<MiningAutoUpgradeButton>();
            autoButton.Configure(upgradeSystem);
            panelCoordinator?.RegisterWorldUpgradePanel(upgradePanel.GetComponent<RectTransform>());
            worldStation.Initialize(this, upgradePanel.GetComponent<RectTransform>(), panelCoordinator);
            // Preserve the authored reference/listeners, but retire the flat HUD button.
            if (openButton != null) openButton.gameObject.SetActive(false);
        }

        internal void NotifyWorldPanelState(bool opened)
        {
            if (opened) { Refresh(); PanelOpened?.Invoke(); }
            else PanelClosed?.Invoke();
        }

        [Header("Editable Text")]
        [SerializeField] private string upgradeFormat = "{0}\n+{1:0.##}%  [{2}/{3}]  -  {4} tiền";
        [SerializeField] private string maximumFormat = "{0}\n+{1:0.##}%  [{2}/{3}]  -  TỐI ĐA";

        public event Action PanelOpened;
        public event Action PanelClosed;

        private void Awake()
        {
            if (upgradePanel == null)
            {
                var item = FindFirstObjectByType<JuicyUpgradeItem>(FindObjectsInactive.Include);
                if (item != null) upgradePanel = item.transform.parent.gameObject;
            }
            // Unity's destroyed-object placeholders are not CLR null: normalize cleared scene bindings.
            if (openButton == null) openButton = null;
            if (backButton == null) backButton = null;
            if (closeButton == null) closeButton = null;
            if (moneyRewardButton == null) moneyRewardButton = null;
            if (luckyBlockRewardButton == null) luckyBlockRewardButton = null;
            if (luckyBlockDropChanceButton == null) luckyBlockDropChanceButton = null;
            if (itemDropChanceButton == null) itemDropChanceButton = null;
            // The 10 purchase buttons get their own distinct feedback (UpgradePurchasedSfx,
            // fired once per successful buy via upgradeSystem.UpgradePurchased — see
            // MiningAudioManager.HandleUpgradePurchased). Strip the generic per-click SFX
            // component from just those buttons so a buy doesn't also play the shared button
            // click sound; Open/Back/Close keep it untouched.
            StripGenericClickSfx(moneyRewardButton, luckyBlockRewardButton, luckyBlockDropChanceButton, itemDropChanceButton);

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
            if (usesCardPurchases) return;
            openButton?.onClick.AddListener(OpenPanel);
            backButton?.onClick.AddListener(ClosePanel);
            closeButton?.onClick.AddListener(ClosePanel);
            moneyRewardButton?.onClick.AddListener(BuyMoneyReward);
            luckyBlockRewardButton?.onClick.AddListener(BuyLuckyBlockReward);
            luckyBlockDropChanceButton?.onClick.AddListener(BuyLuckyBlockDropChance);
            itemDropChanceButton?.onClick.AddListener(BuyItemDropChance);
        }

        private void RemoveListeners()
        {
            openButton?.onClick.RemoveListener(OpenPanel);
            backButton?.onClick.RemoveListener(ClosePanel);
            closeButton?.onClick.RemoveListener(ClosePanel);
            moneyRewardButton?.onClick.RemoveListener(BuyMoneyReward);
            luckyBlockRewardButton?.onClick.RemoveListener(BuyLuckyBlockReward);
            luckyBlockDropChanceButton?.onClick.RemoveListener(BuyLuckyBlockDropChance);
            itemDropChanceButton?.onClick.RemoveListener(BuyItemDropChance);
        }

        private void OpenPanel()
        {
            if (worldStation != null) { worldStation.Open(); return; }
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
            if (worldStation != null) { worldStation.Close(); return; }
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





        private void BuyLuckyBlockReward() => Buy(MiningUpgradeType.LuckyBlockReward);
        private void BuyLuckyBlockDropChance() => Buy(MiningUpgradeType.LuckyBlockDropChance);

        private void BuyItemDropChance() => Buy(MiningUpgradeType.ItemDropChance);

        private void Buy(MiningUpgradeType type)
        {
            upgradeSystem?.TryPurchase(type);
            Refresh();
        }

        private void HandleMoneyChanged(float money) => Refresh();

        private void Refresh()
        {
            if (worldStation != null) return; // Each extensible SVG card refreshes from the same system events.
            RefreshUpgrade(MiningUpgradeType.MoneyReward, moneyRewardButton, moneyRewardLabel);
            RefreshUpgrade(MiningUpgradeType.LuckyBlockReward, luckyBlockRewardButton,
                luckyBlockRewardLabel);
            RefreshUpgrade(MiningUpgradeType.LuckyBlockDropChance, luckyBlockDropChanceButton,
                luckyBlockDropChanceLabel);
            RefreshUpgrade(MiningUpgradeType.ItemDropChance, itemDropChanceButton,
                itemDropChanceLabel);
        }



        private void RefreshUpgrade(MiningUpgradeType type, Button button, TextMeshProUGUI label)
        {
            if (upgradeSystem == null || upgradeSystem.UpgradeData == null)
            {
                SetButtonAvailability(button, false);
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
            SetButtonAvailability(button, upgradeSystem.CanPurchase(type));
        }

        private void SetButtonAvailability(Button button, bool available)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = available;
            CanvasGroup group = button.GetComponent<CanvasGroup>() ??
                                button.gameObject.AddComponent<CanvasGroup>();
            group.alpha = available ? 1f : GetUnavailableAlpha();
        }

        private float GetUnavailableAlpha()
        {
            MiningUiData data = panelCoordinator != null ? panelCoordinator.UiData : null;
            return data != null ? data.UpgradeUnavailableAlpha : 0.42f;
        }

        private void HandleLanguageChanged()
        {
            Refresh();
        }
    }
}
