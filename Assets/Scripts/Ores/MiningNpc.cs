using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Walks to the nearest mineable ore and damages it at a fixed interval.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningNpc : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private OreSpawner oreSpawner;
        [SerializeField] private Transform toolPivot;

        [Header("Movement")]
        [Min(0.1f), SerializeField] private float moveSpeed = 3.5f;
        [Min(0f), SerializeField] private float turnSpeed = 720f;
        [Min(0.1f), SerializeField] private float miningRange = 1.8f;

        [Header("Mining")]
        [Min(1), SerializeField] private int miningPower = 6;
        [Min(1), SerializeField] private int damagePerHit = 2;
        [Min(0.05f), SerializeField] private float secondsPerHit = 0.65f;
        [Min(0.05f), SerializeField] private float targetRefreshInterval = 0.35f;

        private Ore targetOre;
        private float nextHitTime;
        private float nextTargetRefreshTime;
        private Quaternion toolRestRotation;
        private bool isMining;

        public void ConfigureTool(Transform targetToolPivot)
        {
            toolPivot = targetToolPivot;
        }

        public void Initialize(OreSpawner targetSpawner)
        {
            oreSpawner = targetSpawner;
            targetOre = null;
            nextTargetRefreshTime = 0f;
        }

        private void Awake()
        {
            if (toolPivot != null)
            {
                toolRestRotation = toolPivot.localRotation;
            }
        }

        private void Update()
        {
            isMining = false;
            if (oreSpawner == null)
            {
                return;
            }

            if (!IsTargetValid() && Time.time >= nextTargetRefreshTime)
            {
                targetOre = oreSpawner.FindClosestMineableOre(transform.position, miningPower);
                nextTargetRefreshTime = Time.time + targetRefreshInterval;
            }

            if (!IsTargetValid())
            {
                return;
            }

            Vector3 targetPosition = targetOre.transform.position;
            Vector3 flatOffset = targetPosition - transform.position;
            flatOffset.y = 0f;

            FaceDirection(flatOffset);
            if (flatOffset.sqrMagnitude > miningRange * miningRange)
            {
                Vector3 destination = new(targetPosition.x, transform.position.y, targetPosition.z);
                transform.position = Vector3.MoveTowards(
                    transform.position, destination, moveSpeed * Time.deltaTime);
                return;
            }

            if (Time.time < nextHitTime)
            {
                isMining = true;
                return;
            }

            isMining = true;
            nextHitTime = Time.time + secondsPerHit;
            targetOre.ApplyDamage(damagePerHit);
            if (!IsTargetValid())
            {
                targetOre = null;
                nextTargetRefreshTime = 0f;
            }
        }

        private void LateUpdate()
        {
            if (toolPivot == null)
            {
                return;
            }

            Quaternion targetRotation = toolRestRotation;
            if (isMining)
            {
                float swing = Mathf.Sin(Time.time * 12f) * 42f;
                targetRotation *= Quaternion.Euler(0f, 0f, swing);
            }

            toolPivot.localRotation = Quaternion.Slerp(
                toolPivot.localRotation, targetRotation, 14f * Time.deltaTime);
        }

        private bool IsTargetValid()
        {
            return targetOre != null && !targetOre.IsDepleted && targetOre.Data != null &&
                   targetOre.Data.MiningPowerRequired <= miningPower;
        }

        private void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            turnSpeed = Mathf.Max(0f, turnSpeed);
            miningRange = Mathf.Max(0.1f, miningRange);
            miningPower = Mathf.Max(1, miningPower);
            damagePerHit = Mathf.Max(1, damagePerHit);
            secondsPerHit = Mathf.Max(0.05f, secondsPerHit);
            targetRefreshInterval = Mathf.Max(0.05f, targetRefreshInterval);
        }
    }
}
