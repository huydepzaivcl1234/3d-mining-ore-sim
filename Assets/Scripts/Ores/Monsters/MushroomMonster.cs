using UnityEngine;
using System.Collections.Generic;

namespace MiningSimulator.Ores
{
    [RequireComponent(typeof(MiningCharacterHealth), typeof(CharacterController))]
    public sealed class MushroomMonster : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [Tooltip("Animator state containing this species' attack clip.")]
        [SerializeField] private string attackState = "Headbutt";
        [Min(0f), SerializeField] private float moveSpeed = 1.5f;
        [Min(0f), SerializeField] private float detectionRange = 6f;
        [Min(0.1f), SerializeField] private float attackRange = 1.5f;
        [Range(1f, 180f), SerializeField] private float attackArc = 120f;
        [Min(0f), SerializeField] private float damage = 5f;
        [Min(0.1f), SerializeField] private float attackCooldown = 2f;
        [Range(0f, 1f), SerializeField] private float hitMoment = 0.45f;
        public enum HitShape { Sweep, Area }
        [Header("Hit volume and ground warning")]
        [SerializeField] private HitShape hitShape;
        [Min(.1f), SerializeField] private float areaRadius = 1.6f;
        [Min(0f), SerializeField] private float areaForwardOffset = 1.6f;
        [Min(.1f), SerializeField] private float hitHeight = 2.5f;
        [SerializeField] private Color warningColor = new Color(1f, .08f, .02f, .65f);
        [SerializeField] private Shader warningShader;
        private MonsterAttackWarning groundWarning;
        private Vector3 strikeCenter, strikeForward;
        private bool strikeLocked;
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
        private int attackStateHash;
        private MonsterBossSettings bossSettings;
        private MonsterEncounterVisuals encounterVisuals;
        public bool IsBoss { get; private set; }
        public bool IsDespawning { get; private set; }
        public float CombatTimeRemaining => encounterVisuals != null ? encounterVisuals.RemainingSeconds : 0f;
        public MonsterRewardData RewardData => rewards;
        public MiningCharacterHealth Health => health;
        /// <summary>Clear a newly placed ore sideways using the existing collision-aware motor.</summary>
        public bool TryClearSpawnedOre(Collider[] oreColliders)
        {
            if (motor == null || !motor.enabled || health == null || health.Health <= 0f) return true;
            for (int pass = 0; pass < 6; pass++)
            {
                bool overlapping = false;
                foreach (var oreCollider in oreColliders)
                {
                    if (oreCollider == null || !oreCollider.enabled || oreCollider.isTrigger) continue;
                    if (!Physics.ComputePenetration(motor, transform.position, transform.rotation,
                        oreCollider, oreCollider.transform.position, oreCollider.transform.rotation,
                        out Vector3 direction, out float depth)) continue;
                    overlapping = true;
                    Vector3 away = Vector3.ProjectOnPlane(direction, Vector3.up);
                    var bounds = oreCollider.bounds;
                    Vector3 delta = Vector3.ProjectOnPlane(transform.position - bounds.center, Vector3.up);
                    if (away.sqrMagnitude < 0.001f) away = delta.sqrMagnitude > 0.001f ? delta : transform.forward;
                    away.Normalize();
                    float radius = motor.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) + motor.skinWidth + 0.08f;
                    // Exit the inflated horizontal footprint, not the top of the rock.
                    float exitX = Mathf.Abs(away.x) > 0.001f
                        ? (bounds.extents.x + radius - Mathf.Sign(away.x) * delta.x) / Mathf.Abs(away.x) : float.PositiveInfinity;
                    float exitZ = Mathf.Abs(away.z) > 0.001f
                        ? (bounds.extents.z + radius - Mathf.Sign(away.z) * delta.z) / Mathf.Abs(away.z) : float.PositiveInfinity;
                    motor.Move(away * Mathf.Max(depth + 0.08f, Mathf.Min(exitX, exitZ)));
                }
                if (!overlapping) { nextRepath = 0f; chasePath.Clear(); return true; }
            }
            foreach (var oreCollider in oreColliders)
                if (oreCollider != null && oreCollider.enabled && !oreCollider.isTrigger &&
                    Physics.ComputePenetration(motor, transform.position, transform.rotation,
                        oreCollider, oreCollider.transform.position, oreCollider.transform.rotation, out _, out _)) return false;
            nextRepath = 0f; chasePath.Clear();
            return true;
        }
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
        public void Initialize(MonsterSpawnZone owner, MiningCharacterHealth player, bool boss = false)
        {
            zone = owner;
            target = player;
            targetCollider = player != null ? player.GetComponent<Collider>() : null;
            destination = transform.position;
            MonsterRewardData definition = rewards;
            bossSettings = definition != null ? definition.boss : null;
            var stats = player != null ? player.GetComponent<MiningPlayerStats>() : null;
            IsBoss = boss && bossSettings != null && bossSettings.CanSpawn(stats != null ? stats.Level : 1);
            if (IsBoss)
            {
                transform.localScale *= bossSettings.ScaleMultiplier;
                if (bossSettings.rewardOverride != null) rewards = bossSettings.rewardOverride;
            }
            Level = rewards != null ? rewards.RollLevel(Random.value) : 1;
            float scale = rewards != null ? rewards.StatMultiplier(Level) : 1;
            float low = rewards != null ? Mathf.Max(0.01f, Mathf.Min(rewards.randomStatMultiplier.x, rewards.randomStatMultiplier.y)) : 1;
            float high = rewards != null ? Mathf.Max(low, Mathf.Max(rewards.randomStatMultiplier.x, rewards.randomStatMultiplier.y)) : 1;
            scaledDamage = damage * scale * Random.Range(low, high) * (IsBoss ? bossSettings.damageMultiplier : 1f);
            scaledBurnDamage = rewards != null ? rewards.burnDamagePerTick * scale : 0f;
            health.ConfigureSpawnHealth(health.MaxHealth * scale * Random.Range(low, high) * (IsBoss ? bossSettings.healthMultiplier : 1f));
            if (rewards != null) health.ConfigureHealingBonus(rewards.healingBonusPercent);
            if (definition != null)
            {
                encounterVisuals = gameObject.AddComponent<MonsterEncounterVisuals>();
                encounterVisuals.Configure(this, definition, IsBoss ? bossSettings : null);
            }
        }
        private void Awake()
        {
            health = GetComponent<MiningCharacterHealth>();
            motor = GetComponent<CharacterController>();
            scaledDamage = damage;
            attackStateHash = Animator.StringToHash(attackState);
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
            if (animationState != attackStateHash || hitApplied) Play("Damage");
        }
        private void OnDeath()
        {
            if (IsDespawning) return;
            if (encounterVisuals != null) encounterVisuals.CancelExpiry();
            ClearMinerWarning();
            if (rewardsGranted) return;
            rewardsGranted = true;
            Play("Down");
            motor.enabled = false;
            if (zone != null) zone.GrantRewards(rewards, transform.position + Vector3.up * 0.6f,
                (rewards != null ? rewards.GoldMultiplier(Level) : 1f) * (IsBoss ? bossSettings.goldMultiplier : 1f),
                IsBoss ? bossSettings.experienceMultiplier : 1f);
            Destroy(gameObject, deathDelay);
        }
        public void BeginDespawn()
        {
            if (IsDespawning || health == null || health.Health <= 0f) return;
            IsDespawning = true;
            rewardsGranted = true; // Timeout is not a kill, including late hits/DOT during the dissolve.
            ClearMinerWarning();
            health.SetDamageEnabled(false);
            health.enabled = false;
            if (health.HealthBar != null) health.HealthBar.gameObject.SetActive(false);
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            motor.enabled = false;
            enabled = false;
        }
        private void Play(string name, bool restart = false)
        {
            int hash = Animator.StringToHash(name);
            if ((!restart && animationState == hash) || animator == null) return;
            animationState = hash;
            if (hash == attackStateHash && restart)
            {
                Vector3 victim = targetMiner != null ? targetMiner.transform.position : target != null ? target.transform.position : transform.position + transform.forward;
                Vector3 facing = Vector3.ProjectOnPlane(victim - transform.position, Vector3.up);
                if (facing.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(facing);
                strikeForward = transform.forward;
                strikeCenter = transform.position + (hitShape == HitShape.Area ? strikeForward * areaForwardOffset * HitScale : Vector3.zero);
                strikeLocked = true;
                if (hitShape == HitShape.Area)
                {
                    if (groundWarning == null) groundWarning = gameObject.AddComponent<MonsterAttackWarning>();
                    groundWarning.Show(strikeCenter, areaRadius * HitScale, warningColor, warningShader, transform);
                }
            }
            else if (hash != attackStateHash)
            {
                strikeLocked = false;
                if (groundWarning != null) groundWarning.Hide();
            }
            animator.CrossFadeInFixedTime(hash, 0.15f);
        }
        private void Update()
        {
            if (health.Health <= 0f || animator == null) return;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            bool attacking = state.IsName(attackState);
            bool reacting = state.IsName("Damage");
            if (groundWarning != null)
            {
                var windup = animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(attackState)
                    ? animator.GetNextAnimatorStateInfo(0) : state;
                if (windup.IsName(attackState) && !hitApplied)
                    groundWarning.SetProgress(Mathf.Clamp01(windup.normalizedTime / Mathf.Max(.001f, hitMoment)));
                else if (hitApplied || animationState != attackStateHash) groundWarning.Hide();
            }
            if (attacking && !animator.IsInTransition(0) && !hitApplied &&
                state.normalizedTime >= hitMoment)
            {
                hitApplied = true;
                if (groundWarning != null) groundWarning.Hide();
                // One contact per swing. Area and sweep can hit every victim inside the same shown volume.
                if (target != null && target.Health > 0f && ContainsVictim(target.transform, targetCollider))
                {
                    float dealt = target.DealDamage(scaledDamage);
                    ApplyHitHealing(dealt);
                    if (dealt > 0f && rewards != null) target.ApplyBurn(scaledBurnDamage, rewards.burnTickSeconds, rewards.burnDurationSeconds);
                }
                // Reverse iteration: a fatal hit may remove the miner from the registry immediately.
                for (int i = MiningNpc.Miners.Count - 1; i >= 0; i--)
                {
                    var miner = MiningNpc.Miners[i];
                    if (miner != null && miner.isActiveAndEnabled && !miner.IsDead && ContainsVictim(miner.transform, miner.GetComponent<Collider>()))
                        ApplyHitHealing(miner.ApplyMonsterDamage(scaledDamage));
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
                        Play(attackState, true);
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
                        Play(attackState, true);
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
            return ContainsVictim(targetMiner != null ? targetMiner.transform : target.transform, targetMiner != null ? minerCollider : targetCollider);
        }
        private float HitScale => Mathf.Max(.01f, Mathf.Max(transform.lossyScale.x, transform.lossyScale.z));
        private void ApplyHitHealing(float dealt)
        {
            if (dealt > 0f && rewards != null) health.Heal(dealt * Mathf.Clamp(rewards.lifeStealPercent, 0f, 100f) * .01f);
        }
        public bool ContainsHitPoint(Vector3 point)
        {
            Vector3 center = strikeLocked ? strikeCenter : transform.position + (hitShape == HitShape.Area ? transform.forward * areaForwardOffset * HitScale : Vector3.zero);
            Vector3 forward = strikeLocked ? strikeForward : transform.forward;
            Vector3 delta = point - center;
            if (Mathf.Abs(delta.y) > hitHeight * HitScale) return false;
            delta.y = 0;
            float radius = (hitShape == HitShape.Area ? areaRadius : attackRange) * HitScale;
            return delta.sqrMagnitude <= radius * radius && (hitShape == HitShape.Area || delta.sqrMagnitude < .0001f || Vector3.Angle(forward, delta) <= attackArc * .5f);
        }
        private bool ContainsVictim(Transform victim, Collider victimCollider)
        {
            Vector3 origin = transform.TransformPoint(motor.center);
            Vector3 center = strikeLocked ? strikeCenter : transform.position + (hitShape == HitShape.Area ? transform.forward * areaForwardOffset * HitScale : Vector3.zero);
            Vector3 position = victim.position;
            Vector3 point = victimCollider != null && victimCollider.enabled
                ? victimCollider.ClosestPoint(center + Vector3.up * .5f) : position;
            if (!ContainsHitPoint(point)) return false;
            Vector3 ray = point - origin;
            if (ray.sqrMagnitude > 0.0001f)
            {
                int count = Physics.RaycastNonAlloc(origin, ray.normalized, strikeObstructions,
                    ray.magnitude, ~0, QueryTriggerInteraction.Ignore);
                // A full buffer is ambiguous: fail closed rather than hit through a wall.
                if (count == strikeObstructions.Length) return false;
                for (int i = 0; i < count; i++)
                {
                    Transform obstacle = strikeObstructions[i].transform;
                    if (obstacle != null && !obstacle.IsChildOf(transform) && !obstacle.IsChildOf(victim)) return false;
                }
            }
            return true;
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
            if (groundWarning != null) groundWarning.Hide();
            strikeLocked = false;
            ClearMinerWarning();
            ActiveMonsters.Remove(this);
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            DrawHitGizmo();
        }
        private void DrawHitGizmo()
        {
            Gizmos.color = warningColor;
            Vector3 center = strikeLocked ? strikeCenter : transform.position + (hitShape == HitShape.Area ? transform.forward * areaForwardOffset * HitScale : Vector3.zero);
            Vector3 forward = strikeLocked ? strikeForward : transform.forward;
            float angle = hitShape == HitShape.Area ? 360f : attackArc;
            float radius = (hitShape == HitShape.Area ? areaRadius : attackRange) * HitScale;
            Vector3 first = center + Quaternion.AngleAxis(-angle * .5f, Vector3.up) * forward * radius;
            Vector3 previous = first;
            for (int i = 1; i <= 64; i++)
            {
                Vector3 next = center + Quaternion.AngleAxis(-angle * .5f + angle * i / 64f, Vector3.up) * forward * radius;
                Gizmos.DrawLine(previous, next); previous = next;
            }
            if (hitShape == HitShape.Sweep) { Gizmos.DrawLine(center, first); Gizmos.DrawLine(center, previous); }
            Gizmos.DrawLine(center, center + Vector3.up * hitHeight * HitScale);
        }
    }
}
