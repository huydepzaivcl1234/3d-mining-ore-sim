using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// A runtime-only modal for the wandering trader. It is deliberately built at
    /// runtime so adding the trader never writes to the authored HUD or Shop UI.
    /// </summary>
    public sealed class WanderingTraderPanel : MonoBehaviour
    {
        private const string RuntimePanelName = "Wandering Trader Runtime Panel";

        private GameObject card;
        private TMP_Text offerText;
        private TMP_Text feedbackText;
        private Func<bool> acceptAction;
        private Action closedAction;
        private bool isOpen;

        public static WanderingTraderPanel EnsureRuntime()
        {
            WanderingTraderPanel existing = FindFirstObjectByType<WanderingTraderPanel>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return existing;
            }

            Canvas canvas = FindHudCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("Wandering Trader could not create its trade window because no UI Canvas was found.");
                return null;
            }

            GameObject root = CreateUiObject(RuntimePanelName, canvas.transform);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);

            Image backdrop = root.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.7f);
            Button backdropButton = root.AddComponent<Button>();

            WanderingTraderPanel panel = root.AddComponent<WanderingTraderPanel>();
            panel.Build(root.transform);
            backdropButton.onClick.AddListener(panel.Hide);
            root.SetActive(false);
            return panel;
        }

        public void Show(WanderingTraderSystem.TraderOffer offer, Func<bool> onAccept, Action onClosed)
        {
            acceptAction = onAccept;
            closedAction = onClosed;
            isOpen = true;
            feedbackText.text = string.Empty;

            string currency = offer.PaysGems ? "GEMS" : "MONEY";
            string reward = offer.PaysGems
                ? offer.RewardAmount.ToString()
                : MiningMoneyFormatter.Format(offer.RewardAmount);
            offerText.text = $"<b>{offer.Item.DisplayName} x{offer.ItemAmount}</b>\n\n" +
                             $"I will give you <b>{reward} {currency}</b> for these items.";

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            Action callback = closedAction;
            closedAction = null;
            acceptAction = null;
            gameObject.SetActive(false);
            callback?.Invoke();
        }

        private void Accept()
        {
            if (acceptAction == null)
            {
                Hide();
                return;
            }

            if (acceptAction())
            {
                Hide();
                return;
            }

            feedbackText.text = "You no longer have the requested items.";
        }

        private void Build(Transform parent)
        {
            card = CreateUiObject("Trade Card", parent);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(620f, 345f);
            cardRect.anchoredPosition = Vector2.zero;

            Image cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(0.13f, 0.07f, 0.03f, 0.98f);
            // Consume clicks on the card itself so only a click on the dark backdrop closes it.
            Button cardClickBlocker = card.AddComponent<Button>();
            cardClickBlocker.transition = Selectable.Transition.None;

            CreateLabel("Title", card.transform, "WANDERING TRADER", 32, new Vector2(0f, 120f), new Vector2(560f, 52f), new Color(1f, 0.78f, 0.27f));
            offerText = CreateLabel("Offer", card.transform, string.Empty, 25, new Vector2(0f, 32f), new Vector2(530f, 145f), Color.white);
            offerText.alignment = TextAlignmentOptions.Center;

            feedbackText = CreateLabel("Feedback", card.transform, string.Empty, 20, new Vector2(0f, -65f), new Vector2(520f, 34f), new Color(1f, 0.45f, 0.35f));
            feedbackText.alignment = TextAlignmentOptions.Center;

            CreateButton("Trade Button", card.transform, "TRADE", new Vector2(-145f, -122f), new Color(0.18f, 0.52f, 0.22f), Accept);
            CreateButton("Leave Button", card.transform, "LEAVE", new Vector2(145f, -122f), new Color(0.52f, 0.18f, 0.16f), Hide);
        }

        private static Canvas FindHudCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas candidate in canvases)
            {
                if (candidate.name.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return candidate;
                }
            }

            return canvases.Length > 0 ? canvases[0] : null;
        }

        private static GameObject CreateUiObject(string objectName, Transform parent)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
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

        private static void CreateButton(string objectName, Transform parent, string label, Vector2 position, Color color, Action callback)
        {
            GameObject buttonObject = CreateUiObject(objectName, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(230f, 58f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = color;
            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => callback());
            CreateLabel("Label", buttonObject.transform, label, 24, Vector2.zero, rect.sizeDelta, Color.white);
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
