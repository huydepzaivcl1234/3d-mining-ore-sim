using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Owns the shared Shop window opened from the main menu or gameplay HUD.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningShopPanel : MonoBehaviour
    {
        [SerializeField] private MiningUiPanelCoordinator panelCoordinator;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private Button gameplayOpenButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI gameplayButtonLabel;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI messageLabel;

        private void Awake()
        {
            panelRoot?.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            gameplayOpenButton?.onClick.RemoveListener(Open);
            gameplayOpenButton?.onClick.AddListener(Open);
            closeButton?.onClick.RemoveListener(Close);
            closeButton?.onClick.AddListener(Close);
            MiningLocalization.LanguageChanged -= RefreshLocalization;
            MiningLocalization.LanguageChanged += RefreshLocalization;
            RefreshLocalization();
        }

        private void OnDisable()
        {
            gameplayOpenButton?.onClick.RemoveListener(Open);
            closeButton?.onClick.RemoveListener(Close);
            MiningLocalization.LanguageChanged -= RefreshLocalization;
        }

        public void Open()
        {
            if (panelRoot == null)
            {
                return;
            }

            RefreshLocalization();
            panelRoot.SetAsLastSibling();
            if (panelCoordinator != null)
            {
                panelCoordinator.OpenPanel(panelRoot);
            }
            else
            {
                panelRoot.gameObject.SetActive(true);
            }
            EventSystem.current?.SetSelectedGameObject(closeButton != null
                ? closeButton.gameObject
                : null);
        }

        public void Close()
        {
            if (panelRoot == null)
            {
                return;
            }

            if (panelCoordinator != null)
            {
                panelCoordinator.ClosePanel(panelRoot);
            }
            else
            {
                panelRoot.gameObject.SetActive(false);
            }
            EventSystem.current?.SetSelectedGameObject(gameplayOpenButton != null
                ? gameplayOpenButton.gameObject
                : null);
        }

        private void RefreshLocalization()
        {
            SetText(gameplayButtonLabel, "SHOP", "CỬA HÀNG");
            SetText(titleLabel, "SHOP", "CỬA HÀNG");
            SetText(messageLabel, "SHOP ITEMS WILL BE ADDED HERE",
                "VẬT PHẨM CỬA HÀNG SẼ ĐƯỢC THÊM TẠI ĐÂY");
        }

        private static void SetText(TextMeshProUGUI label, string english, string vietnamese)
        {
            if (label != null)
            {
                label.text = MiningLocalization.Text(english, vietnamese);
            }
        }
    }
}
