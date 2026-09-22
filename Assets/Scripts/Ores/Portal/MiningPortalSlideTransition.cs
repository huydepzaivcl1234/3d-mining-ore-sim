using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Masks Ground/Underground swaps with a camera plunge and a runtime-authored slide curtain.
    /// The overlay is created in the active scene so no authored Canvas layout is replaced.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MiningWorldAreaController))]
    public sealed class MiningPortalSlideTransition : MonoBehaviour
    {
        private const string OverlayName = "Portal Slide Transition Overlay";

        [Header("Timing")]
        [Min(0.1f), SerializeField] private float diveDuration = 0.65f;
        [Min(0f), SerializeField] private float coveredSwapHold = 0.08f;
        [Min(0.1f), SerializeField] private float landingDuration = 0.55f;

        [Header("Camera")]
        [Range(60f, 100f), SerializeField] private float tunnelFieldOfView = 78f;
        [Min(1f), SerializeField] private float plungeDistance = 12f;
        [Min(1f), SerializeField] private float cavernDropHeight = 16f;
        [Min(0f), SerializeField] private float rumbleStrength = 0.12f;
        [Min(0f), SerializeField] private float landingShakeStrength = 0.22f;

        [Header("Curtain")]
        [SerializeField] private Color curtainColor = new(0.018f, 0.022f, 0.032f, 1f);
        [SerializeField] private Color cyanEdgeColor = new(0.13f, 0.88f, 1f, 1f);
        [SerializeField] private Color purpleEdgeColor = new(0.58f, 0.25f, 1f, 0.85f);

        private MiningWorldAreaController areaController;
        private MiningOrbitCamera orbitCamera;
        private MiningAudioManager audioManager;
        private Camera targetCamera;
        private Coroutine transitionRoutine;
        private CanvasGroup overlayGroup;
        private RectTransform overlayRoot;
        private RectTransform curtain;
        private RectTransform leadingEdge;
        private RectTransform speedLines;
        private CanvasGroup speedLinesGroup;
        private float authoredFieldOfView = 60f;
        private Vector3 recoveryFocus;

        public bool IsPlaying => transitionRoutine != null;

        public static MiningPortalSlideTransition EnsureRuntime(
            MiningWorldAreaController controller)
        {
            if (controller == null)
            {
                return null;
            }

            MiningPortalSlideTransition transition =
                controller.GetComponent<MiningPortalSlideTransition>();
            return transition != null
                ? transition
                : controller.gameObject.AddComponent<MiningPortalSlideTransition>();
        }

        private void Awake()
        {
            areaController = GetComponent<MiningWorldAreaController>();
        }

        public bool TryPlay(MiningWorldArea destination, Transform portalAnchor = null)
        {
            if (transitionRoutine != null || areaController == null ||
                destination == areaController.CurrentArea)
            {
                return false;
            }

            ResolveReferences();
            if (orbitCamera == null || targetCamera == null)
            {
                return false;
            }

            Vector3 portalPosition = portalAnchor != null
                ? portalAnchor.position
                : areaController.GetCameraFocus(destination, orbitCamera.DefaultFocusPoint);
            transitionRoutine = StartCoroutine(Run(destination, portalPosition));
            return true;
        }

        private IEnumerator Run(MiningWorldArea destination, Vector3 portalPosition)
        {
            BuildOverlayIfNeeded();
            bool goingDown = destination == MiningWorldArea.Underground;
            authoredFieldOfView = targetCamera.fieldOfView;
            recoveryFocus = orbitCamera.FocusPoint;
            orbitCamera.BeginCinematicOverride();
            PrepareOverlay(goingDown);

            Vector3 startPosition = targetCamera.transform.position;
            Quaternion startRotation = targetCamera.transform.rotation;
            Vector3 travelDirection = goingDown ? Vector3.down : Vector3.up;
            Vector3 diveEndPosition = portalPosition + travelDirection * plungeDistance;
            Quaternion diveEndRotation = Quaternion.LookRotation(travelDirection, Vector3.forward);

            PlayRumble();
            yield return AnimateDive(startPosition, startRotation, diveEndPosition,
                diveEndRotation, goingDown);

            areaController.CompleteAnimatedSwitch(destination);
            Canvas.ForceUpdateCanvases();
            if (coveredSwapHold > 0f)
            {
                yield return WaitUnscaled(coveredSwapHold);
            }

            Vector3 destinationFocus = areaController.GetCameraFocus(destination,
                orbitCamera.DefaultFocusPoint);
            orbitCamera.GetGameplayPose(destinationFocus, out Vector3 gameplayPosition,
                out Quaternion gameplayRotation);
            Vector3 arrivalDirection = goingDown ? Vector3.up : Vector3.down;
            Vector3 arrivalStart = destinationFocus + arrivalDirection * cavernDropHeight;
            Quaternion arrivalRotation = Quaternion.LookRotation(
                (destinationFocus - arrivalStart).normalized, Vector3.forward);
            orbitCamera.SetCinematicPose(arrivalStart, arrivalRotation, tunnelFieldOfView);

            yield return AnimateArrival(arrivalStart, arrivalRotation, gameplayPosition,
                gameplayRotation, goingDown);

            orbitCamera.SetCinematicPose(gameplayPosition, gameplayRotation, authoredFieldOfView);
            orbitCamera.EndCinematicOverride(destinationFocus);
            orbitCamera.PlayRewardShake(landingShakeStrength, 0.32f, 25f);
            PlayLanding();
            HideOverlay();
            recoveryFocus = destinationFocus;
            transitionRoutine = null;
        }

        private IEnumerator AnimateDive(Vector3 startPosition, Quaternion startRotation,
            Vector3 endPosition, Quaternion endRotation, bool goingDown)
        {
            float elapsed = 0f;
            float travel = GetCurtainTravel();
            float curtainStart = goingDown ? travel : -travel;
            while (elapsed < diveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / diveDuration);
                float eased = EaseInCubic(t);
                float rumbleEnvelope = Mathf.Sin(t * Mathf.PI);
                Vector3 rumble = startRotation * new Vector3(
                    Mathf.Sin(elapsed * 73f), Mathf.Sin(elapsed * 91f + 1.7f), 0f) *
                    (rumbleStrength * rumbleEnvelope);
                Vector3 position = Vector3.LerpUnclamped(startPosition, endPosition, eased) +
                                   rumble;
                Quaternion rotation = Quaternion.Slerp(startRotation, endRotation, eased);
                float fov = Mathf.Lerp(authoredFieldOfView, tunnelFieldOfView, eased);
                orbitCamera.SetCinematicPose(position, rotation, fov);
                curtain.anchoredPosition = new Vector2(0f,
                    Mathf.LerpUnclamped(curtainStart, 0f, eased));
                speedLinesGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
                AnimateSpeedLines(elapsed, goingDown);
                yield return null;
            }

            curtain.anchoredPosition = Vector2.zero;
            speedLinesGroup.alpha = 0f;
        }

        private IEnumerator AnimateArrival(Vector3 startPosition, Quaternion startRotation,
            Vector3 endPosition, Quaternion endRotation, bool goingDown)
        {
            float elapsed = 0f;
            float curtainEnd = goingDown ? -GetCurtainTravel() : GetCurtainTravel();
            while (elapsed < landingDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / landingDuration);
                float cameraEase = EaseOutBack(t);
                float curtainEase = EaseOutCubic(t);
                Vector3 position = Vector3.LerpUnclamped(startPosition, endPosition,
                    cameraEase);
                Quaternion rotation = Quaternion.Slerp(startRotation, endRotation,
                    Mathf.Clamp01(cameraEase));
                float fov = Mathf.Lerp(tunnelFieldOfView, authoredFieldOfView,
                    EaseOutCubic(t));
                orbitCamera.SetCinematicPose(position, rotation, fov);
                curtain.anchoredPosition = new Vector2(0f,
                    Mathf.LerpUnclamped(0f, curtainEnd, curtainEase));
                yield return null;
            }
        }

        private void ResolveReferences()
        {
            orbitCamera ??= FindFirstObjectByType<MiningOrbitCamera>(FindObjectsInactive.Include);
            targetCamera = orbitCamera != null ? orbitCamera.ControlledCamera : Camera.main;
            audioManager ??= FindFirstObjectByType<MiningAudioManager>(FindObjectsInactive.Include);
        }

        private void BuildOverlayIfNeeded()
        {
            if (overlayRoot != null)
            {
                return;
            }

            GameObject canvasObject = new(OverlayName, typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue - 8;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            overlayRoot = canvasObject.GetComponent<RectTransform>();
            overlayGroup = canvasObject.GetComponent<CanvasGroup>();

            speedLines = CreateStretchRect("Cyan Speed Lines", overlayRoot);
            speedLinesGroup = speedLines.gameObject.AddComponent<CanvasGroup>();
            for (int index = 0; index < 7; index++)
            {
                RectTransform line = CreateRect($"Speed Streak {index + 1}", speedLines);
                float x = Mathf.Lerp(-760f, 760f, index / 6f);
                SetCentered(line, new Vector2(x, index % 2 == 0 ? 260f : -220f),
                    new Vector2(index % 3 == 0 ? 7f : 3f, 760f));
                Image image = line.gameObject.AddComponent<Image>();
                image.color = index % 2 == 0
                    ? new Color(cyanEdgeColor.r, cyanEdgeColor.g, cyanEdgeColor.b, 0.35f)
                    : new Color(purpleEdgeColor.r, purpleEdgeColor.g, purpleEdgeColor.b, 0.42f);
                image.raycastTarget = false;
            }

            curtain = CreateStretchRect("Dark Bedrock Curtain", overlayRoot);
            Image curtainImage = curtain.gameObject.AddComponent<Image>();
            curtainImage.color = curtainColor;
            curtainImage.raycastTarget = true;

            for (int index = 0; index < 3; index++)
            {
                RectTransform seam = CreateRect($"Bedrock Seam {index + 1}", curtain);
                SetCentered(seam, new Vector2((index - 1) * 150f, (index - 1) * 105f),
                    new Vector2(index == 1 ? 980f : 1240f, 5f));
                Image seamImage = seam.gameObject.AddComponent<Image>();
                seamImage.color = new Color(0.28f, 0.31f, 0.36f, 0.26f);
                seamImage.raycastTarget = false;
            }

            leadingEdge = CreateRect("Glowing Cyan Leading Edge", curtain);
            Image edgeImage = leadingEdge.gameObject.AddComponent<Image>();
            edgeImage.color = cyanEdgeColor;
            edgeImage.raycastTarget = false;
            HideOverlay();
        }

        private void PrepareOverlay(bool goingDown)
        {
            overlayRoot.gameObject.SetActive(true);
            overlayGroup.alpha = 1f;
            overlayGroup.blocksRaycasts = true;
            overlayGroup.interactable = true;
            speedLinesGroup.alpha = 0f;
            float travel = GetCurtainTravel();
            curtain.anchoredPosition = new Vector2(0f, goingDown ? travel : -travel);

            leadingEdge.anchorMin = new Vector2(0f, goingDown ? 0f : 1f);
            leadingEdge.anchorMax = new Vector2(1f, goingDown ? 0f : 1f);
            leadingEdge.pivot = new Vector2(0.5f, goingDown ? 0f : 1f);
            leadingEdge.anchoredPosition = Vector2.zero;
            leadingEdge.sizeDelta = new Vector2(0f, 18f);
        }

        private void HideOverlay()
        {
            if (overlayGroup == null)
            {
                return;
            }

            overlayGroup.alpha = 0f;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;
            speedLinesGroup.alpha = 0f;
            overlayRoot.gameObject.SetActive(false);
        }

        private void AnimateSpeedLines(float elapsed, bool goingDown)
        {
            float direction = goingDown ? -1f : 1f;
            float offset = Mathf.Repeat(elapsed * 1450f, 1080f) - 540f;
            speedLines.anchoredPosition = new Vector2(0f, offset * direction);
        }

        private float GetCurtainTravel()
        {
            Canvas.ForceUpdateCanvases();
            return Mathf.Max(1200f, overlayRoot != null ? overlayRoot.rect.height + 80f : 0f);
        }

        private void PlayRumble()
        {
            if (audioManager != null && audioManager.AudioData != null)
            {
                audioManager.PlaySfx(audioManager.AudioData.WheelSpinSfx, 0.7f);
            }
        }

        private void PlayLanding()
        {
            if (audioManager != null && audioManager.AudioData != null)
            {
                audioManager.PlaySfx(audioManager.AudioData.OreBreakSfx, 0.85f);
            }
        }

        private static IEnumerator WaitUnscaled(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void OnDisable()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            if (orbitCamera != null && orbitCamera.CinematicOverrideActive)
            {
                if (targetCamera != null)
                {
                    targetCamera.fieldOfView = authoredFieldOfView;
                }
                orbitCamera.EndCinematicOverride(recoveryFocus);
            }
            HideOverlay();
        }

        private void OnDestroy()
        {
            if (overlayRoot != null)
            {
                Destroy(overlayRoot.gameObject);
            }
        }

        private void OnValidate()
        {
            diveDuration = Mathf.Max(0.1f, diveDuration);
            coveredSwapHold = Mathf.Max(0f, coveredSwapHold);
            landingDuration = Mathf.Max(0.1f, landingDuration);
            plungeDistance = Mathf.Max(1f, plungeDistance);
            cavernDropHeight = Mathf.Max(1f, cavernDropHeight);
            rumbleStrength = Mathf.Max(0f, rumbleStrength);
            landingShakeStrength = Mathf.Max(0f, landingShakeStrength);
        }

        private static RectTransform CreateStretchRect(string objectName, Transform parent)
        {
            RectTransform rect = CreateRect(objectName, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject gameObject = new(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static void SetCentered(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static float EaseInCubic(float value) => value * value * value;

        private static float EaseOutCubic(float value)
        {
            float inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }

        // Same overshoot curve used by Ease.OutBack for the ceiling drop and landing bounce.
        private static float EaseOutBack(float value)
        {
            const float overshoot = 1.70158f;
            float shifted = value - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted +
                   overshoot * shifted * shifted;
        }
    }
}
