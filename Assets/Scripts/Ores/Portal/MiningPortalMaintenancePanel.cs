using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// "Đang sửa chữa" (under maintenance) notice shown when the player clicks a portal gate
    /// that has no destination wired up yet. Builds and owns its own small UI the first time
    /// EnsureRuntime runs, as a new child under the existing HUD canvas - it does not touch,
    /// resize, or reposition any existing panel, icon, or HUD element.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MiningPortalMaintenancePanel : MonoBehaviour
    {
        private const string PanelObjectName = "PortalMaintenancePanel";
        private const string DefaultTitle = "ĐANG SỬA CHỮA";
        private const string DefaultMessage = "Cổng này đang được xây dựng.\nQuay lại sau nhé!";
        private const string CloseButtonLabel = "ĐÃ HIỂU";
        private const float FadeDuration = 0.15f;

        [SerializeField] private string title = DefaultTitle;
        [SerializeField, TextArea] private string message = DefaultMessage;

        private CanvasGroup canvasGroup;
        private RectTransform card;
        private Coroutine activeFade;

        private void Awake()
        {
            // Self-healing safety net: no matter how this object ends up active (Show(), a
            // stray Inspector toggle while debugging, a scene re-save, etc.), it must never sit
            // there blocking clicks while invisible. Show() re-enables raycasts right after.
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        /// <summary>Idempotent: reuses the existing panel object under this canvas if one was
        /// already built by a previous run instead of creating a duplicate.</summary>
        public static MiningPortalMaintenancePanel EnsureRuntime(Canvas canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            Transform existing = canvas.transform.Find(PanelObjectName);
            if (existing != null)
            {
                return existing.GetComponent<MiningPortalMaintenancePanel>();
            }

            RectTransform root = CreateRect(PanelObjectName, canvas.transform);
            Stretch(root);
            root.SetAsLastSibling();

            Image blocker = root.gameObject.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.72f);
            blocker.raycastTarget = true;

            // CanvasGroup must be added before the script component: Awake() (which runs
            // immediately here since the object is still active at this point) reads it to
            // force raycasts off by default.
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            MiningPortalMaintenancePanel panel = root.gameObject.AddComponent<MiningPortalMaintenancePanel>();
            panel.canvasGroup = group;
            panel.Build(root, blocker);
            root.gameObject.SetActive(false);
            return panel;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            if (activeFade != null)
            {
                StopCoroutine(activeFade);
            }
            activeFade = StartCoroutine(Fade(0f, 1f, FadeDuration, null));
        }

        public void Hide()
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            if (activeFade != null)
            {
                StopCoroutine(activeFade);
            }
            activeFade = StartCoroutine(Fade(canvasGroup.alpha, 0f, FadeDuration,
                () => gameObject.SetActive(false)));
        }

        private void Build(RectTransform root, Image blocker)
        {
            canvasGroup.alpha = 0f;

            Button blockerButton = root.gameObject.AddComponent<Button>();
            blockerButton.transition = Selectable.Transition.None;
            blockerButton.targetGraphic = blocker;
            blockerButton.onClick.AddListener(Hide);

            card = CreateRect("Card", root);
            SetCentered(card, new Vector2(560f, 340f));
            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.color = new Color(0.05f, 0.05f, 0.07f, 0.98f);
            cardImage.raycastTarget = true;

            TMP_Text titleText = CreateText("Title", card, title, 34f, FontStyles.Bold);
            titleText.color = new Color(1f, 0.78f, 0.2f, 1f);
            SetRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f),
                new Vector2(480f, 56f));

            TMP_Text messageText = CreateText("Message", card, message, 22f, FontStyles.Normal);
            messageText.color = new Color(0.9f, 0.9f, 0.92f, 1f);
            SetRect(messageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f),
                new Vector2(480f, 120f));

            Button closeButton = CreateButton("CloseButton", card, CloseButtonLabel,
                new Vector2(0f, -125f));
            closeButton.onClick.AddListener(Hide);
        }

        private IEnumerator Fade(float from, float to, float duration, System.Action completed)
        {
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / safeDuration);
                yield return null;
            }

            canvasGroup.alpha = to;
            completed?.Invoke();
        }

        private static Button CreateButton(string objectName, Transform parent, string label,
            Vector2 position)
        {
            GameObject go = new(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(220f, 60f);

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.85f, 0.2f, 0.2f, 1f);

            TMP_Text text = CreateText("Label", go.transform, label, 22f, FontStyles.Bold);
            text.color = Color.white;
            Stretch(text.rectTransform);
            text.raycastTarget = false;

            return go.GetComponent<Button>();
        }

        private static TMP_Text CreateText(string objectName, Transform parent, string value,
            float size, FontStyles style)
        {
            GameObject go = new(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject go = new(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void SetCentered(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }
    }
}
