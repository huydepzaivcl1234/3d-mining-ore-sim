using System.Collections;
using PrimeTween;
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
        private bool mainMenuMusicActive;
        private bool coinRainAmbienceActive;
        private bool ambienceFadedForPeriodChange;
        private MiningMainMenu mainMenu;
        private Tween ambienceFade;
        private Tween musicFade;
        private Coroutine ambiencePlaylist;
        private AudioClip currentPlaylistAmbience;

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
            if (mainMenu == null)
            {
                mainMenu = FindFirstObjectByType<MiningMainMenu>(FindObjectsInactive.Include);
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
                mainMenuMusicActive || shopThemeActive || coinRainAmbienceActive ||
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
            StopAmbiencePlaylist();
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
            if (IsMainMenuVisible())
            {
                PlayMainMenuMusic();
            }
            else
            {
                PlayWorldAmbience();
            }
        }

        /// <summary>Crossfades from world ambience to the main-menu background track.</summary>
        public void PlayMainMenuMusic()
        {
            if (audioData == null)
            {
                return;
            }

            shopThemeActive = false;
            mainMenuMusicActive = true;
            StopAmbiencePlaylist();
            ambienceFadedForPeriodChange = false;
            FadeAmbienceTo(0f, true);
            ambienceCueSource?.Stop();
            PlayMusic(audioData.BackgroundMusic, true);
        }

        /// <summary>Switches the shared music source to the shop theme without changing volume settings.</summary>
        public void PlayShopMusic()
        {
            shopThemeActive = true;
            StopAmbiencePlaylist();
            FadeAmbienceTo(0f, true);
            AudioClip shopTheme = audioData != null && audioData.ShopMusic != null
                ? audioData.ShopMusic
                : audioData != null ? audioData.BackgroundMusic : null;
            PlayMusic(shopTheme, false);
        }

        private void PlayMusic(AudioClip clip, bool fadeIn = false)
        {
            if (audioData == null || musicSource == null || !EnsureClipLoaded(clip) || musicMuted)
            {
                return;
            }

            requestedMusic = clip;
            ConfigureSources();
            StopMusicFade();
            bool clipChanged = musicSource.clip != clip;
            if (clipChanged)
            {
                musicSource.Stop();
                musicSource.clip = clip;
            }

            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
            if (fadeIn)
            {
                musicSource.volume = 0f;
                FadeMusicTo(GetMusicVolume(), false);
            }
            else
            {
                musicSource.volume = GetMusicVolume();
            }
        }

        public void StopBackgroundMusic()
        {
            StopAmbiencePlaylist();
            StopAudioFades();
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
                musicSource.volume = GetMusicVolume();
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
                ambienceSource.loop = ambiencePlaylist == null;
                ambienceSource.volume = GetAmbienceVolume();
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
            for (int index = 0; index < audioData.MorningAmbienceCount; index++)
            {
                EnsureClipLoaded(audioData.GetMorningAmbienceAt(index));
            }
            for (int index = 0; index < audioData.NightAmbienceCount; index++)
            {
                EnsureClipLoaded(audioData.GetNightAmbienceAt(index));
            }
            EnsureClipLoaded(audioData.SunriseRoosterSfx);
            EnsureClipLoaded(audioData.CoinRainAmbience);
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
            ambienceFadedForPeriodChange = false;
            if (coinRainAmbienceActive)
            {
                return;
            }

            if (period == MiningTimePeriod.Day)
            {
                PlaySunriseRooster();
            }

            if (!shopThemeActive && !mainMenuMusicActive)
            {
                PlayWorldAmbience(true);
            }
        }

        /// <summary>Crossfades from menu or shop music to the active world ambience.</summary>
        public void PlayWorldAmbience()
        {
            PlayWorldAmbience(true);
        }

        private void PlayWorldAmbience(bool fadeIn)
        {
            mainMenuMusicActive = false;
            shopThemeActive = false;
            if (musicSource != null && musicSource.isPlaying)
            {
                FadeMusicTo(0f, true);
            }

            if (coinRainAmbienceActive)
            {
                PlayCoinRainAmbience();
                return;
            }

            if (audioData == null || ambienceSource == null || musicMuted)
            {
                return;
            }

            bool isNight = dayNightSystem != null &&
                dayNightSystem.CurrentPeriod == MiningTimePeriod.Night;
            StopAmbiencePlaylist();
            AudioClip ambience = isNight
                ? audioData.GetRandomNightAmbience(currentPlaylistAmbience)
                : audioData.GetRandomMorningAmbience(currentPlaylistAmbience);
            ambience ??= audioData.BackgroundMusic;
            if (!EnsureClipLoaded(ambience))
            {
                return;
            }

            PlayAmbienceClip(ambience, fadeIn);
            currentPlaylistAmbience = ambience;
            ambiencePlaylist = StartCoroutine(PlayAmbiencePlaylist(isNight));
        }

        /// <summary>Uses the normal ambience crossfade, but keeps rain active until the event ends.</summary>
        public void PlayCoinRainAmbience()
        {
            coinRainAmbienceActive = true;
            if (audioData == null || ambienceSource == null || musicMuted || shopThemeActive ||
                mainMenuMusicActive || !EnsureClipLoaded(audioData.CoinRainAmbience))
            {
                return;
            }

            if (ambienceSource.isPlaying && ambienceSource.clip == audioData.CoinRainAmbience)
            {
                return;
            }

            StopAmbiencePlaylist();
            PlayAmbienceClip(audioData.CoinRainAmbience, true);
            ambienceSource.loop = true;
        }

        /// <summary>Returns to the current day/night ambience with the same smooth crossfade.</summary>
        public void StopCoinRainAmbience()
        {
            if (!coinRainAmbienceActive)
            {
                return;
            }

            coinRainAmbienceActive = false;
            if (!shopThemeActive && !mainMenuMusicActive)
            {
                PlayWorldAmbience(true);
            }
        }

        private void PlayAmbienceClip(AudioClip clip, bool fadeIn)
        {
            ConfigureSources();
            StopAmbienceFade();
            ambienceSource.loop = false;
            ambienceSource.clip = clip;
            if (fadeIn && audioData.AmbienceFadeDuration > 0f)
            {
                ambienceSource.volume = 0f;
            }

            ambienceSource.Play();
            if (fadeIn)
            {
                FadeAmbienceTo(GetAmbienceVolume(), false);
            }
        }

        private IEnumerator PlayAmbiencePlaylist(bool isNight)
        {
            while (audioData != null && ambienceSource != null && !musicMuted &&
                   !mainMenuMusicActive && !shopThemeActive && !coinRainAmbienceActive &&
                   dayNightSystem != null &&
                   (dayNightSystem.CurrentPeriod == MiningTimePeriod.Night) == isNight)
            {
                while (ambienceSource.isPlaying)
                {
                    yield return null;
                }

                float pause = isNight
                    ? audioData.GetRandomNightAmbiencePause()
                    : audioData.GetRandomMorningAmbiencePause();
                if (pause > 0f)
                {
                    yield return new WaitForSecondsRealtime(pause);
                }

                if (mainMenuMusicActive || shopThemeActive || coinRainAmbienceActive ||
                    dayNightSystem == null ||
                    (dayNightSystem.CurrentPeriod == MiningTimePeriod.Night) != isNight)
                {
                    yield break;
                }

                AudioClip nextClip = isNight
                    ? audioData.GetRandomNightAmbience(currentPlaylistAmbience)
                    : audioData.GetRandomMorningAmbience(currentPlaylistAmbience);
                if (!EnsureClipLoaded(nextClip))
                {
                    yield break;
                }

                currentPlaylistAmbience = nextClip;
                PlayAmbienceClip(nextClip, true);
            }

            ambiencePlaylist = null;
        }

        private void StopAmbiencePlaylist()
        {
            if (ambiencePlaylist != null)
            {
                StopCoroutine(ambiencePlaylist);
                ambiencePlaylist = null;
            }
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
