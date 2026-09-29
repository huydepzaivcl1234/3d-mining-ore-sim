using UnityEngine;

namespace MiningSimulator.Ores
{
    [RequireComponent(typeof(MiningCharacterHealth), typeof(CharacterController))]
    public sealed class MushroomMonster : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [Min(0f), SerializeField] private float moveSpeed = 1.5f;
        [Min(0f), SerializeField] private float detectionRange = 6f;
        [Min(0.1f), SerializeField] private float attackRange = 1.5f;
        [Range(1f, 180f), SerializeField] private float attackArc = 120f;
        [Min(0f), SerializeField] private float damage = 5f;
        [Min(0.1f), SerializeField] private float attackCooldown = 2f;
        [Range(0f, 1f), SerializeField] private float hitMoment = 0.45f;
        [Min(0f), SerializeField] private float deathDelay = 3f;
        [SerializeField] private MonsterRewardData rewards;
        private bool rewardsGranted;
        private CharacterController motor;
        private MiningCharacterHealth health;
        private MiningCharacterHealth target;
        private Collider targetCollider;
        private MonsterSpawnZone zone;
        private Vector3 destination;
        private float nextDecision, nextAttack, verticalSpeed;
        private bool hitApplied;
        private int animationState;
        private static readonly int HeadbuttState = Animator.StringToHash("Headbutt");
        public MiningCharacterHealth Health => health;
        public int Level { get; private set; } = 1;
        private float scaledDamage;
        private float scaledBurnDamage;
        public void Initialize(MonsterSpawnZone owner, MiningCharacterHealth player)
        {
            zone = owner;
            target = player;
            targetCollider = player != null ? player.GetComponent<Collider>() : null;
            destination = transform.position;
            Level = rewards != null ? rewards.RollLevel(Random.value) : 1;
            float scale = rewards != null ? rewards.StatMultiplier(Level) : 1;
            float low = rewards != null ? Mathf.Max(0.01f, Mathf.Min(rewards.randomStatMultiplier.x, rewards.randomStatMultiplier.y)) : 1;
            float high = rewards != null ? Mathf.Max(low, Mathf.Max(rewards.randomStatMultiplier.x, rewards.randomStatMultiplier.y)) : 1;
            scaledDamage = damage * scale * Random.Range(low, high);
            scaledBurnDamage = rewards != null ? rewards.burnDamagePerTick * scale : 0f;
            health.ConfigureSpawnHealth(health.MaxHealth * scale * Random.Range(low, high));
            if (rewards != null) health.ConfigureHealingBonus(rewards.healingBonusPercent);
        }
        private void Awake()
        {
            health = GetComponent<MiningCharacterHealth>();
            motor = GetComponent<CharacterController>();
            scaledDamage = damage;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            health.Damaged += OnDamage;
            health.Died += OnDeath;
        }
        private void OnDestroy()
        {
            if (health == null) return;
            health.Damaged -= OnDamage;
            health.Died -= OnDeath;
        }
        private void OnDamage()
        {
            if (health.Health <= 0f) return;
            // Preserve the committed contact frame, then allow the hit reaction.
            // Otherwise a player's strike can cancel every incoming headbutt.
            if (animationState != HeadbuttState || hitApplied) Play("Damage");
        }
        private void OnDeath()
        {
            if (rewardsGranted) return;
            rewardsGranted = true;
            Play("Down");
            motor.enabled = false;
            if (zone != null) zone.GrantRewards(rewards, transform.position + Vector3.up * 0.6f, rewards != null ? rewards.GoldMultiplier(Level) : 1);
            Destroy(gameObject, deathDelay);
        }
        private void Play(string name, bool restart = false)
        {
            int hash = Animator.StringToHash(name);
            if ((!restart && animationState == hash) || animator == null) return;
            animationState = hash;
            animator.CrossFadeInFixedTime(hash, 0.15f);
        }
        private void Update()
        {
            if (health.Health <= 0f || animator == null) return;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            bool attacking = state.IsName("Headbutt");
            bool reacting = state.IsName("Damage");
            if (attacking && !animator.IsInTransition(0) && !hitApplied &&
                state.normalizedTime >= hitMoment && state.normalizedTime < 1f)
            {
                hitApplied = true;
                if (CanHitTarget())
                {
                    float dealt = target.DealDamage(scaledDamage);
                    if (dealt > 0f && rewards != null)
                    {
                        health.Heal(dealt * Mathf.Clamp(rewards.lifeStealPercent, 0f, 100f) * 0.01f);
                        target.ApplyBurn(scaledBurnDamage, rewards.burnTickSeconds,
                            rewards.burnDurationSeconds);
                    }
                }
            }
            Vector3 movement = Vector3.zero;
            if (!animator.IsInTransition(0) && ((!attacking && !reacting) || state.normalizedTime >= 1f))
            {
                bool chasing = target != null && target.Health > 0f &&
                    (zone == null || zone.Contains(target.transform.position)) &&
                    Vector3.Distance(transform.position, target.transform.position) <= detectionRange;
                if (chasing) destination = target.transform.position;
                else if (zone != null && Time.time >= nextDecision)
                {
                    destination = zone.RandomPoint();
                    nextDecision = Time.time + Random.Range(3f, 6f);
                }
                Vector3 delta = destination - transform.position;
                delta.y = 0f;
                if (chasing && delta.magnitude <= attackRange && Time.time >= nextAttack)
                {
                    nextAttack = Time.time + attackCooldown;
                    hitApplied = false;
                    Play("Headbutt", true);
                }
                else if (delta.magnitude > (chasing ? attackRange : 0.3f))
                {
                    movement = delta.normalized * moveSpeed;
                    if (zone != null && !zone.Contains(transform.position + movement * Time.deltaTime)) movement = Vector3.zero;
                    Play(movement.sqrMagnitude > 0f ? "Walk" : "Idle");
                }
                else Play("Idle");
                if (delta.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(delta), 240f * Time.deltaTime);
            }
            verticalSpeed = motor.isGrounded ? -2f : verticalSpeed + Physics.gravity.y * Time.deltaTime;
            motor.Move((movement + Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        private bool CanHitTarget()
        {
            if (target == null || target.Health <= 0f) return false;
            Vector3 origin = transform.TransformPoint(motor.center);
            Vector3 point = targetCollider != null && targetCollider.enabled
                ? targetCollider.ClosestPoint(origin) : target.transform.position;
            Vector3 direction = Vector3.ProjectOnPlane(point - origin, Vector3.up);
            if (direction.sqrMagnitude > (attackRange + 0.3f) * (attackRange + 0.3f)) return false;
            return direction.sqrMagnitude < 0.0001f ||
                Vector3.Angle(transform.forward, direction) <= attackArc * 0.5f;
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
