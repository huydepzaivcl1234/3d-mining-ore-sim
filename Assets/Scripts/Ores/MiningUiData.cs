using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Designer-owned layout and style values used to author the editable mining HUD prefab.</summary>
    [CreateAssetMenu(fileName = "MiningUiData", menuName = "Mining Simulator/Game Data/UI")]
    public sealed class MiningUiData : ScriptableObject
    {
        [Header("Canvas")]
        [SerializeField] private Vector2 referenceResolution = new(1920f, 1080f);
        [Range(0f, 1f), SerializeField] private float matchWidthOrHeight = 0.5f;
        [SerializeField] private int canvasSortingOrder = 100;
        [SerializeField] private bool openUpgradePanelOnPlay;

        [Header("NPC Shop Layout")]
        [SerializeField] private Vector2 shopPanelPosition = new(24f, -24f);
        [SerializeField] private Vector2 shopPanelSize = new(330f, 320f);
        [SerializeField] private Vector2 shopTextSize = new(294f, 32f);
        [SerializeField] private Vector2 shopStatTextSize = new(240f, 32f);
        [SerializeField] private Vector2 moneyTextPosition = new(70f, -70f);
        [SerializeField] private Vector2 npcCountTextPosition = new(70f, -116f);
        [SerializeField] private Vector2 buyButtonPosition = new(18f, -158f);
        [SerializeField] private Vector2 buyButtonSize = new(294f, 54f);
        [SerializeField] private Vector2 statusTextPosition = new(18f, -220f);
        [Min(1f), SerializeField] private float moneyFontSize = 26f;
        [Min(1f), SerializeField] private float npcCountFontSize = 21f;
        [Min(1f), SerializeField] private float buyButtonFontSize = 22f;
        [Min(1f), SerializeField] private float statusFontSize = 17f;
        [SerializeField] private Color shopPanelColor = new(0.035f, 0.045f, 0.06f, 0.94f);
        [SerializeField] private Color shopTextColor = Color.white;
        [SerializeField] private Color statusTextColor = new(1f, 0.82f, 0.28f, 1f);
        [SerializeField] private Color buyButtonColor = new(0.95f, 0.57f, 0.1f, 1f);
        [SerializeField] private Color buyButtonTextColor = new(0.08f, 0.06f, 0.03f, 1f);

        [Header("HUD Icons")]
        [SerializeField] private Vector2 hudIconSize = new(42f, 42f);
        [SerializeField] private Vector2 moneyIconPosition = new(16f, -62f);
        [SerializeField] private Vector2 npcIconPosition = new(16f, -108f);
        [SerializeField] private Vector2 buyButtonIconPosition = new(8f, -6f);
        [SerializeField] private Vector2 openUpgradeIconPosition = new(8f, -2f);
        [Min(1f), SerializeField] private float hudIconFontSize = 22f;
        [Min(0f), SerializeField] private float hudIconPadding = 8f;
        [SerializeField] private Color moneyIconColor = new(1f, 0.72f, 0.08f, 1f);
        [SerializeField] private Color npcIconColor = new(0.20f, 0.65f, 0.94f, 1f);
        [SerializeField] private Color upgradeIconColor = new(0.14f, 0.72f, 0.52f, 1f);
        [SerializeField] private Color iconSymbolColor = Color.white;
        [SerializeField] private Sprite moneyIconSprite;
        [SerializeField] private Sprite npcIconSprite;
        [SerializeField] private Sprite buyNpcIconSprite;
        [SerializeField] private Sprite openUpgradeIconSprite;
        [SerializeField] private string moneyIconFallback = "$";
        [SerializeField] private string npcIconFallback = "N";
        [SerializeField] private string buyNpcIconFallback = "+";
        [SerializeField] private string openUpgradeIconFallback = "UP";

        [Header("Upgrade Card Icons")]
        [SerializeField] private Vector2 upgradeCardIconSize = new(52f, 52f);
        [SerializeField] private Vector2 upgradeCardIconPosition = new(14f, -13f);
        [SerializeField] private Sprite moneyRewardIconSprite;
        [SerializeField] private Sprite rareOreIconSprite;
        [SerializeField] private Sprite oreDamageIconSprite;
        [SerializeField] private Sprite oreSpawnSpeedIconSprite;
        [SerializeField] private Sprite npcMoveSpeedIconSprite;
        [SerializeField] private string moneyRewardIconFallback = "$";
        [SerializeField] private string rareOreIconFallback = "R";
        [SerializeField] private string oreDamageIconFallback = "!";
        [SerializeField] private string oreSpawnSpeedIconFallback = "S";
        [SerializeField] private string npcMoveSpeedIconFallback = ">>";

        [Header("NPC Shop Style")]
        [SerializeField] private string shopTitle = "KHU ĐÀO QUẶNG";
        [SerializeField] private Vector2 shopHeaderSize = new(330f, 54f);
        [Min(1f), SerializeField] private float shopTitleFontSize = 21f;
        [SerializeField] private Color shopHeaderColor = new(0.20f, 0.65f, 0.94f, 1f);

        [Header("Ore Reward Popup")]
        [SerializeField] private string rewardPopupFormat = "+{0}";
        [Min(0), SerializeField] private int rewardPopupPreviewAmount = 100;
        [SerializeField] private Vector3 rewardPopupWorldOffset = new(0f, 0.35f, 0f);
        [Min(0.01f), SerializeField] private float rewardPopupDuration = 1.1f;
        [Min(0f), SerializeField] private float rewardPopupRiseDistance = 1.2f;
        [Min(0.001f), SerializeField] private float rewardPopupWorldScale = 0.18f;
        [Min(1f), SerializeField] private float rewardPopupFontSize = 5f;
        [SerializeField] private Color rewardPopupColor = new(1f, 0.82f, 0.16f, 1f);
        [SerializeField] private Color rewardPopupOutlineColor = new(0.08f, 0.04f, 0.01f, 1f);
        [Range(0f, 1f), SerializeField] private float rewardPopupOutlineWidth = 0.2f;

        [Header("Upgrade Panel Layout")]
        [SerializeField] private Vector2 panelSize = new(720f, 680f);
        [SerializeField] private Vector2 headerSize = new(720f, 82f);
        [SerializeField] private Vector2 cardSize = new(620f, 78f);
        [SerializeField] private Vector2 firstCardPosition = new(50f, -104f);
        [Min(0f), SerializeField] private float cardSpacing = 88f;
        [SerializeField] private Vector2 closeButtonSize = new(58f, 58f);
        [SerializeField] private Vector2 closeButtonPosition = new(678f, -12f);
        [SerializeField] private Vector2 backButtonSize = new(180f, 54f);
        [SerializeField] private Vector2 backButtonPosition = new(270f, -606f);
        [SerializeField] private Vector2 openButtonSize = new(294f, 46f);
        [SerializeField] private Vector2 openButtonPosition = new(18f, -266f);
        [Min(0f), SerializeField] private float outlineThickness = 4f;

        [Header("Upgrade Panel Typography")]
        [Min(1f), SerializeField] private float titleFontSize = 32f;
        [Min(1f), SerializeField] private float cardFontSize = 20f;
        [Min(1f), SerializeField] private float navigationFontSize = 20f;
        [SerializeField] private Vector4 cardTextMargin = new(82f, 0f, 18f, 0f);

        [Header("Upgrade Panel Colors")]
        [SerializeField] private Color panelColor = new(0.93f, 0.96f, 0.98f, 1f);
        [SerializeField] private Color headerColor = new(0.20f, 0.65f, 0.94f, 1f);
        [SerializeField] private Color outlineColor = new(0.025f, 0.055f, 0.09f, 1f);
        [SerializeField] private Color cardColor = Color.white;
        [SerializeField] private Color cardTextColor = new(0.04f, 0.055f, 0.075f, 1f);
        [SerializeField] private Color closeButtonColor = new(0.96f, 0.08f, 0.34f, 1f);
        [SerializeField] private Color navigationButtonColor = new(0.14f, 0.72f, 0.52f, 1f);
        [SerializeField] private Color titleTextColor = Color.white;

        public Vector2 ReferenceResolution => referenceResolution;
        public float MatchWidthOrHeight => matchWidthOrHeight;
        public int CanvasSortingOrder => canvasSortingOrder;
        public bool OpenUpgradePanelOnPlay => openUpgradePanelOnPlay;
        public Vector2 ShopPanelPosition => shopPanelPosition;
        public Vector2 ShopPanelSize => shopPanelSize;
        public Vector2 ShopTextSize => shopTextSize;
        public Vector2 ShopStatTextSize => shopStatTextSize;
        public Vector2 MoneyTextPosition => moneyTextPosition;
        public Vector2 NpcCountTextPosition => npcCountTextPosition;
        public Vector2 BuyButtonPosition => buyButtonPosition;
        public Vector2 BuyButtonSize => buyButtonSize;
        public Vector2 StatusTextPosition => statusTextPosition;
        public float MoneyFontSize => moneyFontSize;
        public float NpcCountFontSize => npcCountFontSize;
        public float BuyButtonFontSize => buyButtonFontSize;
        public float StatusFontSize => statusFontSize;
        public Color ShopPanelColor => shopPanelColor;
        public Color ShopTextColor => shopTextColor;
        public Color StatusTextColor => statusTextColor;
        public Color BuyButtonColor => buyButtonColor;
        public Color BuyButtonTextColor => buyButtonTextColor;
        public Vector2 HudIconSize => hudIconSize;
        public Vector2 MoneyIconPosition => moneyIconPosition;
        public Vector2 NpcIconPosition => npcIconPosition;
        public Vector2 BuyButtonIconPosition => buyButtonIconPosition;
        public Vector2 OpenUpgradeIconPosition => openUpgradeIconPosition;
        public float HudIconFontSize => hudIconFontSize;
        public float HudIconPadding => hudIconPadding;
        public Color MoneyIconColor => moneyIconColor;
        public Color NpcIconColor => npcIconColor;
        public Color UpgradeIconColor => upgradeIconColor;
        public Color IconSymbolColor => iconSymbolColor;
        public Sprite MoneyIconSprite => moneyIconSprite;
        public Sprite NpcIconSprite => npcIconSprite;
        public Sprite BuyNpcIconSprite => buyNpcIconSprite;
        public Sprite OpenUpgradeIconSprite => openUpgradeIconSprite;
        public string MoneyIconFallback => moneyIconFallback;
        public string NpcIconFallback => npcIconFallback;
        public string BuyNpcIconFallback => buyNpcIconFallback;
        public string OpenUpgradeIconFallback => openUpgradeIconFallback;
        public Vector2 UpgradeCardIconSize => upgradeCardIconSize;
        public Vector2 UpgradeCardIconPosition => upgradeCardIconPosition;
        public Sprite MoneyRewardIconSprite => moneyRewardIconSprite;
        public Sprite RareOreIconSprite => rareOreIconSprite;
        public Sprite OreDamageIconSprite => oreDamageIconSprite;
        public Sprite OreSpawnSpeedIconSprite => oreSpawnSpeedIconSprite;
        public Sprite NpcMoveSpeedIconSprite => npcMoveSpeedIconSprite;
        public string MoneyRewardIconFallback => moneyRewardIconFallback;
        public string RareOreIconFallback => rareOreIconFallback;
        public string OreDamageIconFallback => oreDamageIconFallback;
        public string OreSpawnSpeedIconFallback => oreSpawnSpeedIconFallback;
        public string NpcMoveSpeedIconFallback => npcMoveSpeedIconFallback;
        public string ShopTitle => shopTitle;
        public Vector2 ShopHeaderSize => shopHeaderSize;
        public float ShopTitleFontSize => shopTitleFontSize;
        public Color ShopHeaderColor => shopHeaderColor;
        public string RewardPopupFormat => rewardPopupFormat;
        public int RewardPopupPreviewAmount => rewardPopupPreviewAmount;
        public Vector3 RewardPopupWorldOffset => rewardPopupWorldOffset;
        public float RewardPopupDuration => rewardPopupDuration;
        public float RewardPopupRiseDistance => rewardPopupRiseDistance;
        public float RewardPopupWorldScale => rewardPopupWorldScale;
        public float RewardPopupFontSize => rewardPopupFontSize;
        public Color RewardPopupColor => rewardPopupColor;
        public Color RewardPopupOutlineColor => rewardPopupOutlineColor;
        public float RewardPopupOutlineWidth => rewardPopupOutlineWidth;
        public Vector2 PanelSize => panelSize;
        public Vector2 HeaderSize => headerSize;
        public Vector2 CardSize => cardSize;
        public Vector2 FirstCardPosition => firstCardPosition;
        public float CardSpacing => cardSpacing;
        public Vector2 CloseButtonSize => closeButtonSize;
        public Vector2 CloseButtonPosition => closeButtonPosition;
        public Vector2 BackButtonSize => backButtonSize;
        public Vector2 BackButtonPosition => backButtonPosition;
        public Vector2 OpenButtonSize => openButtonSize;
        public Vector2 OpenButtonPosition => openButtonPosition;
        public float OutlineThickness => outlineThickness;
        public float TitleFontSize => titleFontSize;
        public float CardFontSize => cardFontSize;
        public float NavigationFontSize => navigationFontSize;
        public Vector4 CardTextMargin => cardTextMargin;
        public Color PanelColor => panelColor;
        public Color HeaderColor => headerColor;
        public Color OutlineColor => outlineColor;
        public Color CardColor => cardColor;
        public Color CardTextColor => cardTextColor;
        public Color CloseButtonColor => closeButtonColor;
        public Color NavigationButtonColor => navigationButtonColor;
        public Color TitleTextColor => titleTextColor;

        private void OnValidate()
        {
            referenceResolution.x = Mathf.Max(1f, referenceResolution.x);
            referenceResolution.y = Mathf.Max(1f, referenceResolution.y);
            panelSize.x = Mathf.Max(1f, panelSize.x);
            panelSize.y = Mathf.Max(1f, panelSize.y);
            headerSize.x = Mathf.Max(1f, headerSize.x);
            headerSize.y = Mathf.Max(1f, headerSize.y);
            cardSize.x = Mathf.Max(1f, cardSize.x);
            cardSize.y = Mathf.Max(1f, cardSize.y);
            shopStatTextSize.x = Mathf.Max(1f, shopStatTextSize.x);
            shopStatTextSize.y = Mathf.Max(1f, shopStatTextSize.y);
            hudIconSize.x = Mathf.Max(1f, hudIconSize.x);
            hudIconSize.y = Mathf.Max(1f, hudIconSize.y);
            hudIconPadding = Mathf.Max(0f, hudIconPadding);
            upgradeCardIconSize.x = Mathf.Max(1f, upgradeCardIconSize.x);
            upgradeCardIconSize.y = Mathf.Max(1f, upgradeCardIconSize.y);
            cardSpacing = Mathf.Max(0f, cardSpacing);
            outlineThickness = Mathf.Max(0f, outlineThickness);
            shopHeaderSize.x = Mathf.Max(1f, shopHeaderSize.x);
            shopHeaderSize.y = Mathf.Max(1f, shopHeaderSize.y);
            rewardPopupDuration = Mathf.Max(0.01f, rewardPopupDuration);
            rewardPopupPreviewAmount = Mathf.Max(0, rewardPopupPreviewAmount);
            rewardPopupRiseDistance = Mathf.Max(0f, rewardPopupRiseDistance);
            rewardPopupWorldScale = Mathf.Max(0.001f, rewardPopupWorldScale);
            rewardPopupFontSize = Mathf.Max(1f, rewardPopupFontSize);
        }
    }
}
