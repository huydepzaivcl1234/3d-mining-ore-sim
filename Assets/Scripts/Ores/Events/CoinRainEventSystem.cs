using System.Collections;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Runs at most one announced Coin Rain during each morning and restores weather at night.</summary>
    [DisallowMultipleComponent]
    public sealed class CoinRainEventSystem : MonoBehaviour
    {
        private const string RainObjectName = "Coin Rain Particles";
        private const float ParticleHeightAboveCamera = 18f;
        private const float ParticleForwardOffset = 14f;

        private DayNightSystem dayNightSystem;
        private DayNightData data;
        private PlayerWallet wallet;
        private MiningUnlockNotifier notifier;
        private MiningAudioManager audioManager;
        private ParticleSystem rainParticles;
        private Material coinMaterial;
        private Coroutine warningRoutine;
        private Camera rainCamera;
        private float originalFarClip;
        private float nextRewardTime;
        private bool morningHandled;
        private bool rainActive;
        private bool cameraRangeOverridden;

        public void Configure(DayNightSystem targetDayNightSystem, DayNightData targetData)
        {
            if (dayNightSystem != targetDayNightSystem && dayNightSystem != null)
            {
                dayNightSystem.PeriodChanged -= HandlePeriodChanged;
            }

            dayNightSystem = targetDayNightSystem;
            if (dayNightSystem != null)
            {
                dayNightSystem.PeriodChanged -= HandlePeriodChanged;
                dayNightSystem.PeriodChanged += HandlePeriodChanged;
            }

            data = targetData;
            wallet ??= FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            notifier ??= FindFirstObjectByType<MiningUnlockNotifier>(FindObjectsInactive.Include);
            audioManager ??= FindFirstObjectByType<MiningAudioManager>(FindObjectsInactive.Include);
            if (dayNightSystem != null && dayNightSystem.CurrentPeriod == MiningTimePeriod.Day &&
                !morningHandled)
            {
                HandleMorning();
            }
        }

        private void OnDisable()
        {
            if (dayNightSystem != null)
            {
                dayNightSystem.PeriodChanged -= HandlePeriodChanged;
            }

            StopCoinRain();
        }

        private void OnDestroy()
        {
            if (dayNightSystem != null)
            {
                dayNightSystem.PeriodChanged -= HandlePeriodChanged;
            }

            if (coinMaterial != null)
            {
                Destroy(coinMaterial);
            }
        }

        private void Update()
        {
            if (!rainActive || data == null)
            {
                return;
            }

            while (Time.time >= nextRewardTime)
            {
                wallet ??= FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
                wallet?.AddMoney(data.CoinRainRewardPerInterval);
                nextRewardTime += data.CoinRainRewardIntervalSeconds;
            }
        }

        private void LateUpdate()
        {
            if (!rainActive || data == null)
            {
                return;
            }

            ApplyRainWeather();
            FollowCamera();
        }

        private void HandlePeriodChanged(MiningTimePeriod period)
        {
            if (period == MiningTimePeriod.Day)
            {
                HandleMorning();
            }
            else
            {
                morningHandled = false;
                StopCoinRain();
            }
        }

        private void HandleMorning()
        {
            if (morningHandled || data == null)
            {
                return;
            }

            morningHandled = true;
            if (!data.CoinRainEnabled || Random.value * 100f > data.CoinRainChancePerMorningPercent)
            {
                return;
            }

            notifier ??= FindFirstObjectByType<MiningUnlockNotifier>(FindObjectsInactive.Include);
            notifier?.ShowToast("COIN RAIN INCOMING!");
            if (warningRoutine != null)
            {
                StopCoroutine(warningRoutine);
            }

            warningRoutine = StartCoroutine(BeginRainAfterWarning());
        }

        private IEnumerator BeginRainAfterWarning()
        {
            if (data.CoinRainWarningSeconds > 0f)
            {
                yield return new WaitForSeconds(data.CoinRainWarningSeconds);
            }

            if (dayNightSystem == null || dayNightSystem.CurrentPeriod != MiningTimePeriod.Day)
            {
                yield break;
            }

            StartCoinRain();
        }

        private void StartCoinRain()
        {
            if (rainActive || data == null)
            {
                return;
            }

            rainActive = true;
            nextRewardTime = Time.time;
            notifier?.ShowToast("COIN RAIN! Money is falling until night.");
            audioManager ??= FindFirstObjectByType<MiningAudioManager>(FindObjectsInactive.Include);
            audioManager?.PlayCoinRainAmbience();
            EnsureRainParticles();
            rainParticles.Play(true);
        }

        private void StopCoinRain()
        {
            if (warningRoutine != null)
            {
                StopCoroutine(warningRoutine);
                warningRoutine = null;
            }

            bool wasActive = rainActive;
            rainActive = false;
            rainParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            RestoreCameraRange();
            if (wasActive)
            {
                audioManager ??= FindFirstObjectByType<MiningAudioManager>(FindObjectsInactive.Include);
                audioManager?.StopCoinRainAmbience();
            }
        }

        private void ApplyRainWeather()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = data.CoinRainFogColor;
            RenderSettings.fogDensity = data.CoinRainFogDensity;
            RenderSettings.ambientLight = data.CoinRainAmbientColor;
        }

        private void FollowCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            if (rainCamera != camera)
            {
                RestoreCameraRange();
                rainCamera = camera;
                originalFarClip = camera.farClipPlane;
            }

            camera.farClipPlane = Mathf.Min(originalFarClip, data.CoinRainCameraFarClip);
            cameraRangeOverridden = true;
            if (rainParticles != null)
            {
                Transform cameraTransform = camera.transform;
                rainParticles.transform.position = cameraTransform.position +
                    cameraTransform.forward * ParticleForwardOffset +
                    Vector3.up * ParticleHeightAboveCamera;
            }
        }

        private void RestoreCameraRange()
        {
            if (cameraRangeOverridden && rainCamera != null)
            {
                rainCamera.farClipPlane = originalFarClip;
            }

            cameraRangeOverridden = false;
            rainCamera = null;
        }

        private void EnsureRainParticles()
        {
            if (rainParticles != null)
            {
                return;
            }

            GameObject rainObject = new(RainObjectName);
            rainObject.transform.SetParent(transform, false);
            rainParticles = rainObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = rainParticles.main;
            main.loop = true;
            main.startLifetime = 2.4f;
            main.startSpeed = 0f;
            main.startSize = 0.48f;
            main.startColor = Color.white;
            main.maxParticles = 350;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = rainParticles.emission;
            emission.rateOverTime = data.CoinRainParticlesPerSecond;

            ParticleSystem.ShapeModule shape = rainParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(48f, 1f, 42f);

            ParticleSystem.VelocityOverLifetimeModule velocity = rainParticles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = 0f;
            velocity.y = -14f;
            velocity.z = 0f;

            ParticleSystem.RotationOverLifetimeModule rotation = rainParticles.rotationOverLifetime;
            rotation.enabled = false;
            ConfigureCoinRenderer(rainParticles.GetComponent<ParticleSystemRenderer>(), coinSprite:
                ResolveCoinSprite());
        }

        private void ConfigureCoinRenderer(ParticleSystemRenderer renderer, Sprite coinSprite)
        {
            if (renderer == null || coinSprite == null)
            {
                return;
            }

            ParticleSystem.TextureSheetAnimationModule spriteAnimation =
                rainParticles.textureSheetAnimation;
            spriteAnimation.enabled = true;
            spriteAnimation.mode = ParticleSystemAnimationMode.Sprites;
            spriteAnimation.AddSprite(coinSprite);

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
                            Shader.Find("Particles/Standard Unlit");
            if (shader == null)
            {
                return;
            }

            coinMaterial = new Material(shader)
            {
                mainTexture = coinSprite.texture
            };
            if (coinMaterial.HasProperty("_BaseMap"))
            {
                coinMaterial.SetTexture("_BaseMap", coinSprite.texture);
            }

            renderer.sharedMaterial = coinMaterial;
        }

        private static Sprite ResolveCoinSprite()
        {
            MiningUiPanelCoordinator coordinator =
                FindFirstObjectByType<MiningUiPanelCoordinator>(FindObjectsInactive.Include);
            return coordinator != null && coordinator.UiData != null
                ? coordinator.UiData.MoneyIconSprite
                : null;
        }
    }
}
