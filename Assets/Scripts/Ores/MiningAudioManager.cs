using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns background music and event-driven mining SFX playback.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningAudioManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MiningAudioData audioData;
        [SerializeField] private OreSpawner oreSpawner;
        [SerializeField] private NpcShop npcShop;
        [SerializeField] private MiningUpgradeSystem upgradeSystem;
        [SerializeField] private MiningUpgradePanel upgradePanel;
        [SerializeField] private MiningRebirthSystem rebirthSystem;
        [SerializeField] private MiningRebirthPanel rebirthPanel;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        private float nextOreHitSfxTime;
        private bool musicMuted;
        private bool sfxMuted;

        public MiningAudioData AudioData => audioData;
        public bool MusicMuted => musicMuted;
        public bool SfxMuted => sfxMuted;

        private void Awake()
        {
            ResolveSources();
            ConfigureSources();
        }

        private void OnEnable()
        {
            if (oreSpawner != null)
            {
                oreSpawner.OreDamaged -= HandleOreDamaged;
                oreSpawner.OreDamaged += HandleOreDamaged;
                oreSpawner.OreRewardGranted -= HandleOreRewardGranted;
                oreSpawner.OreRewardGranted += HandleOreRewardGranted;
            }

            if (npcShop != null)
            {
                npcShop.NpcPurchased -= HandleNpcPurchased;
                npcShop.NpcPurchased += HandleNpcPurchased;
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

            ResolveSources();
            ConfigureSources();
        }

        private void Start()
        {
            if (audioData != null && audioData.PlayMusicOnStart)
            {
                PlayBackgroundMusic();
            }
        }

        private void OnDisable()
        {
            if (oreSpawner != null)
            {
                oreSpawner.OreDamaged -= HandleOreDamaged;
                oreSpawner.OreRewardGranted -= HandleOreRewardGranted;
            }

            if (npcShop != null)
            {
                npcShop.NpcPurchased -= HandleNpcPurchased;
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
        }

        public void PlayBackgroundMusic()
        {
            if (audioData == null || musicSource == null ||
                !EnsureClipLoaded(audioData.BackgroundMusic) || musicMuted)
            {
                return;
            }

            ConfigureSources();
            if (musicSource.clip != audioData.BackgroundMusic)
            {
                musicSource.clip = audioData.BackgroundMusic;
            }

            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        public void StopBackgroundMusic()
        {
            musicSource?.Stop();
        }

        public void SetMusicMuted(bool muted)
        {
            musicMuted = muted;
            if (musicSource != null)
            {
                musicSource.mute = muted;
            }

            if (!muted)
            {
                PlayBackgroundMusic();
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

        public void PlaySfx(AudioClip clip)
        {
            if (audioData == null || sfxSource == null || sfxMuted || !EnsureClipLoaded(clip))
            {
                return;
            }

            sfxSource.pitch = Random.Range(audioData.MinimumPitch, audioData.MaximumPitch);
            sfxSource.PlayOneShot(clip, audioData.SfxVolume);
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
                musicSource.volume = audioData.MusicVolume;
                musicSource.spatialBlend = 0f;
                musicSource.mute = musicMuted;
                musicSource.outputAudioMixerGroup = audioData.MusicMixerGroup;
            }

            if (sfxSource != null)
            {
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.volume = 1f;
                sfxSource.spatialBlend = 0f;
                sfxSource.mute = sfxMuted;
                sfxSource.outputAudioMixerGroup = audioData.SfxMixerGroup;
            }
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
            if (sfxSource == musicSource)
            {
                sfxSource = ResolveSource(null, audioRoot, "SFX Source");
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

            return sourceTransform.GetComponent<AudioSource>() ??
                   sourceTransform.gameObject.AddComponent<AudioSource>();
        }

        private static bool EnsureClipLoaded(AudioClip clip)
        {
            if (clip == null || clip.loadState == AudioDataLoadState.Failed)
            {
                return false;
            }

            return clip.loadState != AudioDataLoadState.Unloaded || clip.LoadAudioData();
        }

        private void HandleOreDamaged(Ore ore)
        {
            if (audioData == null || Time.unscaledTime < nextOreHitSfxTime)
            {
                return;
            }

            nextOreHitSfxTime = Time.unscaledTime + audioData.OreHitMinimumInterval;
            PlaySfx(audioData.OreHitSfx);
        }

        private void HandleOreRewardGranted(Ore ore, int reward)
        {
            if (audioData != null)
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
    }
}
