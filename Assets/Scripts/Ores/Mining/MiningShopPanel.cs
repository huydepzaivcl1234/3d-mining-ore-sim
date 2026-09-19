using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Shared Gem Shop with a data-driven Lucky Wheel.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningShopPanel : MonoBehaviour
    {
        [System.Serializable]
        private sealed class ShopProductView
        {
            [SerializeField] private GameObject root;
            [SerializeField] private CanvasGroup canvasGroup;
            [SerializeField] private Image icon;
            [SerializeField] private TextMeshProUGUI iconFallback;
            [SerializeField] private TextMeshProUGUI nameLabel;
            [SerializeField] private TextMeshProUGUI descriptionLabel;
            [SerializeField] private TextMeshProUGUI amountLabel;
            [SerializeField] private TextMeshProUGUI priceLabel;
            [SerializeField] private Button buyButton;
            [SerializeField] private TextMeshProUGUI buyLabel;

            private UnityAction buyAction;

            public RectTransform Root => root != null ? root.transform as RectTransform : null;

            public void Bind(int productIndex, System.Action<int> purchase)
            {
                Unbind();
                if (buyButton == null) return;
                buyAction = () => purchase(productIndex);
                buyButton.onClick.AddListener(buyAction);
            }

            public void Unbind()
            {
                if (buyButton != null && buyAction != null)
                {
                    buyButton.onClick.RemoveListener(buyAction);
                }
                buyAction = null;
            }

            public void Refresh(MiningShopProduct product, PlayerWallet wallet,
                MiningItemSystem itemSystem, MiningCosmeticSystem cosmeticSystem, bool shopBusy)
            {
                bool valid = product != null && product.IsValid;
                root?.SetActive(valid);
                if (!valid) return;

                Sprite sprite = product.Icon;
                if (icon != null)
                {
                    icon.sprite = sprite;
                    icon.enabled = sprite != null;
                }
                if (iconFallback != null)
                {
                    iconFallback.text = product.IconFallback;
                    iconFallback.color = product.FallbackColor;
                    iconFallback.gameObject.SetActive(sprite == null);
                }
                if (nameLabel != null) nameLabel.text = product.DisplayName;
                if (descriptionLabel != null) descriptionLabel.text = product.Description;
                if (amountLabel != null) amountLabel.text = product.IsCosmetic ? MiningLocalization.Text("SKIN") : $"x{product.ItemAmount}";
                if (priceLabel != null)
                {
                    priceLabel.text = $"{MiningMoneyFormatter.Format(product.GemCost)} GEM";
                }
                if (buyLabel != null)
                {
                    buyLabel.text = MiningLocalization.Text("BUY");
                }

                bool owned = product.IsCosmetic && cosmeticSystem != null && cosmeticSystem.IsOwned(product.Cosmetic);
                bool hasSpace = product.IsCosmetic ? cosmeticSystem != null : itemSystem != null && itemSystem.CanAddItem(product.Item, product.ItemAmount);
                bool affordable = wallet != null && wallet.CurrentGems >= product.GemCost;
                bool available = !shopBusy && hasSpace && affordable;
                if (buyLabel != null && owned) buyLabel.text = MiningLocalization.Text("EQUIPPED");
                if (buyButton != null) buyButton.interactable = available && !owned;
                // Keep the product art and information at full opacity. Fading the entire row
                // made TMP text and detailed icons look blurred in Play Mode whenever the
                // player lacked Gems. The disabled Buy button already communicates availability.
                if (canvasGroup != null) canvasGroup.alpha = 1f;
            }

            public ShopProductView CloneRuntime()
            {
                if (root == null) return null;
                GameObject clone = UnityEngine.Object.Instantiate(root, root.transform.parent);
                clone.name = $"{root.name} Runtime Cosmetic";
                RectTransform sourceRect = root.transform as RectTransform;
                RectTransform cloneRect = clone.transform as RectTransform;
                if (sourceRect != null && cloneRect != null && root.transform.parent.GetComponent<VerticalLayoutGroup>() == null)
                {
                    cloneRect.anchoredPosition = sourceRect.anchoredPosition + Vector2.down * sourceRect.rect.height;
                }
                return new ShopProductView
                {
                    root = clone,
                    canvasGroup = clone.GetComponent<CanvasGroup>(),
                    icon = FindClone<Image>(icon, clone.transform),
                    iconFallback = FindClone<TextMeshProUGUI>(iconFallback, clone.transform),
                    nameLabel = FindClone<TextMeshProUGUI>(nameLabel, clone.transform),
                    descriptionLabel = FindClone<TextMeshProUGUI>(descriptionLabel, clone.transform),
                    amountLabel = FindClone<TextMeshProUGUI>(amountLabel, clone.transform),
                    priceLabel = FindClone<TextMeshProUGUI>(priceLabel, clone.transform),
                    buyButton = FindClone<Button>(buyButton, clone.transform),
                    buyLabel = FindClone<TextMeshProUGUI>(buyLabel, clone.transform)
                };
            }

            private T FindClone<T>(T source, Transform cloneRoot) where T : Component
            {
                if (source == null || cloneRoot == null) return null;
                // A runtime product row is a full clone, so the matching component has
                // the same component order as it had below the authored source row.
                // Mapping this way is unaffected by nested UI object names and ensures
                // a cosmetic row cannot retain the copied product's title or description.
                T[] sourceComponents = root.GetComponentsInChildren<T>(true);
                T[] clonedComponents = cloneRoot.GetComponentsInChildren<T>(true);
                for (int index = 0; index < sourceComponents.Length && index < clonedComponents.Length; index++)
                {
                    if (sourceComponents[index] == source) return clonedComponents[index];
                }
                return null;
            }
        }

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
        [SerializeField] private MiningAudioManager audioManager;
        private MiningCosmeticSystem cosmeticSystem;

        [Header("Panel")]
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private Button gameplayOpenButton;
        [SerializeField] private RectTransform gemHud;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button buyRareGiftButton;
        [SerializeField] private List<ShopProductView> productViews = new();
        private ScrollRect productScrollRect;

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
        private readonly List<int> rolledRewardIndices = new(10);
        private ShopStatus status;
        private int refundedItemRewards;
        private int currentSpinIndex;
        private float pendingSpinCost;
        private bool rewardsRevealed;
        private bool spinning;
        private string purchasedItemName;
        private Tween spinTween;
        private Sequence resultSequence;

        private void Awake()
        {
            RepairUnsupportedUiGlyphs();
            cosmeticSystem = GetComponent<MiningCosmeticSystem>();
            if (cosmeticSystem == null) cosmeticSystem = gameObject.AddComponent<MiningCosmeticSystem>();
            RegisterBaseHud();
            panelRoot?.gameObject.SetActive(false);
        }

        /// <summary>
        /// The authored Gem_Symbol uses ◆, but the project's LiberationSans SDF has no glyph
        /// or fallback for it. Replace only that unsupported character before the Shop UI starts
        /// rebuilding, preserving all authored RectTransforms and image-based gem icons.
        /// </summary>
        private static void RepairUnsupportedUiGlyphs()
        {
            TMP_Text[] labels = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (TMP_Text label in labels)
            {
                // Text can be null during Awake on an inactive UI object. Do not let
                // glyph cleanup stop Shop initialization.
                string labelText = label != null ? label.text : null;
                if (!string.IsNullOrEmpty(labelText) && labelText.IndexOf('\u25C6') >= 0)
                {
                    label.text = labelText.Replace('\u25C6', '*');
                }
            }
        }

        private void OnEnable()
        {
            productViews ??= new List<ShopProductView>();
            EnsureProductViews();
            StabilizeProductScroll();
            RegisterCosmetics();
            gameData ??= wallet != null ? wallet.GameData : null;
            RegisterBaseHud();
            RemoveListeners();
            gameplayOpenButton?.onClick.AddListener(Open);
            closeButton?.onClick.AddListener(Close);
            if (productViews.Count == 0)
            {
                buyRareGiftButton?.onClick.AddListener(BuyRareGiftBox);
            }
            for (int index = 0; index < productViews.Count; index++)
            {
                productViews[index]?.Bind(index, BuyProduct);
            }
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
            RefundInterruptedSpin();
            spinning = false;
        }

        public void Open()
        {
            if (panelRoot == null) return;
            if (!spinning)
            {
                status = ShopStatus.None;
                rolledRewards.Clear();
                rolledRewardIndices.Clear();
                refundedItemRewards = 0;
                currentSpinIndex = 0;
                rewardsRevealed = false;
                HideResultPanel();
            }
            Refresh();
            panelRoot.SetAsLastSibling();
            if (panelCoordinator != null) panelCoordinator.OpenPanel(panelRoot);
            else panelRoot.gameObject.SetActive(true);
            audioManager?.PlayShopMusic();
            EventSystem.current?.SetSelectedGameObject(spinOnceButton != null
                ? spinOnceButton.gameObject
                : buyRareGiftButton != null ? buyRareGiftButton.gameObject : null);
        }

        public void Close()
        {
            if (spinning) return;
            audioManager?.PlayBackgroundMusic();
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
            purchasedItemName = gift.DisplayName;
            Refresh();
        }

        private void BuyProduct(int productIndex)
        {
            MiningShopProduct product = data != null && productIndex >= 0 &&
                                        productIndex < data.Products.Count
                ? data.Products[productIndex]
                : null;
            if (product == null || !product.IsValid || wallet == null || (!product.IsCosmetic && itemSystem == null))
            {
                status = ShopStatus.MissingProduct;
                Refresh();
                return;
            }
            if (product.IsCosmetic && cosmeticSystem != null && cosmeticSystem.IsOwned(product.Cosmetic))
            {
                cosmeticSystem.Equip(product.Cosmetic);
                status = ShopStatus.Purchased;
                purchasedItemName = product.DisplayName;
                Refresh();
                return;
            }
            if (!product.IsCosmetic && !itemSystem.CanAddItem(product.Item, product.ItemAmount))
            {
                status = ShopStatus.InventoryFull;
                Refresh();
                return;
            }
            if (!wallet.TrySpendGems(product.GemCost))
            {
                status = ShopStatus.NotEnoughGems;
                Refresh();
                return;
            }
            if (product.IsCosmetic)
            {
                if (cosmeticSystem == null || !cosmeticSystem.TryPurchaseAndEquip(product.Cosmetic))
                {
                    wallet.AddGems(product.GemCost);
                    status = ShopStatus.MissingProduct;
                    Refresh();
                    return;
                }
            }
            else if (!itemSystem.TryAddItem(product.Item, product.ItemAmount))
            {
                wallet.AddGems(product.GemCost);
                status = ShopStatus.InventoryFull;
                Refresh();
                return;
            }

            purchasedItemName = product.DisplayName;
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
            rolledRewardIndices.Clear();
            count = Mathf.Max(1, count);
            for (int index = 0; index < count; index++)
            {
                if (!data.TryRollWheelReward(out int rewardIndex,
                        out MiningShopWheelReward reward))
                {
                    rolledRewards.Clear();
                    rolledRewardIndices.Clear();
                    status = ShopStatus.InvalidWheel;
                    Refresh();
                    return;
                }
                rolledRewards.Add(reward);
                rolledRewardIndices.Add(rewardIndex);
            }
            if (!wallet.TrySpendGems(totalCost))
            {
                rolledRewards.Clear();
                rolledRewardIndices.Clear();
                status = ShopStatus.NotEnoughGems;
                Refresh();
                return;
            }

            refundedItemRewards = 0;
            currentSpinIndex = 0;
            pendingSpinCost = Mathf.Max(0f, totalCost);
            rewardsRevealed = false;
            spinning = true;
            status = ShopStatus.Spinning;
            HideResultPanel();
            Refresh();
            if (closeButton != null) closeButton.interactable = false;
            if (wheelRoot == null)
            {
                FinishAllSpinsAndGrantRewards();
                return;
            }

            PlayNextWheelSpin();
        }

        private void PlayNextWheelSpin()
        {
            if (!spinning || currentSpinIndex >= rolledRewardIndices.Count)
            {
                FinishAllSpinsAndGrantRewards();
                return;
            }

            int segmentCount = Mathf.Max(1, data.WheelRewards.Count);
            float step = 360f / segmentCount;
            float startAngle = wheelRoot.localEulerAngles.z;
            // Segments start on their boundary; offset by half a segment so the
            // fixed pointer lands on the center of the selected reward.
            float selectedAngle = Mathf.Repeat(
                -(rolledRewardIndices[currentSpinIndex] + 0.5f) * step, 360f);
            float alignment = Mathf.Repeat(selectedAngle - Mathf.Repeat(startAngle, 360f), 360f);
            float endAngle = startAngle + data.WheelSpinRotations * 360f + alignment;
            RefreshStatus();
            audioManager?.PlayWheelSpinSfx();
            spinTween = Tween.Custom(this, startAngle, endAngle,
                    data.WheelSpinDurationSeconds,
                    static (panel, angle) => panel.wheelRoot.localRotation =
                        Quaternion.Euler(0f, 0f, angle),
                    Ease.OutQuart, useUnscaledTime: true)
                .OnComplete(this, static panel => panel.CompleteCurrentWheelSpin());
        }

        private void CompleteCurrentWheelSpin()
        {
            currentSpinIndex++;
            if (currentSpinIndex < rolledRewardIndices.Count)
            {
                PlayNextWheelSpin();
                return;
            }

            FinishAllSpinsAndGrantRewards();
        }

        private void FinishAllSpinsAndGrantRewards()
        {
            if (!spinning) return;

            float refundPerFailedItem = rolledRewards.Count > 0
                ? pendingSpinCost / rolledRewards.Count
                : pendingSpinCost;
            foreach (MiningShopWheelReward reward in rolledRewards)
            {
                if (!GrantReward(reward))
                {
                    wallet.AddGems(refundPerFailedItem);
                    refundedItemRewards++;
                }
            }

            pendingSpinCost = 0f;
            rewardsRevealed = true;
            CompleteWheelSpin();
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
            audioManager?.PlayWheelRewardSfx();
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
                resultSequence = Sequence.Create(useUnscaledTime: true)
                    .Group(Tween.Scale(wheelResultPanel,
                        Vector3.one * 1.06f, 0.20f, Ease.OutBack))
                    .Group(Tween.Custom(wheelResultGroup, 0f, 1f, 0.16f,
                        static (group, value) => group.alpha = value,
                        Ease.OutQuad))
                    .Chain(Tween.Scale(wheelResultPanel, Vector3.one, 0.10f,
                        Ease.OutQuad));
            }
            else
            {
                resultSequence = Sequence.Create(useUnscaledTime: true)
                    .Group(Tween.Scale(wheelResultPanel,
                        Vector3.one * 1.06f, 0.20f, Ease.OutBack))
                    .Chain(Tween.Scale(wheelResultPanel, Vector3.one, 0.10f,
                        Ease.OutQuad));
            }
        }

        private void HideResultPanel()
        {
            if (resultSequence.isAlive) resultSequence.Stop();
            if (wheelResultPanel == null) return;
            wheelResultPanel.localScale = Vector3.one;
            if (wheelResultGroup != null) wheelResultGroup.alpha = 0f;
            wheelResultPanel.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            MiningItemData gift = data != null ? data.RareGiftBox : null;
            float giftCost = data != null ? data.RareGiftBoxGemCost : 100f;
            SetText(gameplayButtonLabel, "SHOP");
            SetText(titleLabel, "GEM SHOP");
            SetText(wheelTitleLabel, "LUCKY WHEEL");
            RefreshGemBalance();
            if (itemNameLabel != null) itemNameLabel.text = gift != null ? gift.DisplayName :
                MiningLocalization.Text("RARE GIFT BOX");
            if (itemDescriptionLabel != null) itemDescriptionLabel.text = gift != null
                ? gift.Description
                : MiningLocalization.Text("Spin the wheel for a rare reward.");
            if (priceLabel != null)
                priceLabel.text = $"{MiningMoneyFormatter.Format(giftCost)} GEM";
            SetText(buyLabel, "BUY");
            RefreshSpinLabels();
            RefreshIcon(gift);
            RefreshStatus();
            RefreshResults();
            RefreshProductViews();

            bool wheelReady = data != null && data.WheelRewards.Count > 0 &&
                              wallet != null && itemSystem != null && !spinning;
            if (spinOnceButton != null) spinOnceButton.interactable = wheelReady;
            if (spinTenButton != null) spinTenButton.interactable = wheelReady;
            if (productViews.Count == 0 && buyRareGiftButton != null)
                buyRareGiftButton.interactable = gift != null && wallet != null &&
                                                 itemSystem != null && !spinning;
        }

        private void RefreshProductViews()
        {
            for (int index = 0; index < productViews.Count; index++)
            {
                MiningShopProduct product = data != null && index < data.Products.Count
                    ? data.Products[index]
                    : null;
                productViews[index]?.Refresh(product, wallet, itemSystem, cosmeticSystem, spinning);
            }
        }

        private void EnsureProductViews()
        {
            int count = data != null ? data.Products.Count : 0;
            while (productViews.Count < count && productViews.Count > 0)
            {
                ShopProductView clone = productViews[productViews.Count - 1]?.CloneRuntime();
                if (clone == null) break;
                productViews.Add(clone);
            }
        }

        /// <summary>
        /// Keeps the product list responsive without saving any RectTransform or ScrollRect
        /// changes to the scene. Elastic scrolling was fighting the extra runtime cosmetic row:
        /// when the content was shorter than its final row, Unity repeatedly pulled it upward.
        /// </summary>
        private void StabilizeProductScroll()
        {
            if (productViews.Count == 0) return;
            RectTransform firstRow = productViews[0]?.Root;
            RectTransform content = firstRow != null ? firstRow.parent as RectTransform : null;
            if (content == null) return;

            productScrollRect ??= FindProductScrollRect(content);
            if (productScrollRect == null) return;

            float requiredHeight = content.rect.height;
            foreach (ShopProductView view in productViews)
            {
                RectTransform row = view?.Root;
                if (row == null || !row.gameObject.activeSelf) continue;
                requiredHeight = Mathf.Max(requiredHeight, -row.anchoredPosition.y + row.rect.height + 8f);
            }
            content.sizeDelta = new Vector2(content.sizeDelta.x, requiredHeight);

            productScrollRect.movementType = ScrollRect.MovementType.Clamped;
            productScrollRect.inertia = true;
            productScrollRect.decelerationRate = 0.08f;
            productScrollRect.scrollSensitivity = 12f;
            productScrollRect.StopMovement();
        }

        private ScrollRect FindProductScrollRect(RectTransform content)
        {
            foreach (ScrollRect candidate in GetComponentsInChildren<ScrollRect>(true))
            {
                if (candidate != null && candidate.content == content) return candidate;
            }
            return null;
        }

        private void RegisterCosmetics()
        {
            if (data == null || cosmeticSystem == null) return;
            foreach (MiningShopProduct product in data.Products)
                if (product != null && product.Cosmetic != null) cosmeticSystem.Register(product.Cosmetic);
        }

        private void RefreshSpinLabels()
        {
            float one = data != null ? data.SingleSpinGemCost : 10f;
            int count = data != null ? data.MultiSpinCount : 10;
            float many = data != null ? data.MultiSpinGemCost : 100f;
            if (spinOnceLabel != null)
                spinOnceLabel.text = string.Format(MiningLocalization.Text("SPIN 1\n{0} GEM"), MiningMoneyFormatter.Format(one));
            if (spinTenLabel != null)
                spinTenLabel.text = string.Format(MiningLocalization.Text("SPIN {0}\n{1} GEM"), count,
                    MiningMoneyFormatter.Format(many));
        }

        private void RefreshStatus()
        {
            if (statusLabel == null) return;
            statusLabel.text = status switch
            {
                ShopStatus.Purchased => string.Format(MiningLocalization.Text("{0} unlocked!"),
                    string.IsNullOrWhiteSpace(purchasedItemName)
                        ? MiningLocalization.Text("Item")
                        : purchasedItemName),
                ShopStatus.NotEnoughGems => MiningLocalization.Text("Not enough Gems."),
                ShopStatus.InventoryFull => MiningLocalization.Text("Inventory is full."),
                ShopStatus.MissingProduct => MiningLocalization.Text("Shop product is not configured."),
                ShopStatus.InvalidWheel => MiningLocalization.Text("Add at least one valid Wheel Reward in MiningShopData."),
                ShopStatus.Spinning => string.Format(MiningLocalization.Text("Spinning {0}/{1}..."),
                    Mathf.Min(currentSpinIndex + 1, rolledRewards.Count),
                    rolledRewards.Count),
                ShopStatus.WheelComplete when refundedItemRewards > 0 => string.Format(
                    MiningLocalization.Text("Done. {0} item reward(s) could not fit and were refunded."),
                    refundedItemRewards),
                ShopStatus.WheelComplete => MiningLocalization.Text("Rewards received!"),
                _ => string.Empty
            };
        }

        private void RefreshResults()
        {
            if (wheelResultsLabel == null) return;
            if (!rewardsRevealed || rolledRewards.Count == 0)
            {
                wheelResultsLabel.text = MiningLocalization.Text("Your rewards appear here.");
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
                gemBalanceLabel.text = string.Format(MiningLocalization.Text("GEMS: {0}"), MiningMoneyFormatter.Format(gemCounter.Value));
        }

        private void RemoveListeners()
        {
            gameplayOpenButton?.onClick.RemoveListener(Open);
            closeButton?.onClick.RemoveListener(Close);
            buyRareGiftButton?.onClick.RemoveListener(BuyRareGiftBox);
            foreach (ShopProductView productView in productViews)
            {
                productView?.Unbind();
            }
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

        private void RefundInterruptedSpin()
        {
            if (!spinning || pendingSpinCost <= 0f || wallet == null) return;
            wallet.AddGems(pendingSpinCost);
            pendingSpinCost = 0f;
            rolledRewards.Clear();
            rolledRewardIndices.Clear();
            currentSpinIndex = 0;
            rewardsRevealed = false;
            status = ShopStatus.None;
        }

        private static void SetText(TextMeshProUGUI label, string english)
        {
            if (label != null) label.text = MiningLocalization.Text(english);
        }
    }
}
