using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MiningSimulator.Ores
{
    public enum LuckyBlockType
    {
        Gold = 0,
        Diamond = 1,
        Rainbow = 2
    }

    [Serializable]
    public sealed class LuckyBlockVariantData
    {
        [SerializeField] private LuckyBlockType type;
        [SerializeField] private string displayName = "Lucky Block";
        [SerializeField] private GameObject model;
        [FormerlySerializedAs("selectionWeight"), InspectorName("Selection Chance (%)")]
        [Range(0f, 100f), SerializeField]
        private float selectionChancePercent = 1f;
        [Min(1), SerializeField] private int durability = 10;
        [Min(1), SerializeField] private int clickDamage = 1;
        [Min(0), SerializeField] private int moneyReward = 100;
        [Min(0.1f), SerializeField] private float sizeMultiplier = 1f;
        [Min(1), SerializeField] private int miningPowerRequired = 1;
        [Min(1), SerializeField] private int maximumMiningNpcs = 1;
        [Min(0f), SerializeField] private float npcStandDistance = 1.25f;

        public LuckyBlockType Type => type;
        public string DisplayName => displayName;
        public GameObject Model => model;
        public float SelectionChancePercent => selectionChancePercent;
        public int Durability => durability;
        public int ClickDamage => clickDamage;
        public int MoneyReward => moneyReward;
        public float SizeMultiplier => sizeMultiplier;
        public int MiningPowerRequired => Mathf.Max(1, miningPowerRequired);
        public int MaximumMiningNpcs => Mathf.Max(1, maximumMiningNpcs);
        public float NpcStandDistance => Mathf.Max(0f, npcStandDistance);

        internal void Validate()
        {
            selectionChancePercent = Mathf.Clamp(selectionChancePercent, 0f, 100f);
            durability = Mathf.Max(1, durability);
            clickDamage = Mathf.Max(1, clickDamage);
            moneyReward = Mathf.Max(0, moneyReward);
            sizeMultiplier = Mathf.Max(0.1f, sizeMultiplier);
            miningPowerRequired = Mathf.Max(1, miningPowerRequired);
            maximumMiningNpcs = Mathf.Max(1, maximumMiningNpcs);
            npcStandDistance = Mathf.Max(0f, npcStandDistance);
        }
    }

    /// <summary>Designer-owned drop, physics, pooling, size and reward rules for Lucky Blocks.</summary>
    [CreateAssetMenu(fileName = "LuckyBlockData", menuName = "Mining Simulator/Game Data/Lucky Blocks")]
    public sealed class LuckyBlockData : ScriptableObject
    {
        [Header("Drop Chance")]
        [Min(0.05f), SerializeField] private float dropCheckIntervalSeconds = 1f;
        [Range(0f, 100f), SerializeField] private float dropChancePerCheckPercent = 2f;
        [Min(0), SerializeField] private int maximumActiveBlocks = 2;
        [Min(1), SerializeField] private int positionAttemptsPerDrop = 16;

        [Header("Drop Placement")]
        [Tooltip("Uniform multiplier for the authored Blender size. A value of 1 keeps the original size.")]
        [Min(0.1f), SerializeField] private float blockSize = 1f;
        [Min(0f), SerializeField] private float dropHeight = 12f;
        [Min(0f), SerializeField] private float placementClearance = 0.35f;
        [SerializeField] private float randomYRotationMinimum;
        [SerializeField] private float randomYRotationMaximum = 360f;

        [Header("Falling Physics")]
        [Min(0.01f), SerializeField] private float mass = 2f;
        [Min(0f), SerializeField] private float linearDamping = 0.08f;
        [Min(0f), SerializeField] private float angularDamping = 0.25f;
        [SerializeField] private Vector2 fallingSpinRange = new(35f, 95f);
        [Min(1f), SerializeField] private float maximumLifetimeSeconds = 45f;
        [SerializeField] private float recycleBelowWorldY = -25f;

        [Header("Feedback")]
        [Range(0f, 0.5f), SerializeField] private float hitPunchScale = 0.10f;
        [Min(0f), SerializeField] private float hitPunchLift = 0.12f;
        [Min(0.01f), SerializeField] private float hitPunchDuration = 0.16f;

        [Header("Health Bar")]
        [SerializeField] private Vector3 healthBarWorldOffset = new(0f, 0.25f, 0f);
        [Min(0.01f), SerializeField] private float healthBarScale = 0.65f;

        [Header("Countdown Label")]
        [Tooltip("Shows a live 'time left' readout above the block until it expires and despawns.")]
        [SerializeField] private bool showCountdownLabel = true;
        [SerializeField] private Vector3 countdownLabelWorldOffset = new(0f, 0.55f, 0f);
        [Min(0.01f), SerializeField] private float countdownLabelScale = 0.5f;
        [Min(1f), SerializeField] private float countdownLabelFontSize = 6f;
        [SerializeField] private Color countdownLabelColor = new(1f, 0.92f, 0.35f, 1f);
        [Tooltip("At or below this many seconds remaining, the label switches to Countdown Urgent Color.")]
        [Min(0f), SerializeField] private float countdownUrgentThresholdSeconds = 5f;
        [SerializeField] private Color countdownUrgentColor = new(1f, 0.28f, 0.24f, 1f);

        [Header("Pooling")]
        [Min(0), SerializeField] private int maximumPooledBlocks = 6;

        [Header("Variants")]
        [Tooltip("Enter percentages from 0 to 100. Valid variants are normalized to a 100% roll at runtime.")]
        [SerializeField] private List<LuckyBlockVariantData> variants = new();

        public float DropCheckIntervalSeconds => dropCheckIntervalSeconds;
        public float DropChancePerCheckPercent => dropChancePerCheckPercent;
        public int MaximumActiveBlocks => maximumActiveBlocks;
        public int PositionAttemptsPerDrop => positionAttemptsPerDrop;
        public float BlockSize => blockSize;
        public float DropHeight => dropHeight;
        public float PlacementClearance => placementClearance;
        public float RandomYRotationMinimum => randomYRotationMinimum;
        public float RandomYRotationMaximum => randomYRotationMaximum;
        public float Mass => mass;
        public float LinearDamping => linearDamping;
        public float AngularDamping => angularDamping;
        public Vector2 FallingSpinRange => fallingSpinRange;
        public float MaximumLifetimeSeconds => maximumLifetimeSeconds;
        public float RecycleBelowWorldY => recycleBelowWorldY;
        public float HitPunchScale => hitPunchScale;
        public float HitPunchLift => hitPunchLift;
        public float HitPunchDuration => hitPunchDuration;
        public Vector3 HealthBarWorldOffset => healthBarWorldOffset;
        public float HealthBarScale => healthBarScale;
        public bool ShowCountdownLabel => showCountdownLabel;
        public Vector3 CountdownLabelWorldOffset => countdownLabelWorldOffset;
        public float CountdownLabelScale => countdownLabelScale;
        public float CountdownLabelFontSize => countdownLabelFontSize;
        public Color CountdownLabelColor => countdownLabelColor;
        public float CountdownUrgentThresholdSeconds => countdownUrgentThresholdSeconds;
        public Color CountdownUrgentColor => countdownUrgentColor;
        public int MaximumPooledBlocks => maximumPooledBlocks;
        public IReadOnlyList<LuckyBlockVariantData> Variants => variants;

        private void OnValidate()
        {
            dropCheckIntervalSeconds = Mathf.Max(0.05f, dropCheckIntervalSeconds);
            dropChancePerCheckPercent = Mathf.Clamp(dropChancePerCheckPercent, 0f, 100f);
            maximumActiveBlocks = Mathf.Max(0, maximumActiveBlocks);
            positionAttemptsPerDrop = Mathf.Max(1, positionAttemptsPerDrop);
            blockSize = Mathf.Max(0.1f, blockSize);
            dropHeight = Mathf.Max(0f, dropHeight);
            placementClearance = Mathf.Max(0f, placementClearance);
            mass = Mathf.Max(0.01f, mass);
            linearDamping = Mathf.Max(0f, linearDamping);
            angularDamping = Mathf.Max(0f, angularDamping);
            maximumLifetimeSeconds = Mathf.Max(1f, maximumLifetimeSeconds);
            hitPunchScale = Mathf.Clamp(hitPunchScale, 0f, 0.5f);
            hitPunchLift = Mathf.Max(0f, hitPunchLift);
            hitPunchDuration = Mathf.Max(0.01f, hitPunchDuration);
            healthBarScale = Mathf.Max(0.01f, healthBarScale);
            countdownLabelScale = Mathf.Max(0.01f, countdownLabelScale);
            countdownLabelFontSize = Mathf.Max(1f, countdownLabelFontSize);
            countdownUrgentThresholdSeconds = Mathf.Max(0f, countdownUrgentThresholdSeconds);
            maximumPooledBlocks = Mathf.Max(0, maximumPooledBlocks);
            if (variants == null)
            {
                return;
            }

            foreach (LuckyBlockVariantData variant in variants)
            {
                variant?.Validate();
            }
        }
    }
}