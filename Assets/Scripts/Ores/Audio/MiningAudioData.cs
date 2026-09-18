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
        [Tooltip("Loop played while the DayNightSystem is in its Night period.")]
        [SerializeField] private AudioClip nightAmbience;
        [Tooltip("One-shot cue played as the world changes from night to morning.")]
        [SerializeField] private AudioClip sunriseRoosterSfx;
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
        public AudioClip SunriseRoosterSfx => sunriseRoosterSfx;
        public float AmbienceVolume => ambienceVolume;
        public float AmbienceFadeDuration => ambienceFadeDuration;
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
            ambienceFadeDuration = Mathf.Max(0f, ambienceFadeDuration);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            minimumPitch = Mathf.Clamp(minimumPitch, 0.1f, 3f);
            maximumPitch = Mathf.Clamp(maximumPitch, minimumPitch, 3f);
            miningSfxCooldown = Mathf.Clamp(miningSfxCooldown, 0.01f, 0.5f);
        }
    }
}
