using UnityEngine;
using System.Collections.Generic;

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
        private MiningNpc targetMiner;
        private Collider minerCollider;
        private float warningUntil;
        private float nextMinerSearch;
        private bool committedMinerAttack;
        private readonly List<Vector3> chasePath = new();
        private readonly RaycastHit[] strikeObstructions = new RaycastHit[32];
        private int chaseWaypoint;
        private float nextRepath;
        private Vector3 lastPathGoal;
        public float WarningRemaining => Mathf.Max(0f, warningUntil - Time.time);
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
        private static readonly List<MushroomMonster> ActiveMonsters = new();
        public static IReadOnlyList<MushroomMonster> Monsters => ActiveMonsters;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetMonsterRegistry() => ActiveMonsters.Clear();
        private void OnEnable()
        {
            if (!ActiveMonsters.Contains(this)) ActiveMonsters.Add(this);
            foreach (MiningNpc miner in MiningNpc.Miners)
                if (miner != null) miner.IgnoreMonsterCollision(this);
        }
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
            ClearMinerWarning();
            if (health == null) return;
            health.Damaged -= OnDamage;
            health.Died -= OnDeath;
        }
        private void OnDamage()
        {
            if (health.Health <= 0f) return;
            ClearMinerWarning();
            // Preserve the committed contact frame, then allow the hit reaction.
            // Otherwise a player's strike can cancel every incoming headbutt.
            if (animationState != HeadbuttState || hitApplied) Play("Damage");
        }
        private void OnDeath()
        {
            ClearMinerWarning();
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
                    float dealt = targetMiner != null
                        ? targetMiner.ApplyMonsterDamage(scaledDamage)
                        : target.DealDamage(scaledDamage);
                    if (dealt > 0f && rewards != null)
                    {
                        health.Heal(dealt * Mathf.Clamp(rewards.lifeStealPercent, 0f, 100f) * 0.01f);
                        if (targetMiner == null && target != null)
                            target.ApplyBurn(scaledBurnDamage, rewards.burnTickSeconds,
                                rewards.burnDurationSeconds);
                    }
                }
            }
            Vector3 movement = Vector3.zero;
            bool warning = warningUntil > 0f;
            if (warning)
            {
                if (!IsMinerTargetValid() || !CanHitTarget())
                {
                    ClearMinerWarning();
                    nextAttack = Time.time + attackCooldown;
                }
                else
                {
                    Vector3 facing = Vector3.ProjectOnPlane(targetMiner.transform.position - transform.position, Vector3.up);
                    if (facing.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(facing), 240f * Time.deltaTime);
                    if (Time.time >= warningUntil)
                    {
                        ClearMinerWarning();
                        nextAttack = Time.time + attackCooldown;
                        hitApplied = false;
                        committedMinerAttack = true;
                        Play("Headbutt", true);
                    }
                }
            }
            if (!warning && !animator.IsInTransition(0) && ((!attacking && !reacting) || state.normalizedTime >= 1f))
            {
                SelectMinerTarget();
                bool chasingPlayer = targetMiner == null && target != null && target.Health > 0f &&
                    (zone == null || zone.IsInsideMine(transform.position)) &&
                    Vector3.Distance(transform.position, target.transform.position) <= detectionRange;
                bool chasing = IsMinerTargetValid() || chasingPlayer;
                if (chasing) destination = targetMiner != null ? targetMiner.transform.position : target.transform.position;
                else if (zone != null && Time.time >= nextDecision)
                {
                    bool found = !zone.IsInsideMine(transform.position)
                        ? zone.TryGetMineEntryPoint(transform.position, out destination)
                        : zone.TryGetMiningApproachPoint(transform.position, out destination);
                    if (!found)
                        destination = transform.position;
                    nextDecision = Time.time + Random.Range(3f, 6f);
                }
                Vector3 delta = destination - transform.position;
                delta.y = 0f;
                if (chasing && delta.magnitude <= attackRange && Time.time >= nextAttack)
                {
                    if (targetMiner != null)
                    {
                        warningUntil = Time.time + Mathf.Max(2f, rewards != null ? rewards.minerWarningSeconds : 2f);
                        targetMiner.SetThreat(this, true);
                        Play("Idle");
                    }
                    else
                    {
                        nextAttack = Time.time + attackCooldown;
                        hitApplied = false;
                        committedMinerAttack = false;
                        Play("Headbutt", true);
                    }
                }
                else if (delta.magnitude > (chasing ? attackRange : 0.3f))
                {
                    movement = ChaseDirection(delta) * moveSpeed;
                    Play(movement.sqrMagnitude > 0f ? "Walk" : "Idle");
                }
                else Play("Idle");
                if (delta.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(movement.sqrMagnitude > 0.01f ? movement : delta), 240f * Time.deltaTime);
            }
            verticalSpeed = motor.isGrounded ? -2f : verticalSpeed + Physics.gravity.y * Time.deltaTime;
            motor.Move((movement + Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        private bool CanHitTarget()
        {
            // A destroyed/disabled miner must not redirect an already committed hit to the player.
            if (committedMinerAttack && targetMiner == null) return false;
            if (targetMiner != null ? !IsMinerTargetValid() : target == null || target.Health <= 0f) return false;
            Vector3 origin = transform.TransformPoint(motor.center);
            Collider victimCollider = targetMiner != null ? minerCollider : targetCollider;
            Vector3 position = targetMiner != null ? targetMiner.transform.position : target.transform.position;
            Vector3 point = victimCollider != null && victimCollider.enabled
                ? victimCollider.ClosestPoint(origin) : position;
            Vector3 direction = Vector3.ProjectOnPlane(point - origin, Vector3.up);
            if (direction.sqrMagnitude > (attackRange + 0.3f) * (attackRange + 0.3f)) return false;
            Vector3 ray = point - origin;
            if (ray.sqrMagnitude > 0.0001f)
            {
                int count = Physics.RaycastNonAlloc(origin, ray.normalized, strikeObstructions,
                    ray.magnitude, ~0, QueryTriggerInteraction.Ignore);
                // A full buffer is ambiguous: fail closed rather than hit through a wall.
                if (count == strikeObstructions.Length) return false;
                Transform victim = targetMiner != null ? targetMiner.transform : target.transform;
                for (int i = 0; i < count; i++)
                {
                    Transform obstacle = strikeObstructions[i].transform;
                    if (obstacle != null && !obstacle.IsChildOf(transform) && !obstacle.IsChildOf(victim)) return false;
                }
            }
            return direction.sqrMagnitude < 0.0001f ||
                Vector3.Angle(transform.forward, direction) <= attackArc * 0.5f;
        }
        private Vector3 ChaseDirection(Vector3 direct)
        {
            // Outside the bake no NavMesh path can start yet. Walk physically to
            // the entry point; the CharacterController still collides with scenery.
            if (zone != null && !zone.IsInsideMine(transform.position))
            {
                chasePath.Clear();
                nextRepath = 0f;
                return zone.TryGetMineEntryPoint(transform.position, out Vector3 entry)
                    ? Vector3.ProjectOnPlane(entry - transform.position, Vector3.up).normalized : Vector3.zero;
            }
            if (Time.time >= nextRepath || (lastPathGoal - destination).sqrMagnitude > 1f)
            {
                nextRepath = Time.time + 0.5f;
                lastPathGoal = destination;
                chaseWaypoint = 0;
                MiningNavigation.TryFindPath(transform.position, destination, chasePath);
            }
            while (chaseWaypoint < chasePath.Count)
            {
                Vector3 delta = Vector3.ProjectOnPlane(chasePath[chaseWaypoint] - transform.position, Vector3.up);
                if (delta.sqrMagnitude > 0.3f * 0.3f) return delta.normalized;
                chaseWaypoint++;
            }
            return MiningNavigation.PathfindingAvailable && chasePath.Count == 0 ? Vector3.zero : direct.normalized;
        }
        private bool IsMinerTargetValid() => targetMiner != null && targetMiner.isActiveAndEnabled &&
            !targetMiner.IsStunned &&
            (zone == null || zone.IsInsideMine(transform.position)) &&
            (targetMiner.transform.position - transform.position).sqrMagnitude <= detectionRange * detectionRange;

        private void SelectMinerTarget()
        {
            if (zone != null && !zone.IsInsideMine(transform.position))
            {
                ClearMinerWarning();
                targetMiner = null;
                minerCollider = null;
                return;
            }
            if (IsMinerTargetValid()) return;
            targetMiner = null;
            minerCollider = null;
            if (Time.time < nextMinerSearch) return;
            nextMinerSearch = Time.time + 0.3f;
            float best = detectionRange * detectionRange;
            foreach (var miner in MiningNpc.Miners)
            {
                if (miner == null || !miner.IsActivelyMining) continue;
                float distance = (miner.transform.position - transform.position).sqrMagnitude;
                if (distance > best) continue;
                best = distance;
                targetMiner = miner;
            }
            if (targetMiner != null) minerCollider = targetMiner.GetComponent<Collider>();
        }

        private void ClearMinerWarning()
        {
            if (targetMiner != null) targetMiner.SetThreat(this, false);
            warningUntil = 0f;
        }
        private void OnDisable()
        {
            ClearMinerWarning();
            ActiveMonsters.Remove(this);
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
