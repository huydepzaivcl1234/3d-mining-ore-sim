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
        [SerializeField] private Vector2 shopPanelSize = new(330f, 260f);
        [SerializeField] private Vector2 shopTextSize = new(294f, 32f);
        [SerializeField] private Vector2 moneyTextPosition = new(18f, -16f);
        [SerializeField] private Vector2 npcCountTextPosition = new(18f, -52f);
        [SerializeField] private Vector2 buyButtonPosition = new(18f, -88f);
        [SerializeField] private Vector2 buyButtonSize = new(294f, 54f);
        [SerializeField] private Vector2 statusTextPosition = new(18f, -151f);
        [Min(1f), SerializeField] private float moneyFontSize = 26f;
        [Min(1f), SerializeField] private float npcCountFontSize = 21f;
        [Min(1f), SerializeField] private float buyButtonFontSize = 22f;
        [Min(1f), SerializeField] private float statusFontSize = 17f;
        [SerializeField] private Color shopPanelColor = new(0.035f, 0.045f, 0.06f, 0.94f);
        [SerializeField] private Color shopTextColor = Color.white;
        [SerializeField] private Color statusTextColor = new(1f, 0.82f, 0.28f, 1f);
        [SerializeField] private Color buyButtonColor = new(0.95f, 0.57f, 0.1f, 1f);
        [SerializeField] private Color buyButtonTextColor = new(0.08f, 0.06f, 0.03f, 1f);

        [Header("Upgrade Panel Layout")]
        [SerializeField] private Vector2 panelSize = new(720f, 560f);
        [SerializeField] private Vector2 headerSize = new(720f, 82f);
        [SerializeField] private Vector2 cardSize = new(620f, 92f);
        [SerializeField] private Vector2 firstCardPosition = new(50f, -116f);
        [Min(0f), SerializeField] private float cardSpacing = 108f;
        [SerializeField] private Vector2 closeButtonSize = new(58f, 58f);
        [SerializeField] private Vector2 closeButtonPosition = new(678f, -12f);
        [SerializeField] private Vector2 backButtonSize = new(180f, 54f);
        [SerializeField] private Vector2 backButtonPosition = new(270f, -486f);
        [SerializeField] private Vector2 openButtonSize = new(294f, 46f);
        [SerializeField] private Vector2 openButtonPosition = new(18f, -194f);
        [Min(0f), SerializeField] private float outlineThickness = 4f;

        [Header("Upgrade Panel Typography")]
        [Min(1f), SerializeField] private float titleFontSize = 32f;
        [Min(1f), SerializeField] private float cardFontSize = 20f;
        [Min(1f), SerializeField] private float navigationFontSize = 20f;
        [SerializeField] private Vector4 cardTextMargin = new(22f, 0f, 18f, 0f);

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
            cardSpacing = Mathf.Max(0f, cardSpacing);
            outlineThickness = Mathf.Max(0f, outlineThickness);
        }
    }
}
