using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>World-space reward feedback spawned from an authored TMP prefab.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class OreRewardPopup : MonoBehaviour
    {
        [SerializeField] private TextMeshPro label;

        private MiningUiData uiData;
        private Camera targetCamera;
        private Vector3 startPosition;
        private Color startColor;
        private float elapsed;

        public void Initialize(int amount, Vector3 worldPosition, MiningUiData targetUiData)
        {
            uiData = targetUiData;
            label ??= GetComponent<TextMeshPro>();
            targetCamera = Camera.main;
            startPosition = worldPosition + uiData.RewardPopupWorldOffset;
            transform.SetPositionAndRotation(startPosition, Quaternion.identity);
            transform.localScale = Vector3.one * uiData.RewardPopupWorldScale;
            label.text = string.Format(uiData.RewardPopupFormat,
                MiningMoneyFormatter.Format(amount));
            label.fontSize = uiData.RewardPopupFontSize;
            label.color = uiData.RewardPopupColor;
            label.outlineColor = uiData.RewardPopupOutlineColor;
            label.outlineWidth = uiData.RewardPopupOutlineWidth;
            startColor = label.color;
            elapsed = 0f;
        }

        private void Update()
        {
            if (uiData == null || label == null)
            {
                Destroy(gameObject);
                return;
            }

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / uiData.RewardPopupDuration);
            float easedProgress = 1f - (1f - progress) * (1f - progress);
            transform.position = startPosition + Vector3.up *
                (uiData.RewardPopupRiseDistance * easedProgress);

            Color color = startColor;
            color.a = 1f - progress;
            label.color = color;

            targetCamera ??= Camera.main;
            if (targetCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(
                    transform.position - targetCamera.transform.position,
                    targetCamera.transform.up);
            }

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
