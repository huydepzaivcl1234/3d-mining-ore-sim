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

        private void Awake()
        {
            BindButtons();
            ApplyShopStyle();
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
                feedbackText.text = $"OFFERS RESET IN {Mathf.CeilToInt(Mathf.Max(0f, resetAt - Time.unscaledTime))}s";
        }

        private void CreateOfferRow(Transform parent, WanderingTraderSystem.TraderOffer offer, int index)
        {
            GameObject row = CreateUiObject($"Trader Offer {index + 1}", parent);
            offerRows.Add(row);
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 72f - index * 62f);
            rect.sizeDelta = new Vector2(540f, 54f);
            Image image = row.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.96f);
            Button button = row.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                if (acceptOfferAction != null && acceptOfferAction(offer)) Hide();
                else if (feedbackText != null) feedbackText.text = "You cannot complete this trade.";
            });
            TMP_Text label = CreateLabel("Offer Text", row.transform, FormatOffer(offer), 19f,
                Vector2.zero, rect.sizeDelta, Color.white);
            label.alignment = TextAlignmentOptions.Center;
        }

        private static string FormatOffer(WanderingTraderSystem.TraderOffer offer)
        {
            if (offer.TraderSellsItem)
                return $"GIVE {MiningMoneyFormatter.Format(offer.RewardAmount)} MONEY   →   RECEIVE {offer.Item.DisplayName} x{offer.ItemAmount}";
            string currency = offer.PaysGems ? "GEMS" : "MONEY";
            string reward = offer.PaysGems ? offer.RewardAmount.ToString("0") : MiningMoneyFormatter.Format(offer.RewardAmount);
            return $"GIVE {offer.Item.DisplayName} x{offer.ItemAmount}   →   RECEIVE {reward} {currency}";
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
            Image cardImage = card != null ? card.GetComponent<Image>() : null;
            if (cardImage != null)
                cardImage.color = new Color(0.18f, 0.08f, 0.03f, 0.98f);
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
