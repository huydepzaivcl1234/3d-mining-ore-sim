using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Reserves an available ore slot, moves there with physics, then mines it.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))]
    public sealed class MiningNpc : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private OreSpawner oreSpawner;
        [SerializeField] private NpcData npcData;
        [SerializeField] private Transform toolPivot;

        private Ore targetOre;
        private Rigidbody body;
        private float nextHitTime;
        private float nextTargetRefreshTime;
        private int reservedSlot = -1;
        private Quaternion toolRestRotation;
        private Vector3 desiredMoveTarget;
        private Vector3 desiredFacingDirection;
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
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (toolPivot != null)
            {
                toolRestRotation = toolPivot.localRotation;
            }
            ConfigurePhysics();
        }

        private void OnDisable()
        {
            ReleaseTarget();
            hasMoveTarget = false;
            desiredFacingDirection = Vector3.zero;
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

            if (!IsTargetValid())
            {
                ReleaseTarget();
            }

            if (targetOre == null && Time.time >= nextTargetRefreshTime)
            {
                TryAcquireTarget();
                nextTargetRefreshTime = Time.time + npcData.TargetRefreshInterval;
            }

            if (!IsTargetValid())
            {
                return;
            }

            Vector3 standPosition = GetReservedStandPosition();
            Vector3 currentPosition = body != null ? body.position : transform.position;
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
                return;
            }

            if (oreOffset.sqrMagnitude > npcData.MiningRange * npcData.MiningRange)
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
            if (npcData == null)
            {
                return;
            }

            float speedMultiplier = oreSpawner != null && oreSpawner.UpgradeSystem != null
                ? oreSpawner.UpgradeSystem.GetMultiplier(MiningUpgradeType.NpcMoveSpeed)
                : 1f;
            Vector3 flatTarget = desiredMoveTarget;
            flatTarget.y = body != null ? body.position.y : transform.position.y;
            Vector3 movementDirection = hasMoveTarget
                ? flatTarget - (body != null ? body.position : transform.position)
                : desiredFacingDirection;
            movementDirection.y = 0f;

            if (body != null)
            {
                if (hasMoveTarget)
                {
                    Vector3 nextPosition = Vector3.MoveTowards(body.position, flatTarget,
                        npcData.MoveSpeed * speedMultiplier * Time.fixedDeltaTime);
                    body.MovePosition(nextPosition);
                }

                if (movementDirection.sqrMagnitude > Mathf.Epsilon)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(movementDirection.normalized, Vector3.up);
                    body.MoveRotation(Quaternion.RotateTowards(
                        body.rotation, targetRotation, npcData.TurnSpeed * Time.fixedDeltaTime));
                }
            }
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

        private void TryAcquireTarget()
        {
            if (oreSpawner.TryReserveClosestOre(this, transform.position, npcData.MiningPower,
                out Ore ore, out int slotIndex))
            {
                targetOre = ore;
                reservedSlot = slotIndex;
            }
        }

        private Vector3 GetReservedStandPosition()
        {
            float angle = 360f * reservedSlot / targetOre.Data.MaximumMiningNpcs;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            return targetOre.transform.position + direction * targetOre.Data.NpcStandDistance;
        }

        private bool IsTargetValid()
        {
            return npcData != null && targetOre != null && targetOre.isActiveAndEnabled &&
                   !targetOre.IsDepleted &&
                   targetOre.Data != null &&
                   targetOre.Data.MiningPowerRequired <= npcData.MiningPower;
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
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;
                body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
        }

    }
}
