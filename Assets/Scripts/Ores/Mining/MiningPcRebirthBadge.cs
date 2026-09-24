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
        private Button button;

        private void Awake() => button = GetComponent<Button>();

        private void OnEnable()
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.AddListener(ToggleDetails);
            if (rebirthSystem != null) rebirthSystem.StateChanged += Refresh;
            if (rebirthPanel != null) rebirthPanel.PanelOpened += CloseDetails;
            Refresh();
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(ToggleDetails);
            if (rebirthSystem != null) rebirthSystem.StateChanged -= Refresh;
            if (rebirthPanel != null) rebirthPanel.PanelOpened -= CloseDetails;
        }

        private void ToggleDetails()
        {
            if (details != null) details.SetActive(!details.activeSelf);
        }

        private void CloseDetails()
        {
            if (details != null) details.SetActive(false);
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
