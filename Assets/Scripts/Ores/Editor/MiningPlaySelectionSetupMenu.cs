#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Authors the Load Game/New Game choice into the open Main Menu scene.</summary>
    public static class MiningPlaySelectionSetupMenu
    {
        private const string PanelSpritePath =
            "Assets/Generated/MiningUI/SettingsLeatherRounded.png";
        private const string ButtonSpritePath =
            "Assets/Generated/MiningUI/UpgradeLeatherRounded.png";
        private const string MedalSpritePath =
            "Assets/Generated/MiningUI/BrassMedalCircle.png";
        private const string LoadIconPath = "Assets/Ores/Icons/Play.png";
        private const string NewGameIconPath = "Assets/Prefabs/UI/UpgradeOreDamage.png";

        [MenuItem("Mining Simulator/UI/Build Play Selection")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Exit Play Mode",
                    "Stop Play Mode before building the Play Selection panel.", "OK");
                return;
            }

            MiningMainMenu mainMenu = Object.FindFirstObjectByType<MiningMainMenu>(
                FindObjectsInactive.Include);
            if (mainMenu == null)
            {
                EditorUtility.DisplayDialog("Main Menu Not Found",
                    "Open the gameplay scene and build the Main Menu first.", "OK");
                return;
            }

            SerializedObject menuFields = new(mainMenu);
            GameObject mainViewObject = menuFields.FindProperty("mainView")?.objectReferenceValue
                as GameObject;
            Button playButton = menuFields.FindProperty("playButton")?.objectReferenceValue
                as Button;
            if (mainViewObject == null || playButton == null)
            {
                EditorUtility.DisplayDialog("Main Menu Wiring Incomplete",
                    "Run Mining Simulator > Setup > Create Or Update Main Menu first.", "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Play Selection");

            // MiningMainMenu owns the Play flow at runtime. Remove old Inspector callbacks
            // that can invoke a cinematic directly and bypass the save-choice panel.
            int persistentPlayCallbacks = playButton.onClick.GetPersistentEventCount();
            if (persistentPlayCallbacks > 0)
            {
                Undo.RecordObject(playButton, "Remove Bypassing Play Callbacks");
                for (int index = persistentPlayCallbacks - 1; index >= 0; index--)
                {
                    UnityEventTools.RemovePersistentListener(playButton.onClick, index);
                }
                EditorUtility.SetDirty(playButton);
            }

            RectTransform mainView = mainViewObject.transform as RectTransform;
            RectTransform overlay = EnsureImage(mainView, "Play Selection Overlay", out bool overlayCreated);
            if (overlayCreated) Stretch(overlay);
            Image overlayImage = overlay.GetComponent<Image>();
            Undo.RecordObject(overlayImage, "Style Play Selection Overlay");
            overlayImage.sprite = null;
            overlayImage.color = new Color(0.015f, 0.004f, 0.008f, 0.62f);
            overlayImage.raycastTarget = true;
            CanvasGroup overlayGroup = overlay.GetComponent<CanvasGroup>() ??
                                       Undo.AddComponent<CanvasGroup>(overlay.gameObject);
            Undo.RecordObject(overlayGroup, "Configure Play Selection Overlay");
            overlayGroup.alpha = 0f;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false;

            RectTransform panel = EnsureImage(overlay, "Play Selection Panel", out bool panelCreated);
            if (panelCreated) SetRect(panel, Vector2.zero, new Vector2(640f, 400f));
            StylePanel(panel);

            TextMeshProUGUI title = EnsureLabel(panel, "Selection Title", out bool titleCreated);
            if (titleCreated) SetRect(title.rectTransform, new Vector2(-15f, 158f),
                new Vector2(430f, 48f));
            StyleLabel(title, "SELECT GAME", 25f, new Color(1f, 0.83f, 0.38f, 1f),
                TextAlignmentOptions.Center, FontStyles.Bold);
            title.characterSpacing = 3f;

            Button backButton = EnsureButton(panel, "Back Button", out bool backCreated);
            if (backCreated) SetRect(backButton.transform as RectTransform,
                new Vector2(282f, 160f), new Vector2(46f, 46f));
            StyleButton(backButton, new Color(0.46f, 0.12f, 0.035f, 1f),
                new Color(0.18f, 0.025f, 0.01f, 1f), false);
            TextMeshProUGUI backLabel = EnsureLabel(backButton.transform, "Back Label",
                out bool backLabelCreated);
            if (backLabelCreated) Stretch(backLabel.rectTransform);
            StyleLabel(backLabel, "X", 22f, Color.white, TextAlignmentOptions.Center,
                FontStyles.Bold);

            Button loadButton = EnsureButton(panel, "Load Game Button", out bool loadCreated);
            if (loadCreated) SetRect(loadButton.transform as RectTransform,
                new Vector2(0f, 68f), new Vector2(552f, 112f));
            StyleButton(loadButton, new Color(0.18f, 0.67f, 0.25f, 1f),
                new Color(0.045f, 0.23f, 0.08f, 1f), true);
            CreateButtonContents(loadButton.transform as RectTransform, true,
                out TextMeshProUGUI loadLabel, out TextMeshProUGUI saveStats);

            Button newGameButton = EnsureButton(panel, "New Game Button", out bool newCreated);
            if (newCreated) SetRect(newGameButton.transform as RectTransform,
                new Vector2(0f, -72f), new Vector2(552f, 112f));
            StyleButton(newGameButton, new Color(0.48f, 0.27f, 0.10f, 1f),
                new Color(0.16f, 0.055f, 0.012f, 1f), false);
            CreateButtonContents(newGameButton.transform as RectTransform, false,
                out TextMeshProUGUI newGameLabel, out TextMeshProUGUI newGameDescription);

            JuicyPlaySelection selection = mainMenu.GetComponent<JuicyPlaySelection>() ??
                                            Undo.AddComponent<JuicyPlaySelection>(mainMenu.gameObject);
            SerializedObject selectionFields = new(selection);
            SetReference(selectionFields, "mainMenu", mainMenu);
            SetReference(selectionFields, "rebirthSystem",
                Object.FindFirstObjectByType<MiningRebirthSystem>(FindObjectsInactive.Include));
            SetReference(selectionFields, "wallet",
                Object.FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include));
            SetReference(selectionFields, "progressionSystem",
                Object.FindFirstObjectByType<NpcProgressionSystem>(FindObjectsInactive.Include));
            SetReference(selectionFields, "playButtonRect", playButton.transform as RectTransform);
            SetReference(selectionFields, "overlayRect", overlay);
            SetReference(selectionFields, "selectionPanelRect", panel);
            SetReference(selectionFields, "selectionCanvasGroup", overlayGroup);
            SetReference(selectionFields, "loadGameButton", loadButton);
            SetReference(selectionFields, "newGameButton", newGameButton);
            SetReference(selectionFields, "backButton", backButton);
            SetReference(selectionFields, "loadGameButtonImage", loadButton.GetComponent<Image>());
            SetReference(selectionFields, "titleLabel", title);
            SetReference(selectionFields, "loadGameLabel", loadLabel);
            SetReference(selectionFields, "saveStatsLabel", saveStats);
            SetReference(selectionFields, "newGameLabel", newGameLabel);
            SetReference(selectionFields, "newGameDescriptionLabel", newGameDescription);
            selectionFields.ApplyModifiedProperties();

            SetReference(menuFields, "playSelection", selection);
            menuFields.ApplyModifiedProperties();

            overlay.SetAsLastSibling();
            overlay.gameObject.SetActive(true);
            SetLayerRecursively(overlay, mainMenu.gameObject.layer);
            MiningButtonSfxSetupMenu.AssignAllButtonSfx(false);
            EditorUtility.SetDirty(mainMenu);
            EditorUtility.SetDirty(selection);
            EditorSceneManager.MarkSceneDirty(mainMenu.gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = overlay.gameObject;
            EditorGUIUtility.PingObject(overlay.gameObject);
            Debug.Log("Play Selection authored into the Scene. Play now opens Load Game/New Game; " +
                      "New Game uses the existing full progression reset and both choices keep the " +
                      "current cinematic transition. Use Preview Play Selection when editing, then " +
                      "Hide Play Selection Preview before saving.", mainMenu);
        }

        [MenuItem("Mining Simulator/UI/Build Play Selection", true)]
        private static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem("Mining Simulator/UI/Preview Play Selection")]
        private static void ShowPreview() => SetPreviewVisible(true);

        [MenuItem("Mining Simulator/UI/Hide Play Selection Preview")]
        private static void HidePreview() => SetPreviewVisible(false);

        private static void SetPreviewVisible(bool visible)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            MiningMainMenu mainMenu = Object.FindFirstObjectByType<MiningMainMenu>(
                FindObjectsInactive.Include);
            SerializedObject fields = mainMenu != null ? new SerializedObject(mainMenu) : null;
            GameObject mainView = fields?.FindProperty("mainView")?.objectReferenceValue as GameObject;
            Transform overlay = mainView != null
                ? mainView.transform.Find("Play Selection Overlay")
                : null;
            CanvasGroup group = overlay != null ? overlay.GetComponent<CanvasGroup>() : null;
            if (overlay == null || group == null)
            {
                Debug.LogWarning("Build Play Selection first. No preview object was found.");
                return;
            }

            Undo.RecordObject(overlay.gameObject, "Toggle Play Selection Preview");
            Undo.RecordObject(group, "Toggle Play Selection Preview");
            overlay.gameObject.SetActive(true);
            group.alpha = visible ? 1f : 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            EditorUtility.SetDirty(group);
            EditorSceneManager.MarkSceneDirty(mainMenu.gameObject.scene);
            if (visible)
            {
                Selection.activeGameObject = overlay.gameObject;
                EditorGUIUtility.PingObject(overlay.gameObject);
            }
        }

        private static void CreateButtonContents(RectTransform button, bool loadGame,
            out TextMeshProUGUI title, out TextMeshProUGUI subtitle)
        {
            RectTransform socket = EnsureImage(button, "Icon Socket", out bool socketCreated);
            if (socketCreated) SetRect(socket, new Vector2(-222f, 0f), new Vector2(72f, 72f));
            Image socketImage = socket.GetComponent<Image>();
            Undo.RecordObject(socketImage, "Style Play Selection Icon Socket");
            socketImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MedalSpritePath);
            socketImage.type = Image.Type.Simple;
            socketImage.color = Color.white;
            socketImage.raycastTarget = false;

            RectTransform icon = EnsureImage(socket, "Icon", out bool iconCreated);
            if (iconCreated) SetRect(icon, Vector2.zero, new Vector2(48f, 48f));
            Image iconImage = icon.GetComponent<Image>();
            Undo.RecordObject(iconImage, "Assign Play Selection Icon");
            iconImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(loadGame
                ? LoadIconPath
                : NewGameIconPath);
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            title = EnsureLabel(button, loadGame ? "Load Game Label" : "New Game Label",
                out bool titleCreated);
            if (titleCreated) SetRect(title.rectTransform, new Vector2(38f, 20f),
                new Vector2(392f, 42f));
            StyleLabel(title, loadGame ? "LOAD GAME" : "NEW GAME", 27f,
                loadGame ? Color.white : new Color(1f, 0.83f, 0.38f, 1f),
                TextAlignmentOptions.Left, FontStyles.Bold);
            title.characterSpacing = 2f;

            subtitle = EnsureLabel(button, loadGame ? "Save Stats" : "New Game Description",
                out bool subtitleCreated);
            if (subtitleCreated) SetRect(subtitle.rectTransform, new Vector2(38f, -23f),
                new Vector2(392f, 32f));
            StyleLabel(subtitle, loadGame ? "NO SAVE DATA" : "Start a fresh mining journey",
                16f, loadGame ? new Color(0.72f, 1f, 0.78f, 1f)
                               : new Color(1f, 0.72f, 0.48f, 1f),
                TextAlignmentOptions.Left, loadGame ? FontStyles.Bold : FontStyles.Italic);
        }

        private static void StylePanel(RectTransform panel)
        {
            Image image = panel.GetComponent<Image>();
            Undo.RecordObject(image, "Style Play Selection Panel");
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = true;

            Shadow shadow = panel.GetComponent<Shadow>() ?? Undo.AddComponent<Shadow>(panel.gameObject);
            Undo.RecordObject(shadow, "Style Play Selection Shadow");
            shadow.effectColor = new Color(0.025f, 0.005f, 0.002f, 0.9f);
            shadow.effectDistance = new Vector2(0f, -10f);
            shadow.useGraphicAlpha = true;

            Outline outline = panel.GetComponent<Outline>() ?? Undo.AddComponent<Outline>(panel.gameObject);
            Undo.RecordObject(outline, "Style Play Selection Border");
            outline.effectColor = new Color(0.84f, 0.56f, 0.17f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = true;

            _ = panel.GetComponent<JuicyButtonTrim>() ?? Undo.AddComponent<JuicyButtonTrim>(panel.gameObject);
        }

        private static void StyleButton(Button button, Color top, Color bottom, bool primary)
        {
            Image image = button.GetComponent<Image>();
            Undo.RecordObject(image, "Style Play Selection Button");
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSpritePath);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = true;
            button.targetGraphic = image;

            MiningUiGradient gradient = button.GetComponent<MiningUiGradient>() ??
                                        Undo.AddComponent<MiningUiGradient>(button.gameObject);
            Undo.RecordObject(gradient, "Style Play Selection Gradient");
            gradient.SetColors(top, bottom);

            JuicyButtonTrim trim = button.GetComponent<JuicyButtonTrim>() ??
                                   Undo.AddComponent<JuicyButtonTrim>(button.gameObject);
            Undo.RecordObject(trim, "Style Play Selection Trim");
            trim.SetMedalRivets(false);

            Shadow shadow = button.GetComponent<Shadow>() ?? Undo.AddComponent<Shadow>(button.gameObject);
            Undo.RecordObject(shadow, "Style Play Selection Button Shadow");
            shadow.effectColor = new Color(0.04f, 0.008f, 0.003f, 0.9f);
            shadow.effectDistance = new Vector2(0f, primary ? -7f : -6f);
            shadow.useGraphicAlpha = true;

            SmoothButtonPunch punch = button.GetComponent<SmoothButtonPunch>() ??
                                      Undo.AddComponent<SmoothButtonPunch>(button.gameObject);
            punch.SetTarget(button.transform as RectTransform);

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.46f, 0.46f, 0.46f, 0.72f);
            button.colors = colors;
        }

        private static Button EnsureButton(Transform parent, string name, out bool created)
        {
            RectTransform rect = EnsureImage(parent, name, out created);
            Button button = rect.GetComponent<Button>() ?? Undo.AddComponent<Button>(rect.gameObject);
            button.targetGraphic = rect.GetComponent<Image>();
            return button;
        }

        private static RectTransform EnsureImage(Transform parent, string name, out bool created)
        {
            Transform existing = parent != null ? parent.Find(name) : null;
            if (existing is RectTransform rect)
            {
                created = false;
                _ = rect.GetComponent<CanvasRenderer>() ?? Undo.AddComponent<CanvasRenderer>(rect.gameObject);
                _ = rect.GetComponent<Image>() ?? Undo.AddComponent<Image>(rect.gameObject);
                return rect;
            }

            GameObject child = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
            Undo.SetTransformParent(child.transform, parent, $"Parent {name}");
            created = true;
            return child.GetComponent<RectTransform>();
        }

        private static TextMeshProUGUI EnsureLabel(Transform parent, string name, out bool created)
        {
            Transform existing = parent != null ? parent.Find(name) : null;
            if (existing != null && existing.TryGetComponent(out TextMeshProUGUI label))
            {
                created = false;
                return label;
            }

            GameObject child = new(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
            Undo.SetTransformParent(child.transform, parent, $"Parent {name}");
            created = true;
            return child.GetComponent<TextMeshProUGUI>();
        }

        private static void StyleLabel(TextMeshProUGUI label, string text, float fontSize,
            Color color, TextAlignmentOptions alignment, FontStyles style)
        {
            Undo.RecordObject(label, "Style Play Selection Text");
            label.text = text;
            label.font = label.font != null ? label.font : TMP_Settings.defaultFontAsset;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(10f, fontSize * 0.65f);
            label.fontSizeMax = fontSize;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            label.outlineColor = new Color32(47, 16, 3, 255);
            label.outlineWidth = 0.12f;
        }

        private static void SetReference(SerializedObject serialized, string propertyName,
            Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root) SetLayerRecursively(child, layer);
        }
    }
}
#endif
