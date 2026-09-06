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
        [SerializeField] private MiningGameData gameData;

        private int purchasedCount;

        public int NpcCost => gameData != null ? gameData.NpcCost : 0;
        public int PurchasedCount => purchasedCount;
        public bool CanBuy => gameData != null && wallet != null && wallet.CurrentMoney >= NpcCost &&
                              oreSpawner != null && npcPrefab != null;
        public event Action<int> NpcCountChanged;

        public bool TryBuyNpc()
        {
            if (wallet == null || oreSpawner == null || npcPrefab == null ||
                gameData == null || !wallet.TrySpend(NpcCost))
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
            npc.Initialize(oreSpawner, gameData);
            purchasedCount++;
            NpcCountChanged?.Invoke(purchasedCount);
            return true;
        }

        private bool TryFindAvailableSpawnPosition(Vector3 origin, out Vector3 position)
        {
            Vector3 fallback = origin + Vector3.up * gameData.NpcSpawnHeightOffset;
            for (int attempt = 0; attempt < gameData.NpcSpawnAttempts; attempt++)
            {
                Vector2 spread = UnityEngine.Random.insideUnitCircle * gameData.NpcSpawnSpread;
                Vector3 candidate = fallback + new Vector3(spread.x, 0f, spread.y);
                Collider[] overlaps = Physics.OverlapSphere(candidate, gameData.NpcColliderRadius,
                    gameData.NpcCollisionLayers, QueryTriggerInteraction.Ignore);
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
    }
}
