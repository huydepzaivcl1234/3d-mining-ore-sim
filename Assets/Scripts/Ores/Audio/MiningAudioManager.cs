using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns background music and event-driven mining SFX playback.</summary>
    [DisallowMultipleComponent]
    public sealed partial class MiningAudioManager : MonoBehaviour
    {
        private const string MasterVolumeKey = "MiningSimulator.Audio.Master.v1";
        private const string MusicVolumeKey = "MiningSimulator.Audio.Music.v1";
        private const string SfxVolumeKey = "MiningSimulator.Audio.Sfx.v1";

        [Header("References")]
        [SerializeField] private MiningAudioData audioData;
        [SerializeField] private MiningUpgradeSystem upgradeSystem;
        [SerializeField] private MiningUpgradePanel upgradePanel;
        [SerializeField] private MiningRebirthSystem rebirthSystem;
        [SerializeField] private MiningRebirthPanel rebirthPanel;
        [SerializeField] private MiningAudioSettingsPanel audioSettingsPanel;
        [SerializeField] private MiningGiftBoxWheelPanel giftBoxWheelPanel;
        // Runtime lookup avoids storing a scene object reference in audio data.
        // This keeps the audio feature independent of authored UI and scene layout.
        private DayNightSystem dayNightSystem;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioSource ambienceCueSource;

        private bool musicMuted;
        private bool sfxMuted;
        private float masterVolume = 1f;
        private float musicVolume = 1f;
        private float sfxVolume = 1f;
        private AudioClip requestedMusic;
        private float nextAllowedMiningSfxTime;
        private bool shopThemeActive;
        private bool mainMenuMusicActive;
        private bool coinRainAmbienceActive;
        private bool ambienceFadedForPeriodChange;
        private MiningMainMenu mainMenu;
        private Tween ambienceFade;
        private Tween musicFade;
        private Coroutine ambiencePlaylist;
        private Coroutine worldAmbienceTransition;
        private AudioClip currentPlaylistAmbience;
        // Local/spatial channels retain their authored level; settings apply once.
        private readonly Dictionary<AudioSource, float> externalSfxSources = new();
        private readonly List<AudioSource> deadSfxSources = new();
        private readonly List<StarterAssets.ThirdPersonController> movementAudioOwners = new();
        private void RegisterMovementAudioSource(AudioSource source) => RegisterWorldSfxSource(source);
        public float SfxGain => (sfxMuted ? 0f : masterVolume * sfxVolume) * (audioData != null ? audioData.SfxVolume : 1f);

        public void RegisterSfxSource(AudioSource source, float baseVolume = -1f)
        {
            if (source == null || source == sfxSource || source == musicSource ||
                source == ambienceSource || source == ambienceCueSource) return;
            if (audioData != null && audioData.MusicMixerGroup != null &&
                audioData.MusicMixerGroup != audioData.SfxMixerGroup &&
                source.outputAudioMixerGroup == audioData.MusicMixerGroup) return;
            if (baseVolume >= 0f || !externalSfxSources.ContainsKey(source))
                externalSfxSources[source] = baseVolume >= 0f ? Mathf.Clamp01(baseVolume) : source.volume;
            if (audioData != null && audioData.SfxMixerGroup != null)
                source.outputAudioMixerGroup = audioData.SfxMixerGroup;
            source.volume = externalSfxSources[source] * SfxGain;
        }

        public void RegisterSfxSources(GameObject owner)
        {
            if (owner == null) return;
            foreach (var source in owner.GetComponentsInChildren<AudioSource>(true)) RegisterSfxSource(source);
        }

        private void RefreshExternalSfx()
        {
            deadSfxSources.Clear();
            foreach (var pair in externalSfxSources)
                if (pair.Key == null) deadSfxSources.Add(pair.Key);
                else pair.Key.volume = pair.Value * SfxGain;
            foreach (var source in deadSfxSources) externalSfxSources.Remove(source);
        }

        public MiningAudioData AudioData => audioData;
        public bool MusicMuted => musicMuted;
        public bool SfxMuted => sfxMuted;
        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

        private void Awake()
        {
            LoadVolumeSettings();
            ResolveSources();
            ConfigureSources();
            PreloadAllAudioClips();
            FindReferencesIfMissing();
        }

        private void FindReferencesIfMissing()
        {



            if (upgradeSystem == null)
            {
                upgradeSystem = FindFirstObjectByType<MiningUpgradeSystem>(FindObjectsInactive.Include);
            }
            if (upgradePanel == null)
            {
                upgradePanel = FindFirstObjectByType<MiningUpgradePanel>(FindObjectsInactive.Include);
            }
            if (rebirthSystem == null)
            {
                rebirthSystem = FindFirstObjectByType<MiningRebirthSystem>(FindObjectsInactive.Include);
            }
            if (rebirthPanel == null)
            {
                rebirthPanel = FindFirstObjectByType<MiningRebirthPanel>(FindObjectsInactive.Include);
            }
            if (audioSettingsPanel == null)
            {
                audioSettingsPanel = FindFirstObjectByType<MiningAudioSettingsPanel>(FindObjectsInactive.Include);
            }
            if (giftBoxWheelPanel == null)
            {
                giftBoxWheelPanel = FindFirstObjectByType<MiningGiftBoxWheelPanel>(FindObjectsInactive.Include);
            }
            if (dayNightSystem == null)
            {
                dayNightSystem = FindFirstObjectByType<DayNightSystem>(FindObjectsInactive.Include);
            }
            if (mainMenu == null)
            {
                mainMenu = FindFirstObjectByType<MiningMainMenu>(FindObjectsInactive.Include);
            }
        }

        private void OnEnable()
        {
            foreach (var movement in FindObjectsByType<StarterAssets.ThirdPersonController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                movement.MovementAudioSourceCreated -= RegisterMovementAudioSource;
                movement.MovementAudioSourceCreated += RegisterMovementAudioSource;
                movementAudioOwners.Add(movement);
                RegisterWorldSfxSource(movement.MovementAudioSource);
            }






            if (upgradeSystem != null)
            {
                upgradeSystem.UpgradePurchased -= HandleUpgradePurchased;
                upgradeSystem.UpgradePurchased += HandleUpgradePurchased;
            }

            if (upgradePanel != null)
            {
                upgradePanel.PanelOpened -= HandlePanelOpened;
                upgradePanel.PanelOpened += HandlePanelOpened;
                upgradePanel.PanelClosed -= HandlePanelClosed;
                upgradePanel.PanelClosed += HandlePanelClosed;
            }

            if (rebirthSystem != null)
            {
                rebirthSystem.RebirthCompleted -= HandleRebirthCompleted;
                rebirthSystem.RebirthCompleted += HandleRebirthCompleted;
            }

            if (rebirthPanel != null)
            {
                rebirthPanel.PanelOpened -= HandlePanelOpened;
                rebirthPanel.PanelOpened += HandlePanelOpened;
                rebirthPanel.PanelClosed -= HandlePanelClosed;
                rebirthPanel.PanelClosed += HandlePanelClosed;
            }

            if (audioSettingsPanel != null)
            {
                audioSettingsPanel.PanelOpened -= HandlePanelOpened;
                audioSettingsPanel.PanelOpened += HandlePanelOpened;
                audioSettingsPanel.PanelClosed -= HandlePanelClosed;
                audioSettingsPanel.PanelClosed += HandlePanelClosed;
            }

            if (giftBoxWheelPanel != null)
            {
                giftBoxWheelPanel.WheelSpinStarted -= HandleWheelSpinStarted;
                giftBoxWheelPanel.WheelSpinStarted += HandleWheelSpinStarted;
                giftBoxWheelPanel.RewardGranted -= HandleWheelRewardGranted;
                giftBoxWheelPanel.RewardGranted += HandleWheelRewardGranted;
            }

            if (dayNightSystem != null)
            {
                dayNightSystem.PeriodChanged -= HandlePeriodChanged;
                dayNightSystem.PeriodChanged += HandlePeriodChanged;
            }

            ResolveSources();
            ConfigureSources();
            // One enable-time scan covers authored UI/player/monster channels. Runtime
            // channels register when created; never scan the scene each frame.
            foreach (var source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                RegisterSfxSource(source);
        }

        private void Start()
        {
            if (audioData != null && audioData.PlayMusicOnStart)
            {
                if (IsMainMenuVisible())
                {
                    PlayMainMenuMusic();
                }
                else
                {
                    PlayWorldAmbience(false);
                }
            }
        }

        private void Update()
        {
            SynchronizeMainMenuMusic();

            if (audioData == null || dayNightSystem == null || ambienceSource == null ||
                mainMenuMusicActive || shopThemeActive || coinRainAmbienceActive || runeAmbienceActive ||
                worldAmbienceTransition != null ||
                ambienceFadedForPeriodChange ||
                !ambienceSource.isPlaying || audioData.AmbienceFadeDuration <= 0f)
            {
                return;
            }

            if (dayNightSystem.CurrentPeriodRemainingSeconds <= audioData.AmbienceFadeDuration)
            {
                ambienceFadedForPeriodChange = true;
                FadeAmbienceTo(0f, true);
            }
        }

        private void OnDisable()
        {
            if (worldSfxVoices != null)
                foreach (var voice in worldSfxVoices)
                    if (voice != null) voice.Stop();
            foreach (var movement in movementAudioOwners)
                if (movement != null) movement.MovementAudioSourceCreated -= RegisterMovementAudioSource;
            movementAudioOwners.Clear();
            foreach (var pair in externalSfxSources)
                if (pair.Key != null) pair.Key.volume = pair.Value;
            externalSfxSources.Clear();
            StopWorldAmbienceSwitch();
            StopAmbiencePlaylist();






            if (upgradeSystem != null)
            {
                upgradeSystem.UpgradePurchased -= HandleUpgradePurchased;
            }

            if (upgradePanel != null)
            {
                upgradePanel.PanelOpened -= HandlePanelOpened;
                upgradePanel.PanelClosed -= HandlePanelClosed;
            }

            if (rebirthSystem != null)
            {
                rebirthSystem.RebirthCompleted -= HandleRebirthCompleted;
            }

            if (rebirthPanel != null)
            {
                rebirthPanel.PanelOpened -= HandlePanelOpened;
                rebirthPanel.PanelClosed -= HandlePanelClosed;
            }

            if (audioSettingsPanel != null)
            {
                audioSettingsPanel.PanelOpened -= HandlePanelOpened;
                audioSettingsPanel.PanelClosed -= HandlePanelClosed;
            }

            if (giftBoxWheelPanel != null)
            {
                giftBoxWheelPanel.WheelSpinStarted -= HandleWheelSpinStarted;
                giftBoxWheelPanel.RewardGranted -= HandleWheelRewardGranted;
            }

            if (dayNightSystem != null)
            {
                dayNightSystem.PeriodChanged -= HandlePeriodChanged;
            }
        }

        public void SetMusicMuted(bool muted)
        {
            musicMuted = muted;
            if (muted) StopWorldAmbienceSwitch();
            if (musicSource != null)
            {
                musicSource.mute = muted;
            }
            if (ambienceSource != null)
            {
                ambienceSource.mute = muted;
            }
            if (ambienceCueSource != null)
            {
                ambienceCueSource.mute = muted;
            }

            if (!muted)
            {
                if (shopThemeActive)
                {
                    PlayMusic(requestedMusic != null ? requestedMusic : audioData?.ShopMusic, false);
                }
                else if (mainMenuMusicActive || IsMainMenuVisible())
                {
                    PlayMainMenuMusic();
                }
                else
                {
                    PlayWorldAmbience();
                }
            }
        }

        public void SetSfxMuted(bool muted)
        {
            sfxMuted = muted;
            RefreshExternalSfx();
            if (sfxSource != null)
            {
                sfxSource.mute = muted;
            }
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            ConfigureSources();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            ConfigureSources();
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            ConfigureSources();
        }

        public void SaveVolumeSettings()
        {
            PlayerPrefs.Save();
        }

        public void PlaySfx(AudioClip clip)
        {
            PlaySfx(clip, 1f);
        }

        public void PlaySfx(AudioClip clip, float volumeMultiplier)
        {
            if (audioData == null || sfxSource == null || sfxMuted || !EnsureClipLoaded(clip))
            {
                return;
            }

            sfxSource.pitch = Random.Range(audioData.MinimumPitch, audioData.MaximumPitch);
            sfxSource.PlayOneShot(clip, audioData.SfxVolume * Mathf.Max(0f, volumeMultiplier));
        }

        public void PlayButtonSfx()
        {
            if (audioData != null)
            {
                PlaySfx(audioData.ButtonClickSfx);
            }
        }

        public void PlayWheelSpinSfx()
        {
            if (audioData != null)
            {
                PlaySfx(audioData.WheelSpinSfx);
            }
        }

        public void PlayWheelRewardSfx()
        {
            if (audioData != null)
            {
                PlaySfx(audioData.WheelRewardSfx);
            }
        }









        private void StopWorldAmbienceSwitch()
        {
            if (worldAmbienceTransition == null) return;
            StopCoroutine(worldAmbienceTransition);
            worldAmbienceTransition = null;
        }

        public void PlayMiningImpactSfx(bool oreBroken)
        {
            if (audioData == null)
            {
                return;
            }

            if (oreBroken)
            {
                PlaySfx(audioData.OreBreakSfx);
                return;
            }

            float now = Time.unscaledTime;
            if (now < nextAllowedMiningSfxTime)
            {
                return;
            }

            nextAllowedMiningSfxTime = now + audioData.MiningSfxCooldown;
            PlaySfx(audioData.OreHitSfx);
        }

        private void SynchronizeMainMenuMusic()
        {
            bool menuVisible = IsMainMenuVisible();
            if (menuVisible && !mainMenuMusicActive && !shopThemeActive)
            {
                PlayMainMenuMusic();
            }
            else if (!menuVisible && mainMenuMusicActive && !shopThemeActive)
            {
                PlayWorldAmbience();
            }
        }

        private bool IsMainMenuVisible()
        {
            mainMenu ??= FindFirstObjectByType<MiningMainMenu>(FindObjectsInactive.Include);
            return mainMenu != null && mainMenu.IsOpen;
        }

        private void FadeAmbienceTo(float targetVolume, bool stopWhenSilent)
        {
            if (ambienceSource == null)
            {
                return;
            }

            StopAmbienceFade();
            float duration = audioData != null ? audioData.AmbienceFadeDuration : 0f;
            if (duration <= 0f)
            {
                ambienceSource.volume = targetVolume;
                if (stopWhenSilent && targetVolume <= 0f) ambienceSource.Stop();
                return;
            }

            ambienceFade = Tween.Custom(this, ambienceSource.volume, targetVolume, duration,
                static (manager, volume) => manager.ambienceSource.volume = volume, Ease.InOutSine,
                useUnscaledTime: true)
                .OnComplete(this, manager =>
                {
                    if (stopWhenSilent && targetVolume <= 0f)
                    {
                        manager.ambienceSource.Stop();
                    }
                });
        }

        private void FadeMusicTo(float targetVolume, bool stopWhenSilent)
        {
            if (musicSource == null)
            {
                return;
            }

            StopMusicFade();
            float duration = audioData != null ? audioData.AmbienceFadeDuration : 0f;
            if (duration <= 0f)
            {
                musicSource.volume = targetVolume;
                if (stopWhenSilent && targetVolume <= 0f) musicSource.Stop();
                return;
            }

            musicFade = Tween.Custom(this, musicSource.volume, targetVolume, duration,
                static (manager, volume) => manager.musicSource.volume = volume, Ease.InOutSine,
                useUnscaledTime: true)
                .OnComplete(this, manager =>
                {
                    if (stopWhenSilent && targetVolume <= 0f)
                    {
                        manager.musicSource.Stop();
                    }
                });
        }

        private float GetMusicVolume() => audioData.MusicVolume * masterVolume * musicVolume;

        private float GetAmbienceVolume() => audioData.AmbienceVolume * masterVolume * musicVolume;

        private void StopAudioFades()
        {
            StopAmbienceFade();
            StopMusicFade();
        }

        private void StopAmbienceFade()
        {
            if (ambienceFade.isAlive)
            {
                ambienceFade.Stop();
            }
        }

        private void StopMusicFade()
        {
            if (musicFade.isAlive)
            {
                musicFade.Stop();
            }
        }

        private void PlaySunriseRooster()
        {
            AudioClip rooster = audioData != null ? audioData.SunriseRoosterSfx : null;
            if (ambienceCueSource == null || musicMuted || !EnsureClipLoaded(rooster))
            {
                return;
            }

            ambienceCueSource.PlayOneShot(rooster);
        }
    }
}
