using PrimeTween;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Reusable smooth hit feedback for mineable objects. The caller chooses the
    /// animated transform so dynamic objects can animate a visual child without
    /// moving their Rigidbody or collider.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MiningHitPunch : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Range(0f, 0.5f), SerializeField] private float scaleAmount = 0.08f;
        [Min(0f), SerializeField] private float liftAmount = 0.12f;
        [Min(0.01f), SerializeField] private float duration = 0.16f;

        private Vector3 baseLocalPosition;
        private Vector3 baseLocalScale = Vector3.one;
        private Sequence animationSequence;

        public void Configure(Transform targetTransform, float targetScaleAmount,
            float targetLiftAmount, float targetDuration)
        {
            if (animationSequence.isAlive)
            {
                animationSequence.Stop();
                RestoreBaseTransform();
            }

            target = targetTransform != null ? targetTransform : transform;
            scaleAmount = Mathf.Clamp(targetScaleAmount, 0f, 0.5f);
            liftAmount = Mathf.Max(0f, targetLiftAmount);
            duration = Mathf.Max(0.01f, targetDuration);
            CaptureBaseTransform();
        }

        public void Play()
        {
            if (target == null)
            {
                target = transform;
                CaptureBaseTransform();
            }

            if (animationSequence.isAlive)
            {
                animationSequence.Stop();
            }
            RestoreBaseTransform();

            float halfDuration = duration * 0.5f;
            Vector3 liftedPosition = baseLocalPosition + Vector3.up * liftAmount;
            animationSequence = Sequence.Create(Tween.Scale(target,
                    baseLocalScale * (1f + scaleAmount), halfDuration, Ease.OutQuad))
                .Group(Tween.LocalPosition(target, liftedPosition, halfDuration, Ease.OutQuad))
                .Chain(Tween.Scale(target, baseLocalScale, halfDuration, Ease.OutBack))
                .Group(Tween.LocalPosition(target, baseLocalPosition, halfDuration, Ease.OutBack))
                .OnComplete(this, static punch => punch.animationSequence = default);
        }

        public void PlayBreak(float squashAmount, float stretchAmount, float breakDuration)
        {
            if (target == null)
            {
                target = transform;
                CaptureBaseTransform();
            }

            if (animationSequence.isAlive)
            {
                animationSequence.Stop();
            }
            RestoreBaseTransform();

            float safeSquash = Mathf.Clamp(squashAmount, 0f, 0.8f);
            float safeStretch = Mathf.Clamp(stretchAmount, 0f, 0.8f);
            float safeDuration = Mathf.Max(0.03f, breakDuration);
            float squashDuration = safeDuration * 0.28f;
            float stretchDuration = safeDuration * 0.25f;
            float vanishDuration = safeDuration - squashDuration - stretchDuration;
            Vector3 squashScale = Vector3.Scale(baseLocalScale,
                new Vector3(1f + safeSquash, 1f - safeSquash, 1f + safeSquash));
            Vector3 stretchScale = Vector3.Scale(baseLocalScale,
                new Vector3(1f - safeStretch, 1f + safeStretch, 1f - safeStretch));

            animationSequence = Sequence.Create(Tween.Scale(target, squashScale,
                    squashDuration, Ease.OutQuad))
                .Chain(Tween.Scale(target, stretchScale, stretchDuration, Ease.OutBack))
                .Chain(Tween.Scale(target, Vector3.zero, vanishDuration, Ease.InBack));
        }

        public void ResetImmediately()
        {
            if (animationSequence.isAlive)
            {
                animationSequence.Stop();
            }
            animationSequence = default;
            if (target != null)
            {
                RestoreBaseTransform();
            }
        }

        private void Awake()
        {
            target ??= transform;
            CaptureBaseTransform();
        }

        private void OnDisable()
        {
            ResetImmediately();
        }

        private void CaptureBaseTransform()
        {
            if (target == null)
            {
                return;
            }

            baseLocalPosition = target.localPosition;
            baseLocalScale = target.localScale;
        }

        private void RestoreBaseTransform()
        {
            target.localPosition = baseLocalPosition;
            target.localScale = baseLocalScale;
        }

        private void OnValidate()
        {
            scaleAmount = Mathf.Clamp(scaleAmount, 0f, 0.5f);
            liftAmount = Mathf.Max(0f, liftAmount);
            duration = Mathf.Max(0.01f, duration);
        }
    }
}
