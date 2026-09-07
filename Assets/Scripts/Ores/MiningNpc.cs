using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Reserves a compatible ore, steers around dynamic blockers, then mines it.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))]
    public sealed class MiningNpc : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private OreSpawner oreSpawner;
        [SerializeField] private NpcData npcData;
        [SerializeField] private Transform toolPivot;

        private readonly RaycastHit[] obstacleHits = new RaycastHit[16];
        private readonly Collider[] separationHits = new Collider[24];
        private Ore targetOre;
        private Ore ignoredOre;
        private Ore avoidanceOre;
        private Rigidbody body;
        private float nextHitTime;
        private float nextTargetRefreshTime;
        private float ignoredOreUntil;
        private float lastProgressTime;
        private float avoidanceSide = 1f;
        private int reservedSlot = -1;
        private Quaternion toolRestRotation;
        private Vector3 desiredMoveTarget;
        private Vector3 desiredFacingDirection;
        private Vector3 lastProgressPosition;
        private bool hasMoveTarget;
        private bool isMining;

        public void ConfigureTool(Transform targetToolPivot)
        {
            toolPivot = targetToolPivot;
        }

        public void Initialize(OreSpawner targetSpawner, NpcData targetNpcData)
        {
            ReleaseTarget();
            oreSpawner = targetSpawner;
            npcData = targetNpcData;
            nextTargetRefreshTime = 0f;
            ConfigurePhysics();
            ResetProgressTracking();
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (toolPivot != null)
            {
                toolRestRotation = toolPivot.localRotation;
            }

            ConfigurePhysics();
            ResetProgressTracking();
        }

        private void OnDisable()
        {
            ReleaseTarget();
            hasMoveTarget = false;
            desiredFacingDirection = Vector3.zero;
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

            if (!IsTargetValid())
            {
                ReleaseTarget();
            }

            if (Time.time >= nextTargetRefreshTime)
            {
                ReevaluateTarget();
                nextTargetRefreshTime = Time.time + npcData.TargetRefreshInterval;
            }

            if (!IsTargetValid())
            {
                return;
            }

            Vector3 currentPosition = body != null ? body.position : transform.position;
            Vector3 standPosition = GetReservedStandPosition();
            Vector3 standOffset = standPosition - currentPosition;
            standOffset.y = 0f;
            Vector3 oreOffset = targetOre.transform.position - currentPosition;
            oreOffset.y = 0f;
            desiredFacingDirection = oreOffset;

            float movementThreshold = isMining
                ? npcData.ResumeMovingDistance
                : npcData.StoppingDistance;
            if (standOffset.sqrMagnitude > movementThreshold * movementThreshold)
            {
                isMining = false;
                desiredMoveTarget = standPosition;
                hasMoveTarget = true;
                TrackMovementProgress(currentPosition);
                return;
            }

            ResetProgressTracking();
            if (targetOre.SqrDistanceToSurface(currentPosition) >
                npcData.MiningRange * npcData.MiningRange)
            {
                isMining = false;
                return;
            }

            isMining = true;
            if (Time.time < nextHitTime)
            {
                return;
            }

            nextHitTime = Time.time + npcData.SecondsPerHit;
            targetOre.ApplyDamage(npcData.DamagePerHit);
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
                ApplyHorizontalVelocity(Vector3.zero, npcData.BrakingAcceleration);
                RotateTowards(desiredFacingDirection);
                return;
            }

            Vector3 movementDirection = movementOffset.normalized;
            float probeDistance = Mathf.Min(npcData.ObstacleProbeDistance, movementOffset.magnitude);
            if (TryGetBlockingOre(movementDirection, probeDistance, out Ore blockingOre,
                out Vector3 blockingPoint))
            {
                if (CanMine(blockingOre) && TrySwitchTarget(blockingOre))
                {
                    ApplyHorizontalVelocity(Vector3.zero, npcData.BrakingAcceleration);
                    return;
                }

                movementDirection = CalculateObstacleAvoidance(
                    movementDirection, currentPosition, blockingOre, blockingPoint);
            }
            else
            {
                avoidanceOre = null;
            }

            Vector3 separation = CalculateNpcSeparation(currentPosition);
            movementDirection = (movementDirection + separation * npcData.NpcSeparationStrength).normalized;

            float speedMultiplier = oreSpawner != null && oreSpawner.UpgradeSystem != null
                ? oreSpawner.UpgradeSystem.GetMultiplier(MiningUpgradeType.NpcMoveSpeed)
                : 1f;
            Vector3 desiredVelocity = movementDirection * (npcData.MoveSpeed * speedMultiplier);
            ApplyHorizontalVelocity(desiredVelocity, npcData.MovementAcceleration);
            RotateTowards(movementDirection);
        }

        private void LateUpdate()
        {
            if (toolPivot == null || npcData == null)
            {
                return;
            }

            Quaternion targetRotation = toolRestRotation;
            if (isMining)
            {
                float swing = Mathf.Sin(Time.time * npcData.ToolSwingSpeed) * npcData.ToolSwingAngle;
                targetRotation *= Quaternion.Euler(0f, 0f, swing);
            }

            toolPivot.localRotation = Quaternion.Slerp(
                toolPivot.localRotation, targetRotation, npcData.ToolReturnSpeed * Time.deltaTime);
        }

        private void ReevaluateTarget()
        {
            Vector3 currentPosition = body != null ? body.position : transform.position;
            if (targetOre == null)
            {
                TryAcquireTarget(currentPosition);
                return;
            }

            Ore temporarilyIgnoredOre = Time.time < ignoredOreUntil ? ignoredOre : null;
            if (!oreSpawner.TryReserveClosestOre(this, currentPosition, npcData.MiningPower,
                targetOre, temporarilyIgnoredOre, out Ore closerOre, out int closerSlot))
            {
                return;
            }

            float currentDistance = Mathf.Sqrt(targetOre.SqrDistanceToSurface(currentPosition));
            float closerDistance = Mathf.Sqrt(closerOre.SqrDistanceToSurface(currentPosition));
            if (closerDistance + npcData.TargetSwitchDistanceAdvantage >= currentDistance)
            {
                closerOre.ReleaseMiner(this);
                return;
            }

            SetTarget(closerOre, closerSlot);
        }

        private void TryAcquireTarget(Vector3 currentPosition)
        {
            Ore excludedOre = Time.time < ignoredOreUntil ? ignoredOre : null;
            if (oreSpawner.TryReserveClosestOre(this, currentPosition, npcData.MiningPower,
                excludedOre, out Ore ore, out int slotIndex))
            {
                SetTarget(ore, slotIndex);
            }
        }

        private bool TrySwitchTarget(Ore ore)
        {
            if (ore == null || ore == targetOre)
            {
                return ore == targetOre;
            }

            if (!oreSpawner.TryReserveOre(this, ore, npcData.MiningPower, out int slotIndex))
            {
                return false;
            }

            SetTarget(ore, slotIndex);
            return true;
        }

        private void SetTarget(Ore ore, int slotIndex)
        {
            Ore previousOre = targetOre;
            targetOre = ore;
            reservedSlot = slotIndex;
            if (previousOre != null && previousOre != ore)
            {
                previousOre.ReleaseMiner(this);
            }

            isMining = false;
            ResetProgressTracking();
        }

        private Vector3 GetReservedStandPosition()
        {
            return targetOre.GetMiningStandPosition(
                reservedSlot, npcData.ColliderRadius, npcData.StandSlotSpacingPadding);
        }

        private bool IsTargetValid()
        {
            return CanMine(targetOre);
        }

        private bool CanMine(Ore ore)
        {
            return npcData != null && ore != null && ore.isActiveAndEnabled &&
                   !ore.IsDepleted && ore.Data != null &&
                   ore.Data.MiningPowerRequired <= npcData.MiningPower;
        }

        private void ReleaseTarget()
        {
            if (targetOre != null)
            {
                targetOre.ReleaseMiner(this);
            }

            targetOre = null;
            reservedSlot = -1;
            hasMoveTarget = false;
            isMining = false;
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

            ignoredOre = targetOre;
            ignoredOreUntil = Time.time + npcData.IgnoredTargetDuration;
            ReleaseTarget();
            nextTargetRefreshTime = 0f;
            ResetProgressTracking();
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
                    sideDot = ((GetEntityId() ^ blockingOre.GetEntityId()) & 1) == 0 ? -1f : 1f;
                }

                avoidanceSide = sideDot > 0f ? -1f : 1f;
            }

            return (forward + side * avoidanceSide * npcData.ObstacleAvoidanceStrength).normalized;
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
                    away = transform.GetEntityId() < otherNpc.transform.GetEntityId()
                        ? transform.right
                        : -transform.right;
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

            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
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
    }
}
