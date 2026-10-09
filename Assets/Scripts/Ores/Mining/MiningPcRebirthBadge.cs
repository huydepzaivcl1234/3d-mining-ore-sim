using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Compact rebirth shortcut; the authored reward and confirmation UI stays in the details.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class MiningPcRebirthBadge : MonoBehaviour
    {
        [SerializeField] private MiningRebirthSystem rebirthSystem;
        [SerializeField] private MiningRebirthPanel rebirthPanel;
        [SerializeField] private GameObject details;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button closeButton;
        [SerializeField] private MiningUiPanelCoordinator panelCoordinator;
        private Button button;

        private void Awake() => button = GetComponent<Button>();

        private void OnEnable()
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.AddListener(ToggleDetails);
            if (closeButton != null) closeButton.onClick.AddListener(CloseDetails);
            if (rebirthSystem != null) rebirthSystem.StateChanged += Refresh;
            if (rebirthPanel != null) rebirthPanel.PanelOpened += CloseDetails;
            Refresh();
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(ToggleDetails);
            if (closeButton != null) closeButton.onClick.RemoveListener(CloseDetails);
            if (rebirthSystem != null) rebirthSystem.StateChanged -= Refresh;
            if (rebirthPanel != null) rebirthPanel.PanelOpened -= CloseDetails;
        }

        private void ToggleDetails()
        {
            if (details == null) return;
            if (details.activeSelf) { CloseDetails(); return; }
            if (panelCoordinator != null) panelCoordinator.OpenPanel(details.GetComponent<RectTransform>());
            else { details.SetActive(true); details.transform.SetAsLastSibling(); details.GetComponent<MiningPanelMotion>()?.PlayOpen(); }
        }

        private void CloseDetails()
        {
            if (details == null || !details.activeSelf) return;
            if (panelCoordinator != null) panelCoordinator.ClosePanel(details.GetComponent<RectTransform>());
            else if (details.TryGetComponent(out MiningPanelMotion motion)) motion.PlayClose();
            else details.SetActive(false);
        }

        private void Refresh()
        {
            if (label != null)
                label.text = rebirthSystem != null
                    ? $"R   x{rebirthSystem.PermanentMoneyMultiplier:0.##}"
                    : "R   REBIRTH";
        }
    }
}
