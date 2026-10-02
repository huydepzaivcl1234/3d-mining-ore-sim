using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.VectorGraphics;

namespace MiningSimulator.Ores
{
    /// <summary>SVG presentation only; prices, saves and purchase SFX remain with the existing upgrade system.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningUpgradeCarousel : MonoBehaviour
    {
        [SerializeField] private Sprite cardSvg;
        [Min(0.01f), SerializeField] private float transitionSeconds = 0.18f;
        private readonly List<Card> cards = new();
        [SerializeField] private RectTransform content;
        [SerializeField] private UnityEngine.UI.Scrollbar scrollbar;
        [SerializeField] private UnityEngine.UI.Button closeButton;
        private Action closeAction;
        private Material vectorMaterial;
        private int first;
        private bool initialized, changingScrollbar;
        private const float Row = 300f, Width = 1380f, Height = 259.3f;
        private sealed class Card
        {
            public JuicyUpgradeItem Item;
            public RectTransform Rect;
            public CanvasGroup Group;
            public MiningUpgradeCardHover Hover;
        }
        public int FirstVisibleIndex => first;
        public int CardCount => cards.Count;
        public int VisibleCardCount => Mathf.Min(3, cards.Count - first);

        public void Initialize(MiningUpgradeSystem system, PlayerWallet wallet, MiningUpgradeType[] order, Action close)
        {
            if (initialized && cards.Count > 0) return;
            cards.Clear();
            gameObject.SetActive(false);
            closeAction = close;
            if (content != null)
            {
                EnsureRegenerationCards(content);
                // Reuse the authored view. Never regenerate its graphics, text sizes or icon offsets.
                foreach (var item in content.GetComponentsInChildren<JuicyUpgradeItem>(true))
                {
                    item.BindCard(system, wallet);
                    cards.Add(new Card { Item = item, Rect = (RectTransform)item.transform,
                        Group = item.GetComponent<CanvasGroup>(), Hover = item.GetComponent<MiningUpgradeCardHover>() });
                }
                var authoredScroll = content.GetComponentInParent<UpgradeCardScrollRect>(true);
                if (authoredScroll != null) authoredScroll.Owner = this;
                content.sizeDelta = new Vector2(content.sizeDelta.x,
                    Mathf.Max(content.sizeDelta.y, (cards.Count + 2) * Row));
                scrollbar.size = Mathf.Min(1f, 3f / Mathf.Max(3, cards.Count));
                scrollbar.numberOfSteps = Mathf.Max(2, cards.Count);
                scrollbar.onValueChanged.AddListener(OnScrollbar);
                if (closeButton != null) closeButton.onClick.AddListener(CloseView);
                initialized = true; SetFirst(0); ApplyPresentation(true);
                return;
            }
            cardSvg ??= Resources.Load<Sprite>("UpgradeCard");
            if (cardSvg == null) { Debug.LogError("UpgradeCard SVG is missing.", this); return; }
            var vectorSource = Resources.Load<GameObject>("UpgradeCard");
            if (vectorSource != null) vectorMaterial = vectorSource.GetComponent<SVGImage>().material;
            var items = GetComponentsInChildren<JuicyUpgradeItem>(true);
            EnsureRegenerationCards(transform);
            items = GetComponentsInChildren<JuicyUpgradeItem>(true);
            // An ordered list is optional: newly added buttons are appended in their authored hierarchy order.
            var sorted = new List<JuicyUpgradeItem>();
            if (order != null) foreach (var type in order)
                foreach (var item in items) if (item.UpgradeType == type && !sorted.Contains(item)) sorted.Add(item);
            foreach (var item in items) if (!sorted.Contains(item)) sorted.Add(item);
            foreach (Transform child in transform) child.gameObject.SetActive(false);
            foreach (var behaviour in GetComponents<Behaviour>())
                if (behaviour != null && behaviour != this && behaviour is not CanvasGroup) behaviour.enabled = false;

            var scrollRoot = Rect("SVG Upgrade Scroll", transform, new Vector2(1580f, 900f), new Vector2(-20f, 0f));
            var scroll = scrollRoot.gameObject.AddComponent<UpgradeCardScrollRect>();
            scroll.Owner = this; scroll.horizontal = false; scroll.vertical = true;
            scroll.inertia = false; scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            var viewport = Rect("Viewport", scrollRoot, new Vector2(1460f, 900f), new Vector2(-45f, 0f));
            var hitArea = viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0.001f);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            content = Rect("Content", viewport, new Vector2(1460f, (sorted.Count + 2) * Row), Vector2.zero);
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f); content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            scroll.viewport = viewport; scroll.content = content;
            foreach (var item in sorted) BuildCard(item, system, wallet);

            var track = Rect("Scrollbar", scrollRoot, new Vector2(34f, 900f), new Vector2(752f, 0f));
            var trackImage = track.gameObject.AddComponent<UnityEngine.UI.Image>();
            trackImage.color = Color.white;
            track.gameObject.AddComponent<MiningUiGradient>().SetColors(new Color(0.03f, 0.63f, 0.73f), new Color(0.03f, 0f, 0f));
            var sliding = Rect("Sliding Area", track, new Vector2(34f, 864f), Vector2.zero);
            var handle = Rect("Handle", sliding, new Vector2(34f, 36f), Vector2.zero);
            var knobRect = Rect("Knob", handle, new Vector2(34f, 34f), new Vector2(0f, -17f));
            knobRect.anchorMin = knobRect.anchorMax = new Vector2(0.5f, 1f);
            var knob = knobRect.gameObject.AddComponent<SVGImage>();
            knob.sprite = Resources.Load<Sprite>("UpgradeScrollKnob"); knob.material = vectorMaterial;
            knob.preserveAspect = true; knob.color = Color.white;
            scrollbar = track.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();
            scrollbar.handleRect = handle; scrollbar.targetGraphic = knob;
            scrollbar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            scrollbar.size = Mathf.Min(1f, 3f / Mathf.Max(3, cards.Count));
            scrollbar.numberOfSteps = Mathf.Max(2, cards.Count);
            scrollbar.onValueChanged.AddListener(OnScrollbar);
            // We intentionally own the snapped scrollbar, rather than allowing two writers to move content.
            var closeRect = Rect("Close SVG Upgrades", transform, new Vector2(95f, 65f), new Vector2(845f, 470f));
            var closeImage = closeRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            closeImage.color = new Color(0.015f, 0.23f, 0.27f, 0.95f);
            closeButton = closeRect.gameObject.AddComponent<UnityEngine.UI.Button>();
            closeButton.targetGraphic = closeImage; closeButton.onClick.AddListener(CloseView);
            closeRect.gameObject.AddComponent<MiningButtonSfx>();
            var closeText = Text("Close", closeRect, new Vector2(95f, 65f), Vector2.zero, 48f);
            closeText.text = "×"; closeText.color = Color.white; closeText.alignment = TextAlignmentOptions.Center;
            initialized = true;
            SetFirst(0); ApplyPresentation(true);
        }

        private void BuildCard(JuicyUpgradeItem item, MiningUpgradeSystem system, PlayerWallet wallet)
        {
            Sprite icon = null;
            foreach (var image in item.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if (image.name == "Sprite" && image.sprite != null) { icon = image.sprite; break; }
            if (item.CardIcon != null) icon = item.CardIcon;
            item.gameObject.SetActive(false);
            foreach (Transform child in item.transform) child.gameObject.SetActive(false);
            var button = item.GetComponent<UnityEngine.UI.Button>();
            // Retire the old leather/punch decorators; they must not fight carousel scale/alpha or double-play SFX.
            foreach (var behaviour in item.GetComponents<Behaviour>())
                if (behaviour != null && behaviour != item && behaviour != button && behaviour is not CanvasGroup) behaviour.enabled = false;
            var rect = (RectTransform)item.transform;
            rect.SetParent(content, false); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f); rect.sizeDelta = new Vector2(Width, Height);
            rect.anchoredPosition = new Vector2(0f, -cards.Count * Row - 12f); rect.localScale = Vector3.one;
            // Unity allows only one Graphic per GameObject; keep the authored Image disabled, add SVG on a child.
            var frame = Rect("SVG Frame", rect, new Vector2(Width, Height), Vector2.zero);
            var svg = frame.gameObject.AddComponent<SVGImage>();
            svg.sprite = cardSvg; svg.material = vectorMaterial;
            svg.preserveAspect = true; svg.color = Color.white; svg.raycastTarget = true;
            button.targetGraphic = svg;
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.82f, 0.94f, 1f); colors.disabledColor = new Color(0.78f, 0.78f, 0.78f);
            button.colors = colors;
            var title = Text("SVG Title", rect, new Vector2(1010f, 78f), new Vector2(-125f, -64f), 44f);
            var detail = Text("SVG Detail", rect, new Vector2(1010f, 56f), new Vector2(-125f, -130f), 32f);
            var price = Text("SVG Price", rect, new Vector2(900f, 50f), new Vector2(-100f, -193f), 35f);
            price.color = new Color(0.02f, 0.17f, 0.20f);
            var coinRect = Rect("Coin", rect, new Vector2(40f, 40f), new Vector2(-608f, -193f));
            coinRect.anchorMin = coinRect.anchorMax = new Vector2(0.5f, 1f);
            var coin = coinRect.gameObject.AddComponent<UnityEngine.UI.Image>(); coin.raycastTarget = false;
            foreach (var image in item.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if (image.name == "Coin_Icon") { coin.sprite = image.sprite; break; }
            var iconRect = Rect("SVG Icon", rect, new Vector2(160f, 160f), new Vector2(538f, -130f));
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
            var iconImage = iconRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            iconImage.sprite = icon; iconImage.preserveAspect = true; iconImage.raycastTarget = false;
            var group = item.GetComponent<CanvasGroup>() ?? item.gameObject.AddComponent<CanvasGroup>();
            var hover = item.gameObject.AddComponent<MiningUpgradeCardHover>();
            item.ConfigureCard(system, wallet, title, detail, price);
            cards.Add(new Card { Item = item, Rect = rect, Group = group, Hover = hover });
            item.gameObject.SetActive(true);
        }

        private static void EnsureRegenerationCards(Transform parent)
        {
            var items = parent.GetComponentsInChildren<JuicyUpgradeItem>(true);
            if (items.Length == 0) return;
            // Clone only missing rows; retain every authored row, order, SVG and purchase behavior.
            foreach (var type in new[] { MiningUpgradeType.RegenIntervalReduction, MiningUpgradeType.HealingEffectiveness })
            {
                bool exists = false;
                foreach (var item in items) if (item.UpgradeType == type) { exists = true; break; }
                if (exists) continue;
                var source = items[items.Length - 1];
                var clone = Instantiate(source, source.transform.parent);
                clone.name = type + " Upgrade";
                clone.SetUpgradeType(type);
                // Never copy an old persistent onClick that purchases the template's upgrade.
                clone.GetComponent<UnityEngine.UI.Button>().onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                var rect = (RectTransform)clone.transform;
                rect.anchoredPosition -= new Vector2(0f, 300f * (type == MiningUpgradeType.RegenIntervalReduction ? 1f : 2f));
            }
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        private static TextMeshProUGUI Text(string name, RectTransform parent, Vector2 size, Vector2 position, float fontSize)
        {
            var rect = Rect(name, parent, size, position);
            if (parent.GetComponent<JuicyUpgradeItem>() != null) rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize; text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.045f, 0.06f, 0.025f); text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal; return text;
        }
        private void OnScrollbar(float value)
        {
            if (!changingScrollbar) SetFirst(Mathf.RoundToInt((1f - value) * Mathf.Max(0, cards.Count - 1)));
        }
        public void ScrollSteps(int steps) => SetFirst(first + steps);
        private void SetFirst(int index)
        {
            first = Mathf.Clamp(index, 0, Mathf.Max(0, cards.Count - 1));
            if (scrollbar != null)
            {
                changingScrollbar = true;
                scrollbar.SetValueWithoutNotify(cards.Count <= 1 ? 1f : 1f - (float)first / (cards.Count - 1));
                changingScrollbar = false;
            }
        }
        private void LateUpdate() { if (initialized) ApplyPresentation(false); }
        private void ApplyPresentation(bool instant)
        {
            float blend = instant ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime * 5f / Mathf.Max(0.01f, transitionSeconds));
            content.anchoredPosition = Vector2.Lerp(content.anchoredPosition, new Vector2(0f, first * Row), blend);
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i]; int slot = i - first;
                bool visible = slot >= 0 && slot < 3;
                // At most three visible cards, including during scroll transitions.
                card.Group.blocksRaycasts = card.Group.interactable = visible;
                float alpha = !visible ? 0f : slot == 0 ? 1f : slot == 1 ? 0.58f : 0.30f;
                card.Group.alpha = !visible ? 0f : Mathf.Lerp(card.Group.alpha, alpha, blend);
                float scale = !visible ? 0.86f : 1f - slot * 0.055f;
                if (slot == 0 && card.Hover.IsHovered) scale += 0.015f;
                card.Rect.localScale = Vector3.Lerp(card.Rect.localScale, Vector3.one * scale, blend);
            }
        }
        private void CloseView() => closeAction?.Invoke();
        private void OnDestroy()
        {
            if (scrollbar != null) scrollbar.onValueChanged.RemoveListener(OnScrollbar);
            if (closeButton != null) closeButton.onClick.RemoveListener(CloseView);
        }
    }
}
