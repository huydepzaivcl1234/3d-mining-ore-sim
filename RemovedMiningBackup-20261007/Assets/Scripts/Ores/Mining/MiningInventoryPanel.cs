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
        [Tooltip("Authored item grid; leave empty to use the legacy Grid child.")]
        [SerializeField] private RectTransform itemGrid;
        [Tooltip("First necklace socket (legacy reference preserved).")]
        [SerializeField] private RectTransform necklaceSocket;
        [SerializeField] private RectTransform[] additionalNecklaceSockets = new RectTransform[2];
        [Header("Empty necklace socket hint")]
        [SerializeField] private Sprite necklacePlaceholder;
        [SerializeField] private Color necklacePlaceholderTint = new(1f, 1f, 1f, 0.25f);

        private readonly SlotView[] slotViews =
            new SlotView[MiningItemDatabase.InventoryCapacity + MiningItemSystem.NecklaceSlotCount];
        private TextMeshProUGUI openButtonLabel;
        private MiningGiftBoxWheelPanel giftBoxWheelPanel;
        public MiningInventoryInteractions Interactions { get; private set; }

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
            openButtonLabel = FindOpenButtonLabel(openButton);
            CacheSlotViews();
            PrepareInteractions();
            EnsureGiftBoxWheelPanel();
            inventoryPanel?.SetActive(false);
        }

        private static TextMeshProUGUI FindOpenButtonLabel(Button button)
        {
            if (button == null) return null;

            Transform named = button.transform.Find("Text (TMP)");
            if (named != null && named.TryGetComponent(out TextMeshProUGUI namedLabel))
            {
                return namedLabel;
            }

            foreach (TextMeshProUGUI label in
                     button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                return label;
            }
            return null;
        }

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
            MiningLocalization.LanguageChanged += HandleLanguageChanged;
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
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
            openButton?.onClick.RemoveListener(OpenPanel);
            closeButton?.onClick.RemoveListener(ClosePanel);
            if (itemSystem != null)
            {
                itemSystem.InventoryChanged -= Refresh;
            }
        }

        public void UseSlot(int index)
        {
            Interactions?.DismissMenu();
            if (itemSystem == null)
            {
                return;
            }

            MiningItemSystem.InventorySlotView slot = itemSystem.GetSlot(index);
            if (!slot.IsEmpty && slot.Item.UseType == MiningItemUseType.GiftBox)
            {
                EnsureGiftBoxWheelPanel();
                giftBoxWheelPanel?.Show(index, slot.Item);
                return;
            }
            itemSystem.TryUseSlot(index);
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
            Interactions?.CancelInteraction();
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
            Transform grid = itemGrid != null ? itemGrid : inventoryPanel.transform.Find("Grid");
            if (grid == null)
            {
                return;
            }
            for (int index = 0; index < MiningItemDatabase.InventoryCapacity; index++)
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
            CacheNecklaceSocket();
        }

        private void CacheNecklaceSocket()
        {
            // Serialized Unity references can be CLR-non-null while comparing equal to null.
            if (necklacePlaceholder == null)
                necklacePlaceholder = Resources.Load<Sprite>("NecklaceSlotPlaceholder");
            var equipmentPanel = inventoryPanel.transform.Find("Character And Equipment");
            if (necklaceSocket == null)
                necklaceSocket = inventoryPanel.transform.Find("Character And Equipment/Equipment Placeholder 01") as RectTransform;
            BindNecklaceSocket(necklaceSocket, MiningItemSystem.NecklaceSlotIndex);
            for (int i = 1; i < MiningItemSystem.NecklaceSlotCount; i++)
            {
                var socket = additionalNecklaceSockets != null && i - 1 < additionalNecklaceSockets.Length
                    ? additionalNecklaceSockets[i - 1] : null;
                // The four original armour placeholders must remain reserved.
                if (socket != null && (socket.name == "Equipment Placeholder 02" ||
                    socket.name == "Equipment Placeholder 03" || socket.name == "Equipment Placeholder 04" ||
                    socket.name == "Equipment Placeholder 05")) socket = null;
                if (socket == null && equipmentPanel != null)
                    socket = CreateNecklaceSocket(equipmentPanel, i);
                BindNecklaceSocket(socket, MiningItemSystem.NecklaceSlotIndex + i);
            }
        }

        [Header("Additional necklace slots (relative to the existing left slot)")]
        [SerializeField] private Vector2 upperNecklaceOffset = new(-117.6f, 58.8f);
        [SerializeField] private Vector2 lowerNecklaceOffset = new(-117.6f, -58.8f);

        private RectTransform CreateNecklaceSocket(Transform parent, int index)
        {
            string socketName = $"Equipment Placeholder {index + 5:00}";
            var existing = parent.Find(socketName) as RectTransform;
            if (existing != null) return existing;
            if (necklaceSocket == null) return null;

            var go = new GameObject(socketName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = necklaceSocket.anchorMin;
            rect.anchorMax = necklaceSocket.anchorMax;
            rect.pivot = necklaceSocket.pivot;
            rect.sizeDelta = necklaceSocket.sizeDelta;
            rect.anchoredPosition = necklaceSocket.anchoredPosition +
                (index == 1 ? upperNecklaceOffset : lowerNecklaceOffset);
            var template = necklaceSocket.GetComponent<Image>();
            var image = go.GetComponent<Image>();
            if (template != null)
            {
                image.sprite = template.sprite;
                image.color = template.color;
                image.type = template.type;
                image.material = template.material;
            }
            image.raycastTarget = true;
            var border = necklaceSocket.GetComponent<Outline>();
            if (border != null)
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = border.effectColor;
                outline.effectDistance = border.effectDistance;
                outline.useGraphicAlpha = border.useGraphicAlpha;
            }
            return rect;
        }

        private void BindNecklaceSocket(RectTransform socket, int index)
        {
            if (socket == null) return;
            var button = socket.GetComponent<UnityEngine.UI.Button>() ?? socket.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = socket.GetComponent<UnityEngine.UI.Image>();
            // The authored equipment frames were decorative (raycasts disabled).
            // Necklace sockets are now interactive, including when empty for drops.
            if (button.targetGraphic != null) button.targetGraphic.raycastTarget = true;
            var binding = socket.GetComponent<MiningInventorySlotButton>() ?? socket.gameObject.AddComponent<MiningInventorySlotButton>();
            binding.Configure(this, index);
            Transform iconChild = socket.Find("Necklace Icon");
            if (iconChild == null)
            {
                var go = new GameObject("Necklace Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                go.transform.SetParent(socket, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.one; rect.offsetMax = -Vector2.one;
                iconChild = rect;
            }
            var icon = iconChild.GetComponent<UnityEngine.UI.Image>();
            icon.preserveAspect = true; icon.raycastTarget = false;
            icon.enabled = false; // Empty sockets should show their frame, not a white icon.
            slotViews[index] = new SlotView { button = button, icon = icon };
        }

        private void Refresh()
        {
            if (openButtonLabel != null)
            {
                openButtonLabel.text = MiningLocalization.Text("Inventory", "TÚI ĐỒ");
            }
            if (titleLabel != null)
            {
                int occupied = itemSystem != null ? itemSystem.OccupiedSlotCount : 0;
                titleLabel.text = string.Format(MiningLocalization.Text(
                        "INVENTORY  •  {0}/{1} SLOTS", "TÚI ĐỒ  •  {0}/{1} Ô"),
                    occupied, MiningItemDatabase.InventoryCapacity);
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
                    bool showHint = !occupied && MiningItemSystem.IsNecklaceSlot(index);
                    view.icon.sprite = occupied ? slot.Item.InventoryIcon : showHint ? necklacePlaceholder : null;
                    view.icon.color = showHint ? necklacePlaceholderTint : Color.white;
                    view.icon.enabled = view.icon.sprite != null;
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
                        ? $"{slot.Item.DisplayName}\n{slot.Item.GetInventorySummary()}"
                        : MiningLocalization.Text("EMPTY", "TRỐNG");
                }
                if (view.countLabel != null)
                {
                    view.countLabel.text = occupied ? $"x{slot.Count}" : string.Empty;
                }
            }
        }

        private void HandleLanguageChanged()
        {
            Refresh();
        }

        public void PrepareInteractions()
        {
            if (inventoryPanel == null) return;
            Interactions = inventoryPanel.GetComponent<MiningInventoryInteractions>();
            if (Interactions == null) Interactions = inventoryPanel.AddComponent<MiningInventoryInteractions>();
            Interactions.Configure(itemSystem);
        }

        private void EnsureGiftBoxWheelPanel()
        {
            if (giftBoxWheelPanel != null || inventoryPanel == null ||
                inventoryPanel.transform.parent == null)
            {
                return;
            }

            Transform parent = inventoryPanel.transform.parent;
            Transform existing = parent.Find("Gift Box Wheel Panel");
            if (existing == null)
            {
                GameObject giftObject = new("Gift Box Wheel Panel", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                giftObject.transform.SetParent(parent, false);
                giftObject.SetActive(false);
                existing = giftObject.transform;
            }

            giftBoxWheelPanel = existing.GetComponent<MiningGiftBoxWheelPanel>() ??
                                existing.gameObject.AddComponent<MiningGiftBoxWheelPanel>();
            PlayerWallet wallet = FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            giftBoxWheelPanel.Configure(itemSystem, wallet, panelCoordinator, uiData,
                inventoryPanel.GetComponent<RectTransform>());
            giftBoxWheelPanel.gameObject.SetActive(false);
        }
    }
}
