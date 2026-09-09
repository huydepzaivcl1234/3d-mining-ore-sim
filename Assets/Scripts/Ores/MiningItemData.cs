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

        [Header("Timed Effect")]
        [SerializeField] private MiningItemEffectType effectType;
        [Min(0f), SerializeField] private float effectPercent = 25f;
        [Min(0.1f), SerializeField] private float effectDurationSeconds = 30f;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
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
        public MiningItemEffectType EffectType => effectType;
        public float EffectPercent => effectPercent;
        public float EffectDurationSeconds => effectDurationSeconds;

        public string EffectName => effectType switch
        {
            MiningItemEffectType.NpcDamage => "Sát thương NPC",
            MiningItemEffectType.MoneyReward => "Vàng nhận được",
            MiningItemEffectType.NpcMoveSpeed => "Tốc chạy NPC",
            _ => "Hiệu ứng"
        };
        public string ShortEffectName => effectType switch
        {
            MiningItemEffectType.NpcDamage => "DMG NPC",
            MiningItemEffectType.MoneyReward => "VÀNG",
            MiningItemEffectType.NpcMoveSpeed => "TỐC NPC",
            _ => "BUFF"
        };

        public string GetEffectSummary()
        {
            return $"{EffectName} +{effectPercent:0.##}% / {effectDurationSeconds:0.#}s";
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
            worldScale = Mathf.Max(0.01f, worldScale);
        }
    }
}
