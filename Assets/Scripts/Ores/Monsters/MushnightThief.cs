using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Optional species behaviour; MushroomMonster retains health, level, loot and registry ownership.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(MushroomMonster))]
    public sealed class MushnightThief : MonoBehaviour
    {
        public enum ThiefState { Sneaking, Stealing, Evading, Escaping, Dead, Gone }
        public ThiefState State { get; private set; }
        public float StolenGold { get; private set; }
        public float StealProgress => config == null ? 0f : Mathf.Clamp01(stealClock / Mathf.Max(.1f, config.stealSeconds));
        public bool IsInvisible { get; private set; }
        public float Stamina => stamina?.Current ?? 0f;
        public float MaxStamina => stamina?.Maximum ?? 0f;
        public bool IsResting => stamina != null && stamina.IsResting;
        public string NavigationStatus => path != null ? path.State : "Idle";

        private MushroomMonster owner;
        private CharacterController motor;
        private Animator animator;
        private Renderer[] visuals;
        private bool[] originalVisibility;
        private MushnightSettings config;
        private MushnightStamina stamina;
        private MonsterCombatSettings movement;
        private MonsterPathFollower path;
        private MiningCharacterHealth player;
        private Collider playerBody;
        private TreasureChest chest;
        private Collider chestBody;
        private PlayerWallet robbedWallet;
        private Vector3 fleeGoal;
        private float stealClock, escapeClock, fleeClock, verticalSpeed;
        private int animationHash;
        private bool initialized, theftCompleted;

        public void Initialize(MushroomMonster monster, MiningCharacterHealth playerTarget)
        {
            owner = monster;
            config = (owner.SpeciesData as MushnightData)?.mushnight;
            if (config == null) { initialized = false; return; }
            stamina = new MushnightStamina(config);
            motor = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>(true);
            player = playerTarget;
            playerBody = player != null ? player.GetComponent<Collider>() : null;
            chest = TreasureChest.Active;
            chestBody = chest != null ? chest.GetComponent<Collider>() : null;
            movement = JsonUtility.FromJson<MonsterCombatSettings>(JsonUtility.ToJson(owner.CombatData ?? new MonsterCombatSettings()));
            path?.Reset();
            path = new MonsterPathFollower(owner, motor);
            if (visuals == null)
            {
                var meshes = new System.Collections.Generic.List<Renderer>();
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                    if (r is MeshRenderer || r is SkinnedMeshRenderer) meshes.Add(r);
                visuals = meshes.ToArray();
                originalVisibility = new bool[visuals.Length];
                for (int i = 0; i < visuals.Length; i++) originalVisibility[i] = visuals[i].enabled;
            }
            StolenGold = stealClock = escapeClock = fleeClock = verticalSpeed = 0f;
            animationHash = 0;
            theftCompleted = false;
            robbedWallet = null;
            State = ThiefState.Sneaking;
            initialized = true;
            SetInvisible(true);
            var lootLabel = GetComponent<MushnightLootLabel>();
            if (lootLabel == null) lootLabel = gameObject.AddComponent<MushnightLootLabel>();
            lootLabel.Initialize(this, config);
        }

        public void Tick(float dt)
        {
            if (!initialized || owner.Health.Health <= 0f || owner.IsDespawning || State == ThiefState.Gone) return;
            if (chest == null || !chest.IsAlive)
            {
                // A broken/destroyed chest cannot be robbed; leave without inventing loot.
                if (State != ThiefState.Escaping) BeginEscape();
            }
            Vector3 velocity = Vector3.zero;
            float playerDistance = PlayerDistance;
            if (!IsInvisible && !theftCompleted && playerDistance < config.fleeFromPlayerRange)
            {
                if (State != ThiefState.Evading) { path.Reset(); fleeClock = 0f; }
                State = ThiefState.Evading;
                stealClock = 0f;
            }
            if (State == ThiefState.Evading && playerDistance >= config.safeFromPlayerRange)
            { State = ThiefState.Sneaking; path.Reset(); }

            if (State == ThiefState.Sneaking)
            {
                if (CanReachChest())
                { State = ThiefState.Stealing; stealClock = 0f; path.Reset(); SetInvisible(false); }
                else if (chest != null && !IsResting)
                {
                    Vector3 goal = ChestCenter;
                    float extent = chestBody != null ? Mathf.Max(chestBody.bounds.extents.x, chestBody.bounds.extents.z) : 0f;
                    float reach = extent + MonsterPathFollower.ProfileFor(motor).Radius + config.stealRange * .6f;
                    movement.moveSpeed = Mathf.Max(0f, config.approachSpeed);
                    velocity = path.Tick(goal, chest.transform, reach, movement, dt) * movement.moveSpeed;
                }
            }
            else if (State == ThiefState.Stealing)
            {
                if (!CanReachChest()) { State = ThiefState.Sneaking; stealClock = 0f; }
                else
                {
                    stealClock += dt;
                    if (stealClock >= Mathf.Max(.1f, config.stealSeconds)) CompleteTheft();
                }
            }
            if (State == ThiefState.Escaping || State == ThiefState.Evading)
            {
                escapeClock += dt;
                fleeClock -= dt;
                if (fleeClock <= 0f || Flat(fleeGoal - transform.position).sqrMagnitude < .25f)
                { ChooseFleeGoal(playerDistance); fleeClock = Mathf.Max(.05f, config.fleeRepathSeconds); }
                movement.moveSpeed = Mathf.Max(0f, config.escapeSpeed);
                if (!IsResting) velocity = path.Tick(fleeGoal, null, 0f, movement, dt) * movement.moveSpeed;
                if (State == ThiefState.Escaping && escapeClock >= config.minimumEscapeSeconds &&
                    (chest == null || Flat(transform.position - ChestCenter).magnitude >= config.escapeDistanceFromChest) &&
                    playerDistance >= config.safeFromPlayerRange)
                {
                    State = ThiefState.Gone;
                    owner.BeginDespawn();
                    Destroy(gameObject);
                    return;
                }
            }
            Play(velocity.sqrMagnitude > .001f ? config.moveState : config.idleState);
            if (velocity.sqrMagnitude > .001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(velocity), movement.chaseTurnSpeed * dt);
            verticalSpeed = motor.isGrounded ? -2f : verticalSpeed + Physics.gravity.y * dt;
            Vector3 beforeMove = transform.position;
            if (motor.enabled) motor.Move((velocity + Vector3.up * verticalSpeed) * dt);
            stamina.Tick(dt, Flat(transform.position - beforeMove).sqrMagnitude > .000001f);
            if (IsInvisible && owner.Health.HealthBar != null) owner.Health.HealthBar.gameObject.SetActive(false);
        }

        private Vector3 ChestCenter => chestBody != null ? chestBody.bounds.center : chest.transform.position;
        private static Vector3 Flat(Vector3 value) => Vector3.ProjectOnPlane(value, Vector3.up);
        private float PlayerDistance => player != null && player.isActiveAndEnabled && player.Health > 0f
            ? Flat(player.transform.position - transform.position).magnitude : float.PositiveInfinity;
        private bool CanReachChest()
        {
            if (chest == null || !chest.IsAlive) return false;
            Vector3 center = transform.TransformPoint(motor.center);
            Vector3 closest = chestBody != null ? chestBody.ClosestPoint(center) : chest.transform.position;
            return Flat(center - closest).magnitude <= MonsterPathFollower.ProfileFor(motor).Radius + config.stealRange &&
                owner.HasStrikeLineOfSight(chest.transform, closest);
        }
        private void CompleteTheft()
        {
            if (theftCompleted || !CanReachChest()) return;
            theftCompleted = true;
            robbedWallet = chest.Wallet;
            StolenGold = robbedWallet != null ? robbedWallet.TakeStolenMoney(config.GoldAtLevel(owner.Level)) : 0f;
            BeginEscape();
        }
        private void BeginEscape()
        {
            State = ThiefState.Escaping;
            theftCompleted = true;
            escapeClock = fleeClock = stealClock = 0f;
            SetInvisible(false);
            path.Reset();
        }
        private void ChooseFleeGoal(float playerDistance)
        {
            var grid = WorldNavigationGrid.Instance;
            fleeGoal = transform.position;
            if (grid == null || !grid.HasBaked) return;
            Vector3 threat = playerDistance < config.safeFromPlayerRange && player != null ? player.transform.position :
                chest != null ? ChestCenter : transform.position - transform.forward;
            Vector3 away = Flat(transform.position - threat).normalized;
            if (away.sqrMagnitude < .001f) away = transform.forward;
            var profile = MonsterPathFollower.ProfileFor(motor);
            float best = float.NegativeInfinity;
            for (int i = 0; i < 12; i++)
            {
                int ring = (i + 1) / 2;
                float angle = ring * (i % 2 == 0 ? -1f : 1f) * 30f;
                Vector3 candidate = transform.position + Quaternion.AngleAxis(angle, Vector3.up) * away * config.fleeGoalDistance;
                if (!grid.TryProject(candidate, grid.StandProjectionRadius, out var point, profile.Radius, profile.Height)) continue;
                float score = Flat(point - threat).magnitude - Mathf.Abs(angle) * .01f;
                if (chest != null) score += Flat(point - ChestCenter).magnitude * .25f;
                if (score <= best) continue;
                best = score; fleeGoal = point;
            }
        }
        public void OnDamaged()
        {
            if (!initialized || owner.Health.Health <= 0f) return;
            SetInvisible(false);
            if (!theftCompleted) { State = ThiefState.Evading; stealClock = fleeClock = 0f; path.Reset(); }
        }
        public void OnKilled()
        {
            if (!initialized || State == ThiefState.Dead || State == ThiefState.Gone) return;
            State = ThiefState.Dead;
            SetInvisible(false);
            path.Reset();
            // Clear before notifying wallet listeners: exactly-once refund, without level/loot multipliers.
            float refund = StolenGold;
            StolenGold = 0f;
            if (refund > 0f && robbedWallet != null) robbedWallet.AddMoney(refund);
        }
        private void SetInvisible(bool invisible)
        {
            IsInvisible = invisible;
            if (playerBody != null && motor != null && motor.enabled && playerBody.enabled)
                Physics.IgnoreCollision(motor, playerBody, invisible);
            for (int i = 0; i < visuals.Length; i++) if (visuals[i] != null) visuals[i].enabled = !invisible && originalVisibility[i];
            if (invisible && owner.Health.HealthBar != null) owner.Health.HealthBar.gameObject.SetActive(false);
        }
        private void Play(string state)
        {
            if (animator == null || string.IsNullOrEmpty(state)) return;
            int hash = Animator.StringToHash(state);
            if (animationHash == hash) return;
            animationHash = hash;
            animator.CrossFadeInFixedTime(hash, movement.animationBlendSeconds);
        }
        private void OnDisable()
        {
            path?.Reset();
            if (playerBody != null && motor != null) Physics.IgnoreCollision(motor, playerBody, false);
        }
    }
}
