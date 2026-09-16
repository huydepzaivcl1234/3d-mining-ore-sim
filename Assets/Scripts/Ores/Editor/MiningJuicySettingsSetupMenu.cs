#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>
    /// One-shot authoring tool for both Settings pages. Runtime code keeps ownership of volume,
    /// language, reset-data and navigation behavior; this tool only authors visuals.
    /// </summary>
    public static class MiningJuicySettingsSetupMenu
    {
        private const string SpriteDirectory = "Assets/Generated/MiningUI";
        private static Sprite roundedSprite;
        private static Sprite circleSprite;

        [MenuItem("Mining Simulator/UI/Build Juicy Gameplay Settings")]
        public static void Build()
        {
            MiningAudioSettingsPanel controller = Object.FindFirstObjectByType<MiningAudioSettingsPanel>(
                FindObjectsInactive.Include);
            if (controller == null)
            {
                Debug.LogWarning("Open the gameplay scene first: MiningAudioSettingsPanel was not found. No scene was changed.");
                return;
            }

            SerializedObject fields = new(controller);
            GameObject panelObject = Reference<GameObject>(fields, "settingsPanel");
            UnityEngine.UI.Button closeButton = Reference<UnityEngine.UI.Button>(fields, "closeButton");
            UnityEngine.UI.Slider master = Reference<UnityEngine.UI.Slider>(fields, "masterSlider");
            UnityEngine.UI.Slider music = Reference<UnityEngine.UI.Slider>(fields, "musicSlider");
            UnityEngine.UI.Slider sfx = Reference<UnityEngine.UI.Slider>(fields, "sfxSlider");
            TextMeshProUGUI masterValue = Reference<TextMeshProUGUI>(fields, "masterValueLabel");
            TextMeshProUGUI musicValue = Reference<TextMeshProUGUI>(fields, "musicValueLabel");
            TextMeshProUGUI sfxValue = Reference<TextMeshProUGUI>(fields, "sfxValueLabel");
            UnityEngine.UI.Button languageButton = Reference<UnityEngine.UI.Button>(fields, "languageButton");
            TextMeshProUGUI languageLabel = Reference<TextMeshProUGUI>(fields, "languageLabel");
            UnityEngine.UI.Button resetButton = Reference<UnityEngine.UI.Button>(fields, "resetDataButton");
            TextMeshProUGUI resetLabel = Reference<TextMeshProUGUI>(fields, "resetDataLabel");
            UnityEngine.UI.Button menuButton = Reference<UnityEngine.UI.Button>(fields, "returnToMenuButton");
            TextMeshProUGUI menuLabel = Reference<TextMeshProUGUI>(fields, "returnToMenuLabel");
            RectTransform panel = panelObject != null ? panelObject.transform as RectTransform : null;
            RectTransform header = panel != null ? panel.Find("Header") as RectTransform : null;
            TextMeshProUGUI title = header != null
                ? header.Find("Title")?.GetComponent<TextMeshProUGUI>()
                : null;

            if (panel == null || header == null || title == null || closeButton == null ||
                master == null || music == null || sfx == null || masterValue == null ||
                musicValue == null || sfxValue == null || languageButton == null ||
                languageLabel == null || resetButton == null || resetLabel == null ||
                menuButton == null || menuLabel == null)
            {
                Debug.LogWarning("The existing gameplay Settings wiring is incomplete. Run 'Create Or Update Gameplay Settings Panel' first; no scene was changed.", controller);
                return;
            }

            roundedSprite = EnsureSprite("SettingsLeatherRounded.png", false);
            circleSprite = EnsureSprite("SettingsBrassCircle.png", true);

            panelObject.SetActive(true);
            StylePanel(panel);
            StyleHeader(header, title, closeButton);
            StyleSliderRow(panel, "Master Label", master, masterValue, 190f);
            StyleSliderRow(panel, "Music Label", music, musicValue, 95f);
            StyleSliderRow(panel, "SFX Label", sfx, sfxValue, 0f);

            RectTransform languageBody = StyleActionButton(languageButton, languageLabel,
                new Vector2(175f, 66f), new Vector2(-212.5f, -112f),
                Hex("#386A45"), Hex("#132819"), "L", 16f);
            TextMeshProUGUI languageCaption = Text(languageBody, "Caption", "LANGUAGE", 13f,
                Hex("#FDE68A"), new Vector2(95f, 24f), new Vector2(12f, 14f),
                TextAlignmentOptions.Center);
            Place(languageLabel.rectTransform, new Vector2(95f, 27f), new Vector2(12f, -11f));
            languageLabel.fontSize = 20f;
            languageLabel.fontStyle = FontStyles.Bold;
            languageLabel.color = Color.white;
            languageLabel.alignment = TextAlignmentOptions.Center;

            JuicyLanguageToggle languagePresentation = languageButton.GetComponent<JuicyLanguageToggle>() ??
                                                         Undo.AddComponent<JuicyLanguageToggle>(languageButton.gameObject);
            SerializedObject languageFields = new(languagePresentation);
            Set(languageFields, "buttonBody", languageBody);
            Set(languageFields, "captionText", languageCaption);
            Set(languageFields, "languageCodeText", languageLabel);
            languageFields.ApplyModifiedProperties();

            StyleActionButton(resetButton, resetLabel, new Vector2(405f, 66f),
                new Vector2(97.5f, -112f), Hex("#C43A22"), Hex("#5B130B"), "!", 18f);
            resetLabel.fontSize = 19f;

            StyleActionButton(menuButton, menuLabel, new Vector2(600f, 62f),
                new Vector2(0f, -198f), Hex("#D98B16"), Hex("#6C3207"), "M", 19f);
            menuLabel.fontSize = 21f;

            UnityEngine.UI.Button bottomClose = EnsureButton(panel, "Close And Save");
            TextMeshProUGUI bottomCloseLabel = EnsureButtonLabel(bottomClose, "Label",
                "CLOSE & SAVE SETTINGS");
            StyleActionButton(bottomClose, bottomCloseLabel, new Vector2(600f, 62f),
                new Vector2(0f, -276f), Hex("#7B4A28"), Hex("#321708"), "X", 19f);
            bottomCloseLabel.fontSize = 20f;

            JuicySettingsPanel presentation = panel.GetComponent<JuicySettingsPanel>() ??
                                               Undo.AddComponent<JuicySettingsPanel>(panel.gameObject);
            SerializedObject presentationFields = new(presentation);
            Set(presentationFields, "panelBody", panel);
            Set(presentationFields, "existingCloseButton", closeButton);
            Set(presentationFields, "bottomCloseButton", bottomClose);
            Set(presentationFields, "bottomCloseLabel", bottomCloseLabel);
            presentationFields.ApplyModifiedProperties();

            bool mainMenuBuilt = BuildMainMenuSettings();
            header.SetAsLastSibling();
            closeButton.transform.SetAsLastSibling();
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(presentation);
            EditorSceneManager.MarkSceneDirty(panelObject.scene);
            Selection.activeGameObject = panelObject;
            EditorGUIUtility.PingObject(panelObject);
            Debug.Log(mainMenuBuilt
                ? "Juicy Settings built for both Main Menu and gameplay. Main Menu Back, gameplay Back To Main Menu, volume, localization and reset-data behavior remain wired. Edit the authored RectTransforms freely and save the scene."
                : "Juicy gameplay Settings built. Main Menu Settings was not found or its wiring is incomplete. Gameplay volume, localization, reset-data and Back To Main Menu behavior remain wired.",
                panelObject);
        }

        private static bool BuildMainMenuSettings()
        {
            MiningMainMenu mainMenu = Object.FindFirstObjectByType<MiningMainMenu>(
                FindObjectsInactive.Include);
            if (mainMenu == null)
            {
                return false;
            }

            SerializedObject fields = new(mainMenu);
            GameObject viewObject = Reference<GameObject>(fields, "settingsView");
            RectTransform view = viewObject != null ? viewObject.transform as RectTransform : null;
            TextMeshProUGUI title = Reference<TextMeshProUGUI>(fields, "settingsTitleLabel");
            TextMeshProUGUI masterLabel = Reference<TextMeshProUGUI>(fields, "masterLabel");
            TextMeshProUGUI musicLabel = Reference<TextMeshProUGUI>(fields, "musicLabel");
            TextMeshProUGUI sfxLabel = Reference<TextMeshProUGUI>(fields, "sfxLabel");
            TextMeshProUGUI masterValue = Reference<TextMeshProUGUI>(fields, "masterValueLabel");
            TextMeshProUGUI musicValue = Reference<TextMeshProUGUI>(fields, "musicValueLabel");
            TextMeshProUGUI sfxValue = Reference<TextMeshProUGUI>(fields, "sfxValueLabel");
            UnityEngine.UI.Slider master = Reference<UnityEngine.UI.Slider>(fields, "masterSlider");
            UnityEngine.UI.Slider music = Reference<UnityEngine.UI.Slider>(fields, "musicSlider");
            UnityEngine.UI.Slider sfx = Reference<UnityEngine.UI.Slider>(fields, "sfxSlider");
            UnityEngine.UI.Button languageButton = Reference<UnityEngine.UI.Button>(fields, "languageButton");
            TextMeshProUGUI languageLabel = Reference<TextMeshProUGUI>(fields, "languageLabel");
            UnityEngine.UI.Button backButton = Reference<UnityEngine.UI.Button>(fields, "backButton");
            TextMeshProUGUI backLabel = Reference<TextMeshProUGUI>(fields, "backLabel");

            if (view == null || title == null || masterLabel == null || musicLabel == null ||
                sfxLabel == null || masterValue == null || musicValue == null || sfxValue == null ||
                master == null || music == null || sfx == null || languageButton == null ||
                languageLabel == null || backButton == null || backLabel == null)
            {
                Debug.LogWarning("Main Menu Settings wiring is incomplete. Run 'Create Or Update Main Menu' first. Gameplay Settings was still built.", mainMenu);
                return false;
            }

            viewObject.SetActive(true);
            StylePanel(view);

            RectTransform header = Child(view, "Settings_Header", new Vector2(600f, 66f),
                new Vector2(0f, 276f));
            Style(header, Hex("#B96838"), Hex("#3D1808"), roundedSprite);
            Border(header.gameObject, Hex("#271004"), new Vector2(2f, -2f));
            RectTransform medal = Child(header, "Settings_Medal", new Vector2(50f, 50f),
                new Vector2(-266f, 0f));
            Style(medal, Hex("#FFE9A3"), Hex("#A45D08"), circleSprite);
            Border(medal.gameObject, Hex("#3A1A04"), new Vector2(1f, -1f));
            TextMeshProUGUI emblem = Text(medal, "Emblem", "S", 25f, Hex("#4A1D08"),
                new Vector2(44f, 42f), Vector2.zero, TextAlignmentOptions.Center);
            emblem.fontStyle = FontStyles.Bold;

            if (title.transform.parent != header)
            {
                Undo.SetTransformParent(title.transform, header, "Move Main Menu Settings title");
            }
            Place(title.rectTransform, new Vector2(500f, 50f), new Vector2(18f, 0f));
            title.fontSize = 28f;
            title.fontStyle = FontStyles.Bold;
            title.enableAutoSizing = true;
            title.fontSizeMin = 18f;
            title.fontSizeMax = 28f;
            title.color = Hex("#FFF1C7");
            title.alignment = TextAlignmentOptions.Center;

            StyleMainMenuSliderRow(masterLabel, master, masterValue, 166f);
            StyleMainMenuSliderRow(musicLabel, music, musicValue, 66f);
            StyleMainMenuSliderRow(sfxLabel, sfx, sfxValue, -34f);

            StyleActionButton(languageButton, languageLabel, new Vector2(290f, 68f),
                new Vector2(-155f, -170f), Hex("#386A45"), Hex("#132819"), "L", 19f);
            StyleActionButton(backButton, backLabel, new Vector2(290f, 68f),
                new Vector2(155f, -170f), Hex("#D98B16"), Hex("#6C3207"), "B", 19f);

            header.SetAsLastSibling();
            EditorUtility.SetDirty(mainMenu);
            EditorSceneManager.MarkSceneDirty(viewObject.scene);
            return true;
        }

        private static void StyleMainMenuSliderRow(TextMeshProUGUI label,
            UnityEngine.UI.Slider slider, TextMeshProUGUI value, float y)
        {
            Place(label.rectTransform, new Vector2(430f, 30f), new Vector2(-78f, y + 28f));
            label.fontSize = 18f;
            label.fontStyle = FontStyles.Bold;
            label.color = Hex("#FFF3D1");
            label.alignment = TextAlignmentOptions.Left;

            Place(value.rectTransform, new Vector2(95f, 30f), new Vector2(252f, y + 28f));
            value.fontSize = 18f;
            value.fontStyle = FontStyles.Bold;
            value.color = Hex("#FDE68A");
            value.alignment = TextAlignmentOptions.Right;

            StyleSlider(slider, y);
        }

        private static void StylePanel(RectTransform panel)
        {
            Place(panel, new Vector2(700f, 650f), Vector2.zero);
            panel.localScale = Vector3.one;
            Style(panel, Hex("#7B3D1D"), Hex("#160603"), roundedSprite);
            Border(panel.gameObject, Hex("#210A03"), new Vector2(3f, -3f));
            DropShadow(panel.gameObject, new Color(0f, 0f, 0f, 0.75f), new Vector2(0f, -12f));

            RectTransform seam = Child(panel, "Stitched_Seam", new Vector2(668f, 618f), Vector2.zero);
            UnityEngine.UI.Image seamImage = EnsureImage(seam);
            seamImage.sprite = roundedSprite;
            seamImage.type = UnityEngine.UI.Image.Type.Sliced;
            seamImage.color = new Color(1f, 1f, 1f, 0.001f);
            Border(seam.gameObject, Hex("#F1C894"), new Vector2(2f, -2f));
            seam.SetAsFirstSibling();

            AddRivet(panel, "Rivet_TL", new Vector2(-327f, 302f));
            AddRivet(panel, "Rivet_TR", new Vector2(327f, 302f));
            AddRivet(panel, "Rivet_BL", new Vector2(-327f, -302f));
            AddRivet(panel, "Rivet_BR", new Vector2(327f, -302f));
        }

        private static void StyleHeader(RectTransform header, TextMeshProUGUI title,
            UnityEngine.UI.Button closeButton)
        {
            Place(header, new Vector2(600f, 66f), new Vector2(-25f, 276f));
            Style(header, Hex("#B96838"), Hex("#3D1808"), roundedSprite);
            Border(header.gameObject, Hex("#271004"), new Vector2(2f, -2f));

            RectTransform medal = Child(header, "Settings_Medal", new Vector2(50f, 50f),
                new Vector2(-266f, 0f));
            Style(medal, Hex("#FFE9A3"), Hex("#A45D08"), circleSprite);
            Border(medal.gameObject, Hex("#3A1A04"), new Vector2(1f, -1f));
            TextMeshProUGUI emblem = Text(medal, "Emblem", "S", 25f, Hex("#4A1D08"),
                new Vector2(44f, 42f), Vector2.zero, TextAlignmentOptions.Center);
            emblem.fontStyle = FontStyles.Bold;

            if (title.transform.parent != header)
            {
                Undo.SetTransformParent(title.transform, header, "Move Settings title into header");
            }
            Place(title.rectTransform, new Vector2(470f, 50f), new Vector2(15f, 0f));
            title.fontSize = 28f;
            title.fontStyle = FontStyles.Bold;
            title.enableAutoSizing = true;
            title.fontSizeMin = 18f;
            title.fontSizeMax = 28f;
            title.color = Hex("#FFF1C7");
            title.alignment = TextAlignmentOptions.Center;

            RectTransform closeRect = closeButton.transform as RectTransform;
            Place(closeRect, new Vector2(54f, 54f), new Vector2(315f, 276f));
            UnityEngine.UI.Image image = closeButton.GetComponent<UnityEngine.UI.Image>() ??
                                         Undo.AddComponent<UnityEngine.UI.Image>(closeButton.gameObject);
            image.sprite = roundedSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = true;
            closeButton.targetGraphic = image;
            closeButton.transition = UnityEngine.UI.Selectable.Transition.None;
            (closeButton.GetComponent<MiningUiGradient>() ??
             Undo.AddComponent<MiningUiGradient>(closeButton.gameObject)).SetColors(
                Hex("#D83B27"), Hex("#6B120B"));
            Border(closeButton.gameObject, Hex("#FFDF78"), new Vector2(2f, -2f));
            TextMeshProUGUI closeText = closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (closeText != null)
            {
                closeText.text = "X";
                closeText.fontSize = 22f;
                closeText.fontStyle = FontStyles.Bold;
                closeText.color = Color.white;
                closeText.alignment = TextAlignmentOptions.Center;
            }
        }

        private static void StyleSliderRow(RectTransform panel, string labelName,
            UnityEngine.UI.Slider slider, TextMeshProUGUI value, float y)
        {
            TextMeshProUGUI label = Find(panel, labelName)?.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                Place(label.rectTransform, new Vector2(430f, 30f), new Vector2(-78f, y + 28f));
                label.fontSize = 18f;
                label.fontStyle = FontStyles.Bold;
                label.color = Hex("#FFF3D1");
                label.alignment = TextAlignmentOptions.Left;
            }
            Place(value.rectTransform, new Vector2(95f, 30f), new Vector2(252f, y + 28f));
            value.fontSize = 18f;
            value.fontStyle = FontStyles.Bold;
            value.color = Hex("#FDE68A");
            value.alignment = TextAlignmentOptions.Right;

            StyleSlider(slider, y);
        }

        private static void StyleSlider(UnityEngine.UI.Slider slider, float y)
        {
            RectTransform sliderRect = slider.transform as RectTransform;
            Place(sliderRect, new Vector2(600f, 38f), new Vector2(0f, y));
            UnityEngine.UI.Image trench = slider.GetComponent<UnityEngine.UI.Image>() ??
                                           Undo.AddComponent<UnityEngine.UI.Image>(slider.gameObject);
            trench.sprite = roundedSprite;
            trench.type = UnityEngine.UI.Image.Type.Sliced;
            trench.color = Hex("#210B04");
            trench.raycastTarget = true;
            Border(slider.gameObject, Hex("#B86A22"), new Vector2(2f, -2f));

            if (slider.fillRect != null)
            {
                UnityEngine.UI.Image fill = slider.fillRect.GetComponent<UnityEngine.UI.Image>() ??
                                            Undo.AddComponent<UnityEngine.UI.Image>(slider.fillRect.gameObject);
                fill.sprite = roundedSprite;
                fill.type = UnityEngine.UI.Image.Type.Sliced;
                fill.color = Color.white;
                (fill.GetComponent<MiningUiGradient>() ??
                 Undo.AddComponent<MiningUiGradient>(fill.gameObject)).SetColors(
                    Hex("#FFE26A"), Hex("#D77909"));
            }
            if (slider.handleRect != null)
            {
                Place(slider.handleRect, new Vector2(34f, 48f), slider.handleRect.anchoredPosition);
                UnityEngine.UI.Image handle = slider.handleRect.GetComponent<UnityEngine.UI.Image>() ??
                                              Undo.AddComponent<UnityEngine.UI.Image>(slider.handleRect.gameObject);
                handle.sprite = roundedSprite;
                handle.type = UnityEngine.UI.Image.Type.Sliced;
                handle.color = Hex("#F8C94E");
                Border(handle.gameObject, Hex("#4B2205"), new Vector2(2f, -2f));
                slider.targetGraphic = handle;
            }
        }

        private static RectTransform StyleActionButton(UnityEngine.UI.Button button,
            TextMeshProUGUI label, Vector2 size, Vector2 position, Color top, Color bottom,
            string emblem, float labelSize)
        {
            RectTransform root = button.transform as RectTransform;
            Place(root, size, position);
            UnityEngine.UI.Image rootImage = button.GetComponent<UnityEngine.UI.Image>() ??
                                             Undo.AddComponent<UnityEngine.UI.Image>(button.gameObject);
            rootImage.sprite = null;
            rootImage.color = new Color(1f, 1f, 1f, 0.001f);
            rootImage.raycastTarget = true;

            RectTransform shadow = Child(root, "Base_Shadow", size, new Vector2(0f, -7f));
            UnityEngine.UI.Image shadowImage = EnsureImage(shadow);
            shadowImage.sprite = roundedSprite;
            shadowImage.type = UnityEngine.UI.Image.Type.Sliced;
            shadowImage.color = Hex("#1B0A03");
            shadow.SetAsFirstSibling();

            RectTransform body = Child(root, "Button_Body", size, Vector2.zero);
            UnityEngine.UI.Image bodyImage = Style(body, top, bottom, roundedSprite);
            bodyImage.raycastTarget = false;
            Border(body.gameObject, Hex("#F5B942"), new Vector2(2f, -2f));
            button.targetGraphic = bodyImage;
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;

            RectTransform medal = Child(body, "Medal", new Vector2(46f, 46f),
                new Vector2(-size.x * 0.5f + 34f, 0f));
            Style(medal, Hex("#FFE9A3"), Hex("#9A5607"), circleSprite);
            Border(medal.gameObject, Hex("#3B1A04"), new Vector2(1f, -1f));
            TextMeshProUGUI emblemText = Text(medal, "Emblem", emblem, 22f, Hex("#4A1D08"),
                new Vector2(40f, 38f), Vector2.zero, TextAlignmentOptions.Center);
            emblemText.fontStyle = FontStyles.Bold;

            if (label.transform.parent != body)
            {
                Undo.SetTransformParent(label.transform, body, "Move Settings button label");
            }
            Place(label.rectTransform, new Vector2(size.x - 76f, size.y - 10f),
                new Vector2(24f, 0f));
            label.fontSize = labelSize;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            body.SetAsLastSibling();
            return body;
        }

        private static UnityEngine.UI.Button EnsureButton(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing == null)
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                Undo.RegisterCreatedObjectUndo(obj, "Build Settings " + name);
                obj.transform.SetParent(parent, false);
            }
            else
            {
                obj = existing.gameObject;
            }
            return obj.GetComponent<UnityEngine.UI.Button>() ??
                   Undo.AddComponent<UnityEngine.UI.Button>(obj);
        }

        private static TextMeshProUGUI EnsureButtonLabel(UnityEngine.UI.Button button,
            string name, string value)
        {
            Transform existing = Find(button.transform, name);
            RectTransform rect;
            if (existing == null)
            {
                rect = Child(button.transform, name, new Vector2(100f, 30f), Vector2.zero);
            }
            else
            {
                rect = existing as RectTransform;
            }
            TextMeshProUGUI label = rect.GetComponent<TextMeshProUGUI>() ??
                                    Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
            label.text = value;
            return label;
        }

        private static T Reference<T>(SerializedObject fields, string name) where T : Object
        {
            return fields.FindProperty(name)?.objectReferenceValue as T;
        }

        private static void Set(SerializedObject fields, string name, Object value)
        {
            SerializedProperty property = fields.FindProperty(name);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static Transform Find(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }
            return null;
        }

        private static RectTransform Child(Transform parent, string name, Vector2 size,
            Vector2 position)
        {
            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing == null)
            {
                obj = new GameObject(name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(obj, "Build Settings " + name);
                obj.transform.SetParent(parent, false);
            }
            else
            {
                obj = existing.gameObject;
                obj.SetActive(true);
            }
            RectTransform rect = obj.GetComponent<RectTransform>();
            Place(rect, size, position);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void Place(RectTransform rect, Vector2 size, Vector2 position)
        {
            if (rect == null)
            {
                return;
            }
            Undo.RecordObject(rect, "Layout Settings " + rect.name);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static UnityEngine.UI.Image EnsureImage(RectTransform rect)
        {
            UnityEngine.UI.Image image = rect.GetComponent<UnityEngine.UI.Image>() ??
                                         Undo.AddComponent<UnityEngine.UI.Image>(rect.gameObject);
            image.raycastTarget = false;
            return image;
        }

        private static UnityEngine.UI.Image Style(RectTransform rect, Color top, Color bottom,
            Sprite sprite)
        {
            UnityEngine.UI.Image image = EnsureImage(rect);
            image.sprite = sprite;
            image.type = sprite == circleSprite
                ? UnityEngine.UI.Image.Type.Simple
                : UnityEngine.UI.Image.Type.Sliced;
            image.color = Color.white;
            (rect.GetComponent<MiningUiGradient>() ??
             Undo.AddComponent<MiningUiGradient>(rect.gameObject)).SetColors(top, bottom);
            return image;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string value,
            float fontSize, Color color, Vector2 size, Vector2 position,
            TextAlignmentOptions alignment)
        {
            RectTransform rect = Child(parent, name, size, position);
            TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>() ??
                                   Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static void AddRivet(Transform parent, string name, Vector2 position)
        {
            RectTransform rivet = Child(parent, name, new Vector2(12f, 12f), position);
            Style(rivet, Hex("#D6D3D1"), Hex("#44403C"), circleSprite);
        }

        private static void Border(GameObject obj, Color color, Vector2 distance)
        {
            UnityEngine.UI.Outline outline = obj.GetComponent<UnityEngine.UI.Outline>() ??
                                              Undo.AddComponent<UnityEngine.UI.Outline>(obj);
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void DropShadow(GameObject obj, Color color, Vector2 distance)
        {
            UnityEngine.UI.Shadow shadow = null;
            foreach (UnityEngine.UI.Shadow candidate in obj.GetComponents<UnityEngine.UI.Shadow>())
            {
                if (candidate.GetType() == typeof(UnityEngine.UI.Shadow))
                {
                    shadow = candidate;
                    break;
                }
            }
            shadow ??= Undo.AddComponent<UnityEngine.UI.Shadow>(obj);
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        private static Sprite EnsureSprite(string fileName, bool circle)
        {
            string path = SpriteDirectory + "/" + fileName;
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }
            Directory.CreateDirectory(SpriteDirectory);
            Texture2D texture = new(64, 64, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[64 * 64];
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float u = x - 31.5f;
                    float v = y - 31.5f;
                    float distance = circle
                        ? Mathf.Sqrt(u * u + v * v)
                        : Mathf.Sqrt(Mathf.Pow(Mathf.Max(0f, Mathf.Abs(u) - 9.5f), 2f) +
                                     Mathf.Pow(Mathf.Max(0f, Mathf.Abs(v) - 9.5f), 2f));
                    byte alpha = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(
                        (circle ? 31.5f : 22.5f) - distance));
                    pixels[y * 64 + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = circle ? Vector4.zero : new Vector4(22f, 22f, 22f, 22f);
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Color Hex(string hex) =>
            ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
    }
}
#endif
