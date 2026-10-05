using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiningSimulator.Ores
{
    /// <summary>Reserves a compatible ore, steers around dynamic blockers, then mines it.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))]
    public sealed partial class MiningNpc : MonoBehaviour
    {
        private static readonly List<MiningNpc> ActiveNpcs = new();
        public static IReadOnlyList<MiningNpc> Miners => ActiveNpcs;
        public bool IsDead => minerHealth <= 0f;
        // Compatibility for existing targeting/navigation callers: dead miners cannot act.
        public bool IsStunned => IsDead;
        public bool IsActivelyMining => isActiveAndEnabled && isMining && !IsStunned;
        public float Health => minerHealth;
        public event System.Action<float, float> HealthChanged;
        public float MaxHealth => npcData != null ? npcData.MinerHealth : 20f;
        // Miner HP is a fixed NpcData value, independent of combat/player levels.
        public void IgnoreMonsterCollision(MushroomMonster monster)
        {
            capsule ??= GetComponent<CapsuleCollider>();
            if (capsule == null || monster == null) return;
            foreach (Collider monsterCollider in monster.GetComponentsInChildren<Collider>(true))
                if (monsterCollider != null) Physics.IgnoreCollision(capsule, monsterCollider, true);
        }
        private float minerHealth = 20f;
        private NpcShop owningShop;
        public void SetOwningShop(NpcShop shop) => owningShop = shop;

        public float ApplyMonsterDamage(float amount)
        {
            if (!isActiveAndEnabled || IsStunned || amount <= 0f) return 0f;
            float dealt = Mathf.Min(minerHealth, amount);
            minerHealth = Mathf.Max(0f, minerHealth - amount);
            HealthChanged?.Invoke(minerHealth, MaxHealth);
            if (minerHealth <= 0f)
            {
                ReleaseTarget();
                SetMovingAnimationState(false);
                StopHorizontalMovement();
                // Only the purchasing shop changes its saved population; authored miners
                // without a shop owner are removed without touching purchased counts.
                if (owningShop == null || !owningShop.TryRemoveNpc(this))
                {
                    gameObject.SetActive(false);
                    Destroy(gameObject);
                }
            }
            return dealt;
        }


        [Header("References")]
        [SerializeField] private OreSpawner oreSpawner;
        [SerializeField] private NpcData npcData;
        [SerializeField] private NpcProgressionSystem progressionSystem;
        [SerializeField] private MiningAudioManager audioManager;
        private MiningItemSystem itemSystem;
        [SerializeField] private Transform toolPivot;

        [Header("Animator")]
        [Tooltip("Auto-resolved from a child object if left empty (GetComponentInChildren).")]
        [SerializeField] private Animator animator;
        [Tooltip("Bool parameter set on the Animator while the NPC is actively mining (swinging).")]
        [SerializeField] private string miningAnimatorBoolParameter = "IsMining";
        [Tooltip("Bool parameter set on the Animator while the NPC is walking toward a target.")]
        [SerializeField] private string movingAnimatorBoolParameter = "IsMoving";
        [Tooltip("Name of the Animator state that plays the mining swing (the 'Mine' box in the controller graph). Once fully inside this state (after any Idle->Mine blend finishes), its playback SPEED is adjusted every frame so exactly one loop of the clip takes the same time as one hit (SecondsPerHit) - so the swing and the mining SFX stay roughly in step without ever forcing the pose/time directly (which can distort the rig).")]
        [SerializeField] private string mineAnimatorStateName = "Mine";

        [Header("Night Headlamp")]
        [Tooltip("Offset from the miner head. When no head exists yet, this is placed above the NPC root instead.")]
        [SerializeField] private Vector3 headlampOffset = new(0f, 0.08f, 0.12f);
        [Min(0f), SerializeField] private float headlampIntensity = 5.5f;
        [Min(0.1f), SerializeField] private float headlampRange = 12f;
        [SerializeField] private Color headlampColor = new(1f, 0.93f, 0.72f);

        [Header("Ground Clamp (floating-feet safety net)")]
        [Tooltip("Assign the visual model's root (the object holding the Animator/skeleton) to enable the runtime fix below. Leave empty to disable.")]
        [SerializeField] private Transform visualModelRoot;
        [Tooltip("Left foot bone. With Right Foot Bone below, the model is pulled down each frame until the lowest foot touches the ground - this compensates for an animation clip that is not baked correctly (Root Transform Position Y / Bake Into Pose), which is the real fix and should still be done on the clip.")]
        [SerializeField] private Transform leftFootBone;
        [SerializeField] private Transform rightFootBone;
        [Tooltip("How far a foot may sit above or below the NPC's true local ground level (bottom of the Capsule Collider, not local Y = 0) before it is treated as floating/clipping and corrected.")]
        [SerializeField, Min(0f)] private float groundClampEpsilon = 0.01f;

        [Header("Global Pathfinding")]
        [Tooltip("Routes through MiningNavigation for the actual shortest route to the target instead of only reacting to whatever is directly ahead. Uses Unity NavMesh when a MiningNavMeshBuilder exists, otherwise the MiningNavGrid A* fallback, otherwise the old direct-line reactive steering.")]
        [SerializeField] private bool useGlobalPathfinding = true;
        [Tooltip("How far from the miner / its stand position the NavMesh query may search for a valid point on the mesh. A little slack avoids failed queries near a NavMesh edge.")]
        [Min(0.1f)][SerializeField] private float navMeshSampleRadius = 2f;
        [Tooltip("Minimum time between path requests to MiningNavGrid for the same target.")]
        [Min(0.05f)][SerializeField] private float repathInterval = 0.4f;
        [Tooltip("How far the stand position has to move before a fresh path is requested early (instead of waiting for Repath Interval).")]
        [Min(0f)][SerializeField] private float repathTargetMoveThreshold = 0.5f;
        [Tooltip("Within this distance of the mining stand position, the miner may walk straight in only when no other ore blocks that final segment. A neighbouring ore keeps the global route active so the miner does not bounce left and right around a cluster.")]
        [Min(0.1f)][SerializeField] private float finalApproachDistance = 2.5f;
        [Tooltip("How far ahead along the route to aim while path-following. Steering exactly at the next corner makes the miner hug it and then snap to the next heading - that is the visible zig-zag. Aiming at a point further along the polyline cuts corners smoothly. Keep it under the typical corner spacing.")]
        [Min(0.1f)][SerializeField] private float pathLookAheadDistance = 1.75f;
        [Tooltip("How many times a stuck miner will force a fresh route before giving up on the target (commanded targets never give up, they just keep re-routing).")]
        [Min(1)][SerializeField] private int maximumStuckRepathAttempts = 3;

        [Header("Path Debug")]
        [Tooltip("Draw the current NavMesh/A* route in the Scene view while the game is running.")]
        [SerializeField] private bool drawPathGizmos = true;

        private readonly List<Vector3> currentPath = new();
        private readonly List<Vector3> pathRequestBuffer = new();
        private int pathWaypointIndex;
        private float nextRepathTime;
        private Vector3 lastPathTarget;
        private bool hasPathTarget;
        private bool inFinalApproach;
        private MiningPathSource currentPathSource;
        private int stuckRepathAttempts;
        public float NavigationRadius => npcData != null ? GetObstacleProbeRadius() : .4f;
        private Ore approachOre;
        private Vector3 oreApproachPoint;
        private float nextApproachRefresh;

        private readonly RaycastHit[] obstacleHits = new RaycastHit[32];
        private Ore targetOre;
        private LuckyBlock targetLuckyBlock;
        private MiningChest targetChest;
        private Ore ignoredOre;
        private LuckyBlock ignoredLuckyBlock;
        private MiningChest ignoredChest;
        private Component avoidanceObstacle;
        private LuckyBlockDropSystem luckyBlockSystem;
        private Rigidbody body;
        private float nextTargetRefreshTime;
        private float nextTargetSwitchTime;
        private float ignoredOreUntil;
        private float ignoredLuckyBlockUntil;
        private float ignoredChestUntil;
        private float lastProgressTime;
        private float avoidanceSide = 1f;
        private float detourDirectionUntil;
        private int reservedSlot = -1;
        private Vector3 desiredMoveTarget;
        private Vector3 desiredFacingDirection;
        private Vector3 lastProgressPosition;
        private Vector3 detourDirection;
        private Vector3 detourWaypoint;
        private Vector3 detourExitWaypoint;
        private Component detourWaypointObstacle;
        private bool hasMoveTarget;
        private bool hasDetourWaypoint;
        private bool hasDetourExitWaypoint;
        private bool hasCommandedTarget;
        private bool isMining;
        private bool isMoving;
        private CapsuleCollider capsule;
        private bool hasMiningBoolParameter;
        private bool hasMovingBoolParameter;
        private float visualModelBaseLocalY;
        private int mineStateHash;

        public void ConfigureTool(Transform targetToolPivot)
        {
            toolPivot = targetToolPivot;
        }

        /// <summary>Attachment point used by equipped miner tool cosmetics.</summary>
        public Transform ToolPivot => toolPivot;

        /// <summary>Moves this miner's AI to the ore population owned by the active world area.</summary>
        public void SetOreSpawner(OreSpawner targetSpawner)
        {
            if (oreSpawner == targetSpawner) return;
            ReleaseTarget();
            oreSpawner = targetSpawner;
            ignoredOre = null;
            ignoredOreUntil = 0f;
            nextTargetRefreshTime = 0f;
            ResetProgressTracking();
        }

        /// <summary>
        /// Immediately replaces the current AI-selected target with the requested ore.
        /// Commanded targets are kept until they are depleted, disabled, or replaced by
        /// another player command.
        /// </summary>
        public bool CommandMine(Ore ore)
        {
            if (IsStunned) return false;
            if (oreSpawner == null || !CanMine(ore) ||
                !oreSpawner.TryReserveOre(this, ore, CurrentMiningPower, out int slotIndex))
            {
                return false;
            }

            ignoredOre = null;
            ignoredOreUntil = 0f;
            ignoredLuckyBlock = null;
            ignoredLuckyBlockUntil = 0f;
            SetTarget(ore, slotIndex, true);
            return true;
        }

        /// <summary>Immediately replaces the current AI-selected target with a Lucky Block.</summary>
        public bool CommandMine(LuckyBlock block)
        {
            if (IsStunned) return false;
            if (luckyBlockSystem == null || !CanMine(block) ||
                !luckyBlockSystem.TryReserveBlock(this, block, CurrentMiningPower,
                    out int slotIndex))
            {
                return false;
            }

            ignoredOre = null;
            ignoredOreUntil = 0f;
            ignoredLuckyBlock = null;
            ignoredLuckyBlockUntil = 0f;
            SetTarget(block, slotIndex, true);
            return true;
        }

        public bool CommandMine(MiningChest chest)
        {
            if (IsStunned) return false;
            if (!CanMine(chest) || !chest.TryReserveMiner(this, CurrentMiningPower)) return false;
            ignoredChest = null;
            ignoredChestUntil = 0f;
            SetTarget(chest, true);
            return true;
        }

        public void Initialize(OreSpawner targetSpawner, NpcData targetNpcData)
        {
            Initialize(targetSpawner, targetNpcData, null, null);
        }

        public void Initialize(OreSpawner targetSpawner, NpcData targetNpcData,
            LuckyBlockDropSystem targetLuckyBlockSystem)
        {
            Initialize(targetSpawner, targetNpcData, targetLuckyBlockSystem, null);
        }

        public void Initialize(OreSpawner targetSpawner, NpcData targetNpcData,
            LuckyBlockDropSystem targetLuckyBlockSystem,
            NpcProgressionSystem targetProgressionSystem)
        {
            ReleaseTarget();
            oreSpawner = targetSpawner;
            npcData = targetNpcData;
            minerHealth = npcData != null ? npcData.MinerHealth : 20f;
            MiningNpcHealthBar.Ensure(this, npcData);
            HealthChanged?.Invoke(minerHealth, MaxHealth);
            luckyBlockSystem = targetLuckyBlockSystem;
            progressionSystem = targetProgressionSystem != null
                ? targetProgressionSystem
                : FindFirstObjectByType<NpcProgressionSystem>(FindObjectsInactive.Include);
            nextTargetRefreshTime = 0f;
            ConfigurePhysics();
            MiningNavMeshBuilder.Instance?.EnsureMinerClearance(NavigationRadius);
            ConfigureShadows();
            RegisterNpcCollisionPairing();
            ResetProgressTracking();
            EnsureNightHeadlamp();
        }

        private int CurrentMiningPower => progressionSystem != null
            ? progressionSystem.CurrentMiningPower
            : npcData != null ? npcData.MiningPower : 1;

        private void Awake()
        {
            minerHealth = npcData != null ? npcData.MinerHealth : 20f;
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (animator != null)
            {
                if (animator.GetComponent<MiningNpcAnimationEventRelay>() == null)
                {
                    animator.gameObject.AddComponent<MiningNpcAnimationEventRelay>();
                }
            }

            if (audioManager == null)
            {
                audioManager = FindFirstObjectByType<MiningAudioManager>(FindObjectsInactive.Include);
            }
            itemSystem ??= FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);

            if (animator != null)
            {
                // Physics/script (this component) drives movement, not the animation clip -
                // keeping this off avoids the Animator fighting the Rigidbody every frame.
                animator.applyRootMotion = false;
                hasMiningBoolParameter = HasParameter(
                    animator, miningAnimatorBoolParameter, AnimatorControllerParameterType.Bool);
                hasMovingBoolParameter = HasParameter(
                    animator, movingAnimatorBoolParameter, AnimatorControllerParameterType.Bool);
            }

            mineStateHash = Animator.StringToHash(mineAnimatorStateName);

            if (visualModelRoot != null)
            {
                visualModelBaseLocalY = visualModelRoot.localPosition.y;
            }

            ConfigurePhysics();
            ConfigureShadows();
            ResetProgressTracking();
            EnsureNightHeadlamp();
        }

        private void OnValidate()
        {
            headlampIntensity = Mathf.Max(0f, headlampIntensity);
            headlampRange = Mathf.Max(0.1f, headlampRange);

            if (Application.isPlaying)
            {
                EnsureNightHeadlamp();
            }
        }

        private void Start()
        {
            MiningNpcHealthBar.Ensure(this, npcData);
        }

        private void EnsureNightHeadlamp()
        {
            MiningNpcHeadlamp headlamp = GetComponent<MiningNpcHeadlamp>();
            if (headlamp == null)
            {
                headlamp = gameObject.AddComponent<MiningNpcHeadlamp>();
            }

            headlamp.Configure(headlampOffset, headlampIntensity, headlampRange, headlampColor);
        }

        private void OnEnable()
        {
            OreActorTraversal.RegisterActor(gameObject);
            RegisterNpcCollisionPairing();
        }

        private void OnDisable()
        {
            OreActorTraversal.UnregisterActor(gameObject);
            ActiveNpcs.Remove(this);
            ReleaseTarget();
            hasMoveTarget = false;
            desiredFacingDirection = Vector3.zero;
            detourDirection = Vector3.zero;
            detourDirectionUntil = 0f;
            ResetGlobalPath();
            SetMovingAnimationState(false);
            StopHorizontalMovement();
        }

        private void Update()
        {
            hasMoveTarget = false;
            desiredFacingDirection = Vector3.zero;
            if (IsStunned) return;
            if (oreSpawner == null || npcData == null)
            {
                ReleaseTarget();
                return;
            }

            if (ignoredOre != null && Time.time >= ignoredOreUntil)
            {
                ignoredOre = null;
            }

            if (ignoredLuckyBlock != null && Time.time >= ignoredLuckyBlockUntil)
            {
                ignoredLuckyBlock = null;
            }

            if (ignoredChest != null && Time.time >= ignoredChestUntil)
                ignoredChest = null;

            if (!IsTargetValid())
            {
                ReleaseTarget();
            }

            if (targetOre == null && targetLuckyBlock == null && targetChest == null &&
                Time.time >= nextTargetRefreshTime)
            {
                TryAcquireTarget(body != null ? body.position : transform.position);
                nextTargetRefreshTime = Time.time + npcData.TargetRefreshInterval;
            }

            if (!IsTargetValid())
            {
                return;
            }

            Vector3 currentPosition = body != null ? body.position : transform.position;
            if (targetOre != null && targetOre.SqrDistanceToSurface(currentPosition) >
                npcData.MiningRange * npcData.MiningRange)
            {
                TryAdoptVisibleOre(currentPosition);
            }

            Vector3 pathTarget = GetPathTargetPosition(currentPosition);
            Vector3 oreOffset = GetTargetPosition() - currentPosition;
            oreOffset.y = 0f;
            desiredFacingDirection = oreOffset;

            bool isWithinMiningRange = SqrDistanceToTargetSurface(currentPosition) <=
                                       npcData.MiningRange * npcData.MiningRange;

            if (!isWithinMiningRange)
            {
                SetMiningAnimationState(false);
                desiredMoveTarget = pathTarget;
                hasMoveTarget = true;
                UpdateGlobalPath(currentPosition, pathTarget);
                TrackMovementProgress(currentPosition);
                return;
            }

            ResetGlobalPath();
            ResetProgressTracking();
            SetMiningAnimationState(true);
        }

        public void OnMiningImpact()
        {
            if (IsStunned) return;
            if (!isMining || !IsTargetValid())
            {
                return;
            }

            ApplyDamageToTarget();
            PlayMiningImpactAudio();
            if (!IsTargetValid())
            {
                ReleaseTarget();
                nextTargetRefreshTime = 0f;
            }
        }

        private void FixedUpdate()
        {
            if (IsStunned) { StopHorizontalMovement(); return; }
            if (npcData == null || body == null)
            {
                SetMovingAnimationState(false);
                return;
            }

            Vector3 currentPosition = body.position;
            float speedMultiplier = GetMoveSpeedMultiplier();
            // Scale acceleration linearly and braking quadratically with speed upgrades so fast
            // miners keep the same acceleration time and stopping distance as normal-speed ones.
            float movementAcceleration = npcData.MovementAcceleration * speedMultiplier;
            float brakingAcceleration = npcData.BrakingAcceleration * speedMultiplier * speedMultiplier;

            // A global path already routes around every known ore. The reactive detour system
            // solves that same problem locally and badly, so running both means two controllers
            // fighting for the heading every frame - that fight is what made a commanded
            // (middle-clicked) target jitter in place: the path says "go left around the rock",
            // the detour says "go right", and the NPC averages into standing still.
            // While a path is live the detour layer is suppressed entirely.
            Vector3 navigationTarget = GetNavigationTarget(currentPosition, desiredMoveTarget);
            bool hasGlobalPath = currentPath.Count > 0;
            if (!hasGlobalPath && TryGetDetourWaypoint(currentPosition, out Vector3 waypoint))
            {
                navigationTarget = waypoint;
            }

            Vector3 movementOffset = navigationTarget - currentPosition;
            movementOffset.y = 0f;
            if (!hasMoveTarget || movementOffset.sqrMagnitude <=
                npcData.StoppingDistance * npcData.StoppingDistance)
            {
                SetMovingAnimationState(false);
                ApplyHorizontalVelocity(Vector3.zero, movementAcceleration, brakingAcceleration);
                RotateTowards(desiredFacingDirection);
                return;
            }

            Vector3 movementDirection = movementOffset.normalized;
            float probeDistance = Mathf.Min(npcData.ObstacleProbeDistance, movementOffset.magnitude);
            if (TryGetBlockingMineable(movementDirection, probeDistance, out Component blocker,
                out Vector3 blockingPoint))
            {
                Ore blockingOre = blocker as Ore;
                if (!hasCommandedTarget && Time.time >= nextTargetSwitchTime &&
                    blockingOre != null && blockingOre != ignoredOre &&
                    CanMine(blockingOre) && IsBlockingOreCloser(blockingOre, currentPosition) &&
                    TrySwitchTarget(blockingOre))
                {
                    RotateTowards(movementDirection);
                    SetMovingAnimationState(true);
                    return;
                }

                // A fresh ore or a tight corner can invalidate body clearance even
                // when the old route was valid. Repath on the normal cadence and
                // use the existing committed detour while carving catches up.
                if (hasGlobalPath)
                {
                    currentPath.Clear();
                    pathWaypointIndex = 0;
                    currentPathSource = MiningPathSource.None;
                    nextRepathTime = Time.time + repathInterval;
                    hasGlobalPath = false;
                }

                if (!hasGlobalPath)
                {
                    movementDirection = ResolveBlockedPath(
                        movementDirection, currentPosition, blocker, blockingPoint);
                }
            }
            else if (!hasGlobalPath)
            {
                avoidanceObstacle = null;
                if (Time.time < detourDirectionUntil && detourDirection.sqrMagnitude > Mathf.Epsilon)
                {
                    movementDirection = detourDirection;
                }
                else
                {
                    ClearDetour();
                }
            }

            float maximumSpeed = npcData.MoveSpeed * speedMultiplier;
            float brakingDistance = Mathf.Max(0f,
                movementOffset.magnitude - npcData.StoppingDistance);
            float arrivalSpeed = Mathf.Sqrt(2f * brakingAcceleration * brakingDistance);
            float cornerSpeed = GetCornerSpeedLimit(currentPosition, maximumSpeed,
                brakingAcceleration);
            Vector3 desiredVelocity = movementDirection * Mathf.Min(maximumSpeed, arrivalSpeed,
                cornerSpeed);
            ApplyHorizontalVelocity(desiredVelocity, movementAcceleration, brakingAcceleration);
            SetMovingAnimationState(true);
            RotateTowards(movementDirection);
        }

        private void ClearDetour()
        {
            detourDirection = Vector3.zero;
            detourDirectionUntil = 0f;
            detourWaypoint = Vector3.zero;
            detourExitWaypoint = Vector3.zero;
            detourWaypointObstacle = null;
            hasDetourWaypoint = false;
            hasDetourExitWaypoint = false;
        }

        private void ApplyHorizontalVelocity(Vector3 desiredHorizontalVelocity, float acceleration,
            float brakingAcceleration)
        {
            Vector3 currentVelocity = body.linearVelocity;
            Vector3 currentHorizontalVelocity = new(currentVelocity.x, 0f, currentVelocity.z);
            float desiredSpeed = desiredHorizontalVelocity.magnitude;
            Vector3 nextHorizontalVelocity;
            if (desiredSpeed <= Mathf.Epsilon)
            {
                nextHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity,
                    Vector3.zero, brakingAcceleration * Time.fixedDeltaTime);
            }
            else
            {
                Vector3 desiredDirection = desiredHorizontalVelocity / desiredSpeed;

                // The route target can turn abruptly at a path corner. Retaining the old
                // perpendicular Rigidbody velocity lets a fast miner slide past that corner
                // before braking catches up. Remove only that sideways momentum immediately;
                // acceleration and braking still control speed along the new heading.
                float currentForwardSpeed = Mathf.Max(0f,
                    Vector3.Dot(currentHorizontalVelocity, desiredDirection));
                float response = desiredSpeed < currentForwardSpeed
                    ? brakingAcceleration
                    : acceleration;
                float nextSpeed = Mathf.MoveTowards(currentForwardSpeed, desiredSpeed,
                    response * Time.fixedDeltaTime);
                nextHorizontalVelocity = desiredDirection * nextSpeed;
            }

            body.linearVelocity = new Vector3(
                nextHorizontalVelocity.x, currentVelocity.y, nextHorizontalVelocity.z);
        }

        private void StopHorizontalMovement()
        {
            if (body == null || body.isKinematic)
            {
                return;
            }

            Vector3 velocity = body.linearVelocity;
            body.linearVelocity = new Vector3(0f, velocity.y, 0f);
        }

        private void RotateTowards(Vector3 direction)
        {
            body.angularVelocity = Vector3.zero;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            body.MoveRotation(Quaternion.RotateTowards(
                body.rotation, targetRotation, npcData.TurnSpeed * Time.fixedDeltaTime));
        }

        private void ConfigurePhysics()
        {
            if (npcData == null)
            {
                return;
            }

            capsule ??= GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.radius = npcData.ColliderRadius;
                capsule.height = npcData.ColliderHeight;
            }

            body ??= GetComponent<Rigidbody>();
            if (body != null)
            {
                body.mass = npcData.Mass;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.useGravity = true;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.constraints = RigidbodyConstraints.FreezeRotationX |
                                   RigidbodyConstraints.FreezeRotationZ;
            }
        }

        private void ConfigureShadows()
        {
            if (npcData == null)
            {
                return;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.shadowCastingMode = npcData.CastShadows
                    ? ShadowCastingMode.On
                    : ShadowCastingMode.Off;
                targetRenderer.receiveShadows = npcData.ReceiveShadows;
            }
        }

        private void RegisterNpcCollisionPairing()
        {
            capsule ??= GetComponent<CapsuleCollider>();
            foreach (MushroomMonster monster in MushroomMonster.Monsters)
                if (monster != null) IgnoreMonsterCollision(monster);
            for (int index = ActiveNpcs.Count - 1; index >= 0; index--)
            {
                MiningNpc other = ActiveNpcs[index];
                if (other == null)
                {
                    ActiveNpcs.RemoveAt(index);
                    continue;
                }

                if (other == this)
                {
                    continue;
                }

                // Miners may overlap and pass through each other. Keep the capsule active so it
                // can still stand on the floor and respect non-miner world collision.
                if (capsule != null)
                {
                    other.capsule ??= other.GetComponent<CapsuleCollider>();
                    if (other.capsule != null)
                    {
                        Physics.IgnoreCollision(capsule, other.capsule, true);
                    }
                }
            }

            if (!ActiveNpcs.Contains(this))
            {
                ActiveNpcs.Add(this);
            }
        }
    }
}
