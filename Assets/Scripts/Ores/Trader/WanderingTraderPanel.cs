using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    public sealed class WanderingTraderPanel : MonoBehaviour
    {
        private const string RuntimePanelName = "Wandering Trader Runtime Panel";
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text offerText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button tradeButton;
        [SerializeField] private Button closeButton;
        private Action closedAction;
        private Func<WanderingTraderSystem.TraderOffer, bool> acceptOfferAction;
        private readonly List<GameObject> offerRows = new();
        private float resetAt;
        private bool isOpen;
        public bool IsOpen => isOpen;

        private void Awake()
        {
            BindButtons();
            ApplyShopStyle();
        }

        private void OnEnable()
        {
            BindButtons();
        }

        public static WanderingTraderPanel EnsureRuntime()
        {
            WanderingTraderPanel existing = FindFirstObjectByType<WanderingTraderPanel>(FindObjectsInactive.Include);
            if (existing != null) return existing;
            Canvas canvas = FindHudCanvas();
            if (canvas == null) return null;
            GameObject root = CreateUiObject(RuntimePanelName, canvas.transform);
            Stretch(root.GetComponent<RectTransform>());
            Image backdrop = root.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.7f);
            Button backdropButton = root.AddComponent<Button>();
            WanderingTraderPanel panel = root.AddComponent<WanderingTraderPanel>();
            panel.Build(root.transform);
            backdropButton.onClick.AddListener(panel.Hide);
            root.SetActive(false);
            return panel;
        }

        public void Show(IReadOnlyList<WanderingTraderSystem.TraderOffer> offers,
            float secondsRemaining, Func<WanderingTraderSystem.TraderOffer, bool> onAccept, Action onClosed)
        {
            if (offers == null || offers.Count == 0 || offerText == null) return;
            ClearOfferRows();
            acceptOfferAction = onAccept;
            closedAction = onClosed;
            resetAt = Time.unscaledTime + Mathf.Max(0f, secondsRemaining);
            isOpen = true;
            if (titleText != null) titleText.text = "WANDERING TRADER OFFERS";
            if (feedbackText != null) feedbackText.text = string.Empty;
            offerText.gameObject.SetActive(false);
            if (tradeButton != null) tradeButton.gameObject.SetActive(false);
            Transform parent = offerText.transform.parent != null ? offerText.transform.parent : transform;
            for (int i = 0; i < offers.Count; i++) CreateOfferRow(parent, offers[i], i);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ApplyShopStyle();
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
            if (feedbackText != null)
            {
                int remaining = Mathf.CeilToInt(Mathf.Max(0f, resetAt - Time.unscaledTime));
                feedbackText.text = $"RESTOCK IN {remaining / 60:00}:{remaining % 60:00}";
            }
        }

        private void CreateOfferRow(Transform parent, WanderingTraderSystem.TraderOffer offer, int index)
        {
            GameObject row = CreateUiObject($"Trader Offer {index + 1}", parent);
            offerRows.Add(row);
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 82f - index * 124f);
            rect.sizeDelta = new Vector2(900f, 108f);
            Image image = row.AddComponent<Image>();
            image.color = new Color(0.09f, 0.025f, 0.008f, 0.98f);
            Button button = row.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                if (acceptOfferAction != null && acceptOfferAction(offer)) Hide();
                else if (feedbackText != null) feedbackText.text = "You cannot complete this trade.";
            });
            CreateOfferWell(row.transform, "Give Well", new Vector2(-245f, 0f),
                offer.TraderSellsItem ? "GIVE" : "GIVE",
                offer.TraderSellsItem ? "MONEY" : offer.Item.DisplayName,
                offer.TraderSellsItem ? MiningMoneyFormatter.Format(offer.RewardAmount) : $"x{offer.ItemAmount}",
                offer.TraderSellsItem ? null : GetItemIcon(offer.Item), new Color(0.56f, 0.22f, 0.06f));
            CreateLabel("Arrow", row.transform, ">", 40f, new Vector2(-22f, 0f),
                new Vector2(52f, 56f), new Color(1f, 0.78f, 0.27f));
            CreateOfferWell(row.transform, "Receive Well", new Vector2(165f, 0f),
                "RECEIVE",
                offer.TraderSellsItem ? offer.Item.DisplayName : (offer.PaysGems ? "GEMS" : "MONEY"),
                offer.TraderSellsItem ? $"x{offer.ItemAmount}" : FormatCurrencyAmount(offer),
                offer.TraderSellsItem ? GetItemIcon(offer.Item) : null, new Color(0.75f, 0.45f, 0.08f));

            Button trade = CreateButton("Trade Button", row.transform, "TRADE",
                new Vector2(356f, 0f), new Color(0.08f, 0.45f, 0.16f));
            RectTransform tradeRect = trade.GetComponent<RectTransform>();
            tradeRect.sizeDelta = new Vector2(140f, 62f);
            trade.onClick.AddListener(() =>
            {
                if (acceptOfferAction != null && acceptOfferAction(offer)) Hide();
                else if (feedbackText != null) feedbackText.text = "You cannot complete this trade.";
            });
        }

        private static void CreateOfferWell(Transform parent, string name, Vector2 position,
            string heading, string itemName, string amount, Sprite icon, Color border)
        {
            GameObject well = CreateUiObject(name, parent);
            RectTransform rect = well.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(300f, 76f);
            Image background = well.AddComponent<Image>();
            background.color = new Color(0.035f, 0.01f, 0.003f, 1f);

            GameObject iconObject = CreateUiObject("Icon", well.transform);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(12f, 0f);
            iconRect.sizeDelta = new Vector2(58f, 58f);
            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.color = icon != null ? Color.white : border;
            iconImage.preserveAspect = true;
            if (icon == null)
            {
                TMP_Text currency = CreateLabel("Currency Mark", iconObject.transform,
                    itemName == "GEMS" ? "G" : "M", 28f, Vector2.zero,
                    iconRect.sizeDelta, Color.white);
                currency.fontStyle = FontStyles.Bold;
            }

            TMP_Text giveReceive = CreateLabel("Heading", well.transform, heading, 13f,
                new Vector2(-46f, 20f), new Vector2(180f, 24f), new Color(0.75f, 0.48f, 0.23f));
            giveReceive.alignment = TextAlignmentOptions.Left;
            TMP_Text value = CreateLabel("Value", well.transform, $"{itemName} {amount}", 20f,
                new Vector2(-46f, -11f), new Vector2(210f, 32f), Color.white);
            value.alignment = TextAlignmentOptions.Left;
        }

        private static string FormatCurrencyAmount(WanderingTraderSystem.TraderOffer offer) =>
            offer.PaysGems ? offer.RewardAmount.ToString("0") : MiningMoneyFormatter.Format(offer.RewardAmount);

        private static Sprite GetItemIcon(MiningItemData item)
        {
            if (item == null) return null;
            var property = item.GetType().GetProperty("Icon");
            return property?.GetValue(item) as Sprite;
        }

        private void ClearOfferRows()
        {
            for (int i = offerRows.Count - 1; i >= 0; i--)
                if (offerRows[i] != null) Destroy(offerRows[i]);
            offerRows.Clear();
        }

        private void Build(Transform parent)
        {
            GameObject card = CreateUiObject("Trade Card", parent);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(620f, 365f);
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(0.13f, 0.07f, 0.03f, 0.98f);
            Button blocker = card.AddComponent<Button>();
            blocker.transition = Selectable.Transition.None;
            titleText = CreateLabel("Title", card.transform, "WANDERING TRADER OFFERS", 30, new Vector2(0f, 135f), new Vector2(560f, 48f), new Color(1f, 0.78f, 0.27f));
            offerText = CreateLabel("Offer", card.transform, string.Empty, 20, Vector2.zero, new Vector2(530f, 180f), Color.white);
            feedbackText = CreateLabel("Countdown", card.transform, string.Empty, 19, new Vector2(0f, -137f), new Vector2(520f, 30f), new Color(0.35f, 1f, 0.55f));
            closeButton = CreateButton("Close Button", card.transform, "CLOSE", new Vector2(0f, -170f), new Color(0.52f, 0.18f, 0.16f));
            BindButtons();
        }

        public void ConfigureSceneUi(TMP_Text title, TMP_Text offer, TMP_Text feedback, Button trade, Button close)
        {
            titleText = title;
            offerText = offer;
            feedbackText = feedback;
            tradeButton = trade;
            closeButton = close;
            BindButtons();
        }

        private void BindButtons()
        {
            if (closeButton == null) return;
            closeButton.onClick.RemoveListener(Hide);
            closeButton.onClick.AddListener(Hide);
        }

        private void ApplyShopStyle()
        {
            Image background = GetComponent<Image>();
            if (background != null)
                background.color = new Color(0.02f, 0.015f, 0.01f, 0.78f);

            Transform card = transform.Find("Offer Card");
            if (card == null) card = transform.Find("Trade Card");
            RectTransform cardRect = card != null ? card.GetComponent<RectTransform>() : null;
            if (cardRect != null) cardRect.sizeDelta = new Vector2(1000f, 660f);
            Image cardImage = card != null ? card.GetComponent<Image>() : null;
            if (cardImage != null)
                cardImage.color = new Color(0.18f, 0.08f, 0.03f, 0.98f);
            if (titleText != null)
            {
                RectTransform titleRect = titleText.rectTransform;
                titleRect.anchoredPosition = new Vector2(-250f, 270f);
                titleRect.sizeDelta = new Vector2(500f, 52f);
                titleText.alignment = TextAlignmentOptions.Left;
                titleText.fontSize = 29f;
                titleText.color = new Color(1f, 0.87f, 0.58f);
            }
            if (feedbackText != null)
            {
                RectTransform timerRect = feedbackText.rectTransform;
                timerRect.anchoredPosition = new Vector2(-275f, 205f);
                timerRect.sizeDelta = new Vector2(360f, 36f);
                feedbackText.alignment = TextAlignmentOptions.Left;
                feedbackText.fontSize = 19f;
                feedbackText.color = new Color(1f, 0.67f, 0.30f);
            }
            if (closeButton != null)
            {
                RectTransform closeRect = closeButton.GetComponent<RectTransform>();
                closeRect.anchoredPosition = new Vector2(430f, 270f);
                closeRect.sizeDelta = new Vector2(62f, 48f);
            }
        }

        private static Canvas FindHudCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas candidate in canvases)
                if (candidate.name.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0) return candidate;
            return canvases.Length > 0 ? canvases[0] : null;
        }

        private static GameObject CreateUiObject(string objectName, Transform parent)
        {
            GameObject value = new GameObject(objectName, typeof(RectTransform));
            value.transform.SetParent(parent, false);
            return value;
        }

        private static TMP_Text CreateLabel(string objectName, Transform parent, string text, float fontSize, Vector2 position, Vector2 size, Color color)
        {
            GameObject labelObject = CreateUiObject(objectName, parent);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = true;
            return label;
        }

        private static Button CreateButton(string objectName, Transform parent, string label, Vector2 position, Color color)
        {
            GameObject buttonObject = CreateUiObject(objectName, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(230f, 52f);
            Image image = buttonObject.AddComponent<Image>();
            image.color = color;
            Button button = buttonObject.AddComponent<Button>();
            CreateLabel("Label", buttonObject.transform, label, 22, Vector2.zero, rect.sizeDelta, Color.white);
            return button;
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
