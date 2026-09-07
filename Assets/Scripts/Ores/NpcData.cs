using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Designer-owned configuration for mining NPCs only.</summary>
    [CreateAssetMenu(fileName = "NpcData", menuName = "Mining Simulator/Game Data/NPC")]
    public sealed class NpcData : ScriptableObject
    {
        [Header("Purchase And Spawn")]
        [Min(0), SerializeField] private int purchaseCost = 25;
        [Min(0), SerializeField] private int startingMaximumMiners = 3;
        [Min(0f), SerializeField] private float spawnSpread = 1.25f;
        [SerializeField] private float spawnHeightOffset = 0.9f;
        [Min(1), SerializeField] private int spawnAttempts = 12;

        [Header("Movement And Mining")]
        [Min(0.1f), SerializeField] private float moveSpeed = 3.5f;
        [Min(0f), SerializeField] private float turnSpeed = 720f;
        [Min(0.1f), SerializeField] private float miningRange = 1.8f;
        [Min(0.01f), SerializeField] private float stoppingDistance = 0.12f;
        [Min(0.1f), SerializeField] private float movementAcceleration = 18f;
        [Min(0.1f), SerializeField] private float brakingAcceleration = 28f;
        [Min(0f), SerializeField] private float targetSwitchDistanceAdvantage = 0.25f;
        [Min(0f), SerializeField] private float targetSwitchCooldown = 0.75f;
        [Min(0f), SerializeField] private float standSlotSpacingPadding = 0.12f;
        [Min(1), SerializeField] private int miningPower = 6;
        [Min(1), SerializeField] private int damagePerHit = 2;
        [Min(0.05f), SerializeField] private float secondsPerHit = 0.65f;
        [Min(0.05f), SerializeField] private float targetRefreshInterval = 0.35f;

        [Header("Ore Sight")]
        [Min(0.05f), SerializeField] private float oreSightProbeRadius = 0.35f;
        [Min(0.1f), SerializeField] private float oreSightDistance = 2.2f;
        [Min(0f), SerializeField] private float oreSightOriginHeight = 0.7f;

        [Header("Collision")]
        [Min(0.05f), SerializeField] private float colliderRadius = 0.4f;
        [Min(0.1f), SerializeField] private float colliderHeight = 1.8f;
        [Min(0.01f), SerializeField] private float mass = 1f;
        [SerializeField] private LayerMask collisionLayers = ~0;
        [SerializeField] private bool ignoreNpcPhysicalCollisions = true;

        [Header("Dynamic Obstacle Response")]
        [Min(0.05f), SerializeField] private float obstacleProbeRadius = 0.32f;
        [Min(0.1f), SerializeField] private float obstacleProbeDistance = 1.25f;
        [Min(0f), SerializeField] private float obstacleAvoidanceStrength = 1.35f;
        [Min(0.1f), SerializeField] private float npcSeparationRadius = 1.05f;
        [Min(0f), SerializeField] private float npcSeparationStrength = 1.2f;
        [Min(0.1f), SerializeField] private float npcSeparationResponsiveness = 8f;
        [Min(0.1f), SerializeField] private float stuckTimeout = 1.25f;
        [Min(0.001f), SerializeField] private float stuckProgressDistance = 0.08f;
        [Min(0.1f), SerializeField] private float ignoredTargetDuration = 1.5f;

        [Header("Tool Animation")]
        [Min(0f), SerializeField] private float toolSwingSpeed = 12f;
        [Min(0f), SerializeField] private float toolSwingAngle = 42f;
        [Min(0f), SerializeField] private float toolReturnSpeed = 14f;

        [Header("Generated Prefab Presentation")]
        [SerializeField] private Vector3 bodyScale = new(0.8f, 0.9f, 0.8f);
        [SerializeField] private Vector3 helmetLocalPosition = new(0f, 0.86f, 0.08f);
        [SerializeField] private Vector3 helmetLocalScale = new(0.9f, 0.18f, 0.92f);
        [SerializeField] private Vector3 toolLocalPosition = new(0.65f, 0f, 0f);
        [SerializeField] private Vector3 toolLocalEulerAngles = new(0f, 0f, -25f);
        [SerializeField] private Vector3 toolLocalScale = new(0.08f, 0.85f, 0.08f);
        [SerializeField] private Vector3 toolHeadLocalPosition = new(0f, 0.55f, 0f);
        [SerializeField] private Vector3 toolHeadLocalScale = new(3.8f, 0.18f, 0.65f);

        public int PurchaseCost => purchaseCost;
        public int StartingMaximumMiners => startingMaximumMiners;
        public float SpawnSpread => spawnSpread;
        public float SpawnHeightOffset => spawnHeightOffset;
        public int SpawnAttempts => spawnAttempts;
        public float MoveSpeed => moveSpeed;
        public float TurnSpeed => turnSpeed;
        public float MiningRange => miningRange;
        public float StoppingDistance => stoppingDistance;
        public float MovementAcceleration => movementAcceleration;
        public float BrakingAcceleration => brakingAcceleration;
        public float TargetSwitchDistanceAdvantage => targetSwitchDistanceAdvantage;
        public float TargetSwitchCooldown => targetSwitchCooldown;
        public float StandSlotSpacingPadding => standSlotSpacingPadding;
        public int MiningPower => miningPower;
        public int DamagePerHit => damagePerHit;
        public float SecondsPerHit => secondsPerHit;
        public float TargetRefreshInterval => targetRefreshInterval;
        public float OreSightProbeRadius => oreSightProbeRadius;
        public float OreSightDistance => oreSightDistance;
        public float OreSightOriginHeight => oreSightOriginHeight;
        public float ColliderRadius => colliderRadius;
        public float ColliderHeight => colliderHeight;
        public float Mass => mass;
        public LayerMask CollisionLayers => collisionLayers;
        public bool IgnoreNpcPhysicalCollisions => ignoreNpcPhysicalCollisions;
        public float ObstacleProbeRadius => obstacleProbeRadius;
        public float ObstacleProbeDistance => obstacleProbeDistance;
        public float ObstacleAvoidanceStrength => obstacleAvoidanceStrength;
        public float NpcSeparationRadius => npcSeparationRadius;
        public float NpcSeparationStrength => npcSeparationStrength;
        public float NpcSeparationResponsiveness => npcSeparationResponsiveness;
        public float StuckTimeout => stuckTimeout;
        public float StuckProgressDistance => stuckProgressDistance;
        public float IgnoredTargetDuration => ignoredTargetDuration;
        public float ToolSwingSpeed => toolSwingSpeed;
        public float ToolSwingAngle => toolSwingAngle;
        public float ToolReturnSpeed => toolReturnSpeed;
        public Vector3 BodyScale => bodyScale;
        public Vector3 HelmetLocalPosition => helmetLocalPosition;
        public Vector3 HelmetLocalScale => helmetLocalScale;
        public Vector3 ToolLocalPosition => toolLocalPosition;
        public Vector3 ToolLocalEulerAngles => toolLocalEulerAngles;
        public Vector3 ToolLocalScale => toolLocalScale;
        public Vector3 ToolHeadLocalPosition => toolHeadLocalPosition;
        public Vector3 ToolHeadLocalScale => toolHeadLocalScale;

        private void OnValidate()
        {
            purchaseCost = Mathf.Max(0, purchaseCost);
            startingMaximumMiners = Mathf.Max(0, startingMaximumMiners);
            spawnSpread = Mathf.Max(0f, spawnSpread);
            spawnAttempts = Mathf.Max(1, spawnAttempts);
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            turnSpeed = Mathf.Max(0f, turnSpeed);
            miningRange = Mathf.Max(0.1f, miningRange);
            stoppingDistance = Mathf.Max(0.01f, stoppingDistance);
            movementAcceleration = Mathf.Max(0.1f, movementAcceleration);
            brakingAcceleration = Mathf.Max(0.1f, brakingAcceleration);
            targetSwitchDistanceAdvantage = Mathf.Max(0f, targetSwitchDistanceAdvantage);
            targetSwitchCooldown = Mathf.Max(0f, targetSwitchCooldown);
            standSlotSpacingPadding = Mathf.Max(0f, standSlotSpacingPadding);
            miningPower = Mathf.Max(1, miningPower);
            damagePerHit = Mathf.Max(1, damagePerHit);
            secondsPerHit = Mathf.Max(0.05f, secondsPerHit);
            targetRefreshInterval = Mathf.Max(0.05f, targetRefreshInterval);
            oreSightProbeRadius = Mathf.Max(0.05f, oreSightProbeRadius);
            oreSightDistance = Mathf.Max(miningRange, oreSightDistance);
            oreSightOriginHeight = Mathf.Max(0f, oreSightOriginHeight);
            colliderRadius = Mathf.Max(0.05f, colliderRadius);
            colliderHeight = Mathf.Max(colliderRadius * 2f, colliderHeight);
            mass = Mathf.Max(0.01f, mass);
            obstacleProbeRadius = Mathf.Max(0.05f, obstacleProbeRadius);
            obstacleProbeDistance = Mathf.Max(0.1f, obstacleProbeDistance);
            obstacleAvoidanceStrength = Mathf.Max(0f, obstacleAvoidanceStrength);
            npcSeparationRadius = Mathf.Max(colliderRadius * 2f, npcSeparationRadius);
            npcSeparationStrength = Mathf.Max(0f, npcSeparationStrength);
            npcSeparationResponsiveness = Mathf.Max(0.1f, npcSeparationResponsiveness);
            stuckTimeout = Mathf.Max(0.1f, stuckTimeout);
            stuckProgressDistance = Mathf.Max(0.001f, stuckProgressDistance);
            ignoredTargetDuration = Mathf.Max(0.1f, ignoredTargetDuration);
            toolSwingSpeed = Mathf.Max(0f, toolSwingSpeed);
            toolSwingAngle = Mathf.Max(0f, toolSwingAngle);
            toolReturnSpeed = Mathf.Max(0f, toolReturnSpeed);
        }
    }
}
