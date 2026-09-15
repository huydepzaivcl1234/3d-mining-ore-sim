using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Owns the startup menu, settings page, and gameplay pause.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningMainMenu : MonoBehaviour
    {
        [Header("Data and pages")]
        [SerializeField] private MiningMainMenuData data;
        [SerializeField] private MiningGameData gameData;
        [SerializeField] private MiningAudioManager audioManager;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform card;
        [SerializeField] private GameObject mainView;
        [SerializeField] private GameObject settingsView;
        [SerializeField] private CanvasGroup mainViewGroup;
        [SerializeField] private CanvasGroup settingsViewGroup;

        [Header("Play transition")]
        [SerializeField] private Image transitionBar;
        [SerializeField] private Image transitionFlash;

        [Header("Exit confirmation")]
        [SerializeField] private GameObject exitConfirmation;
        [SerializeField] private CanvasGroup exitConfirmationGroup;
        [SerializeField] private RectTransform exitConfirmationDialog;
        [SerializeField] private Button confirmExitButton;
        [SerializeField] private Button cancelExitButton;

        [Header("Main buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private MiningShopPanel shopPanel;

        [Header("Settings controls")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button languageButton;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Localized labels")]
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI subtitleLabel;
        [SerializeField] private TextMeshProUGUI gemAmountLabel;
        [SerializeField] private TextMeshProUGUI playLabel;
        [SerializeField] private TextMeshProUGUI shopLabel;
        [SerializeField] private TextMeshProUGUI settingsLabel;
        [SerializeField] private TextMeshProUGUI exitLabel;
        [SerializeField] private TextMeshProUGUI settingsTitleLabel;
        [SerializeField] private TextMeshProUGUI masterLabel;
        [SerializeField] private TextMeshProUGUI musicLabel;
        [SerializeField] private TextMeshProUGUI sfxLabel;
        [SerializeField] private TextMeshProUGUI masterValueLabel;
        [SerializeField] private TextMeshProUGUI musicValueLabel;
        [SerializeField] private TextMeshProUGUI sfxValueLabel;
        [SerializeField] private TextMeshProUGUI languageLabel;
        [SerializeField] private TextMeshProUGUI backLabel;
        [SerializeField] private TextMeshProUGUI exitConfirmationTitleLabel;
        [SerializeField] private TextMeshProUGUI exitConfirmationMessageLabel;
        [SerializeField] private TextMeshProUGUI confirmExitLabel;
        [SerializeField] private TextMeshProUGUI cancelExitLabel;

        private Sequence transition;
        private Sequence pageTransition;
        private float timeScaleBeforeMenu = 1f;
        private bool ownsGameplayPause;
        private bool closing;
        private Vector2 cardHomePosition;
        private readonly MiningAnimatedCurrencyValue gemCounter = new();

        private void Awake()
        {
            if (!ResolveReferences())
            {
                Debug.LogError("Main Menu is missing authored references. Run " +
                    "Mining Simulator > Setup > Create Or Update Main Menu.", this);
                enabled = false;
                return;
            }

            if (!data.ShowOnStart)
            {
                gameObject.SetActive(false);
                return;
            }

            cardHomePosition = card.anchoredPosition;
            PauseGameplay();

            mainView.SetActive(true);
            settingsView.SetActive(false);
            exitConfirmation.SetActive(false);
            ResetPlayTransition();
            ShowImmediately();
        }

        private void OnEnable()
        {
            AddListeners();
            MiningLocalization.LanguageChanged -= RefreshLocalization;
            MiningLocalization.LanguageChanged += RefreshLocalization;
            if (wallet != null)
            {
                wallet.GemsChanged -= HandleGemsChanged;
                wallet.GemsChanged += HandleGemsChanged;
                gemCounter.Initialize(wallet.CurrentGems);
            }
            RefreshLocalization();
        }

        private void Start()
        {
            if (!isActiveAndEnabled || data == null || !data.ShowOnStart)
            {
                return;
            }

            RefreshAudioControls();
            PlayEntrance();
            SelectButton(playButton);
        }

        private void Update()
        {
            if (gemCounter.Tick(Time.unscaledDeltaTime))
            {
                RefreshGemAmount();
            }
        }

        private void OnDisable()
        {
            RemoveListeners();
            MiningLocalization.LanguageChanged -= RefreshLocalization;
            if (wallet != null)
            {
                wallet.GemsChanged -= HandleGemsChanged;
            }
            audioManager?.SaveVolumeSettings();
            StopTransitions();
            ReleaseGameplayPause();
        }

        private void OnDestroy()
        {
            ReleaseGameplayPause();
        }

        public void Play()
        {
            if (closing || data == null)
            {
                return;
            }

            closing = true;
            SetMainButtonsInteractable(false);
            StopTransition(ref transition);
            transitionBar.gameObject.SetActive(true);
            transitionFlash.gameObject.SetActive(true);
            SetGraphicAlpha(transitionBar, 0f);
            SetGraphicAlpha(transitionFlash, 0f);
            Vector2 slideDestination = cardHomePosition +
                                       Vector2.left * data.PlaySlideDistance;
            transition = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Custom(card, card.anchoredPosition, slideDestination,
                    data.PlaySlideDuration,
                    static (rect, position) => rect.anchoredPosition = position, Ease.InCubic))
                .Group(Tween.Custom(transitionBar, 0f, 1f,
                    Mathf.Min(0.18f, data.PlaySlideDuration),
                    static (image, alpha) => SetGraphicAlpha(image, alpha), Ease.OutCubic))
                .Chain(Tween.Custom(transitionFlash, 0f, 1f,
                    data.TransitionFlashDuration,
                    static (image, alpha) => SetGraphicAlpha(image, alpha), Ease.Linear))
                .OnComplete(this, static menu => menu.FinishPlay());
        }

        public void OpenFromGameplay()
        {
            if (gameObject.activeSelf)
            {
                return;
            }

            gameObject.SetActive(true);
            PauseGameplay();
            mainView.SetActive(true);
            settingsView.SetActive(false);
            exitConfirmation.SetActive(false);
            ResetPlayTransition();
            ShowImmediately();
            RefreshAudioControls();
            PlayEntrance();
            SelectButton(playButton);
        }

        public void OpenSettings()
        {
            if (closing)
            {
                return;
            }
            RefreshAudioControls();
            SwitchPage(mainView, settingsView, settingsViewGroup);
            SelectButton(backButton);
        }

        public void OpenShop()
        {
            if (!closing)
            {
                shopPanel?.Open();
            }
        }

        public void CloseSettings()
        {
            audioManager?.SaveVolumeSettings();
            SwitchPage(settingsView, mainView, mainViewGroup);
            SelectButton(settingsButton);
        }

        public void ExitGame()
        {
            if (closing)
            {
                return;
            }

            mainViewGroup.interactable = false;
            exitConfirmation.SetActive(true);
            exitConfirmationGroup.alpha = 0f;
            exitConfirmationGroup.interactable = true;
            exitConfirmationGroup.blocksRaycasts = true;
            exitConfirmationDialog.localScale = Vector3.one * 0.78f;
            StopTransition(ref pageTransition);
            pageTransition = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Custom(exitConfirmationGroup, 0f, 1f, data.ExitDuration,
                    static (group, alpha) => group.alpha = alpha, Ease.OutCubic))
                .Group(Tween.Scale(exitConfirmationDialog, Vector3.one,
                    data.ExitDuration, Ease.OutBack));
            SelectButton(cancelExitButton);
        }

        public void CancelExit()
        {
            StopTransition(ref pageTransition);
            exitConfirmationGroup.interactable = false;
            exitConfirmationGroup.blocksRaycasts = false;
            pageTransition = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Custom(exitConfirmationGroup, exitConfirmationGroup.alpha, 0f,
                    data.ExitDuration,
                    static (group, alpha) => group.alpha = alpha, Ease.InCubic))
                .Group(Tween.Scale(exitConfirmationDialog, Vector3.one * 0.85f,
                    data.ExitDuration, Ease.InBack))
                .OnComplete(this, static menu => menu.FinishCancelExit());
        }

        public void ConfirmExit()
        {
            audioManager?.SaveVolumeSettings();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ToggleLanguage()
        {
            if (!MiningLocalization.LanguageSwitchingEnabled) return;
            MiningLocalization.ToggleLanguage();
        }

        private void SetMasterVolume(float value)
        {
            audioManager?.SetMasterVolume(value);
            SetPercent(masterValueLabel, value);
        }

        private void SetMusicVolume(float value)
        {
            audioManager?.SetMusicVolume(value);
            SetPercent(musicValueLabel, value);
        }

        private void SetSfxVolume(float value)
        {
            audioManager?.SetSfxVolume(value);
            SetPercent(sfxValueLabel, value);
        }

        private void FinishPlay()
        {
            ReleaseGameplayPause();
            gameObject.SetActive(false);
        }

        private void FinishCancelExit()
        {
            exitConfirmation.SetActive(false);
            mainViewGroup.interactable = true;
            mainViewGroup.blocksRaycasts = true;
            SelectButton(exitButton);
        }

        private void PlayEntrance()
        {
            StopTransition(ref transition);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            card.localScale = Vector3.one * data.EntranceStartScale;
            transition = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Custom(this, 0f, 1f, data.EntranceDuration,
                    static (menu, alpha) => menu.canvasGroup.alpha = alpha, Ease.OutCubic))
                .Group(Tween.Scale(card, Vector3.one, data.EntranceDuration, Ease.OutBack));
        }

        private void SwitchPage(GameObject from, GameObject to, CanvasGroup toGroup)
        {
            StopTransition(ref pageTransition);
            from.SetActive(false);
            to.SetActive(true);
            toGroup.alpha = 0f;
            toGroup.interactable = true;
            toGroup.blocksRaycasts = true;

            RectTransform pageRect = to.GetComponent<RectTransform>();
            Vector2 restingPosition = pageRect.anchoredPosition;
            Vector2 startPosition = restingPosition + new Vector2(55f, 0f);
            pageRect.anchoredPosition = startPosition;
            pageTransition = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Custom(toGroup, 0f, 1f, data.ExitDuration,
                    static (group, alpha) => group.alpha = alpha, Ease.OutCubic))
                .Group(Tween.Custom(pageRect, startPosition, restingPosition,
                    data.ExitDuration, static (rect, position) => rect.anchoredPosition = position,
                    Ease.OutCubic));
        }

        private void ShowImmediately()
        {
            closing = false;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            card.localScale = Vector3.one;
            card.anchoredPosition = cardHomePosition;
            SetMainButtonsInteractable(true);
        }

        private void RefreshAudioControls()
        {
            if (audioManager == null)
            {
                return;
            }
            SetSlider(masterSlider, audioManager.MasterVolume, masterValueLabel);
            SetSlider(musicSlider, audioManager.MusicVolume, musicValueLabel);
            SetSlider(sfxSlider, audioManager.SfxVolume, sfxValueLabel);
        }

        private void RefreshLocalization()
        {
            if (data == null)
            {
                return;
            }
            if (languageButton != null)
            {
                languageButton.interactable = MiningLocalization.LanguageSwitchingEnabled;
            }
            SetText(titleLabel, data.EnglishTitle, data.VietnameseTitle);
            SetText(subtitleLabel, data.EnglishSubtitle, data.VietnameseSubtitle);
            RefreshGemAmount();
            SetText(playLabel, data.EnglishPlayLabel, data.VietnamesePlayLabel);
            SetText(shopLabel, data.EnglishShopLabel, data.VietnameseShopLabel);
            SetText(settingsLabel, data.EnglishSettingsLabel, data.VietnameseSettingsLabel);
            SetText(exitLabel, data.EnglishExitLabel, data.VietnameseExitLabel);
            SetText(settingsTitleLabel, data.EnglishSettingsLabel, data.VietnameseSettingsLabel);
            SetText(masterLabel, "MASTER VOLUME", "ÂM LƯỢNG TỔNG");
            SetText(musicLabel, "MUSIC", "NHẠC");
            SetText(sfxLabel, "SOUND EFFECTS", "HIỆU ỨNG");
            SetText(languageLabel, data.EnglishLanguageLabel, data.VietnameseLanguageLabel);
            SetText(backLabel, data.EnglishBackLabel, data.VietnameseBackLabel);
            SetText(exitConfirmationTitleLabel, data.EnglishExitConfirmationTitle,
                data.VietnameseExitConfirmationTitle);
            SetText(exitConfirmationMessageLabel, data.EnglishExitConfirmationMessage,
                data.VietnameseExitConfirmationMessage);
            SetText(confirmExitLabel, data.EnglishConfirmLabel, data.VietnameseConfirmLabel);
            SetText(cancelExitLabel, data.EnglishCancelLabel, data.VietnameseCancelLabel);
        }

        private void AddListeners()
        {
            RemoveListeners();
            playButton?.onClick.AddListener(Play);
            shopButton?.onClick.AddListener(OpenShop);
            settingsButton?.onClick.AddListener(OpenSettings);
            exitButton?.onClick.AddListener(ExitGame);
            confirmExitButton?.onClick.AddListener(ConfirmExit);
            cancelExitButton?.onClick.AddListener(CancelExit);
            backButton?.onClick.AddListener(CloseSettings);
            languageButton?.onClick.AddListener(ToggleLanguage);
            masterSlider?.onValueChanged.AddListener(SetMasterVolume);
            musicSlider?.onValueChanged.AddListener(SetMusicVolume);
            sfxSlider?.onValueChanged.AddListener(SetSfxVolume);
        }

        private void RemoveListeners()
        {
            playButton?.onClick.RemoveListener(Play);
            shopButton?.onClick.RemoveListener(OpenShop);
            settingsButton?.onClick.RemoveListener(OpenSettings);
            exitButton?.onClick.RemoveListener(ExitGame);
            confirmExitButton?.onClick.RemoveListener(ConfirmExit);
            cancelExitButton?.onClick.RemoveListener(CancelExit);
            backButton?.onClick.RemoveListener(CloseSettings);
            languageButton?.onClick.RemoveListener(ToggleLanguage);
            masterSlider?.onValueChanged.RemoveListener(SetMasterVolume);
            musicSlider?.onValueChanged.RemoveListener(SetMusicVolume);
            sfxSlider?.onValueChanged.RemoveListener(SetSfxVolume);
        }

        private bool ResolveReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
            if (audioManager == null)
            {
                audioManager = FindFirstObjectByType<MiningAudioManager>(
                    FindObjectsInactive.Include);
            }
            if (wallet == null)
            {
                wallet = FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            }
            gameData ??= wallet != null ? wallet.GameData : null;
            return data != null && canvasGroup != null && card != null &&
                   mainView != null && settingsView != null && mainViewGroup != null &&
                   settingsViewGroup != null && playButton != null && settingsButton != null &&
                   exitButton != null && backButton != null && languageButton != null &&
                   transitionBar != null && transitionFlash != null &&
                   exitConfirmation != null && exitConfirmationGroup != null &&
                   exitConfirmationDialog != null && confirmExitButton != null &&
                   cancelExitButton != null;
        }

        private void HandleGemsChanged(float amount)
        {
            gemCounter.SetTarget(amount, gameData);
            RefreshGemAmount();
        }

        private void RefreshGemAmount()
        {
            if (gemAmountLabel == null || data == null)
            {
                return;
            }

            string format = MiningLocalization.Text(data.EnglishGemAmountFormat,
                data.VietnameseGemAmountFormat);
            gemAmountLabel.text = string.Format(format,
                MiningMoneyFormatter.Format(gemCounter.Value));
        }

        private void PauseGameplay()
        {
            if (data == null || !data.PauseGameplay || ownsGameplayPause)
            {
                return;
            }
            timeScaleBeforeMenu = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            ownsGameplayPause = true;
        }

        private void ResetPlayTransition()
        {
            card.anchoredPosition = cardHomePosition;
            card.localScale = Vector3.one;
            transitionBar.color = data.TransitionBarColor;
            transitionFlash.color = data.TransitionFlashColor;
            SetGraphicAlpha(transitionBar, 0f);
            SetGraphicAlpha(transitionFlash, 0f);
            transitionBar.gameObject.SetActive(false);
            transitionFlash.gameObject.SetActive(false);
        }

        private void SetMainButtonsInteractable(bool value)
        {
            playButton.interactable = value;
            if (shopButton != null)
            {
                shopButton.interactable = value;
            }
            settingsButton.interactable = value;
            exitButton.interactable = value;
        }

        private void StopTransitions()
        {
            StopTransition(ref transition);
            StopTransition(ref pageTransition);
        }

        private static void StopTransition(ref Sequence sequence)
        {
            if (sequence.isAlive)
            {
                sequence.Stop();
            }
        }

        private void ReleaseGameplayPause()
        {
            if (!ownsGameplayPause)
            {
                return;
            }
            Time.timeScale = timeScaleBeforeMenu;
            ownsGameplayPause = false;
        }

        private static void SetText(TextMeshProUGUI label, string english, string vietnamese)
        {
            if (label != null)
            {
                label.text = MiningLocalization.Text(english, vietnamese);
            }
        }

        private static void SetSlider(Slider slider, float value, TextMeshProUGUI valueLabel)
        {
            slider?.SetValueWithoutNotify(value);
            SetPercent(valueLabel, value);
        }

        private static void SetPercent(TextMeshProUGUI label, float value)
        {
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }

        private static void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null)
            {
                return;
            }
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        private static void SelectButton(Button button)
        {
            if (button != null)
            {
                EventSystem.current?.SetSelectedGameObject(button.gameObject);
            }
        }
    }
}
