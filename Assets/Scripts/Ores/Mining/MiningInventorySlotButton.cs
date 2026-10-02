using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MiningSimulator.Ores
{
    /// <summary>Routes one authored inventory slot button to its panel without lambda listeners.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningInventorySlotButton : MonoBehaviour, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private MiningInventoryPanel panel;
        [SerializeField, Min(0)] private int slotIndex;
        private Button button;
        private bool dragged;

        public void Configure(MiningInventoryPanel targetPanel, int targetSlotIndex)
        {
            panel = targetPanel;
            slotIndex = Mathf.Max(0, targetSlotIndex);
            button ??= GetComponent<Button>();
        }

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button ??= GetComponent<Button>();
            button?.onClick.RemoveListener(UseItem);
            button?.onClick.AddListener(UseItem);
        }

        private void OnDisable()
        {
            button?.onClick.RemoveListener(UseItem);
        }

        private void UseItem()
        {
            if (!dragged) panel?.UseSlot(slotIndex);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Right)
                panel?.Interactions?.ShowMenu(slotIndex, e.position, e.pressEventCamera);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            dragged = true;
            panel?.Interactions?.BeginDrag(slotIndex, (RectTransform)transform, e);
        }
        public void OnDrag(PointerEventData e) => panel?.Interactions?.Drag(e);
        public void OnEndDrag(PointerEventData e)
        {
            panel?.Interactions?.EndDrag();
            dragged = false;
        }
        public void OnDrop(PointerEventData e)
        {
            if (e.pointerDrag != null && e.pointerDrag.TryGetComponent(out MiningInventorySlotButton source) &&
                source.panel == panel) panel?.Interactions?.Drop(slotIndex);
        }
    }
}
