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
        [Tooltip("Optional coin icon shown beside the reward amount. Assign its Sprite in the prefab.")]
        [SerializeField] private SpriteRenderer coinIcon;

        private MiningUiData uiData;
        private Camera targetCamera;
        private Vector3 startPosition;
        private Color startColor;
        private Color iconStartColor;
        private float elapsed;

        public void Initialize(float amount, Vector3 worldPosition, MiningUiData targetUiData)
        {
            uiData = targetUiData;
            label ??= GetComponent<TextMeshPro>();
            coinIcon ??= transform.Find("Coin Icon")?.GetComponent<SpriteRenderer>();
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
            if (coinIcon != null)
            {
                coinIcon.transform.localPosition = uiData.RewardPopupIconLocalPosition;
                coinIcon.transform.localScale = Vector3.one * uiData.RewardPopupIconScale;
                coinIcon.color = uiData.RewardPopupIconColor;
                iconStartColor = coinIcon.color;
            }
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
            if (coinIcon != null)
            {
                Color iconColor = iconStartColor;
                iconColor.a *= 1f - progress;
                coinIcon.color = iconColor;
            }

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
