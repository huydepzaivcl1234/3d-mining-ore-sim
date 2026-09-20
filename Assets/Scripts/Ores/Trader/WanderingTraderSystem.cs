using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Creates one stationary civilian trader from the existing humanoid miner prefab. The trader
    /// stays outside the ore spawn area and owns one item-for-currency offer at a time.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WanderingTraderSystem : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private NpcShop npcShop;

        [Header("Humanoid Source")]
        [SerializeField] private MiningNpc humanoidPrefab;
        [Tooltip("Optional trader prefab. It can be any humanoid model; a collider is added automatically if needed.")]
        [SerializeField] private GameObject traderModelPrefab;
        [Tooltip("Optional Animator Controller for the trader model.")]
        [SerializeField] private RuntimeAnimatorController animatorController;
        [SerializeField] private string walkingBoolParameter = "IsWalking";
        [SerializeField] private string speedFloatParameter = "Speed";

        [Header("Merchant Position")]
        [Tooltip("Keep this enabled to place one stationary merchant at Merchant Position.")]
        [SerializeField] private bool useFixedMerchantPosition = true;
        [Tooltip("World position for the stationary merchant. If it is inside the mining area, it is moved just outside it.")]
        [SerializeField] private Vector3 merchantPosition = new(24f, 0f, 24f);

        [Header("Fallback Spawn Area")]
        [Tooltip("Used only when Fixed Merchant Position is disabled. The mining area is excluded.")]
        [SerializeField] private Vector3 wanderAreaCenter;
        [SerializeField] private Vector2 wanderAreaSize = new(64f, 64f);
        [Min(0f), SerializeField] private float miningAreaPadding = 4f;
        [Min(1), SerializeField] private int destinationAttempts = 24;

        [Header("Offers")]
        [Range(1, 8), SerializeField] private int maximumItemsRequested = 3;
        [Range(1, 8), SerializeField] private int offerCount = 3;
        [Min(1f), SerializeField] private float offerRefreshSeconds = 60f;
        [Min(1f), SerializeField] private float baseMoneyValue = 30f;
        [Min(1f), SerializeField] private float baseGemValue = 1f;

        private readonly List<MiningItemData> eligibleItems = new();
        private OreSpawner oreSpawner;
        private MiningItemSystem itemSystem;
        private PlayerWallet wallet;
        private WanderingTraderAgent trader;
        [SerializeField] private WanderingTraderAgent sceneTrader;
        private WanderingTraderPanel panel;
        private readonly List<TraderOffer> currentOffers = new();
        private float nextOfferRefreshTime;
        private bool hasCurrentOffer;
        public IReadOnlyList<TraderOffer> CurrentOffers => currentOffers;
        public float OfferSecondsRemaining => Mathf.Max(0f, nextOfferRefreshTime - Time.time);

        public static void EnsureRuntime(NpcShop shop)
        {
            if (shop == null || FindFirstObjectByType<WanderingTraderSystem>(
                    FindObjectsInactive.Include) != null)
            {
                return;
            }

            WanderingTraderSystem system = shop.gameObject.AddComponent<WanderingTraderSystem>();
            system.Configure(shop);
        }

        /// <summary>Called by the scene setup tool so the authored system exposes all settings.</summary>
        public void Configure(NpcShop shop)
        {
            npcShop = shop;
            humanoidPrefab ??= shop != null ? shop.NpcPrefab : null;
            traderModelPrefab ??= shop != null ? shop.WanderingTraderModelPrefab : null;
            animatorController ??= shop != null ? shop.WanderingTraderAnimatorController : null;
        }

        private void Awake()
        {
            npcShop ??= GetComponent<NpcShop>();
            npcShop ??= FindFirstObjectByType<NpcShop>(FindObjectsInactive.Include);
            humanoidPrefab ??= npcShop != null ? npcShop.NpcPrefab : null;
            traderModelPrefab ??= npcShop != null ? npcShop.WanderingTraderModelPrefab : null;
            animatorController ??= npcShop != null ? npcShop.WanderingTraderAnimatorController : null;
            oreSpawner = FindFirstObjectByType<OreSpawner>(FindObjectsInactive.Include);
            itemSystem = FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
            wallet = FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            ConfigureWanderAreaFromOreSpawner();
        }

        private void Start()
        {
            if (sceneTrader != null)
            {
                trader = sceneTrader;
                trader.Initialize(this);
                RefreshOffer();
                return;
            }
            SpawnTrader();
            RefreshOffer();
        }

        public GameObject TraderModelPrefab => traderModelPrefab;

        public void AssignSceneTrader(WanderingTraderAgent value)
        {
            sceneTrader = value;
        }

        private void Update()
        {
            if (Time.time >= nextOfferRefreshTime)
            {
                RefreshOffer();
                if (panel != null && panel.IsOpen)
                {
                    panel.Show(currentOffers, OfferSecondsRemaining, offer => TryAcceptOffer(offer),
                        () => HandleTradeClosed(trader));
                }
            }
        }

        private void OnDisable()
        {
            panel?.Hide();
        }

        public bool TryOpenTrade(WanderingTraderAgent requestingTrader)
        {
            if (requestingTrader == null || requestingTrader != trader ||
                itemSystem == null || wallet == null || !hasCurrentOffer)
            {
                return false;
            }

            panel ??= WanderingTraderPanel.EnsureRuntime();
            if (panel == null)
            {
                return false;
            }

            panel.Show(currentOffers, OfferSecondsRemaining, offer => TryAcceptOffer(offer),
                () => HandleTradeClosed(trader));
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
                if (!IsPathOutsideMiningArea(currentPosition, candidate, miningBounds))
                {
                    continue;
                }

                destination = SnapToGround(candidate, currentPosition.y);
                return true;
            }

            destination = currentPosition;
            return false;
        }

        public bool IsOutsideMiningArea(Vector3 position)
        {
            return !GetMiningBounds().Contains(position);
        }

        public Vector3 MoveOutsideMiningArea(Vector3 position)
        {
            Bounds miningBounds = GetMiningBounds();
            if (!miningBounds.Contains(position))
            {
                return position;
            }

            float left = Mathf.Abs(position.x - miningBounds.min.x);
            float right = Mathf.Abs(miningBounds.max.x - position.x);
            float bottom = Mathf.Abs(position.z - miningBounds.min.z);
            float top = Mathf.Abs(miningBounds.max.z - position.z);
            float nearest = Mathf.Min(left, right, bottom, top);
            const float safetyGap = 0.5f;

            if (nearest == left)
            {
                position.x = miningBounds.min.x - safetyGap;
            }
            else if (nearest == right)
            {
                position.x = miningBounds.max.x + safetyGap;
            }
            else if (nearest == bottom)
            {
                position.z = miningBounds.min.z - safetyGap;
            }
            else
            {
                position.z = miningBounds.max.z + safetyGap;
            }

            return SnapToGround(position, position.y);
        }

        public void ApplyMovementAnimation(Animator animator, bool moving)
        {
            if (animator == null)
            {
                return;
            }

            if (animatorController != null && animator.runtimeAnimatorController != animatorController)
            {
                animator.runtimeAnimatorController = animatorController;
            }

            SetAnimatorBoolIfPresent(animator, walkingBoolParameter, moving);
            SetAnimatorFloatIfPresent(animator, speedFloatParameter, 0f);
        }

        private void SpawnTrader()
        {
            if (trader != null || (traderModelPrefab == null && humanoidPrefab == null))
            {
                return;
            }

            Vector3 start = GetMerchantSpawnPosition();
            GameObject traderObject = traderModelPrefab != null
                ? Instantiate(traderModelPrefab, start, Quaternion.identity)
                : Instantiate(humanoidPrefab.gameObject, start, Quaternion.identity);
            traderObject.name = "Wandering Trader";

            MiningNpc copiedMiner = traderObject.GetComponent<MiningNpc>();
            if (copiedMiner != null)
            {
                copiedMiner.enabled = false;
            }
            MiningNpcHeadlamp headlamp = traderObject.GetComponent<MiningNpcHeadlamp>();
            if (headlamp != null)
            {
                headlamp.enabled = false;
            }

            Rigidbody body = traderObject.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (traderObject.GetComponent<Collider>() == null)
            {
                CapsuleCollider collider = traderObject.AddComponent<CapsuleCollider>();
                collider.center = new Vector3(0f, 0.9f, 0f);
                collider.height = 1.8f;
                collider.radius = 0.35f;
            }

            trader = traderObject.GetComponent<WanderingTraderAgent>();
            if (trader == null)
            {
                trader = traderObject.AddComponent<WanderingTraderAgent>();
            }
            trader.Initialize(this);
        }

        private Vector3 GetMerchantSpawnPosition()
        {
            if (!useFixedMerchantPosition && TryGetSpawnPosition(out Vector3 randomPosition))
            {
                return randomPosition;
            }

            Vector3 position = SnapToGround(merchantPosition, merchantPosition.y);
            return MoveOutsideMiningArea(position);
        }

        private bool TryCreateOffer(out TraderOffer offer)
        {
            offer = default;
            MiningItemDatabase database = itemSystem != null ? itemSystem.Database : null;
            if (database == null)
            {
                return false;
            }

            // An offer must still be visible when the player owns none of its item yet.
            // Otherwise clicking the trader looks like a failed interaction and it never pauses.
            eligibleItems.Clear();
            foreach (MiningItemData item in database.Items)
            {
                if (item != null)
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
            int amount = UnityEngine.Random.Range(1, maximumItemsRequested + 1);
            bool traderSellsItem = UnityEngine.Random.value < 0.5f;
            bool paysGems = false;
            int rarityStep = (int)requestedItem.Rarity + 1;
            float configuredValue = traderSellsItem
                ? requestedItem.TraderBuyValue
                : requestedItem.TraderSellValue;
            float fallbackValue = traderSellsItem
                ? rarityStep * baseMoneyValue
                : rarityStep * baseMoneyValue;
            float reward = Mathf.Max(1f, Mathf.Round((configuredValue > 0f ? configuredValue : fallbackValue) * amount));
            offer = new TraderOffer(requestedItem, amount, reward, paysGems, traderSellsItem);
            return true;
        }

        private bool TryAcceptOffer(TraderOffer offer)
        {
            if (itemSystem == null || wallet == null)
            {
                return false;
            }

            if (offer.TraderSellsItem)
            {
                if (!wallet.TrySpend(offer.RewardAmount))
                {
                    return false;
                }
                if (!itemSystem.TryAddItem(offer.Item, offer.ItemAmount))
                {
                    wallet.AddMoney(offer.RewardAmount);
                    return false;
                }
            }
            else
            {
                if (!itemSystem.TryRemoveItem(offer.Item, offer.ItemAmount))
                    return false;
                if (offer.PaysGems) wallet.AddGems(offer.RewardAmount);
                else wallet.AddMoney(offer.RewardAmount);
            }
            RefreshOffer();
            return true;
        }

        private void RefreshOffer()
        {
            currentOffers.Clear();
            for (int index = 0; index < Mathf.Max(1, offerCount); index++)
            {
                if (TryCreateOffer(out TraderOffer offer))
                    currentOffers.Add(offer);
            }
            hasCurrentOffer = currentOffers.Count > 0;
            nextOfferRefreshTime = Time.time + Mathf.Max(1f, offerRefreshSeconds);
        }

        private void OnValidate()
        {
            maximumItemsRequested = Mathf.Max(1, maximumItemsRequested);
            offerCount = Mathf.Max(1, offerCount);
            offerRefreshSeconds = Mathf.Max(1f, offerRefreshSeconds);
            baseMoneyValue = Mathf.Max(1f, baseMoneyValue);
            baseGemValue = Mathf.Max(1f, baseGemValue);
            destinationAttempts = Mathf.Max(1, destinationAttempts);
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

        private bool TryGetSpawnPosition(out Vector3 destination)
        {
            Bounds miningBounds = GetMiningBounds();
            for (int attempt = 0; attempt < destinationAttempts; attempt++)
            {
                Vector3 candidate = wanderAreaCenter + new Vector3(
                    UnityEngine.Random.Range(-wanderAreaSize.x * 0.5f, wanderAreaSize.x * 0.5f),
                    0f,
                    UnityEngine.Random.Range(-wanderAreaSize.y * 0.5f, wanderAreaSize.y * 0.5f));
                if (!miningBounds.Contains(candidate))
                {
                    destination = SnapToGround(candidate, wanderAreaCenter.y);
                    return true;
                }
            }

            destination = MoveOutsideMiningArea(wanderAreaCenter);
            return false;
        }

        private static bool IsPathOutsideMiningArea(Vector3 from, Vector3 to, Bounds miningBounds)
        {
            if (miningBounds.Contains(from) || miningBounds.Contains(to))
            {
                return false;
            }

            Vector3 direction = to - from;
            direction.y = 0f;
            float distance = direction.magnitude;
            if (distance <= 0.001f)
            {
                return true;
            }

            return !miningBounds.IntersectRay(new Ray(from, direction / distance), out float hitDistance) ||
                   hitDistance > distance;
        }

        private static void SetAnimatorBoolIfPresent(Animator animator, string parameterName, bool value)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
                {
                    animator.SetBool(parameterName, value);
                    return;
                }
            }
        }

        private static void SetAnimatorFloatIfPresent(Animator animator, string parameterName, float value)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                return;
            }
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == parameterName)
                {
                    animator.SetFloat(parameterName, value);
                    return;
                }
            }
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
                bool paysGems, bool traderSellsItem = false)
            {
                Item = item;
                ItemAmount = itemAmount;
                RewardAmount = rewardAmount;
                PaysGems = paysGems;
                TraderSellsItem = traderSellsItem;
            }

            public MiningItemData Item { get; }
            public int ItemAmount { get; }
            public float RewardAmount { get; }
            public bool PaysGems { get; }
            public bool TraderSellsItem { get; }
        }
    }
}
