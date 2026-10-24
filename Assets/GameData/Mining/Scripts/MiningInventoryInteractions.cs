using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Presentation only. MiningItemSystem remains the sole inventory/save owner.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningInventoryInteractions : MonoBehaviour
    {
        [SerializeField] private MiningItemSystem items;
        [Header("Context menu (editable defaults)")]
        [SerializeField] private Vector2 menuSize = new(210f, 144f);
        [SerializeField, Min(1f)] private float menuFontSize = 22f;
        [SerializeField] private Color menuColor = new(.08f, .10f, .14f, .98f);
        [SerializeField] private RectTransform menuOverlay;
        [SerializeField] private RectTransform menu;
        [Header("Rejected drop")]
        [SerializeField, Min(0f)] private float returnSeconds = .18f;
        [SerializeField, Range(0f, 1f)] private float draggedSlotAlpha = .4f;
        private readonly Button[] useButtons = new Button[3];
        private int menuSlot = -1, dragSlot = -1;
        private MiningItemData menuItem, dragItem;
        private RectTransform ghost, sourceRect;
        private CanvasGroup sourceGroup;
        private float sourceAlpha;
        private bool accepted;
        private Coroutine returnRoutine;

        public void Configure(MiningItemSystem system)
        {
            items = system;
            EnsureMenu();
        }

        private void OnDisable() => CancelInteraction();

        public void EnsureMenu()
        {
            if (menuOverlay == null)
            {
                menuOverlay = NewRect("Item Context Menu", transform);
                menuOverlay.anchorMin = Vector2.zero;
                menuOverlay.anchorMax = Vector2.one;
                menuOverlay.offsetMin = menuOverlay.offsetMax = Vector2.zero;
                var image = menuOverlay.gameObject.AddComponent<UnityEngine.UI.Image>();
                image.color = Color.clear;
                var dismiss = menuOverlay.gameObject.AddComponent<UnityEngine.UI.Button>();
                dismiss.onClick.AddListener(DismissMenu);
            }
            var outside = menuOverlay.GetComponent<UnityEngine.UI.Button>();
            outside.onClick.RemoveListener(DismissMenu);
            outside.onClick.AddListener(DismissMenu);
            if (menu == null)
            {
                menu = NewRect("Options", menuOverlay);
                menu.anchorMin = menu.anchorMax = Vector2.zero;
                menu.pivot = new Vector2(0f, 1f);
                menu.sizeDelta = menuSize;
                menu.gameObject.AddComponent<UnityEngine.UI.Image>().color = menuColor;
            }
            for (int i = 0; i < useButtons.Length; i++)
            {
                string childName = i == 0 ? "Use 5" : i == 1 ? "Use 10" : "Use All";
                Transform existing = menu.Find(childName);
                RectTransform row = existing != null ? (RectTransform)existing : NewRect(childName, menu);
                if (existing == null)
                {
                    row.anchorMin = new Vector2(0f, 1f - (i + 1f) / 3f);
                    row.anchorMax = new Vector2(1f, 1f - i / 3f);
                    row.offsetMin = new Vector2(4f, 2f);
                    row.offsetMax = new Vector2(-4f, -2f);
                    row.gameObject.AddComponent<UnityEngine.UI.Image>().color = menuColor;
                    row.gameObject.AddComponent<UnityEngine.UI.Button>();
                    var label = NewRect("Label", row);
                    label.anchorMin = Vector2.zero; label.anchorMax = Vector2.one;
                    label.offsetMin = label.offsetMax = Vector2.zero;
                    var text = label.gameObject.AddComponent<TextMeshProUGUI>();
                    text.fontSize = menuFontSize; text.color = Color.white;
                    text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
                }
                useButtons[i] = row.GetComponent<UnityEngine.UI.Button>();
            }
            useButtons[0].onClick.RemoveListener(UseFive); useButtons[0].onClick.AddListener(UseFive);
            useButtons[1].onClick.RemoveListener(UseTen); useButtons[1].onClick.AddListener(UseTen);
            useButtons[2].onClick.RemoveListener(UseAll); useButtons[2].onClick.AddListener(UseAll);
            DismissMenu();
        }

        public void ShowMenu(int index, Vector2 screenPosition, Camera eventCamera)
        {
            if (items == null || ghost != null) return;
            var slot = items.GetSlot(index);
            if (slot.IsEmpty) return;
            if (slot.Item.IsEquipment) return; // Left-click equips / removes; bulk-use is consumables only.
            menuSlot = index; menuItem = slot.Item;
            bool timed = slot.Item.UseType == MiningItemUseType.TimedEffect;
            for (int i = 0; i < useButtons.Length; i++)
            {
                useButtons[i].interactable = timed;
                useButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = i == 0
                    ? MiningLocalization.Text("Use 5", "Dùng 5") : i == 1
                    ? MiningLocalization.Text("Use 10", "Dùng 10")
                    : MiningLocalization.Text("Use all", "Dùng tất cả");
            }
            menuOverlay.gameObject.SetActive(true);
            menuOverlay.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(menuOverlay, screenPosition,
                eventCamera, out Vector2 point);
            Rect bounds = menuOverlay.rect;
            menu.localPosition = new Vector3(
                Mathf.Clamp(point.x, bounds.xMin, Mathf.Max(bounds.xMin, bounds.xMax - menu.rect.width)),
                Mathf.Clamp(point.y, Mathf.Min(bounds.yMax, bounds.yMin + menu.rect.height), bounds.yMax), 0f);
        }

        public void DismissMenu()
        {
            menuSlot = -1; menuItem = null;
            if (menuOverlay != null) menuOverlay.gameObject.SetActive(false);
        }
        private void UseFive() => Use(5);
        private void UseTen() => Use(10);
        private void UseAll() => Use(int.MaxValue);
        private void Use(int count)
        {
            int index = menuSlot; MiningItemData expected = menuItem;
            DismissMenu();
            if (items != null && items.GetSlot(index).Item == expected)
                items.TryUseSlot(index, count);
        }

        public void BeginDrag(int index, RectTransform origin, PointerEventData e)
        {
            CancelInteraction();
            if (items == null || items.GetSlot(index).IsEmpty) return;
            dragSlot = index; dragItem = items.GetSlot(index).Item; sourceRect = origin;
            sourceGroup = origin.GetComponent<CanvasGroup>();
            if (sourceGroup == null) sourceGroup = origin.gameObject.AddComponent<CanvasGroup>();
            sourceAlpha = sourceGroup.alpha; sourceGroup.alpha = draggedSlotAlpha;
            // Clone the visual only; never clone slot listeners, buttons or inventory data.
            ghost = NewRect("Dragged Item", transform);
            ghost.sizeDelta = origin.rect.size;
            var background = ghost.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = menuColor; background.raycastTarget = false;
            var icon = NewRect("Icon", ghost);
            icon.anchorMin = Vector2.zero; icon.anchorMax = Vector2.one;
            icon.offsetMin = new Vector2(6f, 6f); icon.offsetMax = new Vector2(-6f, -6f);
            if (dragItem.InventoryIcon != null)
            {
                var image = icon.gameObject.AddComponent<UnityEngine.UI.Image>();
                image.sprite = dragItem.InventoryIcon; image.preserveAspect = true; image.raycastTarget = false;
            }
            else
            {
                var text = icon.gameObject.AddComponent<TextMeshProUGUI>();
                text.text = dragItem.IconFallback; text.color = dragItem.FallbackColor;
                text.fontSize = menuFontSize; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            }
            ghost.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            Drag(e);
        }

        public void Drag(PointerEventData e)
        {
            if (ghost == null || returnRoutine != null || e.button != PointerEventData.InputButton.Left) return;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)transform,
                e.position, e.pressEventCamera, out Vector3 point)) ghost.position = point;
        }

        public bool Drop(int destination)
        {
            if (ghost == null || accepted || returnRoutine != null || items == null) return false;
            accepted = items.TryMoveSlot(dragSlot, destination, dragItem);
            return accepted;
        }

        public void EndDrag()
        {
            if (ghost == null) return;
            RestoreSource();
            if (accepted || sourceRect == null || returnSeconds <= 0f) CancelInteraction();
            else returnRoutine = StartCoroutine(ReturnToSlot());
        }

        private IEnumerator ReturnToSlot()
        {
            Vector3 start = ghost.position;
            float elapsed = 0f;
            while (elapsed < returnSeconds && ghost != null && sourceRect != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / returnSeconds);
                ghost.position = Vector3.Lerp(start, sourceRect.position, t * t * (3f - 2f * t));
                yield return null;
            }
            returnRoutine = null;
            CancelInteraction();
        }

        private void RestoreSource()
        {
            if (sourceGroup != null) sourceGroup.alpha = sourceAlpha;
            sourceGroup = null;
        }
        public void CancelInteraction()
        {
            DismissMenu();
            if (returnRoutine != null) StopCoroutine(returnRoutine);
            returnRoutine = null;
            RestoreSource();
            if (ghost != null) Destroy(ghost.gameObject);
            ghost = null; sourceRect = null; dragItem = null; dragSlot = -1; accepted = false;
        }
        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
    }
}
