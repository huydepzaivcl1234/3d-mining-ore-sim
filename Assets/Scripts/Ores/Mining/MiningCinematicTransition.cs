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
        [SerializeField] private RectTransform logoRect;
        [SerializeField] private RectTransform[] menuButtons;

        [Header("Camera Zoom")]
        [SerializeField] private Camera mainCamera;
        [Range(15f, 90f), SerializeField] private float targetFieldOfView = 40f;

        [Header("Flash Overlay")]
        [SerializeField] private UnityEngine.UI.Image flashOverlay;
        [SerializeField] private CanvasGroup flashCanvasGroup;
        [SerializeField] private Color flashColor = new(0.18f, 0.035f, 0.24f, 1f);
        [Range(0.1f, 1f), SerializeField] private float flashOpacity = 0.94f;

        [Header("HUD Fly In")]
        [SerializeField] private MiningHudFlyIn[] hudFlyIns;

        [Header("Timing")]
        [Min(0.1f), SerializeField] private float menuExitDuration = 0.38f;
        [Min(0.1f), SerializeField] private float cameraZoomDuration = 0.85f;
        [Min(0f), SerializeField] private float buttonStagger = 0.055f;
        [Min(100f), SerializeField] private float buttonSlideDistance = 650f;
        [Min(50f), SerializeField] private float logoFlyDistance = 220f;
        [Min(0.05f), SerializeField] private float flashInDuration = 0.24f;
        [Min(0f), SerializeField] private float flashHoldDuration = 0.12f;
        [Min(0.05f), SerializeField] private float flashOutDuration = 0.32f;

        private MiningMainMenu owner;
        private Coroutine routine;
        private Vector2 logoPosition;
        private Vector2[] buttonPositions;
        private float menuAlpha = 1f;
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
            float elapsed = 0f;
            while (elapsed < cameraZoomDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                AnimateMenu(elapsed);
                AnimateCamera(elapsed);
                yield return null;
            }

            AnimateMenu(cameraZoomDuration);
            AnimateCamera(cameraZoomDuration);
            yield return FadeFlash(0f, flashOpacity, flashInDuration, EaseOutCubic);

            float hold = 0f;
            while (hold < flashHoldDuration)
            {
                hold += Time.unscaledDeltaTime;
                yield return null;
            }

            // Restore authored transforms while the opaque flash hides the swap. This guarantees
            // that reopening the Main Menu starts from the designer-authored Scene layout.
            RestoreAuthoredState();
            owner?.CompleteCinematicPlay();
            PlayHudAnimations();

            yield return FadeFlash(flashOpacity, 0f, flashOutDuration, EaseInCubic);
            HideOverlay();
            owner = null;
            routine = null;
        }

        private void CacheAuthoredState()
        {
            menuAlpha = mainMenuCanvasGroup.alpha;
            if (logoRect != null)
            {
                logoPosition = logoRect.anchoredPosition;
            }
            int count = menuButtons != null ? menuButtons.Length : 0;
            buttonPositions = new Vector2[count];
            for (int index = 0; index < count; index++)
            {
                if (menuButtons[index] != null)
                {
                    buttonPositions[index] = menuButtons[index].anchoredPosition;
                }
            }
            if (mainCamera != null)
            {
                cameraFieldOfView = mainCamera.fieldOfView;
            }
            cached = true;
        }

        private void AnimateMenu(float elapsed)
        {
            float logoT = Mathf.Clamp01(elapsed / menuExitDuration);
            if (logoRect != null)
            {
                logoRect.anchoredPosition = Vector2.LerpUnclamped(logoPosition,
                    logoPosition + Vector2.up * logoFlyDistance, EaseOutCubic(logoT));
            }

            int count = menuButtons != null ? menuButtons.Length : 0;
            for (int index = 0; index < count; index++)
            {
                RectTransform button = menuButtons[index];
                if (button == null)
                {
                    continue;
                }
                float localT = Mathf.Clamp01((elapsed - index * buttonStagger) /
                                             menuExitDuration);
                button.anchoredPosition = Vector2.LerpUnclamped(buttonPositions[index],
                    buttonPositions[index] + Vector2.left * buttonSlideDistance,
                    EaseInCubic(localT));
            }

            float fadeT = Mathf.Clamp01(elapsed / Mathf.Max(menuExitDuration, 0.01f));
            mainMenuCanvasGroup.alpha = Mathf.Lerp(menuAlpha, 0f, fadeT);
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

        private IEnumerator FadeFlash(float from, float to, float duration,
            System.Func<float, float> easing)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                flashCanvasGroup.alpha = Mathf.Lerp(from, to, easing(t));
                yield return null;
            }
            flashCanvasGroup.alpha = to;
        }

        private void PrepareOverlay()
        {
            flashOverlay.color = flashColor;
            flashOverlay.gameObject.SetActive(true);
            flashCanvasGroup.alpha = 0f;
            flashCanvasGroup.interactable = true;
            flashCanvasGroup.blocksRaycasts = true;
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
            if (logoRect != null)
            {
                logoRect.anchoredPosition = logoPosition;
            }
            int count = Mathf.Min(menuButtons != null ? menuButtons.Length : 0,
                buttonPositions != null ? buttonPositions.Length : 0);
            for (int index = 0; index < count; index++)
            {
                if (menuButtons[index] != null)
                {
                    menuButtons[index].anchoredPosition = buttonPositions[index];
                }
            }
            if (mainCamera != null)
            {
                mainCamera.fieldOfView = cameraFieldOfView;
            }
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

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        private static float EaseInCubic(float t) => t * t * t;
        private static float EaseInOutQuad(float t) => t < 0.5f
            ? 2f * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            menuExitDuration = Mathf.Max(0.1f, menuExitDuration);
            cameraZoomDuration = Mathf.Max(0.1f, cameraZoomDuration);
            buttonStagger = Mathf.Max(0f, buttonStagger);
            buttonSlideDistance = Mathf.Max(100f, buttonSlideDistance);
            logoFlyDistance = Mathf.Max(50f, logoFlyDistance);
            flashInDuration = Mathf.Max(0.05f, flashInDuration);
            flashHoldDuration = Mathf.Max(0f, flashHoldDuration);
            flashOutDuration = Mathf.Max(0.05f, flashOutDuration);
        }
#endif
    }
}
