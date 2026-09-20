using UnityEngine;
using UnityEngine.Audio;

namespace MiningSimulator.Ores
{
    /// <summary>Designer-owned music and sound-effect configuration.</summary>
    [CreateAssetMenu(fileName = "MiningAudioData", menuName = "Mining Simulator/Game Data/Audio")]
    public sealed class MiningAudioData : ScriptableObject
    {
        [Header("Background Music")]
        [SerializeField] private AudioClip backgroundMusic;
        [Tooltip("Theme played while the full-screen Gem Shop is open.")]
        [SerializeField] private AudioClip shopMusic;
        [SerializeField] private bool playMusicOnStart = true;
        [SerializeField] private bool loopMusic = true;
        [Range(0f, 1f), SerializeField] private float musicVolume = 0.55f;
        [SerializeField] private AudioMixerGroup musicMixerGroup;

        [Header("World Ambience")]
        [Tooltip("Loop played while the DayNightSystem is in its Day period.")]
        [SerializeField] private AudioClip morningAmbience;
        [Tooltip("Optional extra day clips. The main Morning Ambience above is always track 1; these are picked randomly after a day track ends.")]
        [SerializeField] private AudioClip[] additionalMorningAmbiences = System.Array.Empty<AudioClip>();
        [Tooltip("Silent time after a day ambience track ends, before the next random day clip fades in.")]
        [SerializeField] private Vector2 morningAmbiencePauseRange = new(2f, 5f);
        [Tooltip("Loop played while the DayNightSystem is in its Night period.")]
        [SerializeField] private AudioClip nightAmbience;
        [Tooltip("Optional extra night clips. The main Night Ambience above is always track 1; these are picked randomly after a night track ends.")]
        [SerializeField] private AudioClip[] additionalNightAmbiences = System.Array.Empty<AudioClip>();
        [Tooltip("Silent time after a night ambience track ends, before the next random night clip fades in.")]
        [SerializeField] private Vector2 nightAmbiencePauseRange = new(2f, 5f);
        [Tooltip("One-shot cue played as the world changes from night to morning.")]
        [SerializeField] private AudioClip sunriseRoosterSfx;
        [Tooltip("Loop played only while the Coin Rain event is active.")]
        [SerializeField] private AudioClip coinRainAmbience;
        [Tooltip("Loop played while the player is in the Underground area.")]
        [SerializeField] private AudioClip undergroundAmbience;
        [Range(0f, 1f), SerializeField] private float undergroundAmbienceVolume = 0.55f;
        [Header("Being Stalked Event")]
        [SerializeField] private AudioClip stalkedCatchSfx;
        [SerializeField] private AudioClip stalkedJumpscareSfx;
        [Range(0f, 1f), SerializeField] private float ambienceVolume = 0.45f;
        [Min(0f), SerializeField] private float ambienceFadeDuration = 2.5f;

        [Header("Gameplay SFX")]
        [SerializeField] private AudioClip oreHitSfx;
        [SerializeField] private AudioClip oreBreakSfx;
        [SerializeField] private AudioClip npcPurchasedSfx;
        [SerializeField] private AudioClip upgradePurchasedSfx;
        [SerializeField] private AudioClip levelUpSfx;
        [SerializeField] private AudioClip rebirthSfx;

        [Header("UI SFX")]
        [SerializeField] private AudioClip buttonClickSfx;
        [SerializeField] private AudioClip panelOpenSfx;
        [SerializeField] private AudioClip panelCloseSfx;
        [Tooltip("Optional. Falls back to Ore Hit SFX when empty.")]
        [SerializeField] private AudioClip wheelSpinSfx;
        [Tooltip("Optional. Falls back to Level Up SFX when empty.")]
        [SerializeField] private AudioClip wheelRewardSfx;

        [Header("SFX Playback")]
        [Range(0f, 1f), SerializeField] private float sfxVolume = 0.8f;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [Range(0.1f, 3f), SerializeField] private float minimumPitch = 0.96f;
        [Range(0.1f, 3f), SerializeField] private float maximumPitch = 1.04f;
        [Tooltip("Minimum duration between consecutive ore hit SFX to prevent voice congestion / lag.")]
        [Range(0.01f, 0.5f), SerializeField] private float miningSfxCooldown = 0.05f;
        [Tooltip("Mute audio output when the application loses focus to prevent OS audio desync.")]
        [SerializeField] private bool muteAudioOnLostFocus = true;

        public AudioClip BackgroundMusic => backgroundMusic;
        public AudioClip ShopMusic => shopMusic;
        public bool PlayMusicOnStart => playMusicOnStart;
        public bool LoopMusic => loopMusic;
        public float MusicVolume => musicVolume;
        public AudioMixerGroup MusicMixerGroup => musicMixerGroup;
        public AudioClip MorningAmbience => morningAmbience;
        public AudioClip NightAmbience => nightAmbience;
        public int MorningAmbienceCount => 1 + additionalMorningAmbiences.Length;
        public int NightAmbienceCount => 1 + additionalNightAmbiences.Length;
        public AudioClip SunriseRoosterSfx => sunriseRoosterSfx;
        public AudioClip CoinRainAmbience => coinRainAmbience;
        public AudioClip UndergroundAmbience => undergroundAmbience;
        public float UndergroundAmbienceVolume => undergroundAmbienceVolume;
        public AudioClip StalkedCatchSfx => stalkedCatchSfx;
        public AudioClip StalkedJumpscareSfx => stalkedJumpscareSfx;
        public float AmbienceVolume => ambienceVolume;
        public float AmbienceFadeDuration => ambienceFadeDuration;

        public AudioClip GetMorningAmbienceAt(int index)
        {
            if (index == 0)
            {
                return morningAmbience;
            }

            int additionalIndex = index - 1;
            return additionalIndex >= 0 && additionalIndex < additionalMorningAmbiences.Length
                ? additionalMorningAmbiences[additionalIndex]
                : null;
        }

        public AudioClip GetNightAmbienceAt(int index)
        {
            if (index == 0)
            {
                return nightAmbience;
            }

            int additionalIndex = index - 1;
            return additionalIndex >= 0 && additionalIndex < additionalNightAmbiences.Length
                ? additionalNightAmbiences[additionalIndex]
                : null;
        }

        public AudioClip GetRandomMorningAmbience(AudioClip previousClip)
        {
            return GetRandomAmbience(false, previousClip);
        }

        public AudioClip GetRandomNightAmbience(AudioClip previousClip)
        {
            return GetRandomAmbience(true, previousClip);
        }

        public float GetRandomMorningAmbiencePause()
        {
            return Random.Range(morningAmbiencePauseRange.x, morningAmbiencePauseRange.y);
        }

        public float GetRandomNightAmbiencePause()
        {
            return Random.Range(nightAmbiencePauseRange.x, nightAmbiencePauseRange.y);
        }

        private AudioClip GetRandomAmbience(bool night, AudioClip previousClip)
        {
            int count = night ? NightAmbienceCount : MorningAmbienceCount;
            int availableCount = 0;
            for (int index = 0; index < count; index++)
            {
                AudioClip candidate = night ? GetNightAmbienceAt(index) : GetMorningAmbienceAt(index);
                if (candidate != null && candidate != previousClip)
                {
                    availableCount++;
                }
            }

            if (availableCount == 0)
            {
                return night ? nightAmbience : morningAmbience;
            }

            int selectedIndex = Random.Range(0, availableCount);
            for (int index = 0; index < count; index++)
            {
                AudioClip candidate = night ? GetNightAmbienceAt(index) : GetMorningAmbienceAt(index);
                if (candidate == null || candidate == previousClip)
                {
                    continue;
                }

                if (selectedIndex-- == 0)
                {
                    return candidate;
                }
            }

            return night ? nightAmbience : morningAmbience;
        }
        public AudioClip OreHitSfx => oreHitSfx;
        public AudioClip OreBreakSfx => oreBreakSfx;
        public AudioClip NpcPurchasedSfx => npcPurchasedSfx;
        public AudioClip UpgradePurchasedSfx => upgradePurchasedSfx;
        public AudioClip LevelUpSfx => levelUpSfx;
        public AudioClip RebirthSfx => rebirthSfx;
        public AudioClip PanelOpenSfx => panelOpenSfx;
        public AudioClip ButtonClickSfx => buttonClickSfx;
        public AudioClip PanelCloseSfx => panelCloseSfx;
        public AudioClip WheelSpinSfx => wheelSpinSfx != null ? wheelSpinSfx : oreHitSfx;
        public AudioClip WheelRewardSfx => wheelRewardSfx != null ? wheelRewardSfx : levelUpSfx;
        public float SfxVolume => sfxVolume;
        public AudioMixerGroup SfxMixerGroup => sfxMixerGroup;
        public float MinimumPitch => minimumPitch;
        public float MaximumPitch => maximumPitch;
        public float MiningSfxCooldown => miningSfxCooldown;
        public bool MuteAudioOnLostFocus => muteAudioOnLostFocus;

        private void OnValidate()
        {
            musicVolume = Mathf.Clamp01(musicVolume);
            ambienceVolume = Mathf.Clamp01(ambienceVolume);
            undergroundAmbienceVolume = Mathf.Clamp01(undergroundAmbienceVolume);
            ambienceFadeDuration = Mathf.Max(0f, ambienceFadeDuration);
            morningAmbiencePauseRange.x = Mathf.Max(0f, morningAmbiencePauseRange.x);
            morningAmbiencePauseRange.y = Mathf.Max(morningAmbiencePauseRange.x,
                morningAmbiencePauseRange.y);
            nightAmbiencePauseRange.x = Mathf.Max(0f, nightAmbiencePauseRange.x);
            nightAmbiencePauseRange.y = Mathf.Max(nightAmbiencePauseRange.x,
                nightAmbiencePauseRange.y);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            minimumPitch = Mathf.Clamp(minimumPitch, 0.1f, 3f);
            maximumPitch = Mathf.Clamp(maximumPitch, minimumPitch, 3f);
            miningSfxCooldown = Mathf.Clamp(miningSfxCooldown, 0.01f, 0.5f);
        }
    }
}
