using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum BossSkillKind { None, MushroomHealing, GolemSlow, BatHaste }

    // Per-spawn state. Configuration belongs to species reward data, never mutated here.
    public sealed class BossSkillRuntime
    {
        private MonsterBossSettings settings;
        private bool healUsed;
        private float healRemaining;
        private int strikes, hasteStacks;
        private float playerDamageProgress;
        public float AttackSpeedMultiplier => 1f + hasteStacks *
            (settings != null ? Mathf.Max(0f, settings.hasteAttackSpeedPercent) * .01f : 0f);

        public void Configure(MonsterBossSettings configuration)
        {
            settings = configuration;
            healUsed = false;
            healRemaining = playerDamageProgress = 0f;
            strikes = hasteStacks = 0;
        }

        public void NotifyHealth(float health, float maximum)
        {
            if (settings == null || settings.skill != BossSkillKind.MushroomHealing || healUsed ||
                health <= 0f || maximum <= 0f || health / maximum > Mathf.Clamp01(settings.healTriggerHealthPercent * .01f) + .000001f) return;
            healUsed = true;
            healRemaining = Mathf.Max(.1f, settings.healSeconds);
        }

        // Returns raw HP, deliberately independent from ordinary regeneration/healing bonuses.
        public float TickHealing(float deltaTime, float maximum)
        {
            if (settings == null || healRemaining <= 0f) return 0f;
            float elapsed = Mathf.Min(Mathf.Max(0f, deltaTime), healRemaining);
            healRemaining -= elapsed;
            return elapsed / Mathf.Max(.1f, settings.healSeconds) * Mathf.Max(0f, maximum) *
                Mathf.Clamp01(settings.healMaxHealthPercent * .01f);
        }

        // Count committed contact frames, including misses; never count a cancelled windup.
        public bool NotifyStrike()
        {
            if (settings == null || settings.skill != BossSkillKind.GolemSlow) return false;
            strikes++;
            if (strikes < Mathf.Max(1, settings.slowEveryStrikes)) return false;
            strikes = 0;
            return true;
        }

        public bool NotifyPlayerDamage(float actualDamage, float playerMaximum)
        {
            if (settings == null || settings.skill != BossSkillKind.BatHaste || actualDamage <= 0f ||
                playerMaximum <= 0f || !settings.hasteStacks && hasteStacks > 0) return false;
            playerDamageProgress += actualDamage / playerMaximum;
            float threshold = Mathf.Max(.001f, settings.hasteDamageThresholdPercent * .01f);
            if (playerDamageProgress + .000001f < threshold) return false;
            int gained = settings.hasteStacks ? Mathf.FloorToInt((playerDamageProgress + .000001f) / threshold) : 1;
            hasteStacks += gained;
            playerDamageProgress = Mathf.Max(0f, playerDamageProgress - gained * threshold);
            return true;
        }
    }
}
