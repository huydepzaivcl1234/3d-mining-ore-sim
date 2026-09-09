using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
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
            item = null;
            float sourceChance = fromLuckyBlock
                ? luckyBlockDropChancePercent
                : oreDropChancePercent;
            if (sourceChance <= 0f || (sourceChance < 100f &&
                Random.value >= sourceChance * 0.01f))
            {
                return false;
            }

            float total = 0f;
            foreach (MiningItemData candidate in items)
            {
                if (candidate != null)
                {
                    total += Mathf.Max(0f, candidate.SelectionChancePercent);
                }
            }
            if (total <= 0f)
            {
                return false;
            }

            float roll = Random.value * total;
            foreach (MiningItemData candidate in items)
            {
                if (candidate == null)
                {
                    continue;
                }
                roll -= Mathf.Max(0f, candidate.SelectionChancePercent);
                if (roll <= 0f)
                {
                    item = candidate;
                    return true;
                }
            }

            return false;
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
