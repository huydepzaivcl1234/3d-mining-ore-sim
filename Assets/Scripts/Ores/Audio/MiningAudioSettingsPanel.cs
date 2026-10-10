using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Connects the editable audio menu to persistent runtime volume controls.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningAudioSettingsPanel : MonoBehaviour
    {
        [SerializeField] private MiningAudioManager audioManager;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private MiningUiPanelCoordinator panelCoordinator;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private MiningOrbitCamera cameraRig;
        [SerializeField] private UnityEngine.UI.Slider sensitivitySlider;
        [SerializeField] private TextMeshProUGUI sensitivityValueLabel;
        [SerializeField] private TextMeshProUGUI masterValueLabel;
        [SerializeField] private TextMeshProUGUI musicValueLabel;
        [SerializeField] private TextMeshProUGUI sfxValueLabel;
        [SerializeField] private MiningUiData uiData;
        [SerializeField] private MiningRebirthSystem rebirthSystem;
        [SerializeField] private Button resetDataButton;
        [SerializeField] private TextMeshProUGUI resetDataLabel;
        [SerializeField] private Button languageButton;
        [SerializeField] private TextMeshProUGUI languageLabel;
        [SerializeField] private MiningMainMenu mainMenu;
        [SerializeField] private Button returnToMenuButton;
        [SerializeField] private TextMeshProUGUI returnToMenuLabel;
        [SerializeField] private Button englishButton;
        [SerializeField] private Button vietnameseButton;
        [SerializeField] private bool flatDesign;

        private Coroutine resetStateCoroutine;
        private bool resetConfirmationArmed;
        private PlayerGraphicsOptions graphicsOptions;
        private PlayerGraphicsPanel graphicsPanel;

        public event Action PanelOpened;
        public event Action PanelClosed;

        private void Awake()
        {
            if (cameraRig == null)
                foreach (var rig in FindObjectsByType<MiningOrbitCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (rig.FollowTarget != null) { cameraRig = rig; break; }
            if (sensitivitySlider != null && cameraRig != null)
            {
                sensitivitySlider.minValue = cameraRig.SensitivityMinimum;
                sensitivitySlider.maxValue = cameraRig.SensitivityMaximum;
            }
            if (uiData == null && panelCoordinator != null)
            {
                uiData = panelCoordinator.UiData;
            }
            FindResetDependenciesIfMissing();
            EnsureResetDataButton();
            EnsureLanguageButton();
            ApplyLanguage();
            settingsPanel?.SetActive(false);
        }

        private void Start()
        {
            if (settingsPanel != null)
            {
                graphicsOptions = GetComponent<PlayerGraphicsOptions>() ?? gameObject.AddComponent<PlayerGraphicsOptions>();
                graphicsOptions.Initialize();
                graphicsPanel = GetComponent<PlayerGraphicsPanel>() ?? gameObject.AddComponent<PlayerGraphicsPanel>();
                graphicsPanel.Build(settingsPanel, graphicsOptions);
            }
            ApplyLanguage();
        }

        private void OnEnable()
        {
            openButton?.onClick.AddListener(OpenPanel);
            closeButton?.onClick.AddListener(ClosePanel);
            masterSlider?.onValueChanged.AddListener(SetMasterVolume);
            musicSlider?.onValueChanged.AddListener(SetMusicVolume);
            sfxSlider?.onValueChanged.AddListener(SetSfxVolume);
            sensitivitySlider?.onValueChanged.AddListener(SetMouseSensitivity);
            resetDataButton?.onClick.RemoveListener(HandleResetDataClicked);
            resetDataButton?.onClick.AddListener(HandleResetDataClicked);
            languageButton?.onClick.RemoveListener(HandleLanguageClicked);
            languageButton?.onClick.AddListener(HandleLanguageClicked);
            returnToMenuButton?.onClick.RemoveListener(HandleReturnToMenuClicked);
            returnToMenuButton?.onClick.AddListener(HandleReturnToMenuClicked);
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
            MiningLocalization.LanguageChanged += HandleLanguageChanged;
            englishButton?.onClick.AddListener(SelectEnglish);
            vietnameseButton?.onClick.AddListener(SelectVietnamese);
            ResetButtonState();
            RefreshFromManager();
        }

        private void OnDisable()
        {
            englishButton?.onClick.RemoveListener(SelectEnglish);
            vietnameseButton?.onClick.RemoveListener(SelectVietnamese);
            openButton?.onClick.RemoveListener(OpenPanel);
            closeButton?.onClick.RemoveListener(ClosePanel);
            masterSlider?.onValueChanged.RemoveListener(SetMasterVolume);
            musicSlider?.onValueChanged.RemoveListener(SetMusicVolume);
            sfxSlider?.onValueChanged.RemoveListener(SetSfxVolume);
            sensitivitySlider?.onValueChanged.RemoveListener(SetMouseSensitivity);
            resetDataButton?.onClick.RemoveListener(HandleResetDataClicked);
            languageButton?.onClick.RemoveListener(HandleLanguageClicked);
            returnToMenuButton?.onClick.RemoveListener(HandleReturnToMenuClicked);
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
            if (resetStateCoroutine != null)
            {
                StopCoroutine(resetStateCoroutine);
                resetStateCoroutine = null;
            }
            ResetButtonState();
        }

        public void OpenPanel()
        {
            ApplyLanguage();
            RefreshFromManager();
            if (panelCoordinator != null)
            {
                panelCoordinator.OpenPanel(settingsPanel != null
                    ? settingsPanel.GetComponent<RectTransform>()
                    : null);
            }
            else
            {
                settingsPanel?.SetActive(true);
            }
            PanelOpened?.Invoke();
        }

        private void ClosePanel()
        {
            graphicsPanel?.Close();
            audioManager?.SaveVolumeSettings();
            if (panelCoordinator != null)
            {
                panelCoordinator.ClosePanel(settingsPanel != null
                    ? settingsPanel.GetComponent<RectTransform>()
                    : null);
            }
            else
            {
                settingsPanel?.SetActive(false);
            }
            PanelClosed?.Invoke();
        }

        private void SetMasterVolume(float value)
        {
            audioManager?.SetMasterVolume(value);
            RefreshValueLabel(masterValueLabel, value);
        }

        private void SetMusicVolume(float value)
        {
            audioManager?.SetMusicVolume(value);
            RefreshValueLabel(musicValueLabel, value);
        }

        private void SetSfxVolume(float value)
        {
            audioManager?.SetSfxVolume(value);
            RefreshValueLabel(sfxValueLabel, value);
        }

        private void RefreshFromManager()
        {
            if (cameraRig != null)
            {
                SetSliderWithoutNotify(sensitivitySlider, cameraRig.MouseSensitivity);
                RefreshSensitivityLabel(cameraRig.MouseSensitivity);
            }
            if (audioManager == null)
            {
                return;
            }

            SetSliderWithoutNotify(masterSlider, audioManager.MasterVolume);
            SetSliderWithoutNotify(musicSlider, audioManager.MusicVolume);
            SetSliderWithoutNotify(sfxSlider, audioManager.SfxVolume);
            RefreshValueLabel(masterValueLabel, audioManager.MasterVolume);
            RefreshValueLabel(musicValueLabel, audioManager.MusicVolume);
            RefreshValueLabel(sfxValueLabel, audioManager.SfxVolume);
        }

        private static void SetSliderWithoutNotify(Slider slider, float value)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(value);
            }
        }

        private void SetMouseSensitivity(float value)
        {
            // Both the gameplay rig and the existing overview rig use the same preference.
            foreach (var rig in FindObjectsByType<MiningOrbitCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                rig.SetMouseSensitivity(value);
            RefreshSensitivityLabel(cameraRig != null ? cameraRig.MouseSensitivity : value);
        }

        private void RefreshSensitivityLabel(float value)
        {
            if (sensitivityValueLabel != null) sensitivityValueLabel.text = $"{value:0.00}x";
        }

        private static void RefreshValueLabel(TextMeshProUGUI label, float value)
        {
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }

        private void HandleResetDataClicked()
        {
            if (!resetConfirmationArmed)
            {
                resetConfirmationArmed = true;
                SetResetButtonVisual(MiningLocalization.Text("CLICK AGAIN TO DELETE"),
                    uiData != null ? uiData.ResetDataArmedColor : new Color(1f, 0.36f, 0.08f));
                if (resetStateCoroutine != null)
                {
                    StopCoroutine(resetStateCoroutine);
                }
                resetStateCoroutine = StartCoroutine(CancelResetConfirmationAfterDelay());
                return;
            }

            if (resetStateCoroutine != null)
            {
                StopCoroutine(resetStateCoroutine);
            }
            resetStateCoroutine = null;
            resetConfirmationArmed = false;
            if (rebirthSystem == null || !rebirthSystem.ResetAllProgress())
            {
                SetResetButtonVisual("RESET FAILED — CHECK CONSOLE", Color.red);
                return;
            }
            SetResetButtonVisual(MiningLocalization.Text("DATA RESET"),
                uiData != null ? uiData.ResetDataButtonColor : new Color(0.88f, 0.12f, 0.18f));
            resetStateCoroutine = StartCoroutine(RestoreResetButtonAfterDelay());
        }

        private IEnumerator CancelResetConfirmationAfterDelay()
        {
            float duration = uiData != null ? uiData.ResetDataConfirmationDuration : 3f;
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, duration));
            resetStateCoroutine = null;
            ResetButtonState();
        }

        private IEnumerator RestoreResetButtonAfterDelay()
        {
            yield return new WaitForSecondsRealtime(1.2f);
            resetStateCoroutine = null;
            ResetButtonState();
        }

        private void ResetButtonState()
        {
            resetConfirmationArmed = false;
            SetResetButtonVisual(flatDesign ? MiningLocalization.Text("RESET", "XÓA") : MiningLocalization.Text("RESET DATA"),
                uiData != null ? uiData.ResetDataButtonColor : new Color(0.88f, 0.12f, 0.18f));
        }

        private void SetResetButtonVisual(string text, Color color)
        {
            if (resetDataLabel != null)
            {
                resetDataLabel.text = text;
            }
            if (resetDataButton != null && resetDataButton.targetGraphic is Image image)
            {
                image.color = color;
            }
        }

        private void FindResetDependenciesIfMissing()
        {
            if (rebirthSystem == null)
            {
                rebirthSystem = FindFirstObjectByType<MiningRebirthSystem>(
                    FindObjectsInactive.Include);
            }
        }

        private void EnsureResetDataButton()
        {
            if (settingsPanel == null)
            {
                return;
            }

            TextMeshProUGUI title = settingsPanel.transform.Find("Header/Title")
                ?.GetComponent<TextMeshProUGUI>();
            if (title != null)
            {
                title.text = MiningLocalization.Text("SETTINGS");
            }

            Transform existing = settingsPanel.transform.Find("Reset Data");
            bool created = existing == null;
            if (existing == null)
            {
                GameObject buttonObject = new("Reset Data", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(settingsPanel.transform, false);
                existing = buttonObject.transform;
            }

            if (resetDataButton == null)
            {
                resetDataButton = existing.GetComponent<Button>() ??
                                  existing.gameObject.AddComponent<Button>();
            }
            Image buttonImage = existing.GetComponent<Image>() ??
                                existing.gameObject.AddComponent<Image>();
            resetDataButton.targetGraphic = buttonImage;

            if (created)
            {
                RectTransform buttonRect = resetDataButton.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0f, 1f);
                buttonRect.anchorMax = new Vector2(0f, 1f);
                buttonRect.pivot = new Vector2(0f, 1f);
                buttonRect.anchoredPosition = uiData != null
                    ? uiData.ResetDataButtonPosition
                    : new Vector2(130f, -354f);
                buttonRect.sizeDelta = uiData != null
                    ? uiData.ResetDataButtonSize
                    : new Vector2(300f, 46f);
            }

            if (resetDataLabel == null)
            {
                Transform labelTransform = existing.Find("Label");
                if (labelTransform == null)
                {
                    GameObject labelObject = new("Label", typeof(RectTransform),
                        typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    labelObject.transform.SetParent(existing, false);
                    labelTransform = labelObject.transform;
                }
                resetDataLabel = labelTransform.GetComponent<TextMeshProUGUI>();
            }

            if (resetDataLabel != null)
            {
                RectTransform labelRect = resetDataLabel.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                resetDataLabel.alignment = TextAlignmentOptions.Center;
                resetDataLabel.color = Color.white;
                resetDataLabel.fontSize = uiData != null ? uiData.ResetDataFontSize : 19f;
                resetDataLabel.raycastTarget = false;

                TextMeshProUGUI fontTemplate = closeButton != null
                    ? closeButton.GetComponentInChildren<TextMeshProUGUI>(true)
                    : null;
                if (fontTemplate != null && resetDataLabel.font == null)
                {
                    resetDataLabel.font = fontTemplate.font;
                }
            }
        }

        private void EnsureLanguageButton()
        {
            if (settingsPanel == null)
            {
                return;
            }

            Transform existing = settingsPanel.transform.Find("Language Toggle");
            bool created = existing == null;
            if (existing == null)
            {
                GameObject buttonObject = new("Language Toggle", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(settingsPanel.transform, false);
                existing = buttonObject.transform;
            }

            languageButton = existing.GetComponent<Button>() ??
                             existing.gameObject.AddComponent<Button>();
            Image image = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
            image.color = uiData != null ? uiData.LanguageButtonColor : new Color(0.2f, 0.65f, 0.94f);
            languageButton.targetGraphic = image;

            if (created)
            {
                RectTransform rect = existing.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = uiData != null
                    ? uiData.LanguageButtonPosition
                    : new Vector2(20f, -354f);
                rect.sizeDelta = uiData != null
                    ? uiData.LanguageButtonSize
                    : new Vector2(100f, 46f);
            }

            Transform labelTransform = existing.Find("Label");
            if (labelTransform == null)
            {
                GameObject labelObject = new("Label", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(existing, false);
                labelTransform = labelObject.transform;
            }
            languageLabel = labelTransform.GetComponent<TextMeshProUGUI>();
            RectTransform labelRect = languageLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            languageLabel.alignment = TextAlignmentOptions.Center;
            languageLabel.color = Color.white;
            languageLabel.fontSize = uiData != null ? uiData.LanguageButtonFontSize : 14f;
            languageLabel.raycastTarget = false;

            TextMeshProUGUI fontTemplate = closeButton != null
                ? closeButton.GetComponentInChildren<TextMeshProUGUI>(true)
                : null;
            if (fontTemplate != null && languageLabel.font == null)
            {
                languageLabel.font = fontTemplate.font;
            }
        }

        private void HandleLanguageClicked()
        {
            MiningLocalization.ToggleLanguage();
        }

        private void HandleReturnToMenuClicked()
        {
            ClosePanel();
            mainMenu?.OpenFromGameplay();
        }

        private void HandleLanguageChanged()
        {
            ApplyLanguage();
            ResetButtonState();
        }

        private void ApplyLanguage()
        {
            MiningLocalization.ApplyToHierarchy(transform.root);
            SetLocalizedChildText(openButton != null ? openButton.transform : null, "Label",
                "SETTINGS", "CÀI ĐẶT");
            SetLocalizedChildText(settingsPanel != null ? settingsPanel.transform : null,
                "Master Label", "MASTER VOLUME", "ÂM LƯỢNG TỔNG");
            SetLocalizedChildText(settingsPanel != null ? settingsPanel.transform : null,
                "Music Label", "MUSIC", "NHẠC");
            SetLocalizedChildText(settingsPanel != null ? settingsPanel.transform : null,
                "SFX Label", "SOUND EFFECTS", "HIỆU ỨNG");
            SetLocalizedChildText(settingsPanel != null ? settingsPanel.transform : null,
                "Mouse Label", "MOUSE SENSITIVITY", "ĐỘ NHẠY CHUỘT");
            TextMeshProUGUI title = settingsPanel != null
                ? settingsPanel.transform.Find("Header/Title")?.GetComponent<TextMeshProUGUI>()
                : null;
            if (title != null)
            {
                title.text = MiningLocalization.Text("SETTINGS");
            }
            if (languageLabel != null)
            {
                languageLabel.text = MiningLocalization.CurrentLanguageName switch
                {
                    MiningLocalization.EnglishLanguageName => "EN",
                    MiningLocalization.VietnameseLanguageName => "VI",
                    string languageName => languageName.ToUpperInvariant()
                };
            }
            if (returnToMenuLabel != null)
            {
                returnToMenuLabel.text = MiningLocalization.Text("MAIN MENU");
            }
            if (flatDesign && settingsPanel != null)
            {
                var root = settingsPanel.transform;
                SetLocalizedChildText(root, "Design Settings Title", "SETTINGS", "CÀI ĐẶT");
                SetLocalizedChildText(root, "Language Caption", "LANGUAGE", "NGÔN NGỮ");
                SetLocalizedChildText(root, "Language Hint", "Change interface language", "Đổi ngôn ngữ giao diện");
                SetLocalizedChildText(root, "Reset Caption", "RESET DATA", "XÓA DỮ LIỆU");
                SetLocalizedChildText(root, "Reset Hint", "Erase saved progress", "Xóa tiến trình đã lưu");
                SetLocalizedChildText(root, "Design Controls", "CONTROLS", "ĐIỀU KHIỂN");
                if (englishButton != null) englishButton.targetGraphic.color = MiningLocalization.IsEnglish ? new Color32(255,210,83,255) : Color.white;
                if (vietnameseButton != null) vietnameseButton.targetGraphic.color = !MiningLocalization.IsEnglish ? new Color32(255,210,83,255) : Color.white;
            }
        }

        private static void SetLocalizedChildText(Transform root, string childName,
            string english, string vietnamese)
        {
            if (root == null)
            {
                return;
            }

            TextMeshProUGUI[] labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI label in labels)
            {
                if (label.name != childName)
                {
                    continue;
                }

                label.text = MiningLocalization.Text(english, vietnamese);
                return;
            }
        }

        private void SelectEnglish() => MiningLocalization.SetLanguage(MiningLanguage.English);
        private void SelectVietnamese() => MiningLocalization.SetLanguage(MiningLanguage.Vietnamese);
    }
}
