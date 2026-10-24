using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class ForestGolemBossSettings
    {
        public bool enabled;
        [Min(1)] public int slamsToPulse = 3;
        [Min(0)] public float pulseSeconds = 3f;
        [Min(.05f)] public float pulseTickSeconds = 1f;
        [Min(0)] public float pulseRadius = 3.6f;
        [Min(0)] public float pulseHeight = 2.5f;
        [Range(0, 100)] public float playerMaxHealthDamagePercent = 1f;
        [Min(.05f)] public float regenerationSeconds = 10f;
        [Range(0, 100)] public float regenerationMaxHealthPercent = 1f;
    }

    public sealed partial class ForestGolemAbility
    {
        private int bossSlams;
        private float pulseElapsed, nextPulseTick, pulseScale = 1f, regenerationElapsed;
        private bool pulsing;
        private ForestGrassShockwave bossPulseVisual;
        public bool IsPulsing => pulsing;
        private ForestGolemBossSettings BossSettings => Settings?.bossSkills;
        private bool BossSkillsEnabled => owner != null && owner.IsBoss &&
            BossSettings != null && BossSettings.enabled;

        // BeginSlam is called once at actual contact, not for each victim in the hit volume.
        private void NotifyBossSlam(float hitScale)
        {
            if (!BossSkillsEnabled || pulsing) return;
            if (++bossSlams < Mathf.Max(1, BossSettings.slamsToPulse)) return;
            bossSlams = 0;
            if (BossSettings.pulseSeconds <= 0) return;
            pulsing = true;
            pulseElapsed = 0;
            pulseScale = Mathf.Max(.01f, hitScale);
            nextPulseTick = Mathf.Max(.05f, BossSettings.pulseTickSeconds);
            if (Settings.impactPrefab != null && bossPulseVisual == null)
            {
                bossPulseVisual = Instantiate(Settings.impactPrefab, transform);
                bossPulseVisual.name = "Forest boss pulse";
            }
            if (bossPulseVisual != null)
            {
                bossPulseVisual.PlayControlled(BossSettings.pulseRadius * pulseScale, pulseScale);
                Vector3 parent = transform.lossyScale;
                bossPulseVisual.transform.localScale = new Vector3(pulseScale / Mathf.Max(.01f, Mathf.Abs(parent.x)),
                    pulseScale / Mathf.Max(.01f, Mathf.Abs(parent.y)), pulseScale / Mathf.Max(.01f, Mathf.Abs(parent.z)));
                PresentBossPulse(0);
            }
        }

        private void TickBossSkills(float delta)
        {
            if (!BossSkillsEnabled || owner.Health == null || owner.Health.Health <= 0 || owner.IsDespawning)
            { ResetBossSkills(); return; }

            float regenInterval = Mathf.Max(.05f, BossSettings.regenerationSeconds);
            regenerationElapsed += delta;
            int regenTicks = Mathf.FloorToInt(regenerationElapsed / regenInterval);
            if (regenTicks > 0)
            {
                regenerationElapsed -= regenTicks * regenInterval;
                // This passive heals exactly its configured max-HP percentage, not healing bonuses.
                owner.Health.Heal(owner.Health.MaxHealth * Mathf.Clamp(BossSettings.regenerationMaxHealthPercent, 0, 100)
                    * .01f * regenTicks / owner.Health.HealingMultiplier);
            }
            if (!pulsing) return;
            float duration = Mathf.Max(0, BossSettings.pulseSeconds);
            float interval = Mathf.Max(.05f, BossSettings.pulseTickSeconds);
            pulseElapsed = Mathf.Min(duration, pulseElapsed + delta);
            // Include the terminal tick at exactly 3 s. Large frames do not drop ticks.
            while (nextPulseTick <= pulseElapsed + .00001f && nextPulseTick <= duration + .00001f)
            {
                ApplyBossPulseDamage();
                nextPulseTick += interval;
            }
            if (pulseElapsed >= duration)
            {
                pulsing = false;
                if (bossPulseVisual != null) bossPulseVisual.Stop();
            }
            else PresentBossPulse(Mathf.Repeat(pulseElapsed, interval) / interval);
        }

        private void ApplyBossPulseDamage()
        {
            if (player == null || !player.isActiveAndEnabled || player.Health <= 0) return;
            Vector3 origin = transform.position;
            Vector3 point = playerCollider != null && playerCollider.enabled
                ? playerCollider.ClosestPoint(origin + Vector3.up * .5f) : player.transform.position;
            float range = Mathf.Max(0, BossSettings.pulseRadius) * pulseScale;
            if (Mathf.Abs(point.y - origin.y) > BossSettings.pulseHeight * pulseScale ||
                Vector3.ProjectOnPlane(point - origin, Vector3.up).sqrMagnitude > range * range ||
                !owner.HasStrikeLineOfSight(player.transform, point)) return;
            player.DealDamage(player.MaxHealth * Mathf.Clamp(BossSettings.playerMaxHealthDamagePercent, 0, 100)
                * .01f, CombatDamageType.True);
        }

        private void PresentBossPulse(float progress)
        {
            if (bossPulseVisual == null) return;
            Vector3 point = transform.position;
            if (WorldNavigationGrid.Instance != null && WorldNavigationGrid.Instance.TryGetGroundPoint(point, out Vector3 ground))
                point = ground;
            else
            {
                foreach (Terrain terrain in Terrain.activeTerrains)
                {
                    Vector3 local = point - terrain.transform.position, size = terrain.terrainData.size;
                    if (local.x < 0 || local.z < 0 || local.x > size.x || local.z > size.z) continue;
                    point.y = terrain.SampleHeight(point) + terrain.transform.position.y;
                    break;
                }
            }
            bossPulseVisual.transform.SetPositionAndRotation(point, Quaternion.identity);
            bossPulseVisual.Present(progress);
        }

        private void ResetBossSkills()
        {
            pulsing = false;
            bossSlams = 0;
            pulseElapsed = nextPulseTick = regenerationElapsed = 0;
            if (bossPulseVisual != null) bossPulseVisual.Stop();
        }
    }
}
