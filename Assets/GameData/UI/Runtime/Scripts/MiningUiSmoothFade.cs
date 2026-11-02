using System.Collections;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Reveals scene-authored HUD in place with a smooth CanvasGroup fade.</summary>
    [AddComponentMenu("Mining Simulator/UI/Smooth Fade (Buttons and HUD)")]
    [DisallowMultipleComponent]
    public sealed class MiningUiSmoothFade : MonoBehaviour
    {
        // Retained for existing scenes and the cinematic setup tool. No direction moves UI.
        public enum SlideDirection { FromTop, FromBottom, FromLeft, FromRight }

        [Tooltip("Leave empty to fade this GameObject. Reuse on any UI button or HUD element.")]
        [SerializeField] private RectTransform target;
        [Tooltip("Fade automatically whenever this UI object becomes active.")]
        [SerializeField] private bool fadeOnEnable;
        [HideInInspector, SerializeField] private SlideDirection direction = SlideDirection.FromTop;
        [HideInInspector, SerializeField] private float slideDistance = 180f;
        [Min(0f), SerializeField] private float delay;
        [Min(0.05f), SerializeField] private float duration = 0.45f;

        private Coroutine routine;
        private CanvasGroup group;
        private float originalAlpha;
        private bool originalInteractable;
        private bool originalBlocksRaycasts;
        private bool hasOriginalState;

        public float TotalDuration => delay + duration;

        private void OnEnable()
        {
            if (fadeOnEnable) Play();
        }

        /// <summary>Fade in from the current opacity; safe to call while fading out.</summary>
        public void Show()
        {
            if (!EnsureGroup() || !gameObject.activeInHierarchy) return;
            if (routine != null) StopCoroutine(routine);
            group.interactable = false;
            group.blocksRaycasts = false;
            routine = StartCoroutine(FadeIn(group.alpha, 0f));
        }

        /// <summary>Fade this element out and stop it receiving clicks.</summary>
        public void Hide()
        {
            if (!EnsureGroup() || !gameObject.activeInHierarchy) return;
            if (routine != null) StopCoroutine(routine);
            if (!hasOriginalState)
            {
                originalAlpha = group.alpha;
                originalInteractable = group.interactable;
                originalBlocksRaycasts = group.blocksRaycasts;
                hasOriginalState = true;
            }
            group.interactable = false;
            group.blocksRaycasts = false;
            routine = StartCoroutine(FadeOut());
        }

        public void Configure(RectTransform authoredTarget, SlideDirection unusedDirection,
            float unusedDistance, float startDelay, float animationDuration)
        {
            target = authoredTarget;
            // Keep old serialized values so running the existing setup tool remains harmless.
            direction = unusedDirection;
            slideDistance = unusedDistance;
            delay = Mathf.Max(0f, startDelay);
            duration = Mathf.Max(0.05f, animationDuration);
        }

        public void Play()
        {
            if (!EnsureGroup() || !gameObject.activeInHierarchy) return;
            if (routine != null) StopCoroutine(routine);
            if (!hasOriginalState)
            {
                originalAlpha = group.alpha;
                originalInteractable = group.interactable;
                originalBlocksRaycasts = group.blocksRaycasts;
                hasOriginalState = true;
            }
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            routine = StartCoroutine(FadeIn(0f, delay));
        }

        public void StopAndRestore()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            if (!hasOriginalState) return;
            hasOriginalState = false;
            if (group == null) return;
            group.alpha = originalAlpha;
            group.interactable = originalInteractable;
            group.blocksRaycasts = originalBlocksRaycasts;
        }

        public void CompleteImmediately()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            hasOriginalState = false;
            if (!EnsureGroup()) return;
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        public void HideImmediately()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            hasOriginalState = false;
            if (!EnsureGroup()) return;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private void OnDisable() => StopAndRestore();

        private bool EnsureGroup()
        {
            if (target == null) target = transform as RectTransform;
            if (target == null) return false;
            if (group == null || group.transform != target)
                group = target.GetComponent<CanvasGroup>() ?? target.gameObject.AddComponent<CanvasGroup>();
            return true;
        }

        private IEnumerator FadeOut()
        {
            float start = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, 0f, Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            group.alpha = 0f;
            routine = null;
        }

        private IEnumerator FadeIn(float startAlpha, float startDelay)
        {
            float waited = 0f;
            while (waited < startDelay)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(startAlpha, 1f,
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            routine = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            delay = Mathf.Max(0f, delay);
            duration = Mathf.Max(0.05f, duration);
        }
#endif
    }
}
