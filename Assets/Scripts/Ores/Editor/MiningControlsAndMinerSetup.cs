#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace MiningSimulator.Ores.Editor
{
    public static class MiningControlsAndMinerSetup
    {
        [MenuItem("Mining Simulator/Setup/Controls And Miner Health")]
        public static void Setup()
        {
            var settings = Object.FindFirstObjectByType<MiningAudioSettingsPanel>(FindObjectsInactive.Include);
            if (settings == null) { Debug.LogError("Settings controller not found."); return; }
            var so = new SerializedObject(settings);
            var panel = so.FindProperty("settingsPanel").objectReferenceValue as GameObject;
            var input = Object.FindFirstObjectByType<PlayerInput>(FindObjectsInactive.Include);
            MiningOrbitCamera rig = null;
            foreach (var candidate in Object.FindObjectsByType<MiningOrbitCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (candidate.FollowTarget != null) { rig = candidate; break; }
            var menu = panel != null ? panel.transform.Find("Return To Main Menu") : null;
            if (panel == null || menu == null || input == null || rig == null) { Debug.LogError("Existing settings/player references missing."); return; }
            if (panel.transform.Find("Controls Panel") == null)
            {
                var template = menu.GetComponent<Button>();
                var font = template.GetComponentInChildren<TextMeshProUGUI>(true);
                var menuRect = menu.GetComponent<RectTransform>();
                Undo.RecordObject(menuRect, "Fit Controls button");
                menuRect.sizeDelta = new Vector2(300f, menuRect.sizeDelta.y);
                var pos = menuRect.anchoredPosition; pos.x = -160f; menuRect.anchoredPosition = pos;
                var open = MakeButton(panel.transform, "Controls", template, font, "CONTROLS", new Vector2(160f, pos.y), new Vector2(300f, menuRect.sizeDelta.y));
                var overlay = NewObject("Controls Panel", panel.transform, typeof(Image));
                var rect = overlay.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
                overlay.GetComponent<Image>().color = new Color(.16f, .08f, .025f, 1f);
                var status = MakeText(overlay.transform, "Status", font, "CLICK TO CHANGE KEY", new Vector2(0f, 265f), new Vector2(610f, 40f));
                var names = new[] { "MOVE FORWARD", "MOVE BACKWARD", "MOVE LEFT", "MOVE RIGHT", "JUMP", "SPRINT", "CAMERA LOCK" };
                var parts = new[] { "up", "down", "left", "right", "", "", "" };
                var rows = new MiningKeybindSettings.BindingRow[names.Length];
                for (int i = 0; i < rows.Length; i++)
                {
                    float y = 202f - i * 60f;
                    var label = MakeText(overlay.transform, "Action " + i, font, names[i], new Vector2(-130f, y), new Vector2(300f, 45f));
                    var button = MakeButton(overlay.transform, "Binding " + i, template, font, "—", new Vector2(170f, y), new Vector2(230f, 46f));
                    rows[i] = new MiningKeybindSettings.BindingRow { labelKey = names[i], actionName = i < 4 ? "Move" : i == 4 ? "Jump" : "Sprint", compositePart = parts[i], cameraLock = i == 6, button = button, label = label, value = button.GetComponentInChildren<TextMeshProUGUI>() };
                }
                var back = MakeButton(overlay.transform, "Back", template, font, "BACK", new Vector2(-160f, -260f), new Vector2(300f, 56f));
                var reset = MakeButton(overlay.transform, "Reset Keys", template, font, "RESET KEYS", new Vector2(160f, -260f), new Vector2(300f, 56f));
                var component = settings.GetComponent<MiningKeybindSettings>() ?? Undo.AddComponent<MiningKeybindSettings>(settings.gameObject);
                var bindings = new SerializedObject(component);
                bindings.FindProperty("playerInput").objectReferenceValue = input;
                bindings.FindProperty("cameraRig").objectReferenceValue = rig;
                bindings.FindProperty("controlsPanel").objectReferenceValue = overlay;
                bindings.FindProperty("openButton").objectReferenceValue = open;
                bindings.FindProperty("backButton").objectReferenceValue = back;
                bindings.FindProperty("resetButton").objectReferenceValue = reset;
                bindings.FindProperty("statusLabel").objectReferenceValue = status;
                var array = bindings.FindProperty("rows"); array.arraySize = rows.Length;
                for (int i = 0; i < rows.Length; i++)
                {
                    var r = array.GetArrayElementAtIndex(i); var row = rows[i];
                    r.FindPropertyRelative("labelKey").stringValue = row.labelKey;
                    r.FindPropertyRelative("actionName").stringValue = row.actionName;
                    r.FindPropertyRelative("compositePart").stringValue = row.compositePart;
                    r.FindPropertyRelative("cameraLock").boolValue = row.cameraLock;
                    r.FindPropertyRelative("button").objectReferenceValue = row.button;
                    r.FindPropertyRelative("label").objectReferenceValue = row.label;
                    r.FindPropertyRelative("value").objectReferenceValue = row.value;
                }
                bindings.ApplyModifiedProperties(); overlay.SetActive(false);
                EditorSceneManager.MarkSceneDirty(panel.scene);
            }
            // Keep repeated setup safe, including the authored footer's anchor convention.
            var footer = menu.GetComponent<RectTransform>();
            Undo.RecordObject(footer, "Fit Controls footer");
            footer.anchorMin = footer.anchorMax = footer.pivot = new Vector2(.5f, .5f);
            footer.anchoredPosition = new Vector2(-160f, -198f); footer.sizeDelta = new Vector2(300f, 56f);
            foreach (string name in new[] { "Base_Shadow", "Button_Body", "Return To Menu Label", "Medal" })
            {
                RectTransform childRect = null;
                foreach (var candidate in menu.GetComponentsInChildren<RectTransform>(true))
                    if (candidate.name == name) { childRect = candidate; break; }
                if (childRect == null) continue;
                Undo.RecordObject(childRect, "Fit Controls footer skin");
                if (name == "Medal") childRect.anchoredPosition = new Vector2(-116f, 0f);
                else childRect.sizeDelta = new Vector2(name == "Return To Menu Label" ? 224f : 300f, childRect.sizeDelta.y);
            }
            var controlsRoot = panel.transform.Find("Controls Panel");
            foreach (var button in controlsRoot.GetComponentsInChildren<Button>(true))
            {
                var skin = button.targetGraphic as Image;
                if (skin != null) { Undo.RecordObject(skin, "Readable Controls skin"); skin.color = new Color(.42f, .22f, .07f, 1f); }
            }
            var controlsButton = panel.transform.Find("Controls").GetComponent<RectTransform>();
            Undo.RecordObject(controlsButton, "Fit Controls footer");
            controlsButton.anchorMin = controlsButton.anchorMax = controlsButton.pivot = new Vector2(.5f,.5f);
            controlsButton.anchoredPosition = new Vector2(160f,-198f); controlsButton.sizeDelta = new Vector2(300f,56f);
            var openSkin = controlsButton.GetComponent<Image>(); Undo.RecordObject(openSkin, "Readable Controls skin"); openSkin.color = new Color(.42f,.22f,.07f,1f);
            EditorSceneManager.MarkSceneDirty(panel.scene);
            var healthPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Plugins/Microlight/MicroBar/Prefabs/SimpleBars/Sprite_SimpleMicroBarSRP.prefab");
            if (healthPrefab == null) { Debug.LogError("Shared MicroBar prefab missing; assign Miner Health Bar Prefab in NpcData."); return; }
            var shop = Object.FindFirstObjectByType<NpcShop>(FindObjectsInactive.Include);
            if (shop != null && shop.NpcData != null && healthPrefab != null)
            {
                var data = new SerializedObject(shop.NpcData);
                if (data.FindProperty("minerHealthBarPrefab").objectReferenceValue == null)
                {
                    Undo.RecordObject(shop.NpcData, "Miner health display");
                    data.FindProperty("minerHealthBarPrefab").objectReferenceValue = healthPrefab;
                    data.ApplyModifiedProperties(); EditorUtility.SetDirty(shop.NpcData);
                    AssetDatabase.SaveAssetIfDirty(shop.NpcData);
                }
            }
            Debug.Log("Controls and miner health configured. Save your scene when ready.");
        }
        private static GameObject NewObject(string name, Transform parent, params System.Type[] components)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            foreach (var type in components) obj.AddComponent(type);
            Undo.RegisterCreatedObjectUndo(obj, "Add Controls UI"); return obj;
        }
        private static TextMeshProUGUI MakeText(Transform parent, string name, TextMeshProUGUI font, string key, Vector2 pos, Vector2 size)
        {
            var text = NewObject(name, parent, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.font = font.font; text.fontSize = 23f; text.color = Color.white; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            text.text = MiningLocalization.Text(key); text.rectTransform.anchoredPosition = pos; text.rectTransform.sizeDelta = size; return text;
        }
        private static Button MakeButton(Transform parent, string name, Button template, TextMeshProUGUI font, string key, Vector2 pos, Vector2 size)
        {
            var obj = NewObject(name, parent, typeof(Image), typeof(Button));
            var image = obj.GetComponent<Image>(); var original = template.targetGraphic as Image;
            if (original != null) { image.sprite = original.sprite; image.type = original.type; image.color = original.color; }
            var button = obj.GetComponent<Button>(); button.targetGraphic = image; button.colors = template.colors;
            var rect = obj.GetComponent<RectTransform>(); rect.anchoredPosition = pos; rect.sizeDelta = size;
            MakeText(obj.transform, "Label", font, key, Vector2.zero, size);
            return button;
        }
    }
}
#endif
