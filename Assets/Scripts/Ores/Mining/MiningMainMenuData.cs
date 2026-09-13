using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Designer-owned presentation and timing values for the startup main menu.</summary>
    [CreateAssetMenu(fileName = "MiningMainMenuData",
        menuName = "Mining Simulator/Game Data/Main Menu")]
    public sealed class MiningMainMenuData : ScriptableObject
    {
        [Header("Startup")]
        [SerializeField] private bool showOnStart = true;
        [SerializeField] private bool pauseGameplay = true;

        [Header("Layout")]
        [SerializeField] private Vector2 cardSize = new(720f, 520f);
        [SerializeField] private Vector2 gemIconSize = new(126f, 126f);
        [SerializeField] private Vector2 gemIconPosition = new(0f, 140f);
        [SerializeField] private Vector2 titleSize = new(650f, 80f);
        [SerializeField] private Vector2 titlePosition = new(0f, 42f);
        [SerializeField] private Vector2 subtitleSize = new(620f, 54f);
        [SerializeField] private Vector2 subtitlePosition = new(0f, -28f);
        [SerializeField] private Vector2 playButtonSize = new(330f, 78f);
        [SerializeField] private Vector2 playButtonPosition = new(0f, -145f);

        [Header("Text")]
        [SerializeField] private string englishTitle = "MINING SIMULATOR";
        [SerializeField] private string vietnameseTitle = "MÔ PHỎNG ĐÀO MỎ";
        [SerializeField] private string englishSubtitle = "Mine ores • Hire miners • Upgrade";
        [SerializeField] private string vietnameseSubtitle = "Đào quặng • Thuê thợ mỏ • Nâng cấp";
        [SerializeField] private string englishPlayLabel = "PLAY";
        [SerializeField] private string vietnamesePlayLabel = "CHƠI";
        [Min(1f), SerializeField] private float titleFontSize = 54f;
        [Min(1f), SerializeField] private float subtitleFontSize = 24f;
        [Min(1f), SerializeField] private float playFontSize = 32f;

        [Header("Colors")]
        [SerializeField] private Color backdropColor = new(0.015f, 0.025f, 0.06f, 0.96f);
        [SerializeField] private Color cardColor = new(0.055f, 0.075f, 0.14f, 0.98f);
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField] private Color subtitleColor = new(0.70f, 0.82f, 1f, 1f);
        [SerializeField] private Color playButtonColor = new(0.12f, 0.78f, 0.56f, 1f);
        [SerializeField] private Color playTextColor = Color.white;

        [Header("PrimeTween Animation")]
        [Range(0.1f, 1f), SerializeField] private float entranceStartScale = 0.72f;
        [Min(0.01f), SerializeField] private float entranceDuration = 0.42f;
        [Range(0.1f, 1f), SerializeField] private float exitScale = 0.88f;
        [Min(0.01f), SerializeField] private float exitDuration = 0.24f;

        public bool ShowOnStart => showOnStart;
        public bool PauseGameplay => pauseGameplay;
        public Vector2 CardSize => cardSize;
        public Vector2 GemIconSize => gemIconSize;
        public Vector2 GemIconPosition => gemIconPosition;
        public Vector2 TitleSize => titleSize;
        public Vector2 TitlePosition => titlePosition;
        public Vector2 SubtitleSize => subtitleSize;
        public Vector2 SubtitlePosition => subtitlePosition;
        public Vector2 PlayButtonSize => playButtonSize;
        public Vector2 PlayButtonPosition => playButtonPosition;
        public string EnglishTitle => englishTitle;
        public string VietnameseTitle => vietnameseTitle;
        public string EnglishSubtitle => englishSubtitle;
        public string VietnameseSubtitle => vietnameseSubtitle;
        public string EnglishPlayLabel => englishPlayLabel;
        public string VietnamesePlayLabel => vietnamesePlayLabel;
        public float TitleFontSize => titleFontSize;
        public float SubtitleFontSize => subtitleFontSize;
        public float PlayFontSize => playFontSize;
        public Color BackdropColor => backdropColor;
        public Color CardColor => cardColor;
        public Color TitleColor => titleColor;
        public Color SubtitleColor => subtitleColor;
        public Color PlayButtonColor => playButtonColor;
        public Color PlayTextColor => playTextColor;
        public float EntranceStartScale => entranceStartScale;
        public float EntranceDuration => entranceDuration;
        public float ExitScale => exitScale;
        public float ExitDuration => exitDuration;

        private void OnValidate()
        {
            cardSize = MaxSize(cardSize);
            gemIconSize = MaxSize(gemIconSize);
            titleSize = MaxSize(titleSize);
            subtitleSize = MaxSize(subtitleSize);
            playButtonSize = MaxSize(playButtonSize);
            titleFontSize = Mathf.Max(1f, titleFontSize);
            subtitleFontSize = Mathf.Max(1f, subtitleFontSize);
            playFontSize = Mathf.Max(1f, playFontSize);
            entranceStartScale = Mathf.Clamp(entranceStartScale, 0.1f, 1f);
            entranceDuration = Mathf.Max(0.01f, entranceDuration);
            exitScale = Mathf.Clamp(exitScale, 0.1f, 1f);
            exitDuration = Mathf.Max(0.01f, exitDuration);
        }

        private static Vector2 MaxSize(Vector2 value)
        {
            return new Vector2(Mathf.Max(1f, value.x), Mathf.Max(1f, value.y));
        }
    }
}
