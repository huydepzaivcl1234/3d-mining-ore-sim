using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class MiningShopProduct
    {
        [SerializeField] private MiningItemData item;
        [SerializeField] private MiningCosmeticData cosmetic;
        [Min(1), SerializeField] private int itemAmount = 1;
        [Min(0f), SerializeField] private float gemCost = 100f;

        public MiningItemData Item => item;
        public MiningCosmeticData Cosmetic => cosmetic;
        public bool IsCosmetic => cosmetic != null;
        public int ItemAmount => Mathf.Max(1, itemAmount);
        public float GemCost => Mathf.Max(0f, gemCost);
        public bool IsValid => (item != null && ItemAmount > 0) || cosmetic != null;
        public string DisplayName => cosmetic != null ? cosmetic.DisplayName : item != null ? item.DisplayName : string.Empty;
        public string Description => cosmetic != null ? cosmetic.Description : item != null ? item.Description : string.Empty;
        public Sprite Icon => cosmetic != null ? cosmetic.Icon : item != null ? item.InventoryIcon : null;
        public string IconFallback => cosmetic != null ? cosmetic.IconFallback : item != null ? item.IconFallback : "?";
        public Color FallbackColor => cosmetic != null ? cosmetic.FallbackColor : item != null ? item.FallbackColor : Color.white;

        public void Validate()
        {
            itemAmount = Mathf.Max(1, itemAmount);
            gemCost = Mathf.Max(0f, gemCost);
        }
    }

    public enum MiningShopWheelRewardType
    {
        Money = 0,
        Gems = 1,
        Item = 2
    }

    [Serializable]
    public sealed class MiningShopWheelReward
    {
        [SerializeField] private MiningShopWheelRewardType rewardType;
        [Tooltip("Relative weight. Entries are normalized automatically and do not need to total 100.")]
        [Min(0f), SerializeField] private float weight = 1f;
        [Tooltip("Gold or Gem amount. Ignored for Item rewards.")]
        [Min(0f), SerializeField] private float currencyAmount = 100f;
        [SerializeField] private MiningItemData item;
        [Min(1), SerializeField] private int itemAmount = 1;
        [SerializeField] private string englishName;
        [SerializeField] private string vietnameseName;
        [SerializeField] private Sprite icon;
        [SerializeField] private Color wheelColor = new(0.31f, 0.85f, 1f, 1f);

        public MiningShopWheelRewardType RewardType => rewardType;
        public float Weight => Mathf.Max(0f, weight);
        public float CurrencyAmount => Mathf.Max(0f, currencyAmount);
        public MiningItemData Item => item;
        public int ItemAmount => Mathf.Max(1, itemAmount);
        public Sprite Icon => icon != null ? icon : item != null ? item.InventoryIcon : null;
        public Color WheelColor => wheelColor;
        public bool IsValid => Weight > 0f &&
                               (rewardType == MiningShopWheelRewardType.Item
                                   ? item != null && ItemAmount > 0
                                   : CurrencyAmount > 0f);

        public string GetDisplayName()
        {
            string configured = string.IsNullOrWhiteSpace(englishName) &&
                                string.IsNullOrWhiteSpace(vietnameseName)
                ? string.Empty
                : MiningLocalization.Text(englishName, vietnameseName);
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            return rewardType switch
            {
                MiningShopWheelRewardType.Money => string.Format(
                    MiningLocalization.Text("{0} GOLD"),
                    MiningMoneyFormatter.Format(CurrencyAmount)),
                MiningShopWheelRewardType.Gems => string.Format(
                    MiningLocalization.Text("{0} GEMS"),
                    MiningMoneyFormatter.Format(CurrencyAmount)),
                _ => item != null ? $"{item.DisplayName} x{ItemAmount}" : string.Empty
            };
        }
    }

    /// <summary>Designer-owned products and prices for the Gem shop.</summary>
    [CreateAssetMenu(fileName = "MiningShopData",
        menuName = "Mining Simulator/Game Data/Shop")]
    public sealed class MiningShopData : ScriptableObject
    {
        [Header("Shop Products")]
        [Tooltip("Add, remove or reorder any gift/item sold for Gems. The Shop UI syncs from this list.")]
        [SerializeField] private List<MiningShopProduct> products = new();

        [HideInInspector, SerializeField] private int productSchemaVersion;
        [HideInInspector, SerializeField] private MiningItemData rareGiftBox;
        [HideInInspector, Min(0f), SerializeField] private float rareGiftBoxGemCost = 100f;

        /* Legacy fields are kept serialized so existing Shop data remains compatible. */

        [Header("Lucky Wheel")]
        [Min(0f), SerializeField] private float singleSpinGemCost = 10f;
        [Range(1, 100), SerializeField] private int multiSpinCount = 10;
        [Min(0f), SerializeField] private float multiSpinGemCost = 100f;
        [Min(0.1f), SerializeField] private float wheelSpinDurationSeconds = 2.4f;
        [Range(1, 12), SerializeField] private int wheelSpinRotations = 6;
        [Tooltip("Relative weights are normalized automatically. Add, remove or reorder rewards freely.")]
        [SerializeField] private List<MiningShopWheelReward> wheelRewards = new();

        public MiningItemData RareGiftBox => rareGiftBox;
        public float RareGiftBoxGemCost => Mathf.Max(0f, rareGiftBoxGemCost);
        public IReadOnlyList<MiningShopProduct> Products => products ??
            (IReadOnlyList<MiningShopProduct>)Array.Empty<MiningShopProduct>();
        public float SingleSpinGemCost => Mathf.Max(0f, singleSpinGemCost);
        public int MultiSpinCount => Mathf.Clamp(multiSpinCount, 1, 100);
        public float MultiSpinGemCost => Mathf.Max(0f, multiSpinGemCost);
        public float WheelSpinDurationSeconds => Mathf.Max(0.1f, wheelSpinDurationSeconds);
        public int WheelSpinRotations => Mathf.Clamp(wheelSpinRotations, 1, 12);
        public IReadOnlyList<MiningShopWheelReward> WheelRewards => wheelRewards;

        public bool TryRollWheelReward(out int rewardIndex, out MiningShopWheelReward reward)
        {
            rewardIndex = -1;
            reward = null;
            float totalWeight = 0f;
            foreach (MiningShopWheelReward candidate in wheelRewards)
            {
                if (candidate != null && candidate.IsValid)
                {
                    totalWeight += candidate.Weight;
                }
            }
            if (totalWeight <= 0f)
            {
                return false;
            }

            float roll = UnityEngine.Random.value * totalWeight;
            for (int index = 0; index < wheelRewards.Count; index++)
            {
                MiningShopWheelReward candidate = wheelRewards[index];
                if (candidate == null || !candidate.IsValid)
                {
                    continue;
                }
                roll -= candidate.Weight;
                if (roll <= 0f)
                {
                    rewardIndex = index;
                    reward = candidate;
                    return true;
                }
            }
            return false;
        }

        public float GetWheelRewardDisplayPercent(MiningShopWheelReward reward)
        {
            if (reward == null || !reward.IsValid)
            {
                return 0f;
            }
            float totalWeight = 0f;
            foreach (MiningShopWheelReward candidate in wheelRewards)
            {
                if (candidate != null && candidate.IsValid)
                {
                    totalWeight += candidate.Weight;
                }
            }
            return totalWeight > 0f ? reward.Weight / totalWeight * 100f : 0f;
        }

        private void OnValidate()
        {
            products ??= new List<MiningShopProduct>();
            foreach (MiningShopProduct product in products)
            {
                product?.Validate();
            }
            rareGiftBoxGemCost = Mathf.Max(0f, rareGiftBoxGemCost);
            singleSpinGemCost = Mathf.Max(0f, singleSpinGemCost);
            multiSpinCount = Mathf.Clamp(multiSpinCount, 1, 100);
            multiSpinGemCost = Mathf.Max(0f, multiSpinGemCost);
            wheelSpinDurationSeconds = Mathf.Max(0.1f, wheelSpinDurationSeconds);
            wheelSpinRotations = Mathf.Clamp(wheelSpinRotations, 1, 12);
        }
    }
}
