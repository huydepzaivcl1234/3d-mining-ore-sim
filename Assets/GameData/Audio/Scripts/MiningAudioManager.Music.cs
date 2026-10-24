using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MiningAudioManager
    {
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
            StopWorldAmbienceSwitch();
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
            StopWorldAmbienceSwitch();
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
            StopWorldAmbienceSwitch();
            StopAmbiencePlaylist();
            StopAudioFades();
            musicSource?.Stop();
            ambienceSource?.Stop();
            ambienceCueSource?.Stop();
        }

    }
}
