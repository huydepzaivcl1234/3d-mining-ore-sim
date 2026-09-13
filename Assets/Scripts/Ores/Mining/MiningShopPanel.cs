using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Shared Gem Shop opened from the main menu and gameplay HUD.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningShopPanel : MonoBehaviour
    {
        private enum PurchaseStatus
        {
            None,
            Purchased,
            NotEnoughGems,
            InventoryFull,
            MissingProduct
        }

        [Header("Data and systems")]
        [SerializeField] private MiningShopData data;
        [SerializeField] private MiningGameData gameData;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MiningItemSystem itemSystem;
        [SerializeField] private MiningUiPanelCoordinator panelCoordinator;

        [Header("Panel")]
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private Button gameplayOpenButton;
        [SerializeField] private RectTransform gemHud;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button buyRareGiftButton;

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI gameplayButtonLabel;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI gemBalanceLabel;
        [SerializeField] private TextMeshProUGUI itemNameLabel;
        [SerializeField] private TextMeshProUGUI itemDescriptionLabel;
        [SerializeField] private TextMeshProUGUI priceLabel;
        [SerializeField] private TextMeshProUGUI buyLabel;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemIconFallback;

        private PurchaseStatus purchaseStatus;
        private readonly MiningAnimatedCurrencyValue gemCounter = new();

        private void Awake()
        {
            RegisterBaseHud();
            panelRoot?.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            gameData ??= wallet != null ? wallet.GameData : null;
            RegisterBaseHud();
            RemoveListeners();
            gameplayOpenButton?.onClick.AddListener(Open);
            closeButton?.onClick.AddListener(Close);
            buyRareGiftButton?.onClick.AddListener(BuyRareGiftBox);
            MiningLocalization.LanguageChanged += Refresh;
            if (wallet != null)
            {
                wallet.GemsChanged += HandleGemsChanged;
                gemCounter.Initialize(wallet.CurrentGems);
            }
            if (itemSystem != null)
            {
                itemSystem.InventoryChanged += Refresh;
            }
            Refresh();
        }

        private void Update()
        {
            if (gemCounter.Tick(Time.unscaledDeltaTime))
            {
                RefreshGemBalance();
            }
        }

        private void OnDisable()
        {
            RemoveListeners();
        }

        public void Open()
        {
            if (panelRoot == null)
            {
                return;
            }

            purchaseStatus = PurchaseStatus.None;
            Refresh();
            panelRoot.SetAsLastSibling();
            if (panelCoordinator != null)
            {
                panelCoordinator.OpenPanel(panelRoot);
            }
            else
            {
                panelRoot.gameObject.SetActive(true);
            }
            EventSystem.current?.SetSelectedGameObject(buyRareGiftButton != null
                ? buyRareGiftButton.gameObject
                : null);
        }

        public void Close()
        {
            if (panelCoordinator != null)
            {
                panelCoordinator.ClosePanel(panelRoot);
            }
            else
            {
                panelRoot?.gameObject.SetActive(false);
            }
        }

        public void BuyRareGiftBox()
        {
            MiningItemData gift = data != null ? data.RareGiftBox : null;
            float cost = data != null ? data.RareGiftBoxGemCost : 0f;
            if (gift == null || wallet == null || itemSystem == null)
            {
                purchaseStatus = PurchaseStatus.MissingProduct;
                Refresh();
                return;
            }
            if (!itemSystem.CanAddItem(gift))
            {
                purchaseStatus = PurchaseStatus.InventoryFull;
                Refresh();
                return;
            }
            if (!wallet.TrySpendGems(cost))
            {
                purchaseStatus = PurchaseStatus.NotEnoughGems;
                Refresh();
                return;
            }
            if (!itemSystem.TryAddItem(gift))
            {
                wallet.AddGems(cost);
                purchaseStatus = PurchaseStatus.InventoryFull;
                Refresh();
                return;
            }

            purchaseStatus = PurchaseStatus.Purchased;
            Refresh();
        }

        private void Refresh()
        {
            MiningItemData gift = data != null ? data.RareGiftBox : null;
            float cost = data != null ? data.RareGiftBoxGemCost : 100f;
            SetText(gameplayButtonLabel, "SHOP", "CỬA HÀNG");
            SetText(titleLabel, "GEM SHOP", "CỬA HÀNG GEM");
            RefreshGemBalance();
            if (itemNameLabel != null)
            {
                itemNameLabel.text = gift != null
                    ? gift.DisplayName
                    : MiningLocalization.Text("RARE GIFT BOX", "HỘP QUÀ HIẾM");
            }
            if (itemDescriptionLabel != null)
            {
                itemDescriptionLabel.text = gift != null
                    ? gift.Description
                    : MiningLocalization.Text("Spin the wheel for a rare reward.",
                        "Quay vòng quay để nhận phần thưởng hiếm.");
            }
            if (priceLabel != null)
            {
                priceLabel.text = $"{MiningMoneyFormatter.Format(cost)} GEM";
            }
            SetText(buyLabel, "BUY", "MUA");
            RefreshIcon(gift);
            RefreshStatus();

            if (buyRareGiftButton != null)
            {
                buyRareGiftButton.interactable = gift != null && wallet != null &&
                    itemSystem != null;
            }
        }

        private void RefreshStatus()
        {
            if (statusLabel == null)
            {
                return;
            }
            statusLabel.text = purchaseStatus switch
            {
                PurchaseStatus.Purchased => MiningLocalization.Text(
                    "Rare Gift Box added to Inventory!", "Đã thêm Hộp Quà Hiếm vào Túi đồ!"),
                PurchaseStatus.NotEnoughGems => MiningLocalization.Text(
                    "Not enough Gems.", "Không đủ Gem."),
                PurchaseStatus.InventoryFull => MiningLocalization.Text(
                    "Inventory is full.", "Túi đồ đã đầy."),
                PurchaseStatus.MissingProduct => MiningLocalization.Text(
                    "Shop product is not configured.", "Vật phẩm Shop chưa được thiết lập."),
                _ => MiningLocalization.Text(
                    "Purchased gifts appear in Inventory.", "Hộp quà đã mua sẽ vào Túi đồ.")
            };
        }

        private void RefreshIcon(MiningItemData gift)
        {
            if (itemIcon != null)
            {
                itemIcon.sprite = gift != null ? gift.InventoryIcon : null;
                itemIcon.enabled = itemIcon.sprite != null;
            }
            if (itemIconFallback != null)
            {
                itemIconFallback.text = gift != null ? gift.IconFallback : "?";
                itemIconFallback.color = gift != null ? gift.FallbackColor : Color.magenta;
                itemIconFallback.gameObject.SetActive(itemIcon == null || itemIcon.sprite == null);
            }
        }

        private void HandleGemsChanged(float amount)
        {
            gemCounter.SetTarget(amount, gameData);
            Refresh();
        }

        private void RefreshGemBalance()
        {
            if (gemBalanceLabel != null)
            {
                gemBalanceLabel.text = string.Format(MiningLocalization.Text(
                        "GEMS: {0}", "GEM: {0}"),
                    MiningMoneyFormatter.Format(gemCounter.Value));
            }
        }

        private void RemoveListeners()
        {
            gameplayOpenButton?.onClick.RemoveListener(Open);
            closeButton?.onClick.RemoveListener(Close);
            buyRareGiftButton?.onClick.RemoveListener(BuyRareGiftBox);
            MiningLocalization.LanguageChanged -= Refresh;
            if (wallet != null)
            {
                wallet.GemsChanged -= HandleGemsChanged;
            }
            if (itemSystem != null)
            {
                itemSystem.InventoryChanged -= Refresh;
            }
        }

        private void RegisterBaseHud()
        {
            panelCoordinator?.RegisterGemAndShopUi(gemHud,
                gameplayOpenButton != null
                    ? gameplayOpenButton.transform as RectTransform
                    : null);
        }

        private static void SetText(TextMeshProUGUI label, string english, string vietnamese)
        {
            if (label != null)
            {
                label.text = MiningLocalization.Text(english, vietnamese);
            }
        }
    }
}
