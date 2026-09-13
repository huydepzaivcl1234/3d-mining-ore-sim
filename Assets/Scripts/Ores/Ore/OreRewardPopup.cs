using PrimeTween;
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
        private Vector3 restingScale;
        private Color startColor;
        private Color iconStartColor;
        private Sequence animationSequence;

        public void Initialize(float amount, Vector3 worldPosition, MiningUiData targetUiData)
        {
            uiData = targetUiData;
            label ??= GetComponent<TextMeshPro>();
            coinIcon ??= transform.Find("Coin Icon")?.GetComponent<SpriteRenderer>();
            targetCamera = Camera.main;
            startPosition = worldPosition + uiData.RewardPopupWorldOffset;
            transform.SetPositionAndRotation(startPosition, Quaternion.identity);
            restingScale = Vector3.one * uiData.RewardPopupWorldScale;
            transform.localScale = restingScale * uiData.RewardPopupStartScale;
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
            PlayAnimation();
        }

        private void LateUpdate()
        {
            if (uiData == null || label == null)
            {
                Destroy(gameObject);
                return;
            }

            targetCamera ??= Camera.main;
            if (targetCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(
                    transform.position - targetCamera.transform.position,
                    targetCamera.transform.up);
            }
        }

        private void OnDisable()
        {
            if (animationSequence.isAlive)
            {
                animationSequence.Stop();
            }
        }

        private void PlayAnimation()
        {
            if (animationSequence.isAlive)
            {
                animationSequence.Stop();
            }

            float duration = uiData.RewardPopupDuration;
            float popDuration = Mathf.Min(uiData.RewardPopupPopDuration, duration);
            float settleDuration = Mathf.Min(uiData.RewardPopupSettleDuration,
                Mathf.Max(0.01f, duration - popDuration));
            Vector3 poppedScale = restingScale * uiData.RewardPopupPopScale;

            animationSequence = Sequence.Create(Tween.Custom(this, 0f, 1f, duration,
                    static (popup, progress) => popup.ApplyAnimationProgress(progress),
                    Ease.OutCubic))
                .Group(Tween.Scale(transform, poppedScale, popDuration, Ease.OutBack))
                .Insert(popDuration, Tween.Scale(transform, restingScale, settleDuration,
                    Ease.OutSine))
                .ChainCallback(this, static popup => popup.DestroyAfterAnimation());
        }

        private void ApplyAnimationProgress(float progress)
        {
            transform.position = startPosition + Vector3.up *
                (uiData.RewardPopupRiseDistance * progress);

            float fadeStart = uiData.RewardPopupFadeStart;
            float alpha = 1f - Mathf.InverseLerp(fadeStart, 1f, progress);
            Color color = startColor;
            color.a *= alpha;
            label.color = color;
            if (coinIcon != null)
            {
                Color iconColor = iconStartColor;
                iconColor.a *= alpha;
                coinIcon.color = iconColor;
            }
        }

        private void DestroyAfterAnimation()
        {
            animationSequence = default;
            if (this != null)
            {
                Destroy(gameObject);
            }
        }
    }
}
