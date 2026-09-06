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
        [Min(0), SerializeField] private int npcCost = 25;
        [Min(0f), SerializeField] private float spawnSpread = 1.25f;
        [SerializeField] private float spawnHeightOffset = 0.9f;

        private int purchasedCount;

        public int NpcCost => npcCost;
        public int PurchasedCount => purchasedCount;
        public bool CanBuy => wallet != null && wallet.CurrentMoney >= npcCost &&
                              oreSpawner != null && npcPrefab != null;
        public event Action<int> NpcCountChanged;

        public bool TryBuyNpc()
        {
            if (wallet == null || oreSpawner == null || npcPrefab == null ||
                !wallet.TrySpend(npcCost))
            {
                return false;
            }

            Vector2 spread = UnityEngine.Random.insideUnitCircle * spawnSpread;
            Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;
            Vector3 position = origin + new Vector3(spread.x, spawnHeightOffset, spread.y);
            MiningNpc npc = Instantiate(npcPrefab, position, Quaternion.identity);
            if (npc == null)
            {
                wallet.AddMoney(npcCost);
                return false;
            }

            npc.name = $"Mining NPC {purchasedCount + 1}";
            npc.Initialize(oreSpawner);
            purchasedCount++;
            NpcCountChanged?.Invoke(purchasedCount);
            return true;
        }

        private void OnValidate()
        {
            npcCost = Mathf.Max(0, npcCost);
            spawnSpread = Mathf.Max(0f, spawnSpread);
        }
    }
}
