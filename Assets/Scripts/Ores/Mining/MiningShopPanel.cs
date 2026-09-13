using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Shared Gem Shop with a data-driven Lucky Wheel.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningShopPanel : MonoBehaviour
    {
        private enum ShopStatus
        {
            None, Purchased, NotEnoughGems, InventoryFull, MissingProduct,
            InvalidWheel, Spinning, WheelComplete
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

        [Header("Lucky Wheel")]
        [SerializeField] private RectTransform wheelRoot;
        [SerializeField] private Button spinOnceButton;
        [SerializeField] private Button spinTenButton;
        [SerializeField] private RectTransform wheelResultPanel;
        [SerializeField] private CanvasGroup wheelResultGroup;
        [SerializeField] private TextMeshProUGUI wheelTitleLabel;
        [SerializeField] private TextMeshProUGUI spinOnceLabel;
        [SerializeField] private TextMeshProUGUI spinTenLabel;
        [SerializeField] private TextMeshProUGUI wheelResultsLabel;

        [Header("Shop labels")]
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

        private readonly MiningAnimatedCurrencyValue gemCounter = new();
        private readonly List<MiningShopWheelReward> rolledRewards = new(10);
        private ShopStatus status;
        private int refundedItemRewards;
        private bool spinning;
        private Tween spinTween;
        private Sequence resultSequence;

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
            spinOnceButton?.onClick.AddListener(SpinOnce);
            spinTenButton?.onClick.AddListener(SpinTen);
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
            if (spinTween.isAlive) spinTween.Stop();
            if (resultSequence.isAlive) resultSequence.Stop();
            spinning = false;
        }

        public void Open()
        {
            if (panelRoot == null) return;
            status = ShopStatus.None;
            rolledRewards.Clear();
            refundedItemRewards = 0;
            Refresh();
            panelRoot.SetAsLastSibling();
            if (panelCoordinator != null) panelCoordinator.OpenPanel(panelRoot);
            else panelRoot.gameObject.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(spinOnceButton != null
                ? spinOnceButton.gameObject
                : buyRareGiftButton != null ? buyRareGiftButton.gameObject : null);
        }

        public void Close()
        {
            if (spinning) return;
            if (panelCoordinator != null) panelCoordinator.ClosePanel(panelRoot);
            else panelRoot?.gameObject.SetActive(false);
        }

        public void BuyRareGiftBox()
        {
            MiningItemData gift = data != null ? data.RareGiftBox : null;
            float cost = data != null ? data.RareGiftBoxGemCost : 0f;
            if (gift == null || wallet == null || itemSystem == null)
            {
                status = ShopStatus.MissingProduct;
                Refresh();
                return;
            }
            if (!itemSystem.CanAddItem(gift))
            {
                status = ShopStatus.InventoryFull;
                Refresh();
                return;
            }
            if (!wallet.TrySpendGems(cost))
            {
                status = ShopStatus.NotEnoughGems;
                Refresh();
                return;
            }
            if (!itemSystem.TryAddItem(gift))
            {
                wallet.AddGems(cost);
                status = ShopStatus.InventoryFull;
                Refresh();
                return;
            }
            status = ShopStatus.Purchased;
            Refresh();
        }

        public void SpinOnce() => StartWheelSpin(1,
            data != null ? data.SingleSpinGemCost : 10f);

        public void SpinTen() => StartWheelSpin(data != null ? data.MultiSpinCount : 10,
            data != null ? data.MultiSpinGemCost : 100f);

        private void StartWheelSpin(int count, float totalCost)
        {
            if (spinning || data == null || wallet == null || itemSystem == null)
            {
                status = ShopStatus.InvalidWheel;
                Refresh();
                return;
            }

            rolledRewards.Clear();
            int finalRewardIndex = -1;
            count = Mathf.Max(1, count);
            for (int index = 0; index < count; index++)
            {
                if (!data.TryRollWheelReward(out int rewardIndex,
                        out MiningShopWheelReward reward))
                {
                    rolledRewards.Clear();
                    status = ShopStatus.InvalidWheel;
                    Refresh();
                    return;
                }
                rolledRewards.Add(reward);
                finalRewardIndex = rewardIndex;
            }
            if (!wallet.TrySpendGems(totalCost))
            {
                rolledRewards.Clear();
                status = ShopStatus.NotEnoughGems;
                Refresh();
                return;
            }

            // Grant first: quitting or unloading during the animation can never eat a paid spin.
            refundedItemRewards = 0;
            float refundPerFailedItem = totalCost / count;
            foreach (MiningShopWheelReward reward in rolledRewards)
            {
                if (!GrantReward(reward))
                {
                    wallet.AddGems(refundPerFailedItem);
                    refundedItemRewards++;
                }
            }

            spinning = true;
            status = ShopStatus.Spinning;
            Refresh();
            if (closeButton != null) closeButton.interactable = false;
            if (wheelRoot == null)
            {
                CompleteWheelSpin();
                return;
            }

            int segmentCount = Mathf.Max(1, data.WheelRewards.Count);
            float step = 360f / segmentCount;
            float startAngle = wheelRoot.localEulerAngles.z;
            float selectedAngle = Mathf.Repeat(-finalRewardIndex * step, 360f);
            float alignment = Mathf.Repeat(selectedAngle - Mathf.Repeat(startAngle, 360f), 360f);
            float endAngle = startAngle + data.WheelSpinRotations * 360f + alignment;
            spinTween = Tween.Custom(this, startAngle, endAngle,
                    data.WheelSpinDurationSeconds,
                    static (panel, angle) => panel.wheelRoot.localRotation =
                        Quaternion.Euler(0f, 0f, angle),
                    Ease.OutQuart, useUnscaledTime: true)
                .OnComplete(this, static panel => panel.CompleteWheelSpin());
        }

        private bool GrantReward(MiningShopWheelReward reward)
        {
            if (reward == null || !reward.IsValid) return false;
            switch (reward.RewardType)
            {
                case MiningShopWheelRewardType.Money:
                    wallet.AddMoney(reward.CurrencyAmount);
                    return true;
                case MiningShopWheelRewardType.Gems:
                    wallet.AddGems(reward.CurrencyAmount);
                    return true;
                case MiningShopWheelRewardType.Item:
                    return itemSystem.TryAddItem(reward.Item, reward.ItemAmount);
                default:
                    return false;
            }
        }

        private void CompleteWheelSpin()
        {
            spinning = false;
            status = ShopStatus.WheelComplete;
            Refresh();
            if (closeButton != null) closeButton.interactable = true;
            ShowResultPanel();
            EventSystem.current?.SetSelectedGameObject(spinOnceButton != null
                ? spinOnceButton.gameObject : null);
        }

        private void ShowResultPanel()
        {
            if (wheelResultPanel == null) return;
            if (resultSequence.isAlive) resultSequence.Stop();
            wheelResultPanel.gameObject.SetActive(true);
            wheelResultPanel.localScale = Vector3.one * 0.72f;
            if (wheelResultGroup != null) wheelResultGroup.alpha = 0f;
            if (wheelResultGroup != null)
            {
                resultSequence = Sequence.Create(Tween.Scale(wheelResultPanel,
                        Vector3.one * 1.06f, 0.20f, Ease.OutBack, useUnscaledTime: true))
                    .Group(Tween.Custom(wheelResultGroup, 0f, 1f, 0.16f,
                        static (group, value) => group.alpha = value,
                        Ease.OutQuad, useUnscaledTime: true))
                    .Chain(Tween.Scale(wheelResultPanel, Vector3.one, 0.10f,
                        Ease.OutQuad, useUnscaledTime: true));
            }
            else
            {
                resultSequence = Sequence.Create(Tween.Scale(wheelResultPanel,
                        Vector3.one * 1.06f, 0.20f, Ease.OutBack, useUnscaledTime: true))
                    .Chain(Tween.Scale(wheelResultPanel, Vector3.one, 0.10f,
                        Ease.OutQuad, useUnscaledTime: true));
            }
        }

        private void Refresh()
        {
            MiningItemData gift = data != null ? data.RareGiftBox : null;
            float giftCost = data != null ? data.RareGiftBoxGemCost : 100f;
            SetText(gameplayButtonLabel, "SHOP", "CỬA HÀNG");
            SetText(titleLabel, "GEM SHOP", "CỬA HÀNG GEM");
            SetText(wheelTitleLabel, "LUCKY WHEEL", "VÒNG QUAY MAY MẮN");
            RefreshGemBalance();
            if (itemNameLabel != null) itemNameLabel.text = gift != null ? gift.DisplayName :
                MiningLocalization.Text("RARE GIFT BOX", "HỘP QUÀ HIẾM");
            if (itemDescriptionLabel != null) itemDescriptionLabel.text = gift != null
                ? gift.Description
                : MiningLocalization.Text("Spin the wheel for a rare reward.",
                    "Quay vòng quay để nhận phần thưởng hiếm.");
            if (priceLabel != null)
                priceLabel.text = $"{MiningMoneyFormatter.Format(giftCost)} GEM";
            SetText(buyLabel, "BUY", "MUA");
            RefreshSpinLabels();
            RefreshIcon(gift);
            RefreshStatus();
            RefreshResults();

            bool wheelReady = data != null && data.WheelRewards.Count > 0 &&
                              wallet != null && itemSystem != null && !spinning;
            if (spinOnceButton != null) spinOnceButton.interactable = wheelReady;
            if (spinTenButton != null) spinTenButton.interactable = wheelReady;
            if (buyRareGiftButton != null)
                buyRareGiftButton.interactable = gift != null && wallet != null &&
                                                 itemSystem != null && !spinning;
        }

        private void RefreshSpinLabels()
        {
            float one = data != null ? data.SingleSpinGemCost : 10f;
            int count = data != null ? data.MultiSpinCount : 10;
            float many = data != null ? data.MultiSpinGemCost : 100f;
            if (spinOnceLabel != null)
                spinOnceLabel.text = string.Format(MiningLocalization.Text(
                    "SPIN 1\n{0} GEM", "QUAY 1\n{0} GEM"), MiningMoneyFormatter.Format(one));
            if (spinTenLabel != null)
                spinTenLabel.text = string.Format(MiningLocalization.Text(
                    "SPIN {0}\n{1} GEM", "QUAY {0}\n{1} GEM"), count,
                    MiningMoneyFormatter.Format(many));
        }

        private void RefreshStatus()
        {
            if (statusLabel == null) return;
            statusLabel.text = status switch
            {
                ShopStatus.Purchased => MiningLocalization.Text(
                    "Rare Gift Box added to Inventory!", "Đã thêm Hộp Quà Hiếm vào Túi đồ!"),
                ShopStatus.NotEnoughGems => MiningLocalization.Text(
                    "Not enough Gems.", "Không đủ Gem."),
                ShopStatus.InventoryFull => MiningLocalization.Text(
                    "Inventory is full.", "Túi đồ đã đầy."),
                ShopStatus.MissingProduct => MiningLocalization.Text(
                    "Shop product is not configured.", "Vật phẩm Shop chưa được thiết lập."),
                ShopStatus.InvalidWheel => MiningLocalization.Text(
                    "Add at least one valid Wheel Reward in MiningShopData.",
                    "Hãy thêm ít nhất một phần thưởng hợp lệ trong MiningShopData."),
                ShopStatus.Spinning => MiningLocalization.Text("Spinning...", "Đang quay..."),
                ShopStatus.WheelComplete when refundedItemRewards > 0 => string.Format(
                    MiningLocalization.Text(
                        "Done. {0} item reward(s) could not fit and were refunded.",
                        "Đã xong. {0} vật phẩm không đủ chỗ và đã được hoàn Gem."),
                    refundedItemRewards),
                ShopStatus.WheelComplete => MiningLocalization.Text(
                    "Rewards received!", "Đã nhận phần thưởng!"),
                _ => string.Empty
            };
        }

        private void RefreshResults()
        {
            if (wheelResultsLabel == null) return;
            if (rolledRewards.Count == 0)
            {
                wheelResultsLabel.text = MiningLocalization.Text(
                    "Your rewards appear here.", "Phần thưởng sẽ hiện ở đây.");
                return;
            }
            System.Text.StringBuilder builder = new();
            for (int index = 0; index < rolledRewards.Count; index++)
            {
                if (index > 0) builder.Append('\n');
                builder.Append(index + 1).Append(". ")
                    .Append(rolledRewards[index].GetDisplayName());
            }
            wheelResultsLabel.text = builder.ToString();
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
                gemBalanceLabel.text = string.Format(MiningLocalization.Text(
                    "GEMS: {0}", "GEM: {0}"), MiningMoneyFormatter.Format(gemCounter.Value));
        }

        private void RemoveListeners()
        {
            gameplayOpenButton?.onClick.RemoveListener(Open);
            closeButton?.onClick.RemoveListener(Close);
            buyRareGiftButton?.onClick.RemoveListener(BuyRareGiftBox);
            spinOnceButton?.onClick.RemoveListener(SpinOnce);
            spinTenButton?.onClick.RemoveListener(SpinTen);
            MiningLocalization.LanguageChanged -= Refresh;
            if (wallet != null) wallet.GemsChanged -= HandleGemsChanged;
            if (itemSystem != null) itemSystem.InventoryChanged -= Refresh;
        }

        private void RegisterBaseHud()
        {
            panelCoordinator?.RegisterGemAndShopUi(gemHud,
                gameplayOpenButton != null ? gameplayOpenButton.transform as RectTransform : null);
        }

        private static void SetText(TextMeshProUGUI label, string english, string vietnamese)
        {
            if (label != null) label.text = MiningLocalization.Text(english, vietnamese);
        }
    }
}
