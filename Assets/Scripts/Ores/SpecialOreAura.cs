using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum SpecialOreTheme
    {
        Light = 0,
        Dark = 1
    }

    /// <summary>Lightweight pulsing light and particles for the two timed ores.</summary>
    [DisallowMultipleComponent]
    public sealed class SpecialOreAura : MonoBehaviour
    {
        [SerializeField] private DayNightData data;
        [SerializeField] private SpecialOreTheme theme;
        [SerializeField] private Transform auraRoot;
        [SerializeField] private Light auraLight;
        [SerializeField] private ParticleSystem auraParticles;

        private Vector3 baseScale = Vector3.one;
        private float phaseOffset;

        private void Awake()
        {
            if (auraRoot == null)
            {
                auraRoot = transform;
            }

            baseScale = auraRoot.localScale;
            phaseOffset = Mathf.Abs(transform.position.x * 0.73f + transform.position.z * 0.41f);
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
                ParticleSystem.MainModule main = auraParticles.main;
                main.startColor = color;
                main.startLifetime = data.AuraParticleLifetime;
                main.startSize = data.AuraParticleSize;
                ParticleSystem.EmissionModule emission = auraParticles.emission;
                emission.rateOverTime = data.AuraParticlesPerSecond;
            }
        }
    }
}
