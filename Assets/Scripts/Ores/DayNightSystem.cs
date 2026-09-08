using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns the scene's day/night clock and presents its lighting state.</summary>
    [DisallowMultipleComponent]
    public sealed class DayNightSystem : MonoBehaviour
    {
        [SerializeField] private DayNightData data;
        [SerializeField] private Light sun;

        private MiningTimePeriod currentPeriod;
        private MiningTimePeriod previousPeriod;
        private float periodElapsed;
        private bool initialized;
        private Material originalSkybox;
        private Material runtimeSkybox;

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
            ApplyLighting(1f);
        }

        private void OnDestroy()
        {
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
                    previousPeriod = currentPeriod;
                    currentPeriod = currentPeriod == MiningTimePeriod.Day
                        ? MiningTimePeriod.Night
                        : MiningTimePeriod.Day;
                    PeriodChanged?.Invoke(currentPeriod);
                }
            }

            float transition = data.TransitionDurationSeconds <= 0f
                ? 1f
                : Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(periodElapsed / data.TransitionDurationSeconds));
            ApplyLighting(transition);
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

            previousPeriod = currentPeriod;
            currentPeriod = period;
            periodElapsed = 0f;
            ApplyLighting(data != null && data.TransitionDurationSeconds > 0f ? 0f : 1f);
            PeriodChanged?.Invoke(currentPeriod);
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            currentPeriod = data != null ? data.StartingPeriod : MiningTimePeriod.Day;
            previousPeriod = currentPeriod;
            periodElapsed = 0f;
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

        private void ApplyLighting(float transition)
        {
            if (data == null)
            {
                return;
            }

            float targetDay = currentPeriod == MiningTimePeriod.Day ? 1f : 0f;
            float sourceDay = previousPeriod == MiningTimePeriod.Day ? 1f : 0f;
            float daylight = Mathf.Lerp(sourceDay, targetDay, transition);

            if (sun != null)
            {
                RenderSettings.sun = sun;
                sun.transform.rotation = Quaternion.Slerp(
                    Quaternion.Euler(data.NightSunRotation),
                    Quaternion.Euler(data.DaySunRotation), daylight);
                sun.color = Color.Lerp(data.NightSunColor, data.DaySunColor, daylight);
                sun.intensity = Mathf.Lerp(data.NightSunIntensity, data.DaySunIntensity, daylight);
            }

            RenderSettings.ambientLight = Color.Lerp(
                data.NightAmbientColor, data.DayAmbientColor, daylight);
            ApplySkybox(daylight);
            if (data.ControlFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = Color.Lerp(data.NightFogColor, data.DayFogColor, daylight);
                RenderSettings.fogDensity = Mathf.Lerp(
                    data.NightFogDensity, data.DayFogDensity, daylight);
            }
        }

        private void PrepareRuntimeSkybox()
        {
            if (data == null || !data.ControlSkybox || RenderSettings.skybox == null)
            {
                return;
            }

            originalSkybox = RenderSettings.skybox;
            runtimeSkybox = new Material(originalSkybox)
            {
                name = originalSkybox.name + " (Day Night Runtime)"
            };
            RenderSettings.skybox = runtimeSkybox;
        }

        private void ApplySkybox(float daylight)
        {
            if (runtimeSkybox == null)
            {
                return;
            }

            Color tint = Color.Lerp(data.NightSkyTint, data.DaySkyTint, daylight);
            if (runtimeSkybox.HasProperty("_SkyTint")) runtimeSkybox.SetColor("_SkyTint", tint);
            if (runtimeSkybox.HasProperty("_Tint")) runtimeSkybox.SetColor("_Tint", tint);
            if (runtimeSkybox.HasProperty("_Exposure"))
            {
                runtimeSkybox.SetFloat("_Exposure", Mathf.Lerp(
                    data.NightSkyExposure, data.DaySkyExposure, daylight));
            }
        }

        private static bool RollSucceeds(float rollPercent, float chancePercent)
        {
            return chancePercent > 0f &&
                   (chancePercent >= 100f || rollPercent < chancePercent);
        }
    }
}
