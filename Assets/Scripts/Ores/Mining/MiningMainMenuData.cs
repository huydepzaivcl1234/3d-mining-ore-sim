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
        [SerializeField] private Vector2 cardSize = new(1040f, 620f);
        [SerializeField] private Vector2 gemIconSize = new(220f, 220f);
        [SerializeField] private Vector2 gemIconPosition = new(-250f, 90f);
        [SerializeField] private Vector2 titleSize = new(470f, 80f);
        [SerializeField] private Vector2 titlePosition = new(-250f, -65f);
        [SerializeField] private Vector2 subtitleSize = new(460f, 64f);
        [SerializeField] private Vector2 subtitlePosition = new(-250f, -132f);
        [SerializeField] private Vector2 playButtonSize = new(330f, 78f);
        [SerializeField] private Vector2 playButtonPosition = new(250f, 100f);
        [SerializeField] private Vector2 settingsButtonPosition = new(250f, 0f);
        [SerializeField] private Vector2 exitButtonPosition = new(250f, -100f);
        [SerializeField] private Vector2 settingsTitlePosition = new(0f, 225f);
        [SerializeField] private Vector2 settingsTitleSize = new(760f, 70f);
        [SerializeField] private Vector2 settingsSliderSize = new(460f, 34f);
        [SerializeField] private Vector2 settingsFirstSliderPosition = new(80f, 105f);
        [Min(1f), SerializeField] private float settingsRowSpacing = 92f;
        [SerializeField] private Vector2 settingsLabelSize = new(240f, 44f);
        [SerializeField] private Vector2 settingsLanguagePosition = new(-145f, -185f);
        [SerializeField] private Vector2 settingsBackPosition = new(210f, -185f);
        [SerializeField] private Vector2 settingsSmallButtonSize = new(300f, 64f);
        [SerializeField] private Vector2 gameplayReturnButtonPosition = new(330f, -15f);
        [SerializeField] private Vector2 gameplayReturnButtonSize = new(160f, 44f);

        [Header("Exit Confirmation")]
        [SerializeField] private Vector2 exitConfirmationSize = new(620f, 330f);
        [SerializeField] private Vector2 exitConfirmationButtonSize = new(230f, 64f);

        [Header("Text")]
        [SerializeField] private string englishTitle = "MINING SIMULATOR";
        [SerializeField] private string vietnameseTitle = "MÔ PHỎNG ĐÀO MỎ";
        [SerializeField] private string englishSubtitle = "Mine ores • Hire miners • Upgrade";
        [SerializeField] private string vietnameseSubtitle = "Đào quặng • Thuê thợ mỏ • Nâng cấp";
        [SerializeField] private string englishPlayLabel = "PLAY";
        [SerializeField] private string vietnamesePlayLabel = "CHƠI";
        [SerializeField] private string englishSettingsLabel = "SETTINGS";
        [SerializeField] private string vietnameseSettingsLabel = "CÀI ĐẶT";
        [SerializeField] private string englishExitLabel = "EXIT";
        [SerializeField] private string vietnameseExitLabel = "THOÁT";
        [SerializeField] private string englishBackLabel = "BACK";
        [SerializeField] private string vietnameseBackLabel = "QUAY LẠI";
        [SerializeField] private string englishLanguageLabel = "LANGUAGE: ENGLISH";
        [SerializeField] private string vietnameseLanguageLabel = "NGÔN NGỮ: TIẾNG VIỆT";
        [SerializeField] private string englishReturnToMenuLabel = "MAIN MENU";
        [SerializeField] private string vietnameseReturnToMenuLabel = "MENU CHÍNH";
        [SerializeField] private string englishExitConfirmationTitle = "EXIT GAME?";
        [SerializeField] private string vietnameseExitConfirmationTitle = "THOÁT GAME?";
        [SerializeField] private string englishExitConfirmationMessage =
            "Are you sure you want to exit?";
        [SerializeField] private string vietnameseExitConfirmationMessage =
            "Bạn có chắc muốn thoát không?";
        [SerializeField] private string englishConfirmLabel = "YES, EXIT";
        [SerializeField] private string vietnameseConfirmLabel = "CÓ, THOÁT";
        [SerializeField] private string englishCancelLabel = "CANCEL";
        [SerializeField] private string vietnameseCancelLabel = "HỦY";
        [Min(1f), SerializeField] private float titleFontSize = 54f;
        [Min(1f), SerializeField] private float subtitleFontSize = 24f;
        [Min(1f), SerializeField] private float playFontSize = 32f;
        [Min(1f), SerializeField] private float settingsFontSize = 26f;

        [Header("Colors")]
        [SerializeField] private Color backdropColor = new(0.012f, 0.022f, 0.055f, 1f);
        [SerializeField] private Color cardColor = new(0.045f, 0.07f, 0.13f, 1f);
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField] private Color subtitleColor = new(0.70f, 0.82f, 1f, 1f);
        [SerializeField] private Color playButtonColor = new(0.12f, 0.78f, 0.56f, 1f);
        [SerializeField] private Color settingsButtonColor = new(0.10f, 0.55f, 0.88f, 1f);
        [SerializeField] private Color exitButtonColor = new(0.82f, 0.16f, 0.24f, 1f);
        [SerializeField] private Color sliderBackgroundColor = new(0.09f, 0.12f, 0.19f, 1f);
        [SerializeField] private Color sliderFillColor = new(0.12f, 0.75f, 0.95f, 1f);
        [SerializeField] private Color playTextColor = Color.white;

        [Header("Play Transition")]
        [Min(100f), SerializeField] private float playSlideDistance = 1400f;
        [Range(0.01f, 0.5f), SerializeField] private float playCollapseScaleX = 0.06f;
        [Min(0.05f), SerializeField] private float playSlideDuration = 0.55f;
        [Min(1f), SerializeField] private float transitionBarWidth = 42f;
        [SerializeField] private Color transitionBarColor = new(0.05f, 0.78f, 0.92f, 1f);
        [SerializeField] private Color transitionFlashColor = new(0.45f, 0.95f, 1f, 1f);
        [Min(0.01f), SerializeField] private float transitionFlashDuration = 0.10f;

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
        public Vector2 SettingsButtonPosition => settingsButtonPosition;
        public Vector2 ExitButtonPosition => exitButtonPosition;
        public Vector2 SettingsTitlePosition => settingsTitlePosition;
        public Vector2 SettingsTitleSize => settingsTitleSize;
        public Vector2 SettingsSliderSize => settingsSliderSize;
        public Vector2 SettingsFirstSliderPosition => settingsFirstSliderPosition;
        public float SettingsRowSpacing => settingsRowSpacing;
        public Vector2 SettingsLabelSize => settingsLabelSize;
        public Vector2 SettingsLanguagePosition => settingsLanguagePosition;
        public Vector2 SettingsBackPosition => settingsBackPosition;
        public Vector2 SettingsSmallButtonSize => settingsSmallButtonSize;
        public Vector2 GameplayReturnButtonPosition => gameplayReturnButtonPosition;
        public Vector2 GameplayReturnButtonSize => gameplayReturnButtonSize;
        public Vector2 ExitConfirmationSize => exitConfirmationSize;
        public Vector2 ExitConfirmationButtonSize => exitConfirmationButtonSize;
        public string EnglishTitle => englishTitle;
        public string VietnameseTitle => vietnameseTitle;
        public string EnglishSubtitle => englishSubtitle;
        public string VietnameseSubtitle => vietnameseSubtitle;
        public string EnglishPlayLabel => englishPlayLabel;
        public string VietnamesePlayLabel => vietnamesePlayLabel;
        public string EnglishSettingsLabel => englishSettingsLabel;
        public string VietnameseSettingsLabel => vietnameseSettingsLabel;
        public string EnglishExitLabel => englishExitLabel;
        public string VietnameseExitLabel => vietnameseExitLabel;
        public string EnglishBackLabel => englishBackLabel;
        public string VietnameseBackLabel => vietnameseBackLabel;
        public string EnglishLanguageLabel => englishLanguageLabel;
        public string VietnameseLanguageLabel => vietnameseLanguageLabel;
        public string EnglishReturnToMenuLabel => englishReturnToMenuLabel;
        public string VietnameseReturnToMenuLabel => vietnameseReturnToMenuLabel;
        public string EnglishExitConfirmationTitle => englishExitConfirmationTitle;
        public string VietnameseExitConfirmationTitle => vietnameseExitConfirmationTitle;
        public string EnglishExitConfirmationMessage => englishExitConfirmationMessage;
        public string VietnameseExitConfirmationMessage => vietnameseExitConfirmationMessage;
        public string EnglishConfirmLabel => englishConfirmLabel;
        public string VietnameseConfirmLabel => vietnameseConfirmLabel;
        public string EnglishCancelLabel => englishCancelLabel;
        public string VietnameseCancelLabel => vietnameseCancelLabel;
        public float TitleFontSize => titleFontSize;
        public float SubtitleFontSize => subtitleFontSize;
        public float PlayFontSize => playFontSize;
        public float SettingsFontSize => settingsFontSize;
        public Color BackdropColor => backdropColor;
        public Color CardColor => cardColor;
        public Color TitleColor => titleColor;
        public Color SubtitleColor => subtitleColor;
        public Color PlayButtonColor => playButtonColor;
        public Color SettingsButtonColor => settingsButtonColor;
        public Color ExitButtonColor => exitButtonColor;
        public Color SliderBackgroundColor => sliderBackgroundColor;
        public Color SliderFillColor => sliderFillColor;
        public Color PlayTextColor => playTextColor;
        public float PlaySlideDistance => Mathf.Max(100f, playSlideDistance);
        public float PlayCollapseScaleX => Mathf.Clamp(playCollapseScaleX, 0.01f, 0.5f);
        public float PlaySlideDuration => Mathf.Max(0.05f, playSlideDuration);
        public float TransitionBarWidth => Mathf.Max(1f, transitionBarWidth);
        public Color TransitionBarColor => transitionBarColor.a > 0f
            ? transitionBarColor
            : new Color(0.05f, 0.78f, 0.92f, 1f);
        public Color TransitionFlashColor => transitionFlashColor.a > 0f
            ? transitionFlashColor
            : new Color(0.45f, 0.95f, 1f, 1f);
        public float TransitionFlashDuration => Mathf.Max(0.01f, transitionFlashDuration);
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
            settingsTitleSize = MaxSize(settingsTitleSize);
            settingsSliderSize = MaxSize(settingsSliderSize);
            settingsLabelSize = MaxSize(settingsLabelSize);
            settingsSmallButtonSize = MaxSize(settingsSmallButtonSize);
            gameplayReturnButtonSize = MaxSize(gameplayReturnButtonSize);
            exitConfirmationSize = MaxSize(exitConfirmationSize);
            exitConfirmationButtonSize = MaxSize(exitConfirmationButtonSize);
            settingsRowSpacing = Mathf.Max(1f, settingsRowSpacing);
            titleFontSize = Mathf.Max(1f, titleFontSize);
            subtitleFontSize = Mathf.Max(1f, subtitleFontSize);
            playFontSize = Mathf.Max(1f, playFontSize);
            settingsFontSize = Mathf.Max(1f, settingsFontSize);
            playSlideDistance = Mathf.Max(100f, playSlideDistance);
            playCollapseScaleX = Mathf.Clamp(playCollapseScaleX, 0.01f, 0.5f);
            playSlideDuration = Mathf.Max(0.05f, playSlideDuration);
            transitionBarWidth = Mathf.Max(1f, transitionBarWidth);
            transitionFlashDuration = Mathf.Max(0.01f, transitionFlashDuration);
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
