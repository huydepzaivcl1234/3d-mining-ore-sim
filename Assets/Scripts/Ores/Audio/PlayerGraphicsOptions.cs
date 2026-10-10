using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MiningSimulator.Ores
{
    /// <summary>Applies graphics choices only to runtime render state; restores borrowed state on teardown.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerGraphicsOptions : MonoBehaviour
    {
        public PlayerGraphicsPreferences Values { get; private set; }
        public PlayerGraphicsPreferences Defaults { get; private set; }
        public Resolution[] Resolutions { get; private set; }
        public bool SupportsMsaa { get; private set; } = true;
        public bool SupportsAo => aoStates.Count > 0;
        public bool SupportsMotionBlur => optionsProfile != null && optionsProfile.Has<MotionBlur>();
        public bool SupportsDepthOfField => optionsProfile != null && optionsProfile.Has<DepthOfField>();
        public bool DisplayPending { get; private set; }
        public float DisplaySecondsRemaining => Mathf.Max(0f, displayDeadline - Time.unscaledTime);

        private RenderPipelineAsset originalQualityPipeline;
        private UniversalRenderPipelineAsset runtimePipeline;
        private int originalVsync;
        private float originalShadowDistance;
        private readonly List<(UniversalAdditionalCameraData camera, bool post, AntialiasingMode aa, AntialiasingQuality quality)> cameras = new();
        private readonly List<(ScriptableRendererFeature feature, bool active)> aoStates = new();
        private Volume volume;
        private VolumeProfile borrowedProfile, optionsProfile;
        private DayNightSystem dayNight;
        private PlayerGraphicsPreferences previousDisplay;
        private float displayDeadline;
        private bool initialized;

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            cameras.Clear(); aoStates.Clear(); SupportsMsaa = true;
            var source = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            originalQualityPipeline = QualitySettings.renderPipeline;
            originalVsync = QualitySettings.vSyncCount;
            originalShadowDistance = QualitySettings.shadowDistance;
            Resolutions = Screen.resolutions;
            Defaults = new PlayerGraphicsPreferences
            {
                fullscreen = Screen.fullScreen, width = Screen.width, height = Screen.height,
                refreshNumerator = Screen.currentResolution.refreshRateRatio.numerator,
                refreshDenominator = Screen.currentResolution.refreshRateRatio.denominator,
                vsync = originalVsync > 0
            };
            if (source != null)
            {
                Defaults.msaa = source.msaaSampleCount; Defaults.renderScale = source.renderScale;
                Defaults.shadowDistance = source.shadowDistance; Defaults.cascades = source.shadowCascadeCount;
                foreach (var renderer in source.rendererDataList)
                {
                    if (renderer is UniversalRendererData universal &&
                        (universal.renderingMode == RenderingMode.Deferred || universal.renderingMode == RenderingMode.DeferredPlus)) SupportsMsaa = false;
                    if (renderer == null) continue;
                    foreach (var feature in renderer.rendererFeatures)
                        if (feature is ScreenSpaceAmbientOcclusion) aoStates.Add((feature, feature.isActive));
                }
                // Scalar pipeline settings are never written to the imported asset.
                runtimePipeline = Instantiate(source);
                runtimePipeline.name = source.name + " (Player Graphics Runtime)";
                QualitySettings.renderPipeline = runtimePipeline;
            }
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (camera.TryGetComponent(out UniversalAdditionalCameraData data) && data.renderType == CameraRenderType.Base)
                {
                    cameras.Add((data, data.renderPostProcessing, data.antialiasing, data.antialiasingQuality));
                    if (camera.CompareTag("MainCamera"))
                    {
                        Defaults.postProcessing = data.renderPostProcessing;
                        Defaults.aaMode = Mathf.Clamp((int)data.antialiasing, 0, 2);
                        Defaults.aaQuality = (int)data.antialiasingQuality;
                    }
                }
            if (aoStates.Count > 0) Defaults.ambientOcclusion = aoStates[0].active;
            dayNight = FindFirstObjectByType<DayNightSystem>(FindObjectsInactive.Include);
            volume = dayNight != null ? dayNight.CinematicVolume : FindFirstObjectByType<Volume>();
            if (volume != null && volume.sharedProfile != null)
            {
                borrowedProfile = volume.sharedProfile;
                optionsProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                foreach (var component in borrowedProfile.components)
                    if (component != null) optionsProfile.components.Add(Instantiate(component));
                optionsProfile.name = "Player Graphics Runtime Volume";
                volume.sharedProfile = optionsProfile;
                if (optionsProfile.TryGet(out Bloom b)) Defaults.bloom = b.active;
                if (optionsProfile.TryGet(out Vignette v)) Defaults.vignette = v.active;
                if (optionsProfile.TryGet(out MotionBlur m)) Defaults.motionBlur = m.active && m.intensity.value > 0f;
                if (optionsProfile.TryGet(out DepthOfField d)) Defaults.depthOfField = d.active && d.mode.value != DepthOfFieldMode.Off;
            }
            Values = PlayerGraphicsPreferences.Read(PlayerPrefs.GetString(PlayerGraphicsPreferences.SaveKey, ""), Defaults);
            Apply();
            dayNight?.SetGraphicsOptions(this);
            // Only a real player build changes the OS display; opening Play Mode doesn't resize the Editor.
            if (!Application.isEditor && PlayerGraphicsPreferences.MatchResolution(Resolutions, Values.width, Values.height,
                Values.refreshNumerator, Values.refreshDenominator) >= 0) ApplyDisplay(Values);
        }

        public void Apply()
        {
            if (!initialized || Values == null) return;
            Values.Sanitize();
            QualitySettings.vSyncCount = Values.vsync ? 1 : 0;
            if (runtimePipeline != null)
            {
                runtimePipeline.msaaSampleCount = SupportsMsaa ? Values.msaa : 1;
                runtimePipeline.renderScale = Values.renderScale;
                runtimePipeline.shadowCascadeCount = Values.cascades;
                runtimePipeline.shadowDistance = Values.shadowDistance;
            }
            foreach (var state in cameras)
                if (state.camera != null)
                {
                    state.camera.renderPostProcessing = Values.postProcessing;
                    state.camera.antialiasing = (AntialiasingMode)Values.aaMode;
                    state.camera.antialiasingQuality = (AntialiasingQuality)Values.aaQuality;
                }
            foreach (var state in aoStates)
                if (state.feature != null) state.feature.SetActive(Values.ambientOcclusion);
            if (optionsProfile != null)
            {
                SetActive<MotionBlur>(Values.motionBlur);
                SetActive<DepthOfField>(Values.depthOfField);
                if (Values.motionBlur && optionsProfile.TryGet(out MotionBlur motion) && motion.intensity.value <= 0f)
                    motion.intensity.Override(.15f);
                if (Values.depthOfField && optionsProfile.TryGet(out DepthOfField depth) && depth.mode.value == DepthOfFieldMode.Off)
                    depth.mode.Override(DepthOfFieldMode.Gaussian);
                var gamma = GetOrAdd<LiftGammaGain>();
                gamma.gamma.Override(new Vector4(1f, 1f, 1f, Values.gammaOffset));
                if (dayNight == null || !dayNight.ControlsCinematicPostProcessing)
                {
                    // Re-copy authored bases before applying offsets: sliders must not accumulate.
                    if (borrowedProfile.TryGet(out Bloom baseBloom))
                        GetOrAdd<Bloom>().intensity.Override(baseBloom.intensity.value * Values.bloomMultiplier);
                    GetOrAdd<Bloom>().active = Values.bloom;
                    GetOrAdd<Vignette>().active = Values.vignette;
                    float exposure = borrowedProfile.TryGet(out ColorAdjustments baseColor) ? baseColor.postExposure.value : 0f;
                    GetOrAdd<ColorAdjustments>().postExposure.Override(exposure + Values.exposureOffset);
                }
            }
            dayNight?.RefreshGraphicsOptions();
        }

        private T GetOrAdd<T>() where T : VolumeComponent =>
            optionsProfile.TryGet(out T component) ? component : optionsProfile.Add<T>(true);

        private void SetActive<T>(bool active) where T : VolumeComponent
        {
            if (optionsProfile.TryGet(out T component)) component.active = active;
        }

        public void ApplyCinematic(Bloom bloom, ColorAdjustments color, Vignette vignette)
        {
            if (Values == null) return;
            if (bloom != null) { bloom.active = Values.bloom; bloom.intensity.value *= Values.bloomMultiplier; }
            if (color != null) color.postExposure.value += Values.exposureOffset;
            if (vignette != null) vignette.active = Values.vignette;
        }

        public void BeginDisplayPreview(int index, bool fullscreen)
        {
            if (index < 0 || index >= Resolutions.Length) return;
            if (!DisplayPending) previousDisplay = Values.Copy();
            var mode = Resolutions[index];
            Values.width = mode.width; Values.height = mode.height; Values.fullscreen = fullscreen;
            Values.refreshNumerator = mode.refreshRateRatio.numerator;
            Values.refreshDenominator = mode.refreshRateRatio.denominator;
            ApplyDisplay(Values);
            DisplayPending = true;
            displayDeadline = Time.unscaledTime + 15f;
        }

        private static void ApplyDisplay(PlayerGraphicsPreferences value)
        {
            if (!Application.isEditor)
                Screen.SetResolution(value.width, value.height,
                    value.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed,
                    new RefreshRate { numerator = value.refreshNumerator, denominator = value.refreshDenominator });
        }

        public void ConfirmDisplay() { DisplayPending = false; Save(); }
        public void RevertDisplay()
        {
            if (!DisplayPending) return;
            Values.width = previousDisplay.width; Values.height = previousDisplay.height;
            Values.fullscreen = previousDisplay.fullscreen;
            Values.refreshNumerator = previousDisplay.refreshNumerator;
            Values.refreshDenominator = previousDisplay.refreshDenominator;
            ApplyDisplay(Values); DisplayPending = false;
        }
        private void Update() { if (DisplayPending && Time.unscaledTime >= displayDeadline) RevertDisplay(); }
        public void Save()
        {
            if (Values == null || DisplayPending) return;
            PlayerPrefs.SetString(PlayerGraphicsPreferences.SaveKey, JsonUtility.ToJson(Values));
            PlayerPrefs.Save();
        }
        public void ResetGraphics()
        {
            RevertDisplay();
            var display = Values;
            Values = Defaults.Copy();
            // Reset quality without silently changing the OS mode or bypassing confirmation.
            Values.width = display.width; Values.height = display.height;
            Values.fullscreen = display.fullscreen;
            Values.refreshNumerator = display.refreshNumerator;
            Values.refreshDenominator = display.refreshDenominator;
            Apply();
        }

        private void OnDestroy() => Release();

        public void Release()
        {
            if (!initialized) return;
            RevertDisplay();
            foreach (var state in cameras)
                if (state.camera != null)
                { state.camera.renderPostProcessing = state.post; state.camera.antialiasing = state.aa; state.camera.antialiasingQuality = state.quality; }
            foreach (var state in aoStates) if (state.feature != null) state.feature.SetActive(state.active);
            QualitySettings.vSyncCount = originalVsync; QualitySettings.shadowDistance = originalShadowDistance;
            if (QualitySettings.renderPipeline == runtimePipeline) QualitySettings.renderPipeline = originalQualityPipeline;
            if (runtimePipeline != null) DestroyRuntimeObject(runtimePipeline);
            if (volume != null && volume.sharedProfile == optionsProfile) volume.sharedProfile = borrowedProfile;
            dayNight?.SetGraphicsOptions(null);
            if (optionsProfile != null)
            { foreach (var component in optionsProfile.components) if (component != null) DestroyRuntimeObject(component); DestroyRuntimeObject(optionsProfile); }
            initialized = false;
        }

        private static void DestroyRuntimeObject(Object value)
        {
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
