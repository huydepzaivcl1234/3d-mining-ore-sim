using StarterAssets;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [DefaultExecutionOrder(-900), DisallowMultipleComponent, RequireComponent(typeof(MiningPlayerStats))]
    public sealed class MiningPlayerStamina : MonoBehaviour
    {
        private MiningPlayerStats stats;
        private ThirdPersonController movement;
        private StarterAssetsInputs input;
        private MiningCharacterHealth health;
        private MiningAudioManager audioManager;
        private AudioSource fallbackSource;
        private AudioClip generatedBreathing;
        private float regenTimer, nextBreathing;
        private bool wasDead;
        public float Current { get; private set; }
        public float Maximum => stats != null && stats.Data != null ? Mathf.Max(1, stats.Data.maxStamina) : 100;
        public bool Exhausted { get; private set; }
        private void Awake()
        {
            stats = GetComponent<MiningPlayerStats>();
            movement = GetComponent<ThirdPersonController>();
            input = GetComponent<StarterAssetsInputs>();
            health = GetComponent<MiningCharacterHealth>();
            audioManager = FindAnyObjectByType<MiningAudioManager>();
            Current = Maximum;
        }
        private void Update()
        {
            var data = stats.Data;
            if (data == null || movement == null || input == null) return;
            bool dead = health != null && health.Health <= 0;
            if (wasDead && !dead) { Current = Maximum; Exhausted = false; regenTimer = 0; }
            wasDead = dead;
            Current = Mathf.Clamp(Current, 0, Maximum);
            float threshold = Mathf.Clamp(data.staminaResumeThreshold, 0.01f, Maximum);
            if (Exhausted && Current >= threshold) Exhausted = false;
            movement.SprintAllowed = !Exhausted && !dead;
            bool sprinting = !dead && movement.enabled && input.enabled && input.sprint &&
                input.move.sqrMagnitude > 0.01f && movement.SprintAllowed && data.SprintSpeed > data.MoveSpeed;
            if (sprinting)
            {
                regenTimer = 0;
                Current = Mathf.Max(0, Current - Mathf.Max(0, data.sprintStaminaPerSecond) * Time.deltaTime);
                if (Current <= 0) { Exhausted = true; movement.SprintAllowed = false; }
            }
            else if (!dead && Current < Maximum)
            {
                regenTimer += Time.deltaTime;
                float interval = Mathf.Max(0.1f, data.staminaRegenInterval);
                if (regenTimer >= interval)
                {
                    int ticks = Mathf.FloorToInt(regenTimer / interval);
                    regenTimer -= ticks * interval;
                    Current = Mathf.Min(Maximum, Current + Mathf.Max(0, data.staminaRegenAmount) * ticks);
                }
            }
            else regenTimer = 0;
            if (Exhausted && !dead && Time.time >= nextBreathing && Time.deltaTime > 0)
            {
                nextBreathing = Time.time + Mathf.Max(1, data.breathingInterval);
                AudioClip clip = data.exhaustedBreathing != null ? data.exhaustedBreathing : GetBreathingFallback();
                if (audioManager != null) audioManager.PlaySfx(clip, data.breathingVolume);
                else
                {
                    if (fallbackSource == null)
                    {
                        fallbackSource = gameObject.AddComponent<AudioSource>();
                        fallbackSource.playOnAwake = false;
                        fallbackSource.spatialBlend = 0;
                    }
                    fallbackSource.PlayOneShot(clip, Mathf.Clamp01(data.breathingVolume));
                }
            }
        }
        private AudioClip GetBreathingFallback()
        {
            if (generatedBreathing != null) return generatedBreathing;
            // Soft filtered noise inhale/exhale when no authored breathing clip is assigned.
            const int rate = 22050;
            var samples = new float[rate * 2];
            var random = new System.Random(173);
            float filtered = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float phase = i / (float)rate;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Repeat(phase, 1));
                filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2 - 1, 0.18f);
                samples[i] = filtered * envelope * envelope * (phase < 1 ? 0.4f : 0.55f);
            }
            generatedBreathing = AudioClip.Create("Exhausted breathing fallback", samples.Length, 1, rate, false);
            generatedBreathing.SetData(samples, 0);
            return generatedBreathing;
        }
        private void OnDisable() { if (movement != null) movement.SprintAllowed = true; }
        private void OnDestroy()
        {
            if (generatedBreathing != null) Destroy(generatedBreathing);
            if (fallbackSource != null) Destroy(fallbackSource);
        }
    }
}
