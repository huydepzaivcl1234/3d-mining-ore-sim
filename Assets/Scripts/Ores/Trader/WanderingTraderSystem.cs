using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Creates one stationary civilian trader with rotating buy offers and an inventory-driven
    /// sell-for-Gem page. The trader stays outside the ore spawn area.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WanderingTraderSystem : MonoBehaviour
    {
        private const int RequiredOfferCount = 3;

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
        // Preserved for existing Scene serialization; the shop now always displays three per page.
        [SerializeField, HideInInspector] private int offerCount = RequiredOfferCount;
        [Min(1f), SerializeField] private float offerRefreshSeconds = 60f;
        [Min(1f), SerializeField] private float baseMoneyValue = 30f;
        [Min(1f), SerializeField] private float baseGemValue = 1f;

        private readonly List<MiningItemData> eligibleItems = new();
        private readonly List<MiningItemData> sellCandidates = new();
        private OreSpawner oreSpawner;
        private MiningItemSystem itemSystem;
        private PlayerWallet wallet;
        private WanderingTraderAgent trader;
        [SerializeField] private WanderingTraderAgent sceneTrader;
        private WanderingTraderPanel panel;
        private readonly List<TraderOffer> currentOffers = new();
        private readonly List<TraderOffer> currentSellOffers = new();
        private float nextOfferRefreshTime;
        private bool hasCurrentOffer;
        public IReadOnlyList<TraderOffer> CurrentOffers => currentOffers;
        public IReadOnlyList<TraderOffer> CurrentSellOffers => currentSellOffers;
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
                    panel.Show(currentOffers, currentSellOffers, OfferSecondsRemaining, CanAcceptOffer,
                        TryAcceptOffer, () => HandleTradeClosed(trader));
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

            panel.Show(currentOffers, currentSellOffers, OfferSecondsRemaining, CanAcceptOffer,
                TryAcceptOffer, () => HandleTradeClosed(trader));
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

        private bool TryCreateBuyOffer(MiningItemData item, out TraderOffer offer)
        {
            offer = default;
            if (item == null || !item.TraderCanBuy)
            {
                return false;
            }

            int amount = RollOfferAmount(item);
            TraderCurrency currency = UnityEngine.Random.value < 0.5f
                ? TraderCurrency.Coin
                : TraderCurrency.Gem;
            int rarityStep = (int)item.Rarity + 1;
            float price;
            if (currency == TraderCurrency.Coin)
            {
                float fallback = rarityStep * baseMoneyValue;
                price = RollPrice(item.TraderBuyValue, item.TraderCoinBuyMaximum, fallback);
            }
            else
            {
                float fallback = rarityStep * baseGemValue;
                float perItem = RollPrice(item.TraderGemBuyValue,
                    item.TraderGemBuyMaximum, fallback);
                price = perItem * amount;
            }
            price = Mathf.Max(1f, Mathf.Round(price));
            offer = new TraderOffer(item, amount, price, currency, TraderOfferType.BuyItem);
            return true;
        }

        private int RollOfferAmount(MiningItemData item, int owned = int.MaxValue)
        {
            int maximum = Mathf.Min(maximumItemsRequested,
                item.TraderMaximumOfferAmount, owned);
            int minimum = Mathf.Min(item.TraderMinimumOfferAmount, maximum);
            return UnityEngine.Random.Range(Mathf.Max(1, minimum), Mathf.Max(1, maximum) + 1);
        }

        private static float RollPrice(float minimum, float maximum, float fallback)
        {
            if (minimum <= 0f && maximum <= 0f)
            {
                return fallback;
            }

            minimum = Mathf.Max(0f, minimum);
            maximum = Mathf.Max(minimum, maximum);
            return UnityEngine.Random.Range(minimum, maximum);
        }

        private bool CanAcceptOffer(TraderOffer offer)
        {
            if (itemSystem == null || wallet == null || offer.Item == null)
            {
                return false;
            }

            if (offer.OfferType == TraderOfferType.BuyItem)
            {
                bool hasCurrency = offer.Currency == TraderCurrency.Gem
                    ? wallet.CurrentGems >= offer.Price
                    : wallet.CurrentMoney >= offer.Price;
                return hasCurrency &&
                       itemSystem.CanAddItem(offer.Item, offer.ItemAmount);
            }

            return itemSystem.GetItemCount(offer.Item) >= offer.ItemAmount;
        }

        private bool TryAcceptOffer(TraderOffer offer)
        {
            if (itemSystem == null || wallet == null)
            {
                return false;
            }

            if (offer.OfferType == TraderOfferType.BuyItem)
            {
                bool spent = offer.Currency == TraderCurrency.Gem
                    ? wallet.TrySpendGems(offer.Price)
                    : wallet.TrySpend(offer.Price);
                if (!spent)
                {
                    return false;
                }
                if (!itemSystem.TryAddItem(offer.Item, offer.ItemAmount))
                {
                    if (offer.Currency == TraderCurrency.Gem) wallet.AddGems(offer.Price);
                    else wallet.AddMoney(offer.Price);
                    return false;
                }
            }
            else
            {
                if (!itemSystem.TryRemoveItem(offer.Item, offer.ItemAmount))
                    return false;
                wallet.AddGems(offer.Price);
            }
            return true;
        }

        private void RefreshOffer()
        {
            currentOffers.Clear();
            currentSellOffers.Clear();
            MiningItemDatabase database = itemSystem != null ? itemSystem.Database : null;
            if (database == null)
            {
                hasCurrentOffer = false;
                nextOfferRefreshTime = Time.time + Mathf.Max(1f, offerRefreshSeconds);
                return;
            }

            eligibleItems.Clear();
            foreach (MiningItemData item in database.Items)
            {
                if (item == null || !item.TraderCanBuy) continue;
                eligibleItems.Add(item);
            }
            Shuffle(eligibleItems);

            for (int index = 0; index < RequiredOfferCount && eligibleItems.Count > 0; index++)
            {
                MiningItemData item = eligibleItems[index % eligibleItems.Count];
                if (TryCreateBuyOffer(item, out TraderOffer offer))
                    currentOffers.Add(offer);
            }

            RefreshSellOffers();
            hasCurrentOffer = currentOffers.Count > 0 || currentSellOffers.Count > 0;
            nextOfferRefreshTime = Time.time + Mathf.Max(1f, offerRefreshSeconds);
        }

        private void RefreshSellOffers()
        {
            currentSellOffers.Clear();
            MiningItemDatabase database = itemSystem != null ? itemSystem.Database : null;
            if (database == null) return;
            sellCandidates.Clear();
            foreach (MiningItemData item in database.Items)
            {
                if (item != null && item.TraderCanSell)
                    sellCandidates.Add(item);
            }
            Shuffle(sellCandidates);
            for (int index = 0; index < RequiredOfferCount && sellCandidates.Count > 0; index++)
            {
                MiningItemData item = sellCandidates[index % sellCandidates.Count];
                int owned = itemSystem.GetItemCount(item);
                int amount = owned > 0
                    ? RollOfferAmount(item, owned)
                    : RollOfferAmount(item);
                int rarityStep = (int)item.Rarity + 1;
                float value = RollPrice(item.TraderSellValue,
                    item.TraderGemSellMaximum, rarityStep * baseGemValue);
                float gems = Mathf.Max(1f, Mathf.Round(value * amount));
                currentSellOffers.Add(new TraderOffer(item, amount, gems,
                    TraderCurrency.Gem, TraderOfferType.SellItem));
            }
        }

        private static void Shuffle(List<MiningItemData> items)
        {
            for (int index = items.Count - 1; index > 0; index--)
            {
                int other = UnityEngine.Random.Range(0, index + 1);
                MiningItemData temporary = items[index];
                items[index] = items[other];
                items[other] = temporary;
            }
        }

        private void OnValidate()
        {
            maximumItemsRequested = Mathf.Max(1, maximumItemsRequested);
            offerCount = RequiredOfferCount;
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

        public enum TraderCurrency
        {
            Coin = 0,
            Gem = 1
        }

        public enum TraderOfferType
        {
            BuyItem = 0,
            SellItem = 1
        }

        public readonly struct TraderOffer
        {
            public TraderOffer(MiningItemData item, int itemAmount, float price,
                TraderCurrency currency, TraderOfferType offerType)
            {
                Item = item;
                ItemAmount = itemAmount;
                Price = price;
                Currency = currency;
                OfferType = offerType;
            }

            public MiningItemData Item { get; }
            public int ItemAmount { get; }
            public float Price { get; }
            public TraderCurrency Currency { get; }
            public TraderOfferType OfferType { get; }
        }
    }
}
