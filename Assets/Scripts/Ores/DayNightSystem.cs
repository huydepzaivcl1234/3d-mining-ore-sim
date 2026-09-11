using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiningSimulator.Ores
{
    /// <summary>Owns the scene's day/night clock and presents its lighting state.</summary>
    [DisallowMultipleComponent]
    public sealed class DayNightSystem : MonoBehaviour
    {
        [SerializeField] private DayNightData data;
        [SerializeField] private Light sun;

        private static readonly int SkyTintId = Shader.PropertyToID("_SkyTint");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int CubemapTransitionId = Shader.PropertyToID("_CubemapTransition");
        private static readonly int RotationSpeedId = Shader.PropertyToID("_RotationSpeed");
        private static readonly int EnableRotationId = Shader.PropertyToID("_EnableRotation");
        private static readonly int EnableFogId = Shader.PropertyToID("_EnableFog");
        private static readonly int FogIntensityId = Shader.PropertyToID("_FogIntensity");
        private static readonly int FogHeightId = Shader.PropertyToID("_FogHeight");
        private static readonly int FogSmoothnessId = Shader.PropertyToID("_FogSmoothness");
        private static readonly int FogFillId = Shader.PropertyToID("_FogFill");

        private MiningTimePeriod currentPeriod;
        private float periodElapsed;
        private float daylight;
        private float transitionFromDaylight;
        private float transitionToDaylight;
        private float transitionBellCurve;
        private bool initialized;
        private Material originalSkybox;
        private Material runtimeSkybox;
        private Tween lightingTween;
        private SpriteRenderer celestialRenderer;
        private Transform celestialTransform;
        private Camera billboardCamera;

        public MiningTimePeriod CurrentPeriod => currentPeriod;

        public int GetMaximumActiveSpecialOres(OreKind kind)
        {
            if (data == null)
            {
                return 0;
            }

            return kind switch
            {
                OreKind.LightStone => data.MaximumActiveLightStones,
                OreKind.DarkStone => data.MaximumActiveDarkStones,
                _ => 0
            };
        }

        public float CurrentPeriodProgress
        {
            get
            {
                float duration = GetPeriodDuration(currentPeriod);
                return duration > 0f ? Mathf.Clamp01(periodElapsed / duration) : 0f;
            }
        }

        public event Action<MiningTimePeriod> PeriodChanged;

        private void Awake()
        {
            InitializeIfNeeded();
            PrepareRuntimeSkybox();
            EnsureCelestialDisc();
            ApplyLighting(daylight);
        }

        private void OnDisable()
        {
            StopLightingTween();
        }

        private void OnDestroy()
        {
            StopLightingTween();
            if (runtimeSkybox == null)
            {
                return;
            }

            if (RenderSettings.skybox == runtimeSkybox)
            {
                RenderSettings.skybox = originalSkybox;
            }
            Destroy(runtimeSkybox);
        }

        private void Update()
        {
            if (data == null)
            {
                return;
            }

            InitializeIfNeeded();
            if (data.CycleEnabled)
            {
                periodElapsed += Time.deltaTime;
                float duration = GetPeriodDuration(currentPeriod);
                if (periodElapsed >= duration)
                {
                    periodElapsed %= duration;
                    currentPeriod = currentPeriod == MiningTimePeriod.Day
                        ? MiningTimePeriod.Night
                        : MiningTimePeriod.Day;
                    BeginLightingTransition();
                    PeriodChanged?.Invoke(currentPeriod);
                }
            }

            // Re-apply every frame instead of only during the ~few-second transition tween,
            // so the sun keeps drifting across the sky and gently shimmering for the whole
            // ~1-2 minute period instead of freezing in place once the transition ends.
            ApplyLighting(daylight);
        }

        private void LateUpdate()
        {
            if (celestialTransform == null || sun == null)
            {
                return;
            }

            if (billboardCamera == null)
            {
                billboardCamera = Camera.main;
                if (billboardCamera == null)
                {
                    return;
                }
            }

            Transform cameraTransform = billboardCamera.transform;
            celestialTransform.position = cameraTransform.position -
                sun.transform.forward * data.CelestialDiscDistance;
            celestialTransform.rotation = cameraTransform.rotation;
            celestialTransform.localScale =
                Vector3.one * Mathf.Lerp(data.MoonDiscScale, data.SunDiscScale, daylight);

            Color tint = Color.Lerp(data.MoonDiscColor, data.SunDiscColor, daylight);
            float intensity = Mathf.Lerp(data.MoonDiscIntensity, data.SunDiscIntensity, daylight);
            celestialRenderer.color = tint * intensity;
        }

        public bool TryChooseSpecialOre(float rollPercent, out OreData ore)
        {
            ore = null;
            if (data == null)
            {
                return false;
            }

            InitializeIfNeeded();

            if (currentPeriod == MiningTimePeriod.Day)
            {
                ore = data.LightStone;
                return ore != null && RollSucceeds(
                    rollPercent, data.LightStoneChancePerSpawnPercent);
            }

            ore = data.DarkStone;
            return ore != null && RollSucceeds(
                rollPercent, data.DarkStoneChancePerSpawnPercent);
        }

        public void SetPeriod(MiningTimePeriod period)
        {
            InitializeIfNeeded();
            if (currentPeriod == period)
            {
                return;
            }

            currentPeriod = period;
            periodElapsed = 0f;
            BeginLightingTransition();
            PeriodChanged?.Invoke(currentPeriod);
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            currentPeriod = data != null ? data.StartingPeriod : MiningTimePeriod.Day;
            periodElapsed = 0f;
            daylight = currentPeriod == MiningTimePeriod.Day ? 1f : 0f;
            initialized = true;
        }

        private float GetPeriodDuration(MiningTimePeriod period)
        {
            if (data == null)
            {
                return 1f;
            }

            return period == MiningTimePeriod.Day
                ? data.DayDurationSeconds
                : data.NightDurationSeconds;
        }

        private void BeginLightingTransition()
        {
            if (data == null)
            {
                return;
            }

            StopLightingTween();
            transitionFromDaylight = daylight;
            transitionToDaylight = currentPeriod == MiningTimePeriod.Day ? 1f : 0f;
            float duration = data.TransitionDurationSeconds;
            if (duration <= 0f || Mathf.Approximately(transitionFromDaylight, transitionToDaylight))
            {
                daylight = transitionToDaylight;
                transitionBellCurve = 0f;
                ApplyLighting(daylight);
                return;
            }

            lightingTween = Tween.Custom(this, 0f, 1f, duration,
                static (target, progress) => target.ApplyTransitionProgress(progress), Ease.Linear)
                .OnComplete(this, static target => target.transitionBellCurve = 0f);
        }

        private void ApplyTransitionProgress(float progress)
        {
            daylight = Mathf.Lerp(transitionFromDaylight, transitionToDaylight,
                data.EvaluateTransition(progress));
            // Peaks at the midpoint of the transition and fades back to 0 at either end —
            // drives the golden-hour warm tint in ApplyLighting.
            transitionBellCurve = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
            ApplyLighting(daylight);
        }

        private void StopLightingTween()
        {
            if (lightingTween.isAlive)
            {
                lightingTween.Stop();
            }
        }

        private void ApplyLighting(float daylightAmount)
        {
            if (data == null)
            {
                return;
            }

            if (sun != null)
            {
                RenderSettings.sun = sun;
                Quaternion baseRotation = Quaternion.Slerp(
                    Quaternion.Euler(data.NightSunRotation),
                    Quaternion.Euler(data.DaySunRotation), daylightAmount);

                // Continuous arc: instead of snapping to a fixed pose for the whole period,
                // the sun (or moon) keeps drifting across the sky as the period progresses.
                float arcDegrees = currentPeriod == MiningTimePeriod.Day
                    ? data.DaySunArcDegrees
                    : data.NightSunArcDegrees;
                float arcProgress = CurrentPeriodProgress - 0.5f; // -0.5..0.5 across the period
                sun.transform.rotation = baseRotation * Quaternion.Euler(arcProgress * arcDegrees, 0f, 0f);

                float intensityShimmer = 1f + (Mathf.PerlinNoise(Time.time * data.ShimmerSpeed, 0.37f) - 0.5f) *
                    2f * data.SunShimmerAmount;
                sun.color = Color.Lerp(data.NightSunColor, data.DaySunColor, daylightAmount);
                sun.intensity = Mathf.Lerp(data.NightSunIntensity, data.DaySunIntensity, daylightAmount) *
                    intensityShimmer;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            float ambientShimmer = 1f + (Mathf.PerlinNoise(Time.time * data.ShimmerSpeed * 0.6f, 8.21f) - 0.5f) *
                2f * data.AmbientShimmerAmount;
            Color ambient = Color.Lerp(data.NightAmbientColor, data.DayAmbientColor, daylightAmount);
            Color fogColor = Color.Lerp(data.NightFogColor, data.DayFogColor, daylightAmount);

            // Golden hour: only present while a transition is actually in progress (see
            // transitionBellCurve), peaking halfway through it so sunrise/sunset get a warm
            // glow instead of just crossfading straight between the day and night colors.
            float goldenAmount = transitionBellCurve * data.GoldenHourStrength;
            if (goldenAmount > 0f)
            {
                if (sun != null)
                {
                    sun.color = Color.Lerp(sun.color, data.GoldenHourColor, Mathf.Clamp01(goldenAmount));
                    sun.intensity *= 1f + Mathf.Clamp01(goldenAmount) * 0.35f;
                }
                ambient = Color.Lerp(ambient, data.GoldenHourColor, Mathf.Clamp01(goldenAmount * 0.5f));
                fogColor = Color.Lerp(fogColor, data.GoldenHourColor, Mathf.Clamp01(goldenAmount * 0.4f));
            }

            RenderSettings.ambientLight = ambient * ambientShimmer;
            ApplySkybox(daylightAmount);
            if (data.ControlFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogDensity = Mathf.Lerp(
                    data.NightFogDensity, data.DayFogDensity, daylightAmount);
            }
        }

        /// <summary>
        /// Builds a soft glowing sun/moon disc in the sky at runtime — no art asset needed.
        /// It billboards to the camera every frame in LateUpdate and cross-fades between a
        /// warm sun look and a cool moon look based on <see cref="daylight"/>.
        /// </summary>
        private void EnsureCelestialDisc()
        {
            if (data == null || !data.ShowCelestialDisc || celestialRenderer != null)
            {
                return;
            }

            GameObject discObject = new("Celestial Disc (Sun & Moon)", typeof(SpriteRenderer));
            discObject.transform.SetParent(transform, false);
            celestialTransform = discObject.transform;

            celestialRenderer = discObject.GetComponent<SpriteRenderer>();
            celestialRenderer.sprite = GenerateGlowSprite();
            celestialRenderer.sortingOrder = -100;

            // Additive blending gives a genuine glow (brightens whatever's behind it, no
            // dark quad edge); fall back gracefully if the built-in shader isn't available.
            Shader glowShader = Shader.Find("Particles/Additive") ?? Shader.Find("Sprites/Default");
            if (glowShader != null)
            {
                celestialRenderer.material = new Material(glowShader) { name = "Celestial Glow (Runtime)" };
            }
        }

        private static Sprite GenerateGlowSprite()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "CelestialGlow"
            };

            Vector2 center = new(size * 0.5f, size * 0.5f);
            float maxDistance = size * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / maxDistance;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.2f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private void PrepareRuntimeSkybox()
        {
            if (data == null || !data.ControlSkybox)
            {
                return;
            }

            originalSkybox = RenderSettings.skybox;
            Material sourceSkybox = data.SkyboxMaterial != null
                ? data.SkyboxMaterial
                : originalSkybox;
            if (sourceSkybox == null)
            {
                return;
            }

            runtimeSkybox = new Material(sourceSkybox)
            {
                name = sourceSkybox.name + " (Day Night Runtime)"
            };
            RenderSettings.skybox = runtimeSkybox;

            if (data.ControlExtendedSkybox)
            {
                SetExtendedSkyboxFeatures();
            }
        }

        private void ApplySkybox(float daylight)
        {
            if (runtimeSkybox == null)
            {
                return;
            }

            Color tint = Color.Lerp(data.NightSkyTint, data.DaySkyTint, daylight);
            if (runtimeSkybox.HasProperty(SkyTintId)) runtimeSkybox.SetColor(SkyTintId, tint);
            if (runtimeSkybox.HasProperty(TintId)) runtimeSkybox.SetColor(TintId, tint);
            if (runtimeSkybox.HasProperty(TintColorId)) runtimeSkybox.SetColor(TintColorId, tint);
            if (runtimeSkybox.HasProperty(ExposureId))
            {
                runtimeSkybox.SetFloat(ExposureId, Mathf.Lerp(
                    data.NightSkyExposure, data.DaySkyExposure, daylight));
            }

            if (!data.ControlExtendedSkybox)
            {
                return;
            }

            SetFloatIfPresent(CubemapTransitionId, 1f - daylight);
            SetFloatIfPresent(FogIntensityId, Mathf.Lerp(
                data.NightSkyFogIntensity, data.DaySkyFogIntensity, daylight));
            SetFloatIfPresent(FogHeightId, Mathf.Lerp(
                data.NightSkyFogHeight, data.DaySkyFogHeight, daylight));
            SetFloatIfPresent(FogSmoothnessId, Mathf.Lerp(
                data.NightSkyFogSmoothness, data.DaySkyFogSmoothness, daylight));
            SetFloatIfPresent(FogFillId, Mathf.Lerp(
                data.NightSkyFogFill, data.DaySkyFogFill, daylight));
        }

        private void SetExtendedSkyboxFeatures()
        {
            if (runtimeSkybox.HasProperty(EnableRotationId))
            {
                runtimeSkybox.SetFloat(EnableRotationId, 1f);
                runtimeSkybox.EnableKeyword("_ENABLEROTATION_ON");
            }

            if (runtimeSkybox.HasProperty(EnableFogId))
            {
                runtimeSkybox.SetFloat(EnableFogId, 1f);
                runtimeSkybox.EnableKeyword("_ENABLEFOG_ON");
            }

            SetFloatIfPresent(RotationSpeedId, data.SkyRotationSpeed);
        }

        private void SetFloatIfPresent(int propertyId, float value)
        {
            if (runtimeSkybox.HasProperty(propertyId))
            {
                runtimeSkybox.SetFloat(propertyId, value);
            }
        }

        private static bool RollSucceeds(float rollPercent, float chancePercent)
        {
            return chancePercent > 0f &&
                   (chancePercent >= 100f || rollPercent < chancePercent);
        }
    }
}