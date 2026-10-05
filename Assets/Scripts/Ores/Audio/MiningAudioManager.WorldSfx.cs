using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MiningAudioManager
    {
        private AudioSource[] worldSfxVoices;
        private int nextWorldVoice;

        // UI/music remain 2D. Only explicitly registered world sounds attenuate.
        public void RegisterWorldSfxSource(AudioSource source, float baseVolume = -1f)
        {
            if (source == null || audioData == null) return;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = audioData.WorldSfxMinimumDistance;
            source.maxDistance = audioData.WorldSfxMaximumDistance;
            source.dopplerLevel = 0f;
            RegisterSfxSource(source, baseVolume);
        }

        public void RegisterWorldSfxSources(GameObject owner)
        {
            if (owner == null) return;
            foreach (var source in owner.GetComponentsInChildren<AudioSource>(true))
                RegisterWorldSfxSource(source);
        }

        public void PlayWorldSfx(AudioClip clip, Vector3 position, float volumeMultiplier = 1f)
        {
            if (!isActiveAndEnabled || audioData == null || sfxMuted || !EnsureClipLoaded(clip)) return;
            if (worldSfxVoices == null)
                worldSfxVoices = new AudioSource[audioData.WorldSfxVoiceCount];

            int index = nextWorldVoice;
            for (int i = 0; i < worldSfxVoices.Length; i++)
            {
                int candidate = (nextWorldVoice + i) % worldSfxVoices.Length;
                if (worldSfxVoices[candidate] == null || !worldSfxVoices[candidate].isPlaying)
                {
                    index = candidate;
                    break;
                }
            }
            var voice = worldSfxVoices[index];
            if (voice == null)
            {
                var channel = new GameObject($"World SFX {index + 1}");
                channel.transform.SetParent(transform, false);
                voice = channel.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                worldSfxVoices[index] = voice;
            }
            voice.Stop();
            voice.transform.position = position;
            RegisterWorldSfxSource(voice, 1f);
            voice.pitch = Random.Range(audioData.MinimumPitch, audioData.MaximumPitch);
            voice.clip = clip;
            // Settings gain is already applied by registration; never multiply it twice.
            voice.PlayOneShot(clip, Mathf.Max(0f, volumeMultiplier));
            nextWorldVoice = (index + 1) % worldSfxVoices.Length;
        }

        public void PlayMiningImpactSfx(bool oreBroken, Vector3 position)
        {
            if (audioData == null) return;
            if (!oreBroken)
            {
                if (Time.unscaledTime < nextAllowedMiningSfxTime) return;
                nextAllowedMiningSfxTime = Time.unscaledTime + audioData.MiningSfxCooldown;
            }
            PlayWorldSfx(oreBroken ? audioData.OreBreakSfx : audioData.OreHitSfx, position);
        }
    }
}
