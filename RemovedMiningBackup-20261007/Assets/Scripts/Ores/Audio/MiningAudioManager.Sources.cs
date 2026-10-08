using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MiningAudioManager
    {
        private void ConfigureSources()
        {
            RefreshExternalSfx();
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
            EnsureClipLoaded(audioData.LavaWorldAmbience);
            EnsureClipLoaded(audioData.LavaPortalWhooshSfx);
            EnsureClipLoaded(audioData.LavaPortalImpactSfx);
            EnsureClipLoaded(audioData.StalkedCatchSfx);
            EnsureClipLoaded(audioData.StalkedJumpscareSfx);
            EnsureClipLoaded(audioData.OreHitSfx);
            EnsureClipLoaded(audioData.OreBreakSfx);
            EnsureClipLoaded(audioData.ButtonClickSfx);
            EnsureClipLoaded(audioData.NpcPurchasedSfx);
            EnsureClipLoaded(audioData.UpgradePurchasedSfx);
            EnsureClipLoaded(audioData.LevelUpSfx);
            EnsureClipLoaded(audioData.RebirthSfx);
            EnsureClipLoaded(audioData.MonsterDropSfx);
            EnsureClipLoaded(audioData.MonsterLootSfx);
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

    }
}
