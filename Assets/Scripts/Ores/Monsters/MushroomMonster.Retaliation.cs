using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MushroomMonster
    {
        private MiningCharacterHealth retaliationTarget;
        private readonly MonsterRetaliationClock retaliationClock = new();
        private bool returningFromRetaliation;
        public bool IsRetaliating => retaliationClock.Active;

        private void ResetRetaliation()
        {
            retaliationClock.Reset();
            retaliationTarget = null;
            returningFromRetaliation = false;
        }

        private void RememberPlayerAttacker()
        {
            var source = health.LastDamageSource;
            if (source == null) return;
            var attacker = source.GetComponentInParent<MiningCharacterHealth>();
            if (attacker == null || attacker.Health <= 0f || !attacker.gameObject.activeInHierarchy) return;
            if (attacker != playerTarget && source.GetComponentInParent<PlayerCombatInput>(true) == null) return;
            bool takeover = !retaliationClock.Active || retaliationTarget != attacker;
            retaliationTarget = playerTarget = attacker;
            returningFromRetaliation = false;
            if (!takeover) return; // Repeated hits/DOT must not bypass attack recovery.
            retaliationClock.Acquire();
            if (animationState != attackStateHash) nextAttack = Mathf.Min(nextAttack, Time.time);
        }

        private bool PlayerInsideRetaliationRange(MiningCharacterHealth player)
        {
            return player != null && player.Health > 0f && player.gameObject.activeInHierarchy &&
                Vector3.ProjectOnPlane(player.transform.position - transform.position, Vector3.up).sqrMagnitude <=
                EffectiveDetectionRange * EffectiveDetectionRange;
        }

        private void TickRetaliation(float dt)
        {
            if (returningFromRetaliation && PlayerInsideRetaliationRange(playerTarget)) returningFromRetaliation = false;
            bool valid = retaliationTarget != null && retaliationTarget.Health > 0f && retaliationTarget.gameObject.activeInHierarchy;
            if (!retaliationClock.Tick(valid, PlayerInsideRetaliationRange(retaliationTarget), dt,
                    (CombatData ?? legacyNavigation).retaliationForgetSeconds)) return;
            retaliationTarget = null;
            returningFromRetaliation = true;
            playerDetected = false;
            navigation?.Reset();
            nextDecision = 0f;
            // A committed strike/landing finishes first. Target changes only at the next AI decision.
        }

        private bool TrySelectRetaliationTarget()
        {
            if (!retaliationClock.Active || retaliationTarget == null) return false;
            if (target != retaliationTarget)
            {
                navigation?.Reset();
                target = retaliationTarget;
                targetCollider = target.GetComponent<Collider>();
            }
            playerDetected = true;
            lastKnownPlayer = target.transform.position;
            return true;
        }
    }

    /// <summary>Continuous time outside range, independent of AI scan cadence and attack playback.</summary>
    public sealed class MonsterRetaliationClock
    {
        public bool Active { get; private set; }
        public float OutsideSeconds { get; private set; }
        public void Acquire() { Active = true; OutsideSeconds = 0f; }
        public void Reset() { Active = false; OutsideSeconds = 0f; }
        public bool Tick(bool valid, bool insideRange, float dt, float grace)
        {
            if (!Active) return false;
            OutsideSeconds = insideRange ? 0f : OutsideSeconds + Mathf.Max(0f, dt);
            if (valid && OutsideSeconds < Mathf.Max(.1f, grace)) return false;
            Reset();
            return true;
        }
    }
}
