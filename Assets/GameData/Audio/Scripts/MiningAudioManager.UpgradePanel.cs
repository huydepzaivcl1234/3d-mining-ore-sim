using PrimeTween;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Audio for the "Choose an upgrade / ability" panel. That panel pauses the game with
    /// <c>AudioListener.pause</c>, which silences every AudioSource that doesn't opt out, so
    /// the music and SFX sources temporarily ignore the listener pause while the panel is open.
    /// </summary>
    public sealed partial class MiningAudioManager
    {
        private bool upgradeAudioBypass;
        private bool upgradeThemeActive;
        private AudioClip musicBeforeUpgradePanel;
        private Tween upgradeThemeFade;

        /// <summary>Starts the panel theme (fade-in) and lets UI sounds play while the game is paused.</summary>
        public void BeginUpgradePanelAudio()
        {
            if (audioData == null || upgradeThemeActive) return;
            upgradeThemeActive = true;
            SetUpgradeAudioBypass(true);
            if (upgradeThemeFade.isAlive) upgradeThemeFade.Stop();

            AudioClip theme = audioData.UpgradeMusic;
            if (musicSource == null || musicMuted || !EnsureClipLoaded(theme)) return;

            // Remember whatever the music source was doing (normally nothing in the world).
            musicBeforeUpgradePanel = musicSource.isPlaying && musicSource.clip != theme ? musicSource.clip : null;
            StopMusicFade();
            ConfigureSources();
            musicSource.Stop();
            musicSource.clip = theme;
            musicSource.loop = true;
            musicSource.volume = 0f;
            musicSource.Play();
            FadeUpgradeTheme(GetUpgradeMusicVolume(), audioData.UpgradeMusicFadeSeconds, false);
        }

        /// <summary>Fades the panel theme out and hands the music source back to what it was doing.</summary>
        public void EndUpgradePanelAudio()
        {
            if (!upgradeThemeActive) return;
            upgradeThemeActive = false;
            if (audioData == null || musicSource == null || musicSource.clip != audioData.UpgradeMusic)
            {
                SetUpgradeAudioBypass(false);
                return;
            }

            FadeUpgradeTheme(0f, Mathf.Min(0.45f, audioData.UpgradeMusicFadeSeconds), true);
        }

        private void FadeUpgradeTheme(float target, float seconds, bool finish)
        {
            if (upgradeThemeFade.isAlive) upgradeThemeFade.Stop();
            if (seconds <= 0f)
            {
                musicSource.volume = target;
                if (finish) FinishUpgradeTheme();
                return;
            }

            upgradeThemeFade = Tween.Custom(this, musicSource.volume, target, seconds,
                static (manager, volume) => manager.musicSource.volume = volume, Ease.InOutSine,
                useUnscaledTime: true)
                .OnComplete(this, manager => { if (finish) manager.FinishUpgradeTheme(); });
        }

        private void FinishUpgradeTheme()
        {
            // A newer panel session may have started while the fade-out was running.
            if (upgradeThemeActive) return;
            if (musicSource != null)
            {
                musicSource.Stop();
                musicSource.clip = null;
            }

            SetUpgradeAudioBypass(false);
            AudioClip previous = musicBeforeUpgradePanel;
            musicBeforeUpgradePanel = null;
            if (previous != null) PlayMusic(previous, true);
        }

        private void SetUpgradeAudioBypass(bool enabled)
        {
            upgradeAudioBypass = enabled;
            ApplyUpgradeAudioBypass(Application.isFocused);
        }

        // Called on focus changes too: "mute when focus is lost" must still win over the bypass.
        private void ApplyUpgradeAudioBypass(bool hasFocus)
        {
            bool bypass = upgradeAudioBypass && (hasFocus || audioData != null && !audioData.MuteAudioOnLostFocus);
            if (musicSource != null) musicSource.ignoreListenerPause = bypass;
            if (sfxSource != null) sfxSource.ignoreListenerPause = bypass;
        }

        private float GetUpgradeMusicVolume() => audioData.UpgradeMusicVolume * masterVolume * musicVolume;

        /// <summary>Soft tick used when the pointer or keyboard moves onto an upgrade card.</summary>
        float nextCardHoverTime;
        public void PlayCardHoverSfx()
        {
            if (audioData == null) return;
            if(Time.unscaledTime<nextCardHoverTime)return;
            nextCardHoverTime=Time.unscaledTime+.06f;
            PlaySfxWithPitch(audioData.CardHoverSfx, audioData.CardHoverVolume, 1f + Random.Range(-0.03f, 0.03f));
        }

        /// <summary>Pick confirmation; higher rarity (0 common … 4 epic) plays a little higher and louder.</summary>
        public void PlayCardPickSfx(int rarity)
        {
            if (audioData == null) return;
            rarity = Mathf.Clamp(rarity, 0, 4);
            PlaySfxWithPitch(audioData.CardPickSfx, 0.75f + rarity * 0.06f, 1f + rarity * 0.045f);
        }

        private void PlaySfxWithPitch(AudioClip clip, float volumeMultiplier, float pitch)
        {
            if (sfxSource == null || sfxMuted || !EnsureClipLoaded(clip)) return;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, audioData.SfxVolume * Mathf.Max(0f, volumeMultiplier));
        }
    }
}
