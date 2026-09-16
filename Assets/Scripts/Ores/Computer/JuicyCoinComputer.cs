using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Presentation-only animation for the Coin Computer panel. MiningComputerPanel remains
    /// the sole owner of gameplay state, localization, upgrades, opening and closing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JuicyCoinComputer : MonoBehaviour
    {
        [Header("Authored Presentation")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform emblemRect;
        [SerializeField] private RectTransform upgradeButtonRect;
        [SerializeField] private RectTransform shimmerRect;
        [SerializeField] private Image levelFill;

        [Header("Animation Only")]
        [Min(0.05f), SerializeField] private float showDuration = 0.3f;
        [Range(0.5f, 1f), SerializeField] private float startScale = 0.84f;
        [Min(0.5f), SerializeField] private float shimmerInterval = 2.8f;
        [Min(0.1f), SerializeField] private float shimmerDuration = 0.7f;

        private Coroutine presentationRoutine;
        private Coroutine upgradeRoutine;
        private Vector3 panelAuthoredScale = Vector3.one;
        private Vector3 emblemAuthoredScale = Vector3.one;
        private Vector3 buttonAuthoredScale = Vector3.one;
        private bool scalesCached;

        public void Configure(RectTransform panel, CanvasGroup group, RectTransform emblem,
            RectTransform upgradeButton, RectTransform shimmer, Image progressFill)
        {
            panelRect = panel;
            canvasGroup = group;
            emblemRect = emblem;
            upgradeButtonRect = upgradeButton;
            shimmerRect = shimmer;
            levelFill = progressFill;
            scalesCached = false;
            CacheScales();
        }

        public void SetLevelProgress(int currentLevel, int maximumLevel)
        {
            if (levelFill != null)
            {
                levelFill.fillAmount = maximumLevel > 0
                    ? Mathf.Clamp01((float)currentLevel / maximumLevel)
                    : 0f;
            }
        }

        public void PlayUpgradeFeedback()
        {
            if (!isActiveAndEnabled || upgradeButtonRect == null)
            {
                return;
            }
            if (upgradeRoutine != null)
            {
                StopCoroutine(upgradeRoutine);
            }
            upgradeRoutine = StartCoroutine(AnimateUpgrade());
        }

        private void Awake()
        {
            ResolveReferences();
            CacheScales();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CacheScales();
            StopPresentation();
            presentationRoutine = StartCoroutine(AnimatePresentation());
        }

        private void OnDisable()
        {
            StopPresentation();
            if (upgradeRoutine != null)
            {
                StopCoroutine(upgradeRoutine);
                upgradeRoutine = null;
            }
            RestoreAuthoredTransforms();
        }

        private void ResolveReferences()
        {
            panelRect ??= transform as RectTransform;
            canvasGroup ??= GetComponent<CanvasGroup>();
        }

        private void CacheScales()
        {
            if (scalesCached)
            {
                return;
            }
            panelAuthoredScale = panelRect != null ? panelRect.localScale : Vector3.one;
            emblemAuthoredScale = emblemRect != null ? emblemRect.localScale : Vector3.one;
            buttonAuthoredScale = upgradeButtonRect != null
                ? upgradeButtonRect.localScale
                : Vector3.one;
            scalesCached = true;
        }

        private void StopPresentation()
        {
            if (presentationRoutine != null)
            {
                StopCoroutine(presentationRoutine);
                presentationRoutine = null;
            }
        }

        private IEnumerator AnimatePresentation()
        {
            float elapsed = 0f;
            if (panelRect != null)
            {
                panelRect.localScale = panelAuthoredScale * startScale;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            while (elapsed < showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / showDuration);
                if (panelRect != null)
                {
                    panelRect.localScale = Vector3.LerpUnclamped(
                        panelAuthoredScale * startScale, panelAuthoredScale, EaseOutBack(t));
                }
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = t;
                }
                yield return null;
            }

            if (panelRect != null)
            {
                panelRect.localScale = panelAuthoredScale;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            float shimmerClock = shimmerInterval;
            float emblemClock = 0f;
            while (true)
            {
                float delta = Time.unscaledDeltaTime;
                emblemClock += delta * 2.2f;
                if (emblemRect != null)
                {
                    float pulse = Mathf.Lerp(1f, 1.06f,
                        (Mathf.Sin(emblemClock) + 1f) * 0.5f);
                    emblemRect.localScale = emblemAuthoredScale * pulse;
                }

                shimmerClock += delta;
                if (shimmerRect != null && shimmerClock >= shimmerInterval)
                {
                    shimmerClock = 0f;
                    yield return AnimateShimmer();
                }
                yield return null;
            }
        }

        private IEnumerator AnimateShimmer()
        {
            Vector2 start = new(-260f, shimmerRect.anchoredPosition.y);
            Vector2 end = new(260f, shimmerRect.anchoredPosition.y);
            float elapsed = 0f;
            while (elapsed < shimmerDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / shimmerDuration);
                shimmerRect.anchoredPosition = Vector2.Lerp(start, end, SmoothStep(t));
                yield return null;
            }
            shimmerRect.anchoredPosition = start;
        }

        private IEnumerator AnimateUpgrade()
        {
            yield return ScaleTo(upgradeButtonRect, buttonAuthoredScale * 0.92f, 0.07f);
            yield return ScaleTo(upgradeButtonRect, buttonAuthoredScale * 1.06f, 0.1f);
            yield return ScaleTo(upgradeButtonRect, buttonAuthoredScale, 0.12f);
            upgradeRoutine = null;
        }

        private static IEnumerator ScaleTo(RectTransform target, Vector3 destination,
            float duration)
        {
            Vector3 origin = target.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.LerpUnclamped(origin, destination,
                    EaseOutCubic(Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            target.localScale = destination;
        }

        private void RestoreAuthoredTransforms()
        {
            if (panelRect != null) panelRect.localScale = panelAuthoredScale;
            if (emblemRect != null) emblemRect.localScale = emblemAuthoredScale;
            if (upgradeButtonRect != null) upgradeButtonRect.localScale = buttonAuthoredScale;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        private static float SmoothStep(float t) => t * t * (3f - 2f * t);
        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            return 1f + (overshoot + 1f) * Mathf.Pow(t - 1f, 3f) +
                   overshoot * Mathf.Pow(t - 1f, 2f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            showDuration = Mathf.Max(0.05f, showDuration);
            startScale = Mathf.Clamp(startScale, 0.5f, 1f);
            shimmerInterval = Mathf.Max(0.5f, shimmerInterval);
            shimmerDuration = Mathf.Max(0.1f, shimmerDuration);
        }
#endif
    }
}
