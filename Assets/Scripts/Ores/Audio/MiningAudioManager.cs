using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns background music and event-driven mining SFX playback.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningAudioManager : MonoBehaviour
    {
        private const string MasterVolumeKey = "MiningSimulator.Audio.Master.v1";
        private const string MusicVolumeKey = "MiningSimulator.Audio.Music.v1";
        private const string SfxVolumeKey = "MiningSimulator.Audio.Sfx.v1";

        [Header("References")]
        [SerializeField] private MiningAudioData audioData;
        [SerializeField] private OreSpawner oreSpawner;
        [SerializeField] private NpcShop npcShop;
        [SerializeField] private NpcProgressionSystem progressionSystem;
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
            if (oreSpawner == null)
            {
                oreSpawner = FindFirstObjectByType<OreSpawner>(FindObjectsInactive.Include);
            }
            if (npcShop == null)
            {
                npcShop = FindFirstObjectByType<NpcShop>(FindObjectsInactive.Include);
            }
            if (progressionSystem == null)
            {
                progressionSystem = FindFirstObjectByType<NpcProgressionSystem>(FindObjectsInactive.Include);
            }
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
        }

        private void OnEnable()
        {
            if (oreSpawner != null)
            {
                oreSpawner.OreRewardGranted -= HandleOreRewardGranted;
                oreSpawner.OreRewardGranted += HandleOreRewardGranted;
            }

            if (npcShop != null)
            {
                npcShop.NpcPurchased -= HandleNpcPurchased;
                npcShop.NpcPurchased += HandleNpcPurchased;
            }

            if (progressionSystem != null)
            {
                progressionSystem.LevelChanged -= HandleLevelUp;
                progressionSystem.LevelChanged += HandleLevelUp;
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
        }

        private void Start()
        {
            if (audioData != null && audioData.PlayMusicOnStart)
            {
                PlayWorldAmbience();
            }
        }

        private void OnDisable()
        {
            if (oreSpawner != null)
            {
                oreSpawner.OreRewardGranted -= HandleOreRewardGranted;
            }

            if (npcShop != null)
            {
                npcShop.NpcPurchased -= HandleNpcPurchased;
            }

            if (progressionSystem != null)
            {
                progressionSystem.LevelChanged -= HandleLevelUp;
            }

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

        public void PlayBackgroundMusic()
        {
            shopThemeActive = false;
            musicSource?.Stop();
            PlayWorldAmbience();
        }

        /// <summary>Switches the shared music source to the shop theme without changing volume settings.</summary>
        public void PlayShopMusic()
        {
            shopThemeActive = true;
            ambienceSource?.Pause();
            AudioClip shopTheme = audioData != null && audioData.ShopMusic != null
                ? audioData.ShopMusic
                : audioData != null ? audioData.BackgroundMusic : null;
            PlayMusic(shopTheme);
        }

        private void PlayMusic(AudioClip clip)
        {
            if (audioData == null || musicSource == null || !EnsureClipLoaded(clip) || musicMuted)
            {
                return;
            }

            requestedMusic = clip;
            ConfigureSources();
            if (musicSource.clip != clip)
            {
                musicSource.clip = clip;
            }

            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        public void StopBackgroundMusic()
        {
            musicSource?.Stop();
            ambienceSource?.Stop();
            ambienceCueSource?.Stop();
        }

        public void SetMusicMuted(bool muted)
        {
            musicMuted = muted;
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
                    PlayMusic(requestedMusic != null ? requestedMusic : audioData?.ShopMusic);
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
            if (audioData == null || sfxSource == null || sfxMuted || !EnsureClipLoaded(clip))
            {
                return;
            }

            sfxSource.pitch = Random.Range(audioData.MinimumPitch, audioData.MaximumPitch);
            sfxSource.PlayOneShot(clip, audioData.SfxVolume);
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

        private void ConfigureSources()
        {
            if (audioData == null)
            {
                return;
            }

            if (musicSource != null)
            {
                musicSource.playOnAwake = false;
                musicSource.loop = audioData.LoopMusic;
                musicSource.volume = audioData.MusicVolume * masterVolume * musicVolume;
                musicSource.spatialBlend = 0f;
                musicSource.mute = musicMuted;
                musicSource.outputAudioMixerGroup = audioData.MusicMixerGroup;
            }

            if (sfxSource != null)
            {
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.volume = masterVolume * sfxVolume;
                sfxSource.spatialBlend = 0f;
                sfxSource.mute = sfxMuted;
                sfxSource.outputAudioMixerGroup = audioData.SfxMixerGroup;
            }

            if (ambienceSource != null)
            {
                ambienceSource.playOnAwake = false;
                ambienceSource.loop = true;
                ambienceSource.volume = audioData.AmbienceVolume * masterVolume * musicVolume;
                ambienceSource.spatialBlend = 0f;
                ambienceSource.mute = musicMuted;
                ambienceSource.outputAudioMixerGroup = audioData.MusicMixerGroup;
            }

            if (ambienceCueSource != null)
            {
                ambienceCueSource.playOnAwake = false;
                ambienceCueSource.loop = false;
                ambienceCueSource.volume = masterVolume * musicVolume;
                ambienceCueSource.spatialBlend = 0f;
                ambienceCueSource.mute = musicMuted;
                ambienceCueSource.outputAudioMixerGroup = audioData.MusicMixerGroup;
            }
        }

        private void LoadVolumeSettings()
        {
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveVolumeSettings();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (audioData != null && audioData.MuteAudioOnLostFocus)
            {
                AudioListener.pause = !hasFocus;
            }

            if (hasFocus)
            {
                nextAllowedMiningSfxTime = 0f;
            }
        }

        private void OnApplicationQuit()
        {
            AudioListener.pause = false;
            SaveVolumeSettings();
        }

        private void ResolveSources()
        {
            Transform audioRoot = transform.Find("Audio");
            if (audioRoot == null)
            {
                var audioObject = new GameObject("Audio");
                audioObject.transform.SetParent(transform, false);
                audioRoot = audioObject.transform;
            }

            musicSource = ResolveSource(musicSource, audioRoot, "Music Source");
            sfxSource = ResolveSource(sfxSource, audioRoot, "SFX Source");
            ambienceSource = ResolveSource(ambienceSource, audioRoot, "Ambience Source");
            ambienceCueSource = ResolveSource(ambienceCueSource, audioRoot, "Ambience Cue Source");
            if (sfxSource == musicSource)
            {
                sfxSource = ResolveSource(null, audioRoot, "SFX Source");
            }
            if (ambienceSource == musicSource || ambienceSource == sfxSource)
            {
                ambienceSource = ResolveSource(null, audioRoot, "Ambience Source");
            }
            if (ambienceCueSource == musicSource || ambienceCueSource == sfxSource || ambienceCueSource == ambienceSource)
            {
                ambienceCueSource = ResolveSource(null, audioRoot, "Ambience Cue Source");
            }
        }

        private static AudioSource ResolveSource(AudioSource current, Transform parent, string objectName)
        {
            if (current != null)
            {
                return current;
            }

            Transform sourceTransform = parent.Find(objectName);
            if (sourceTransform == null)
            {
                var sourceObject = new GameObject(objectName);
                sourceObject.transform.SetParent(parent, false);
                sourceTransform = sourceObject.transform;
            }

            AudioSource source = sourceTransform.GetComponent<AudioSource>();
            if (source == null)
            {
                source = sourceTransform.gameObject.AddComponent<AudioSource>();
            }

            return source;
        }

        private void PreloadAllAudioClips()
        {
            if (audioData == null)
            {
                return;
            }

            EnsureClipLoaded(audioData.BackgroundMusic);
            EnsureClipLoaded(audioData.ShopMusic);
            EnsureClipLoaded(audioData.MorningAmbience);
            EnsureClipLoaded(audioData.NightAmbience);
            EnsureClipLoaded(audioData.SunriseRoosterSfx);
            EnsureClipLoaded(audioData.OreHitSfx);
            EnsureClipLoaded(audioData.OreBreakSfx);
            EnsureClipLoaded(audioData.ButtonClickSfx);
            EnsureClipLoaded(audioData.NpcPurchasedSfx);
            EnsureClipLoaded(audioData.UpgradePurchasedSfx);
            EnsureClipLoaded(audioData.LevelUpSfx);
            EnsureClipLoaded(audioData.RebirthSfx);
            EnsureClipLoaded(audioData.PanelOpenSfx);
            EnsureClipLoaded(audioData.PanelCloseSfx);
            EnsureClipLoaded(audioData.WheelSpinSfx);
            EnsureClipLoaded(audioData.WheelRewardSfx);
        }

        private static bool EnsureClipLoaded(AudioClip clip)
        {
            if (clip == null || clip.loadState == AudioDataLoadState.Failed)
            {
                return false;
            }

            return clip.loadState != AudioDataLoadState.Unloaded || clip.LoadAudioData();
        }

        private void HandleOreRewardGranted(Ore ore, float reward)
        {
            if (audioData != null && (ore == null || !ore.LastDamageWasNpc))
            {
                PlaySfx(audioData.OreBreakSfx);
            }
        }

        private void HandleNpcPurchased(MiningNpc npc)
        {
            if (audioData != null)
            {
                PlaySfx(audioData.NpcPurchasedSfx);
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            if (audioData != null)
            {
                PlaySfx(audioData.LevelUpSfx);
            }
        }

        private void HandleUpgradePurchased(MiningUpgradeType type)
        {
            if (audioData != null)
            {
                PlaySfx(audioData.UpgradePurchasedSfx);
            }
        }

        private void HandlePanelOpened()
        {
            if (audioData != null)
            {
                PlaySfx(audioData.PanelOpenSfx);
            }
        }

        private void HandlePanelClosed()
        {
            if (audioData != null)
            {
                PlaySfx(audioData.PanelCloseSfx);
            }
        }

        private void HandleRebirthCompleted(int count)
        {
            if (audioData != null)
            {
                PlaySfx(audioData.RebirthSfx);
            }
        }

        private void HandleWheelSpinStarted()
        {
            PlayWheelSpinSfx();
        }

        private void HandleWheelRewardGranted(MiningGiftReward reward)
        {
            PlayWheelRewardSfx();
        }

        private void HandlePeriodChanged(MiningTimePeriod period)
        {
            if (period == MiningTimePeriod.Day)
            {
                PlaySunriseRooster();
            }

            if (!shopThemeActive)
            {
                PlayWorldAmbience();
            }
        }

        private void PlayWorldAmbience()
        {
            if (audioData == null || ambienceSource == null || musicMuted || shopThemeActive)
            {
                return;
            }

            AudioClip ambience = dayNightSystem != null && dayNightSystem.CurrentPeriod == MiningTimePeriod.Night
                ? audioData.NightAmbience
                : audioData.MorningAmbience;
            ambience ??= audioData.BackgroundMusic;
            if (!EnsureClipLoaded(ambience))
            {
                return;
            }

            ConfigureSources();
            if (ambienceSource.clip != ambience)
            {
                ambienceSource.clip = ambience;
            }
            if (!ambienceSource.isPlaying)
            {
                ambienceSource.Play();
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
