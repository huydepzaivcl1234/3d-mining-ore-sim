using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Creates one stationary civilian trader with rotating buy offers and an inventory-driven
    /// sell-for-Gem page. Placement follows the authored merchant or wander area.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WanderingTraderSystem : MonoBehaviour
    {
        private const int RequiredOfferCount = 3;

        [Header("Scene References")]

        [Header("Humanoid Source")]
        [Tooltip("Optional trader prefab. It can be any humanoid model; a collider is added automatically if needed.")]
        [SerializeField] private GameObject traderModelPrefab;
        [Tooltip("Optional Animator Controller for the trader model.")]
        [SerializeField] private RuntimeAnimatorController animatorController;
        [SerializeField] private string walkingBoolParameter = "IsWalking";
        [SerializeField] private string speedFloatParameter = "Speed";

        [Header("Merchant Position")]
        [Tooltip("Keep this enabled to place one stationary merchant at Merchant Position.")]
        [SerializeField] private bool useFixedMerchantPosition = true;
        [Tooltip("World position for the stationary merchant.")]
        [SerializeField] private Vector3 merchantPosition = new(24f, 0f, 24f);

        [Header("Fallback Spawn Area")]
        [Tooltip("Used only when Fixed Merchant Position is disabled.")]
        [SerializeField] private Vector3 wanderAreaCenter;
        [SerializeField] private Vector2 wanderAreaSize = new(64f, 64f);
        [Min(1), SerializeField] private int destinationAttempts = 24;

        [Header("Offers")]
        [Range(1, 8), SerializeField] private int maximumItemsRequested = 3;
        // Preserved for existing Scene serialization; the shop now always displays three per page.
        [SerializeField, HideInInspector] private int offerCount = RequiredOfferCount;
        [Min(1f), SerializeField] private float offerRefreshSeconds = 60f;
        [Min(1f), SerializeField] private float baseMoneyValue = 30f;
        [Min(1f), SerializeField] private float baseGemValue = 1f;

        [Header("Offer Stock Per Restock")]
        [Tooltip("Maximum number of times a Common offer can be bought or sold before restocking.")]
        [Min(1), SerializeField] private int commonStock = 8;
        [Min(1), SerializeField] private int uncommonStock = 6;
        [Min(1), SerializeField] private int rareStock = 4;
        [Min(1), SerializeField] private int epicStock = 2;
        [Min(1), SerializeField] private int legendaryStock = 1;

        private readonly List<MiningItemData> eligibleItems = new();
        private readonly List<MiningItemData> sellCandidates = new();
        private MiningItemSystem itemSystem;
        private PlayerWallet wallet;
        private WanderingTraderAgent trader;
        private bool traderWasActive;
        [SerializeField] private WanderingTraderAgent sceneTrader;
        private WanderingTraderPanel panel;
        private readonly List<TraderOffer> currentOffers = new();
        private readonly List<TraderOffer> currentSellOffers = new();
        private float nextOfferRefreshTime;
        private bool hasCurrentOffer;
        public IReadOnlyList<TraderOffer> CurrentOffers => currentOffers;
        public IReadOnlyList<TraderOffer> CurrentSellOffers => currentSellOffers;
        public float OfferSecondsRemaining => Mathf.Max(0f, nextOfferRefreshTime - Time.time);



        /// <summary>Called by the scene setup tool so the authored system exposes all settings.</summary>


        private void Awake()
        {
            itemSystem = FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
            wallet = FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
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
            // A runtime trader is instantiated outside this system's hierarchy.
            // Hide the trader when its owning system is disabled.
            if (trader != null)
            {
                traderWasActive = trader.gameObject.activeSelf;
                trader.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (trader != null) trader.gameObject.SetActive(traderWasActive);
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
            for (int attempt = 0; attempt < destinationAttempts; attempt++)
            {
                Vector3 candidate = wanderAreaCenter + new Vector3(
                    UnityEngine.Random.Range(-wanderAreaSize.x * 0.5f, wanderAreaSize.x * 0.5f),
                    0f,
                    UnityEngine.Random.Range(-wanderAreaSize.y * 0.5f, wanderAreaSize.y * 0.5f));


                destination = SnapToGround(candidate, currentPosition.y);
                return true;
            }

            destination = currentPosition;
            return false;
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
            if (trader != null || traderModelPrefab == null)
            {
                return;
            }

            Vector3 start = GetMerchantSpawnPosition();
            GameObject traderObject = Instantiate(traderModelPrefab, start, Quaternion.identity);
            traderObject.name = "Wandering Trader";




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
            return position;
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
            offer = new TraderOffer(item, amount, price, currency,
                TraderOfferType.BuyItem, GetStockForRarity(item.Rarity));
            return true;
        }

        private int GetStockForRarity(MiningItemRarity rarity)
        {
            return rarity switch
            {
                MiningItemRarity.Legendary => legendaryStock,
                MiningItemRarity.Epic => epicStock,
                MiningItemRarity.Rare => rareStock,
                MiningItemRarity.Uncommon => uncommonStock,
                _ => commonStock
            };
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
            if (itemSystem == null || wallet == null || offer == null ||
                offer.Item == null || offer.IsOutOfStock)
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
            if (!CanAcceptOffer(offer))
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
            return offer.TryConsumeStock();
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
                    TraderCurrency.Gem, TraderOfferType.SellItem,
                    GetStockForRarity(item.Rarity)));
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
            legendaryStock = Mathf.Max(1, legendaryStock);
            epicStock = Mathf.Max(legendaryStock + 1, epicStock);
            rareStock = Mathf.Max(epicStock + 1, rareStock);
            uncommonStock = Mathf.Max(rareStock + 1, uncommonStock);
            commonStock = Mathf.Max(uncommonStock + 1, commonStock);
            destinationAttempts = Mathf.Max(1, destinationAttempts);
        }





        private bool TryGetSpawnPosition(out Vector3 destination)
        {
            for (int attempt = 0; attempt < destinationAttempts; attempt++)
            {
                Vector3 candidate = wanderAreaCenter + new Vector3(
                    UnityEngine.Random.Range(-wanderAreaSize.x * 0.5f, wanderAreaSize.x * 0.5f),
                    0f,
                    UnityEngine.Random.Range(-wanderAreaSize.y * 0.5f, wanderAreaSize.y * 0.5f));
                destination = SnapToGround(candidate, wanderAreaCenter.y);
                return true;
            }

            destination = SnapToGround(wanderAreaCenter, wanderAreaCenter.y);
            return false;
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

        public sealed class TraderOffer
        {
            public TraderOffer(MiningItemData item, int itemAmount, float price,
                TraderCurrency currency, TraderOfferType offerType, int maximumStock)
            {
                Item = item;
                ItemAmount = itemAmount;
                Price = price;
                Currency = currency;
                OfferType = offerType;
                MaximumStock = Mathf.Max(1, maximumStock);
                RemainingStock = MaximumStock;
            }

            public MiningItemData Item { get; }
            public int ItemAmount { get; }
            public float Price { get; }
            public TraderCurrency Currency { get; }
            public TraderOfferType OfferType { get; }
            public int MaximumStock { get; }
            public int RemainingStock { get; private set; }
            public bool IsOutOfStock => RemainingStock <= 0;

            public bool TryConsumeStock()
            {
                if (IsOutOfStock)
                {
                    return false;
                }

                RemainingStock--;
                return true;
            }
        }
    }
}
