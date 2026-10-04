using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Open animation and optional bottom-close forwarding for the authored Quest card.</summary>
    [DisallowMultipleComponent]
    public sealed class JuicyQuestPanel : AnimatedPanel
    {
        [SerializeField] private RectTransform panelBody;
        [SerializeField] private MiningQuestPanel questPanel;
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
            bottomCloseButton?.onClick.RemoveListener(CloseThroughExistingPanel);
            bottomCloseButton?.onClick.AddListener(CloseThroughExistingPanel);
            RefreshLocalizedText();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            MiningLocalization.LanguageChanged -= RefreshLocalizedText;
            bottomCloseButton?.onClick.RemoveListener(CloseThroughExistingPanel);
            base.OnDisable();
        }

        private void CloseThroughExistingPanel()
        {
            questPanel?.Close();
        }

        private void RefreshLocalizedText()
        {
            if (bottomCloseLabel != null)
            {
                bottomCloseLabel.text = MiningLocalization.Text(
                    "CLOSE QUEST PANEL", "ĐÓNG BẢNG NHIỆM VỤ");
            }
        }

    }
}
