using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Event-driven 32-slot inventory panel. Clicking a non-empty slot consumes one item.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningInventoryPanel : MonoBehaviour
    {
        private sealed class SlotView
        {
            public Button button;
            public Image icon;
            public TextMeshProUGUI fallback;
            public TextMeshProUGUI nameLabel;
            public TextMeshProUGUI countLabel;
        }

        [SerializeField] private MiningItemSystem itemSystem;
        [SerializeField] private MiningUiPanelCoordinator panelCoordinator;
        [SerializeField] private MiningUiData uiData;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleLabel;

        private readonly SlotView[] slotViews =
            new SlotView[MiningItemDatabase.InventoryCapacity];

        public event Action PanelOpened;
        public event Action PanelClosed;

        private void Awake()
        {
            if (uiData == null && panelCoordinator != null)
            {
                uiData = panelCoordinator.UiData;
            }
            panelCoordinator?.RegisterInventoryUi(
                openButton != null ? openButton.GetComponent<RectTransform>() : null,
                inventoryPanel != null ? inventoryPanel.GetComponent<RectTransform>() : null);
            CacheSlotViews();
            inventoryPanel?.SetActive(false);
        }

        private void OnEnable()
        {
            openButton?.onClick.RemoveListener(OpenPanel);
            openButton?.onClick.AddListener(OpenPanel);
            closeButton?.onClick.RemoveListener(ClosePanel);
            closeButton?.onClick.AddListener(ClosePanel);
            if (itemSystem != null)
            {
                itemSystem.InventoryChanged -= Refresh;
                itemSystem.InventoryChanged += Refresh;
            }
            Refresh();
        }

        private void OnDisable()
        {
            openButton?.onClick.RemoveListener(OpenPanel);
            closeButton?.onClick.RemoveListener(ClosePanel);
            if (itemSystem != null)
            {
                itemSystem.InventoryChanged -= Refresh;
            }
        }

        public void UseSlot(int index)
        {
            itemSystem?.TryUseSlot(index);
        }

        private void OpenPanel()
        {
            Refresh();
            if (panelCoordinator != null)
            {
                panelCoordinator.OpenPanel(inventoryPanel != null
                    ? inventoryPanel.GetComponent<RectTransform>()
                    : null);
            }
            else
            {
                inventoryPanel?.SetActive(true);
            }
            PanelOpened?.Invoke();
        }

        private void ClosePanel()
        {
            if (panelCoordinator != null)
            {
                panelCoordinator.ClosePanel(inventoryPanel != null
                    ? inventoryPanel.GetComponent<RectTransform>()
                    : null);
            }
            else
            {
                inventoryPanel?.SetActive(false);
            }
            PanelClosed?.Invoke();
        }

        private void CacheSlotViews()
        {
            if (inventoryPanel == null)
            {
                return;
            }
            Transform grid = inventoryPanel.transform.Find("Grid");
            if (grid == null)
            {
                return;
            }
            for (int index = 0; index < slotViews.Length; index++)
            {
                Transform slot = grid.Find($"Slot {index + 1:00}");
                if (slot == null)
                {
                    continue;
                }
                MiningInventorySlotButton binding =
                    slot.GetComponent<MiningInventorySlotButton>() ??
                    slot.gameObject.AddComponent<MiningInventorySlotButton>();
                binding.Configure(this, index);
                slotViews[index] = new SlotView
                {
                    button = slot.GetComponent<Button>(),
                    icon = slot.Find("Icon")?.GetComponent<Image>(),
                    fallback = slot.Find("Fallback")?.GetComponent<TextMeshProUGUI>(),
                    nameLabel = slot.Find("Name")?.GetComponent<TextMeshProUGUI>(),
                    countLabel = slot.Find("Count")?.GetComponent<TextMeshProUGUI>()
                };
            }
        }

        private void Refresh()
        {
            if (titleLabel != null)
            {
                int occupied = itemSystem != null ? itemSystem.OccupiedSlotCount : 0;
                titleLabel.text = $"TÚI ĐỒ  •  {occupied}/{MiningItemDatabase.InventoryCapacity} Ô";
            }

            for (int index = 0; index < slotViews.Length; index++)
            {
                SlotView view = slotViews[index];
                if (view == null)
                {
                    continue;
                }
                MiningItemSystem.InventorySlotView slot = itemSystem != null
                    ? itemSystem.GetSlot(index)
                    : new MiningItemSystem.InventorySlotView(null, 0);
                bool occupied = !slot.IsEmpty;
                if (view.button != null)
                {
                    view.button.interactable = occupied;
                }
                if (view.icon != null)
                {
                    view.icon.sprite = occupied ? slot.Item.InventoryIcon : null;
                    view.icon.enabled = occupied && slot.Item.InventoryIcon != null;
                }
                if (view.fallback != null)
                {
                    view.fallback.text = occupied ? slot.Item.IconFallback : string.Empty;
                    view.fallback.color = occupied ? slot.Item.FallbackColor : Color.clear;
                    view.fallback.enabled = occupied && slot.Item.InventoryIcon == null;
                }
                if (view.nameLabel != null)
                {
                    view.nameLabel.text = occupied
                        ? $"{slot.Item.DisplayName}\n{slot.Item.ShortEffectName} +{slot.Item.EffectPercent:0.##}%"
                        : "TRỐNG";
                }
                if (view.countLabel != null)
                {
                    view.countLabel.text = occupied ? $"x{slot.Count}" : string.Empty;
                }
            }
        }
    }
}
