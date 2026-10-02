using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Opt-in authoring only. Existing item objects, bindings and sprites remain intact.</summary>
    public static class MiningInventoryCharacterSetup
    {
        [MenuItem("Mining Simulator/UI/Setup Character Inventory And Readable Effects")]
        public static void Setup()
        {
            if (Application.isPlaying) { Debug.LogWarning("Exit Play Mode before authoring inventory."); return; }
            var inventory = Object.FindFirstObjectByType<MiningInventoryPanel>(FindObjectsInactive.Include);
            var player = Object.FindFirstObjectByType<MiningPlayerStats>(FindObjectsInactive.Include);
            if (inventory == null || player == null) { Debug.LogWarning("Gameplay inventory/player missing."); return; }
            var serialized = new SerializedObject(inventory);
            var panel = serialized.FindProperty("inventoryPanel").objectReferenceValue as GameObject;
            var data = serialized.FindProperty("uiData").objectReferenceValue as MiningUiData;
            if (data == null)
                data = AssetDatabase.LoadAssetAtPath<MiningUiData>("Assets/GameData/UI/MiningUiData.asset");
            if (panel == null || data == null) return;
            Undo.RegisterFullObjectHierarchyUndo(panel, "Character Inventory Layout");
            var rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = data.InventoryPanelSize;
            var image = panel.GetComponent<Image>();
            if (image != null) { image.sprite = null; image.color = data.InventoryPanelColor; }
            var header = panel.transform.Find("Header") as RectTransform;
            if (header != null)
            {
                header.sizeDelta = data.InventoryHeaderSize;
                var headerImage = header.GetComponent<Image>();
                if (headerImage != null) headerImage.color = data.InventoryHeaderColor;
            }
            var close = panel.transform.Find("Close") as RectTransform;
            if (close != null) At(close, data.InventoryCloseButtonPosition, data.InventoryCloseButtonSize);

            var equipment = Ensure(panel.transform, "Character And Equipment");
            At(equipment, data.InventoryEquipmentLayoutPosition,
                new Vector2(105f, 96f) * data.InventoryEquipmentLayoutScale);
            var svg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameData/UI/InventoryReferences/Group 1.svg");
            var equipmentGraphic = equipment.GetComponent<Image>() ?? Undo.AddComponent<Image>(equipment.gameObject);
            equipmentGraphic.sprite = svg;
            // This supplied SVG wraps PNGs in <pattern>; Vector Graphics imports a Sprite but drops those fills.
            // Keep it as an authoring reference, and draw editable equivalents rather than invisible slots.
            equipmentGraphic.color = Color.clear;
            equipmentGraphic.raycastTarget = false;
            var portrait = Ensure(equipment, "Character Model Preview");
            var scale = data.InventoryEquipmentLayoutScale;
            At(portrait, new Vector2(data.InventoryModelRectPosition.x, -data.InventoryModelRectPosition.y) * scale,
                data.InventoryModelRectSize * scale);
            var black = portrait.GetComponent<Image>() ?? Undo.AddComponent<Image>(portrait.gameObject);
            black.color = Color.black;
            black.raycastTarget = false;
            var display = Ensure(portrait, "Model RenderTexture");
            display.anchorMin = Vector2.zero; display.anchorMax = Vector2.one;
            display.offsetMin = Vector2.one * 3f; display.offsetMax = -Vector2.one * 3f;
            var raw = display.GetComponent<RawImage>() ?? Undo.AddComponent<RawImage>(display.gameObject);
            raw.raycastTarget = false;
            var preview = portrait.GetComponent<MiningInventoryModelPreview>() ?? Undo.AddComponent<MiningInventoryModelPreview>(portrait.gameObject);
            var previewData = new SerializedObject(preview);
            previewData.FindProperty("player").objectReferenceValue = player.transform;
            previewData.FindProperty("visualRoot").objectReferenceValue = player.transform.Find("Geometry");
            previewData.FindProperty("output").objectReferenceValue = raw;
            previewData.ApplyModifiedProperties();
            var points = data.InventoryEquipmentSlotPositions;
            for (int i = 0; i < points.Length; i++)
            {
                var slot = Ensure(equipment, "Equipment Placeholder " + (i + 1).ToString("00"));
                At(slot, new Vector2(points[i].x, -points[i].y) * scale, data.InventoryEquipmentSlotSize * scale);
                var face = slot.GetComponent<Image>() ?? Undo.AddComponent<Image>(slot.gameObject);
                face.color = data.InventoryEquipmentSlotColor;
                face.raycastTarget = false; // Reserved visuals: cannot spend/equip anything yet.
                var outline = slot.GetComponent<Outline>() ?? Undo.AddComponent<Outline>(slot.gameObject);
                outline.effectColor = data.InventoryEquipmentOutlineColor;
                outline.effectDistance = new Vector2(2f, -2f);
                outline.enabled = true;
            }
            // Keep the direct Grid path for existing scripts and external authored references.
            var grid = panel.transform.Find("Grid") as RectTransform;
            if (grid != null)
            {
                serialized.FindProperty("itemGrid").objectReferenceValue = grid;
                for (int i = 0; i < MiningItemDatabase.InventoryCapacity; i++)
                {
                    var slot = grid.Find("Slot " + (i + 1).ToString("00")) as RectTransform;
                    if (slot == null) continue;
                    At(slot, data.InventoryFirstSlotPosition + new Vector2(i % data.InventoryGridColumns * data.InventorySlotSpacing.x,
                        -i / data.InventoryGridColumns * data.InventorySlotSpacing.y), data.InventorySlotSize);
                    var slotImage = slot.GetComponent<Image>();
                    if (slotImage != null)
                    {
                        slotImage.sprite = null;
                        slotImage.color = data.InventorySlotColor;
                        var border = slot.GetComponent<Outline>() ?? Undo.AddComponent<Outline>(slot.gameObject);
                        border.effectColor = data.InventoryEquipmentOutlineColor;
                        border.effectDistance = new Vector2(1f, -1f);
                    }
                    var icon = slot.Find("Icon") as RectTransform;
                    if (icon != null) At(icon, new Vector2(20f, -5f), new Vector2(56f, 42f));
                    var fallback = slot.Find("Fallback") as RectTransform;
                    if (fallback != null) At(fallback, new Vector2(20f, -5f), new Vector2(56f, 42f));
                    var name = slot.Find("Name")?.GetComponent<TextMeshProUGUI>();
                    if (name != null)
                    {
                        At(name.rectTransform, new Vector2(4f, -49f), new Vector2(data.InventorySlotSize.x - 8f, data.InventorySlotSize.y - 52f));
                        name.fontSize = data.InventoryItemFontSize;
                        name.enableAutoSizing = false;
                        name.overflowMode = TextOverflowModes.Ellipsis;
                    }
                    var count = slot.Find("Count")?.GetComponent<TextMeshProUGUI>();
                    if (count != null) count.fontSize = data.InventoryCountFontSize;
                }
            }
            serialized.ApplyModifiedProperties();
            var effects = Object.FindFirstObjectByType<MiningEffectToast>(FindObjectsInactive.Include);
            if (effects != null)
            {
                var effectData = new SerializedObject(effects);
                var root = effectData.FindProperty("toastRoot").objectReferenceValue as GameObject;
                if (root != null)
                {
                    Undo.RecordObject(root.GetComponent<RectTransform>(), "Readable Status Effects");
                    root.GetComponent<RectTransform>().sizeDelta = data.EffectToastSize;
                }
            }
            if (player.GetComponent<PlayerMonsterHeadDeflection>() == null) Undo.AddComponent<PlayerMonsterHeadDeflection>(player.gameObject);
            EditorSceneManager.MarkSceneDirty(panel.scene);
            Selection.activeGameObject = panel;
            Debug.Log("Inventory authored: model preview + equipment placeholders + original item bindings. Save the scene manually (Ctrl+S).");
        }

        private static RectTransform Ensure(Transform parent, string name)
        {
            var found = parent.Find(name) as RectTransform;
            if (found != null) return found;
            var child = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, "Inventory UI");
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }
        private static void At(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
