using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Plays a radial screen wipe for preview. Neither test command changes the world.
    /// Add this to a scene object and assign the existing portal and gameplay camera.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public sealed class MiningDynamicRadialMaskTransition : MonoBehaviour
    {
        private static readonly int CenterId = Shader.PropertyToID("_Center");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int PhaseId = Shader.PropertyToID("_Phase");
        private static readonly int EffectTimeId = Shader.PropertyToID("_EffectTime");
        private static readonly int MaskColorId = Shader.PropertyToID("_MaskColor");
        private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        private static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
        private static readonly int EdgeGlowId = Shader.PropertyToID("_EdgeGlow");
        private static readonly int NoiseFreqId = Shader.PropertyToID("_NoiseFreq");
        private static readonly int NoiseAmplitudeId = Shader.PropertyToID("_NoiseAmplitude");
        private static readonly int NoiseSpeedId = Shader.PropertyToID("_NoiseSpeed");

        [Header("Scene references")]
        [SerializeField] private Transform portalGate;
        [SerializeField] private Camera gameplayCamera;
        [Tooltip("Optional existing overlay Image. If absent, a Canvas and Image are created at runtime.")]
        [SerializeField] private Image overlayImage;
        [SerializeField] private Shader transitionShader;

        [Header("Timing (unscaled seconds)")]
        [Min(0.01f), SerializeField] private float coverDuration = 0.75f;
        [Min(0f), SerializeField] private float coveredPause = 0.10f;
        [Min(0.01f), SerializeField] private float revealDuration = 0.65f;

        [Header("Appearance")]
        [SerializeField] private Color maskColor = new Color(0.035f, 0.012f, 0.055f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color goldEdge = new Color(1.5f, 0.68f, 0.08f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color lavaEdge = new Color(1.6f, 0.18f, 0.025f, 1f);
        [SerializeField] private bool useLavaEdge;
        [Range(0.002f, 0.15f), SerializeField] private float edgeWidth = 0.025f;
        [Range(0f, 8f), SerializeField] private float edgeGlow = 3.5f;
        [Range(1f, 40f), SerializeField] private float rippleFrequency = 12f;
        [Range(0f, 0.08f), SerializeField] private float rippleAmplitude = 0.022f;
        [Range(0f, 10f), SerializeField] private float rippleSpeed = 3f;
        [SerializeField] private Vector2 fallbackViewportCenter = new Vector2(0.5f, 0.5f);
        [SerializeField] private int overlaySortingOrder = 32000;

        [Header("Camera (optional)")]
        [SerializeField] private bool animateCamera = true;
        [Range(0f, 10f), SerializeField] private float zoomFovReduction = 3f;
        [Range(0f, 0.2f), SerializeField] private float coverShakeStrength = 0.04f;
        [Range(0f, 0.2f), SerializeField] private float landingShakeStrength = 0.07f;

        [Header("Audio (assign your own clips)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip whooshClip;
        [SerializeField] private AudioClip impactClip;

        private Material runtimeMaterial;
        private Canvas generatedCanvas;
        private Coroutine currentTransition;
        private Camera activeCamera;
        private float originalFov;
        private Vector3 lastShakeOffset;
        private Vector3 previousShakenPosition;
        private float shakeEndTime;
        private float shakeMagnitude;
        private float shakeStartTime;

        public bool IsPlaying => currentTransition != null;

        [ContextMenu("Test Radial Transition to Underground")]
        private void TestToUnderground() => PlayPreview();

        [ContextMenu("Test Radial Transition to Ground")]
        private void TestToGround() => PlayPreview();

        /// <summary>Preview the cover/pause/reveal animation. Does not switch areas.</summary>
        public void PlayPreview()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode to test the radial transition.", this);
                return;
            }

            if (currentTransition != null)
            {
                // Keep the current preview intact instead of restarting mid-cover.
                return;
            }

            if (!EnsureOverlay())
            {
                return;
            }

            activeCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
            originalFov = activeCamera != null ? activeCamera.fieldOfView : 60f;
            UpdateMaterialSettings();
            overlayImage.enabled = true;
            currentTransition = StartCoroutine(AnimatePreview());
        }

        private bool EnsureOverlay()
        {
            if (overlayImage == null)
            {
                var canvasObject = new GameObject("Radial Transition Canvas", typeof(RectTransform), typeof(Canvas));
                generatedCanvas = canvasObject.GetComponent<Canvas>();
                generatedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                generatedCanvas.overrideSorting = true;
                generatedCanvas.sortingOrder = overlaySortingOrder;

                var imageObject = new GameObject("Radial Mask (runtime)", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                imageObject.transform.SetParent(canvasObject.transform, false);
                var rect = (RectTransform)imageObject.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                overlayImage = imageObject.GetComponent<Image>();
                overlayImage.color = Color.white;
                overlayImage.raycastTarget = false;
            }

            var shader = transitionShader != null
                ? transitionShader
                : Shader.Find("UI/Mining/DynamicRadialMaskTransition");
            if (shader == null)
            {
                Debug.LogError("Missing DynamicRadialMaskTransition.shader. Assign Transition Shader in the Inspector.", this);
                if (generatedCanvas != null)
                {
                    Destroy(generatedCanvas.gameObject);
                    generatedCanvas = null;
                    overlayImage = null;
                }
                return false;
            }

            if (runtimeMaterial == null)
            {
                runtimeMaterial = new Material(shader) { name = "Radial Transition (runtime)" };
            }
            overlayImage.material = runtimeMaterial;
            overlayImage.raycastTarget = false;
            return true;
        }

        private IEnumerator AnimatePreview()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            runtimeMaterial.SetFloat(PhaseId, 0f);
            runtimeMaterial.SetFloat(RadiusId, 0f);
            if (whooshClip != null) audioSource.PlayOneShot(whooshClip);
            BeginShake(coverShakeStrength, coverDuration * 0.55f);

            float elapsed = 0f;
            while (elapsed < coverDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / coverDuration);
                float eased = progress * progress * (3f - 2f * progress);
                runtimeMaterial.SetFloat(RadiusId, Mathf.LerpUnclamped(0f, 1.75f, eased));
                if (animateCamera && activeCamera != null)
                    activeCamera.fieldOfView = originalFov - zoomFovReduction * eased;
                UpdateMaterialFrame();
                yield return null;
            }

            // At this point the entire screen is covered. No world change yet.
            runtimeMaterial.SetFloat(RadiusId, 1.75f);
            elapsed = 0f;
            while (elapsed < coveredPause)
            {
                elapsed += Time.unscaledDeltaTime;
                UpdateMaterialFrame();
                yield return null;
            }

            runtimeMaterial.SetFloat(PhaseId, 1f);
            if (impactClip != null) audioSource.PlayOneShot(impactClip);
            BeginShake(landingShakeStrength, revealDuration * 0.5f);
            elapsed = 0f;
            while (elapsed < revealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / revealDuration);
                float eased = 1f - (1f - progress) * (1f - progress);
                runtimeMaterial.SetFloat(RadiusId, Mathf.LerpUnclamped(1.75f, 2.8f, eased));
                if (animateCamera && activeCamera != null)
                    activeCamera.fieldOfView = Mathf.Lerp(originalFov - zoomFovReduction, originalFov, eased);
                UpdateMaterialFrame();
                yield return null;
            }

            FinishPreview();
        }

        private void UpdateMaterialSettings()
        {
            runtimeMaterial.SetColor(MaskColorId, maskColor);
            runtimeMaterial.SetColor(EdgeColorId, useLavaEdge ? lavaEdge : goldEdge);
            runtimeMaterial.SetFloat(EdgeWidthId, edgeWidth);
            runtimeMaterial.SetFloat(EdgeGlowId, edgeGlow);
            runtimeMaterial.SetFloat(NoiseFreqId, rippleFrequency);
            runtimeMaterial.SetFloat(NoiseAmplitudeId, rippleAmplitude);
            runtimeMaterial.SetFloat(NoiseSpeedId, rippleSpeed);
        }

        private void UpdateMaterialFrame()
        {
            Vector2 center = fallbackViewportCenter;
            if (portalGate == null)
            {
                var found = GameObject.Find("Mining Portal Gate");
                if (found != null) portalGate = found.transform;
            }

            if (activeCamera != null && portalGate != null)
            {
                Vector3 viewport = activeCamera.WorldToViewportPoint(portalGate.position);
                if (viewport.z > 0f) center = viewport;
            }

            center.x = Mathf.Clamp01(center.x);
            center.y = Mathf.Clamp01(center.y);
            runtimeMaterial.SetVector(CenterId, new Vector4(center.x, center.y, 0f, 0f));
            runtimeMaterial.SetFloat(EffectTimeId, Time.unscaledTime);
        }

        private void BeginShake(float strength, float duration)
        {
            if (!animateCamera || activeCamera == null) return;
            shakeMagnitude = strength;
            shakeStartTime = Time.unscaledTime;
            shakeEndTime = shakeStartTime + duration;
        }

        private void LateUpdate()
        {
            if (activeCamera == null || currentTransition == null || !animateCamera) return;

            Transform cameraTransform = activeCamera.transform;
            Vector3 basePosition = cameraTransform.position;
            // If there is no external camera rig, remove the offset from last frame.
            if (lastShakeOffset != Vector3.zero &&
                (basePosition - previousShakenPosition).sqrMagnitude < 0.000001f)
                basePosition -= lastShakeOffset;

            float remaining = shakeEndTime - Time.unscaledTime;
            lastShakeOffset = Vector3.zero;
            if (remaining > 0f)
            {
                float fade = Mathf.Clamp01(remaining / Mathf.Max(shakeEndTime - shakeStartTime, 0.001f));
                float t = Time.unscaledTime * 35f;
                lastShakeOffset = cameraTransform.rotation * new Vector3(
                    Mathf.Sin(t * 1.93f), Mathf.Sin(t * 2.37f + 1.2f), 0f) * (shakeMagnitude * fade);
            }

            cameraTransform.position = basePosition + lastShakeOffset;
            previousShakenPosition = cameraTransform.position;
        }

        private void FinishPreview()
        {
            if (activeCamera != null)
            {
                if (lastShakeOffset != Vector3.zero &&
                    (activeCamera.transform.position - previousShakenPosition).sqrMagnitude < 0.000001f)
                    activeCamera.transform.position -= lastShakeOffset;
                if (animateCamera) activeCamera.fieldOfView = originalFov;
            }
            lastShakeOffset = Vector3.zero;
            shakeEndTime = 0f;
            if (overlayImage != null) overlayImage.enabled = false;
            currentTransition = null;
            activeCamera = null;
        }

        private void OnDisable()
        {
            if (currentTransition == null) return;
            StopCoroutine(currentTransition);
            FinishPreview();
        }

        private void OnDestroy()
        {
            if (generatedCanvas != null) Destroy(generatedCanvas.gameObject);
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
        }
    }
}
