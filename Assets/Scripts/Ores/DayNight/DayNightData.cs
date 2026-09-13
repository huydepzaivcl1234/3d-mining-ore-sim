using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum MiningTimePeriod
    {
        Day = 0,
        Night = 1
    }

    /// <summary>Designer-owned day/night timing, lighting and special-ore rules.</summary>
    [CreateAssetMenu(fileName = "DayNightData", menuName = "Mining Simulator/Game Data/Day Night")]
    public sealed class DayNightData : ScriptableObject
    {
        [Header("Cycle")]
        [SerializeField] private bool cycleEnabled = true;
        [SerializeField] private MiningTimePeriod startingPeriod = MiningTimePeriod.Day;
        [Min(1f), SerializeField] private float dayDurationSeconds = 120f;
        [Min(1f), SerializeField] private float nightDurationSeconds = 90f;
        [Min(0f), SerializeField] private float transitionDurationSeconds = 8f;
        [SerializeField]
        private AnimationCurve transitionCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Sun And Ambient Light")]
        [Tooltip("Sun pose at the start/end of the Day period (sunrise/sunset) — low to the " +
                 "horizon so shadows read long. It climbs toward overhead by Day Sun Arc " +
                 "Degrees as the day progresses; see the Sun Arc section below.")]
        [SerializeField] private Vector3 daySunRotation = new(15f, -30f, 0f);
        [Tooltip("Moon pose at the start/end of the Night period, same idea as Day Sun Rotation.")]
        [SerializeField] private Vector3 nightSunRotation = new(210f, -30f, 0f);
        [SerializeField] private Color daySunColor = new(1f, 0.94f, 0.78f, 1f);
        [SerializeField] private Color nightSunColor = new(0.42f, 0.5f, 0.85f, 1f);
        [Min(0f), SerializeField] private float daySunIntensity = 1.35f;
        [Min(0f), SerializeField] private float nightSunIntensity = 0.18f;
        [SerializeField] private Color dayAmbientColor = new(0.72f, 0.78f, 0.86f, 1f);
        [SerializeField] private Color nightAmbientColor = new(0.12f, 0.16f, 0.28f, 1f);

        [Header("Sun Arc And Shimmer")]
        [Tooltip("Degrees the sun climbs above Day Sun Rotation as the Day period progresses, " +
                 "peaking (Day Sun Rotation X + this value) at solar noon — the period's " +
                 "midpoint — then sinking back down by sunset. Higher values (e.g. 70-80, so " +
                 "the peak lands near 90) make midday shadows go short and flat under objects, " +
                 "matching real sunlight; lower values keep a shallower, more even arc.")]
        [Min(0f), SerializeField] private float daySunArcDegrees = 72f;
        [Tooltip("Same climb-to-overhead-then-back-down arc, but for the moon across the Night period.")]
        [Min(0f), SerializeField] private float nightSunArcDegrees = 10f;
        [Tooltip("How much the sun's intensity gently breathes up and down, as a fraction " +
                 "of its base intensity (0 = perfectly steady).")]
        [Range(0f, 0.3f), SerializeField] private float sunShimmerAmount = 0.06f;
        [Tooltip("How fast the intensity/ambient shimmer cycles.")]
        [Min(0f), SerializeField] private float shimmerSpeed = 0.15f;
        [Tooltip("Same breathing effect applied to the ambient light color.")]
        [Range(0f, 0.3f), SerializeField] private float ambientShimmerAmount = 0.04f;

        [Header("Golden Hour")]
        [Tooltip("Warm tint blended in only while a Day<->Night transition is in progress, " +
                 "peaking halfway through it — gives sunrise/sunset an actual glow instead " +
                 "of just crossfading between the day and night colors.")]
        [SerializeField] private Color goldenHourColor = new(1f, 0.55f, 0.24f, 1f);
        [Tooltip("0 disables the effect. Around 0.5-0.8 looks like a proper sunset/sunrise.")]
        [Range(0f, 2f), SerializeField] private float goldenHourStrength = 0.6f;

        [Header("Celestial Disc (Sun & Moon)")]
        [Tooltip("Adds a soft glowing sun/moon disc in the sky, generated at runtime — no " +
                 "art asset needed. Disable if you already have a sun/moon visual.")]
        [SerializeField] private bool showCelestialDisc = true;
        [SerializeField] private Color sunDiscColor = new(1f, 0.86f, 0.58f, 1f);
        [SerializeField] private Color moonDiscColor = new(0.78f, 0.85f, 1f, 1f);
        [Min(0f), SerializeField] private float sunDiscIntensity = 1.5f;
        [Min(0f), SerializeField] private float moonDiscIntensity = 0.85f;
        [Min(0.1f), SerializeField] private float sunDiscScale = 16f;
        [Min(0.1f), SerializeField] private float moonDiscScale = 8f;
        [Min(1f), SerializeField] private float celestialDiscDistance = 480f;

        [Header("Shadows")]
        [Tooltip("Lets this system drive the sun's shadow type/strength/bias instead of " +
                 "leaving them fixed. Shadow length already changes on its own from the sun " +
                 "arc rotation added above — this adds crisp day shadows, faint dim ones at " +
                 "night, and no jarring pop when the Light component's own settings differ.")]
        [SerializeField] private bool controlShadows = true;
        [SerializeField] private LightShadows dayShadowType = LightShadows.Soft;
        [SerializeField] private LightShadows nightShadowType = LightShadows.Soft;
        [Range(0f, 1f), SerializeField] private float dayShadowStrength = 1f;
        [Tooltip("Kept above 0 so moonlit shadows stay faintly visible instead of vanishing.")]
        [Range(0f, 1f), SerializeField] private float nightShadowStrength = 0.45f;
        [Min(0f), SerializeField] private float shadowBias = 0.05f;
        [Min(0f), SerializeField] private float shadowNormalBias = 0.4f;
        [Min(0f), SerializeField] private float dayShadowDistance = 120f;
        [Min(0f), SerializeField] private float nightShadowDistance = 70f;

        [Header("Skybox")]
        [SerializeField] private bool controlSkybox = true;
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private Color daySkyTint = new(0.52f, 0.72f, 1f, 1f);
        [SerializeField] private Color nightSkyTint = new(0.25f, 0.31f, 0.52f, 1f);
        [Min(0f), SerializeField] private float daySkyExposure = 1.15f;
        [Min(0f), SerializeField] private float nightSkyExposure = 0.58f;

        [Header("Skybox Extended")]
        [SerializeField] private bool controlExtendedSkybox = true;
        [SerializeField] private float skyRotationSpeed = 0.35f;
        [Range(0f, 1f), SerializeField] private float daySkyFogIntensity = 0.72f;
        [Range(0f, 1f), SerializeField] private float nightSkyFogIntensity = 0.95f;
        [Range(0.01f, 1f), SerializeField] private float daySkyFogHeight = 0.74f;
        [Range(0.01f, 1f), SerializeField] private float nightSkyFogHeight = 0.58f;
        [Range(0.01f, 1f), SerializeField] private float daySkyFogSmoothness = 0.12f;
        [Range(0.01f, 1f), SerializeField] private float nightSkyFogSmoothness = 0.48f;
        [Range(0f, 1f), SerializeField] private float daySkyFogFill = 0.04f;
        [Range(0f, 1f), SerializeField] private float nightSkyFogFill = 0.12f;

        [Header("Fog")]
        [SerializeField] private bool controlFog = true;
        [SerializeField] private Color dayFogColor = new(0.65f, 0.78f, 0.9f, 1f);
        [SerializeField] private Color nightFogColor = new(0.07f, 0.1f, 0.2f, 1f);
        [Min(0f), SerializeField] private float dayFogDensity = 0.002f;
        [Min(0f), SerializeField] private float nightFogDensity = 0.012f;

        [Header("Cinematic Post Processing")]
        [SerializeField] private bool controlCinematicPostProcessing = true;
        [Tooltip("Use the filmic ACES tonemapper on the existing Global Volume.")]
        [SerializeField] private bool useAcesTonemapping = true;
        [Min(0f), SerializeField] private float dayBloomIntensity = 0.22f;
        [Min(0f), SerializeField] private float nightBloomIntensity = 0.38f;
        [Min(0f), SerializeField] private float bloomThreshold = 0.9f;
        [Min(0f), SerializeField] private float goldenHourBloomBoost = 0.28f;
        [Range(-5f, 5f), SerializeField] private float dayPostExposure = 0.05f;
        [Range(-5f, 5f), SerializeField] private float nightPostExposure = -0.18f;
        [Range(-100f, 100f), SerializeField] private float dayContrast = 7f;
        [Range(-100f, 100f), SerializeField] private float nightContrast = 13f;
        [Range(-100f, 100f), SerializeField] private float daySaturation = 4f;
        [Range(-100f, 100f), SerializeField] private float nightSaturation = -5f;
        [SerializeField] private Color dayColorFilter = Color.white;
        [SerializeField] private Color nightColorFilter = new(0.72f, 0.8f, 1f, 1f);
        [Range(0f, 1f), SerializeField] private float dayVignetteIntensity = 0.12f;
        [Range(0f, 1f), SerializeField] private float nightVignetteIntensity = 0.2f;
        [Range(0.01f, 1f), SerializeField] private float vignetteSmoothness = 0.55f;

        [Header("Day Special Ore")]
        [SerializeField] private OreData lightStone;
        [Range(0f, 100f), SerializeField] private float lightStoneChancePerSpawnPercent = 5f;
        [Min(0), SerializeField] private int maximumActiveLightStones = 2;

        [Header("Night Special Ore")]
        [SerializeField] private OreData darkStone;
        [Range(0f, 100f), SerializeField] private float darkStoneChancePerSpawnPercent = 5f;
        [Min(0), SerializeField] private int maximumActiveDarkStones = 2;

        [Header("Special Ore Aura - Shared")]
        [SerializeField] private Color lightStoneAuraColor = new(1f, 0.82f, 0.25f, 1f);
        [SerializeField] private Color darkStoneAuraColor = new(0.012f, 0.018f, 0.026f, 0.9f);
        [Min(0f), SerializeField] private float auraLightIntensity = 2.4f;
        [Min(0f), SerializeField] private float auraLightRange = 4.5f;
        [Min(0f), SerializeField] private float auraPulseSpeed = 2f;
        [Range(0f, 0.5f), SerializeField] private float auraPulseAmount = 0.12f;

        [Header("Light Stone Halo")]
        [SerializeField] private Color lightHaloOuterColor = new(1f, 0.62f, 0.12f, 0.55f);
        [Min(0.1f), SerializeField] private float lightHaloScale = 1.55f;
        [Range(0f, 1f), SerializeField] private float lightHaloOpacity = 0.42f;

        [Header("Dark Stone Shadow Aura")]
        [SerializeField] private Color darkAuraOuterColor = new(0.035f, 0.055f, 0.065f, 0.72f);
        [Min(0.1f), SerializeField] private float darkAuraScale = 1.48f;
        [Range(0f, 1f), SerializeField] private float darkAuraOpacity = 0.58f;
        [Min(0f), SerializeField] private float darkAuraFlowSpeed = 0.32f;
        [Min(0f), SerializeField] private float darkAuraRotationDegreesPerSecond = 7f;

        [HideInInspector, Min(0f), SerializeField] private float auraParticlesPerSecond = 7f;
        [HideInInspector, Min(0.01f), SerializeField] private float auraParticleLifetime = 1.4f;
        [HideInInspector, Min(0.01f), SerializeField] private float auraParticleSize = 0.12f;

        public bool CycleEnabled => cycleEnabled;
        public MiningTimePeriod StartingPeriod => startingPeriod;
        public float DayDurationSeconds => dayDurationSeconds;
        public float NightDurationSeconds => nightDurationSeconds;
        public float TransitionDurationSeconds => transitionDurationSeconds;
        public float EvaluateTransition(float progress)
        {
            progress = Mathf.Clamp01(progress);
            return transitionCurve == null || transitionCurve.length == 0
                ? Mathf.SmoothStep(0f, 1f, progress)
                : Mathf.Clamp01(transitionCurve.Evaluate(progress));
        }
        public Vector3 DaySunRotation => daySunRotation;
        public Vector3 NightSunRotation => nightSunRotation;
        public Color DaySunColor => daySunColor;
        public Color NightSunColor => nightSunColor;
        public float DaySunIntensity => daySunIntensity;
        public float NightSunIntensity => nightSunIntensity;
        public Color DayAmbientColor => dayAmbientColor;
        public Color NightAmbientColor => nightAmbientColor;
        public float DaySunArcDegrees => daySunArcDegrees;
        public float NightSunArcDegrees => nightSunArcDegrees;
        public float SunShimmerAmount => sunShimmerAmount;
        public float ShimmerSpeed => shimmerSpeed;
        public float AmbientShimmerAmount => ambientShimmerAmount;
        public Color GoldenHourColor => goldenHourColor;
        public float GoldenHourStrength => goldenHourStrength;
        public bool ShowCelestialDisc => showCelestialDisc;
        public Color SunDiscColor => sunDiscColor;
        public Color MoonDiscColor => moonDiscColor;
        public float SunDiscIntensity => sunDiscIntensity;
        public float MoonDiscIntensity => moonDiscIntensity;
        public float SunDiscScale => sunDiscScale;
        public float MoonDiscScale => moonDiscScale;
        public float CelestialDiscDistance => celestialDiscDistance;
        public bool ControlShadows => controlShadows;
        public LightShadows DayShadowType => dayShadowType;
        public LightShadows NightShadowType => nightShadowType;
        public float DayShadowStrength => dayShadowStrength;
        public float NightShadowStrength => nightShadowStrength;
        public float ShadowBias => shadowBias;
        public float ShadowNormalBias => shadowNormalBias;
        public float DayShadowDistance => dayShadowDistance;
        public float NightShadowDistance => nightShadowDistance;
        public bool ControlSkybox => controlSkybox;
        public Material SkyboxMaterial => skyboxMaterial;
        public Color DaySkyTint => daySkyTint;
        public Color NightSkyTint => nightSkyTint;
        public float DaySkyExposure => daySkyExposure;
        public float NightSkyExposure => nightSkyExposure;
        public bool ControlExtendedSkybox => controlExtendedSkybox;
        public float SkyRotationSpeed => skyRotationSpeed;
        public float DaySkyFogIntensity => daySkyFogIntensity;
        public float NightSkyFogIntensity => nightSkyFogIntensity;
        public float DaySkyFogHeight => daySkyFogHeight;
        public float NightSkyFogHeight => nightSkyFogHeight;
        public float DaySkyFogSmoothness => daySkyFogSmoothness;
        public float NightSkyFogSmoothness => nightSkyFogSmoothness;
        public float DaySkyFogFill => daySkyFogFill;
        public float NightSkyFogFill => nightSkyFogFill;
        public bool ControlFog => controlFog;
        public Color DayFogColor => dayFogColor;
        public Color NightFogColor => nightFogColor;
        public float DayFogDensity => dayFogDensity;
        public float NightFogDensity => nightFogDensity;
        public bool ControlCinematicPostProcessing => controlCinematicPostProcessing;
        public bool UseAcesTonemapping => useAcesTonemapping;
        public float DayBloomIntensity => dayBloomIntensity;
        public float NightBloomIntensity => nightBloomIntensity;
        public float BloomThreshold => bloomThreshold;
        public float GoldenHourBloomBoost => goldenHourBloomBoost;
        public float DayPostExposure => dayPostExposure;
        public float NightPostExposure => nightPostExposure;
        public float DayContrast => dayContrast;
        public float NightContrast => nightContrast;
        public float DaySaturation => daySaturation;
        public float NightSaturation => nightSaturation;
        public Color DayColorFilter => dayColorFilter;
        public Color NightColorFilter => nightColorFilter;
        public float DayVignetteIntensity => dayVignetteIntensity;
        public float NightVignetteIntensity => nightVignetteIntensity;
        public float VignetteSmoothness => vignetteSmoothness;
        public OreData LightStone => lightStone;
        public float LightStoneChancePerSpawnPercent => lightStoneChancePerSpawnPercent;
        public int MaximumActiveLightStones => maximumActiveLightStones;
        public OreData DarkStone => darkStone;
        public float DarkStoneChancePerSpawnPercent => darkStoneChancePerSpawnPercent;
        public int MaximumActiveDarkStones => maximumActiveDarkStones;
        public Color LightStoneAuraColor => lightStoneAuraColor;
        public Color DarkStoneAuraColor => darkStoneAuraColor;
        public float AuraLightIntensity => auraLightIntensity;
        public float AuraLightRange => auraLightRange;
        public float AuraPulseSpeed => auraPulseSpeed;
        public float AuraPulseAmount => auraPulseAmount;
        public Color LightHaloOuterColor => lightHaloOuterColor;
        public float LightHaloScale => lightHaloScale;
        public float LightHaloOpacity => lightHaloOpacity;
        public Color DarkAuraOuterColor => darkAuraOuterColor;
        public float DarkAuraScale => darkAuraScale;
        public float DarkAuraOpacity => darkAuraOpacity;
        public float DarkAuraFlowSpeed => darkAuraFlowSpeed;
        public float DarkAuraRotationDegreesPerSecond => darkAuraRotationDegreesPerSecond;
        public float AuraParticlesPerSecond => auraParticlesPerSecond;
        public float AuraParticleLifetime => auraParticleLifetime;
        public float AuraParticleSize => auraParticleSize;

        private void OnValidate()
        {
            dayDurationSeconds = Mathf.Max(1f, dayDurationSeconds);
            nightDurationSeconds = Mathf.Max(1f, nightDurationSeconds);
            transitionDurationSeconds = Mathf.Clamp(transitionDurationSeconds, 0f,
                Mathf.Min(dayDurationSeconds, nightDurationSeconds));
            lightStoneChancePerSpawnPercent = Mathf.Clamp(lightStoneChancePerSpawnPercent, 0f, 100f);
            darkStoneChancePerSpawnPercent = Mathf.Clamp(darkStoneChancePerSpawnPercent, 0f, 100f);
            maximumActiveLightStones = Mathf.Max(0, maximumActiveLightStones);
            maximumActiveDarkStones = Mathf.Max(0, maximumActiveDarkStones);
            daySunIntensity = Mathf.Max(0f, daySunIntensity);
            nightSunIntensity = Mathf.Max(0f, nightSunIntensity);
            daySkyExposure = Mathf.Max(0f, daySkyExposure);
            nightSkyExposure = Mathf.Max(0f, nightSkyExposure);
            daySkyFogHeight = Mathf.Clamp(daySkyFogHeight, 0.01f, 1f);
            nightSkyFogHeight = Mathf.Clamp(nightSkyFogHeight, 0.01f, 1f);
            daySkyFogSmoothness = Mathf.Clamp(daySkyFogSmoothness, 0.01f, 1f);
            nightSkyFogSmoothness = Mathf.Clamp(nightSkyFogSmoothness, 0.01f, 1f);
            dayFogDensity = Mathf.Max(0f, dayFogDensity);
            nightFogDensity = Mathf.Max(0f, nightFogDensity);
            dayBloomIntensity = Mathf.Max(0f, dayBloomIntensity);
            nightBloomIntensity = Mathf.Max(0f, nightBloomIntensity);
            bloomThreshold = Mathf.Max(0f, bloomThreshold);
            goldenHourBloomBoost = Mathf.Max(0f, goldenHourBloomBoost);
            dayPostExposure = Mathf.Clamp(dayPostExposure, -5f, 5f);
            nightPostExposure = Mathf.Clamp(nightPostExposure, -5f, 5f);
            dayContrast = Mathf.Clamp(dayContrast, -100f, 100f);
            nightContrast = Mathf.Clamp(nightContrast, -100f, 100f);
            daySaturation = Mathf.Clamp(daySaturation, -100f, 100f);
            nightSaturation = Mathf.Clamp(nightSaturation, -100f, 100f);
            dayVignetteIntensity = Mathf.Clamp01(dayVignetteIntensity);
            nightVignetteIntensity = Mathf.Clamp01(nightVignetteIntensity);
            vignetteSmoothness = Mathf.Clamp(vignetteSmoothness, 0.01f, 1f);
            auraLightIntensity = Mathf.Max(0f, auraLightIntensity);
            auraLightRange = Mathf.Max(0f, auraLightRange);
            auraPulseSpeed = Mathf.Max(0f, auraPulseSpeed);
            lightHaloScale = Mathf.Max(0.1f, lightHaloScale);
            darkAuraScale = Mathf.Max(0.1f, darkAuraScale);
            darkAuraFlowSpeed = Mathf.Max(0f, darkAuraFlowSpeed);
            darkAuraRotationDegreesPerSecond = Mathf.Max(0f, darkAuraRotationDegreesPerSecond);
            auraParticlesPerSecond = Mathf.Max(0f, auraParticlesPerSecond);
            auraParticleLifetime = Mathf.Max(0.01f, auraParticleLifetime);
            auraParticleSize = Mathf.Max(0.01f, auraParticleSize);
        }
    }
}
