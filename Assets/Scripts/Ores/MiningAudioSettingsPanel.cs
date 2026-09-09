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
        [SerializeField] private TextMeshProUGUI masterValueLabel;
        [SerializeField] private TextMeshProUGUI musicValueLabel;
        [SerializeField] private TextMeshProUGUI sfxValueLabel;
        [SerializeField] private MiningUiData uiData;
        [SerializeField] private MiningRebirthSystem rebirthSystem;
        [SerializeField] private Button resetDataButton;
        [SerializeField] private TextMeshProUGUI resetDataLabel;

        private Coroutine resetStateCoroutine;
        private bool resetConfirmationArmed;

        public event Action PanelOpened;
        public event Action PanelClosed;

        private void Awake()
        {
            if (uiData == null && panelCoordinator != null)
            {
                uiData = panelCoordinator.UiData;
            }
            FindResetDependenciesIfMissing();
            EnsureResetDataButton();
            settingsPanel?.SetActive(false);
        }

        private void OnEnable()
        {
            openButton?.onClick.AddListener(OpenPanel);
            closeButton?.onClick.AddListener(ClosePanel);
            masterSlider?.onValueChanged.AddListener(SetMasterVolume);
            musicSlider?.onValueChanged.AddListener(SetMusicVolume);
            sfxSlider?.onValueChanged.AddListener(SetSfxVolume);
            resetDataButton?.onClick.RemoveListener(HandleResetDataClicked);
            resetDataButton?.onClick.AddListener(HandleResetDataClicked);
            ResetButtonState();
            RefreshFromManager();
        }

        private void OnDisable()
        {
            openButton?.onClick.RemoveListener(OpenPanel);
            closeButton?.onClick.RemoveListener(ClosePanel);
            masterSlider?.onValueChanged.RemoveListener(SetMasterVolume);
            musicSlider?.onValueChanged.RemoveListener(SetMusicVolume);
            sfxSlider?.onValueChanged.RemoveListener(SetSfxVolume);
            resetDataButton?.onClick.RemoveListener(HandleResetDataClicked);
            if (resetStateCoroutine != null)
            {
                StopCoroutine(resetStateCoroutine);
                resetStateCoroutine = null;
            }
            ResetButtonState();
        }

        private void OpenPanel()
        {
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
                SetResetButtonVisual("BẤM LẦN NỮA ĐỂ XÓA",
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
            rebirthSystem?.ResetAllProgress();
            SetResetButtonVisual("ĐÃ RESET DỮ LIỆU",
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
            SetResetButtonVisual("RESET DỮ LIỆU",
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
            if (title != null && title.text == "CÀI ĐẶT ÂM THANH")
            {
                title.text = "CÀI ĐẶT";
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
    }
}
