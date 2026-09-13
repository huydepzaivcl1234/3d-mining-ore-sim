#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Authors the editable startup menu and assigns the generated Gem sprite.</summary>
    public static class MiningMainMenuSetupMenu
    {
        private const string CanvasName = "Mining HUD Canvas";
        private const string MenuName = "Main Menu";
        private const string DataFolder = "Assets/GameData/UI";
        private const string DataPath = DataFolder + "/MiningMainMenuData.asset";
        private const string GemIconPath = "Assets/Ores/Icons/GemCurrencyIcon.png";

        [MenuItem("Mining Simulator/Setup/Create Or Update Main Menu")]
        public static void CreateOrUpdateMainMenu()
        {
            Canvas canvas = FindHudCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Main Menu Setup",
                    $"Could not find the Canvas named '{CanvasName}' in the open scene.", "OK");
                return;
            }

            MiningMainMenuData data = LoadOrCreateData();
            Sprite gemSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GemIconPath);
            AssignGemIcon(gemSprite);

            Transform existing = canvas.transform.Find(MenuName);
            bool created = existing == null;
            GameObject root = created ? CreateRoot(canvas, data) : existing.gameObject;
            MiningMainMenu mainMenu = root.GetComponent<MiningMainMenu>() ??
                                      Undo.AddComponent<MiningMainMenu>(root);
            CanvasGroup group = root.GetComponent<CanvasGroup>() ??
                                Undo.AddComponent<CanvasGroup>(root);

            RectTransform card = FindRect(root.transform, "Main Menu Card");
            Button playButton = FindComponent<Button>(root.transform, "Play Button");
            TextMeshProUGUI title = FindComponent<TextMeshProUGUI>(root.transform, "Title");
            TextMeshProUGUI subtitle = FindComponent<TextMeshProUGUI>(root.transform, "Subtitle");
            TextMeshProUGUI playLabel = FindComponent<TextMeshProUGUI>(root.transform,
                "Play Label");

            SerializedObject serialized = new(mainMenu);
            SetIfMissing(serialized, "data", data);
            SetIfMissing(serialized, "canvasGroup", group);
            SetIfMissing(serialized, "card", card);
            SetIfMissing(serialized, "playButton", playButton);
            SetIfMissing(serialized, "titleLabel", title);
            SetIfMissing(serialized, "subtitleLabel", subtitle);
            SetIfMissing(serialized, "playLabel", playLabel);
            serialized.ApplyModifiedProperties();

            root.SetActive(true);
            root.transform.SetAsLastSibling();
            EditorUtility.SetDirty(mainMenu);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            EditorUtility.DisplayDialog("Main Menu Setup",
                created
                    ? "Main Menu and Gem icon are ready. Edit the selected panel, then save the scene."
                    : "Main Menu references and Gem icon are updated. Your existing layout was preserved.",
                "OK");
        }

        private static GameObject CreateRoot(Canvas canvas, MiningMainMenuData data)
        {
            GameObject root = new(MenuName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(CanvasGroup));
            Undo.RegisterCreatedObjectUndo(root, "Create Mining Main Menu");
            root.transform.SetParent(canvas.transform, false);
            Stretch(root.GetComponent<RectTransform>());

            Image backdrop = root.GetComponent<Image>();
            backdrop.color = data.BackdropColor;
            backdrop.raycastTarget = true;

            GameObject cardObject = CreateImage(root.transform, "Main Menu Card", data.CardColor);
            RectTransform card = cardObject.GetComponent<RectTransform>();
            Center(card, Vector2.zero, data.CardSize);

            Sprite gemSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GemIconPath);
            GameObject iconObject = CreateImage(card, "Gem Icon", Color.white);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            Center(iconRect, data.GemIconPosition, data.GemIconSize);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = gemSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            CreateLabel(card, "Title", data.EnglishTitle, data.TitlePosition,
                data.TitleSize, data.TitleFontSize, data.TitleColor, FontStyles.Bold);
            CreateLabel(card, "Subtitle", data.EnglishSubtitle, data.SubtitlePosition,
                data.SubtitleSize, data.SubtitleFontSize, data.SubtitleColor,
                FontStyles.Normal);

            GameObject buttonObject = CreateImage(card, "Play Button", data.PlayButtonColor);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            Center(buttonRect, data.PlayButtonPosition, data.PlayButtonSize);
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            MiningUiData uiData = FindFirstAsset<MiningUiData>();
            if (uiData == null || uiData.SmoothButtonAnimationEnabled)
            {
                SmoothButtonPunch punch = buttonObject.AddComponent<SmoothButtonPunch>();
                if (uiData != null)
                {
                    punch.Configure(uiData.ButtonHoverScale, uiData.ButtonHoverPunchScale,
                        uiData.ButtonPressedScale, uiData.ButtonClickBounceScale,
                        uiData.ButtonHoverPunchDuration, uiData.ButtonHoverSettleDuration,
                        uiData.ButtonPressDuration, uiData.ButtonClickBounceDuration,
                        uiData.ButtonClickSettleDuration);
                }
            }

            CreateLabel(buttonRect, "Play Label", data.EnglishPlayLabel, Vector2.zero,
                data.PlayButtonSize, data.PlayFontSize, data.PlayTextColor, FontStyles.Bold);
            root.AddComponent<MiningMainMenu>();
            return root;
        }

        private static void AssignGemIcon(Sprite gemSprite)
        {
            if (gemSprite == null)
            {
                Debug.LogWarning($"Gem icon was not imported at {GemIconPath}.");
                return;
            }

            MiningUiData uiData = FindFirstAsset<MiningUiData>();
            if (uiData != null)
            {
                SerializedObject uiSerialized = new(uiData);
                SerializedProperty iconProperty = uiSerialized.FindProperty("gemIconSprite");
                if (iconProperty != null && iconProperty.objectReferenceValue == null)
                {
                    iconProperty.objectReferenceValue = gemSprite;
                    uiSerialized.ApplyModifiedProperties();
                    EditorUtility.SetDirty(uiData);
                }
            }

            GameObject gemHud = GameObject.Find("Gem HUD");
            Transform iconTransform = gemHud != null ? gemHud.transform.Find("Gem Icon") : null;
            if (iconTransform == null)
            {
                return;
            }

            Image image = iconTransform.GetComponent<Image>();
            if (image != null && image.sprite == null)
            {
                Undo.RecordObject(image, "Assign Gem Currency Icon");
                image.sprite = gemSprite;
                image.color = Color.white;
                image.preserveAspect = true;
                EditorUtility.SetDirty(image);
            }

            Transform fallback = iconTransform.Find("Fallback");
            if (fallback != null && image != null && image.sprite != null)
            {
                Undo.RecordObject(fallback.gameObject, "Hide Gem Icon Fallback");
                fallback.gameObject.SetActive(false);
            }
        }

        private static MiningMainMenuData LoadOrCreateData()
        {
            MiningMainMenuData data = AssetDatabase.LoadAssetAtPath<MiningMainMenuData>(DataPath);
            if (data != null)
            {
                return data;
            }

            if (!AssetDatabase.IsValidFolder("Assets/GameData"))
            {
                AssetDatabase.CreateFolder("Assets", "GameData");
            }
            if (!AssetDatabase.IsValidFolder(DataFolder))
            {
                AssetDatabase.CreateFolder("Assets/GameData", "UI");
            }

            data = ScriptableObject.CreateInstance<MiningMainMenuData>();
            AssetDatabase.CreateAsset(data, DataPath);
            AssetDatabase.SaveAssets();
            return data;
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

        private static GameObject CreateImage(Transform parent, string name, Color color)
        {
            GameObject value = new(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            value.transform.SetParent(parent, false);
            Image image = value.GetComponent<Image>();
            image.color = color;
            return value;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
            Vector2 position, Vector2 size, float fontSize, Color color, FontStyles style)
        {
            GameObject labelObject = new(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            Center(label.rectTransform, position, size);
            return label;
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

        private static RectTransform FindRect(Transform root, string name)
        {
            Transform value = FindDescendant(root, name);
            return value != null ? value.GetComponent<RectTransform>() : null;
        }

        private static T FindComponent<T>(Transform root, string name) where T : Component
        {
            Transform value = FindDescendant(root, name);
            return value != null ? value.GetComponent<T>() : null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == name)
                {
                    return child;
                }
            }
            return null;
        }

        private static void SetIfMissing(SerializedObject serialized, string name, Object value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null && property.objectReferenceValue == null && value != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static T FindFirstAsset<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }
    }
}
#endif
