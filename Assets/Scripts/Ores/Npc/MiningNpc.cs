using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MiningSimulator.Ores
{
    /// <summary>Reserves a compatible ore, steers around dynamic blockers, then mines it.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))]
    public sealed class MiningNpc : MonoBehaviour
    {
        private static readonly List<MiningNpc> ActiveNpcs = new();

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
            luckyBlockSystem = targetLuckyBlockSystem;
            progressionSystem = targetProgressionSystem != null
                ? targetProgressionSystem
                : FindFirstObjectByType<NpcProgressionSystem>(FindObjectsInactive.Include);
            nextTargetRefreshTime = 0f;
            ConfigurePhysics();
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
            RegisterNpcCollisionPairing();
        }

        private void OnDisable()
        {
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

                // A chest can spawn after the path was calculated. Drop that stale
                // path so the next query includes its carved footprint; steer
                // around it locally on this frame too.
                if (hasGlobalPath && blocker is MiningChest)
                {
                    currentPath.Clear();
                    pathWaypointIndex = 0;
                    currentPathSource = MiningPathSource.None;
                    nextRepathTime = 0f;
                    hasGlobalPath = false;
                }

                // With a global path the route around an ore is already planned, so the heading
                // is left alone. The probe above still runs purely so a non-commanded miner can
                // opportunistically claim a closer ore it happens to walk past (handled in the
                // branch above) - that is a gameplay feature, not steering.
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

        private void LateUpdate()
        {
            if (animator != null && hasMiningBoolParameter)
            {
                if (isMining && npcData != null)
                {
                    itemSystem ??= FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (stateInfo.shortNameHash == mineStateHash && !animator.IsInTransition(0) &&
                        stateInfo.length > 0.0001f)
                    {
                        // Stretch/compress the clip's own playback speed so exactly one loop
                        // takes the same time as one hit (SecondsPerHit). This keeps Unity's
                        // normal Animator update (blending, any IK, avatar masks) completely
                        // intact - unlike forcing the normalized time directly, which can snap
                        // the rig into a broken pose if the state relies on more than a single
                        // plain clip.
                        float miningSpeed = itemSystem != null ? itemSystem.MiningSpeedMultiplier : 1f;
                        animator.speed = stateInfo.length * miningSpeed /
                            Mathf.Max(0.0001f, npcData.SecondsPerHit);
                    }
                }
                else if (!Mathf.Approximately(animator.speed, 1f))
                {
                    animator.speed = 1f;
                }
            }

            ApplyGroundClamp();
        }

        private void SetMiningAnimationState(bool mining)
        {
            if (isMining == mining)
            {
                if (mining)
                {
                    SetMovingAnimationState(false);
                }
                return;
            }

            isMining = mining;
            if (animator != null && hasMiningBoolParameter)
            {
                animator.SetBool(miningAnimatorBoolParameter, mining);
            }

            if (mining)
            {
                SetMovingAnimationState(false);
            }
        }

        private void SetMovingAnimationState(bool moving)
        {
            moving &= !isMining;
            if (isMoving == moving)
            {
                return;
            }

            isMoving = moving;
            if (animator != null && hasMovingBoolParameter)
            {
                animator.SetBool(movingAnimatorBoolParameter, moving);
            }
        }

        /// <summary>
        /// Runtime safety net for floating or clipping feet: pulls the visual model up or down
        /// each frame until the lower of the two assigned foot bones sits exactly on the NPC's
        /// true local ground level - the bottom of the Capsule Collider, NOT local Y = 0 (the
        /// capsule's center sits at its middle by default, so the ground is half the capsule's
        /// height below the transform's origin; see localGroundY below). This does NOT replace
        /// the proper fix - baking "Root Transform Position (Y)" into the animation clip's pose
        /// on the FBX import settings (Based Upon: Original) - it only masks the symptom for
        /// clips that were not baked correctly, or as an extra safeguard.
        /// </summary>
        private void ApplyGroundClamp()
        {
            if (visualModelRoot == null || leftFootBone == null || rightFootBone == null)
            {
                return;
            }

            // The capsule's center sits at the middle of its height (Unity default), not at the
            // NPC transform's local origin - so local Y = 0 is NOT the ground. The true ground,
            // in the same local space as the foot bones below, is the bottom of the capsule
            // (this is exactly the offset NpcData.SpawnHeightOffset lifts the NPC by at spawn so
            // the capsule's bottom touches the real ground).
            float localGroundY = capsule != null ? capsule.center.y - capsule.height * 0.5f : 0f;

            Vector3 localPosition = visualModelRoot.localPosition;
            localPosition.y = visualModelBaseLocalY;
            visualModelRoot.localPosition = localPosition;

            float leftFootY = transform.InverseTransformPoint(leftFootBone.position).y;
            float rightFootY = transform.InverseTransformPoint(rightFootBone.position).y;
            float lowestFootY = Mathf.Min(leftFootY, rightFootY);
            float offsetFromGround = lowestFootY - localGroundY;
            if (Mathf.Abs(offsetFromGround) > groundClampEpsilon)
            {
                localPosition.y = visualModelBaseLocalY - offsetFromGround;
                visualModelRoot.localPosition = localPosition;
            }
        }

        private static bool HasParameter(Animator target, string parameterName,
            AnimatorControllerParameterType parameterType)
        {
            if (target == null || string.IsNullOrEmpty(parameterName))
            {
                return false;
            }

            foreach (AnimatorControllerParameter parameter in target.parameters)
            {
                if (parameter.type == parameterType && parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }

        private void TryAcquireTarget(Vector3 currentPosition)
        {
            Ore excludedOre = Time.time < ignoredOreUntil ? ignoredOre : null;
            LuckyBlock excludedBlock = Time.time < ignoredLuckyBlockUntil
                ? ignoredLuckyBlock
                : null;
            MiningChest excludedChest = Time.time < ignoredChestUntil ? ignoredChest : null;
            bool foundOre = oreSpawner.TryReserveClosestOre(this, currentPosition,
                CurrentMiningPower, excludedOre, out Ore ore, out int oreSlotIndex);
            LuckyBlock block = null;
            int blockSlotIndex = -1;
            bool foundBlock = luckyBlockSystem != null &&
                              luckyBlockSystem.TryReserveClosestBlock(this, currentPosition,
                                  CurrentMiningPower, excludedBlock, out block,
                                  out blockSlotIndex);

            bool foundChest = MiningChest.TryReserveClosest(this, currentPosition,
                CurrentMiningPower, excludedChest, out MiningChest chest);
            float oreDistance = foundOre ? ore.SqrDistanceToSurface(currentPosition) : float.PositiveInfinity;
            float blockDistance = foundBlock ? block.SqrDistanceToSurface(currentPosition) : float.PositiveInfinity;
            float chestDistance = foundChest ? chest.SqrDistanceToSurface(currentPosition) : float.PositiveInfinity;
            if (foundChest && chestDistance <= oreDistance && chestDistance <= blockDistance)
            {
                if (foundOre) ore.ReleaseMiner(this);
                if (foundBlock) block.ReleaseMiner(this);
                SetTarget(chest);
                return;
            }
            if (foundChest) chest.ReleaseMiner(this);

            if (foundOre && foundBlock)
            {
                if (block.SqrDistanceToSurface(currentPosition) <
                    ore.SqrDistanceToSurface(currentPosition))
                {
                    ore.ReleaseMiner(this);
                    SetTarget(block, blockSlotIndex);
                }
                else
                {
                    block.ReleaseMiner(this);
                    SetTarget(ore, oreSlotIndex);
                }
            }
            else if (foundBlock)
            {
                SetTarget(block, blockSlotIndex);
            }
            else if (foundOre)
            {
                SetTarget(ore, oreSlotIndex);
            }
        }

        private bool TrySwitchTarget(Ore ore)
        {
            if (ore == null || ore == targetOre)
            {
                return ore == targetOre;
            }

            if (!oreSpawner.TryReserveOre(this, ore, CurrentMiningPower, out int slotIndex))
            {
                return false;
            }

            SetTarget(ore, slotIndex);
            return true;
        }

        private void SetTarget(Ore ore, int slotIndex, bool commandedTarget = false)
        {
            Ore previousOre = targetOre;
            LuckyBlock previousBlock = targetLuckyBlock;
            MiningChest previousChest = targetChest;
            targetOre = ore;
            targetLuckyBlock = null;
            targetChest = null;
            reservedSlot = slotIndex;
            hasCommandedTarget = commandedTarget;
            previousBlock?.ReleaseMiner(this);
            previousChest?.ReleaseMiner(this);
            if (previousOre != null && previousOre != ore)
            {
                previousOre.ReleaseMiner(this);
                ignoredOre = previousOre;
                ignoredOreUntil = Time.time + npcData.IgnoredTargetDuration;
            }

            SetMiningAnimationState(false);
            nextTargetSwitchTime = Time.time + npcData.TargetSwitchCooldown;
            stuckRepathAttempts = 0;
            ClearDetour();
            ResetGlobalPath();
            ResetProgressTracking();
        }

        private void SetTarget(LuckyBlock block, int slotIndex, bool commandedTarget = false)
        {
            Ore previousOre = targetOre;
            LuckyBlock previousBlock = targetLuckyBlock;
            MiningChest previousChest = targetChest;
            targetOre = null;
            targetLuckyBlock = block;
            targetChest = null;
            reservedSlot = slotIndex;
            hasCommandedTarget = commandedTarget;
            previousOre?.ReleaseMiner(this);
            previousChest?.ReleaseMiner(this);
            if (previousBlock != null && previousBlock != block)
            {
                previousBlock.ReleaseMiner(this);
                ignoredLuckyBlock = previousBlock;
                ignoredLuckyBlockUntil = Time.time + npcData.IgnoredTargetDuration;
            }

            SetMiningAnimationState(false);
            nextTargetSwitchTime = Time.time + npcData.TargetSwitchCooldown;
            stuckRepathAttempts = 0;
            ClearDetour();
            ResetGlobalPath();
            ResetProgressTracking();
        }

        private void SetTarget(MiningChest chest, bool commandedTarget = false)
        {
            Ore previousOre = targetOre;
            LuckyBlock previousBlock = targetLuckyBlock;
            MiningChest previousChest = targetChest;
            targetOre = null;
            targetLuckyBlock = null;
            targetChest = chest;
            reservedSlot = -1;
            hasCommandedTarget = commandedTarget;
            previousOre?.ReleaseMiner(this);
            previousBlock?.ReleaseMiner(this);
            if (previousChest != null && previousChest != chest)
                previousChest.ReleaseMiner(this);
            SetMiningAnimationState(false);
            nextTargetSwitchTime = Time.time + npcData.TargetSwitchCooldown;
            stuckRepathAttempts = 0;
            ClearDetour();
            ResetGlobalPath();
            ResetProgressTracking();
        }

        private Vector3 GetPathTargetPosition(Vector3 currentPosition)
        {
            if (targetChest != null)
                return targetChest.GetClosestSurfacePoint(currentPosition);
            if (targetLuckyBlock != null)
            {
                return targetLuckyBlock.GetMiningStandPosition(
                    reservedSlot, npcData.ColliderRadius, npcData.StandSlotSpacingPadding);
            }

            return targetOre != null
                ? targetOre.GetClosestSurfacePoint(currentPosition)
                : transform.position;
        }

        /// <summary>Prevents runtime ore placement on or directly beside an active miner.</summary>
        public static bool IsSpawnPositionClear(Vector3 position, float clearance)
        {
            clearance = Mathf.Max(0f, clearance);
            for (int index = ActiveNpcs.Count - 1; index >= 0; index--)
            {
                MiningNpc npc = ActiveNpcs[index];
                if (npc == null)
                {
                    ActiveNpcs.RemoveAt(index);
                    continue;
                }

                if (!npc.isActiveAndEnabled)
                {
                    continue;
                }

                Vector3 npcPosition = npc.body != null ? npc.body.position : npc.transform.position;
                Vector3 offset = position - npcPosition;
                offset.y = 0f;
                float npcRadius = npc.npcData != null
                    ? Mathf.Max(0f, npc.npcData.ColliderRadius)
                    : 0.5f;
                float requiredDistance = clearance + npcRadius;
                if (offset.sqrMagnitude < requiredDistance * requiredDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsTargetValid()
        {
            if (targetChest != null) return CanMine(targetChest);
            return targetLuckyBlock != null
                ? CanMine(targetLuckyBlock)
                : CanMine(targetOre);
        }

        private bool CanMine(Ore ore)
        {
            return npcData != null && ore != null && ore.isActiveAndEnabled &&
                   !ore.IsDepleted && ore.Data != null &&
                   ore.Data.MiningPowerRequired <= CurrentMiningPower;
        }

        private bool CanMine(LuckyBlock block)
        {
            return npcData != null && block != null && block.isActiveAndEnabled &&
                   !block.IsResolved && block.CanAcceptMiner(this, CurrentMiningPower);
        }

        private bool CanMine(MiningChest chest)
        {
            return npcData != null && chest != null && chest.CanAcceptMiner(this, CurrentMiningPower);
        }

        private Vector3 GetTargetPosition()
        {
            if (targetChest != null) return targetChest.transform.position;
            return targetLuckyBlock != null
                ? targetLuckyBlock.transform.position
                : targetOre != null ? targetOre.transform.position : transform.position;
        }

        private float SqrDistanceToTargetSurface(Vector3 currentPosition)
        {
            if (targetChest != null) return targetChest.SqrDistanceToSurface(currentPosition);
            return targetLuckyBlock != null
                ? targetLuckyBlock.SqrDistanceToSurface(currentPosition)
                : targetOre != null
                    ? targetOre.SqrDistanceToSurface(currentPosition)
                    : float.PositiveInfinity;
        }

        private void ApplyDamageToTarget()
        {
            itemSystem ??= FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
            if (targetChest != null)
            {
                float damage = progressionSystem != null
                    ? progressionSystem.CurrentDamagePerHit : npcData.DamagePerHit;
                targetChest.ApplyNpcDamage(damage);
                return;
            }
            if (targetLuckyBlock != null)
            {
                float damage = progressionSystem != null
                    ? progressionSystem.CurrentDamagePerHit
                    : npcData.DamagePerHit;
                targetLuckyBlock.ApplyNpcDamage(itemSystem != null
                    ? itemSystem.RollOreLuckyDamage(damage) : damage);
            }
            else
            {
                float damage = progressionSystem != null
                    ? progressionSystem.CurrentDamagePerHit
                    : npcData.DamagePerHit;
                targetOre?.ApplyNpcDamage(itemSystem != null
                    ? itemSystem.RollOreLuckyDamage(damage) : damage);
            }
        }

        private void PlayMiningImpactAudio()
        {
            if (audioManager == null)
            {
                return;
            }

            if (targetOre != null && targetOre.LastDamageWasNpc)
            {
                audioManager.PlayMiningImpactSfx(targetOre.IsDepleted);
                return;
            }

            if (targetLuckyBlock != null && targetLuckyBlock.LastDamageWasNpc)
            {
                audioManager.PlayMiningImpactSfx(targetLuckyBlock.IsResolved);
            }
        }

        private void TryAdoptVisibleOre(Vector3 currentPosition)
        {
            if (hasCommandedTarget || targetLuckyBlock != null || targetChest != null)
            {
                return;
            }

            Vector3 direction = targetOre != null
                ? targetOre.transform.position - currentPosition
                : transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Vector3 origin = currentPosition + Vector3.up * npcData.OreSightOriginHeight;
            int hitCount = Physics.SphereCastNonAlloc(origin, npcData.OreSightProbeRadius,
                direction.normalized, obstacleHits, npcData.OreSightDistance,
                npcData.CollisionLayers, QueryTriggerInteraction.Ignore);
            Ore visibleOre = null;
            float closestHitDistance = float.PositiveInfinity;
            float miningRangeSqr = npcData.MiningRange * npcData.MiningRange;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = obstacleHits[index];
                Ore ore = hit.collider != null ? hit.collider.GetComponentInParent<Ore>() : null;
                if (ore == null || ore == targetOre ||
                    (ore == ignoredOre && Time.time < ignoredOreUntil) ||
                    hit.distance >= closestHitDistance ||
                    !CanMine(ore) || ore.SqrDistanceToSurface(currentPosition) > miningRangeSqr ||
                    !ore.CanAcceptMiner(this, CurrentMiningPower))
                {
                    continue;
                }

                visibleOre = ore;
                closestHitDistance = hit.distance;
            }

            if (visibleOre != null)
            {
                TrySwitchTarget(visibleOre);
            }
        }

        private void ReleaseTarget()
        {
            if (targetOre != null)
            {
                targetOre.ReleaseMiner(this);
            }

            if (targetLuckyBlock != null)
            {
                targetLuckyBlock.ReleaseMiner(this);
            }

            if (targetChest != null) targetChest.ReleaseMiner(this);

            targetOre = null;
            targetLuckyBlock = null;
            targetChest = null;
            reservedSlot = -1;
            hasCommandedTarget = false;
            hasMoveTarget = false;
            stuckRepathAttempts = 0;
            SetMiningAnimationState(false);
            ClearDetour();
            ResetGlobalPath();
        }

        private void TrackMovementProgress(Vector3 currentPosition)
        {
            Vector3 progress = currentPosition - lastProgressPosition;
            progress.y = 0f;
            if (progress.sqrMagnitude >= npcData.StuckProgressDistance * npcData.StuckProgressDistance)
            {
                lastProgressPosition = currentPosition;
                lastProgressTime = Time.time;
                return;
            }

            if (Time.time - lastProgressTime < npcData.StuckTimeout)
            {
                return;
            }

            // First remedy for any stuck miner: force a fresh route. The old route may be stale
            // (ore mined out from under it, pushed off the path by separation, carve boundary
            // shifted). This used to permanently disable pathfinding for the rest of the
            // approach and hand control to the reactive detour layer, which is what produced the
            // wander-off-and-never-commit behaviour - the final-approach handoff in
            // UpdateGlobalPath solves that case properly now, so re-routing is enough.
            if (useGlobalPathfinding && stuckRepathAttempts < maximumStuckRepathAttempts)
            {
                stuckRepathAttempts++;
                ClearDetour();
                ResetGlobalPath();
                ResetProgressTracking();
                return;
            }

            if (hasCommandedTarget)
            {
                // A player command is stronger than the normal stuck-target timeout: never drop a
                // middle-clicked target back to the auto AI. Keep re-routing forever instead, and
                // let the attempt budget refill so the miner never stops trying.
                stuckRepathAttempts = 0;
                ClearDetour();
                ResetGlobalPath();
                ResetProgressTracking();
                return;
            }

            if (targetLuckyBlock != null)
            {
                ignoredLuckyBlock = targetLuckyBlock;
                ignoredLuckyBlockUntil = Time.time + npcData.IgnoredTargetDuration;
            }
            else if (targetChest != null)
            {
                ignoredChest = targetChest;
                ignoredChestUntil = Time.time + npcData.IgnoredTargetDuration;
            }
            else
            {
                ignoredOre = targetOre;
                ignoredOreUntil = Time.time + npcData.IgnoredTargetDuration;
            }
            ReleaseTarget();
            nextTargetRefreshTime = 0f;
            ResetProgressTracking();
        }

        private bool IsBlockingOreCloser(Ore blockingOre, Vector3 currentPosition)
        {
            if (blockingOre == null || targetOre == null)
            {
                return false;
            }

            float blockingDistance = Mathf.Sqrt(blockingOre.SqrDistanceToSurface(currentPosition));
            float targetDistance = Mathf.Sqrt(targetOre.SqrDistanceToSurface(currentPosition));
            return blockingDistance + npcData.TargetSwitchDistanceAdvantage < targetDistance;
        }

        private void ResetProgressTracking()
        {
            lastProgressPosition = body != null ? body.position : transform.position;
            lastProgressTime = Time.time;
        }

        private bool TryGetBlockingMineable(Vector3 direction, float distance,
            out Component blocker,
            out Vector3 blockingPoint)
        {
            blocker = null;
            blockingPoint = Vector3.zero;
            Vector3 origin = body.position + Vector3.up * npcData.ColliderRadius;
            int hitCount = Physics.SphereCastNonAlloc(origin, GetObstacleProbeRadius(),
                direction, obstacleHits, distance, npcData.CollisionLayers,
                QueryTriggerInteraction.Ignore);
            float closestDistance = float.PositiveInfinity;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = obstacleHits[index];
                Ore ore = hit.collider != null ? hit.collider.GetComponentInParent<Ore>() : null;
                MiningChest chest = hit.collider != null
                    ? hit.collider.GetComponentInParent<MiningChest>() : null;
                if (hit.distance >= closestDistance ||
                    (ore == null || ore == targetOre || ore.IsDepleted) &&
                    (chest == null || chest == targetChest || !chest.CanMine)) continue;

                blocker = ore != null && ore != targetOre && !ore.IsDepleted ? ore : chest;
                blockingPoint = hit.point;
                closestDistance = hit.distance;
            }

            return blocker != null;
        }

        private Vector3 CalculateObstacleAvoidance(Vector3 forward, Vector3 currentPosition,
            Component blocker, Vector3 blockingPoint)
        {
            Vector3 toBlocker = blockingPoint - currentPosition;
            toBlocker.y = 0f;
            Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
            if (blocker != avoidanceObstacle)
            {
                avoidanceObstacle = blocker;
                float sideDot = Vector3.Dot(toBlocker, side);
                if (Mathf.Abs(sideDot) <= Mathf.Epsilon)
                {
                    sideDot = 1f;
                }

                avoidanceSide = sideDot > 0f ? -1f : 1f;
            }

            return (forward + side * avoidanceSide * npcData.ObstacleAvoidanceStrength).normalized;
        }

        private Vector3 ResolveBlockedPath(Vector3 forward, Vector3 currentPosition,
            Component blocker, Vector3 blockingPoint)
        {
            if (TryCreateDetourWaypoint(forward, currentPosition, blocker,
                out Vector3 waypointDirection))
            {
                return waypointDirection;
            }

            if (Time.time < detourDirectionUntil && detourDirection.sqrMagnitude > Mathf.Epsilon &&
                GetOreClearance(detourDirection, npcData.DetourProbeDistance) >=
                npcData.DetourMinimumClearance)
            {
                return detourDirection;
            }

            Vector3 normalAvoidance = CalculateObstacleAvoidance(
                forward, currentPosition, blocker, blockingPoint);
            Vector3 bestDirection = normalAvoidance;
            float bestClearance = GetOreClearance(normalAvoidance, npcData.DetourProbeDistance);

            // Prefer the previously selected side when two directions have similar clearance.
            float signedAngle = npcData.DetourAngle * avoidanceSide;
            EvaluateDetourCandidate(forward, signedAngle, ref bestDirection, ref bestClearance);
            EvaluateDetourCandidate(forward, -signedAngle, ref bestDirection, ref bestClearance);
            EvaluateDetourCandidate(forward, signedAngle * 2f, ref bestDirection, ref bestClearance);
            EvaluateDetourCandidate(forward, -signedAngle * 2f, ref bestDirection, ref bestClearance);
            EvaluateDetourCandidate(forward, signedAngle * 3f, ref bestDirection, ref bestClearance);
            EvaluateDetourCandidate(forward, -signedAngle * 3f, ref bestDirection, ref bestClearance);

            if (bestClearance < npcData.DetourMinimumClearance)
            {
                // A closed pair or cluster needs space before another route can be evaluated.
                Vector3 reverseDirection = -forward;
                float reverseClearance = GetOreClearance(
                    reverseDirection, npcData.DetourProbeDistance);
                if (reverseClearance > bestClearance + 0.01f)
                {
                    bestDirection = reverseDirection;
                }
            }

            detourDirection = bestDirection.normalized;
            detourDirectionUntil = Time.time + npcData.DetourDirectionHoldTime;
            ResetProgressTracking();
            return detourDirection;
        }

        private static bool IsMineableObstacleActive(Component obstacle)
        {
            if (obstacle is Ore ore) return ore.isActiveAndEnabled && !ore.IsDepleted;
            if (obstacle is MiningChest chest) return chest.CanMine;
            return false;
        }

        private static bool TryGetMineableObstacleBounds(Component obstacle, out Bounds bounds)
        {
            if (obstacle is Ore ore) return ore.TryGetWorldBounds(out bounds);
            if (obstacle is MiningChest chest) return chest.TryGetWorldBounds(out bounds);
            bounds = default;
            return false;
        }

        private bool TryCreateDetourWaypoint(Vector3 forward, Vector3 currentPosition,
            Component blocker, out Vector3 waypointDirection)
        {
            waypointDirection = Vector3.zero;

            // Already mid-route around this exact ore - keep following that route instead of
            // recalculating from the current position/heading every frame. Recomputing here
            // was the actual pathfinding bug: inside a dense cluster, tiny frame-to-frame shifts
            // in position/forward flip the positive-vs-negative clearance comparison below back
            // and forth, so the NPC kept switching which side to go around on and never actually
            // made progress past the ore (looked "stuck"/jittering in place, most noticeable on
            // a middle-click commanded target since commanded NPCs aren't allowed to just switch
            // to a different ore instead - see the `!hasCommandedTarget` check in FixedUpdate).
            if (hasDetourWaypoint && blocker == detourWaypointObstacle &&
                IsMineableObstacleActive(blocker))
            {
                waypointDirection = detourWaypoint - currentPosition;
                waypointDirection.y = 0f;
                if (waypointDirection.sqrMagnitude > Mathf.Epsilon)
                {
                    waypointDirection.Normalize();
                    return true;
                }
            }

            if (!TryGetMineableObstacleBounds(blocker, out Bounds bounds))
            {
                return false;
            }

            forward.y = 0f;
            if (forward.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            forward.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
            float probeRadius = GetObstacleProbeRadius();
            float forwardExtent = Mathf.Abs(forward.x) * bounds.extents.x +
                                  Mathf.Abs(forward.z) * bounds.extents.z;
            float sideExtent = Mathf.Abs(side.x) * bounds.extents.x +
                               Mathf.Abs(side.z) * bounds.extents.z;
            float forwardPadding = probeRadius + npcData.StandSlotSpacingPadding;
            float sidePadding = probeRadius + npcData.StandSlotSpacingPadding * 2f;

            Vector3 nearCorner = bounds.center - forward * (forwardExtent + forwardPadding);
            nearCorner.y = currentPosition.y;
            Vector3 farCorner = bounds.center + forward * (forwardExtent + forwardPadding);
            farCorner.y = currentPosition.y;
            Vector3 positiveWaypoint = nearCorner + side * (sideExtent + sidePadding);
            Vector3 negativeWaypoint = nearCorner - side * (sideExtent + sidePadding);
            Vector3 positiveExit = farCorner + side * (sideExtent + sidePadding);
            Vector3 negativeExit = farCorner - side * (sideExtent + sidePadding);

            float positiveClearance = GetWaypointClearance(currentPosition, positiveWaypoint) +
                                      GetWaypointClearance(positiveWaypoint, positiveExit);
            float negativeClearance = GetWaypointClearance(currentPosition, negativeWaypoint) +
                                      GetWaypointClearance(negativeWaypoint, negativeExit);
            bool choosePositive;
            if (Mathf.Abs(positiveClearance - negativeClearance) <= 0.05f)
            {
                choosePositive = avoidanceSide >= 0f;
            }
            else
            {
                choosePositive = positiveClearance > negativeClearance;
            }

            detourWaypoint = choosePositive ? positiveWaypoint : negativeWaypoint;
            detourExitWaypoint = choosePositive ? positiveExit : negativeExit;
            avoidanceSide = choosePositive ? 1f : -1f;
            detourWaypointObstacle = blocker;
            hasDetourWaypoint = true;
            hasDetourExitWaypoint = true;
            avoidanceObstacle = blocker;
            detourDirectionUntil = Time.time + npcData.DetourDirectionHoldTime;

            waypointDirection = detourWaypoint - currentPosition;
            waypointDirection.y = 0f;
            if (waypointDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                ClearDetour();
                return false;
            }

            waypointDirection.Normalize();
            detourDirection = waypointDirection;
            ResetProgressTracking();
            return true;
        }

        private bool TryGetDetourWaypoint(Vector3 currentPosition, out Vector3 waypoint)
        {
            waypoint = default;
            if (!hasDetourWaypoint || !IsMineableObstacleActive(detourWaypointObstacle))
            {
                ClearDetour();
                return false;
            }

            Vector3 waypointOffset = detourWaypoint - currentPosition;
            waypointOffset.y = 0f;
            float reachedDistance = Mathf.Max(npcData.StoppingDistance,
                npcData.ColliderRadius * 0.35f);
            if (waypointOffset.sqrMagnitude <= reachedDistance * reachedDistance)
            {
                if (!hasDetourExitWaypoint)
                {
                    ClearDetour();
                    return false;
                }

                detourWaypoint = detourExitWaypoint;
                hasDetourExitWaypoint = false;
                waypointOffset = detourWaypoint - currentPosition;
                waypointOffset.y = 0f;
                if (waypointOffset.sqrMagnitude <= reachedDistance * reachedDistance)
                {
                    ClearDetour();
                    return false;
                }
            }

            waypoint = detourWaypoint;
            return true;
        }

        private float GetWaypointClearance(Vector3 currentPosition, Vector3 waypoint)
        {
            Vector3 offset = waypoint - currentPosition;
            offset.y = 0f;
            float distance = offset.magnitude;
            return distance <= Mathf.Epsilon
                ? 0f
                : GetOreClearanceFrom(currentPosition, offset / distance, distance);
        }

        private void EvaluateDetourCandidate(Vector3 forward, float angle,
            ref Vector3 bestDirection, ref float bestClearance)
        {
            Vector3 candidate = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            candidate.y = 0f;
            candidate.Normalize();
            float clearance = GetOreClearance(candidate, npcData.DetourProbeDistance);
            if (clearance > bestClearance + 0.01f)
            {
                bestDirection = candidate;
                bestClearance = clearance;
            }
        }

        private float GetOreClearance(Vector3 direction, float distance)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return 0f;
            }

            return GetOreClearanceFrom(body.position, direction, distance);
        }

        private float GetOreClearanceFrom(Vector3 worldPosition, Vector3 direction,
            float distance)
        {
            Vector3 origin = worldPosition + Vector3.up * npcData.ColliderRadius;
            int hitCount = Physics.SphereCastNonAlloc(origin, GetObstacleProbeRadius(),
                direction.normalized, obstacleHits, distance, npcData.CollisionLayers,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = distance;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = obstacleHits[index];
                Ore ore = hit.collider != null ? hit.collider.GetComponentInParent<Ore>() : null;
                MiningChest chest = hit.collider != null
                    ? hit.collider.GetComponentInParent<MiningChest>() : null;
                if ((ore == null || ore == targetOre || ore.IsDepleted) &&
                    (chest == null || chest == targetChest || !chest.CanMine)) continue;

                nearestDistance = Mathf.Min(nearestDistance, hit.distance);
            }

            return nearestDistance;
        }

        private float GetObstacleProbeRadius()
        {
            // The route probe must be at least as wide as the physical capsule. The authored
            // data currently uses a 0.32 probe for a 0.5-radius NPC, which lets the query report
            // a clear route that the Rigidbody cannot physically fit through.
            return Mathf.Max(npcData.ObstacleProbeRadius,
                npcData.ColliderRadius + npcData.StandSlotSpacingPadding);
        }

        /// <summary>
        /// Requests (or reuses) a global shortest-path route to standPosition via
        /// MiningNavigation (NavMesh first, MiningNavGrid A* as fallback). Throttled by
        /// repathInterval/repathTargetMoveThreshold so it doesn't re-query every frame. If no
        /// backend can produce a route, currentPath is cleared and FixedUpdate falls straight back
        /// to the old direct-line reactive steering for standPosition.
        /// </summary>
        private void UpdateGlobalPath(Vector3 currentPosition, Vector3 standPosition)
        {
            if (!useGlobalPathfinding)
            {
                inFinalApproach = false;
                currentPathSource = MiningPathSource.None;
                if (currentPath.Count > 0)
                {
                    currentPath.Clear();
                }

                return;
            }

            if (CanUseDirectFinalApproach(currentPosition, standPosition))
            {
                inFinalApproach = true;
                if (currentPath.Count > 0)
                {
                    currentPath.Clear();
                    pathWaypointIndex = 0;
                }

                return;
            }

            // Left the final-approach radius (pushed out, or the target moved): allow routing
            // again immediately instead of waiting out the repath throttle.
            if (inFinalApproach)
            {
                inFinalApproach = false;
                nextRepathTime = 0f;
            }

            bool targetMoved = !hasPathTarget || (standPosition - lastPathTarget).sqrMagnitude >
                repathTargetMoveThreshold * repathTargetMoveThreshold;
            bool pathExhausted = currentPath.Count == 0 || pathWaypointIndex >= currentPath.Count;

            if (Time.time < nextRepathTime && !targetMoved && !pathExhausted)
            {
                return;
            }

            nextRepathTime = Time.time + repathInterval;
            lastPathTarget = standPosition;
            hasPathTarget = true;

            if (MiningNavigation.TryFindPath(currentPosition, standPosition, pathRequestBuffer,
                    out MiningPathSource pathSource, UnityEngine.AI.NavMesh.AllAreas,
                    navMeshSampleRadius))
            {
                currentPath.Clear();
                currentPath.AddRange(pathRequestBuffer);
                pathWaypointIndex = 0;
                currentPathSource = pathSource;
            }
            else
            {
                currentPath.Clear();
                currentPathSource = MiningPathSource.None;
            }
        }

        private float GetMoveSpeedMultiplier()
        {
            float multiplier = progressionSystem != null
                ? progressionSystem.CurrentMoveSpeedMultiplier
                : oreSpawner != null && oreSpawner.UpgradeSystem != null
                    ? oreSpawner.UpgradeSystem.GetMultiplier(MiningUpgradeType.NpcMoveSpeed)
                    : 1f;
            return Mathf.Max(0.01f, multiplier);
        }

        private bool CanUseDirectFinalApproach(Vector3 currentPosition, Vector3 standPosition)
        {
            Vector3 toStand = standPosition - currentPosition;
            toStand.y = 0f;
            float distance = toStand.magnitude;
            if (distance > finalApproachDistance)
            {
                return false;
            }

            if (distance <= npcData.StoppingDistance)
            {
                return true;
            }

            return !TryGetBlockingMineable(toStand / distance, distance, out _, out _);
        }

        /// <summary>
        /// Limits velocity before the next route corner. A NavMeshAgent normally provides this
        /// steering internally; this NPC deliberately uses a Rigidbody, so its route follower
        /// must perform the equivalent slowdown itself to avoid overshooting a left/right turn.
        /// </summary>
        private float GetCornerSpeedLimit(Vector3 currentPosition, float maximumSpeed,
            float brakingAcceleration)
        {
            if (pathWaypointIndex >= currentPath.Count - 1)
            {
                return maximumSpeed;
            }

            Vector3 corner = currentPath[pathWaypointIndex];
            Vector3 approach = corner - currentPosition;
            approach.y = 0f;
            float cornerDistance = approach.magnitude;
            if (cornerDistance <= Mathf.Epsilon)
            {
                return maximumSpeed;
            }

            Vector3 exit = currentPath[pathWaypointIndex + 1] - corner;
            exit.y = 0f;
            if (exit.sqrMagnitude <= Mathf.Epsilon)
            {
                return maximumSpeed;
            }

            float turnAmount = Mathf.InverseLerp(15f, 150f,
                Vector3.Angle(approach, exit));
            if (turnAmount <= Mathf.Epsilon)
            {
                return maximumSpeed;
            }

            // Keep enough motion to round gentle corners, while tight turns slow substantially.
            float turnSpeed = Mathf.Lerp(maximumSpeed, maximumSpeed * 0.2f, turnAmount);
            float turnRadius = Mathf.Max(npcData.ColliderRadius, npcData.StoppingDistance);
            return Mathf.Sqrt(turnSpeed * turnSpeed + 2f * brakingAcceleration *
                Mathf.Max(0f, cornerDistance - turnRadius));
        }

        /// <summary>
        /// Returns the point along the global path to steer toward. Advances past every waypoint
        /// already reached, then aims pathLookAheadDistance further along the polyline rather than
        /// exactly at the next corner - steering at the corner itself makes the miner hug it and
        /// then snap onto the next heading, which is the zig-zag. Falls back to fallbackTarget
        /// (the direct stand position) when there is no active path or the route is used up.
        /// </summary>
        private Vector3 GetNavigationTarget(Vector3 currentPosition, Vector3 fallbackTarget)
        {
            if (currentPath.Count == 0)
            {
                return fallbackTarget;
            }

            float reach = Mathf.Max(npcData.StoppingDistance, npcData.ColliderRadius * 0.5f);
            // Note this advances past the LAST waypoint too. The previous version stopped at
            // Count - 1, so an arrived miner kept steering at a corner it was already standing
            // on: zero movement, and the stuck timer was the only way out.
            while (pathWaypointIndex < currentPath.Count)
            {
                Vector3 offset = currentPath[pathWaypointIndex] - currentPosition;
                offset.y = 0f;
                if (offset.sqrMagnitude > reach * reach)
                {
                    break;
                }

                pathWaypointIndex++;
            }

            if (pathWaypointIndex >= currentPath.Count)
            {
                // Whole route consumed - whatever is left is the final approach.
                currentPath.Clear();
                pathWaypointIndex = 0;
                return fallbackTarget;
            }

            return GetLookAheadPoint(currentPosition, fallbackTarget);
        }

        /// <summary>
        /// Walks forward along the remaining route accumulating distance, and returns the point
        /// pathLookAheadDistance along it (interpolated inside whichever segment that lands in).
        /// Running off the end of the route returns the real destination, so the miner aims at
        /// where it is actually going rather than at the last corner.
        /// </summary>
        private Vector3 GetLookAheadPoint(Vector3 currentPosition, Vector3 fallbackTarget)
        {
            float remaining = pathLookAheadDistance;
            Vector3 segmentStart = currentPosition;
            for (int index = pathWaypointIndex; index < currentPath.Count; index++)
            {
                Vector3 segmentEnd = currentPath[index];
                Vector3 segment = segmentEnd - segmentStart;
                segment.y = 0f;
                float segmentLength = segment.magnitude;
                if (segmentLength >= remaining)
                {
                    return segmentLength <= Mathf.Epsilon
                        ? segmentEnd
                        : segmentStart + segment * (remaining / segmentLength);
                }

                remaining -= segmentLength;
                segmentStart = segmentEnd;
            }

            return fallbackTarget;
        }

        private void ResetGlobalPath()
        {
            currentPath.Clear();
            pathWaypointIndex = 0;
            hasPathTarget = false;
            nextRepathTime = 0f;
            inFinalApproach = false;
            currentPathSource = MiningPathSource.None;
        }

        private void OnDrawGizmos()
        {
            if (!drawPathGizmos)
            {
                return;
            }

            Vector3 currentPosition = body != null ? body.position : transform.position;
            Vector3 routePosition = currentPosition + Vector3.up * 0.08f;
            Color routeColor = inFinalApproach
                ? Color.green
                : currentPathSource == MiningPathSource.NavMesh
                    ? Color.cyan
                    : currentPathSource == MiningPathSource.Grid ? Color.yellow : Color.gray;

            Gizmos.color = routeColor;
            Gizmos.DrawSphere(routePosition, 0.08f);

            if (currentPath.Count > 0)
            {
                for (int index = 0; index < currentPath.Count; index++)
                {
                    Vector3 waypoint = currentPath[index] + Vector3.up * 0.08f;
                    Gizmos.color = index < pathWaypointIndex
                        ? new Color(routeColor.r, routeColor.g, routeColor.b, 0.28f)
                        : routeColor;
                    Gizmos.DrawLine(routePosition, waypoint);
                    Gizmos.DrawSphere(waypoint, index == pathWaypointIndex ? 0.16f : 0.09f);
                    routePosition = waypoint;
                }

                if (pathWaypointIndex < currentPath.Count)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(currentPath[pathWaypointIndex] + Vector3.up * 0.08f,
                        0.22f);
                }
            }
            else if (hasMoveTarget)
            {
                // Gray direct line: no global route is currently available, so this NPC is using
                // reactive steering. It makes an unbaked/disconnected navigation area obvious.
                Gizmos.color = inFinalApproach ? Color.green : Color.gray;
                Gizmos.DrawLine(routePosition, desiredMoveTarget + Vector3.up * 0.08f);
            }

            if (hasMoveTarget)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(desiredMoveTarget + Vector3.up * 0.08f,
                    Vector3.one * 0.22f);
            }

#if UNITY_EDITOR
            string sourceLabel = inFinalApproach
                ? $"{currentPathSource} final approach"
                : currentPathSource == MiningPathSource.None
                ? "Reactive fallback"
                : currentPathSource.ToString();
            Handles.Label(currentPosition + Vector3.up * 1.25f,
                $"Path: {sourceLabel}\nWaypoint: {pathWaypointIndex}/{currentPath.Count}");
#endif
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
            if (body == null)
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
