using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Creates one civilian trader from the existing humanoid miner prefab. The trader is kept
    /// outside the ore spawn area, walks between random ground positions, and owns the current
    /// item-for-currency offer. This component is added at runtime by NpcShop so no scene or
    /// Shop UI layout needs to be changed for the feature to start working.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WanderingTraderSystem : MonoBehaviour
    {
        [Header("Humanoid Source")]
        [SerializeField] private MiningNpc humanoidPrefab;

        [Header("Wander Area")]
        [Tooltip("Horizontal area where the trader can walk. The mining area is excluded.")]
        [SerializeField] private Vector3 wanderAreaCenter;
        [SerializeField] private Vector2 wanderAreaSize = new(64f, 64f);
        [Min(0f), SerializeField] private float miningAreaPadding = 4f;
        [Min(0.1f), SerializeField] private float wanderSpeed = 2f;
        [SerializeField] private Vector2 waitSeconds = new(1.5f, 4f);
        [Min(1), SerializeField] private int destinationAttempts = 24;

        [Header("Offers")]
        [Range(1, 8), SerializeField] private int maximumItemsRequested = 3;
        [Min(1f), SerializeField] private float moneyPerRarityStep = 30f;
        [Min(1f), SerializeField] private float gemsPerRarityStep = 1f;

        private readonly List<MiningItemData> eligibleItems = new();
        private NpcShop npcShop;
        private OreSpawner oreSpawner;
        private MiningItemSystem itemSystem;
        private PlayerWallet wallet;
        private WanderingTraderAgent trader;
        private WanderingTraderPanel panel;

        public static void EnsureRuntime(NpcShop shop)
        {
            if (shop == null || shop.GetComponent<WanderingTraderSystem>() != null)
            {
                return;
            }

            WanderingTraderSystem system = shop.gameObject.AddComponent<WanderingTraderSystem>();
            system.npcShop = shop;
            system.humanoidPrefab = shop.NpcPrefab;
        }

        private void Awake()
        {
            npcShop ??= GetComponent<NpcShop>();
            humanoidPrefab ??= npcShop != null ? npcShop.NpcPrefab : null;
            oreSpawner = FindFirstObjectByType<OreSpawner>(FindObjectsInactive.Include);
            itemSystem = FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
            wallet = FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            ConfigureWanderAreaFromOreSpawner();
        }

        private void Start()
        {
            SpawnTrader();
        }

        private void OnDisable()
        {
            panel?.Hide();
        }

        public bool TryOpenTrade(WanderingTraderAgent requestingTrader)
        {
            if (requestingTrader == null || requestingTrader != trader ||
                itemSystem == null || wallet == null || !TryCreateOffer(out TraderOffer offer))
            {
                return false;
            }

            panel ??= WanderingTraderPanel.EnsureRuntime();
            if (panel == null)
            {
                return false;
            }

            panel.Show(offer, () => TryAcceptOffer(offer), () => HandleTradeClosed(trader));
            trader.SetTrading(true);
            return true;
        }

        public void HandleTradeClosed(WanderingTraderAgent requestingTrader)
        {
            if (requestingTrader == trader)
            {
                trader.SetTrading(false);
            }
        }

        public bool TryGetDestination(Vector3 currentPosition, out Vector3 destination)
        {
            Bounds miningBounds = GetMiningBounds();
            for (int attempt = 0; attempt < destinationAttempts; attempt++)
            {
                Vector3 candidate = wanderAreaCenter + new Vector3(
                    UnityEngine.Random.Range(-wanderAreaSize.x * 0.5f, wanderAreaSize.x * 0.5f),
                    0f,
                    UnityEngine.Random.Range(-wanderAreaSize.y * 0.5f, wanderAreaSize.y * 0.5f));
                if (miningBounds.Contains(candidate))
                {
                    continue;
                }

                destination = SnapToGround(candidate, currentPosition.y);
                return true;
            }

            destination = currentPosition;
            return false;
        }

        public float WanderSpeed => wanderSpeed;
        public Vector2 WaitSeconds => waitSeconds;

        private void SpawnTrader()
        {
            if (trader != null || humanoidPrefab == null)
            {
                return;
            }

            Vector3 start = wanderAreaCenter;
            TryGetDestination(start, out start);
            MiningNpc copiedMiner = Instantiate(humanoidPrefab, start, Quaternion.identity);
            copiedMiner.name = "Wandering Trader";
            copiedMiner.enabled = false;
            MiningNpcHeadlamp headlamp = copiedMiner.GetComponent<MiningNpcHeadlamp>();
            if (headlamp != null)
            {
                headlamp.enabled = false;
            }

            Rigidbody body = copiedMiner.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            trader = copiedMiner.gameObject.AddComponent<WanderingTraderAgent>();
            trader.Initialize(this);
        }

        private bool TryCreateOffer(out TraderOffer offer)
        {
            offer = default;
            MiningItemDatabase database = itemSystem != null ? itemSystem.Database : null;
            if (database == null)
            {
                return false;
            }

            eligibleItems.Clear();
            foreach (MiningItemData item in database.Items)
            {
                if (item != null && itemSystem.GetItemCount(item) > 0)
                {
                    eligibleItems.Add(item);
                }
            }
            if (eligibleItems.Count == 0)
            {
                return false;
            }

            MiningItemData requestedItem = eligibleItems[UnityEngine.Random.Range(0,
                eligibleItems.Count)];
            int amount = UnityEngine.Random.Range(1, Mathf.Min(maximumItemsRequested,
                itemSystem.GetItemCount(requestedItem)) + 1);
            bool paysGems = UnityEngine.Random.value < 0.5f;
            int rarityStep = (int)requestedItem.Rarity + 1;
            float reward = paysGems
                ? Mathf.Max(1f, Mathf.Round(rarityStep * gemsPerRarityStep * amount))
                : Mathf.Max(1f, Mathf.Round(rarityStep * moneyPerRarityStep * amount));
            offer = new TraderOffer(requestedItem, amount, reward, paysGems);
            return true;
        }

        private bool TryAcceptOffer(TraderOffer offer)
        {
            if (itemSystem == null || wallet == null || !itemSystem.TryRemoveItem(offer.Item,
                offer.ItemAmount))
            {
                return false;
            }

            if (offer.PaysGems)
            {
                wallet.AddGems(offer.RewardAmount);
            }
            else
            {
                wallet.AddMoney(offer.RewardAmount);
            }
            return true;
        }

        private void ConfigureWanderAreaFromOreSpawner()
        {
            OreSpawnData data = oreSpawner != null ? oreSpawner.SpawnData : null;
            if (data == null)
            {
                return;
            }

            if (wanderAreaCenter == Vector3.zero)
            {
                wanderAreaCenter = data.AreaCenter;
            }
            wanderAreaSize.x = Mathf.Max(wanderAreaSize.x, data.AreaSize.x + 24f);
            wanderAreaSize.y = Mathf.Max(wanderAreaSize.y, data.AreaSize.z + 24f);
        }

        private Bounds GetMiningBounds()
        {
            OreSpawnData data = oreSpawner != null ? oreSpawner.SpawnData : null;
            Vector3 center = data != null ? data.AreaCenter : wanderAreaCenter;
            Vector3 size = data != null ? data.AreaSize : new Vector3(20f, 0f, 20f);
            size.x += miningAreaPadding * 2f;
            size.z += miningAreaPadding * 2f;
            size.y = 1000f;
            return new Bounds(center, size);
        }

        private static Vector3 SnapToGround(Vector3 position, float fallbackY)
        {
            if (Physics.Raycast(position + Vector3.up * 50f, Vector3.down, out RaycastHit hit,
                    120f, ~0, QueryTriggerInteraction.Ignore))
            {
                position.y = hit.point.y;
            }
            else
            {
                position.y = fallbackY;
            }
            return position;
        }

        public readonly struct TraderOffer
        {
            public TraderOffer(MiningItemData item, int itemAmount, float rewardAmount,
                bool paysGems)
            {
                Item = item;
                ItemAmount = itemAmount;
                RewardAmount = rewardAmount;
                PaysGems = paysGems;
            }

            public MiningItemData Item { get; }
            public int ItemAmount { get; }
            public float RewardAmount { get; }
            public bool PaysGems { get; }
        }
    }
}
