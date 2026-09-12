using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum MiningItemRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    public enum MiningItemEffectType
    {
        NpcDamage = 0,
        MoneyReward = 1,
        NpcMoveSpeed = 2
    }

    public enum MiningItemUseType
    {
        TimedEffect = 0,
        GiftBox = 1
    }

    public enum MiningGiftRewardType
    {
        Money = 0,
        Item = 1
    }

    [Serializable]
    public sealed class MiningGiftReward
    {
        [SerializeField] private MiningGiftRewardType rewardType;
        [Min(0f), SerializeField] private float chancePercent = 1f;
        [Min(0f), SerializeField] private float moneyAmount = 1000f;
        [SerializeField] private MiningItemData item;
        [Min(1), SerializeField] private int itemAmount = 1;
        [SerializeField] private Color wheelColor = Color.white;

        public MiningGiftRewardType RewardType => rewardType;
        public float ChancePercent => chancePercent;
        public float MoneyAmount => moneyAmount;
        public MiningItemData Item => item;
        public int ItemAmount => Mathf.Max(1, itemAmount);
        public Color WheelColor => wheelColor;

        public bool IsValid => chancePercent > 0f &&
                               (rewardType == MiningGiftRewardType.Money
                                   ? moneyAmount > 0f
                                   : item != null && itemAmount > 0);

        public string GetDisplayName()
        {
            return rewardType == MiningGiftRewardType.Money
                ? string.Format(MiningLocalization.Text("{0} GOLD", "{0} VÀNG"),
                    MiningMoneyFormatter.Format(moneyAmount))
                : item != null ? $"{item.DisplayName} x{ItemAmount}" : string.Empty;
        }
    }

    /// <summary>Designer-owned identity, drop selection and timed effect for one consumable.</summary>
    [CreateAssetMenu(fileName = "MiningItem", menuName = "Mining Simulator/Game Data/Item")]
    public sealed class MiningItemData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemId = "item";
        [SerializeField] private string displayName = "Vật phẩm";
        [TextArea, SerializeField] private string description;
        [SerializeField] private MiningItemRarity rarity = MiningItemRarity.Common;

        [Header("Presentation")]
        [Tooltip("Optional world prefab or FBX. Leave empty to use the colored fallback model.")]
        [SerializeField] private GameObject worldModel;
        [Tooltip("Optional inventory icon. A text symbol is shown when this is empty.")]
        [SerializeField] private Sprite inventoryIcon;
        [SerializeField] private string iconFallback = "?";
        [SerializeField] private Color fallbackColor = Color.white;
        [Min(0.01f), SerializeField] private float worldScale = 0.42f;
        [SerializeField] private Vector3 modelLocalPosition;
        [SerializeField] private Vector3 modelLocalEulerAngles;

        [Header("Drop And Stack")]
        [Tooltip("Relative chance inside the item table after a successful source drop roll.")]
        [Range(0f, 100f), SerializeField] private float selectionChancePercent = 33.33f;
        [Range(1, 64), SerializeField] private int maximumStack = 64;

        [Header("Use")]
        [SerializeField] private MiningItemUseType useType = MiningItemUseType.TimedEffect;

        [Header("Timed Effect")]
        [SerializeField] private MiningItemEffectType effectType;
        [Min(0f), SerializeField] private float effectPercent = 25f;
        [Min(0.1f), SerializeField] private float effectDurationSeconds = 30f;

        [Header("Gift Box")]
        [Tooltip("Relative reward chances. Values are normalized automatically and do not need to total 100.")]
        [SerializeField] private List<MiningGiftReward> giftRewards = new();
        [Min(0.1f), SerializeField] private float giftSpinDurationSeconds = 3.5f;
        [Range(1, 12), SerializeField] private int giftSpinRotations = 6;

        public string ItemId => itemId;
        public string DisplayName => MiningLocalization.GetItemName(itemId, displayName);
        public string Description => MiningLocalization.GetItemDescription(itemId, description);
        public MiningItemRarity Rarity => rarity;
        public GameObject WorldModel => worldModel;
        public Sprite InventoryIcon => inventoryIcon;
        public string IconFallback => iconFallback;
        public Color FallbackColor => fallbackColor;
        public float WorldScale => worldScale;
        public Vector3 ModelLocalPosition => modelLocalPosition;
        public Vector3 ModelLocalEulerAngles => modelLocalEulerAngles;
        public float SelectionChancePercent => selectionChancePercent;
        public int MaximumStack => maximumStack;
        public MiningItemUseType UseType => useType;
        public MiningItemEffectType EffectType => effectType;
        public float EffectPercent => effectPercent;
        public float EffectDurationSeconds => effectDurationSeconds;
        public IReadOnlyList<MiningGiftReward> GiftRewards => giftRewards;
        public float GiftSpinDurationSeconds => giftSpinDurationSeconds;
        public int GiftSpinRotations => giftSpinRotations;

        public string EffectName => MiningLocalization.GetEffectName(effectType, false);
        public string ShortEffectName => MiningLocalization.GetEffectName(effectType, true);

        public string GetEffectSummary()
        {
            return $"{EffectName} +{effectPercent:0.##}% / {effectDurationSeconds:0.#}s";
        }

        public string GetInventorySummary()
        {
            return useType == MiningItemUseType.GiftBox
                ? MiningLocalization.Text("OPEN TO SPIN", "MỞ ĐỂ QUAY")
                : $"{ShortEffectName} +{effectPercent:0.##}%";
        }

        public bool TryRollGiftReward(out int rewardIndex, out MiningGiftReward reward)
        {
            rewardIndex = -1;
            reward = null;
            float total = 0f;
            foreach (MiningGiftReward candidate in giftRewards)
            {
                if (candidate != null && candidate.IsValid)
                {
                    total += candidate.ChancePercent;
                }
            }
            if (total <= 0f)
            {
                return false;
            }

            float roll = UnityEngine.Random.value * total;
            for (int index = 0; index < giftRewards.Count; index++)
            {
                MiningGiftReward candidate = giftRewards[index];
                if (candidate == null || !candidate.IsValid)
                {
                    continue;
                }
                roll -= candidate.ChancePercent;
                if (roll <= 0f)
                {
                    rewardIndex = index;
                    reward = candidate;
                    return true;
                }
            }

            return false;
        }

        public float GetGiftRewardDisplayPercent(MiningGiftReward reward)
        {
            if (reward == null || !reward.IsValid)
            {
                return 0f;
            }
            float total = 0f;
            foreach (MiningGiftReward candidate in giftRewards)
            {
                if (candidate != null && candidate.IsValid)
                {
                    total += candidate.ChancePercent;
                }
            }
            return total > 0f ? reward.ChancePercent / total * 100f : 0f;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                itemId = name;
            }
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = name;
            }
            maximumStack = Mathf.Clamp(maximumStack, 1, 64);
            selectionChancePercent = Mathf.Clamp(selectionChancePercent, 0f, 100f);
            effectPercent = Mathf.Max(0f, effectPercent);
            effectDurationSeconds = Mathf.Max(0.1f, effectDurationSeconds);
            giftSpinDurationSeconds = Mathf.Max(0.1f, giftSpinDurationSeconds);
            giftSpinRotations = Mathf.Clamp(giftSpinRotations, 1, 12);
            worldScale = Mathf.Max(0.01f, worldScale);
        }
    }
}
