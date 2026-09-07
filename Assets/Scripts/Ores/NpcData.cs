using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Designer-owned configuration for mining NPCs only.</summary>
    [CreateAssetMenu(fileName = "NpcData", menuName = "Mining Simulator/Game Data/NPC")]
    public sealed class NpcData : ScriptableObject
    {
        [Header("Purchase And Spawn")]
        [Min(0), SerializeField] private int purchaseCost = 25;
        [Min(0f), SerializeField] private float spawnSpread = 1.25f;
        [SerializeField] private float spawnHeightOffset = 0.9f;
        [Min(1), SerializeField] private int spawnAttempts = 12;

        [Header("Movement And Mining")]
        [Min(0.1f), SerializeField] private float moveSpeed = 3.5f;
        [Min(0f), SerializeField] private float turnSpeed = 720f;
        [Min(0.1f), SerializeField] private float miningRange = 1.8f;
        [Min(0.01f), SerializeField] private float stoppingDistance = 0.12f;
        [Min(0.01f), SerializeField] private float resumeMovingDistance = 0.2f;
        [Min(1), SerializeField] private int miningPower = 6;
        [Min(1), SerializeField] private int damagePerHit = 2;
        [Min(0.05f), SerializeField] private float secondsPerHit = 0.65f;
        [Min(0.05f), SerializeField] private float targetRefreshInterval = 0.35f;

        [Header("Collision")]
        [Min(0.05f), SerializeField] private float colliderRadius = 0.4f;
        [Min(0.1f), SerializeField] private float colliderHeight = 1.8f;
        [Min(0.01f), SerializeField] private float mass = 1f;
        [SerializeField] private LayerMask collisionLayers = ~0;

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
        public float SpawnSpread => spawnSpread;
        public float SpawnHeightOffset => spawnHeightOffset;
        public int SpawnAttempts => spawnAttempts;
        public float MoveSpeed => moveSpeed;
        public float TurnSpeed => turnSpeed;
        public float MiningRange => miningRange;
        public float StoppingDistance => stoppingDistance;
        public float ResumeMovingDistance => resumeMovingDistance;
        public int MiningPower => miningPower;
        public int DamagePerHit => damagePerHit;
        public float SecondsPerHit => secondsPerHit;
        public float TargetRefreshInterval => targetRefreshInterval;
        public float ColliderRadius => colliderRadius;
        public float ColliderHeight => colliderHeight;
        public float Mass => mass;
        public LayerMask CollisionLayers => collisionLayers;
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
            spawnSpread = Mathf.Max(0f, spawnSpread);
            spawnAttempts = Mathf.Max(1, spawnAttempts);
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            turnSpeed = Mathf.Max(0f, turnSpeed);
            miningRange = Mathf.Max(0.1f, miningRange);
            stoppingDistance = Mathf.Max(0.01f, stoppingDistance);
            resumeMovingDistance = Mathf.Max(stoppingDistance, resumeMovingDistance);
            miningPower = Mathf.Max(1, miningPower);
            damagePerHit = Mathf.Max(1, damagePerHit);
            secondsPerHit = Mathf.Max(0.05f, secondsPerHit);
            targetRefreshInterval = Mathf.Max(0.05f, targetRefreshInterval);
            colliderRadius = Mathf.Max(0.05f, colliderRadius);
            colliderHeight = Mathf.Max(colliderRadius * 2f, colliderHeight);
            mass = Mathf.Max(0.01f, mass);
            toolSwingSpeed = Mathf.Max(0f, toolSwingSpeed);
            toolSwingAngle = Mathf.Max(0f, toolSwingAngle);
            toolReturnSpeed = Mathf.Max(0f, toolReturnSpeed);
        }
    }
}
