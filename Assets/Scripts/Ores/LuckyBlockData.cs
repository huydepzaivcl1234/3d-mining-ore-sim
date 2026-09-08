using System;
using System.Collections.Generic;
using UnityEngine;

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
        [Min(0f), SerializeField] private float selectionWeight = 1f;
        [Min(1), SerializeField] private int durability = 10;
        [Min(1), SerializeField] private int clickDamage = 1;
        [Min(0), SerializeField] private int moneyReward = 100;
        [Min(0.1f), SerializeField] private float sizeMultiplier = 1f;

        public LuckyBlockType Type => type;
        public string DisplayName => displayName;
        public GameObject Model => model;
        public float SelectionWeight => selectionWeight;
        public int Durability => durability;
        public int ClickDamage => clickDamage;
        public int MoneyReward => moneyReward;
        public float SizeMultiplier => sizeMultiplier;

        internal void Validate()
        {
            selectionWeight = Mathf.Max(0f, selectionWeight);
            durability = Mathf.Max(1, durability);
            clickDamage = Mathf.Max(1, clickDamage);
            moneyReward = Mathf.Max(0, moneyReward);
            sizeMultiplier = Mathf.Max(0.1f, sizeMultiplier);
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
        [Min(0.01f), SerializeField] private float hitPunchDuration = 0.16f;

        [Header("Health Bar")]
        [SerializeField] private Vector3 healthBarWorldOffset = new(0f, 0.25f, 0f);
        [Min(0.01f), SerializeField] private float healthBarScale = 0.65f;

        [Header("Pooling")]
        [Min(0), SerializeField] private int maximumPooledBlocks = 6;

        [Header("Variants")]
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
        public float HitPunchDuration => hitPunchDuration;
        public Vector3 HealthBarWorldOffset => healthBarWorldOffset;
        public float HealthBarScale => healthBarScale;
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
            hitPunchDuration = Mathf.Max(0.01f, hitPunchDuration);
            healthBarScale = Mathf.Max(0.01f, healthBarScale);
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
