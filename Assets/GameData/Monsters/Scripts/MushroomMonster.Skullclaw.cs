using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MushroomMonster
    {
        private SkullclawData Skullclaw => SpeciesData as SkullclawData;
        private readonly SkullclawCombo skullCombo = new();
        private Vector3 skullJumpStart, skullJumpEnd;
        private float skullJumpProgress;
        private bool skullJumpActive;
        private readonly SkullclawJumpGate skullJumpGate = new();
        private MiningCharacterHealth skullApproachTarget;
        private float SkullclawTargetDistance => targetCollider != null
            ? Vector3.ProjectOnPlane(targetCollider.ClosestPoint(transform.TransformPoint(motor.center)) - transform.position, Vector3.up).magnitude
            : target != null ? Vector3.ProjectOnPlane(target.transform.position - transform.position, Vector3.up).magnitude : float.PositiveInfinity;
        private void TickSkullclawApproach(float dt)
        {
            if (Skullclaw == null) return;
            if (skullApproachTarget != target) { skullJumpGate.Reset(); skullApproachTarget = target; }
            float distance = SkullclawTargetDistance;
            // The selected target can be a player, tower or chest. Navigation
            // approaches all three at jump range; none may stall there forever.
            skullJumpGate.Tick(target != null && target.Health > 0f &&
                target.gameObject.activeInHierarchy && !IsSkullclawJump &&
                distance > EffectiveAttackRange && distance <= Skullclaw.jumpRange * HitScale, dt);
        }
        private bool CanSkullclawEngage => SkullclawTargetDistance <= EffectiveAttackRange ||
            (SkullclawTargetDistance <= Skullclaw.jumpRange * HitScale && skullJumpGate.Ready(Skullclaw.jumpApproachDelay));
        public int SkullclawAttackStep => skullCombo.Current;
        private bool IsSkullclawJump => Skullclaw != null && skullCombo.Current == 2 && animationState == attackStateHash;
        private float EngagementRange => Skullclaw != null
            ? Mathf.Max(EffectiveAttackRange, Skullclaw.jumpRange * HitScale) : EffectiveAttackRange;

        private bool FinishSkullclawAttack(AnimatorStateInfo state)
        {
            if (Skullclaw == null || animationState != attackStateHash ||
                animator.IsInTransition(0) || !state.IsName(ActiveAttackState) || state.normalizedTime < 1f) return false;
            // Recover after the complete clip, whether contact hit or missed. Never chain this frame.
            nextAttack = Time.time + Mathf.Max(.15f, EffectiveAttackCooldown);
            Play("Idle");
            navigation?.Reset();
            return true;
        }

        private void BeginSkullclawAttack()
        {
            Vector3 victimPoint = targetCollider != null
                ? targetCollider.ClosestPoint(transform.TransformPoint(motor.center))
                : target != null ? target.transform.position : transform.position;
            float distance = Vector3.ProjectOnPlane(victimPoint - transform.position, Vector3.up).magnitude;
            skullCombo.Begin(distance > EffectiveAttackRange);
            skullJumpGate.Reset();
            skullJumpActive = skullCombo.Current == 2;
            skullJumpProgress = 0f;
            skullJumpStart = transform.position;
            Vector3 goal = target != null ? target.transform.position : transform.position;
            if (targetCollider != null)
            {
                goal = targetCollider.ClosestPoint(transform.position);
                Vector3 away = Vector3.ProjectOnPlane(transform.position - goal, Vector3.up).normalized;
                goal += away * (motor.radius * HitScale + .08f);
            }
            Vector3 delta = Vector3.ProjectOnPlane(goal - skullJumpStart, Vector3.up);
            skullJumpEnd = skullJumpStart + Vector3.ClampMagnitude(delta, Skullclaw.jumpRange * HitScale);
            navigation?.Reset();
        }

        // Called by the existing movement owner. No second Update or root-motion writer.
        private bool MoveSkullclawJump(float normalizedTime)
        {
            if (!IsSkullclawJump || !skullJumpActive) return false;
            var data = Skullclaw;
            float progress = Mathf.InverseLerp(data.takeoff, Mathf.Max(data.takeoff + .01f, data.landing), normalizedTime);
            float lift = Mathf.Max(0f, data.jumpHeight.Evaluate(Mathf.Clamp01(normalizedTime))) * HitScale;
            Vector3 horizontal = (skullJumpEnd - skullJumpStart) * (progress - skullJumpProgress);
            skullJumpProgress = progress;
            float vertical = skullJumpStart.y + lift - transform.position.y;
            motor.Move(horizontal + Vector3.up * (vertical - (progress >= 1f ? .03f : 0f)));
            verticalSpeed = 0f;
            strikeCenter = new Vector3(transform.position.x, skullJumpStart.y, transform.position.z);
            if (groundWarning != null) groundWarning.MoveCenter(progress < 1f ? skullJumpEnd : strikeCenter, transform);
            if (normalizedTime >= data.landing) skullJumpActive = false;
            return true;
        }

        private void ApplySkullclawAreaHit()
        {
            // Players and the defended chest are distinct victims; never double-hit the current target.
            DamageSkullclawAreaVictim(playerTarget);
            var chestHealth = TreasureChest.Active != null ? TreasureChest.Active.Health : null;
            if (chestHealth != playerTarget) DamageSkullclawAreaVictim(chestHealth);
            if (target != playerTarget && target != chestHealth) DamageSkullclawAreaVictim(target);
        }

        private void DamageSkullclawAreaVictim(MiningCharacterHealth victim)
        {
            if (victim == null || victim.Health <= 0f || !victim.gameObject.activeInHierarchy) return;
            if (!ContainsVictim(victim.transform, victim.GetComponent<Collider>())) return;
            ApplyProjectileHit(victim); // shared armor, burn, lifesteal and boss damage rules
        }
    }
}
