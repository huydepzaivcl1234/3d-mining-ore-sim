using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Instance-only stamina clock; authored GameData never stores current stamina.</summary>
    public sealed class MushnightStamina
    {
        private readonly MushnightSettings settings;
        private float idleSeconds;
        public float Maximum => Mathf.Max(1f, CombatDamage.NonNegative(settings.maxStamina));
        public float Current { get; private set; }
        public bool IsResting { get; private set; }

        public MushnightStamina(MushnightSettings settings)
        { this.settings = settings; Current = Maximum; }

        public void Tick(float delta, bool moved)
        {
            delta = CombatDamage.NonNegative(delta);
            if (moved && !IsResting)
            {
                idleSeconds = 0f;
                Current = Mathf.Max(0f, Current - CombatDamage.NonNegative(settings.staminaPerMovingSecond) * delta);
                if (Current <= 0f) IsResting = true;
                return;
            }
            float previousIdle = idleSeconds;
            idleSeconds += delta;
            float delay = CombatDamage.NonNegative(settings.staminaRecoveryDelay);
            float recoverTime = Mathf.Max(0f, idleSeconds - delay) - Mathf.Max(0f, previousIdle - delay);
            Current = Mathf.Min(Maximum, Current + CombatDamage.NonNegative(settings.staminaRecoveryPerSecond) * recoverTime);
            if (IsResting && Current >= Maximum * Mathf.Clamp(settings.resumeStaminaFraction, .01f, 1f)) IsResting = false;
        }
    }
}
