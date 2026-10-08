using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>One timed effect. Refresh keeps tick progress and the strongest DPS.</summary>
    public sealed class DamageOverTime
    {
        private float damagePerTick;
        private float interval;
        private float remaining;
        private float elapsed;

        public bool IsActive => remaining > 0f;
        public float DamagePerSecond => IsActive && interval > 0f ? damagePerTick / interval : 0f;

        public void Apply(float damage, float tickSeconds, float duration)
        {
            if (damage <= 0f || tickSeconds <= 0f || duration <= 0f) return;
            tickSeconds = Mathf.Max(0.1f, tickSeconds);
            if (!IsActive || damage / tickSeconds >= damagePerTick / interval)
            {
                float progress = IsActive ? elapsed / interval : 0f;
                damagePerTick = damage;
                interval = tickSeconds;
                elapsed = progress * interval;
            }
            remaining = Mathf.Max(remaining, duration);
        }

        public void Tick(float deltaTime, Action<float> dealDamage)
        {
            if (!IsActive) return;
            float activeTime = Mathf.Min(Mathf.Max(0f, deltaTime), remaining);
            remaining -= activeTime;
            elapsed += activeTime;
            while (interval > 0f && elapsed >= interval)
            {
                elapsed -= interval;
                dealDamage(damagePerTick);
                // The receiver may clear the effect when damage kills it.
            }
            if (remaining <= 0f) Clear();
        }

        public void Clear()
        {
            damagePerTick = interval = remaining = elapsed = 0f;
        }
    }
}
