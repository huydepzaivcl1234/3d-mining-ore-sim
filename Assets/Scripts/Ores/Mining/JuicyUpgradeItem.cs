using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Leather card presentation; purchases and save state stay with MiningUpgradePanel/System.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class JuicyUpgradeItem : MonoBehaviour
    {
        [SerializeField] private MiningUpgradeType upgradeType;
        [SerializeField] private MiningUpgradeSystem upgradeSystem;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI detailText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI buyCaptionText;
        [SerializeField] private UnityEngine.UI.Image priceCoinIcon;
        [Tooltip("Optional SVG card icon override; otherwise reuse the authored icon child.")]
        [SerializeField] private Sprite cardIcon;

        private UnityEngine.UI.Button purchaseButton;
        [SerializeField, HideInInspector] private bool ownsPurchase;
        public MiningUpgradeType UpgradeType => upgradeType;
        public Sprite CardIcon => cardIcon;

        public void BindCard(MiningUpgradeSystem system, PlayerWallet playerWallet)
        {
            upgradeSystem = system; wallet = playerWallet; ownsPurchase = true;
        }

        public void ConfigureCard(MiningUpgradeSystem system, PlayerWallet playerWallet,
            TextMeshProUGUI title, TextMeshProUGUI detail, TextMeshProUGUI price)
        {
            // Called while the card is inactive, before subscribing to its new view.
            upgradeSystem = system; wallet = playerWallet;
            titleText = title; detailText = detail; priceText = price;
            buyCaptionText = null; priceCoinIcon = null;
            ownsPurchase = true;
        }

        private void Purchase() => upgradeSystem?.TryPurchase(upgradeType);

        private void Awake()
        {
            purchaseButton = GetComponent<UnityEngine.UI.Button>();
        }

        private void OnEnable()
        {
            purchaseButton ??= GetComponent<UnityEngine.UI.Button>();
            if (ownsPurchase) { purchaseButton.onClick.RemoveListener(Purchase); purchaseButton.onClick.AddListener(Purchase); }
            MiningLocalization.LanguageChanged += Refresh;
            if (upgradeSystem != null) upgradeSystem.UpgradesChanged += Refresh;
            if (wallet != null) wallet.MoneyChanged += HandleMoneyChanged;
            Refresh();
        }

        private void OnDisable()
        {
            purchaseButton?.onClick.RemoveListener(Purchase);
            MiningLocalization.LanguageChanged -= Refresh;
            if (upgradeSystem != null) upgradeSystem.UpgradesChanged -= Refresh;
            if (wallet != null) wallet.MoneyChanged -= HandleMoneyChanged;
        }

        private void HandleMoneyChanged(float amount) => Refresh();

        public void Refresh()
        {
            if (purchaseButton == null) purchaseButton = GetComponent<UnityEngine.UI.Button>();
            if (buyCaptionText != null) buyCaptionText.text = MiningLocalization.Text("UPGRADE", "NÂNG CẤP");
            MiningUpgradeDefinition definition = upgradeSystem != null && upgradeSystem.UpgradeData != null
                ? upgradeSystem.UpgradeData.GetDefinition(upgradeType) : null;
            if (definition == null)
            {
                if (titleText != null) titleText.text = MiningLocalization.Text("Upgrade unavailable", "Chưa có nâng cấp");
                if (detailText != null) detailText.text = string.Empty;
                if (priceText != null) priceText.text = "—";
                SetCoinVisible(false);
                purchaseButton.interactable = false;
                return;
            }

            int stacks = upgradeSystem.GetStacks(upgradeType);
            bool maximum = upgradeSystem.IsMaximum(upgradeType);
            if (titleText != null)
                titleText.text = MiningLocalization.GetUpgradeName(upgradeType, definition.DisplayName);
            if (detailText != null)
            {
                float value = upgradeType == MiningUpgradeType.NpcCapacity
                    ? definition.ValuePerStack : definition.PercentPerStack;
                string format = upgradeType == MiningUpgradeType.NpcCapacity
                    ? MiningLocalization.Text("+{0:0.##} miner/Lv {1}/{2}", "+{0:0.##} thợ/cấp {1}/{2}")
                    : MiningLocalization.Text("+{0:0.##}% / level [{1}/{2}]", "+{0:0.##}% / cấp [{1}/{2}]");
                detailText.text = string.Format(format, value, stacks, definition.MaximumStacks);
            }
            if (priceText != null)
                priceText.text = maximum ? MiningLocalization.Text("MAX", "TỐI ĐA")
                    : MiningMoneyFormatter.Format(upgradeSystem.GetCost(upgradeType));
            SetCoinVisible(!maximum);
            purchaseButton.interactable = upgradeSystem.CanPurchase(upgradeType);
        }

        private void SetCoinVisible(bool visible)
        {
            if (priceCoinIcon != null)
            {
                priceCoinIcon.gameObject.SetActive(visible);
            }
        }
    }
}
