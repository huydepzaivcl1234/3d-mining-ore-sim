#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Authors the gameplay Settings controls so they remain editable in the Scene.</summary>
    public static class MiningGameplaySettingsSetupMenu
    {
        private const string DayNightDataPath = "Assets/GameData/DayNight/DayNightData.asset";
        private static readonly Vector2 PanelSize = new(640f, 540f);

        [MenuItem("Mining Simulator/Setup/Create Or Update Gameplay Settings Panel")]
        public static void CreateOrUpdateGameplaySettingsPanel()
        {
            MiningAudioSettingsPanel controller = Object.FindFirstObjectByType<
                MiningAudioSettingsPanel>(FindObjectsInactive.Include);
            if (controller == null)
            {
                Debug.LogError("Gameplay Settings setup could not find MiningAudioSettingsPanel.");
                return;
            }

            var controllerSerialized = new SerializedObject(controller);
            GameObject panelObject = controllerSerialized.FindProperty("settingsPanel")
                ?.objectReferenceValue as GameObject;
            if (panelObject == null)
            {
                Debug.LogError("MiningAudioSettingsPanel has no Settings Panel reference.", controller);
                return;
            }

            MiningUiData uiData = FindFirstAsset<MiningUiData>();
            MiningAudioManager audioManager = Object.FindFirstObjectByType<MiningAudioManager>(
                FindObjectsInactive.Include);
            MiningRebirthSystem rebirthSystem = Object.FindFirstObjectByType<MiningRebirthSystem>(
                FindObjectsInactive.Include);
            MiningMainMenu mainMenu = Object.FindFirstObjectByType<MiningMainMenu>(
                FindObjectsInactive.Include);

            Undo.RecordObject(panelObject, "Arrange Gameplay Settings Panel");
            panelObject.SetActive(true);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = PanelSize;

            Image panelImage = panelObject.GetComponent<Image>();
            if (panelImage != null && uiData != null)
            {
                Undo.RecordObject(panelImage, "Style Gameplay Settings Panel");
                panelImage.color = uiData.AudioPanelColor;
            }

            Transform panel = panelObject.transform;
            ArrangeHeader(panel, uiData);
            ArrangeAudioRow(panel, "Master", 36f, -116f);
            ArrangeAudioRow(panel, "Music", 36f, -194f);
            ArrangeAudioRow(panel, "SFX", 36f, -272f);
            ArrangeExisting(panel, "Close", 580f, -12f, 48f, 48f);

            Button languageButton = EnsureButton(panel, "Language Toggle", "Label", "EN ✓",
                30f, -365f, 170f, 54f,
                uiData != null ? uiData.LanguageButtonColor : new Color(0.2f, 0.65f, 0.94f),
                uiData, audioManager);
            Button resetButton = EnsureButton(panel, "Reset Data", "Label", "RESET DATA",
                215f, -365f, 395f, 54f,
                uiData != null ? uiData.ResetDataButtonColor : new Color(0.88f, 0.12f, 0.18f),
                uiData, audioManager);
            Button returnButton = EnsureButton(panel, "Return To Main Menu", "Return To Menu Label",
                "MAIN MENU", 30f, -445f, 580f, 56f,
                uiData != null ? uiData.NavigationButtonColor : new Color(0.12f, 0.55f, 0.88f),
                uiData, audioManager);

            SetReference(controllerSerialized, "audioManager", audioManager);
            SetReference(controllerSerialized, "uiData", uiData);
            SetReference(controllerSerialized, "rebirthSystem", rebirthSystem);
            SetReference(controllerSerialized, "mainMenu", mainMenu);
            SetReference(controllerSerialized, "languageButton", languageButton);
            SetReference(controllerSerialized, "languageLabel",
                FindLabel(languageButton.transform, "Label"));
            SetReference(controllerSerialized, "resetDataButton", resetButton);
            SetReference(controllerSerialized, "resetDataLabel",
                FindLabel(resetButton.transform, "Label"));
            SetReference(controllerSerialized, "returnToMenuButton", returnButton);
            SetReference(controllerSerialized, "returnToMenuLabel",
                FindLabel(returnButton.transform, "Return To Menu Label"));
            controllerSerialized.ApplyModifiedProperties();

            SetNamedLabel(panel, "Master Label", "MASTER VOLUME");
            SetNamedLabel(panel, "Music Label", "MUSIC");
            SetNamedLabel(panel, "SFX Label", "SOUND EFFECTS");
            SetNamedLabel(panel, "Title", "SETTINGS");
            SetOpenButtonLabel(controllerSerialized, "SETTINGS");

            MiningButtonSfxSetupMenu.AssignAllButtonSfx(false);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(panelObject.scene);
            Selection.activeGameObject = panelObject;
            EditorGUIUtility.PingObject(panelObject);
            Debug.Log("Gameplay Settings is visible for Scene editing. Save the scene. " +
                      "It will hide automatically when Play Mode starts.", panelObject);
        }

        [MenuItem("Mining Simulator/Fixes/Reduce Daylight Overexposure")]
        public static void ReduceDaylightOverexposure()
        {
            DayNightData data = AssetDatabase.LoadAssetAtPath<DayNightData>(DayNightDataPath);
            if (data == null)
            {
                Debug.LogError($"Could not find '{DayNightDataPath}'.");
                return;
            }

            Undo.RecordObject(data, "Reduce Daylight Overexposure");
            var serialized = new SerializedObject(data);
            ReduceIfHigher(serialized, "daySunIntensity", 1.12f);
            ReduceIfHigher(serialized, "daySkyExposure", 1f);
            ReduceIfHigher(serialized, "sunDiscIntensity", 1.2f);
            ReduceIfHigher(serialized, "dayBloomIntensity", 0.18f);
            ReduceIfHigher(serialized, "goldenHourBloomBoost", 0.18f);
            ReduceIfHigher(serialized, "dayPostExposure", -0.08f);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Selection.activeObject = data;
            Debug.Log("Daylight highlights were reduced without changing night lighting.", data);
        }

        private static void ArrangeHeader(Transform panel, MiningUiData uiData)
        {
            Transform header = panel.Find("Header");
            if (header == null)
            {
                return;
            }
            SetTopLeft(header.GetComponent<RectTransform>(), 0f, 0f, PanelSize.x, 74f);
            Image image = header.GetComponent<Image>();
            if (image != null && uiData != null)
            {
                Undo.RecordObject(image, "Style Gameplay Settings Header");
                image.color = uiData.AudioHeaderColor;
            }
        }

        private static void ArrangeAudioRow(Transform panel, string prefix, float left, float top)
        {
            ArrangeExisting(panel, prefix + " Label", left, top, 150f, 34f);
            ArrangeExisting(panel, prefix + " Slider", left + 162f, top, 330f, 28f);
            ArrangeExisting(panel, prefix + " Value", left + 504f, top, 70f, 34f);
        }

        private static void ArrangeExisting(Transform panel, string name, float left, float top,
            float width, float height)
        {
            Transform child = FindDescendant(panel, name);
            if (child != null)
            {
                SetTopLeft(child.GetComponent<RectTransform>(), left, top, width, height);
            }
        }

        private static Button EnsureButton(Transform parent, string name, string labelName,
            string text, float left, float top, float width, float height, Color color,
            MiningUiData uiData, MiningAudioManager audioManager)
        {
            Transform existing = parent.Find(name);
            GameObject target;
            if (existing == null)
            {
                target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(Button));
                Undo.RegisterCreatedObjectUndo(target, "Create " + name);
                target.transform.SetParent(parent, false);
            }
            else
            {
                target = existing.gameObject;
                Undo.RecordObject(target.transform, "Arrange " + name);
            }

            RectTransform rect = target.GetComponent<RectTransform>();
            SetTopLeft(rect, left, top, width, height);
            Image image = target.GetComponent<Image>() ?? Undo.AddComponent<Image>(target);
            Undo.RecordObject(image, "Style " + name);
            image.color = color;
            Button button = target.GetComponent<Button>() ?? Undo.AddComponent<Button>(target);
            Undo.RecordObject(button, "Configure " + name);
            button.targetGraphic = image;

            Transform labelTransform = target.transform.Find(labelName);
            if (labelTransform == null)
            {
                GameObject labelObject = new(labelName, typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(labelObject, "Create " + labelName);
                labelObject.transform.SetParent(target.transform, false);
                labelTransform = labelObject.transform;
            }
            TextMeshProUGUI label = labelTransform.GetComponent<TextMeshProUGUI>();
            Undo.RecordObject(label, "Style " + labelName);
            Stretch(label.rectTransform);
            label.text = text;
            label.fontSize = 20f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            SmoothButtonPunch punch = target.GetComponent<SmoothButtonPunch>() ??
                                      Undo.AddComponent<SmoothButtonPunch>(target);
            Undo.RecordObject(punch, "Configure " + name + " Animation");
            punch.SetTarget(rect);
            if (uiData != null)
            {
                punch.Configure(uiData.ButtonHoverScale, uiData.ButtonHoverPunchScale,
                    uiData.ButtonPressedScale, uiData.ButtonClickBounceScale,
                    uiData.ButtonHoverPunchDuration, uiData.ButtonHoverSettleDuration,
                    uiData.ButtonPressDuration, uiData.ButtonClickBounceDuration,
                    uiData.ButtonClickSettleDuration);
            }

            MiningButtonSfx sfx = target.GetComponent<MiningButtonSfx>() ??
                                  Undo.AddComponent<MiningButtonSfx>(target);
            var sfxSerialized = new SerializedObject(sfx);
            SetReference(sfxSerialized, "audioManager", audioManager);
            sfxSerialized.ApplyModifiedPropertiesWithoutUndo();
            return button;
        }

        private static void SetOpenButtonLabel(SerializedObject controller, string text)
        {
            Button openButton = controller.FindProperty("openButton")?.objectReferenceValue as Button;
            if (openButton == null)
            {
                return;
            }
            TextMeshProUGUI label = openButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                Undo.RecordObject(label, "Localize Gameplay Settings Button");
                label.text = text;
            }
        }

        private static void SetNamedLabel(Transform root, string name, string text)
        {
            Transform target = FindDescendant(root, name);
            TextMeshProUGUI label = target != null ? target.GetComponent<TextMeshProUGUI>() : null;
            if (label != null)
            {
                Undo.RecordObject(label, "Localize Gameplay Settings Label");
                label.text = text;
            }
        }

        private static TextMeshProUGUI FindLabel(Transform root, string name)
        {
            Transform target = root != null ? root.Find(name) : null;
            return target != null ? target.GetComponent<TextMeshProUGUI>() : null;
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

        private static void SetTopLeft(RectTransform rect, float left, float top,
            float width, float height)
        {
            if (rect == null)
            {
                return;
            }
            Undo.RecordObject(rect, "Arrange Gameplay Settings UI");
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(left + width * 0.5f, top - height * 0.5f);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            Undo.RecordObject(rect, "Stretch Gameplay Settings Label");
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetReference(SerializedObject serialized, string propertyName,
            Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null && value != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void ReduceIfHigher(SerializedObject serialized, string propertyName,
            float maximum)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null && property.floatValue > maximum)
            {
                property.floatValue = maximum;
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
