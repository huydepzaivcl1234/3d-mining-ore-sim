using System.Collections;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Animation-only fly-in for an authored HUD RectTransform.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningHudFlyIn : MonoBehaviour
    {
        public enum SlideDirection
        {
            FromTop,
            FromBottom,
            FromLeft,
            FromRight
        }

        [SerializeField] private RectTransform target;
        [SerializeField] private SlideDirection direction = SlideDirection.FromTop;
        [Min(10f), SerializeField] private float slideDistance = 180f;
        [Min(0f), SerializeField] private float delay;
        [Min(0.05f), SerializeField] private float duration = 0.45f;

        private Vector2 targetPosition;
        private Coroutine routine;
        private bool hasTargetPosition;

        /// <summary>Total unscaled time needed before this HUD reaches its authored position.</summary>
        public float TotalDuration => delay + duration;

        public void Configure(RectTransform authoredTarget, SlideDirection slideDirection,
            float distance, float startDelay, float animationDuration)
        {
            target = authoredTarget;
            direction = slideDirection;
            slideDistance = Mathf.Max(10f, distance);
            delay = Mathf.Max(0f, startDelay);
            duration = Mathf.Max(0.05f, animationDuration);
        }

        public void Play()
        {
            if (target == null)
            {
                target = transform as RectTransform;
            }
            if (target == null || !gameObject.activeInHierarchy)
            {
                return;
            }

            StopAndRestore();
            targetPosition = target.anchoredPosition;
            hasTargetPosition = true;
            target.anchoredPosition = targetPosition + DirectionOffset();
            routine = StartCoroutine(Animate());
        }

        public void StopAndRestore()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
            if (hasTargetPosition && target != null)
            {
                target.anchoredPosition = targetPosition;
            }
            hasTargetPosition = false;
        }

        /// <summary>
        /// Forces the last cached authored position without starting another animation.
        /// Safe to call after the fly-in has already completed.
        /// </summary>
        public void CompleteImmediately()
        {
            StopAndRestore();
        }

        private void OnDisable()
        {
            StopAndRestore();
        }

        private IEnumerator Animate()
        {
            float wait = 0f;
            while (wait < delay)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            Vector2 start = target.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.anchoredPosition = Vector2.LerpUnclamped(
                    start, targetPosition, EaseOutBack(t));
                yield return null;
            }

            target.anchoredPosition = targetPosition;
            hasTargetPosition = false;
            routine = null;
        }

        private Vector2 DirectionOffset()
        {
            return direction switch
            {
                SlideDirection.FromTop => Vector2.up * slideDistance,
                SlideDirection.FromBottom => Vector2.down * slideDistance,
                SlideDirection.FromLeft => Vector2.left * slideDistance,
                SlideDirection.FromRight => Vector2.right * slideDistance,
                _ => Vector2.zero
            };
        }

        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            return 1f + (overshoot + 1f) * Mathf.Pow(t - 1f, 3f) +
                   overshoot * Mathf.Pow(t - 1f, 2f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            slideDistance = Mathf.Max(10f, slideDistance);
            delay = Mathf.Max(0f, delay);
            duration = Mathf.Max(0.05f, duration);
        }
#endif
    }
}
