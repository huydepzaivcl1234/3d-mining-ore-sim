#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Authors the editable modern menu and assigns the generated Gem sprite.</summary>
    public static class MiningMainMenuSetupMenu
    {
        private const string CanvasName = "Mining HUD Canvas";
        private const string MenuName = "Main Menu";
        private const string DataFolder = "Assets/GameData/UI";
        private const string DataPath = DataFolder + "/MiningMainMenuData.asset";
        // Keep the existing imported asset path so its GUID and user assignments remain intact.
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
            UpgradeLegacyLayoutIfUnchanged(data);
            Sprite gemSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GemIconPath);
            AssignGemIcon(gemSprite);

            Transform existing = canvas.transform.Find(MenuName);
            bool created = existing == null;
            GameObject root = created ? CreateRoot(canvas, data) : existing.gameObject;
            EnsureModernStructure(root, data, gemSprite);
            EnsureMainMenuGemAmount(root, data);
            EnsureMainMenuShopButton(root, data);
            EnsureExitConfirmation(root, data);
            EnsurePlayTransition(root, data);

            MiningMainMenu mainMenu = root.GetComponent<MiningMainMenu>() ??
                                      Undo.AddComponent<MiningMainMenu>(root);
            CanvasGroup rootGroup = root.GetComponent<CanvasGroup>() ??
                                    Undo.AddComponent<CanvasGroup>(root);
            EnsureGameplayReturnButton(data, mainMenu);
            Transform card = FindDescendant(root.transform, "Main Menu Card");
            Transform mainView = FindDescendant(root.transform, "Main View");
            Transform settingsView = FindDescendant(root.transform, "Settings View");

            SerializedObject serialized = new(mainMenu);
            SetReference(serialized, "data", data);
            SetReference(serialized, "gameData", FindFirstAsset<MiningGameData>());
            SetReference(serialized, "audioManager", Object.FindFirstObjectByType<MiningAudioManager>(
                FindObjectsInactive.Include));
            SetReference(serialized, "wallet", Object.FindFirstObjectByType<PlayerWallet>(
                FindObjectsInactive.Include));
            SetReference(serialized, "canvasGroup", rootGroup);
            SetReference(serialized, "card", card?.GetComponent<RectTransform>());
            SetReference(serialized, "mainView", mainView?.gameObject);
            SetReference(serialized, "settingsView", settingsView?.gameObject);
            SetReference(serialized, "mainViewGroup", mainView?.GetComponent<CanvasGroup>());
            SetReference(serialized, "settingsViewGroup", settingsView?.GetComponent<CanvasGroup>());
            SetReference(serialized, "transitionBar",
                FindComponent<Image>(root.transform, "Aqua Transition Bar"));
            SetReference(serialized, "transitionFlash",
                FindComponent<Image>(root.transform, "Transition Flash"));
            Transform exitConfirmation = FindDescendant(root.transform, "Exit Confirmation");
            SetReference(serialized, "exitConfirmation", exitConfirmation?.gameObject);
            SetReference(serialized, "exitConfirmationGroup",
                exitConfirmation?.GetComponent<CanvasGroup>());
            SetReference(serialized, "exitConfirmationDialog",
                FindComponent<RectTransform>(root.transform, "Exit Confirmation Dialog"));
            SetReference(serialized, "confirmExitButton",
                FindComponent<Button>(root.transform, "Confirm Exit Button"));
            SetReference(serialized, "cancelExitButton",
                FindComponent<Button>(root.transform, "Cancel Exit Button"));
            SetReference(serialized, "playButton", FindComponent<Button>(root.transform, "Play Button"));
            SetReference(serialized, "shopButton", FindComponent<Button>(root.transform, "Shop Button"));
            SetReference(serialized, "shopPanel", Object.FindFirstObjectByType<MiningShopPanel>(
                FindObjectsInactive.Include));
            SetReference(serialized, "settingsButton", FindComponent<Button>(root.transform, "Settings Button"));
            SetReference(serialized, "exitButton", FindComponent<Button>(root.transform, "Exit Button"));
            SetReference(serialized, "backButton", FindComponent<Button>(root.transform, "Back Button"));
            SetReference(serialized, "languageButton", FindComponent<Button>(root.transform, "Language Button"));
            SetReference(serialized, "masterSlider", FindComponent<Slider>(root.transform, "Master Slider"));
            SetReference(serialized, "musicSlider", FindComponent<Slider>(root.transform, "Music Slider"));
            SetReference(serialized, "sfxSlider", FindComponent<Slider>(root.transform, "SFX Slider"));
            SetLabels(serialized, root.transform);
            serialized.ApplyModifiedProperties();

            root.SetActive(true);
            mainView?.gameObject.SetActive(true);
            settingsView?.gameObject.SetActive(false);
            root.transform.SetAsLastSibling();
            MiningButtonSfxSetupMenu.AssignAllButtonSfx(false);
            EditorUtility.SetDirty(mainMenu);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            EditorUtility.DisplayDialog("Main Menu Setup",
                created
                    ? "Modern Main Menu and Gem icon are ready. Save the scene after editing."
                    : "Main Menu was upgraded with Settings and Exit. Existing HUD/Scene data was preserved.",
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
            return root;
        }

        private static void EnsureModernStructure(GameObject root, MiningMainMenuData data,
            Sprite gemSprite)
        {
            Image backdrop = root.GetComponent<Image>() ?? Undo.AddComponent<Image>(root);
            Color opaqueBackdrop = data.BackdropColor;
            opaqueBackdrop.a = 1f;
            backdrop.color = opaqueBackdrop;
            backdrop.raycastTarget = true;

            Transform card = root.transform.Find("Main Menu Card");
            bool needsUpgrade = card == null || card.Find("Main View") == null ||
                                card.Find("Settings View") == null;
            if (!needsUpgrade)
            {
                return;
            }

            if (card != null)
            {
                Undo.DestroyObjectImmediate(card.gameObject);
            }
            CreateModernCard(root.transform, data, gemSprite);
        }

        private static void EnsurePlayTransition(GameObject root, MiningMainMenuData data)
        {
            Transform barTransform = root.transform.Find("Aqua Transition Bar");
            if (barTransform == null)
            {
                GameObject barObject = CreateImage(root.transform, "Aqua Transition Bar",
                    data.TransitionBarColor);
                barTransform = barObject.transform;
                RectTransform barRect = barObject.GetComponent<RectTransform>();
                barRect.anchorMin = new Vector2(0f, 0f);
                barRect.anchorMax = new Vector2(0f, 1f);
                barRect.pivot = new Vector2(0f, 0.5f);
                barRect.anchoredPosition = Vector2.zero;
                barRect.sizeDelta = new Vector2(data.TransitionBarWidth, 0f);
            }

            Transform flashTransform = root.transform.Find("Transition Flash");
            if (flashTransform == null)
            {
                GameObject flashObject = CreateImage(root.transform, "Transition Flash",
                    data.TransitionFlashColor);
                flashTransform = flashObject.transform;
                Stretch(flashObject.GetComponent<RectTransform>());
            }

            barTransform.SetAsLastSibling();
            flashTransform.SetAsLastSibling();
            barTransform.gameObject.SetActive(false);
            flashTransform.gameObject.SetActive(false);
        }

        private static void EnsureExitConfirmation(GameObject root, MiningMainMenuData data)
        {
            if (root.transform.Find("Exit Confirmation") != null)
            {
                return;
            }

            GameObject overlay = CreateImage(root.transform, "Exit Confirmation",
                new Color(0f, 0f, 0f, 0.72f));
            Stretch(overlay.GetComponent<RectTransform>());
            overlay.AddComponent<CanvasGroup>();

            GameObject dialog = CreateImage(overlay.transform, "Exit Confirmation Dialog",
                data.CardColor);
            Center(dialog.GetComponent<RectTransform>(), Vector2.zero,
                data.ExitConfirmationSize);
            CreateLabel(dialog.transform, "Exit Confirmation Title",
                data.EnglishExitConfirmationTitle, new Vector2(0f, 80f),
                new Vector2(data.ExitConfirmationSize.x - 60f, 70f), data.TitleFontSize,
                data.TitleColor, FontStyles.Bold);
            CreateLabel(dialog.transform, "Exit Confirmation Message",
                data.EnglishExitConfirmationMessage, new Vector2(0f, 15f),
                new Vector2(data.ExitConfirmationSize.x - 80f, 54f), data.SettingsFontSize,
                data.SubtitleColor, FontStyles.Normal);
            CreateButton(dialog.transform, "Confirm Exit Button", "Confirm Exit Label",
                data.EnglishConfirmLabel, new Vector2(-130f, -95f),
                data.ExitConfirmationButtonSize, data.ExitButtonColor, data.PlayTextColor,
                data.SettingsFontSize);
            CreateButton(dialog.transform, "Cancel Exit Button", "Cancel Exit Label",
                data.EnglishCancelLabel, new Vector2(130f, -95f),
                data.ExitConfirmationButtonSize, data.SettingsButtonColor, data.PlayTextColor,
                data.SettingsFontSize);
            overlay.SetActive(false);
        }

        private static void EnsureGameplayReturnButton(MiningMainMenuData data,
            MiningMainMenu mainMenu)
        {
            MiningAudioSettingsPanel audioPanel = Object.FindFirstObjectByType<
                MiningAudioSettingsPanel>(FindObjectsInactive.Include);
            if (audioPanel == null)
            {
                Debug.LogWarning("Main Menu setup could not find the gameplay Audio Settings Panel.");
                return;
            }

            SerializedObject audioSerialized = new(audioPanel);
            GameObject settingsPanelObject = audioSerialized.FindProperty("settingsPanel")
                ?.objectReferenceValue as GameObject;
            Transform panel = settingsPanelObject != null
                ? settingsPanelObject.transform
                : audioPanel.transform;
            Transform existing = panel.Find("Return To Main Menu");
            GameObject buttonObject;
            if (existing == null)
            {
                buttonObject = CreateImage(panel, "Return To Main Menu",
                    data.SettingsButtonColor);
                RectTransform rect = buttonObject.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = data.GameplayReturnButtonPosition;
                rect.sizeDelta = data.GameplayReturnButtonSize;
                Button button = buttonObject.AddComponent<Button>();
                button.targetGraphic = buttonObject.GetComponent<Image>();
                SmoothButtonPunch punch = buttonObject.AddComponent<SmoothButtonPunch>();
                punch.SetTarget(rect);
                CreateLabel(rect, "Return To Menu Label", data.EnglishReturnToMenuLabel,
                    Vector2.zero, data.GameplayReturnButtonSize, data.SettingsFontSize * 0.72f,
                    data.PlayTextColor, FontStyles.Bold);
            }
            else
            {
                buttonObject = existing.gameObject;
            }

            SetReference(audioSerialized, "mainMenu", mainMenu);
            SetReference(audioSerialized, "returnToMenuButton", buttonObject.GetComponent<Button>());
            SetReference(audioSerialized, "returnToMenuLabel",
                FindComponent<TextMeshProUGUI>(buttonObject.transform, "Return To Menu Label"));
            audioSerialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(audioPanel);
        }

        private static void CreateModernCard(Transform parent, MiningMainMenuData data,
            Sprite gemSprite)
        {
            GameObject cardObject = CreateImage(parent, "Main Menu Card", data.CardColor);
            RectTransform card = cardObject.GetComponent<RectTransform>();
            Center(card, Vector2.zero, data.CardSize);

            GameObject accent = CreateImage(card, "Accent", data.SliderFillColor);
            RectTransform accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(8f, 0f);

            GameObject mainView = CreateView(card, "Main View");
            CreateMainView(mainView.transform, data, gemSprite);

            GameObject settingsView = CreateView(card, "Settings View");
            CreateSettingsView(settingsView.transform, data);
            settingsView.SetActive(false);
        }

        private static void CreateMainView(Transform parent, MiningMainMenuData data,
            Sprite gemSprite)
        {
            GameObject iconObject = CreateImage(parent, "Gem Icon", Color.white);
            Center(iconObject.GetComponent<RectTransform>(), data.GemIconPosition,
                data.GemIconSize);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = gemSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            CreateLabel(parent, "Gem Amount", string.Format(data.EnglishGemAmountFormat, "0"),
                data.GemAmountPosition, data.GemAmountSize, data.GemAmountFontSize,
                data.GemAmountColor, FontStyles.Bold);

            CreateLabel(parent, "Title", data.EnglishTitle, data.TitlePosition,
                data.TitleSize, data.TitleFontSize, data.TitleColor, FontStyles.Bold);
            CreateLabel(parent, "Subtitle", data.EnglishSubtitle, data.SubtitlePosition,
                data.SubtitleSize, data.SubtitleFontSize, data.SubtitleColor, FontStyles.Normal);

            CreateButton(parent, "Play Button", "Play Label", data.EnglishPlayLabel,
                data.PlayButtonPosition, data.PlayButtonSize, data.PlayButtonColor,
                data.PlayTextColor, data.PlayFontSize);
            CreateButton(parent, "Shop Button", "Shop Label", data.EnglishShopLabel,
                data.ShopButtonPosition, data.PlayButtonSize, data.ShopButtonColor,
                data.PlayTextColor, data.PlayFontSize);
            CreateButton(parent, "Settings Button", "Settings Label", data.EnglishSettingsLabel,
                data.SettingsButtonPosition, data.PlayButtonSize, data.SettingsButtonColor,
                data.PlayTextColor, data.PlayFontSize);
            CreateButton(parent, "Exit Button", "Exit Label", data.EnglishExitLabel,
                data.ExitButtonPosition, data.PlayButtonSize, data.ExitButtonColor,
                data.PlayTextColor, data.PlayFontSize);
        }

        private static void CreateSettingsView(Transform parent, MiningMainMenuData data)
        {
            CreateLabel(parent, "Settings Title", data.EnglishSettingsLabel,
                data.SettingsTitlePosition, data.SettingsTitleSize, data.TitleFontSize,
                data.TitleColor, FontStyles.Bold);

            CreateAudioRow(parent, "Master", "MASTER VOLUME", "Master Slider", 0,
                data);
            CreateAudioRow(parent, "Music", "MUSIC", "Music Slider", 1, data);
            CreateAudioRow(parent, "SFX", "SOUND EFFECTS", "SFX Slider", 2, data);

            CreateButton(parent, "Language Button", "Language Label", data.EnglishLanguageLabel,
                data.SettingsLanguagePosition, data.SettingsSmallButtonSize,
                data.SettingsButtonColor, data.PlayTextColor, data.SettingsFontSize);
            CreateButton(parent, "Back Button", "Back Label", data.EnglishBackLabel,
                data.SettingsBackPosition, data.SettingsSmallButtonSize,
                data.PlayButtonColor, data.PlayTextColor, data.SettingsFontSize);
        }

        private static void CreateAudioRow(Transform parent, string prefix, string labelText,
            string sliderName, int rowIndex, MiningMainMenuData data)
        {
            float y = data.SettingsFirstSliderPosition.y - rowIndex * data.SettingsRowSpacing;
            Vector2 labelPosition = new(-310f, y);
            Vector2 sliderPosition = new(data.SettingsFirstSliderPosition.x, y);
            Vector2 valuePosition = new(365f, y);
            CreateLabel(parent, prefix + " Label", labelText, labelPosition,
                data.SettingsLabelSize, data.SettingsFontSize, data.TitleColor, FontStyles.Bold);
            CreateSlider(parent, sliderName, sliderPosition, data.SettingsSliderSize,
                data.SliderBackgroundColor, data.SliderFillColor);
            CreateLabel(parent, prefix + " Value", "100%", valuePosition,
                new Vector2(100f, 44f), data.SettingsFontSize, data.SubtitleColor,
                FontStyles.Bold);
        }

        private static GameObject CreateView(Transform parent, string name)
        {
            GameObject view = new(name, typeof(RectTransform), typeof(CanvasGroup));
            view.transform.SetParent(parent, false);
            Stretch(view.GetComponent<RectTransform>());
            return view;
        }

        private static Button CreateButton(Transform parent, string name, string labelName,
            string text, Vector2 position, Vector2 size, Color color, Color textColor,
            float fontSize)
        {
            GameObject buttonObject = CreateImage(parent, name, color);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            Center(rect, position, size);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.86f, 0.94f, 1f, 1f);
            colors.pressedColor = new Color(0.72f, 0.82f, 0.9f, 1f);
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

            CreateLabel(rect, labelName, text, Vector2.zero, size, fontSize, textColor,
                FontStyles.Bold);
            return button;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 position,
            Vector2 size, Color backgroundColor, Color fillColor)
        {
            GameObject sliderObject = CreateImage(parent, name, backgroundColor);
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            Center(sliderRect, position, size);
            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            GameObject fillObject = CreateImage(sliderRect, "Fill", fillColor);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            StretchWithPadding(fillRect, 5f);
            fillObject.GetComponent<Image>().raycastTarget = false;

            GameObject handleObject = CreateImage(sliderRect, "Handle", Color.white);
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.anchorMin = handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(size.y + 10f, size.y + 10f);
            handleRect.anchoredPosition = Vector2.zero;

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleObject.GetComponent<Image>();
            return slider;
        }

        private static void SetLabels(SerializedObject serialized, Transform root)
        {
            SetReference(serialized, "titleLabel", FindComponent<TextMeshProUGUI>(root, "Title"));
            SetReference(serialized, "subtitleLabel", FindComponent<TextMeshProUGUI>(root, "Subtitle"));
            SetReference(serialized, "gemAmountLabel",
                FindComponent<TextMeshProUGUI>(root, "Gem Amount"));
            SetReference(serialized, "playLabel", FindComponent<TextMeshProUGUI>(root, "Play Label"));
            SetReference(serialized, "shopLabel", FindComponent<TextMeshProUGUI>(root, "Shop Label"));
            SetReference(serialized, "settingsLabel", FindComponent<TextMeshProUGUI>(root, "Settings Label"));
            SetReference(serialized, "exitLabel", FindComponent<TextMeshProUGUI>(root, "Exit Label"));
            SetReference(serialized, "settingsTitleLabel", FindComponent<TextMeshProUGUI>(root, "Settings Title"));
            SetReference(serialized, "masterLabel", FindComponent<TextMeshProUGUI>(root, "Master Label"));
            SetReference(serialized, "musicLabel", FindComponent<TextMeshProUGUI>(root, "Music Label"));
            SetReference(serialized, "sfxLabel", FindComponent<TextMeshProUGUI>(root, "SFX Label"));
            SetReference(serialized, "masterValueLabel", FindComponent<TextMeshProUGUI>(root, "Master Value"));
            SetReference(serialized, "musicValueLabel", FindComponent<TextMeshProUGUI>(root, "Music Value"));
            SetReference(serialized, "sfxValueLabel", FindComponent<TextMeshProUGUI>(root, "SFX Value"));
            SetReference(serialized, "languageLabel", FindComponent<TextMeshProUGUI>(root, "Language Label"));
            SetReference(serialized, "backLabel", FindComponent<TextMeshProUGUI>(root, "Back Label"));
            SetReference(serialized, "exitConfirmationTitleLabel",
                FindComponent<TextMeshProUGUI>(root, "Exit Confirmation Title"));
            SetReference(serialized, "exitConfirmationMessageLabel",
                FindComponent<TextMeshProUGUI>(root, "Exit Confirmation Message"));
            SetReference(serialized, "confirmExitLabel",
                FindComponent<TextMeshProUGUI>(root, "Confirm Exit Label"));
            SetReference(serialized, "cancelExitLabel",
                FindComponent<TextMeshProUGUI>(root, "Cancel Exit Label"));
        }

        private static void EnsureMainMenuGemAmount(GameObject root, MiningMainMenuData data)
        {
            Transform mainView = FindDescendant(root.transform, "Main View");
            if (mainView == null || FindDescendant(mainView, "Gem Amount") != null)
            {
                return;
            }

            Undo.RecordObject(mainView, "Add Main Menu Gem Amount");
            CreateLabel(mainView, "Gem Amount", string.Format(data.EnglishGemAmountFormat, "0"),
                data.GemAmountPosition, data.GemAmountSize, data.GemAmountFontSize,
                data.GemAmountColor, FontStyles.Bold);
        }

        internal static void EnsureShopButtonForCurrentMenu()
        {
            Canvas canvas = FindHudCanvas();
            GameObject root = canvas != null ? canvas.transform.Find(MenuName)?.gameObject : null;
            if (root == null)
            {
                return;
            }

            MiningMainMenuData data = LoadOrCreateData();
            EnsureMainMenuShopButton(root, data);
            MiningMainMenu mainMenu = root.GetComponent<MiningMainMenu>();
            if (mainMenu == null)
            {
                return;
            }

            SerializedObject serialized = new(mainMenu);
            SetReference(serialized, "shopButton",
                FindComponent<Button>(root.transform, "Shop Button"));
            SetReference(serialized, "shopLabel",
                FindComponent<TextMeshProUGUI>(root.transform, "Shop Label"));
            SetReference(serialized, "shopPanel", Object.FindFirstObjectByType<MiningShopPanel>(
                FindObjectsInactive.Include));
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(mainMenu);
        }

        private static void EnsureMainMenuShopButton(GameObject root, MiningMainMenuData data)
        {
            Transform mainView = FindDescendant(root.transform, "Main View");
            if (mainView == null || FindDescendant(mainView, "Shop Button") != null)
            {
                return;
            }

            Undo.RecordObject(mainView, "Add Main Menu Shop Button");
            CreateButton(mainView, "Shop Button", "Shop Label", data.EnglishShopLabel,
                data.ShopButtonPosition, data.PlayButtonSize, data.ShopButtonColor,
                data.PlayTextColor, data.PlayFontSize);
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
            Image image = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            if (image != null && image.sprite == null)
            {
                Undo.RecordObject(image, "Assign Gem Icon");
                image.sprite = gemSprite;
                image.color = Color.white;
                image.preserveAspect = true;
                EditorUtility.SetDirty(image);
            }

            Transform fallback = iconTransform?.Find("Fallback");
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

        private static void UpgradeLegacyLayoutIfUnchanged(MiningMainMenuData data)
        {
            SerializedObject serialized = new(data);
            SerializedProperty cardSize = serialized.FindProperty("cardSize");
            SerializedProperty gemPosition = serialized.FindProperty("gemIconPosition");
            SerializedProperty playPosition = serialized.FindProperty("playButtonPosition");
            bool untouchedLegacyLayout = cardSize != null && gemPosition != null &&
                                         playPosition != null &&
                                         cardSize.vector2Value == new Vector2(720f, 520f) &&
                                         gemPosition.vector2Value == new Vector2(0f, 140f) &&
                                         playPosition.vector2Value == new Vector2(0f, -145f);
            if (!untouchedLegacyLayout)
            {
                return;
            }

            cardSize.vector2Value = new Vector2(1040f, 620f);
            serialized.FindProperty("gemIconSize").vector2Value = new Vector2(220f, 220f);
            gemPosition.vector2Value = new Vector2(-250f, 90f);
            serialized.FindProperty("titleSize").vector2Value = new Vector2(470f, 80f);
            serialized.FindProperty("titlePosition").vector2Value = new Vector2(-250f, -65f);
            serialized.FindProperty("subtitleSize").vector2Value = new Vector2(460f, 64f);
            serialized.FindProperty("subtitlePosition").vector2Value = new Vector2(-250f, -132f);
            playPosition.vector2Value = new Vector2(250f, 100f);
            SerializedProperty backdrop = serialized.FindProperty("backdropColor");
            Color backdropColor = backdrop.colorValue;
            backdropColor.a = 1f;
            backdrop.colorValue = backdropColor;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(data);
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
            GameObject value = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            value.transform.SetParent(parent, false);
            value.GetComponent<Image>().color = color;
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

        private static void StretchWithPadding(RectTransform rect, float padding)
        {
            Stretch(rect);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static void Center(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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

        private static void SetReference(SerializedObject serialized, string name, Object value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null)
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
