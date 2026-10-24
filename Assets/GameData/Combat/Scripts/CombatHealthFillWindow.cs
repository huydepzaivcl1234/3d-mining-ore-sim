using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    // Micro Bar still owns timing and HP. Clip its decorative SVG without
    // counting the portion hidden behind the level badge as visible health.
    public sealed class CombatHealthFillWindow : MonoBehaviour
    {
        [SerializeField] private Image animatedFill;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private Image visibleFill;
        [SerializeField, Min(0)] private float coveredWidth = 28;
        [SerializeField, Min(1)] private float visibleWidth = 134;
        [SerializeField] private bool useAuthoredColor;
        [SerializeField] private Color authoredColor = Color.white;
        private float lastFill = -1;
        private void OnEnable() => lastFill = -1;
        private void LateUpdate()
        {
            if (animatedFill == null || viewport == null) return;
            float value = Mathf.Clamp01(animatedFill.fillAmount);
            Color color = useAuthoredColor ? authoredColor : animatedFill.color;
            if (visibleFill != null && visibleFill.color != color)
                visibleFill.color = color;
            if (value == lastFill) return;
            lastFill = value;
            viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                value <= 0 ? 0 : coveredWidth + visibleWidth * value);
        }
    }
}
