using System.Collections.Generic;
using UnityEngine;

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
        [SerializeField] private Transform toolPivot;

        [Header("Animator")]
        [Tooltip("Auto-resolved from a child object if left empty (GetComponentInChildren).")]
        [SerializeField] private Animator animator;
        [Tooltip("Bool parameter set on the Animator while the NPC is actively mining (swinging).")]
        [SerializeField] private string miningAnimatorBoolParameter = "IsMining";
        [Tooltip("Name of the Animator state that plays the mining swing (the 'Mine' box in the controller graph). Once fully inside this state (after any Idle->Mine blend finishes), its playback SPEED is adjusted every frame so exactly one loop of the clip takes the same time as one hit (SecondsPerHit) - so the swing and the mining SFX stay roughly in step without ever forcing the pose/time directly (which can distort the rig).")]
        [SerializeField] private string mineAnimatorStateName = "Mine";

        [Header("Ground Clamp (floating-feet safety net)")]
        [Tooltip("Assign the visual model's root (the object holding the Animator/skeleton) to enable the runtime fix below. Leave empty to disable.")]
        [SerializeField] private Transform visualModelRoot;
        [Tooltip("Left foot bone. With Right Foot Bone below, the model is pulled down each frame until the lowest foot touches the ground - this compensates for an animation clip that is not baked correctly (Root Transform Position Y / Bake Into Pose), which is the real fix and should still be done on the clip.")]
        [SerializeField] private Transform leftFootBone;
        [SerializeField] private Transform rightFootBone;
        [Tooltip("How far a foot may sit above or below the NPC's true local ground level (bottom of the Capsule Collider, not local Y = 0) before it is treated as floating/clipping and corrected.")]
        [SerializeField, Min(0f)] private float groundClampEpsilon = 0.01f;

        private readonly RaycastHit[] obstacleHits = new RaycastHit[32];
        private readonly Collider[] separationHits = new Collider[24];
        private Ore targetOre;
        private LuckyBlock targetLuckyBlock;
        private Ore ignoredOre;
        private LuckyBlock ignoredLuckyBlock;
        private Ore avoidanceOre;
        private LuckyBlockDropSystem luckyBlockSystem;
        private Rigidbody body;
        private float nextTargetRefreshTime;
        private float nextTargetSwitchTime;
        private float ignoredOreUntil;
        private float ignoredLuckyBlockUntil;
        private float lastProgressTime;
        private float avoidanceSide = 1f;
        private float detourDirectionUntil;
        private int reservedSlot = -1;
        private Vector3 desiredMoveTarget;
        private Vector3 desiredFacingDirection;
        private Vector3 lastProgressPosition;
        private Vector3 smoothedSeparation;
        private Vector3 detourDirection;
        private bool hasMoveTarget;
        private bool isMining;
        private CapsuleCollider capsule;
        private bool hasMiningBoolParameter;
        private float visualModelBaseLocalY;
        private int mineStateHash;

        public void ConfigureTool(Transform targetToolPivot)
        {
            toolPivot = targetToolPivot;
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
            RegisterNpcCollisionPairing();
            ResetProgressTracking();
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

            if (animator != null)
            {
                // Physics/script (this component) drives movement, not the animation clip -
                // keeping this off avoids the Animator fighting the Rigidbody every frame.
                animator.applyRootMotion = false;
                hasMiningBoolParameter = HasParameter(
                    animator, miningAnimatorBoolParameter, AnimatorControllerParameterType.Bool);
            }

            mineStateHash = Animator.StringToHash(mineAnimatorStateName);

            if (visualModelRoot != null)
            {
                visualModelBaseLocalY = visualModelRoot.localPosition.y;
            }

            ConfigurePhysics();
            ResetProgressTracking();
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
            smoothedSeparation = Vector3.zero;
            detourDirection = Vector3.zero;
            detourDirectionUntil = 0f;
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

            if (!IsTargetValid())
            {
                ReleaseTarget();
            }

            if (targetOre == null && targetLuckyBlock == null &&
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

            Vector3 standPosition = GetReservedStandPosition();
            Vector3 oreOffset = GetTargetPosition() - currentPosition;
            oreOffset.y = 0f;
            desiredFacingDirection = oreOffset;

            bool isWithinMiningRange = SqrDistanceToTargetSurface(currentPosition) <=
                                       npcData.MiningRange * npcData.MiningRange;

            if (!isWithinMiningRange)
            {
                SetMiningAnimationState(false);
                desiredMoveTarget = standPosition;
                hasMoveTarget = true;
                TrackMovementProgress(currentPosition);
                return;
            }

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
                return;
            }

            Vector3 currentPosition = body.position;
            Vector3 movementOffset = desiredMoveTarget - currentPosition;
            movementOffset.y = 0f;
            if (!hasMoveTarget || movementOffset.sqrMagnitude <=
                npcData.StoppingDistance * npcData.StoppingDistance)
            {
                smoothedSeparation = Vector3.MoveTowards(smoothedSeparation, Vector3.zero,
                    npcData.NpcSeparationResponsiveness * Time.fixedDeltaTime);
                ApplyHorizontalVelocity(Vector3.zero, npcData.BrakingAcceleration);
                RotateTowards(desiredFacingDirection);
                return;
            }

            Vector3 movementDirection = movementOffset.normalized;
            float probeDistance = Mathf.Min(npcData.ObstacleProbeDistance, movementOffset.magnitude);
            if (TryGetBlockingOre(movementDirection, probeDistance, out Ore blockingOre,
                out Vector3 blockingPoint))
            {
                if (Time.time >= nextTargetSwitchTime && blockingOre != ignoredOre &&
                    CanMine(blockingOre) && IsBlockingOreCloser(blockingOre, currentPosition) &&
                    TrySwitchTarget(blockingOre))
                {
                    RotateTowards(movementDirection);
                    return;
                }

                movementDirection = ResolveBlockedPath(
                    movementDirection, currentPosition, blockingOre, blockingPoint);
            }
            else
            {
                avoidanceOre = null;
                if (Time.time < detourDirectionUntil && detourDirection.sqrMagnitude > Mathf.Epsilon)
                {
                    movementDirection = detourDirection;
                }
                else
                {
                    ClearDetour();
                }
            }

            Vector3 separation = CalculateNpcSeparation(currentPosition);
            smoothedSeparation = Vector3.MoveTowards(smoothedSeparation, separation,
                npcData.NpcSeparationResponsiveness * Time.fixedDeltaTime);
            movementDirection = (movementDirection +
                smoothedSeparation * npcData.NpcSeparationStrength).normalized;

            float speedMultiplier = progressionSystem != null
                ? progressionSystem.CurrentMoveSpeedMultiplier
                : oreSpawner != null && oreSpawner.UpgradeSystem != null
                    ? oreSpawner.UpgradeSystem.GetMultiplier(MiningUpgradeType.NpcMoveSpeed)
                    : 1f;
            Vector3 desiredVelocity = movementDirection * (npcData.MoveSpeed * speedMultiplier);
            ApplyHorizontalVelocity(desiredVelocity, npcData.MovementAcceleration);
            RotateTowards(movementDirection);
        }

        private void LateUpdate()
        {
            if (animator != null && hasMiningBoolParameter)
            {
                if (isMining && npcData != null)
                {
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
                        animator.speed = stateInfo.length / Mathf.Max(0.0001f, npcData.SecondsPerHit);
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
                return;
            }

            isMining = mining;
            if (animator != null && hasMiningBoolParameter)
            {
                animator.SetBool(miningAnimatorBoolParameter, mining);
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
            bool foundOre = oreSpawner.TryReserveClosestOre(this, currentPosition,
                CurrentMiningPower, excludedOre, out Ore ore, out int oreSlotIndex);
            LuckyBlock block = null;
            int blockSlotIndex = -1;
            bool foundBlock = luckyBlockSystem != null &&
                              luckyBlockSystem.TryReserveClosestBlock(this, currentPosition,
                                  CurrentMiningPower, excludedBlock, out block,
                                  out blockSlotIndex);

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

        private void SetTarget(Ore ore, int slotIndex)
        {
            Ore previousOre = targetOre;
            LuckyBlock previousBlock = targetLuckyBlock;
            targetOre = ore;
            targetLuckyBlock = null;
            reservedSlot = slotIndex;
            previousBlock?.ReleaseMiner(this);
            if (previousOre != null && previousOre != ore)
            {
                previousOre.ReleaseMiner(this);
                ignoredOre = previousOre;
                ignoredOreUntil = Time.time + npcData.IgnoredTargetDuration;
            }

            SetMiningAnimationState(false);
            nextTargetSwitchTime = Time.time + npcData.TargetSwitchCooldown;
            ClearDetour();
            ResetProgressTracking();
        }

        private void SetTarget(LuckyBlock block, int slotIndex)
        {
            Ore previousOre = targetOre;
            LuckyBlock previousBlock = targetLuckyBlock;
            targetOre = null;
            targetLuckyBlock = block;
            reservedSlot = slotIndex;
            previousOre?.ReleaseMiner(this);
            if (previousBlock != null && previousBlock != block)
            {
                previousBlock.ReleaseMiner(this);
                ignoredLuckyBlock = previousBlock;
                ignoredLuckyBlockUntil = Time.time + npcData.IgnoredTargetDuration;
            }

            SetMiningAnimationState(false);
            nextTargetSwitchTime = Time.time + npcData.TargetSwitchCooldown;
            ClearDetour();
            ResetProgressTracking();
        }

        private Vector3 GetReservedStandPosition()
        {
            if (targetLuckyBlock != null)
            {
                return targetLuckyBlock.GetMiningStandPosition(
                    reservedSlot, npcData.ColliderRadius, npcData.StandSlotSpacingPadding);
            }

            return targetOre != null
                ? targetOre.GetMiningStandPosition(
                    reservedSlot, npcData.ColliderRadius, npcData.StandSlotSpacingPadding)
                : transform.position;
        }

        private bool IsTargetValid()
        {
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

        private Vector3 GetTargetPosition()
        {
            return targetLuckyBlock != null
                ? targetLuckyBlock.transform.position
                : targetOre != null ? targetOre.transform.position : transform.position;
        }

        private float SqrDistanceToTargetSurface(Vector3 currentPosition)
        {
            return targetLuckyBlock != null
                ? targetLuckyBlock.SqrDistanceToSurface(currentPosition)
                : targetOre != null
                    ? targetOre.SqrDistanceToSurface(currentPosition)
                    : float.PositiveInfinity;
        }

        private void ApplyDamageToTarget()
        {
            if (targetLuckyBlock != null)
            {
                float damage = progressionSystem != null
                    ? progressionSystem.CurrentDamagePerHit
                    : npcData.DamagePerHit;
                targetLuckyBlock.ApplyNpcDamage(damage);
            }
            else
            {
                float damage = progressionSystem != null
                    ? progressionSystem.CurrentDamagePerHit
                    : npcData.DamagePerHit;
                targetOre?.ApplyNpcDamage(damage);
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
            if (targetLuckyBlock != null)
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

            targetOre = null;
            targetLuckyBlock = null;
            reservedSlot = -1;
            hasMoveTarget = false;
            SetMiningAnimationState(false);
            ClearDetour();
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

            if (targetLuckyBlock != null)
            {
                ignoredLuckyBlock = targetLuckyBlock;
                ignoredLuckyBlockUntil = Time.time + npcData.IgnoredTargetDuration;
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

        private bool TryGetBlockingOre(Vector3 direction, float distance, out Ore blockingOre,
            out Vector3 blockingPoint)
        {
            blockingOre = null;
            blockingPoint = Vector3.zero;
            Vector3 origin = body.position + Vector3.up * npcData.ColliderRadius;
            int hitCount = Physics.SphereCastNonAlloc(origin, npcData.ObstacleProbeRadius,
                direction, obstacleHits, distance, npcData.CollisionLayers,
                QueryTriggerInteraction.Ignore);
            float closestDistance = float.PositiveInfinity;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = obstacleHits[index];
                Ore ore = hit.collider != null ? hit.collider.GetComponentInParent<Ore>() : null;
                if (ore == null || ore == targetOre || hit.distance >= closestDistance)
                {
                    continue;
                }

                blockingOre = ore;
                blockingPoint = hit.point;
                closestDistance = hit.distance;
            }

            return blockingOre != null;
        }

        private Vector3 CalculateObstacleAvoidance(Vector3 forward, Vector3 currentPosition,
            Ore blockingOre, Vector3 blockingPoint)
        {
            Vector3 toBlocker = blockingPoint - currentPosition;
            toBlocker.y = 0f;
            Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
            if (blockingOre != avoidanceOre)
            {
                avoidanceOre = blockingOre;
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
            Ore blockingOre, Vector3 blockingPoint)
        {
            if (Time.time < detourDirectionUntil && detourDirection.sqrMagnitude > Mathf.Epsilon &&
                GetOreClearance(detourDirection, npcData.DetourProbeDistance) >=
                npcData.DetourMinimumClearance * 0.5f)
            {
                return detourDirection;
            }

            Vector3 normalAvoidance = CalculateObstacleAvoidance(
                forward, currentPosition, blockingOre, blockingPoint);
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
                if (reverseClearance >= npcData.DetourMinimumClearance * 0.5f)
                {
                    bestDirection = reverseDirection;
                }
            }

            detourDirection = bestDirection.normalized;
            detourDirectionUntil = Time.time + npcData.DetourDirectionHoldTime;
            ResetProgressTracking();
            return detourDirection;
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

            Vector3 origin = body.position + Vector3.up * npcData.ColliderRadius;
            int hitCount = Physics.SphereCastNonAlloc(origin, npcData.ObstacleProbeRadius,
                direction.normalized, obstacleHits, distance, npcData.CollisionLayers,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = distance;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = obstacleHits[index];
                Ore ore = hit.collider != null ? hit.collider.GetComponentInParent<Ore>() : null;
                if (ore == null || ore == targetOre)
                {
                    continue;
                }

                nearestDistance = Mathf.Min(nearestDistance, hit.distance);
            }

            return nearestDistance;
        }

        private void ClearDetour()
        {
            detourDirection = Vector3.zero;
            detourDirectionUntil = 0f;
        }

        private Vector3 CalculateNpcSeparation(Vector3 currentPosition)
        {
            int overlapCount = Physics.OverlapSphereNonAlloc(currentPosition,
                npcData.NpcSeparationRadius, separationHits, npcData.CollisionLayers,
                QueryTriggerInteraction.Ignore);
            Vector3 separation = Vector3.zero;
            for (int index = 0; index < overlapCount; index++)
            {
                Collider overlap = separationHits[index];
                MiningNpc otherNpc = overlap != null ? overlap.GetComponentInParent<MiningNpc>() : null;
                if (otherNpc == null || otherNpc == this)
                {
                    continue;
                }

                Vector3 away = currentPosition - otherNpc.transform.position;
                away.y = 0f;
                float distance = away.magnitude;
                if (distance <= Mathf.Epsilon)
                {
                    int separationOrder = transform.GetSiblingIndex()
                        .CompareTo(otherNpc.transform.GetSiblingIndex());
                    if (separationOrder == 0)
                    {
                        separationOrder = string.CompareOrdinal(name, otherNpc.name);
                    }

                    away = separationOrder <= 0 ? Vector3.right : Vector3.left;
                    distance = npcData.ColliderRadius;
                }

                float weight = 1f - Mathf.Clamp01(distance / npcData.NpcSeparationRadius);
                separation += away.normalized * weight;
            }

            return Vector3.ClampMagnitude(separation, 1f);
        }

        private void ApplyHorizontalVelocity(Vector3 desiredHorizontalVelocity, float acceleration)
        {
            Vector3 currentVelocity = body.linearVelocity;
            Vector3 currentHorizontalVelocity = new(currentVelocity.x, 0f, currentVelocity.z);
            Vector3 nextHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity,
                desiredHorizontalVelocity, acceleration * Time.fixedDeltaTime);
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
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
        }

        private void RegisterNpcCollisionPairing()
        {
            capsule ??= GetComponent<CapsuleCollider>();
            if (capsule == null || npcData == null || !npcData.IgnoreNpcPhysicalCollisions)
            {
                return;
            }

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

                other.capsule ??= other.GetComponent<CapsuleCollider>();
                if (other.capsule != null)
                {
                    Physics.IgnoreCollision(capsule, other.capsule, true);
                }
            }

            if (!ActiveNpcs.Contains(this))
            {
                ActiveNpcs.Add(this);
            }
        }
    }
}