using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Designer-owned multiplier applied to every item of one rarity during selection.</summary>
    [Serializable]
    public sealed class MiningItemRaritySelectionRule
    {
        [SerializeField] private MiningItemRarity rarity;
        [Tooltip("Multiplies every item of this rarity's Selection Chance before the drop roll. 1 = unchanged.")]
        [Min(0f), SerializeField] private float dropChanceMultiplier = 1f;

        public MiningItemRarity Rarity => rarity;
        public float DropChanceMultiplier => dropChanceMultiplier;
    }

    /// <summary>Drop, pickup, inventory and save tuning shared by all mining items.</summary>
    [CreateAssetMenu(fileName = "MiningItemDatabase", menuName = "Mining Simulator/Game Data/Item Database")]
    public sealed class MiningItemDatabase : ScriptableObject
    {
        public const int InventoryCapacity = 32;

        [Header("Source Drop Chance")]
        [Tooltip("Chance that a depleted Ore produces one item.")]
        [Range(0f, 100f), SerializeField] private float oreDropChancePercent = 12f;
        [Tooltip("Chance that a broken Lucky Block produces one item.")]
        [Range(0f, 100f), SerializeField] private float luckyBlockDropChancePercent = 35f;
        [SerializeField] private List<MiningItemData> items = new();

        [Header("Rarity Drop Chance")]
        [Tooltip("Per-rarity multiplier applied to each item's Selection Chance (Common, Uncommon, Rare, Epic, Legendary). Leave at 1 to keep authored per-item chances unchanged.")]
        [SerializeField] private List<MiningItemRaritySelectionRule> rarityRules = new();

        [Header("World Drop")]
        [Min(0f), SerializeField] private float spawnHeight = 0.65f;
        [SerializeField] private Vector2 horizontalImpulseRange = new(0.5f, 1.25f);
        [SerializeField] private Vector2 upwardImpulseRange = new(2.2f, 3.2f);
        [SerializeField] private Vector2 angularSpeedRange = new(80f, 180f);
        [Min(0.01f), SerializeField] private float mass = 0.35f;
        [Min(0f), SerializeField] private float linearDamping = 0.15f;
        [Min(0f), SerializeField] private float angularDamping = 0.4f;
        [Min(0.05f), SerializeField] private float colliderRadius = 0.3f;
        [Range(0, 5), SerializeField] private int bounceCount = 2;
        [Min(0f), SerializeField] private float bounceVelocity = 1.8f;
        [Min(0f), SerializeField] private float autoPickupDelay = 1.6f;
        [Min(1f), SerializeField] private float maximumWorldLifetime = 30f;

        [Header("Persistence")]
        [SerializeField] private string inventorySaveKey = "MiningSimulator.Inventory.v1";

        public float OreDropChancePercent => oreDropChancePercent;
        public float LuckyBlockDropChancePercent => luckyBlockDropChancePercent;
        public IReadOnlyList<MiningItemData> Items => items;
        public IReadOnlyList<MiningItemRaritySelectionRule> RarityRules => rarityRules;
        public float SpawnHeight => spawnHeight;
        public Vector2 HorizontalImpulseRange => horizontalImpulseRange;
        public Vector2 UpwardImpulseRange => upwardImpulseRange;
        public Vector2 AngularSpeedRange => angularSpeedRange;
        public float Mass => mass;
        public float LinearDamping => linearDamping;
        public float AngularDamping => angularDamping;
        public float ColliderRadius => colliderRadius;
        public int BounceCount => bounceCount;
        public float BounceVelocity => bounceVelocity;
        public float AutoPickupDelay => autoPickupDelay;
        public float MaximumWorldLifetime => maximumWorldLifetime;
        public string InventorySaveKey => inventorySaveKey;

        public MiningItemRaritySelectionRule GetRarityRule(MiningItemRarity rarity)
        {
            foreach (MiningItemRaritySelectionRule rule in rarityRules)
            {
                if (rule != null && rule.Rarity == rarity)
                {
                    return rule;
                }
            }
            return null;
        }

        public float GetRarityDropChanceMultiplier(MiningItemRarity rarity)
        {
            MiningItemRaritySelectionRule rule = GetRarityRule(rarity);
            return rule != null ? Mathf.Max(0f, rule.DropChanceMultiplier) : 1f;
        }

        public MiningItemData FindById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }
            foreach (MiningItemData item in items)
            {
                if (item != null && item.ItemId == itemId)
                {
                    return item;
                }
            }
            return null;
        }

        public bool TryRoll(bool fromLuckyBlock, out MiningItemData item)
        {
            return TryRoll(fromLuckyBlock, 0f, out item);
        }

        public bool TryRoll(bool fromLuckyBlock, float addedDropChancePercent,
            out MiningItemData item)
        {
            item = null;
            float baseSourceChance = fromLuckyBlock
                ? luckyBlockDropChancePercent
                : oreDropChancePercent;
            float sourceChance = Mathf.Clamp(baseSourceChance +
                Mathf.Max(0f, addedDropChancePercent), 0f, 100f);
            if (sourceChance <= 0f || (sourceChance < 100f &&
                UnityEngine.Random.value >= sourceChance * 0.01f))
            {
                return false;
            }

            float total = 0f;
            foreach (MiningItemData candidate in items)
            {
                if (candidate != null)
                {
                    total += GetWeightedSelectionChance(candidate);
                }
            }
            if (total <= 0f)
            {
                return false;
            }

            float roll = UnityEngine.Random.value * total;
            foreach (MiningItemData candidate in items)
            {
                if (candidate == null)
                {
                    continue;
                }
                roll -= GetWeightedSelectionChance(candidate);
                if (roll <= 0f)
                {
                    item = candidate;
                    return true;
                }
            }

            return false;
        }

        private float GetWeightedSelectionChance(MiningItemData candidate)
        {
            float baseChance = Mathf.Max(0f, candidate.SelectionChancePercent);
            float rarityMultiplier = GetRarityDropChanceMultiplier(candidate.Rarity);
            return baseChance * rarityMultiplier;
        }

        private void OnValidate()
        {
            oreDropChancePercent = Mathf.Clamp(oreDropChancePercent, 0f, 100f);
            luckyBlockDropChancePercent = Mathf.Clamp(luckyBlockDropChancePercent, 0f, 100f);
            spawnHeight = Mathf.Max(0f, spawnHeight);
            mass = Mathf.Max(0.01f, mass);
            linearDamping = Mathf.Max(0f, linearDamping);
            angularDamping = Mathf.Max(0f, angularDamping);
            colliderRadius = Mathf.Max(0.05f, colliderRadius);
            bounceCount = Mathf.Clamp(bounceCount, 0, 5);
            bounceVelocity = Mathf.Max(0f, bounceVelocity);
            autoPickupDelay = Mathf.Max(0f, autoPickupDelay);
            maximumWorldLifetime = Mathf.Max(1f, maximumWorldLifetime);
            if (string.IsNullOrWhiteSpace(inventorySaveKey))
            {
                inventorySaveKey = "MiningSimulator.Inventory.v1";
            }
        }
    }
}