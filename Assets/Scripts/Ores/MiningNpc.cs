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
        [SerializeField] private MiningGameData gameData;
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

        public void Initialize(OreSpawner targetSpawner, MiningGameData targetGameData)
        {
            ReleaseTarget();
            oreSpawner = targetSpawner;
            gameData = targetGameData;
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
            if (oreSpawner == null || gameData == null)
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
                nextTargetRefreshTime = Time.time + gameData.NpcTargetRefreshInterval;
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
            if (oreOffset.sqrMagnitude > gameData.NpcMiningRange * gameData.NpcMiningRange)
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

            nextHitTime = Time.time + gameData.NpcSecondsPerHit;
            targetOre.ApplyDamage(gameData.NpcDamagePerHit);
            if (!IsTargetValid())
            {
                ReleaseTarget();
                nextTargetRefreshTime = 0f;
            }
        }

        private void FixedUpdate()
        {
            if (desiredMoveDirection.sqrMagnitude < 0.0001f || gameData == null)
            {
                return;
            }

            Vector3 movement = desiredMoveDirection * (gameData.NpcMoveSpeed * Time.fixedDeltaTime);
            if (body != null)
            {
                body.MovePosition(body.position + movement);
                Quaternion targetRotation = Quaternion.LookRotation(desiredMoveDirection, Vector3.up);
                body.MoveRotation(Quaternion.RotateTowards(
                    body.rotation, targetRotation, gameData.NpcTurnSpeed * Time.fixedDeltaTime));
            }
            else
            {
                transform.position += movement;
                FaceDirection(desiredMoveDirection);
            }
        }

        private void LateUpdate()
        {
            if (toolPivot == null || gameData == null)
            {
                return;
            }

            Quaternion targetRotation = toolRestRotation;
            if (isMining)
            {
                float swing = Mathf.Sin(Time.time * gameData.ToolSwingSpeed) * gameData.ToolSwingAngle;
                targetRotation *= Quaternion.Euler(0f, 0f, swing);
            }

            toolPivot.localRotation = Quaternion.Slerp(
                toolPivot.localRotation, targetRotation, gameData.ToolReturnSpeed * Time.deltaTime);
        }

        private void TryAcquireTarget()
        {
            if (oreSpawner.TryReserveClosestOre(this, transform.position, gameData.NpcMiningPower,
                out Ore ore, out int slotIndex))
            {
                targetOre = ore;
                reservedSlot = slotIndex;
            }
        }

        private Vector3 GetReservedStandPosition()
        {
            float angle = 360f * reservedSlot / gameData.MaximumNpcsPerOre;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            return targetOre.transform.position + direction * gameData.NpcOreStandDistance;
        }

        private bool IsTargetValid()
        {
            return gameData != null && targetOre != null && targetOre.isActiveAndEnabled &&
                   !targetOre.IsDepleted &&
                   targetOre.Data != null &&
                   targetOre.Data.MiningPowerRequired <= gameData.NpcMiningPower;
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
            if (gameData == null)
            {
                return;
            }

            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.radius = gameData.NpcColliderRadius;
                capsule.height = gameData.NpcColliderHeight;
            }

            body ??= GetComponent<Rigidbody>();
            if (body != null)
            {
                body.mass = gameData.NpcMass;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;
                body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
        }

        private void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f || gameData == null)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, gameData.NpcTurnSpeed * Time.deltaTime);
        }
    }
}
