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
        [SerializeField] private Vector2 upgradeCardIconSize = new(86f, 86f);
        [SerializeField] private Vector2 upgradeCardIconPosition = new(22f, -25f);
        [Min(0f), SerializeField] private float upgradeCardIconPadding = 1f;
        [SerializeField] private Sprite moneyRewardIconSprite;
        [SerializeField] private Sprite rareOreIconSprite;
        [SerializeField] private Sprite oreDamageIconSprite;
        [SerializeField] private Sprite oreSpawnSpeedIconSprite;
        [SerializeField] private Sprite npcMoveSpeedIconSprite;
        [SerializeField] private Sprite npcCapacityIconSprite;
        [SerializeField] private Sprite luckyBlockRewardIconSprite;
        [SerializeField] private Sprite luckyBlockDropChanceIconSprite;
        [SerializeField] private string moneyRewardIconFallback = "$";
        [SerializeField] private string rareOreIconFallback = "R";
        [SerializeField] private string oreDamageIconFallback = "!";
        [SerializeField] private string oreSpawnSpeedIconFallback = "S";
        [SerializeField] private string npcMoveSpeedIconFallback = ">>";
        [SerializeField] private string npcCapacityIconFallback = "+1";
        [SerializeField] private string luckyBlockRewardIconFallback = "L$";
        [SerializeField] private string luckyBlockDropChanceIconFallback = "L%";
        [SerializeField] private Sprite npcExperienceIconSprite;
        [SerializeField] private string npcExperienceIconFallback = "XP";
        [SerializeField] private Sprite itemDropChanceIconSprite;
        [SerializeField] private string itemDropChanceIconFallback = "I%";

        [Header("NPC Progress HUD")]
        [SerializeField] private Vector2 npcProgressHudPosition = new(370f, -24f);
        [SerializeField] private Vector2 npcProgressHudSize = new(380f, 170f);
        [SerializeField] private Vector2 npcProgressHeaderIconPosition = new(10f, -4f);
        [SerializeField] private Vector2 npcProgressHeaderIconSize = new(42f, 42f);
        [SerializeField] private Vector4 npcProgressTitleMargin = new(58f, 0f, 14f, 0f);
        [SerializeField] private Vector2 npcProgressTextPosition = new(18f, -14f);
        [SerializeField] private Vector2 npcProgressTextSize = new(344f, 30f);
        [SerializeField] private Vector2 npcPowerTextPosition = new(18f, -50f);
        [SerializeField] private Vector2 npcPowerTextSize = new(344f, 30f);
        [SerializeField] private Vector2 npcExperienceBarPosition = new(18f, -94f);
        [SerializeField] private Vector2 npcExperienceBarSize = new(344f, 28f);
        [SerializeField] private Vector2 npcExperienceTextPosition = new(18f, -126f);
        [SerializeField] private Vector2 npcExperienceTextSize = new(344f, 24f);
        [Min(1f), SerializeField] private float npcProgressTitleFontSize = 21f;
        [Min(1f), SerializeField] private float npcProgressInfoFontSize = 17f;
        [Min(0.01f), SerializeField] private float npcExperienceBarAnimationSpeed = 2.5f;
        [SerializeField] private Color npcProgressPanelColor = new(0.93f, 0.96f, 0.98f, 0.98f);
        [SerializeField] private Color npcExperienceBarColor = new(0.12f, 0.9f, 0.22f, 1f);
        [SerializeField] private Color npcExperienceBarBackgroundColor = new(0.18f, 0.22f, 0.24f, 1f);

        [Header("Smooth Button Animation")]
        [SerializeField] private bool smoothButtonAnimationEnabled = true;
        [Min(1f), SerializeField] private float buttonHoverScale = 1.035f;
        [Min(1f), SerializeField] private float buttonHoverPunchScale = 1.075f;
        [Range(0.5f, 1f), SerializeField] private float buttonPressedScale = 0.96f;
        [Min(1f), SerializeField] private float buttonClickBounceScale = 1.08f;
        [Min(0.01f), SerializeField] private float buttonHoverPunchDuration = 0.08f;
        [Min(0.01f), SerializeField] private float buttonHoverSettleDuration = 0.10f;
        [Min(0.01f), SerializeField] private float buttonPressDuration = 0.06f;
        [Min(0.01f), SerializeField] private float buttonClickBounceDuration = 0.09f;
        [Min(0.01f), SerializeField] private float buttonClickSettleDuration = 0.12f;

        [Header("Audio Menu Layout")]
        [SerializeField] private Vector2 audioMenuButtonPosition = new(-24f, -214f);
        [SerializeField] private Vector2 audioMenuButtonSize = new(180f, 48f);
        [SerializeField] private Vector2 audioPanelSize = new(560f, 420f);
        [SerializeField] private Vector2 audioHeaderSize = new(560f, 74f);
        [SerializeField] private Vector2 audioSliderSize = new(300f, 28f);
        [SerializeField] private Vector2 audioFirstRowPosition = new(42f, -118f);
        [Min(0f), SerializeField] private float audioRowSpacing = 86f;
        [SerializeField] private Vector2 audioLabelSize = new(130f, 34f);
        [SerializeField] private Vector2 audioValueSize = new(72f, 34f);
        [Min(0f), SerializeField] private float audioColumnSpacing = 12f;
        [Min(0f), SerializeField] private float audioHandleExtraSize = 8f;
        [SerializeField] private Vector2 audioCloseButtonPosition = new(510f, -10f);
        [SerializeField] private Vector2 audioCloseButtonSize = new(48f, 48f);
        [Min(1f), SerializeField] private float audioTitleFontSize = 30f;
        [Min(1f), SerializeField] private float audioLabelFontSize = 22f;
        [SerializeField] private Color audioPanelColor = new(0.93f, 0.96f, 0.98f, 1f);
        [SerializeField] private Color audioHeaderColor = new(0.20f, 0.65f, 0.94f, 1f);
        [SerializeField] private Color audioSliderColor = new(0.14f, 0.72f, 0.52f, 1f);
        [SerializeField] private Color audioSliderBackgroundColor = new(0.15f, 0.18f, 0.22f, 1f);

        [Header("Reset Data Button")]
        [SerializeField] private Vector2 resetDataButtonPosition = new(130f, -354f);
        [SerializeField] private Vector2 resetDataButtonSize = new(300f, 46f);
        [Min(1f), SerializeField] private float resetDataFontSize = 19f;
        [Min(0.1f), SerializeField] private float resetDataConfirmationDuration = 3f;
        [SerializeField] private Color resetDataButtonColor = new(0.88f, 0.12f, 0.18f, 1f);
        [SerializeField] private Color resetDataArmedColor = new(1f, 0.36f, 0.08f, 1f);

        [Header("Language Button")]
        [SerializeField] private Vector2 languageButtonPosition = new(20f, -354f);
        [SerializeField] private Vector2 languageButtonSize = new(100f, 46f);
        [Min(8f), SerializeField] private float languageButtonFontSize = 14f;
        [SerializeField] private Color languageButtonColor = new(0.2f, 0.65f, 0.94f, 1f);

        [Header("Inventory Layout")]
        [SerializeField] private Vector2 inventoryMenuButtonPosition = new(-24f, -278f);
        [SerializeField] private Vector2 inventoryMenuButtonSize = new(180f, 48f);
        [SerializeField] private Vector2 inventoryPanelSize = new(930f, 590f);
        [SerializeField] private Vector2 inventoryHeaderSize = new(930f, 72f);
        [SerializeField] private Vector2 inventoryCloseButtonPosition = new(872f, -8f);
        [SerializeField] private Vector2 inventoryCloseButtonSize = new(48f, 48f);
        [SerializeField] private Vector2 inventoryFirstSlotPosition = new(35f, -94f);
        [SerializeField] private Vector2 inventorySlotSize = new(96f, 104f);
        [SerializeField] private Vector2 inventorySlotSpacing = new(114f, 116f);
        [Min(1f), SerializeField] private float inventoryItemFontSize = 15f;
        [Min(1f), SerializeField] private float inventoryCountFontSize = 17f;
        [SerializeField] private Color inventoryPanelColor = new(0.93f, 0.96f, 0.98f, 1f);
        [SerializeField] private Color inventoryHeaderColor = new(0.48f, 0.24f, 0.75f, 1f);
        [SerializeField] private Color inventorySlotColor = new(0.12f, 0.15f, 0.20f, 1f);

        [Header("Gift Box Wheel")]
        [SerializeField] private Vector2 giftWheelPanelSize = new(720f, 620f);
        [SerializeField] private Vector2 giftWheelHeaderSize = new(720f, 72f);
        [SerializeField] private Vector2 giftWheelSize = new(360f, 360f);
        [SerializeField] private Vector2 giftWheelRewardSize = new(150f, 58f);
        [Min(1f), SerializeField] private float giftWheelRewardRadius = 142f;
        [SerializeField] private Vector2 giftWheelSpinButtonSize = new(260f, 56f);
        [SerializeField] private Color giftWheelPanelColor = new(0.06f, 0.075f, 0.11f, 0.98f);
        [SerializeField] private Color giftWheelHeaderColor = new(0.48f, 0.24f, 0.75f, 1f);
        [SerializeField] private Color giftWheelSpinButtonColor = new(1f, 0.62f, 0.08f, 1f);

        [Header("Active Effect Toast")]
        [SerializeField] private Vector2 effectToastPosition = new(0f, -190f);
        [SerializeField] private Vector2 effectToastSize = new(620f, 120f);
        [Min(1f), SerializeField] private float effectToastFontSize = 19f;
        [SerializeField] private Color effectToastColor = new(0.08f, 0.10f, 0.14f, 0.94f);
        [SerializeField] private Vector2 effectToastSlotSize = new(78f, 78f);
        [Min(0f), SerializeField] private float effectToastSlotSpacing = 10f;
        [Min(0f), SerializeField] private float effectToastIconPadding = 7f;
        [Min(1f), SerializeField] private float effectToastTimerFontSize = 16f;
        [Min(1f), SerializeField] private float effectToastPercentFontSize = 13f;
        [SerializeField] private Color effectToastSlotColor = new(0.10f, 0.11f, 0.13f, 0.96f);

        [Header("Panel Slide Animation")]
        [Min(0.01f), SerializeField] private float panelTransitionDuration = 0.28f;
        [Min(0f), SerializeField] private float panelSlideExtraDistance = 80f;
        [Tooltip("Direction used when the NPC shop leaves the screen.")]
        [SerializeField] private Vector2 shopSlideDirection = Vector2.left;
        [Tooltip("Direction used when the Rebirth HUD leaves the screen.")]
        [SerializeField] private Vector2 rebirthHudSlideDirection = Vector2.up;
        [Tooltip("Direction used when the audio menu button leaves the screen.")]
        [SerializeField] private Vector2 audioMenuSlideDirection = Vector2.right;
        [Tooltip("Direction used when the inventory menu button leaves the screen.")]
        [SerializeField] private Vector2 inventoryMenuSlideDirection = Vector2.right;
        [Tooltip("Direction used when the NPC Progress HUD leaves the screen.")]
        [SerializeField] private Vector2 npcProgressHudSlideDirection = Vector2.up;
        [Tooltip("Direction used when a modal panel opens and closes.")]
        [SerializeField] private Vector2 modalSlideDirection = Vector2.down;

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
        [SerializeField] private bool upgradePanelFullscreen = true;
        [Min(1), SerializeField] private int upgradeCardColumns = 2;
        [Min(0f), SerializeField] private float upgradeCardColumnSpacing = 920f;
        [SerializeField] private Vector2 panelSize = new(1920f, 1080f);
        [SerializeField] private Vector2 headerSize = new(1920f, 82f);
        [SerializeField] private Vector2 cardSize = new(820f, 136f);
        [SerializeField] private Vector2 firstCardPosition = new(90f, -110f);
        [Min(0f), SerializeField] private float cardSpacing = 154f;
        [SerializeField] private Vector2 closeButtonSize = new(58f, 58f);
        [SerializeField] private Vector2 closeButtonPosition = new(1848f, -12f);
        [SerializeField] private Vector2 backButtonSize = new(220f, 54f);
        [SerializeField] private Vector2 backButtonPosition = new(850f, -990f);
        [Tooltip("Minimum panel height used when all Lucky Block upgrade cards are present.")]
        [Min(1f), SerializeField] private float expandedUpgradePanelMinimumHeight = 960f;
        [Tooltip("Back button Y position used when all Lucky Block upgrade cards are present.")]
        [SerializeField] private float expandedUpgradeBackButtonY = -878f;
        [Tooltip("Minimum panel height when the NPC experience upgrade card is present.")]
        [Min(1f), SerializeField] private float experienceUpgradePanelMinimumHeight = 1060f;
        [SerializeField] private float experienceUpgradeBackButtonY = -978f;
        [SerializeField] private Vector2 openButtonSize = new(294f, 46f);
        [SerializeField] private Vector2 openButtonPosition = new(18f, -266f);
        [Min(0f), SerializeField] private float outlineThickness = 4f;

        [Header("Upgrade Panel Typography")]
        [Min(1f), SerializeField] private float titleFontSize = 32f;
        [Min(1f), SerializeField] private float cardFontSize = 22f;
        [Min(1f), SerializeField] private float navigationFontSize = 20f;
        [SerializeField] private Vector4 cardTextMargin = new(132f, 0f, 24f, 0f);

        [Header("Upgrade Panel Colors")]
        [SerializeField] private Color panelColor = new(0.93f, 0.96f, 0.98f, 1f);
        [SerializeField] private Color headerColor = new(0.20f, 0.65f, 0.94f, 1f);
        [SerializeField] private Color outlineColor = new(0.025f, 0.055f, 0.09f, 1f);
        [SerializeField] private Color cardColor = Color.white;
        [SerializeField] private Color cardTextColor = new(0.04f, 0.055f, 0.075f, 1f);
        [SerializeField] private Color closeButtonColor = new(0.96f, 0.08f, 0.34f, 1f);
        [SerializeField] private Color navigationButtonColor = new(0.14f, 0.72f, 0.52f, 1f);
        [SerializeField] private Color titleTextColor = Color.white;

        [Header("Rebirth HUD Layout")]
        [SerializeField] private Vector2 rebirthHudPosition = new(-24f, -24f);
        [SerializeField] private Vector2 rebirthHudSize = new(380f, 172f);
        [SerializeField] private Vector2 rebirthHudHeaderSize = new(380f, 50f);
        [SerializeField] private Vector2 rebirthBoostPosition = new(20f, -56f);
        [SerializeField] private Vector2 rebirthBoostSize = new(340f, 24f);
        [SerializeField] private Vector2 rebirthProgressPosition = new(20f, -84f);
        [SerializeField] private Vector2 rebirthProgressSize = new(340f, 28f);
        [SerializeField] private Vector2 rebirthOpenButtonPosition = new(20f, -126f);
        [SerializeField] private Vector2 rebirthOpenButtonSize = new(340f, 34f);
        [Min(1f), SerializeField] private float rebirthTitleFontSize = 23f;
        [Min(1f), SerializeField] private float rebirthInfoFontSize = 17f;

        [Header("Rebirth Confirmation Layout")]
        [SerializeField] private Vector2 rebirthModalSize = new(620f, 480f);
        [SerializeField] private Vector2 rebirthModalHeaderSize = new(620f, 78f);
        [SerializeField] private Vector2 rebirthWarningPosition = new(45f, -112f);
        [SerializeField] private Vector2 rebirthWarningSize = new(530f, 150f);
        [SerializeField] private Vector2 rebirthNextBoostPosition = new(45f, -270f);
        [SerializeField] private Vector2 rebirthNextBoostSize = new(530f, 50f);
        [SerializeField] private Vector2 rebirthConfirmButtonPosition = new(55f, -382f);
        [SerializeField] private Vector2 rebirthCancelButtonPosition = new(325f, -382f);
        [SerializeField] private Vector2 rebirthModalButtonSize = new(240f, 58f);
        [Min(1f), SerializeField] private float rebirthWarningFontSize = 26f;
        [Min(1f), SerializeField] private float rebirthModalTextFontSize = 22f;

        [Header("Rebirth Colors")]
        [SerializeField] private Color rebirthHudColor = new(0.96f, 0.98f, 1f, 0.96f);
        [SerializeField] private Color rebirthHeaderColor = new(1f, 0.15f, 0.18f, 1f);
        [SerializeField] private Color rebirthProgressColor = new(0.12f, 0.92f, 0.18f, 1f);
        [SerializeField] private Color rebirthProgressGhostColor = new(0.55f, 1f, 0.58f, 1f);
        [SerializeField] private Color rebirthConfirmColor = new(0.12f, 0.95f, 0.16f, 1f);
        [SerializeField] private Color rebirthCancelColor = new(1f, 0.12f, 0.18f, 1f);

        [Header("Rebirth Screen Flash")]
        [SerializeField] private Color rebirthFlashColor = Color.white;
        [Min(0.01f), SerializeField] private float rebirthFlashFadeInDuration = 0.18f;
        [Min(0f), SerializeField] private float rebirthFlashHoldDuration = 0.08f;
        [Min(0.01f), SerializeField] private float rebirthFlashFadeOutDuration = 0.35f;

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
        public float UpgradeCardIconPadding => upgradeCardIconPadding;
        public Sprite MoneyRewardIconSprite => moneyRewardIconSprite;
        public Sprite RareOreIconSprite => rareOreIconSprite;
        public Sprite OreDamageIconSprite => oreDamageIconSprite;
        public Sprite OreSpawnSpeedIconSprite => oreSpawnSpeedIconSprite;
        public Sprite NpcMoveSpeedIconSprite => npcMoveSpeedIconSprite;
        public Sprite NpcCapacityIconSprite => npcCapacityIconSprite;
        public Sprite LuckyBlockRewardIconSprite => luckyBlockRewardIconSprite;
        public Sprite LuckyBlockDropChanceIconSprite => luckyBlockDropChanceIconSprite;
        public Sprite NpcExperienceIconSprite => npcExperienceIconSprite;
        public Sprite ItemDropChanceIconSprite => itemDropChanceIconSprite;
        public string MoneyRewardIconFallback => moneyRewardIconFallback;
        public string RareOreIconFallback => rareOreIconFallback;
        public string OreDamageIconFallback => oreDamageIconFallback;
        public string OreSpawnSpeedIconFallback => oreSpawnSpeedIconFallback;
        public string NpcMoveSpeedIconFallback => npcMoveSpeedIconFallback;
        public string NpcCapacityIconFallback => npcCapacityIconFallback;
        public string LuckyBlockRewardIconFallback => string.IsNullOrWhiteSpace(
            luckyBlockRewardIconFallback) ? "L$" : luckyBlockRewardIconFallback;
        public string LuckyBlockDropChanceIconFallback => string.IsNullOrWhiteSpace(
            luckyBlockDropChanceIconFallback) ? "L%" : luckyBlockDropChanceIconFallback;
        public string NpcExperienceIconFallback => string.IsNullOrWhiteSpace(
            npcExperienceIconFallback) ? "XP" : npcExperienceIconFallback;
        public string ItemDropChanceIconFallback => string.IsNullOrWhiteSpace(
            itemDropChanceIconFallback) ? "I%" : itemDropChanceIconFallback;
        public Vector2 NpcProgressHudPosition => npcProgressHudPosition;
        public Vector2 NpcProgressHudSize => npcProgressHudSize;
        public Vector2 NpcProgressHeaderIconPosition => npcProgressHeaderIconPosition;
        public Vector2 NpcProgressHeaderIconSize => npcProgressHeaderIconSize;
        public Vector4 NpcProgressTitleMargin => npcProgressTitleMargin;
        public Vector2 NpcProgressTextPosition => npcProgressTextPosition;
        public Vector2 NpcProgressTextSize => npcProgressTextSize;
        public Vector2 NpcPowerTextPosition => npcPowerTextPosition;
        public Vector2 NpcPowerTextSize => npcPowerTextSize;
        public Vector2 NpcExperienceBarPosition => npcExperienceBarPosition;
        public Vector2 NpcExperienceBarSize => npcExperienceBarSize;
        public Vector2 NpcExperienceTextPosition => npcExperienceTextPosition;
        public Vector2 NpcExperienceTextSize => npcExperienceTextSize;
        public float NpcProgressTitleFontSize => npcProgressTitleFontSize;
        public float NpcProgressInfoFontSize => npcProgressInfoFontSize;
        public float NpcExperienceBarAnimationSpeed => npcExperienceBarAnimationSpeed;
        public Color NpcProgressPanelColor => npcProgressPanelColor;
        public Color NpcExperienceBarColor => npcExperienceBarColor;
        public Color NpcExperienceBarBackgroundColor => npcExperienceBarBackgroundColor;
        public bool SmoothButtonAnimationEnabled => smoothButtonAnimationEnabled;
        public float ButtonHoverScale => buttonHoverScale;
        public float ButtonHoverPunchScale => buttonHoverPunchScale;
        public float ButtonPressedScale => buttonPressedScale;
        public float ButtonClickBounceScale => buttonClickBounceScale;
        public float ButtonHoverPunchDuration => buttonHoverPunchDuration;
        public float ButtonHoverSettleDuration => buttonHoverSettleDuration;
        public float ButtonPressDuration => buttonPressDuration;
        public float ButtonClickBounceDuration => buttonClickBounceDuration;
        public float ButtonClickSettleDuration => buttonClickSettleDuration;
        public Vector2 AudioMenuButtonPosition => audioMenuButtonPosition;
        public Vector2 AudioMenuButtonSize => audioMenuButtonSize;
        public Vector2 AudioPanelSize => audioPanelSize;
        public Vector2 AudioHeaderSize => audioHeaderSize;
        public Vector2 AudioSliderSize => audioSliderSize;
        public Vector2 AudioFirstRowPosition => audioFirstRowPosition;
        public float AudioRowSpacing => audioRowSpacing;
        public Vector2 AudioLabelSize => audioLabelSize;
        public Vector2 AudioValueSize => audioValueSize;
        public float AudioColumnSpacing => audioColumnSpacing;
        public float AudioHandleExtraSize => audioHandleExtraSize;
        public Vector2 AudioCloseButtonPosition => audioCloseButtonPosition;
        public Vector2 AudioCloseButtonSize => audioCloseButtonSize;
        public float AudioTitleFontSize => audioTitleFontSize;
        public float AudioLabelFontSize => audioLabelFontSize;
        public Color AudioPanelColor => audioPanelColor;
        public Color AudioHeaderColor => audioHeaderColor;
        public Color AudioSliderColor => audioSliderColor;
        public Color AudioSliderBackgroundColor => audioSliderBackgroundColor;
        public Vector2 ResetDataButtonPosition => resetDataButtonPosition;
        public Vector2 ResetDataButtonSize => resetDataButtonSize;
        public float ResetDataFontSize => resetDataFontSize;
        public float ResetDataConfirmationDuration => resetDataConfirmationDuration;
        public Color ResetDataButtonColor => resetDataButtonColor;
        public Color ResetDataArmedColor => resetDataArmedColor;
        public Vector2 LanguageButtonPosition => languageButtonPosition;
        public Vector2 LanguageButtonSize => languageButtonSize;
        public float LanguageButtonFontSize => languageButtonFontSize;
        public Color LanguageButtonColor => languageButtonColor;
        public Vector2 InventoryMenuButtonPosition => inventoryMenuButtonPosition;
        public Vector2 InventoryMenuButtonSize => inventoryMenuButtonSize;
        public Vector2 InventoryPanelSize => inventoryPanelSize;
        public Vector2 InventoryHeaderSize => inventoryHeaderSize;
        public Vector2 InventoryCloseButtonPosition => inventoryCloseButtonPosition;
        public Vector2 InventoryCloseButtonSize => inventoryCloseButtonSize;
        public Vector2 InventoryFirstSlotPosition => inventoryFirstSlotPosition;
        public Vector2 InventorySlotSize => inventorySlotSize;
        public Vector2 InventorySlotSpacing => inventorySlotSpacing;
        public float InventoryItemFontSize => inventoryItemFontSize;
        public float InventoryCountFontSize => inventoryCountFontSize;
        public Color InventoryPanelColor => inventoryPanelColor;
        public Color InventoryHeaderColor => inventoryHeaderColor;
        public Color InventorySlotColor => inventorySlotColor;
        public Vector2 GiftWheelPanelSize => giftWheelPanelSize;
        public Vector2 GiftWheelHeaderSize => giftWheelHeaderSize;
        public Vector2 GiftWheelSize => giftWheelSize;
        public Vector2 GiftWheelRewardSize => giftWheelRewardSize;
        public float GiftWheelRewardRadius => giftWheelRewardRadius;
        public Vector2 GiftWheelSpinButtonSize => giftWheelSpinButtonSize;
        public Color GiftWheelPanelColor => giftWheelPanelColor;
        public Color GiftWheelHeaderColor => giftWheelHeaderColor;
        public Color GiftWheelSpinButtonColor => giftWheelSpinButtonColor;
        public Vector2 EffectToastPosition => effectToastPosition;
        public Vector2 EffectToastSize => effectToastSize;
        public float EffectToastFontSize => effectToastFontSize;
        public Color EffectToastColor => effectToastColor;
        public Vector2 EffectToastSlotSize => effectToastSlotSize;
        public float EffectToastSlotSpacing => effectToastSlotSpacing;
        public float EffectToastIconPadding => effectToastIconPadding;
        public float EffectToastTimerFontSize => effectToastTimerFontSize;
        public float EffectToastPercentFontSize => effectToastPercentFontSize;
        public Color EffectToastSlotColor => effectToastSlotColor;
        public float PanelTransitionDuration => panelTransitionDuration;
        public float PanelSlideExtraDistance => panelSlideExtraDistance;
        public Vector2 ShopSlideDirection => shopSlideDirection;
        public Vector2 RebirthHudSlideDirection => rebirthHudSlideDirection;
        public Vector2 AudioMenuSlideDirection => audioMenuSlideDirection;
        public Vector2 InventoryMenuSlideDirection => inventoryMenuSlideDirection;
        public Vector2 NpcProgressHudSlideDirection => npcProgressHudSlideDirection;
        public Vector2 ModalSlideDirection => modalSlideDirection;
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
        public bool UpgradePanelFullscreen => upgradePanelFullscreen;
        public int UpgradeCardColumns => Mathf.Max(1, upgradeCardColumns);
        public float UpgradeCardColumnSpacing => Mathf.Max(0f, upgradeCardColumnSpacing);
        public Vector2 HeaderSize => headerSize;
        public Vector2 CardSize => cardSize;
        public Vector2 FirstCardPosition => firstCardPosition;
        public float CardSpacing => cardSpacing;
        public Vector2 CloseButtonSize => closeButtonSize;
        public Vector2 CloseButtonPosition => closeButtonPosition;
        public Vector2 BackButtonSize => backButtonSize;
        public Vector2 BackButtonPosition => backButtonPosition;
        public float ExpandedUpgradePanelMinimumHeight => Mathf.Max(panelSize.y,
            expandedUpgradePanelMinimumHeight > 0f ? expandedUpgradePanelMinimumHeight : 960f);
        public float ExpandedUpgradeBackButtonY => expandedUpgradeBackButtonY < 0f
            ? expandedUpgradeBackButtonY
            : -878f;
        public float ExperienceUpgradePanelMinimumHeight => Mathf.Max(
            ExpandedUpgradePanelMinimumHeight,
            experienceUpgradePanelMinimumHeight > 0f ? experienceUpgradePanelMinimumHeight : 1060f);
        public float ExperienceUpgradeBackButtonY => experienceUpgradeBackButtonY < 0f
            ? experienceUpgradeBackButtonY
            : -978f;
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

        public Vector2 GetUpgradeCardPosition(int index)
        {
            int safeIndex = Mathf.Max(0, index);
            int columns = UpgradeCardColumns;
            int column = safeIndex % columns;
            int row = safeIndex / columns;
            return firstCardPosition + new Vector2(
                column * UpgradeCardColumnSpacing,
                -row * cardSpacing);
        }
        public Vector2 RebirthHudPosition => rebirthHudPosition;
        public Vector2 RebirthHudSize => rebirthHudSize;
        public Vector2 RebirthHudHeaderSize => rebirthHudHeaderSize;
        public Vector2 RebirthBoostPosition => rebirthBoostPosition;
        public Vector2 RebirthBoostSize => rebirthBoostSize;
        public Vector2 RebirthProgressPosition => rebirthProgressPosition;
        public Vector2 RebirthProgressSize => rebirthProgressSize;
        public Vector2 RebirthOpenButtonPosition => rebirthOpenButtonPosition;
        public Vector2 RebirthOpenButtonSize => rebirthOpenButtonSize;
        public float RebirthTitleFontSize => rebirthTitleFontSize;
        public float RebirthInfoFontSize => rebirthInfoFontSize;
        public Vector2 RebirthModalSize => rebirthModalSize;
        public Vector2 RebirthModalHeaderSize => rebirthModalHeaderSize;
        public Vector2 RebirthWarningPosition => rebirthWarningPosition;
        public Vector2 RebirthWarningSize => rebirthWarningSize;
        public Vector2 RebirthNextBoostPosition => rebirthNextBoostPosition;
        public Vector2 RebirthNextBoostSize => rebirthNextBoostSize;
        public Vector2 RebirthConfirmButtonPosition => rebirthConfirmButtonPosition;
        public Vector2 RebirthCancelButtonPosition => rebirthCancelButtonPosition;
        public Vector2 RebirthModalButtonSize => rebirthModalButtonSize;
        public float RebirthWarningFontSize => rebirthWarningFontSize;
        public float RebirthModalTextFontSize => rebirthModalTextFontSize;
        public Color RebirthHudColor => rebirthHudColor;
        public Color RebirthHeaderColor => rebirthHeaderColor;
        public Color RebirthProgressColor => rebirthProgressColor;
        public Color RebirthProgressGhostColor => rebirthProgressGhostColor;
        public Color RebirthConfirmColor => rebirthConfirmColor;
        public Color RebirthCancelColor => rebirthCancelColor;
        public Color RebirthFlashColor => rebirthFlashColor;
        public float RebirthFlashFadeInDuration => rebirthFlashFadeInDuration;
        public float RebirthFlashHoldDuration => rebirthFlashHoldDuration;
        public float RebirthFlashFadeOutDuration => rebirthFlashFadeOutDuration;

        private void OnValidate()
        {
            referenceResolution.x = Mathf.Max(1f, referenceResolution.x);
            referenceResolution.y = Mathf.Max(1f, referenceResolution.y);
            panelSize.x = Mathf.Max(1f, panelSize.x);
            panelSize.y = Mathf.Max(1f, panelSize.y);
            expandedUpgradePanelMinimumHeight = Mathf.Max(panelSize.y,
                expandedUpgradePanelMinimumHeight);
            experienceUpgradePanelMinimumHeight = Mathf.Max(expandedUpgradePanelMinimumHeight,
                experienceUpgradePanelMinimumHeight);
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
            upgradeCardIconPadding = Mathf.Max(0f, upgradeCardIconPadding);
            npcProgressHudSize.x = Mathf.Max(1f, npcProgressHudSize.x);
            npcProgressHudSize.y = Mathf.Max(1f, npcProgressHudSize.y);
            npcProgressHeaderIconSize.x = Mathf.Max(1f, npcProgressHeaderIconSize.x);
            npcProgressHeaderIconSize.y = Mathf.Max(1f, npcProgressHeaderIconSize.y);
            npcProgressTextSize.x = Mathf.Max(1f, npcProgressTextSize.x);
            npcProgressTextSize.y = Mathf.Max(1f, npcProgressTextSize.y);
            npcPowerTextSize.x = Mathf.Max(1f, npcPowerTextSize.x);
            npcPowerTextSize.y = Mathf.Max(1f, npcPowerTextSize.y);
            npcExperienceBarSize.x = Mathf.Max(1f, npcExperienceBarSize.x);
            npcExperienceBarSize.y = Mathf.Max(1f, npcExperienceBarSize.y);
            npcExperienceTextSize.x = Mathf.Max(1f, npcExperienceTextSize.x);
            npcExperienceTextSize.y = Mathf.Max(1f, npcExperienceTextSize.y);
            npcProgressTitleFontSize = Mathf.Max(1f, npcProgressTitleFontSize);
            npcProgressInfoFontSize = Mathf.Max(1f, npcProgressInfoFontSize);
            npcExperienceBarAnimationSpeed = Mathf.Max(0.01f, npcExperienceBarAnimationSpeed);
            buttonHoverScale = Mathf.Max(1f, buttonHoverScale);
            buttonHoverPunchScale = Mathf.Max(buttonHoverScale, buttonHoverPunchScale);
            buttonPressedScale = Mathf.Clamp(buttonPressedScale, 0.5f, 1f);
            buttonClickBounceScale = Mathf.Max(buttonHoverScale, buttonClickBounceScale);
            buttonHoverPunchDuration = Mathf.Max(0.01f, buttonHoverPunchDuration);
            buttonHoverSettleDuration = Mathf.Max(0.01f, buttonHoverSettleDuration);
            buttonPressDuration = Mathf.Max(0.01f, buttonPressDuration);
            buttonClickBounceDuration = Mathf.Max(0.01f, buttonClickBounceDuration);
            buttonClickSettleDuration = Mathf.Max(0.01f, buttonClickSettleDuration);
            audioMenuButtonSize.x = Mathf.Max(1f, audioMenuButtonSize.x);
            audioMenuButtonSize.y = Mathf.Max(1f, audioMenuButtonSize.y);
            audioPanelSize.x = Mathf.Max(1f, audioPanelSize.x);
            audioPanelSize.y = Mathf.Max(1f, audioPanelSize.y);
            audioHeaderSize.x = Mathf.Max(1f, audioHeaderSize.x);
            audioHeaderSize.y = Mathf.Max(1f, audioHeaderSize.y);
            audioSliderSize.x = Mathf.Max(1f, audioSliderSize.x);
            audioSliderSize.y = Mathf.Max(1f, audioSliderSize.y);
            audioRowSpacing = Mathf.Max(0f, audioRowSpacing);
            audioLabelSize.x = Mathf.Max(1f, audioLabelSize.x);
            audioLabelSize.y = Mathf.Max(1f, audioLabelSize.y);
            audioValueSize.x = Mathf.Max(1f, audioValueSize.x);
            audioValueSize.y = Mathf.Max(1f, audioValueSize.y);
            audioColumnSpacing = Mathf.Max(0f, audioColumnSpacing);
            audioHandleExtraSize = Mathf.Max(0f, audioHandleExtraSize);
            audioCloseButtonSize.x = Mathf.Max(1f, audioCloseButtonSize.x);
            audioCloseButtonSize.y = Mathf.Max(1f, audioCloseButtonSize.y);
            audioTitleFontSize = Mathf.Max(1f, audioTitleFontSize);
            audioLabelFontSize = Mathf.Max(1f, audioLabelFontSize);
            resetDataButtonSize.x = Mathf.Max(1f, resetDataButtonSize.x);
            resetDataButtonSize.y = Mathf.Max(1f, resetDataButtonSize.y);
            resetDataFontSize = Mathf.Max(1f, resetDataFontSize);
            resetDataConfirmationDuration = Mathf.Max(0.1f, resetDataConfirmationDuration);
            inventoryMenuButtonSize.x = Mathf.Max(1f, inventoryMenuButtonSize.x);
            inventoryMenuButtonSize.y = Mathf.Max(1f, inventoryMenuButtonSize.y);
            inventoryPanelSize.x = Mathf.Max(1f, inventoryPanelSize.x);
            inventoryPanelSize.y = Mathf.Max(1f, inventoryPanelSize.y);
            inventoryHeaderSize.x = Mathf.Max(1f, inventoryHeaderSize.x);
            inventoryHeaderSize.y = Mathf.Max(1f, inventoryHeaderSize.y);
            inventoryCloseButtonSize.x = Mathf.Max(1f, inventoryCloseButtonSize.x);
            inventoryCloseButtonSize.y = Mathf.Max(1f, inventoryCloseButtonSize.y);
            inventorySlotSize.x = Mathf.Max(1f, inventorySlotSize.x);
            inventorySlotSize.y = Mathf.Max(1f, inventorySlotSize.y);
            inventorySlotSpacing.x = Mathf.Max(1f, inventorySlotSpacing.x);
            inventorySlotSpacing.y = Mathf.Max(1f, inventorySlotSpacing.y);
            inventoryItemFontSize = Mathf.Max(1f, inventoryItemFontSize);
            inventoryCountFontSize = Mathf.Max(1f, inventoryCountFontSize);
            giftWheelPanelSize.x = Mathf.Max(1f, giftWheelPanelSize.x);
            giftWheelPanelSize.y = Mathf.Max(1f, giftWheelPanelSize.y);
            giftWheelHeaderSize.x = Mathf.Max(1f, giftWheelHeaderSize.x);
            giftWheelHeaderSize.y = Mathf.Max(1f, giftWheelHeaderSize.y);
            giftWheelSize.x = Mathf.Max(1f, giftWheelSize.x);
            giftWheelSize.y = Mathf.Max(1f, giftWheelSize.y);
            giftWheelRewardSize.x = Mathf.Max(1f, giftWheelRewardSize.x);
            giftWheelRewardSize.y = Mathf.Max(1f, giftWheelRewardSize.y);
            giftWheelRewardRadius = Mathf.Max(1f, giftWheelRewardRadius);
            giftWheelSpinButtonSize.x = Mathf.Max(1f, giftWheelSpinButtonSize.x);
            giftWheelSpinButtonSize.y = Mathf.Max(1f, giftWheelSpinButtonSize.y);
            effectToastSize.x = Mathf.Max(1f, effectToastSize.x);
            effectToastSize.y = Mathf.Max(1f, effectToastSize.y);
            effectToastFontSize = Mathf.Max(1f, effectToastFontSize);
            effectToastSlotSize.x = Mathf.Max(1f, effectToastSlotSize.x);
            effectToastSlotSize.y = Mathf.Max(1f, effectToastSlotSize.y);
            effectToastSlotSpacing = Mathf.Max(0f, effectToastSlotSpacing);
            effectToastIconPadding = Mathf.Max(0f, effectToastIconPadding);
            effectToastTimerFontSize = Mathf.Max(1f, effectToastTimerFontSize);
            effectToastPercentFontSize = Mathf.Max(1f, effectToastPercentFontSize);
            panelTransitionDuration = Mathf.Max(0.01f, panelTransitionDuration);
            panelSlideExtraDistance = Mathf.Max(0f, panelSlideExtraDistance);
            cardSpacing = Mathf.Max(0f, cardSpacing);
            outlineThickness = Mathf.Max(0f, outlineThickness);
            shopHeaderSize.x = Mathf.Max(1f, shopHeaderSize.x);
            shopHeaderSize.y = Mathf.Max(1f, shopHeaderSize.y);
            rewardPopupDuration = Mathf.Max(0.01f, rewardPopupDuration);
            rewardPopupPreviewAmount = Mathf.Max(0, rewardPopupPreviewAmount);
            rewardPopupRiseDistance = Mathf.Max(0f, rewardPopupRiseDistance);
            rewardPopupWorldScale = Mathf.Max(0.001f, rewardPopupWorldScale);
            rewardPopupFontSize = Mathf.Max(1f, rewardPopupFontSize);
            rebirthHudSize.x = Mathf.Max(1f, rebirthHudSize.x);
            rebirthHudSize.y = Mathf.Max(1f, rebirthHudSize.y);
            rebirthHudHeaderSize.x = Mathf.Max(1f, rebirthHudHeaderSize.x);
            rebirthHudHeaderSize.y = Mathf.Max(1f, rebirthHudHeaderSize.y);
            rebirthBoostSize.x = Mathf.Max(1f, rebirthBoostSize.x);
            rebirthBoostSize.y = Mathf.Max(1f, rebirthBoostSize.y);
            rebirthProgressSize.x = Mathf.Max(1f, rebirthProgressSize.x);
            rebirthProgressSize.y = Mathf.Max(1f, rebirthProgressSize.y);
            rebirthOpenButtonSize.x = Mathf.Max(1f, rebirthOpenButtonSize.x);
            rebirthOpenButtonSize.y = Mathf.Max(1f, rebirthOpenButtonSize.y);
            rebirthModalSize.x = Mathf.Max(1f, rebirthModalSize.x);
            rebirthModalSize.y = Mathf.Max(1f, rebirthModalSize.y);
            rebirthModalHeaderSize.x = Mathf.Max(1f, rebirthModalHeaderSize.x);
            rebirthModalHeaderSize.y = Mathf.Max(1f, rebirthModalHeaderSize.y);
            rebirthModalButtonSize.x = Mathf.Max(1f, rebirthModalButtonSize.x);
            rebirthModalButtonSize.y = Mathf.Max(1f, rebirthModalButtonSize.y);
            rebirthTitleFontSize = Mathf.Max(1f, rebirthTitleFontSize);
            rebirthInfoFontSize = Mathf.Max(1f, rebirthInfoFontSize);
            rebirthWarningFontSize = Mathf.Max(1f, rebirthWarningFontSize);
            rebirthModalTextFontSize = Mathf.Max(1f, rebirthModalTextFontSize);
            rebirthFlashFadeInDuration = Mathf.Max(0.01f, rebirthFlashFadeInDuration);
            rebirthFlashHoldDuration = Mathf.Max(0f, rebirthFlashHoldDuration);
            rebirthFlashFadeOutDuration = Mathf.Max(0.01f, rebirthFlashFadeOutDuration);
        }
    }
}
