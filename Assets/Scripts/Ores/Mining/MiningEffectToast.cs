using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Shows the authoritative MiningItemSystem effects in a three-card status bar.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningEffectToast : MonoBehaviour
    {
        private sealed class EffectSlotView
        {
            public GameObject root;
            public RectTransform rect;
            public CanvasGroup group;
            public MiningStatusEffectCardGraphic card;
            public Image icon;
            public TextMeshProUGUI fallback;
            public TextMeshProUGUI name;
            public TextMeshProUGUI timer;
            public MiningItemData item;
            public float lastRemaining;
            public Coroutine animation;
        }

        [SerializeField] private MiningItemSystem itemSystem;
        [SerializeField] private MiningUiData uiData;
        [SerializeField] private GameObject toastRoot;
        [Tooltip("Legacy text kept for existing Scene references. The status cards replace it at runtime.")]
        [SerializeField] private TextMeshProUGUI effectLabel;

        private readonly List<MiningItemSystem.ActiveEffectView> activeEffects = new(3);
        private readonly List<EffectSlotView> slotViews = new(3);
        private RectTransform slotsRoot;
        private TextMeshProUGUI headerLabel;
        private float nextRefreshTime;

        private void Awake()
        {
            FindUiDataIfMissing();
            EnsureStatusUi();
            Refresh();
        }

        private void OnEnable()
        {
            if (itemSystem != null)
            {
                itemSystem.EffectsChanged -= Refresh;
                itemSystem.EffectsChanged += Refresh;
            }
            MiningLocalization.LanguageChanged -= Refresh;
            MiningLocalization.LanguageChanged += Refresh;
            EnsureStatusUi();
            Refresh();
        }

        private void OnDisable()
        {
            if (itemSystem != null) itemSystem.EffectsChanged -= Refresh;
            MiningLocalization.LanguageChanged -= Refresh;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime || toastRoot == null || !toastRoot.activeSelf)
                return;
            nextRefreshTime = Time.unscaledTime + 0.1f;
            Refresh();
        }

        private void Refresh()
        {
            EnsureStatusUi();
            if (headerLabel != null)
                headerLabel.text = MiningLocalization.Text("ACTIVE EFFECTS", "HIỆU ỨNG ĐANG CÓ");

            activeEffects.Clear();
            itemSystem?.GetActiveEffects(activeEffects);
            EnsureSlotCount(3);

            for (int index = 0; index < slotViews.Count; index++)
            {
                EffectSlotView view = slotViews[index];
                bool active = index < activeEffects.Count;
                if (!active)
                {
                    view.group.alpha = 0f;
                    view.item = null;
                    view.lastRemaining = 0f;
                    continue;
                }

                MiningItemSystem.ActiveEffectView effect = activeEffects[index];
                bool newlyAssigned = view.item != effect.Item;
                bool durationExtended = !newlyAssigned && effect.RemainingSeconds > view.lastRemaining + 0.25f;
                view.group.alpha = 1f;
                Bind(view, effect);
                if (newlyAssigned) PlayAnimation(view, true);
                else if (durationExtended) PlayAnimation(view, false);
            }

            toastRoot?.SetActive(activeEffects.Count > 0);
        }

        private void FindUiDataIfMissing()
        {
            if (uiData != null) return;
            MiningUiPanelCoordinator coordinator = FindFirstObjectByType<MiningUiPanelCoordinator>(
                FindObjectsInactive.Include);
            uiData = coordinator != null ? coordinator.UiData : null;
        }

        private void EnsureStatusUi()
        {
            if (toastRoot == null || slotsRoot != null) return;

            if (effectLabel != null)
            {
                effectLabel.text = string.Empty;
                effectLabel.enabled = false;
            }
            Image rootImage = toastRoot.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = Color.clear;
                rootImage.raycastTarget = false;
            }
            Transform compact = toastRoot.transform.Find("Compact Effects");
            if (compact != null) compact.gameObject.SetActive(false);

            Transform existing = toastRoot.transform.Find("Juicy Status Effects");
            RectTransform presentation;
            if (existing == null)
            {
                GameObject rootObject = new("Juicy Status Effects", typeof(RectTransform));
                rootObject.transform.SetParent(toastRoot.transform, false);
                presentation = rootObject.GetComponent<RectTransform>();
                Stretch(presentation, 0f, 0f);
            }
            else
            {
                presentation = existing.GetComponent<RectTransform>();
            }

            MiningStatusBarFrameGraphic frame = presentation.GetComponent<MiningStatusBarFrameGraphic>() ??
                                                presentation.gameObject.AddComponent<MiningStatusBarFrameGraphic>();
            frame.raycastTarget = false;

            Transform headerTransform = presentation.Find("Header");
            headerLabel = headerTransform != null
                ? headerTransform.GetComponent<TextMeshProUGUI>()
                : CreateText(presentation, "Header", 14f, TextAlignmentOptions.Center);
            RectTransform headerRect = headerLabel.rectTransform;
            headerRect.anchorMin = new Vector2(0.18f, 0.78f);
            headerRect.anchorMax = new Vector2(0.82f, 0.98f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;
            headerLabel.color = new Color(1f, 0.82f, 0.35f, 1f);
            headerLabel.enableAutoSizing = true;
            headerLabel.fontSizeMin = 9f;
            headerLabel.fontSizeMax = 16f;
            headerLabel.characterSpacing = 2f;

            Transform slotsTransform = presentation.Find("Cards");
            if (slotsTransform == null)
            {
                GameObject cards = new("Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                cards.transform.SetParent(presentation, false);
                slotsRoot = cards.GetComponent<RectTransform>();
            }
            else slotsRoot = slotsTransform.GetComponent<RectTransform>();

            slotsRoot.anchorMin = new Vector2(0.045f, 0.13f);
            slotsRoot.anchorMax = new Vector2(0.955f, 0.76f);
            slotsRoot.offsetMin = Vector2.zero;
            slotsRoot.offsetMax = Vector2.zero;
            HorizontalLayoutGroup layout = slotsRoot.GetComponent<HorizontalLayoutGroup>() ??
                                           slotsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 7f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }

        private void EnsureSlotCount(int count)
        {
            if (slotsRoot == null) return;
            while (slotViews.Count < count)
            {
                int index = slotViews.Count;
                Transform existing = slotsRoot.Find($"Effect Card {index + 1:00}");
                slotViews.Add(existing != null ? CacheSlot(existing) : CreateSlot(index));
            }
        }

        private EffectSlotView CreateSlot(int index)
        {
            GameObject slotObject = new($"Effect Card {index + 1:00}", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(MiningStatusEffectCardGraphic), typeof(LayoutElement),
                typeof(CanvasGroup));
            slotObject.transform.SetParent(slotsRoot, false);
            RectTransform rect = slotObject.GetComponent<RectTransform>();
            LayoutElement layout = slotObject.GetComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            layout.flexibleHeight = 1f;
            layout.minWidth = 70f;
            CanvasGroup group = slotObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            Image icon = CreateImage(slotObject.transform, "Icon");
            SetAnchored(icon.rectTransform, new Vector2(0.075f, 0.22f), new Vector2(0.36f, 0.78f));
            icon.preserveAspect = true;

            TextMeshProUGUI fallback = CreateText(slotObject.transform, "Fallback", 20f,
                TextAlignmentOptions.Center);
            SetAnchored(fallback.rectTransform, new Vector2(0.075f, 0.22f), new Vector2(0.36f, 0.78f));

            TextMeshProUGUI name = CreateText(slotObject.transform, "Name", 10f,
                TextAlignmentOptions.Left);
            SetAnchored(name.rectTransform, new Vector2(0.42f, 0.53f), new Vector2(0.95f, 0.88f));
            name.enableAutoSizing = true;
            name.fontSizeMin = 6f;
            name.fontSizeMax = 11f;
            name.overflowMode = TextOverflowModes.Ellipsis;

            TextMeshProUGUI timer = CreateText(slotObject.transform, "Time", 12f,
                TextAlignmentOptions.Left);
            SetAnchored(timer.rectTransform, new Vector2(0.42f, 0.26f), new Vector2(0.95f, 0.58f));
            timer.enableAutoSizing = true;
            timer.fontSizeMin = 7f;
            timer.fontSizeMax = 13f;

            return new EffectSlotView
            {
                root = slotObject,
                rect = rect,
                group = group,
                card = slotObject.GetComponent<MiningStatusEffectCardGraphic>(),
                icon = icon,
                fallback = fallback,
                name = name,
                timer = timer
            };
        }

        private static EffectSlotView CacheSlot(Transform slot)
        {
            return new EffectSlotView
            {
                root = slot.gameObject,
                rect = slot.GetComponent<RectTransform>(),
                group = slot.GetComponent<CanvasGroup>() ?? slot.gameObject.AddComponent<CanvasGroup>(),
                card = slot.GetComponent<MiningStatusEffectCardGraphic>(),
                icon = slot.Find("Icon")?.GetComponent<Image>(),
                fallback = slot.Find("Fallback")?.GetComponent<TextMeshProUGUI>(),
                name = slot.Find("Name")?.GetComponent<TextMeshProUGUI>(),
                timer = slot.Find("Time")?.GetComponent<TextMeshProUGUI>()
            };
        }

        private static void Bind(EffectSlotView view, MiningItemSystem.ActiveEffectView effect)
        {
            MiningItemData item = effect.Item;
            bool hasIcon = item != null && item.InventoryIcon != null;
            if (view.icon != null)
            {
                view.icon.sprite = hasIcon ? item.InventoryIcon : null;
                view.icon.enabled = hasIcon;
            }
            if (view.fallback != null)
            {
                view.fallback.text = item != null ? item.IconFallback : "?";
                view.fallback.color = item != null ? item.FallbackColor : Color.white;
                view.fallback.enabled = !hasIcon;
            }
            if (view.name != null)
            {
                view.name.text = item != null
                    ? $"{item.ShortEffectName}  +{item.EffectPercent:0.##}%"
                    : string.Empty;
                view.name.color = item != null ? item.FallbackColor : Color.white;
            }
            if (view.timer != null)
            {
                int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(effect.RemainingSeconds));
                view.timer.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
                view.timer.color = effect.RemainingSeconds <= 10f
                    ? new Color(1f, 0.38f, 0.28f, 1f)
                    : Color.white;
            }
            if (view.card != null)
            {
                Color accent = item != null ? item.FallbackColor : Color.white;
                float duration = item != null ? item.EffectDurationSeconds : 1f;
                view.card.SetState(accent, effect.RemainingSeconds / Mathf.Max(0.1f, duration),
                    effect.RemainingSeconds <= 10f);
            }
            view.item = item;
            view.lastRemaining = effect.RemainingSeconds;
        }

        private void PlayAnimation(EffectSlotView view, bool entering)
        {
            if (view.animation != null) StopCoroutine(view.animation);
            view.animation = StartCoroutine(AnimateCard(view, entering));
        }

        private static float EaseOutBack(float value)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = value - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        private IEnumerator AnimateCard(EffectSlotView view, bool entering)
        {
            float duration = entering ? 0.28f : 0.20f;
            float elapsed = 0f;
            while (elapsed < duration && view.root != null && view.root.activeSelf)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = entering ? EaseOutBack(t) : 1f + Mathf.Sin(t * Mathf.PI) * 0.12f;
                view.rect.localScale = Vector3.one * scale;
                yield return null;
            }
            if (view.rect != null) view.rect.localScale = Vector3.one;
            view.animation = null;
        }

        private TextMeshProUGUI CreateText(Transform parent, string objectName, float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject child = new(objectName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            child.transform.SetParent(parent, false);
            TextMeshProUGUI label = child.GetComponent<TextMeshProUGUI>();
            label.font = effectLabel != null && effectLabel.font != null
                ? effectLabel.font
                : TMP_Settings.defaultFontAsset;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        private static Image CreateImage(Transform parent, string objectName)
        {
            GameObject child = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(parent, false);
            Image image = child.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect, float horizontal, float vertical)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontal, vertical);
            rect.offsetMax = new Vector2(-horizontal, -vertical);
        }

        private static void SetAnchored(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
