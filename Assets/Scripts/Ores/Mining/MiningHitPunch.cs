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
        private float elapsed;
        private bool playing;

        public void Configure(Transform targetTransform, float targetScaleAmount,
            float targetLiftAmount, float targetDuration)
        {
            if (playing && target != null)
            {
                RestoreBaseTransform();
            }

            elapsed = 0f;
            playing = false;
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

            RestoreBaseTransform();
            elapsed = 0f;
            playing = true;
        }

        public void ResetImmediately()
        {
            if (target != null)
            {
                RestoreBaseTransform();
            }

            elapsed = 0f;
            playing = false;
        }

        private void Awake()
        {
            target ??= transform;
            CaptureBaseTransform();
        }

        private void Update()
        {
            if (!playing || target == null)
            {
                return;
            }

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.SmoothStep(0f, 1f, Mathf.Sin(progress * Mathf.PI));
            target.localScale = baseLocalScale * (1f + scaleAmount * pulse);
            target.localPosition = baseLocalPosition + Vector3.up * (liftAmount * pulse);

            if (progress >= 1f)
            {
                ResetImmediately();
            }
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
