using System;
using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class MiningDamagePopupSettings
    {
        public bool enabled = true;
        [Min(0.1f)] public float duration = 0.85f;
        [Min(0f)] public float riseHeight = 0.65f;
        [Min(0f)] public float fallDistance = 0.3f;
        [Range(0.1f, 0.9f)] public float peakTime = 0.32f;
        [Min(0f)] public float sidewaysDistance = 0.25f;
        public Vector3 worldOffset = new(0f, 0.35f, 0f);
        [Min(0.001f)] public float worldScale = 0.18f;
        [Min(1f)] public float fontSize = 18f;
        public TMP_FontAsset font;
        public Color color = new(1f, 0.85f, 0.24f, 1f);
        public Color outlineColor = Color.black;
        [Range(0f, 1f)] public float outlineWidth = 0.2f;
        [Range(0f, 0.95f)] public float fadeStart = 0.55f;
        [Min(0f)] public float scalePunch = 0.25f;
        [Range(0.01f, 1f)] public float punchFraction = 0.25f;
        [Min(1)] public int maximumVisible = 64;
    }

    /// <summary>Detached world-space hit numbers. Never follows the sword or the damaged enemy.</summary>
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class MiningDamagePopup : MonoBehaviour
    {
        private static int visibleCount;
        private MiningDamagePopupSettings settings;
        private TextMeshPro label;
        private Camera view;
        private Vector3 origin, sideways;
        private float elapsed;
        private bool counted;

        public static void Show(float damage, Vector3 hitPoint, MiningDamagePopupSettings settings)
        {
            if (damage <= 0f || settings == null || !settings.enabled ||
                visibleCount >= Mathf.Max(1, settings.maximumVisible)) return;
            var go = new GameObject("Damage Popup", typeof(TextMeshPro));
            var popup = go.AddComponent<MiningDamagePopup>();
            popup.settings = settings;
            popup.label = go.GetComponent<TextMeshPro>();
            popup.view = Camera.main;
            popup.origin = hitPoint + settings.worldOffset;
            popup.sideways = (popup.view != null ? popup.view.transform.right : Vector3.right) *
                UnityEngine.Random.Range(-settings.sidewaysDistance, settings.sidewaysDistance);
            popup.label.font = settings.font != null ? settings.font : TMP_Settings.defaultFontAsset;
            popup.label.text = damage.ToString("0.##");
            popup.label.fontSize = settings.fontSize;
            popup.label.fontStyle = FontStyles.Bold;
            popup.label.alignment = TextAlignmentOptions.Center;
            popup.label.textWrappingMode = TextWrappingModes.NoWrap;
            popup.label.color = settings.color;
            popup.label.outlineColor = settings.outlineColor;
            popup.label.outlineWidth = settings.outlineWidth;
            popup.label.raycastTarget = false;
            popup.transform.position = popup.origin;
            popup.counted = true;
            visibleCount++;
            popup.ApplyProgress(0f);
        }

        // Smooth velocity reaches zero at the apex, then accelerates downwards.
        public static float EvaluateHeight(float progress, MiningDamagePopupSettings settings)
        {
            float peak = Mathf.Clamp(settings.peakTime, 0.1f, 0.9f);
            if (progress <= peak)
            {
                float t = Mathf.Clamp01(progress / peak);
                return settings.riseHeight * (1f - (1f - t) * (1f - t));
            }
            float fall = Mathf.Clamp01((progress - peak) / (1f - peak));
            return settings.riseHeight - (settings.riseHeight + settings.fallDistance) * fall * fall;
        }

        private void LateUpdate()
        {
            if (settings == null) return;
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, settings.duration));
            ApplyProgress(progress);
            if (view == null) view = Camera.main;
            if (view != null) transform.rotation = view.transform.rotation;
            if (progress >= 1f) Destroy(gameObject);
        }

        private void ApplyProgress(float progress)
        {
            transform.position = origin + sideways * progress +
                Vector3.up * EvaluateHeight(progress, settings);
            // A brief size punch, followed by a soft settle.
            float punch = 1f + settings.scalePunch * Mathf.Sin(
                Mathf.Clamp01(progress / Mathf.Max(0.01f, settings.punchFraction)) * Mathf.PI);
            transform.localScale = Vector3.one * Mathf.Max(0.001f, settings.worldScale) * punch;
            Color color = settings.color;
            color.a *= 1f - Mathf.InverseLerp(settings.fadeStart, 1f, progress);
            label.color = color;
        }

        private void OnDestroy()
        {
            if (counted) visibleCount = Mathf.Max(0, visibleCount - 1);
        }
    }
}
