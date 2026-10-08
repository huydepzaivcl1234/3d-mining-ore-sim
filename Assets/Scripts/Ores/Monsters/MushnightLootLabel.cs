using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>World-space presentation only. The thief owns the actual stolen balance.</summary>
    public sealed class MushnightLootLabel : MonoBehaviour
    {
        private MushnightThief thief;
        private MushnightSettings settings;
        private CharacterController body;
        private RectTransform panel;
        private TextMeshProUGUI label;
        private Camera view;
        private float shownGold = -1f;
        private bool languageDirty = true;
        private float nextCameraCheck;

        public void Initialize(MushnightThief owner, MushnightSettings data)
        {
            thief = owner; settings = data; body = GetComponent<CharacterController>();
            if (panel != null) return;
            var root = new GameObject("Stolen gold", typeof(RectTransform), typeof(Canvas));
            panel = (RectTransform)root.transform;
            panel.SetParent(transform, false);
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            panel.sizeDelta = new Vector2(220, 42);
            var background = root.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(.025f, .035f, .06f, .88f); background.raycastTarget = false;
            var textObject = new GameObject("Amount", typeof(RectTransform));
            var rect = (RectTransform)textObject.transform; rect.SetParent(panel, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6, 2); rect.offsetMax = new Vector2(-6, -2);
            label = textObject.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            label.fontStyle = FontStyles.Bold;
            label.color = settings.goldLabelColor;
            if (settings.goldLabelFont != null) label.font = settings.goldLabelFont;
            root.SetActive(false);
        }

        private void OnEnable() => MiningLocalization.LanguageChanged += OnLanguageChanged;
        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= OnLanguageChanged;
            if (panel != null) panel.gameObject.SetActive(false);
        }
        private void OnLanguageChanged() => languageDirty = true;

        private void LateUpdate()
        {
            if (panel == null || thief == null) return;
            bool visible = !thief.IsInvisible && thief.StolenGold > 0f &&
                thief.State != MushnightThief.ThiefState.Dead && thief.State != MushnightThief.ThiefState.Gone;
            if (panel.gameObject.activeSelf != visible) panel.gameObject.SetActive(visible);
            if (!visible) return;
            if (shownGold != thief.StolenGold || languageDirty)
            {
                shownGold = thief.StolenGold; languageDirty = false;
                label.text = string.Format(MiningLocalization.TextKey("Mushnight.StolenGold", "Stolen: {0}"), shownGold.ToString("0.##"));
            }
            label.fontSize = settings.goldLabelFontSize;
            Vector3 parentScale = transform.lossyScale;
            panel.localScale = new Vector3(settings.goldLabelWidth / 220f / Mathf.Max(.001f, Mathf.Abs(parentScale.x)),
                settings.goldLabelHeight / 42f / Mathf.Max(.001f, Mathf.Abs(parentScale.y)), 1f / Mathf.Max(.001f, Mathf.Abs(parentScale.z)));
            Vector3 head = body != null ? transform.TransformPoint(body.center + Vector3.up * body.height * .5f) : transform.position + Vector3.up * 1.5f;
            panel.position = head + Vector3.up * settings.goldLabelHeadOffset;
            if (view == null && Time.unscaledTime >= nextCameraCheck)
            { view = Camera.main; nextCameraCheck = Time.unscaledTime + 1f; }
            if (view != null) panel.rotation = view.transform.rotation;
        }
    }
}
