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
        private bool initialized;
        private Material originalSkybox;
        private Material runtimeSkybox;
        private Tween lightingTween;

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
                ApplyLighting(daylight);
                return;
            }

            lightingTween = Tween.Custom(this, 0f, 1f, duration,
                static (target, progress) => target.ApplyTransitionProgress(progress), Ease.Linear);
        }

        private void ApplyTransitionProgress(float progress)
        {
            daylight = Mathf.Lerp(transitionFromDaylight, transitionToDaylight,
                data.EvaluateTransition(progress));
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
                sun.transform.rotation = Quaternion.Slerp(
                    Quaternion.Euler(data.NightSunRotation),
                    Quaternion.Euler(data.DaySunRotation), daylightAmount);
                sun.color = Color.Lerp(data.NightSunColor, data.DaySunColor, daylightAmount);
                sun.intensity = Mathf.Lerp(data.NightSunIntensity, data.DaySunIntensity, daylightAmount);
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(
                data.NightAmbientColor, data.DayAmbientColor, daylightAmount);
            ApplySkybox(daylightAmount);
            if (data.ControlFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = Color.Lerp(data.NightFogColor, data.DayFogColor, daylightAmount);
                RenderSettings.fogDensity = Mathf.Lerp(
                    data.NightFogDensity, data.DayFogDensity, daylightAmount);
            }
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
