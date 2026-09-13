#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Authors one editable Shop panel shared by the main menu and gameplay HUD.</summary>
    public static class MiningShopSetupMenu
    {
        private const string CanvasName = "Mining HUD Canvas";
        private const string PanelName = "Shop Panel";
        private const string OpenButtonName = "Shop Menu Button";

        [MenuItem("Mining Simulator/Setup/Create Or Update Shop Panel")]
        public static void CreateOrUpdateShopPanel()
        {
            Canvas canvas = FindHudCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Shop Setup",
                    $"Could not find the Canvas named '{CanvasName}' in the open scene.", "OK");
                return;
            }

            MiningUiData uiData = FindFirstAsset<MiningUiData>();
            RectTransform panel = EnsurePanel(canvas.transform, uiData);
            Button openButton = EnsureGameplayButton(canvas.transform, uiData);
            MiningShopPanel controller = canvas.GetComponent<MiningShopPanel>() ??
                                         Undo.AddComponent<MiningShopPanel>(canvas.gameObject);
            MiningUiPanelCoordinator coordinator = Object.FindFirstObjectByType<
                MiningUiPanelCoordinator>(FindObjectsInactive.Include);

            SerializedObject serialized = new(controller);
            SetReference(serialized, "panelCoordinator", coordinator);
            SetReference(serialized, "panelRoot", panel);
            SetReference(serialized, "gameplayOpenButton", openButton);
            SetReference(serialized, "closeButton", FindComponent<Button>(panel, "Close Button"));
            SetReference(serialized, "gameplayButtonLabel",
                FindComponent<TextMeshProUGUI>(openButton.transform, "Shop Button Label"));
            SetReference(serialized, "titleLabel",
                FindComponent<TextMeshProUGUI>(panel, "Shop Title"));
            SetReference(serialized, "messageLabel",
                FindComponent<TextMeshProUGUI>(panel, "Shop Message"));
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);

            MiningMainMenuSetupMenu.EnsureShopButtonForCurrentMenu();
            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            MiningButtonSfxSetupMenu.AssignAllButtonSfx(false);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = panel.gameObject;
            EditorGUIUtility.PingObject(panel.gameObject);
            EditorUtility.DisplayDialog("Shop Setup",
                "The shared Shop panel and both Shop entry buttons are ready. " +
                "The panel is visible for editing; save the scene when finished.", "OK");
        }

        private static RectTransform EnsurePanel(Transform canvas, MiningUiData uiData)
        {
            Transform existing = canvas.Find(PanelName);
            if (existing != null)
            {
                return existing.GetComponent<RectTransform>();
            }

            GameObject overlay = CreateImage(canvas, PanelName, new Color(0f, 0f, 0f, 0.74f));
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            Stretch(overlayRect);
            overlay.AddComponent<CanvasGroup>();

            Color cardColor = uiData != null
                ? uiData.InventoryPanelColor
                : new Color(0.04f, 0.07f, 0.13f, 1f);
            Color headerColor = uiData != null
                ? uiData.InventoryHeaderColor
                : new Color(0.52f, 0.27f, 0.88f, 1f);
            Color textColor = Color.white;

            GameObject cardObject = CreateImage(overlay.transform, "Shop Card", cardColor);
            RectTransform card = cardObject.GetComponent<RectTransform>();
            Center(card, Vector2.zero, new Vector2(780f, 560f));

            GameObject headerObject = CreateImage(card, "Header", headerColor);
            RectTransform header = headerObject.GetComponent<RectTransform>();
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = Vector2.one;
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(0f, 78f);
            CreateLabel(header, "Shop Title", "SHOP", Vector2.zero,
                new Vector2(560f, 70f), 38f, textColor, FontStyles.Bold);

            CreateLabel(card, "Shop Message", "SHOP ITEMS WILL BE ADDED HERE",
                new Vector2(0f, -25f), new Vector2(650f, 180f), 28f,
                new Color(0.78f, 0.84f, 0.95f, 1f), FontStyles.Bold);

            Button closeButton = CreateButton(header, "Close Button", "Close Label", "X",
                new Vector2(340f, -39f), new Vector2(64f, 56f),
                new Color(0.82f, 0.16f, 0.24f, 1f), textColor, 28f, uiData);
            closeButton.GetComponent<RectTransform>().SetAsLastSibling();
            return overlayRect;
        }

        private static Button EnsureGameplayButton(Transform canvas, MiningUiData uiData)
        {
            Transform existing = canvas.Find(OpenButtonName);
            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            Button button = CreateButton(canvas, OpenButtonName, "Shop Button Label", "SHOP",
                Vector2.zero, new Vector2(180f, 52f),
                new Color(0.52f, 0.27f, 0.88f, 1f), Color.white, 24f, uiData);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -342f);
            return button;
        }

        private static Button CreateButton(Transform parent, string name, string labelName,
            string text, Vector2 position, Vector2 size, Color color, Color textColor,
            float fontSize, MiningUiData uiData)
        {
            GameObject buttonObject = CreateImage(parent, name, color);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            Center(rect, position, size);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.78f, 0.68f, 1f, 1f);
            colors.pressedColor = new Color(0.40f, 0.20f, 0.70f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            SmoothButtonPunch punch = buttonObject.AddComponent<SmoothButtonPunch>();
            punch.SetTarget(rect);
            if (uiData != null)
            {
                punch.Configure(uiData.ButtonHoverScale, uiData.ButtonHoverPunchScale,
                    uiData.ButtonPressedScale, uiData.ButtonClickBounceScale,
                    uiData.ButtonHoverPunchDuration, uiData.ButtonHoverSettleDuration,
                    uiData.ButtonPressDuration, uiData.ButtonClickBounceDuration,
                    uiData.ButtonClickSettleDuration);
            }

            CreateLabel(rect, labelName, text, Vector2.zero, size, fontSize, textColor,
                FontStyles.Bold);
            return button;
        }

        private static GameObject CreateImage(Transform parent, string name, Color color)
        {
            GameObject value = new(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            Undo.RegisterCreatedObjectUndo(value, "Create " + name);
            value.transform.SetParent(parent, false);
            value.GetComponent<Image>().color = color;
            return value;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
            Vector2 position, Vector2 size, float fontSize, Color color, FontStyles style)
        {
            GameObject labelObject = new(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(labelObject, "Create " + name);
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = true;
            label.raycastTarget = false;
            Center(label.rectTransform, position, size);
            return label;
        }

        private static Canvas FindHudCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas != null && canvas.name == CanvasName)
                {
                    return canvas;
                }
            }
            return null;
        }

        private static T FindFirstAsset<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        private static T FindComponent<T>(Transform root, string name) where T : Component
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == name)
                {
                    return child.GetComponent<T>();
                }
            }
            return null;
        }

        private static void SetReference(SerializedObject serialized, string name, Object value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Center(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
#endif
