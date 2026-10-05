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
        [Tooltip("Optional second attack. Empty keeps this species' original single attack.")]
        [SerializeField] private string secondAttackState;
        [Range(0f, 1f), SerializeField] private float secondHitMoment = .52f;
        [Min(.1f), SerializeField] private float secondAreaRadius = 1.6f;
        [SerializeField] private Vector2 secondAreaOffset = new Vector2(.3f, 1.71f);
        [Min(0f), SerializeField] private float attackTurnSpeed = 720f;
        [Range(0f, 1f), Tooltip("Fraction of windup where tracking ends; 1 tracks until contact, 0 locks at start.")]
        [SerializeField] private float trackingEndFraction = .85f;
        [Min(0f), SerializeField] private float chaseTurnSpeed = 240f;
        [Min(0f), SerializeField] private float animationBlendSeconds = .15f;
        private bool secondAttack, nextSecondAttack;
        private string ActiveAttackState => secondAttack ? secondAttackState : attackState;
        private float ActiveHitMoment => secondAttack ? secondHitMoment : hitMoment;
        private float ActiveAreaRadius => secondAttack ? secondAreaRadius : areaRadius;
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
        [SerializeField] private float areaSideOffset;
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
        private float nextMinerSearch;
        private readonly List<Vector3> chasePath = new();
        private readonly RaycastHit[] strikeObstructions = new RaycastHit[32];
        private int chaseWaypoint;
        private float nextRepath;
        private Vector3 lastPathGoal;
        private bool chasePathPending;
        private int chaseRouteRevision;
        [Header("Navigation (collision-aware, including boss size)")]
        [Min(.05f), SerializeField] private float chaseRepathSeconds = .4f;
        [Min(4), SerializeField] private int chaseStandDirections = 16;
        [Range(.1f, .95f), SerializeField] private float chaseStandRangeFraction = .75f;
        [Range(.1f, 1f), SerializeField] private float chaseStandMaximumRangeFraction = .95f;
        [Min(.1f), SerializeField, HideInInspector] private float avoidanceLookAhead = 1.5f;
        [Min(.05f), SerializeField, HideInInspector] private float avoidanceHoldSeconds = .5f;
        private MonsterSpawnZone zone;
        private Vector3 destination;
        private float nextDecision, nextAttack, verticalSpeed;
        private bool hitApplied;
        private int animationState;
        private int attackStateHash;
        private MonsterBossSettings bossSettings;
        private readonly BossSkillRuntime bossSkill = new BossSkillRuntime();
        private float baseAnimatorSpeed = 1f;
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

        // Applied once after normal level and boss tuning, never to shared prefab data.
        public void ApplyEncounterModifiers(float sizeMultiplier, float damageMultiplier, float healthMultiplier = 1f)
        {
            transform.localScale *= Mathf.Max(0.01f, sizeMultiplier);
            scaledDamage *= Mathf.Max(0f, damageMultiplier);
            if (healthMultiplier != 1f) health.ConfigureSpawnHealth(health.MaxHealth * Mathf.Max(.01f, healthMultiplier));
        }
        private float scaledBurnDamage;
        private static readonly List<MushroomMonster> ActiveMonsters = new();
        public static IReadOnlyList<MushroomMonster> Monsters => ActiveMonsters;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetMonsterRegistry() => ActiveMonsters.Clear();
        private void OnEnable()
        {
            OreActorTraversal.RegisterActor(gameObject);
            var audioManager = FindFirstObjectByType<MiningAudioManager>();
            if (audioManager != null) audioManager.RegisterWorldSfxSources(gameObject);
            if (!ActiveMonsters.Contains(this)) ActiveMonsters.Add(this);
            foreach (MiningNpc miner in MiningNpc.Miners)
                if (miner != null) miner.IgnoreMonsterCollision(this);
        }
        public void Initialize(MonsterSpawnZone owner, MiningCharacterHealth player, bool boss = false)
        {
            MiningNavGrid.Instance?.Cancel(this); chasePathPending = false;
            chasePath.Clear(); chaseWaypoint = 0; nextRepath = 0; chaseRouteRevision = -1;
            zone = owner;
            target = player;
            targetCollider = player != null ? player.GetComponent<Collider>() : null;
            destination = transform.position;
            MonsterRewardData definition = rewards;
            bossSettings = definition != null ? definition.boss : null;
            var stats = player != null ? player.GetComponent<MiningPlayerStats>() : null;
            IsBoss = boss && bossSettings != null && bossSettings.CanSpawn(stats != null ? stats.Level : 1,
                MiningGameplayTuning.Current != null && MiningGameplayTuning.Current.IgnoreBossLevel);
            bossSkill.Configure(IsBoss ? bossSettings : null);
            if (IsBoss)
            {
                transform.localScale *= bossSettings.ScaleMultiplier;
                if (bossSettings.rewardOverride != null) rewards = bossSettings.rewardOverride;
            }
            // Read the current day at spawn; existing enemies retain their rolled level.
            Level = rewards != null ? rewards.RollSpawnLevel(Random.value,
                stats != null ? stats.Level : 1, owner != null ? owner.SpawnDayNumber : 1) : 1;
            float scale = rewards != null ? rewards.StatMultiplier(Level) : 1;
            float low = rewards != null ? Mathf.Max(0.01f, Mathf.Min(rewards.randomStatMultiplier.x, rewards.randomStatMultiplier.y)) : 1;
            float high = rewards != null ? Mathf.Max(low, Mathf.Max(rewards.randomStatMultiplier.x, rewards.randomStatMultiplier.y)) : 1;
            scaledDamage = damage * scale * Random.Range(low, high) * (IsBoss ? bossSettings.damageMultiplier : 1f);
            scaledBurnDamage = rewards != null ? rewards.burnDamagePerTick * scale : 0f;
            health.ConfigureSpawnHealth(health.MaxHealth * scale * Random.Range(low, high) * (IsBoss ? bossSettings.healthMultiplier : 1f));
            if (rewards != null)
            {
                health.ConfigureHealingBonus(rewards.healingBonusPercent);
                health.ConfigureDefenses(rewards.ArmorAtLevel(Level), rewards.MagicResistanceAtLevel(Level), rewards.resistanceScale);
            }
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
            baseAnimatorSpeed = animator != null ? animator.speed : 1f;
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
            bossSkill.NotifyHealth(health.Health, health.MaxHealth);
            // Preserve the committed contact frame, then allow the hit reaction.
            // Otherwise a player's strike can cancel every incoming headbutt.
            if (animationState != attackStateHash || hitApplied) Play("Damage");
        }
        private void OnDeath()
        {
            if (animator != null) animator.speed = baseAnimatorSpeed;
            if (IsDespawning) return;
            if (encounterVisuals != null) encounterVisuals.CancelExpiry();
            if (rewardsGranted) return;
            rewardsGranted = true;
            Play("Down");
            var cards = FindFirstObjectByType<MiningCardSystem>();
            if (cards != null) cards.TryDrop(transform.position, IsBoss);
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
                strikeLocked = true;
                UpdateStrikeCenter();
                if (hitShape == HitShape.Area)
                {
                    if (groundWarning == null) groundWarning = gameObject.AddComponent<MonsterAttackWarning>();
                    groundWarning.Show(strikeCenter, ActiveAreaRadius * HitScale, warningColor, warningShader, transform);
                }
            }
            else if (hash != attackStateHash)
            {
                strikeLocked = false;
                if (groundWarning != null) groundWarning.Hide();
            }
            animator.CrossFadeInFixedTime(hash, animationBlendSeconds);
        }
        private void BeginAttack()
        {
            secondAttack = nextSecondAttack && !string.IsNullOrEmpty(secondAttackState);
            nextSecondAttack = !secondAttack && !string.IsNullOrEmpty(secondAttackState);
            attackStateHash = Animator.StringToHash(ActiveAttackState);
            nextAttack = Time.time + attackCooldown / bossSkill.AttackSpeedMultiplier;
            hitApplied = false;
            Play(ActiveAttackState, true);
        }
        private Vector3 AreaCenter
        {
            get
            {
                Vector2 offset = secondAttack ? secondAreaOffset : new Vector2(areaSideOffset, areaForwardOffset);
                return transform.position + (transform.right * offset.x + transform.forward * offset.y) * HitScale;
            }
        }
        private void UpdateStrikeCenter()
        {
            strikeForward = transform.forward;
            strikeCenter = hitShape == HitShape.Area ? AreaCenter : transform.position;
        }
        private void TrackAttackTarget(float normalizedTime)
        {
            if (hitApplied || normalizedTime >= ActiveHitMoment * trackingEndFraction) return;
            Transform victim = IsMinerTargetValid() ? targetMiner.transform : target != null && target.Health > 0f ? target.transform : null;
            if (victim == null) return;
            Vector3 facing = Vector3.ProjectOnPlane(victim.position - transform.position, Vector3.up);
            if (facing.sqrMagnitude > .001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(facing), attackTurnSpeed * Time.deltaTime);
            UpdateStrikeCenter();
            if (groundWarning != null && hitShape == HitShape.Area) groundWarning.MoveCenter(strikeCenter, transform);
        }
        private void Update()
        {
            if (health.Health <= 0f || animator == null) return;
            if (IsDespawning) return;
            float healing = bossSkill.TickHealing(Time.deltaTime, health.MaxHealth);
            if (healing > 0f) health.Heal(healing / health.HealingMultiplier);
            // Only the attack speeds up, not walk/down/hit-reaction animations.
            animator.speed = baseAnimatorSpeed * (animationState == attackStateHash ? bossSkill.AttackSpeedMultiplier : 1f);
            var state = animator.GetCurrentAnimatorStateInfo(0);
            bool attacking = state.IsName(ActiveAttackState);
            bool reacting = state.IsName("Damage");
            var windup = animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(ActiveAttackState)
                ? animator.GetNextAnimatorStateInfo(0) : state;
            if (windup.IsName(ActiveAttackState) && animationState == attackStateHash) TrackAttackTarget(windup.normalizedTime);
            if (groundWarning != null)
            {
                if (windup.IsName(ActiveAttackState) && !hitApplied)
                    groundWarning.SetProgress(Mathf.Clamp01(windup.normalizedTime / Mathf.Max(.001f, ActiveHitMoment)));
                else if (hitApplied || animationState != attackStateHash) groundWarning.Hide();
            }
            if (attacking && !animator.IsInTransition(0) && !hitApplied &&
                state.normalizedTime >= ActiveHitMoment)
            {
                hitApplied = true;
                if (bossSkill.NotifyStrike() && target != null && target.Health > 0f &&
                    (target.transform.position - transform.position).sqrMagnitude <= bossSettings.slowRadius * bossSettings.slowRadius)
                {
                    var playerMotor = target.GetComponent<StarterAssets.ThirdPersonController>();
                    if (playerMotor != null) playerMotor.ApplyMovementSlow(bossSettings.slowPercent, bossSettings.slowSeconds);
                }
                if (groundWarning != null) groundWarning.Hide();
                // One contact per swing. Area and sweep can hit every victim inside the same shown volume.
                if (target != null && target.Health > 0f && ContainsVictim(target.transform, targetCollider))
                {
                    float dealt = target.DealDamage(scaledDamage,
                        rewards != null ? rewards.attackDamageType : CombatDamageType.Physical);
                    if (bossSkill.NotifyPlayerDamage(dealt, target.MaxHealth))
                    {
                        nextAttack = Mathf.Min(nextAttack, Time.time + attackCooldown / bossSkill.AttackSpeedMultiplier);
                        animator.speed = baseAnimatorSpeed * bossSkill.AttackSpeedMultiplier;
                    }
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
            if (!animator.IsInTransition(0) && ((!attacking && !reacting) || state.normalizedTime >= 1f))
            {
                SelectMinerTarget();
                bool chasingPlayer = targetMiner == null && target != null && target.Health > 0f &&
                    (zone == null || zone.IsInsideMine(transform.position)) &&
                    Vector3.Distance(transform.position, target.transform.position) <= EffectiveDetectionRange;
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
                if (chasing && delta.magnitude <= EffectiveAttackRange && Time.time >= nextAttack)
                {
                    BeginAttack();
                }
                else if (delta.magnitude > (chasing ? EffectiveAttackRange : 0.3f))
                {
                    movement = ChaseDirection(chasing) * moveSpeed;
                    Play(movement.sqrMagnitude > 0f ? "Walk" : "Idle");
                }
                else Play("Idle");
                if (delta.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(movement.sqrMagnitude > 0.01f ? movement : delta), chaseTurnSpeed * Time.deltaTime);
            }
            verticalSpeed = motor.isGrounded ? -2f : verticalSpeed + Physics.gravity.y * Time.deltaTime;
            motor.Move((movement + Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        private float HitScale => Mathf.Max(.01f, Mathf.Max(transform.lossyScale.x, transform.lossyScale.z));
        public float EffectiveAttackRange => attackRange * HitScale;
        public float EffectiveDetectionRange => detectionRange * HitScale;
        public float EffectiveAreaRadius => ActiveAreaRadius * HitScale;
        private void ApplyHitHealing(float dealt)
        {
            if (dealt > 0f && rewards != null) health.Heal(dealt * Mathf.Clamp(rewards.lifeStealPercent, 0f, 100f) * .01f);
        }
        public bool ContainsHitPoint(Vector3 point)
        {
            Vector3 center = strikeLocked ? strikeCenter : hitShape == HitShape.Area ? AreaCenter : transform.position;
            Vector3 forward = strikeLocked ? strikeForward : transform.forward;
            Vector3 delta = point - center;
            if (Mathf.Abs(delta.y) > hitHeight * HitScale) return false;
            delta.y = 0;
            float radius = (hitShape == HitShape.Area ? ActiveAreaRadius : attackRange) * HitScale;
            return delta.sqrMagnitude <= radius * radius && (hitShape == HitShape.Area || delta.sqrMagnitude < .0001f || Vector3.Angle(forward, delta) <= attackArc * .5f);
        }
        private bool ContainsVictim(Transform victim, Collider victimCollider)
        {
            Vector3 origin = transform.TransformPoint(motor.center);
            Vector3 center = strikeLocked ? strikeCenter : hitShape == HitShape.Area ? AreaCenter : transform.position;
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
                    if (obstacle != null && obstacle.GetComponentInParent<Ore>() != null) continue;
                    if (obstacle != null && !obstacle.IsChildOf(transform) && !obstacle.IsChildOf(victim)) return false;
                }
            }
            return true;
        }
        private Vector3 ChaseDirection(bool chasing)
        {
            var grid = MiningNavGrid.Instance;
            if (grid == null || !grid.HasBaked) return Vector3.zero;
            // Skin width is collision tolerance, not extra body size. Inflating a small
            // monster with miner clearance can trap it beside an otherwise clear portal.
            float radius = motor.radius * HitScale;
            float height = motor.height * Mathf.Abs(transform.lossyScale.y);
            if (!chasePathPending && ((Time.time >= nextRepath && chaseWaypoint >= chasePath.Count) ||
                (lastPathGoal - destination).sqrMagnitude > 1f || chaseRouteRevision != grid.Revision))
            {
                nextRepath = Time.time + chaseRepathSeconds;
                lastPathGoal = destination;
                Vector3 goal = destination;
                if (zone != null && !zone.IsInsideMine(transform.position) && zone.TryGetMineEntryPoint(transform.position, out Vector3 entry)) goal = entry;
                if (chasing && !TryGetChaseStandPoint(grid, goal, radius, height, out goal)) return Vector3.zero;
                chasePathPending = true;
                grid.RequestPath(this, transform.position, goal, (success, route) =>
                {
                    chasePathPending = false; if (!isActiveAndEnabled) return;
                    chasePath.Clear(); chaseWaypoint = 0; chaseRouteRevision = grid.Revision;
                    if (success) chasePath.AddRange(route);
                }, radius, height);
            }
            while (chaseWaypoint < chasePath.Count)
            {
                Vector3 delta = Vector3.ProjectOnPlane(chasePath[chaseWaypoint] - transform.position, Vector3.up);
                if (delta.sqrMagnitude > 0.3f * 0.3f)
                {
                    Vector3 next = transform.position + delta.normalized * Mathf.Min(delta.magnitude, radius);
                    if (grid.IsSegmentClear(transform.position, next, radius, height)) return delta.normalized;
                    chasePath.Clear(); nextRepath = 0; return Vector3.zero;
                }
                chaseWaypoint++;
            }
            return Vector3.zero;
        }
        private bool TryGetChaseStandPoint(MiningNavGrid grid, Vector3 victim, float radius, float height, out Vector3 point)
        {
            point = victim;
            Vector3 toward = Vector3.ProjectOnPlane(transform.position - victim, Vector3.up).normalized;
            int directions = Mathf.Max(4, chaseStandDirections);
            for (int step = 0; step < directions; step++)
            {
                Vector3 candidate = victim + Quaternion.AngleAxis(step * 360f / directions, Vector3.up) * toward * (EffectiveAttackRange * chaseStandRangeFraction);
                if (!grid.TryProject(candidate, grid.StandProjectionRadius, out Vector3 projected)) continue;
                if (Vector3.ProjectOnPlane(projected - victim, Vector3.up).magnitude > EffectiveAttackRange * chaseStandMaximumRangeFraction ||
                    !grid.IsPointClear(projected, radius, height)) continue;
                point = projected; return true;
            }
            return false;
        }
        private bool IsMinerTargetValid() => targetMiner != null && targetMiner.isActiveAndEnabled &&
            !targetMiner.IsStunned &&
            (zone == null || zone.IsInsideMine(transform.position)) &&
            (targetMiner.transform.position - transform.position).sqrMagnitude <= EffectiveDetectionRange * EffectiveDetectionRange;

        private void SelectMinerTarget()
        {
            if (zone != null && !zone.IsInsideMine(transform.position))
            {
                targetMiner = null;
                minerCollider = null;
                return;
            }
            if (IsMinerTargetValid()) return;
            targetMiner = null;
            minerCollider = null;
            if (Time.time < nextMinerSearch) return;
            nextMinerSearch = Time.time + 0.3f;
            float best = EffectiveDetectionRange * EffectiveDetectionRange;
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

        private void OnDisable()
        {
            MiningNavGrid.Instance?.Cancel(this); chasePathPending = false;
            OreActorTraversal.UnregisterActor(gameObject);
            if (animator != null) animator.speed = baseAnimatorSpeed;
            if (groundWarning != null) groundWarning.Hide();
            strikeLocked = false;
            ActiveMonsters.Remove(this);
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, EffectiveDetectionRange);
            DrawHitGizmo();
        }
        private void DrawHitGizmo()
        {
            Gizmos.color = warningColor;
            Vector3 center = strikeLocked ? strikeCenter : hitShape == HitShape.Area ? AreaCenter : transform.position;
            Vector3 forward = strikeLocked ? strikeForward : transform.forward;
            float angle = hitShape == HitShape.Area ? 360f : attackArc;
            float radius = (hitShape == HitShape.Area ? ActiveAreaRadius : attackRange) * HitScale;
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
