using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Shows a live "time left" readout above a Lucky Block, counting down to when it
    /// despawns, and switches to an urgent color once time is almost up.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LuckyBlockCountdownLabel : MonoBehaviour
    {
        [SerializeField] private LuckyBlock luckyBlock;
        [SerializeField] private TextMeshPro label;
        [SerializeField] private Camera targetCamera;

        public void Configure(LuckyBlock targetBlock, TextMeshPro targetLabel)
        {
            luckyBlock = targetBlock;
            label = targetLabel;
        }

        private void Awake()
        {
            luckyBlock ??= GetComponentInParent<LuckyBlock>();
            label ??= GetComponent<TextMeshPro>();
            targetCamera ??= Camera.main;
        }

        private void LateUpdate()
        {
            if (luckyBlock == null || label == null)
            {
                return;
            }

            LuckyBlockData settings = luckyBlock.Settings;
            if (settings == null || !settings.ShowCountdownLabel)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            transform.position = luckyBlock.GetWorldTopCenter() + settings.CountdownLabelWorldOffset;
            SetWorldScale(settings.CountdownLabelScale);

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
            if (targetCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(
                    transform.position - targetCamera.transform.position, targetCamera.transform.up);
            }

            float remaining = luckyBlock.TimeRemainingSeconds;
            label.fontSize = settings.CountdownLabelFontSize;
            label.color = remaining <= settings.CountdownUrgentThresholdSeconds
                ? settings.CountdownUrgentColor
                : settings.CountdownLabelColor;
            label.text = FormatTime(remaining);
        }

        private void SetVisible(bool visible)
        {
            if (label != null && label.gameObject.activeSelf != visible)
            {
                label.gameObject.SetActive(visible);
            }
        }

        private static string FormatTime(float seconds)
        {
            int wholeSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            int minutes = wholeSeconds / 60;
            int remainder = wholeSeconds % 60;
            return minutes > 0 ? $"{minutes}:{remainder:00}" : $"{remainder}s";
        }

        private void SetWorldScale(float uniformScale)
        {
            Transform parent = transform.parent;
            if (parent == null)
            {
                transform.localScale = Vector3.one * uniformScale;
                return;
            }

            Vector3 parentScale = parent.lossyScale;
            transform.localScale = new Vector3(
                SafeDivide(uniformScale, parentScale.x),
                SafeDivide(uniformScale, parentScale.y),
                SafeDivide(uniformScale, parentScale.z));
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) > Mathf.Epsilon ? value / divisor : value;
        }
    }
}