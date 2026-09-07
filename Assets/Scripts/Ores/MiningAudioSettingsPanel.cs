using System;
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

        public event Action PanelOpened;
        public event Action PanelClosed;

        private void Awake()
        {
            settingsPanel?.SetActive(false);
        }

        private void OnEnable()
        {
            openButton?.onClick.AddListener(OpenPanel);
            closeButton?.onClick.AddListener(ClosePanel);
            masterSlider?.onValueChanged.AddListener(SetMasterVolume);
            musicSlider?.onValueChanged.AddListener(SetMusicVolume);
            sfxSlider?.onValueChanged.AddListener(SetSfxVolume);
            RefreshFromManager();
        }

        private void OnDisable()
        {
            openButton?.onClick.RemoveListener(OpenPanel);
            closeButton?.onClick.RemoveListener(ClosePanel);
            masterSlider?.onValueChanged.RemoveListener(SetMasterVolume);
            musicSlider?.onValueChanged.RemoveListener(SetMusicVolume);
            sfxSlider?.onValueChanged.RemoveListener(SetSfxVolume);
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
    }
}
