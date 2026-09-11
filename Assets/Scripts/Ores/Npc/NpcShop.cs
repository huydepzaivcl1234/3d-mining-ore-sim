using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Spends player money and spawns configured mining NPCs.</summary>
    [DisallowMultipleComponent]
    public sealed class NpcShop : MonoBehaviour
    {
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private OreSpawner oreSpawner;
        [SerializeField] private MiningNpc npcPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private NpcData npcData;
        [SerializeField] private NpcProgressionSystem progressionSystem;
        [SerializeField] private MiningUpgradeSystem upgradeSystem;
        [SerializeField] private LuckyBlockDropSystem luckyBlockSystem;

        private int purchasedCount;

        public int NpcCost => npcData != null ? npcData.PurchaseCost : 0;
        public int PurchasedCount => purchasedCount;
        public NpcData NpcData => npcData;
        public int MaximumMiners
        {
            get
            {
                int baseCapacity = npcData != null ? npcData.StartingMaximumMiners : 0;
                if (upgradeSystem == null || upgradeSystem.UpgradeData == null)
                {
                    return baseCapacity;
                }

                MiningUpgradeDefinition definition =
                    upgradeSystem.UpgradeData.GetDefinition(MiningUpgradeType.NpcCapacity);
                long addedCapacity = (long)Mathf.Max(0, Mathf.RoundToInt(definition.ValuePerStack)) *
                                     upgradeSystem.GetStacks(MiningUpgradeType.NpcCapacity);
                return (int)Math.Min(int.MaxValue, baseCapacity + addedCapacity);
            }
        }
        public bool CanBuy => npcData != null && wallet != null && wallet.CurrentMoney >= NpcCost &&
                              oreSpawner != null && npcPrefab != null && purchasedCount < MaximumMiners;
        public event Action<int> NpcCountChanged;
        public event Action<MiningNpc> NpcPurchased;

        private void Awake()
        {
            FindLuckyBlockSystemIfMissing();
            FindProgressionSystemIfMissing();
        }

        private void OnEnable()
        {
            if (upgradeSystem != null)
            {
                upgradeSystem.UpgradesChanged -= HandleUpgradesChanged;
                upgradeSystem.UpgradesChanged += HandleUpgradesChanged;
            }
        }

        private void OnDisable()
        {
            if (upgradeSystem != null)
            {
                upgradeSystem.UpgradesChanged -= HandleUpgradesChanged;
            }
        }

        public bool TryBuyNpc()
        {
            if (!CanBuy || wallet == null || oreSpawner == null || npcPrefab == null ||
                npcData == null || !wallet.TrySpend(NpcCost))
            {
                return false;
            }

            Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;
            if (!TryFindAvailableSpawnPosition(origin, out Vector3 position))
            {
                wallet.AddMoney(NpcCost);
                return false;
            }

            MiningNpc npc = Instantiate(npcPrefab, position, Quaternion.identity);
            if (npc == null)
            {
                wallet.AddMoney(NpcCost);
                return false;
            }

            npc.name = $"Mining NPC {purchasedCount + 1}";
            FindLuckyBlockSystemIfMissing();
            FindProgressionSystemIfMissing();
            npc.Initialize(oreSpawner, npcData, luckyBlockSystem, progressionSystem);
            purchasedCount++;
            NpcCountChanged?.Invoke(purchasedCount);
            NpcPurchased?.Invoke(npc);
            return true;
        }

        /// <summary>Removes every spawned miner and restores the shop count.</summary>
        public void ResetAllNpcs()
        {
            MiningNpc[] npcs = FindObjectsByType<MiningNpc>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (MiningNpc npc in npcs)
            {
                if (npc == null || !npc.gameObject.scene.IsValid())
                {
                    continue;
                }

                // OnDisable releases the reserved ore before the object is destroyed.
                npc.gameObject.SetActive(false);
                Destroy(npc.gameObject);
            }

            purchasedCount = 0;
            NpcCountChanged?.Invoke(purchasedCount);
        }

        private void HandleUpgradesChanged()
        {
            NpcCountChanged?.Invoke(purchasedCount);
        }

        private bool TryFindAvailableSpawnPosition(Vector3 origin, out Vector3 position)
        {
            Vector3 fallback = origin + Vector3.up * npcData.SpawnHeightOffset;
            for (int attempt = 0; attempt < npcData.SpawnAttempts; attempt++)
            {
                Vector2 spread = UnityEngine.Random.insideUnitCircle * npcData.SpawnSpread;
                Vector3 candidate = fallback + new Vector3(spread.x, 0f, spread.y);
                Collider[] overlaps = Physics.OverlapSphere(candidate, npcData.ColliderRadius,
                    npcData.CollisionLayers, QueryTriggerInteraction.Ignore);
                bool occupiedByNpc = false;
                foreach (Collider overlap in overlaps)
                {
                    if (overlap.GetComponentInParent<MiningNpc>() != null)
                    {
                        occupiedByNpc = true;
                        break;
                    }
                }

                if (!occupiedByNpc)
                {
                    position = candidate;
                    return true;
                }
            }

            position = fallback;
            return false;
        }

        private void FindLuckyBlockSystemIfMissing()
        {
            if (luckyBlockSystem == null)
            {
                luckyBlockSystem = FindFirstObjectByType<LuckyBlockDropSystem>(
                    FindObjectsInactive.Include);
            }
        }

        private void FindProgressionSystemIfMissing()
        {
            if (progressionSystem == null)
            {
                progressionSystem = FindFirstObjectByType<NpcProgressionSystem>(
                    FindObjectsInactive.Include);
            }
        }
    }
}
