using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum SpecialOreTheme
    {
        Light = 0,
        Dark = 1
    }

    /// <summary>Drives the soft light halo or shadow aura without particle dots.</summary>
    [DisallowMultipleComponent]
    public sealed class SpecialOreAura : MonoBehaviour
    {
        [SerializeField] private DayNightData data;
        [SerializeField] private SpecialOreTheme theme;
        [SerializeField] private Transform auraRoot;
        [SerializeField] private Light auraLight;
        [SerializeField] private ParticleSystem auraParticles;
        [SerializeField] private Renderer[] auraRenderers = System.Array.Empty<Renderer>();

        private Vector3 baseScale = Vector3.one;
        private Quaternion baseRotation = Quaternion.identity;
        private float phaseOffset;
        private MaterialPropertyBlock propertyBlock;
        private static readonly int PulseId = Shader.PropertyToID("_Pulse");
        private static readonly int FlowSpeedId = Shader.PropertyToID("_FlowSpeed");
        private static readonly int AuraColorId = Shader.PropertyToID("_AuraColor");
        private static readonly int OuterColorId = Shader.PropertyToID("_OuterColor");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");

        private void Awake()
        {
            if (auraRoot == null)
            {
                auraRoot = transform;
            }

            baseScale = auraRoot.localScale;
            baseRotation = auraRoot.localRotation;
            phaseOffset = Mathf.Abs(transform.position.x * 0.73f + transform.position.z * 0.41f);
            propertyBlock = new MaterialPropertyBlock();
            ApplySettings();
        }

        private void OnEnable()
        {
            ApplySettings();
        }

        private void Update()
        {
            if (data == null || auraRoot == null)
            {
                return;
            }

            float wave = 0.5f + 0.5f * Mathf.Sin(
                Time.time * data.AuraPulseSpeed + phaseOffset);
            auraRoot.localScale = baseScale * (1f + wave * data.AuraPulseAmount);
            if (theme == SpecialOreTheme.Dark)
            {
                auraRoot.Rotate(0f, data.DarkAuraRotationDegreesPerSecond * Time.deltaTime, 0f,
                    Space.Self);
            }

            ApplyRendererPulse(wave);
            if (auraLight != null)
            {
                auraLight.intensity = data.AuraLightIntensity * Mathf.Lerp(0.72f, 1f, wave);
            }
        }

        private void OnDisable()
        {
            if (auraRoot != null)
            {
                auraRoot.localScale = baseScale;
                auraRoot.localRotation = baseRotation;
            }
        }

        private void ApplySettings()
        {
            if (data == null)
            {
                return;
            }

            Color color = theme == SpecialOreTheme.Light
                ? data.LightStoneAuraColor
                : data.DarkStoneAuraColor;
            if (auraLight != null)
            {
                auraLight.color = color;
                auraLight.intensity = data.AuraLightIntensity;
                auraLight.range = data.AuraLightRange;
            }

            if (auraParticles != null)
            {
                auraParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystem.EmissionModule emission = auraParticles.emission;
                emission.enabled = false;
            }

            ApplyRendererPulse(0.5f);
        }

        private void ApplyRendererPulse(float pulse)
        {
            if (auraRenderers == null || auraRenderers.Length == 0)
            {
                return;
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            float flowSpeed = data != null && theme == SpecialOreTheme.Dark
                ? data.DarkAuraFlowSpeed
                : 0f;
            Color auraColor = theme == SpecialOreTheme.Light
                ? data.LightStoneAuraColor
                : data.DarkStoneAuraColor;
            Color outerColor = theme == SpecialOreTheme.Light
                ? data.LightHaloOuterColor
                : data.DarkAuraOuterColor;
            float opacity = theme == SpecialOreTheme.Light
                ? data.LightHaloOpacity
                : data.DarkAuraOpacity;
            // The Inspector value represents the complete aura. Split it between
            // transparent layers so several shells do not multiply into a black screen.
            float perLayerOpacity = opacity / Mathf.Max(1, auraRenderers.Length);
            for (int i = 0; i < auraRenderers.Length; i++)
            {
                Renderer target = auraRenderers[i];
                if (target == null)
                {
                    continue;
                }

                target.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(PulseId, pulse);
                propertyBlock.SetFloat(FlowSpeedId, flowSpeed);
                propertyBlock.SetColor(AuraColorId, auraColor);
                propertyBlock.SetColor(OuterColorId, outerColor);
                propertyBlock.SetFloat(OpacityId, perLayerOpacity);
                target.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
