using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Runtime presentation for the wandering trader's rotating offers.</summary>
    public sealed class WanderingTraderPanel : MonoBehaviour
    {
        private const string RuntimePanelName = "Wandering Trader Runtime Panel";
        private static readonly Color Leather = new(0.23f, 0.09f, 0.025f, 0.99f);
        private static readonly Color HeaderLeather = new(0.36f, 0.16f, 0.055f, 1f);
        private static readonly Color DarkWell = new(0.035f, 0.009f, 0.002f, 1f);
        private static readonly Color Gold = new(0.79f, 0.49f, 0.13f, 1f);
        private static readonly Color PaleGold = new(1f, 0.86f, 0.48f, 1f);
        private static readonly Color Green = new(0.08f, 0.48f, 0.16f, 1f);
        private static readonly Color DisabledGreen = new(0.18f, 0.22f, 0.16f, 1f);

        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text offerText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button tradeButton;
        [SerializeField] private Button closeButton;

        private TMP_Text subtitleText;
        private TMP_Text balanceText;
        private Button headerCloseButton;
        private RectTransform cardRect;
        private PlayerWallet wallet;
        private MiningItemSystem itemSystem;
        private Action closedAction;
        private Func<WanderingTraderSystem.TraderOffer, bool> canAcceptOfferAction;
        private Func<WanderingTraderSystem.TraderOffer, bool> acceptOfferAction;
        private readonly List<OfferRowView> offerRows = new();
        private float resetAt;
        private bool isOpen;
        private bool sourcesSubscribed;

        public bool IsOpen => isOpen;

        private sealed class OfferRowView
        {
            public WanderingTraderSystem.TraderOffer Offer;
            public GameObject Root;
            public Button TradeButton;
            public Image TradeBackground;
            public TMP_Text TradeLabel;
        }

        private void Awake()
        {
            ApplyShopStyle();
            BindButtons();
        }

        private void OnEnable()
        {
            BindButtons();
            FindRuntimeSources();
            SubscribeSources();
        }

        private void OnDisable() => UnsubscribeSources();

        public static WanderingTraderPanel EnsureRuntime()
        {
            WanderingTraderPanel existing = FindFirstObjectByType<WanderingTraderPanel>(
                FindObjectsInactive.Include);
            if (existing != null) return existing;
            Canvas canvas = FindHudCanvas();
            if (canvas == null) return null;

            GameObject root = CreateUiObject(RuntimePanelName, canvas.transform);
            Stretch(root.GetComponent<RectTransform>());
            Image backdrop = root.AddComponent<Image>();
            backdrop.color = new Color(0.015f, 0.005f, 0.02f, 0.78f);
            Button backdropButton = root.AddComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            WanderingTraderPanel panel = root.AddComponent<WanderingTraderPanel>();
            panel.Build(root.transform);
            backdropButton.onClick.AddListener(panel.Hide);
            root.SetActive(false);
            return panel;
        }

        public void Show(IReadOnlyList<WanderingTraderSystem.TraderOffer> offers,
            float secondsRemaining,
            Func<WanderingTraderSystem.TraderOffer, bool> canAccept,
            Func<WanderingTraderSystem.TraderOffer, bool> onAccept,
            Action onClosed)
        {
            if (offers == null || offers.Count == 0) return;

            ApplyShopStyle();
            ClearOfferRows();
            canAcceptOfferAction = canAccept;
            acceptOfferAction = onAccept;
            closedAction = onClosed;
            resetAt = Time.unscaledTime + Mathf.Max(0f, secondsRemaining);
            isOpen = true;

            if (offerText != null) offerText.gameObject.SetActive(false);
            if (tradeButton != null) tradeButton.gameObject.SetActive(false);

            Transform rowsParent = cardRect != null ? cardRect : transform;
            for (int index = 0; index < Mathf.Min(3, offers.Count); index++)
                CreateOfferRow(rowsParent, offers[index], index);

            FindRuntimeSources();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            SubscribeSources();
            RefreshBalance();
            RefreshOfferButtons();
            UpdateCountdown();
        }

        public void Hide()
        {
            if (!isOpen) return;
            isOpen = false;
            ClearOfferRows();
            if (offerText != null) offerText.gameObject.SetActive(true);
            if (tradeButton != null) tradeButton.gameObject.SetActive(true);

            Action callback = closedAction;
            closedAction = null;
            canAcceptOfferAction = null;
            acceptOfferAction = null;
            gameObject.SetActive(false);
            callback?.Invoke();
        }

        private void Update()
        {
            if (isOpen) UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            if (feedbackText == null) return;
            int remaining = Mathf.CeilToInt(Mathf.Max(0f, resetAt - Time.unscaledTime));
            feedbackText.text = $"ĐỔI HÀNG SAU  {remaining / 60:00}:{remaining % 60:00}";
        }

        private void CreateOfferRow(Transform parent, WanderingTraderSystem.TraderOffer offer, int index)
        {
            GameObject row = CreateUiObject($"Trader Offer {index + 1}", parent);
            ConfigureCenteredRect(row.GetComponent<RectTransform>(),
                new Vector2(0f, 96f - index * 92f), new Vector2(556f, 80f));
            Image rowImage = row.AddComponent<Image>();
            rowImage.color = index == 2
                ? new Color(0.09f, 0.015f, 0.12f, 1f)
                : new Color(0.08f, 0.02f, 0.006f, 1f);
            AddOutline(row, index == 2 ? new Color(0.52f, 0.17f, 0.68f) :
                new Color(0.31f, 0.13f, 0.035f), new Vector2(2f, -2f));

            CreateOfferWell(row.transform, "Give Well", new Vector2(-190f, 0f),
                new Vector2(160f, 52f), "ĐƯA",
                offer.TraderSellsItem ? "TIỀN" : offer.Item.DisplayName,
                offer.TraderSellsItem ? MiningMoneyFormatter.Format(offer.RewardAmount) :
                    $"x{offer.ItemAmount}",
                offer.TraderSellsItem ? null : GetItemIcon(offer.Item),
                offer.TraderSellsItem ? "$" : GetItemFallback(offer.Item),
                new Color(0.45f, 0.19f, 0.055f));

            TMP_Text arrow = CreateLabel("Exchange Arrow", row.transform, "➜", 25f,
                new Vector2(-80f, 0f), new Vector2(42f, 42f), PaleGold);
            arrow.fontStyle = FontStyles.Bold;

            CreateOfferWell(row.transform, "Receive Well", new Vector2(34f, 0f),
                new Vector2(180f, 52f), "NHẬN",
                offer.TraderSellsItem ? offer.Item.DisplayName :
                    (offer.PaysGems ? "GEM" : "TIỀN"),
                offer.TraderSellsItem ? $"x{offer.ItemAmount}" : FormatCurrencyAmount(offer),
                offer.TraderSellsItem ? GetItemIcon(offer.Item) : null,
                offer.TraderSellsItem ? GetItemFallback(offer.Item) :
                    (offer.PaysGems ? "♦" : "$"), Gold);

            Button trade = CreateButton("Trade Button", row.transform, "GIAO DỊCH",
                new Vector2(204f, 0f), new Vector2(120f, 40f), Green, 14f);
            Image tradeImage = trade.GetComponent<Image>();
            TMP_Text tradeLabel = trade.GetComponentInChildren<TMP_Text>();
            trade.onClick.AddListener(() => TryAccept(offer));
            offerRows.Add(new OfferRowView
            {
                Offer = offer,
                Root = row,
                TradeButton = trade,
                TradeBackground = tradeImage,
                TradeLabel = tradeLabel
            });
        }

        private void TryAccept(WanderingTraderSystem.TraderOffer offer)
        {
            if (acceptOfferAction != null && acceptOfferAction(offer))
            {
                Hide();
                return;
            }

            if (feedbackText != null) feedbackText.text = "KHÔNG ĐỦ VẬT PHẨM HOẶC TIỀN";
            RefreshBalance();
            RefreshOfferButtons();
        }

        private static void CreateOfferWell(Transform parent, string objectName, Vector2 position,
            Vector2 size, string heading, string itemName, string amount, Sprite icon,
            string fallback, Color border)
        {
            GameObject well = CreateUiObject(objectName, parent);
            RectTransform rect = well.GetComponent<RectTransform>();
            ConfigureCenteredRect(rect, position, size);
            Image background = well.AddComponent<Image>();
            background.color = DarkWell;
            AddOutline(well, border, new Vector2(1.5f, -1.5f));

            GameObject iconObject = CreateUiObject("Icon", well.transform);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(8f, 0f);
            iconRect.sizeDelta = new Vector2(40f, 40f);
            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.color = icon != null ? Color.white : new Color(1f, 0.78f, 0.25f);
            iconImage.preserveAspect = true;
            if (icon == null)
            {
                TMP_Text fallbackText = CreateLabel("Fallback", iconObject.transform,
                    string.IsNullOrWhiteSpace(fallback) ? "?" : fallback, 25f,
                    Vector2.zero, iconRect.sizeDelta, Color.white);
                fallbackText.fontStyle = FontStyles.Bold;
            }

            float textWidth = Mathf.Max(70f, size.x - 58f);
            TMP_Text headingText = CreateLabel("Heading", well.transform, heading, 9.5f,
                new Vector2(23f, 12f), new Vector2(textWidth, 18f),
                new Color(0.68f, 0.48f, 0.28f));
            headingText.alignment = TextAlignmentOptions.Left;
            headingText.fontStyle = FontStyles.Bold;
            TMP_Text value = CreateLabel("Value", well.transform, $"{itemName} {amount}", 13f,
                new Vector2(23f, -8f), new Vector2(textWidth, 24f), Color.white);
            value.alignment = TextAlignmentOptions.Left;
            value.fontStyle = FontStyles.Bold;
            value.enableAutoSizing = true;
            value.fontSizeMin = 9f;
            value.fontSizeMax = 13f;
        }

        private static string FormatCurrencyAmount(WanderingTraderSystem.TraderOffer offer) =>
            offer.PaysGems ? offer.RewardAmount.ToString("0") :
                MiningMoneyFormatter.Format(offer.RewardAmount);

        private static Sprite GetItemIcon(MiningItemData item) => item != null ? item.InventoryIcon : null;
        private static string GetItemFallback(MiningItemData item) => item != null ? item.IconFallback : "?";

        private void RefreshOfferButtons()
        {
            foreach (OfferRowView view in offerRows)
            {
                if (view?.TradeButton == null) continue;
                bool canTrade = canAcceptOfferAction == null || canAcceptOfferAction(view.Offer);
                view.TradeButton.interactable = canTrade;
                if (view.TradeBackground != null)
                    view.TradeBackground.color = canTrade ? Green : DisabledGreen;
                if (view.TradeLabel != null)
                    view.TradeLabel.color = canTrade ? Color.white :
                        new Color(0.62f, 0.62f, 0.58f, 1f);
            }
        }

        private void RefreshBalance()
        {
            if (balanceText == null) return;
            float money = wallet != null ? wallet.CurrentMoney : 0f;
            float gems = wallet != null ? wallet.CurrentGems : 0f;
            balanceText.text = $"$ {MiningMoneyFormatter.Format(money)}    |    ♦ {MiningMoneyFormatter.Format(gems)}";
        }

        private void FindRuntimeSources()
        {
            wallet ??= FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            itemSystem ??= FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
        }

        private void SubscribeSources()
        {
            if (sourcesSubscribed) return;
            if (wallet != null)
            {
                wallet.MoneyChanged += HandleBalanceChanged;
                wallet.GemsChanged += HandleBalanceChanged;
            }
            if (itemSystem != null) itemSystem.InventoryChanged += HandleInventoryChanged;
            sourcesSubscribed = true;
        }

        private void UnsubscribeSources()
        {
            if (!sourcesSubscribed) return;
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleBalanceChanged;
                wallet.GemsChanged -= HandleBalanceChanged;
            }
            if (itemSystem != null) itemSystem.InventoryChanged -= HandleInventoryChanged;
            sourcesSubscribed = false;
        }

        private void HandleBalanceChanged(float value)
        {
            RefreshBalance();
            RefreshOfferButtons();
        }

        private void HandleInventoryChanged() => RefreshOfferButtons();

        private void ClearOfferRows()
        {
            for (int index = offerRows.Count - 1; index >= 0; index--)
                if (offerRows[index]?.Root != null) Destroy(offerRows[index].Root);
            offerRows.Clear();
        }

        private void Build(Transform parent)
        {
            GameObject card = CreateUiObject("Trade Card", parent);
            cardRect = card.GetComponent<RectTransform>();
            ConfigureCenteredRect(cardRect, Vector2.zero, new Vector2(620f, 540f));
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = Leather;
            titleText = CreateLabel("Title", card.transform, "THƯƠNG NHÂN LANG THANG", 20f,
                new Vector2(0f, 231f), new Vector2(430f, 30f), new Color(1f, 0.95f, 0.8f));
            offerText = CreateLabel("Offer", card.transform, string.Empty, 20f, Vector2.zero,
                new Vector2(530f, 180f), Color.white);
            feedbackText = CreateLabel("Countdown", card.transform, string.Empty, 12f,
                new Vector2(-168f, 164f), new Vector2(220f, 28f), new Color(1f, 0.65f, 0.28f));
            closeButton = CreateButton("Close Button", card.transform, "ĐÓNG",
                new Vector2(0f, -220f), new Vector2(300f, 46f),
                new Color(0.49f, 0.09f, 0.025f), 17f);
            ApplyShopStyle();
            BindButtons();
        }

        public void ConfigureSceneUi(TMP_Text title, TMP_Text offer, TMP_Text feedback,
            Button trade, Button close)
        {
            titleText = title;
            offerText = offer;
            feedbackText = feedback;
            tradeButton = trade;
            closeButton = close;
            ApplyShopStyle();
            BindButtons();
        }

        private void BindButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }
            if (headerCloseButton != null)
            {
                headerCloseButton.onClick.RemoveListener(Hide);
                headerCloseButton.onClick.AddListener(Hide);
            }
        }

        private void ApplyShopStyle()
        {
            Image backdrop = GetComponent<Image>();
            if (backdrop != null) backdrop.color = new Color(0.015f, 0.005f, 0.02f, 0.78f);

            Transform card = transform.Find("Offer Card") ?? transform.Find("Trade Card");
            if (card == null) return;
            cardRect = card.GetComponent<RectTransform>();
            ConfigureCenteredRect(cardRect, Vector2.zero, new Vector2(620f, 540f));
            GetOrAdd<Image>(card.gameObject).color = Leather;
            AddOutline(card.gameObject, Gold, new Vector2(3f, -3f));

            RectTransform header = EnsureImage("Header Band", card, HeaderLeather,
                new Vector2(0f, 237f), new Vector2(598f, 66f));
            AddOutline(header.gameObject, new Color(0.55f, 0.29f, 0.08f), new Vector2(2f, -2f));
            header.SetAsFirstSibling();
            RectTransform innerFrame = EnsureImage("Inner Frame", card, Color.clear,
                Vector2.zero, new Vector2(588f, 508f));
            AddOutline(innerFrame.gameObject, new Color(0.92f, 0.69f, 0.45f, 0.35f),
                new Vector2(1f, -1f));
            innerFrame.SetAsFirstSibling();

            CreateCornerRivet(card, "Rivet TL", new Vector2(-286f, 246f));
            CreateCornerRivet(card, "Rivet TR", new Vector2(286f, 246f));
            CreateCornerRivet(card, "Rivet BL", new Vector2(-286f, -246f));
            CreateCornerRivet(card, "Rivet BR", new Vector2(286f, -246f));

            if (titleText != null)
            {
                ConfigureCenteredRect(titleText.rectTransform, new Vector2(0f, 239f),
                    new Vector2(440f, 28f));
                titleText.text = "THƯƠNG NHÂN LANG THANG";
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.fontSize = 20f;
                titleText.fontStyle = FontStyles.Bold;
                titleText.color = new Color(1f, 0.95f, 0.8f);
            }

            subtitleText = EnsureLabel("Subtitle", card, "WANDERING TRADER OFFERS", 10f,
                new Vector2(0f, 218f), new Vector2(400f, 20f),
                new Color(0.86f, 0.64f, 0.39f));
            subtitleText.fontStyle = FontStyles.Bold;
            RectTransform lantern = EnsureImage("Lantern Badge", card,
                new Color(0.08f, 0.022f, 0.005f), new Vector2(-270f, 237f),
                new Vector2(42f, 42f));
            AddOutline(lantern.gameObject, PaleGold, new Vector2(2f, -2f));
            TMP_Text lanternText = EnsureLabel("Lantern", lantern, "✦", 24f, Vector2.zero,
                new Vector2(42f, 42f), new Color(1f, 0.68f, 0.16f));
            lanternText.fontStyle = FontStyles.Bold;

            if (feedbackText != null)
            {
                RectTransform timerPill = EnsureImage("Timer Pill", card,
                    new Color(0.06f, 0.012f, 0.002f, 0.95f),
                    new Vector2(-168f, 164f), new Vector2(220f, 28f));
                AddOutline(timerPill.gameObject, new Color(0.55f, 0.28f, 0.08f),
                    new Vector2(1.5f, -1.5f));
                ConfigureCenteredRect(feedbackText.rectTransform, new Vector2(-168f, 164f),
                    new Vector2(220f, 28f));
                feedbackText.alignment = TextAlignmentOptions.Center;
                feedbackText.fontSize = 12f;
                feedbackText.fontStyle = FontStyles.Bold;
                feedbackText.color = new Color(1f, 0.65f, 0.28f);
                feedbackText.transform.SetAsLastSibling();
            }

            RectTransform balancePill = EnsureImage("Balance Pill", card,
                new Color(0.06f, 0.012f, 0.002f, 0.95f),
                new Vector2(162f, 164f), new Vector2(256f, 28f));
            AddOutline(balancePill.gameObject, Gold, new Vector2(1.5f, -1.5f));
            balanceText = EnsureLabel("Player Balance", card, "$ 0    |    ♦ 0", 12f,
                new Vector2(162f, 164f), new Vector2(256f, 28f), PaleGold);
            balanceText.fontStyle = FontStyles.Bold;
            balanceText.transform.SetAsLastSibling();

            TMP_Text note = EnsureLabel("Stock Note", card,
                "MỖI MẶT HÀNG CHỈ CÓ SỐ LƯỢNG GIỚI HẠN", 10f,
                new Vector2(0f, -139f), new Vector2(520f, 24f),
                new Color(0.78f, 0.55f, 0.32f));
            note.fontStyle = FontStyles.Bold;

            if (closeButton != null)
            {
                ConfigureCenteredRect(closeButton.GetComponent<RectTransform>(),
                    new Vector2(0f, -220f), new Vector2(300f, 46f));
                GetOrAdd<Image>(closeButton.gameObject).color = new Color(0.49f, 0.09f, 0.025f);
                AddOutline(closeButton.gameObject, new Color(0.9f, 0.3f, 0.12f),
                    new Vector2(2f, -2f));
                TMP_Text label = closeButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = "ĐÓNG";
                    label.fontSize = 17f;
                    label.fontStyle = FontStyles.Bold;
                }
            }

            Transform headerClose = card.Find("Header Close Button");
            headerCloseButton = headerClose == null
                ? CreateButton("Header Close Button", card, "×", new Vector2(276f, 237f),
                    new Vector2(34f, 34f), new Color(0.16f, 0.035f, 0.008f), 22f)
                : GetOrAdd<Button>(headerClose.gameObject);
            AddOutline(headerCloseButton.gameObject, Gold, new Vector2(1.5f, -1.5f));
            headerCloseButton.transform.SetAsLastSibling();
            if (titleText != null) titleText.transform.SetAsLastSibling();
            if (subtitleText != null) subtitleText.transform.SetAsLastSibling();
            BindButtons();
        }

        private static void CreateCornerRivet(Transform parent, string name, Vector2 position)
        {
            RectTransform rivet = EnsureImage(name, parent, new Color(0.86f, 0.62f, 0.18f),
                position, new Vector2(8f, 8f));
            AddOutline(rivet.gameObject, new Color(0.25f, 0.12f, 0.01f), new Vector2(1f, -1f));
        }

        private static Canvas FindHudCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Canvas candidate in canvases)
                if (candidate.name.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0)
                    return candidate;
            return canvases.Length > 0 ? canvases[0] : null;
        }

        private static GameObject CreateUiObject(string objectName, Transform parent)
        {
            GameObject value = new(objectName, typeof(RectTransform));
            value.transform.SetParent(parent, false);
            return value;
        }

        private static TMP_Text CreateLabel(string objectName, Transform parent, string text,
            float fontSize, Vector2 position, Vector2 size, Color color)
        {
            GameObject labelObject = CreateUiObject(objectName, parent);
            ConfigureCenteredRect(labelObject.GetComponent<RectTransform>(), position, size);
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            return label;
        }

        private static TMP_Text EnsureLabel(string objectName, Transform parent, string text,
            float fontSize, Vector2 position, Vector2 size, Color color)
        {
            Transform existing = parent.Find(objectName);
            TMP_Text label = existing != null ? existing.GetComponent<TMP_Text>() : null;
            if (label == null) return CreateLabel(objectName, parent, text, fontSize, position, size, color);
            ConfigureCenteredRect(label.rectTransform, position, size);
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            return label;
        }

        private static Button CreateButton(string objectName, Transform parent, string label,
            Vector2 position, Vector2 size, Color color, float fontSize)
        {
            GameObject buttonObject = CreateUiObject(objectName, parent);
            ConfigureCenteredRect(buttonObject.GetComponent<RectTransform>(), position, size);
            Image image = buttonObject.AddComponent<Image>();
            image.color = color;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.disabledColor = DisabledGreen;
            button.colors = colors;
            TMP_Text buttonLabel = CreateLabel("Label", buttonObject.transform, label, fontSize,
                Vector2.zero, size, Color.white);
            buttonLabel.fontStyle = FontStyles.Bold;
            return button;
        }

        private static RectTransform EnsureImage(string objectName, Transform parent, Color color,
            Vector2 position, Vector2 size)
        {
            Transform existing = parent.Find(objectName);
            GameObject target = existing != null ? existing.gameObject : CreateUiObject(objectName, parent);
            RectTransform rect = target.GetComponent<RectTransform>();
            ConfigureCenteredRect(rect, position, size);
            Image image = GetOrAdd<Image>(target);
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = GetOrAdd<Outline>(target);
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T value = target.GetComponent<T>();
            return value != null ? value : target.AddComponent<T>();
        }

        private static void ConfigureCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
