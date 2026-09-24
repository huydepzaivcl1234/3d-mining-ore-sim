using System.Collections;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Visual-only portal. Put this on a scene-authored Quad with a Toxic Slime Portal material.
    /// Does not trigger MiningPortalGate or change worlds.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class ToxicSlimePortalVisual : MonoBehaviour
    {
        private static readonly int OpenId = Shader.PropertyToID("_Open");
        private static readonly int SpeedId = Shader.PropertyToID("_SwirlSpeed");
        private static readonly int CellsId = Shader.PropertyToID("_CellScale");
        private static readonly int SplashId = Shader.PropertyToID("_RimSplash");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");
        private static readonly int CoreId = Shader.PropertyToID("_CoreColor");
        private static readonly int SlimeId = Shader.PropertyToID("_SlimeColor");
        private static readonly int RimId = Shader.PropertyToID("_RimColor");
        private static readonly int GlowId = Shader.PropertyToID("_Glow");
        private static readonly int TimeOffsetId = Shader.PropertyToID("_TimeOffset");

        [Header("Portal look")]
        [Range(0.5f, 6f), SerializeField] private float swirlSpeed = 2.5f;
        [Range(4f, 16f), SerializeField] private float bubbleDensity = 8f;
        [Range(0.01f, 0.12f), SerializeField] private float rimSplash = 0.05f;
        [Range(0f, 6f), SerializeField] private float glowStrength = 2f;
        [ColorUsage(true, true), SerializeField] private Color coreColor = new Color(0.016f, 0.09f, 0.03f);
        [ColorUsage(true, true), SerializeField] private Color slimeColor = new Color(0.17f, 0.88f, 0.07f);
        [ColorUsage(true, true), SerializeField] private Color rimColor = new Color(1f, 1.8f, 0.01f);
        [Range(0.25f, 4f), SerializeField] private float aspectCorrection = 1f;
        [SerializeField] private float animationTimeOffset;

        [Header("Preview (no world changes)")]
        [Range(0f, 1f), SerializeField] private float openAmount = 1f;
        [Min(0.05f), SerializeField] private float popDuration = 0.26f;
        [Min(0.05f), SerializeField] private float closeDuration = 0.35f;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip popClip;
        [SerializeField] private AudioClip warpClip;

        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Coroutine animationRoutine;

        private void OnEnable()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            ApplyProperties();
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            meshRenderer = GetComponent<MeshRenderer>();
            ApplyProperties();
        }

        private void Update()
        {
            // Property blocks update while animating in Play Mode; inspector edits work in Edit Mode.
            if (Application.isPlaying && animationRoutine != null) ApplyProperties();
        }

        private void ApplyProperties()
        {
            if (meshRenderer == null) return;
            propertyBlock ??= new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(OpenId, openAmount);
            propertyBlock.SetFloat(SpeedId, swirlSpeed);
            propertyBlock.SetFloat(CellsId, bubbleDensity);
            propertyBlock.SetFloat(SplashId, rimSplash);
            propertyBlock.SetFloat(AspectId, aspectCorrection);
            propertyBlock.SetFloat(GlowId, glowStrength);
            propertyBlock.SetColor(CoreId, coreColor);
            propertyBlock.SetColor(SlimeId, slimeColor);
            propertyBlock.SetColor(RimId, rimColor);
            propertyBlock.SetFloat(TimeOffsetId, animationTimeOffset);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        [ContextMenu("Preview / Pop Portal Open")]
        public void PopOpen()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to animate the portal.", this); return; }
            RunAnimation(0f, 1f, popDuration, popClip, true);
        }

        [ContextMenu("Preview / Close Portal")]
        public void ClosePortal()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to animate the portal.", this); return; }
            RunAnimation(openAmount, 0f, closeDuration, null, false);
        }

        [ContextMenu("Preview / Warp Pulse (Visual Only)")]
        public void PreviewWarp()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to preview the warp.", this); return; }
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            if (sfxSource != null && warpClip != null) sfxSource.PlayOneShot(warpClip);
            animationRoutine = StartCoroutine(WarpPulse());
        }

        private void RunAnimation(float from, float to, float duration, AudioClip clip, bool overshoot)
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            openAmount = from;
            ApplyProperties();
            if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
            animationRoutine = StartCoroutine(AnimateOpen(from, to, duration, overshoot));
        }

        private IEnumerator AnimateOpen(float from, float to, float duration, bool overshoot)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = overshoot
                    ? 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f)
                    : t * t * (3f - 2f * t);
                openAmount = Mathf.Clamp01(Mathf.LerpUnclamped(from, to, ease));
                ApplyProperties();
                yield return null;
            }
            openAmount = to;
            ApplyProperties();
            animationRoutine = null;
        }

        private IEnumerator WarpPulse()
        {
            // Preview only. Neither portal interaction nor scene/world state is touched.
            float start = openAmount;
            float elapsed = 0f;
            while (elapsed < 0.20f)
            {
                elapsed += Time.unscaledDeltaTime;
                openAmount = Mathf.Lerp(start, 0.10f, Mathf.Clamp01(elapsed / 0.20f));
                ApplyProperties();
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < 0.30f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.30f);
                openAmount = Mathf.Lerp(0.10f, 1f, t * t * (3f - 2f * t));
                ApplyProperties();
                yield return null;
            }
            openAmount = 1f;
            ApplyProperties();
            animationRoutine = null;
        }

        private void OnDisable()
        {
            if (animationRoutine == null) return;
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }
    }
}
