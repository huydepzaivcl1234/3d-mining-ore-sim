using System.Collections;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MiningAudioManager
    {
        private AudioClip runeAmbience;
        private bool runeAmbienceActive;
        public void BeginRuneAmbience(AudioClip clip)
        {
            runeAmbience = clip;
            runeAmbienceActive = clip != null;
            if (!runeAmbienceActive || musicMuted || ambienceSource == null) return;
            StopAmbiencePlaylist();
            StopWorldAmbienceSwitch();
            PlayAmbienceClip(clip, true);
            ambienceSource.loop = true;
        }
        public void EndRuneAmbience()
        {
            if (!runeAmbienceActive) return;
            runeAmbienceActive = false;
            runeAmbience = null;
            if (!mainMenuMusicActive && !shopThemeActive) PlayWorldAmbience(true);
        }
        public void PlayWorldAmbience()
        {
            PlayWorldAmbience(true);
        }

        private void PlayWorldAmbience(bool fadeIn)
        {
            if (runeAmbienceActive) { BeginRuneAmbience(runeAmbience); return; }
            mainMenuMusicActive = false;
            shopThemeActive = false;
            if (musicSource != null && musicSource.isPlaying)
            {
                FadeMusicTo(0f, true);
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

        private void PlayAmbienceClip(AudioClip clip, bool fadeIn)
        {
            PlayAmbienceClip(clip, fadeIn, GetAmbienceVolume());
        }

        private void PlayAmbienceClip(AudioClip clip, bool fadeIn, float targetVolume)
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
                FadeAmbienceTo(targetVolume, false);
            }
            else
            {
                ambienceSource.volume = targetVolume;
            }
        }

        private IEnumerator PlayAmbiencePlaylist(bool isNight)
        {
            while (audioData != null && ambienceSource != null && !musicMuted &&
                   !mainMenuMusicActive && !shopThemeActive && dayNightSystem != null &&
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

                if (mainMenuMusicActive || shopThemeActive || dayNightSystem == null ||
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

    }
}
