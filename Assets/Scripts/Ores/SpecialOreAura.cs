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
        private Vector3[] auraShapeRatios = System.Array.Empty<Vector3>();
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
            CacheAuraShape();
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

            DisableLegacyParticleAura();
            FitAuraToOreBounds();

            Color color = theme == SpecialOreTheme.Light
                ? data.LightStoneAuraColor
                : data.DarkStoneAuraColor;
            if (auraLight != null)
            {
                auraLight.color = color;
                auraLight.intensity = data.AuraLightIntensity;
                auraLight.range = data.AuraLightRange;
            }

            ApplyRendererPulse(0.5f);
        }

        private void DisableLegacyParticleAura()
        {
            if (auraRoot != null)
            {
                ParticleSystem[] particleSystems =
                    auraRoot.GetComponentsInChildren<ParticleSystem>(true);
                foreach (ParticleSystem particleSystem in particleSystems)
                {
                    DisableParticleSystem(particleSystem);
                }
            }

            if (auraParticles != null &&
                (auraRoot == null || !auraParticles.transform.IsChildOf(auraRoot)))
            {
                DisableParticleSystem(auraParticles);
            }
        }

        private static void DisableParticleSystem(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
            {
                return;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = false;
            ParticleSystemRenderer particleRenderer =
                particleSystem.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer != null)
            {
                particleRenderer.enabled = false;
            }
        }

        private void CacheAuraShape()
        {
            if (auraRenderers == null || auraRenderers.Length == 0)
            {
                auraShapeRatios = System.Array.Empty<Vector3>();
                return;
            }

            Vector3 maximumScale = Vector3.zero;
            for (int i = 0; i < auraRenderers.Length; i++)
            {
                Renderer target = auraRenderers[i];
                if (target == null || target is ParticleSystemRenderer)
                {
                    continue;
                }

                Vector3 scale = Abs(target.transform.localScale);
                maximumScale = Vector3.Max(maximumScale, scale);
            }

            auraShapeRatios = new Vector3[auraRenderers.Length];
            for (int i = 0; i < auraRenderers.Length; i++)
            {
                Renderer target = auraRenderers[i];
                if (target == null || target is ParticleSystemRenderer)
                {
                    continue;
                }

                Vector3 scale = Abs(target.transform.localScale);
                auraShapeRatios[i] = new Vector3(
                    SafeDivide(scale.x, maximumScale.x),
                    SafeDivide(scale.y, maximumScale.y),
                    SafeDivide(scale.z, maximumScale.z));
            }
        }

        private void FitAuraToOreBounds()
        {
            if (auraRoot == null || auraRenderers == null || auraRenderers.Length == 0)
            {
                return;
            }

            if (auraShapeRatios == null || auraShapeRatios.Length != auraRenderers.Length)
            {
                CacheAuraShape();
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            bool hasBounds = false;
            Bounds oreBounds = default;
            foreach (Collider targetCollider in colliders)
            {
                if (targetCollider == null || !targetCollider.enabled || targetCollider.isTrigger ||
                    targetCollider.transform.IsChildOf(auraRoot))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    oreBounds = targetCollider.bounds;
                    hasBounds = true;
                }
                else
                {
                    oreBounds.Encapsulate(targetCollider.bounds);
                }
            }

            if (!hasBounds)
            {
                return;
            }

            float auraScale = theme == SpecialOreTheme.Light
                ? data.LightHaloScale
                : data.DarkAuraScale;
            Vector3 targetWorldSize = oreBounds.size * Mathf.Max(0.1f, auraScale);
            for (int i = 0; i < auraRenderers.Length; i++)
            {
                Renderer target = auraRenderers[i];
                if (target == null || target is ParticleSystemRenderer)
                {
                    continue;
                }

                Transform rendererTransform = target.transform;
                Transform parent = rendererTransform.parent;
                Vector3 parentScale = parent != null ? Abs(parent.lossyScale) : Vector3.one;
                Vector3 ratio = auraShapeRatios[i];
                rendererTransform.position = oreBounds.center;
                rendererTransform.localScale = new Vector3(
                    SafeDivide(targetWorldSize.x * ratio.x, parentScale.x),
                    SafeDivide(targetWorldSize.y * ratio.y, parentScale.y),
                    SafeDivide(targetWorldSize.z * ratio.z, parentScale.z));
            }
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }

        private static float SafeDivide(float value, float divisor)
        {
            return divisor > Mathf.Epsilon ? value / divisor : value;
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
