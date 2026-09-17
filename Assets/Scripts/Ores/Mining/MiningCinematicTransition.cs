using System.Collections;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Presentation-only Main Menu to gameplay cinematic. MiningMainMenu remains responsible for
    /// pausing, input, saving and changing the actual menu/gameplay state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MiningCinematicTransition : MonoBehaviour
    {
        [Header("Main Menu")]
        [SerializeField] private CanvasGroup mainMenuCanvasGroup;
        [SerializeField] private CanvasGroup menuPresentationCanvasGroup;
        [SerializeField] private RectTransform menuPresentationRect;
        [SerializeField] private RectTransform logoRect;
        [SerializeField] private RectTransform[] menuButtons;

        [Header("Showcase Background")]
        [SerializeField] private MiningMainMenuShowcaseMotion showcaseMotion;
        [SerializeField] private RectTransform backgroundRect;
        [Min(1f), SerializeField] private float backgroundZoom = 2.6f;
        [Min(0f), SerializeField] private float rumbleStrength = 5f;

        [Header("Camera Zoom")]
        [SerializeField] private Camera mainCamera;
        [Range(15f, 90f), SerializeField] private float targetFieldOfView = 40f;

        [Header("Radiant Transition")]
        [SerializeField] private UnityEngine.UI.Image flashOverlay;
        [SerializeField] private CanvasGroup flashCanvasGroup;
        [SerializeField] private RectTransform flareRect;
        [SerializeField] private CanvasGroup flareCanvasGroup;
        [SerializeField] private RectTransform raysRect;
        [SerializeField] private CanvasGroup raysCanvasGroup;
        [SerializeField] private Color flashColor = new(1f, 0.86f, 1f, 1f);
        [Range(0.1f, 1f), SerializeField] private float flashOpacity = 1f;

        [Header("HUD Fly In")]
        [SerializeField] private MiningHudFlyIn[] hudFlyIns;

        [Header("Timing")]
        [Min(0.1f), SerializeField] private float menuExitDuration = 0.4f;
        [Min(0.1f), SerializeField] private float cameraZoomDuration = 0.85f;
        [Min(0f), SerializeField] private float flashStartDelay = 0.45f;
        [Min(0.05f), SerializeField] private float flashInDuration = 0.15f;
        [Min(0f), SerializeField] private float handoffDelayAfterFlash = 0.25f;
        [Min(0f), SerializeField] private float flashHoldAfterHandoff = 0.4f;
        [Min(0.05f), SerializeField] private float flashOutDuration = 1.35f;

        private MiningMainMenu owner;
        private Coroutine routine;
        private float menuAlpha = 1f;
        private float presentationAlpha = 1f;
        private Vector3 presentationScale = Vector3.one;
        private Vector2 backgroundPosition;
        private Vector3 backgroundScale = Vector3.one;
        private float cameraFieldOfView;
        private bool cached;

        public bool TryPlay(MiningMainMenu menu)
        {
            if (routine != null)
            {
                return true;
            }
            if (menu == null || mainMenuCanvasGroup == null || flashCanvasGroup == null ||
                flashOverlay == null)
            {
                return false;
            }

            owner = menu;
            showcaseMotion?.BeginCinematic();
            CacheAuthoredState();
            PrepareOverlay();
            routine = StartCoroutine(Run());
            return true;
        }

        public void ResetImmediate()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
            RestoreAuthoredState();
            HideOverlay();
            StopHudAnimations();
            owner = null;
        }

        private void OnDisable()
        {
            ResetImmediate();
        }

        private IEnumerator Run()
        {
            // Reference timing: 0.45 seconds of build-up, then a 0.15 second radiant whiteout.
            yield return AnimateBuildUp(0f, flashStartDelay, false);
            float handoffTime = flashStartDelay + handoffDelayAfterFlash;
            yield return AnimateBuildUp(flashStartDelay, handoffTime, true);
            flashCanvasGroup.alpha = flashOpacity;

            // Swap to gameplay only while the cover is fully opaque. Buttons are never moved,
            // resized or restyled by this component.
            owner?.CompleteCinematicPlay();
            RestoreAuthoredState();
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null;

            yield return HoldRadiance(handoffTime, flashHoldAfterHandoff);
            PlayHudAnimations();
            float revealDuration = Mathf.Max(flashOutDuration, GetHudRevealDuration());
            yield return FadeRadiance(revealDuration);
            CompleteHudAnimations();
            Canvas.ForceUpdateCanvases();
            HideOverlay();
            owner = null;
            routine = null;
        }

        private void CacheAuthoredState()
        {
            menuAlpha = mainMenuCanvasGroup.alpha;
            if (menuPresentationCanvasGroup != null)
            {
                presentationAlpha = menuPresentationCanvasGroup.alpha;
            }
            if (menuPresentationRect != null)
            {
                presentationScale = menuPresentationRect.localScale;
            }
            if (backgroundRect != null)
            {
                backgroundPosition = backgroundRect.anchoredPosition;
                backgroundScale = backgroundRect.localScale;
            }
            if (mainCamera != null)
            {
                cameraFieldOfView = mainCamera.fieldOfView;
            }
            cached = true;
        }

        private IEnumerator AnimateBuildUp(float startTime, float endTime, bool animateFlash)
        {
            float elapsed = startTime;
            while (elapsed < endTime)
            {
                elapsed = Mathf.Min(endTime, elapsed + Time.unscaledDeltaTime);
                AnimateMenuPresentation(elapsed);
                AnimateBackground(elapsed);
                AnimateCamera(elapsed);
                AnimateRadiance(elapsed);
                if (animateFlash)
                {
                    float flashT = Mathf.Clamp01((elapsed - flashStartDelay) / flashInDuration);
                    flashCanvasGroup.alpha = Mathf.Lerp(0f, flashOpacity,
                        EaseOutCubic(flashT));
                }
                yield return null;
            }
        }

        private IEnumerator HoldRadiance(float startTime, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                AnimateRadiance(startTime + elapsed);
                flashCanvasGroup.alpha = flashOpacity;
                yield return null;
            }
        }

        private void AnimateMenuPresentation(float elapsed)
        {
            float fadeT = Mathf.Clamp01(elapsed / Mathf.Max(menuExitDuration, 0.01f));
            float eased = EaseOutCubic(fadeT);
            if (menuPresentationCanvasGroup != null)
            {
                menuPresentationCanvasGroup.alpha = Mathf.Lerp(presentationAlpha, 0f, eased);
            }
            else
            {
                mainMenuCanvasGroup.alpha = Mathf.Lerp(menuAlpha, 0f, eased);
            }
            if (menuPresentationRect != null)
            {
                menuPresentationRect.localScale = Vector3.LerpUnclamped(presentationScale,
                    presentationScale * 0.92f, eased);
            }
        }

        private void AnimateBackground(float elapsed)
        {
            if (backgroundRect == null)
            {
                return;
            }

            float t = Mathf.Clamp01(elapsed / cameraZoomDuration);
            float eased = EaseInOutQuad(t);
            float width = Mathf.Max(1f, backgroundRect.rect.width);
            float height = Mathf.Max(1f, backgroundRect.rect.height);
            Vector2 dive = new(-width * 0.03f, -height * 0.02f);
            float rumbleT = Mathf.Clamp01(elapsed / 0.75f);
            float strength = rumbleStrength * (1f - rumbleT);
            Vector2 rumble = new(Mathf.Sin(elapsed * 67f), Mathf.Sin(elapsed * 83f + 1.3f));
            backgroundRect.anchoredPosition = Vector2.LerpUnclamped(backgroundPosition,
                backgroundPosition + dive, eased) + rumble * strength;
            backgroundRect.localScale = Vector3.LerpUnclamped(backgroundScale,
                backgroundScale * backgroundZoom, eased);
        }

        private void AnimateRadiance(float elapsed)
        {
            float t = Mathf.Clamp01(elapsed / cameraZoomDuration);
            float eased = EaseOutCubic(t);
            if (flareCanvasGroup != null)
            {
                flareCanvasGroup.alpha = eased;
            }
            if (flareRect != null)
            {
                flareRect.localScale = Vector3.one * Mathf.Lerp(0.05f, 1.15f, eased);
            }
            if (raysCanvasGroup != null)
            {
                raysCanvasGroup.alpha = Mathf.Lerp(0f, 0.9f, eased);
            }
            if (raysRect != null)
            {
                raysRect.localRotation = Quaternion.Euler(0f, 0f, elapsed * 115f);
            }
        }

        private void AnimateCamera(float elapsed)
        {
            if (mainCamera == null)
            {
                return;
            }
            float t = Mathf.Clamp01(elapsed / cameraZoomDuration);
            mainCamera.fieldOfView = Mathf.Lerp(cameraFieldOfView, targetFieldOfView,
                EaseInOutQuad(t));
        }

        private IEnumerator FadeRadiance(float duration)
        {
            float elapsed = 0f;
            float flareStart = flareCanvasGroup != null ? flareCanvasGroup.alpha : 0f;
            float raysStart = raysCanvasGroup != null ? raysCanvasGroup.alpha : 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutCubic(t);
                flashCanvasGroup.alpha = Mathf.Lerp(flashOpacity, 0f, eased);
                if (flareCanvasGroup != null)
                {
                    flareCanvasGroup.alpha = Mathf.Lerp(flareStart, 0f, eased);
                }
                if (raysCanvasGroup != null)
                {
                    raysCanvasGroup.alpha = Mathf.Lerp(raysStart, 0f, eased);
                }
                if (raysRect != null)
                {
                    raysRect.Rotate(0f, 0f, Time.unscaledDeltaTime * 90f);
                }
                yield return null;
            }
            flashCanvasGroup.alpha = 0f;
        }

        private void PrepareOverlay()
        {
            flashOverlay.color = flashColor;
            flashOverlay.gameObject.SetActive(true);
            flashCanvasGroup.alpha = 0f;
            flashCanvasGroup.interactable = true;
            flashCanvasGroup.blocksRaycasts = true;
            if (flareCanvasGroup != null)
            {
                flareCanvasGroup.alpha = 0f;
            }
            if (raysCanvasGroup != null)
            {
                raysCanvasGroup.alpha = 0f;
            }
        }

        private void HideOverlay()
        {
            if (flashCanvasGroup == null)
            {
                return;
            }
            flashCanvasGroup.alpha = 0f;
            flashCanvasGroup.interactable = false;
            flashCanvasGroup.blocksRaycasts = false;
            if (flareCanvasGroup != null)
            {
                flareCanvasGroup.alpha = 0f;
            }
            if (raysCanvasGroup != null)
            {
                raysCanvasGroup.alpha = 0f;
            }
        }

        private void RestoreAuthoredState()
        {
            if (!cached)
            {
                return;
            }
            if (mainMenuCanvasGroup != null)
            {
                mainMenuCanvasGroup.alpha = menuAlpha;
            }
            if (menuPresentationCanvasGroup != null)
            {
                menuPresentationCanvasGroup.alpha = presentationAlpha;
            }
            if (menuPresentationRect != null)
            {
                menuPresentationRect.localScale = presentationScale;
            }
            if (backgroundRect != null)
            {
                backgroundRect.anchoredPosition = backgroundPosition;
                backgroundRect.localScale = backgroundScale;
            }
            if (mainCamera != null)
            {
                mainCamera.fieldOfView = cameraFieldOfView;
            }
            showcaseMotion?.EndCinematic();
            cached = false;
        }

        private void PlayHudAnimations()
        {
            if (hudFlyIns == null)
            {
                return;
            }
            foreach (MiningHudFlyIn flyIn in hudFlyIns)
            {
                flyIn?.Play();
            }
        }

        private void StopHudAnimations()
        {
            if (hudFlyIns == null)
            {
                return;
            }
            foreach (MiningHudFlyIn flyIn in hudFlyIns)
            {
                flyIn?.StopAndRestore();
            }
        }

        private float GetHudRevealDuration()
        {
            float result = 0f;
            if (hudFlyIns == null)
            {
                return result;
            }
            foreach (MiningHudFlyIn flyIn in hudFlyIns)
            {
                if (flyIn != null)
                {
                    result = Mathf.Max(result, flyIn.TotalDuration);
                }
            }
            return result;
        }

        private void CompleteHudAnimations()
        {
            if (hudFlyIns == null)
            {
                return;
            }
            foreach (MiningHudFlyIn flyIn in hudFlyIns)
            {
                flyIn?.CompleteImmediately();
            }
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        private static float EaseInOutQuad(float t) => t < 0.5f
            ? 2f * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            menuExitDuration = Mathf.Max(0.1f, menuExitDuration);
            cameraZoomDuration = Mathf.Max(0.1f, cameraZoomDuration);
            backgroundZoom = Mathf.Max(1f, backgroundZoom);
            rumbleStrength = Mathf.Max(0f, rumbleStrength);
            flashStartDelay = Mathf.Max(0f, flashStartDelay);
            flashInDuration = Mathf.Max(0.05f, flashInDuration);
            handoffDelayAfterFlash = Mathf.Max(flashInDuration, handoffDelayAfterFlash);
            flashHoldAfterHandoff = Mathf.Max(0f, flashHoldAfterHandoff);
            flashOutDuration = Mathf.Max(0.05f, flashOutDuration);
            flashOpacity = 1f;
        }
#endif
    }
}
