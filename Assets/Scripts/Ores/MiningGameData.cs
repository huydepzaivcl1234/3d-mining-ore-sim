using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class OreSpawnEntry
    {
        [SerializeField] private OreData data;
        [Min(0f), SerializeField] private float weight = 1f;

        public OreData Data => data;
        public float Weight => weight;
    }

    /// <summary>Central designer-owned balancing data for the mining game.</summary>
    [CreateAssetMenu(fileName = "MiningGameData", menuName = "Mining Simulator/Game Data")]
    public sealed class MiningGameData : ScriptableObject
    {
        [Header("Economy")]
        [Min(0), SerializeField] private int startingMoney = 100;
        [Min(0), SerializeField] private int npcCost = 25;

        [Header("NPC Spawn")]
        [Min(0f), SerializeField] private float npcSpawnSpread = 1.25f;
        [SerializeField] private float npcSpawnHeightOffset = 0.9f;
        [Min(1), SerializeField] private int npcSpawnAttempts = 12;

        [Header("NPC Movement And Mining")]
        [Min(0.1f), SerializeField] private float npcMoveSpeed = 3.5f;
        [Min(0f), SerializeField] private float npcTurnSpeed = 720f;
        [Min(0.1f), SerializeField] private float npcMiningRange = 1.8f;
        [Min(1), SerializeField] private int npcMiningPower = 6;
        [Min(1), SerializeField] private int npcDamagePerHit = 2;
        [Min(0.05f), SerializeField] private float npcSecondsPerHit = 0.65f;
        [Min(0.05f), SerializeField] private float npcTargetRefreshInterval = 0.35f;
        [Min(1), SerializeField] private int maximumNpcsPerOre = 3;
        [Min(0.1f), SerializeField] private float npcOreStandDistance = 1.4f;

        [Header("NPC Collision")]
        [Min(0.05f), SerializeField] private float npcColliderRadius = 0.4f;
        [Min(0.1f), SerializeField] private float npcColliderHeight = 1.8f;
        [Min(0.01f), SerializeField] private float npcMass = 1f;
        [SerializeField] private LayerMask npcCollisionLayers = ~0;

        [Header("NPC Tool Animation")]
        [Min(0f), SerializeField] private float toolSwingSpeed = 12f;
        [Min(0f), SerializeField] private float toolSwingAngle = 42f;
        [Min(0f), SerializeField] private float toolReturnSpeed = 14f;

        [Header("Ore Population")]
        [SerializeField] private bool spawnOnEnable = true;
        [Min(0), SerializeField] private int initialSpawnCount = 8;
        [Min(0), SerializeField] private int maximumAliveOres = 12;
        [Min(0.05f), SerializeField] private float oreSpawnInterval = 2f;
        [SerializeField] private List<OreSpawnEntry> orePool = new();

        [Header("Ore Spawn Area")]
        [SerializeField] private Vector3 oreAreaCenter;
        [SerializeField] private Vector3 oreAreaSize = new(16f, 0f, 16f);
        [SerializeField] private float oreHeightOffset;
        [SerializeField] private bool randomOreYRotation = true;
        [SerializeField] private Vector2 oreUniformScaleRange = Vector2.one;

        [Header("Ore Ground Placement")]
        [SerializeField] private bool alignOresToGround;
        [SerializeField] private LayerMask oreGroundLayers = ~0;
        [Min(0.1f), SerializeField] private float oreGroundRayStartHeight = 20f;
        [Min(0.1f), SerializeField] private float oreGroundRayDistance = 50f;

        [Header("Click Mining")]
        [SerializeField] private LayerMask clickableLayers = ~0;
        [Min(0.1f), SerializeField] private float clickMaximumDistance = 500f;

        [Header("Money Count Animation")]
        [Min(1f), SerializeField] private float moneyCountUnitsPerSecond = 25f;
        [Min(0.05f), SerializeField] private float moneyCountMaximumDuration = 1.25f;

        [Header("Camera")]
        [SerializeField] private Vector3 cameraFocusPoint;
        [Min(0.1f), SerializeField] private float cameraDistance = 18f;
        [SerializeField] private float cameraYaw = 45f;
        [SerializeField] private float cameraPitch = 38f;
        [Range(-89f, 89f), SerializeField] private float cameraMinimumPitch = -85f;
        [Range(-89f, 89f), SerializeField] private float cameraMaximumPitch = 85f;
        [Min(0f), SerializeField] private float cameraMoveSpeed = 10f;
        [Min(1f), SerializeField] private float cameraFastMoveMultiplier = 2f;
        [Min(0f), SerializeField] private float cameraRotationDegreesPerPixel = 0.2f;
        [Min(0f), SerializeField] private float cameraKeyboardRotationSpeed = 90f;
        [Min(0f), SerializeField] private float cameraMousePanSpeed = 0.0025f;
        [Min(0f), SerializeField] private float cameraZoomSpeed = 0.012f;
        [Min(0.1f), SerializeField] private float cameraMinimumDistance = 4f;
        [Min(0.1f), SerializeField] private float cameraMaximumDistance = 45f;

        public int StartingMoney => startingMoney;
        public int NpcCost => npcCost;
        public float NpcSpawnSpread => npcSpawnSpread;
        public float NpcSpawnHeightOffset => npcSpawnHeightOffset;
        public int NpcSpawnAttempts => npcSpawnAttempts;
        public float NpcMoveSpeed => npcMoveSpeed;
        public float NpcTurnSpeed => npcTurnSpeed;
        public float NpcMiningRange => npcMiningRange;
        public int NpcMiningPower => npcMiningPower;
        public int NpcDamagePerHit => npcDamagePerHit;
        public float NpcSecondsPerHit => npcSecondsPerHit;
        public float NpcTargetRefreshInterval => npcTargetRefreshInterval;
        public int MaximumNpcsPerOre => maximumNpcsPerOre;
        public float NpcOreStandDistance => npcOreStandDistance;
        public float NpcColliderRadius => npcColliderRadius;
        public float NpcColliderHeight => npcColliderHeight;
        public float NpcMass => npcMass;
        public LayerMask NpcCollisionLayers => npcCollisionLayers;
        public float ToolSwingSpeed => toolSwingSpeed;
        public float ToolSwingAngle => toolSwingAngle;
        public float ToolReturnSpeed => toolReturnSpeed;
        public bool SpawnOnEnable => spawnOnEnable;
        public int InitialSpawnCount => initialSpawnCount;
        public int MaximumAliveOres => maximumAliveOres;
        public float OreSpawnInterval => oreSpawnInterval;
        public IReadOnlyList<OreSpawnEntry> OrePool => orePool;

        public Vector3 OreAreaCenter => oreAreaCenter;
        public Vector3 OreAreaSize => oreAreaSize;
        public float OreHeightOffset => oreHeightOffset;
        public bool RandomOreYRotation => randomOreYRotation;
        public Vector2 OreUniformScaleRange => oreUniformScaleRange;
        public bool AlignOresToGround => alignOresToGround;
        public LayerMask OreGroundLayers => oreGroundLayers;
        public float OreGroundRayStartHeight => oreGroundRayStartHeight;
        public float OreGroundRayDistance => oreGroundRayDistance;
        public LayerMask ClickableLayers => clickableLayers;
        public float ClickMaximumDistance => clickMaximumDistance;
        public float MoneyCountUnitsPerSecond => moneyCountUnitsPerSecond;
        public float MoneyCountMaximumDuration => moneyCountMaximumDuration;
        public Vector3 CameraFocusPoint => cameraFocusPoint;
        public float CameraDistance => cameraDistance;
        public float CameraYaw => cameraYaw;
        public float CameraPitch => cameraPitch;
        public float CameraMinimumPitch => cameraMinimumPitch;
        public float CameraMaximumPitch => cameraMaximumPitch;
        public float CameraMoveSpeed => cameraMoveSpeed;
        public float CameraFastMoveMultiplier => cameraFastMoveMultiplier;
        public float CameraRotationDegreesPerPixel => cameraRotationDegreesPerPixel;
        public float CameraKeyboardRotationSpeed => cameraKeyboardRotationSpeed;
        public float CameraMousePanSpeed => cameraMousePanSpeed;
        public float CameraZoomSpeed => cameraZoomSpeed;
        public float CameraMinimumDistance => cameraMinimumDistance;
        public float CameraMaximumDistance => cameraMaximumDistance;

        private void OnValidate()
        {
            startingMoney = Mathf.Max(0, startingMoney);
            npcCost = Mathf.Max(0, npcCost);
            npcSpawnSpread = Mathf.Max(0f, npcSpawnSpread);
            npcSpawnAttempts = Mathf.Max(1, npcSpawnAttempts);
            npcMoveSpeed = Mathf.Max(0.1f, npcMoveSpeed);
            npcTurnSpeed = Mathf.Max(0f, npcTurnSpeed);
            npcMiningRange = Mathf.Max(0.1f, npcMiningRange);
            npcMiningPower = Mathf.Max(1, npcMiningPower);
            npcDamagePerHit = Mathf.Max(1, npcDamagePerHit);
            npcSecondsPerHit = Mathf.Max(0.05f, npcSecondsPerHit);
            npcTargetRefreshInterval = Mathf.Max(0.05f, npcTargetRefreshInterval);
            maximumNpcsPerOre = Mathf.Max(1, maximumNpcsPerOre);
            npcOreStandDistance = Mathf.Clamp(npcOreStandDistance, 0.1f, npcMiningRange);
            npcColliderRadius = Mathf.Max(0.05f, npcColliderRadius);
            npcColliderHeight = Mathf.Max(npcColliderRadius * 2f, npcColliderHeight);
            npcMass = Mathf.Max(0.01f, npcMass);
            toolSwingSpeed = Mathf.Max(0f, toolSwingSpeed);
            toolSwingAngle = Mathf.Max(0f, toolSwingAngle);
            toolReturnSpeed = Mathf.Max(0f, toolReturnSpeed);
            initialSpawnCount = Mathf.Max(0, initialSpawnCount);
            maximumAliveOres = Mathf.Max(0, maximumAliveOres);
            oreSpawnInterval = Mathf.Max(0.05f, oreSpawnInterval);
            oreAreaSize = new Vector3(Mathf.Abs(oreAreaSize.x), Mathf.Abs(oreAreaSize.y), Mathf.Abs(oreAreaSize.z));
            oreGroundRayStartHeight = Mathf.Max(0.1f, oreGroundRayStartHeight);
            oreGroundRayDistance = Mathf.Max(0.1f, oreGroundRayDistance);
            clickMaximumDistance = Mathf.Max(0.1f, clickMaximumDistance);
            moneyCountUnitsPerSecond = Mathf.Max(1f, moneyCountUnitsPerSecond);
            moneyCountMaximumDuration = Mathf.Max(0.05f, moneyCountMaximumDuration);
            cameraMinimumDistance = Mathf.Max(0.1f, cameraMinimumDistance);
            cameraMaximumDistance = Mathf.Max(cameraMinimumDistance, cameraMaximumDistance);
            cameraDistance = Mathf.Clamp(cameraDistance, cameraMinimumDistance, cameraMaximumDistance);
            cameraMinimumPitch = Mathf.Clamp(cameraMinimumPitch, -89f, 89f);
            cameraMaximumPitch = Mathf.Clamp(cameraMaximumPitch, cameraMinimumPitch, 89f);
            cameraPitch = Mathf.Clamp(cameraPitch, cameraMinimumPitch, cameraMaximumPitch);
            cameraMoveSpeed = Mathf.Max(0f, cameraMoveSpeed);
            cameraFastMoveMultiplier = Mathf.Max(1f, cameraFastMoveMultiplier);
            cameraRotationDegreesPerPixel = Mathf.Max(0f, cameraRotationDegreesPerPixel);
            cameraKeyboardRotationSpeed = Mathf.Max(0f, cameraKeyboardRotationSpeed);
            cameraMousePanSpeed = Mathf.Max(0f, cameraMousePanSpeed);
            cameraZoomSpeed = Mathf.Max(0f, cameraZoomSpeed);
        }
    }
}
