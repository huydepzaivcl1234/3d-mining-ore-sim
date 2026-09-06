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
        private Vector3 desiredMoveDirection;
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
            desiredMoveDirection = Vector3.zero;
        }

        private void Update()
        {
            desiredMoveDirection = Vector3.zero;
            isMining = false;
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
            Vector3 standOffset = standPosition - transform.position;
            standOffset.y = 0f;
            Vector3 oreOffset = targetOre.transform.position - transform.position;
            oreOffset.y = 0f;

            FaceDirection(oreOffset);
            if (oreOffset.sqrMagnitude > npcData.MiningRange * npcData.MiningRange)
            {
                desiredMoveDirection = standOffset.sqrMagnitude > 0.0001f
                    ? standOffset.normalized
                    : oreOffset.normalized;
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
            if (desiredMoveDirection.sqrMagnitude < 0.0001f || npcData == null)
            {
                return;
            }

            Vector3 movement = desiredMoveDirection * (npcData.MoveSpeed * Time.fixedDeltaTime);
            if (body != null)
            {
                body.MovePosition(body.position + movement);
                Quaternion targetRotation = Quaternion.LookRotation(desiredMoveDirection, Vector3.up);
                body.MoveRotation(Quaternion.RotateTowards(
                    body.rotation, targetRotation, npcData.TurnSpeed * Time.fixedDeltaTime));
            }
            else
            {
                transform.position += movement;
                FaceDirection(desiredMoveDirection);
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

        private void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f || npcData == null)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, npcData.TurnSpeed * Time.deltaTime);
        }
    }
}
