using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Builds the Inventory button presentation around its existing sprite and click logic.
    /// The authored root RectTransform remains the layout source of truth.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button), typeof(RectTransform))]
    public sealed class JuicyInventoryButton : MonoBehaviour
    {
        private const string PresentationName = "Juicy Inventory Visuals";

        private Button button;
        private TextMeshProUGUI titleLabel;
        private Sprite inventorySprite;
        private bool built;

        public void Configure(Button owner, TextMeshProUGUI localizedTitle)
        {
            button = owner != null ? owner : GetComponent<Button>();
            titleLabel = localizedTitle != null
                ? localizedTitle
                : FindAuthoredTitle();
            BuildIfNeeded();
            RefreshLocalization();
        }

        private void Awake()
        {
            button = GetComponent<Button>();
            titleLabel = FindAuthoredTitle();
            BuildIfNeeded();
        }

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged -= RefreshLocalization;
            MiningLocalization.LanguageChanged += RefreshLocalization;
            RefreshLocalization();
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= RefreshLocalization;
        }

        private void BuildIfNeeded()
        {
            if (built || button == null) return;

            Image legacyImage = GetComponent<Image>();
            if (legacyImage != null)
            {
                inventorySprite = legacyImage.sprite;
            }

            Transform existing = transform.Find(PresentationName);
            RectTransform visualRoot;
            if (existing == null)
            {
                GameObject root = new(PresentationName, typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(MiningInventoryButtonGraphic));
                root.transform.SetParent(transform, false);
                visualRoot = root.GetComponent<RectTransform>();
            }
            else
            {
                visualRoot = existing as RectTransform;
            }
            if (visualRoot == null)
            {
                Debug.LogError("Inventory button visual root is not a RectTransform.", this);
                return;
            }

            MiningInventoryButtonGraphic frame =
                visualRoot.GetComponent<MiningInventoryButtonGraphic>() ??
                visualRoot.gameObject.AddComponent<MiningInventoryButtonGraphic>();
            if (frame == null)
            {
                Debug.LogError("Inventory button frame could not be created.", this);
                return;
            }

            if (legacyImage != null)
            {
                legacyImage.raycastTarget = false;
                legacyImage.enabled = false;
            }
            Shadow oldShadow = GetComponent<Shadow>();
            if (oldShadow != null) oldShadow.enabled = false;

            Stretch(visualRoot);
            visualRoot.SetAsFirstSibling();
            frame.raycastTarget = true;
            button.targetGraphic = frame;

            HideLegacyHotkey(visualRoot);
            EnsureIcon(visualRoot);
            StyleTitle();
            built = true;
        }

        private static void HideLegacyHotkey(RectTransform visualRoot)
        {
            Transform staleHotkey = visualRoot.Find("Hotkey");
            if (staleHotkey != null) staleHotkey.gameObject.SetActive(false);
        }

        private void EnsureIcon(RectTransform visualRoot)
        {
            Transform existing = visualRoot.Find("Icon Socket");
            GameObject socketObject;
            if (existing == null)
            {
                socketObject = new GameObject("Icon Socket", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(MiningInventoryIconMaskGraphic), typeof(Mask));
                socketObject.transform.SetParent(visualRoot, false);
            }
            else socketObject = existing.gameObject;

            RectTransform socket = socketObject.GetComponent<RectTransform>();
            SetAnchors(socket, new Vector2(0.035f, 0.15f), new Vector2(0.285f, 0.85f));
            Mask mask = socketObject.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            Transform iconTransform = socket.Find("Inventory Icon");
            Image icon;
            if (iconTransform == null)
            {
                GameObject iconObject = new("Inventory Icon", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
                iconObject.transform.SetParent(socket, false);
                icon = iconObject.GetComponent<Image>();
            }
            else icon = iconTransform.GetComponent<Image>();

            icon.sprite = inventorySprite;
            icon.color = Color.white;
            icon.raycastTarget = false;
            RectTransform iconRect = icon.rectTransform;
            Stretch(iconRect);
            AspectRatioFitter fitter = icon.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = inventorySprite != null && inventorySprite.rect.height > 0f
                ? inventorySprite.rect.width / inventorySprite.rect.height
                : 1f;
        }

        private void StyleTitle()
        {
            if (titleLabel == null) return;
            RectTransform rect = titleLabel.rectTransform;
            SetAnchors(rect, new Vector2(0.30f, 0.10f), new Vector2(0.95f, 0.90f));
            titleLabel.alignment = TextAlignmentOptions.Center;
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.color = Color.white;
            titleLabel.enableVertexGradient = true;
            titleLabel.colorGradient = new VertexGradient(Color.white, Color.white,
                new Color(0.95f, 0.61f, 0.12f, 1f), new Color(0.95f, 0.61f, 0.12f, 1f));
            titleLabel.enableAutoSizing = true;
            titleLabel.fontSizeMax = Mathf.Clamp(titleLabel.fontSize, 18f, 28f);
            titleLabel.fontSizeMin = 12f;
            titleLabel.enableWordWrapping = false;
            titleLabel.overflowMode = TextOverflowModes.Ellipsis;
            titleLabel.characterSpacing = 1.5f;
            titleLabel.outlineColor = new Color32(52, 18, 2, 255);
            titleLabel.outlineWidth = 0.16f;
            titleLabel.raycastTarget = false;
            titleLabel.transform.SetAsLastSibling();
        }

        private void RefreshLocalization()
        {
            if (titleLabel != null)
                titleLabel.text = MiningLocalization.Text("Inventory", "TÚI ĐỒ");
        }

        private TextMeshProUGUI FindAuthoredTitle()
        {
            Transform named = transform.Find("Text (TMP)");
            if (named != null && named.TryGetComponent(out TextMeshProUGUI namedLabel))
            {
                return namedLabel;
            }

            Transform presentation = transform.Find(PresentationName);
            foreach (TextMeshProUGUI label in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (presentation == null || !label.transform.IsChildOf(presentation))
                {
                    return label;
                }
            }
            return null;
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }
}
