using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Animation-only companion for the existing MiningAudioSettingsPanel. It forwards the
    /// optional bottom close button through the already-wired top close button.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JuicySettingsPanel : AnimatedPanel
    {
        [SerializeField] private RectTransform panelBody;
        [SerializeField] private UnityEngine.UI.Button existingCloseButton;
        [SerializeField] private UnityEngine.UI.Button bottomCloseButton;
        [SerializeField] private TextMeshProUGUI bottomCloseLabel;
        [Min(0.01f), SerializeField] private float openDuration = 0.18f;
        [Range(0.5f, 1f), SerializeField] private float openStartScale = 0.88f;

        protected override RectTransform Body => panelBody;
        protected override float OpenDuration => openDuration;
        protected override float OpenStartScale => openStartScale;

        protected override void OnEnable()
        {
            MiningLocalization.LanguageChanged -= RefreshLocalizedText;
            MiningLocalization.LanguageChanged += RefreshLocalizedText;
            bottomCloseButton?.onClick.RemoveListener(CloseThroughExistingButton);
            bottomCloseButton?.onClick.AddListener(CloseThroughExistingButton);
            RefreshLocalizedText();

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            MiningLocalization.LanguageChanged -= RefreshLocalizedText;
            bottomCloseButton?.onClick.RemoveListener(CloseThroughExistingButton);
            base.OnDisable();
        }

        private void CloseThroughExistingButton()
        {
            // MiningAudioSettingsPanel already saves volume settings and closes via this Button.
            existingCloseButton?.onClick.Invoke();
        }

        private void RefreshLocalizedText()
        {
            if (bottomCloseLabel != null)
            {
                bottomCloseLabel.text = MiningLocalization.Text(
                    "CLOSE & SAVE SETTINGS", "ĐÓNG & LƯU CÀI ĐẶT");
            }
        }

    }
}
