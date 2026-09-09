using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Shows all active consumable effects and their live remaining durations.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningEffectToast : MonoBehaviour
    {
        private sealed class EffectSlotView
        {
            public GameObject root;
            public Image icon;
            public TextMeshProUGUI fallback;
            public TextMeshProUGUI percent;
            public TextMeshProUGUI timer;
            public Outline outline;
        }

        [SerializeField] private MiningItemSystem itemSystem;
        [SerializeField] private MiningUiData uiData;
        [SerializeField] private GameObject toastRoot;
        [Tooltip("Legacy text kept for existing Scene references. Compact slots replace it at runtime.")]
        [SerializeField] private TextMeshProUGUI effectLabel;

        private readonly List<MiningItemSystem.ActiveEffectView> activeEffects = new(3);
        private readonly List<EffectSlotView> slotViews = new(3);
        private RectTransform slotsRoot;
        private float nextRefreshTime;

        private void Awake()
        {
            FindUiDataIfMissing();
            EnsureCompactUi();
            Refresh();
        }

        private void OnEnable()
        {
            if (itemSystem != null)
            {
                itemSystem.EffectsChanged -= Refresh;
                itemSystem.EffectsChanged += Refresh;
            }
            EnsureCompactUi();
            Refresh();
        }

        private void OnDisable()
        {
            if (itemSystem != null)
            {
                itemSystem.EffectsChanged -= Refresh;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime || toastRoot == null ||
                !toastRoot.activeSelf)
            {
                return;
            }
            nextRefreshTime = Time.unscaledTime + 0.1f;
            Refresh();
        }

        private void Refresh()
        {
            EnsureCompactUi();
            activeEffects.Clear();
            itemSystem?.GetActiveEffects(activeEffects);
            bool visible = activeEffects.Count > 0;
            EnsureSlotCount(activeEffects.Count);
            for (int index = 0; index < slotViews.Count; index++)
            {
                bool active = index < activeEffects.Count;
                EffectSlotView view = slotViews[index];
                view.root.SetActive(active);
                if (active)
                {
                    Bind(view, activeEffects[index]);
                }
            }
            toastRoot?.SetActive(visible);
        }

        private void FindUiDataIfMissing()
        {
            if (uiData != null)
            {
                return;
            }
            MiningUiPanelCoordinator coordinator = FindFirstObjectByType<MiningUiPanelCoordinator>(
                FindObjectsInactive.Include);
            uiData = coordinator != null ? coordinator.UiData : null;
        }

        private void EnsureCompactUi()
        {
            if (toastRoot == null || slotsRoot != null)
            {
                return;
            }

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

            Transform existing = toastRoot.transform.Find("Compact Effects");
            if (existing == null)
            {
                GameObject rootObject = new("Compact Effects", typeof(RectTransform),
                    typeof(HorizontalLayoutGroup));
                rootObject.transform.SetParent(toastRoot.transform, false);
                slotsRoot = rootObject.GetComponent<RectTransform>();
                slotsRoot.anchorMin = Vector2.zero;
                slotsRoot.anchorMax = Vector2.one;
                slotsRoot.offsetMin = Vector2.zero;
                slotsRoot.offsetMax = Vector2.zero;
            }
            else
            {
                slotsRoot = existing.GetComponent<RectTransform>();
            }

            HorizontalLayoutGroup layout = slotsRoot.GetComponent<HorizontalLayoutGroup>() ??
                                           slotsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = uiData != null ? uiData.EffectToastSlotSpacing : 10f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private void EnsureSlotCount(int count)
        {
            if (slotsRoot == null)
            {
                return;
            }
            while (slotViews.Count < count)
            {
                int index = slotViews.Count;
                Transform existing = slotsRoot.Find($"Effect Slot {index + 1:00}");
                slotViews.Add(existing != null
                    ? CacheSlot(existing)
                    : CreateSlot(index));
            }
        }

        private EffectSlotView CreateSlot(int index)
        {
            Vector2 slotSize = uiData != null ? uiData.EffectToastSlotSize : new Vector2(78f, 78f);
            GameObject slotObject = new($"Effect Slot {index + 1:00}", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(LayoutElement));
            slotObject.transform.SetParent(slotsRoot, false);
            RectTransform slotRect = slotObject.GetComponent<RectTransform>();
            slotRect.sizeDelta = slotSize;
            Image background = slotObject.GetComponent<Image>();
            background.color = uiData != null
                ? uiData.EffectToastSlotColor
                : new Color(0.10f, 0.11f, 0.13f, 0.96f);
            background.raycastTarget = false;
            Outline outline = slotObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.38f, 0.42f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);
            LayoutElement layout = slotObject.GetComponent<LayoutElement>();
            layout.preferredWidth = slotSize.x;
            layout.preferredHeight = slotSize.y;

            Image icon = CreateImage(slotObject.transform, "Icon");
            float padding = uiData != null ? uiData.EffectToastIconPadding : 7f;
            Stretch(icon.rectTransform, padding, padding);
            icon.preserveAspect = true;

            TextMeshProUGUI fallback = CreateText(slotObject.transform, "Fallback", 34f,
                TextAlignmentOptions.Center);
            Stretch(fallback.rectTransform, padding, padding);

            float percentSize = uiData != null ? uiData.EffectToastPercentFontSize : 13f;
            TextMeshProUGUI percent = CreateText(slotObject.transform, "Percent", percentSize,
                TextAlignmentOptions.TopRight);
            SetTopRight(percent.rectTransform, new Vector2(-4f, -3f), new Vector2(52f, 20f));

            float timerSize = uiData != null ? uiData.EffectToastTimerFontSize : 16f;
            TextMeshProUGUI timer = CreateText(slotObject.transform, "Time", timerSize,
                TextAlignmentOptions.BottomLeft);
            SetBottomStretch(timer.rectTransform, new Vector2(5f, 2f), 25f);
            timer.fontStyle = FontStyles.Bold;

            return new EffectSlotView
            {
                root = slotObject,
                icon = icon,
                fallback = fallback,
                percent = percent,
                timer = timer,
                outline = outline
            };
        }

        private EffectSlotView CacheSlot(Transform slot)
        {
            return new EffectSlotView
            {
                root = slot.gameObject,
                icon = slot.Find("Icon")?.GetComponent<Image>(),
                fallback = slot.Find("Fallback")?.GetComponent<TextMeshProUGUI>(),
                percent = slot.Find("Percent")?.GetComponent<TextMeshProUGUI>(),
                timer = slot.Find("Time")?.GetComponent<TextMeshProUGUI>(),
                outline = slot.GetComponent<Outline>()
            };
        }

        private static void Bind(EffectSlotView view,
            MiningItemSystem.ActiveEffectView effect)
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
            if (view.percent != null)
            {
                view.percent.text = item != null ? $"+{item.EffectPercent:0.##}%" : string.Empty;
                view.percent.color = item != null ? item.FallbackColor : Color.white;
            }
            if (view.timer != null)
            {
                view.timer.text = $"{Mathf.CeilToInt(effect.RemainingSeconds)}s";
            }
            if (view.outline != null && item != null)
            {
                Color accent = item.FallbackColor;
                accent.a = 1f;
                view.outline.effectColor = accent;
            }
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
            GameObject child = new(objectName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
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

        private static void SetTopRight(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetBottomStretch(RectTransform rect, Vector2 inset, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(inset.x, inset.y);
            rect.offsetMax = new Vector2(-inset.x, inset.y + height);
        }
    }
}
