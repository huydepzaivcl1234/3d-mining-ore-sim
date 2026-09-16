#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Restyles the existing wired Main Menu without replacing its gameplay controller.</summary>
    public static class MiningMainMenuShowcaseSetupMenu
    {
        private const string BackgroundPath = "Assets/Ores/Icons/background.png";

        [MenuItem("Mining Simulator/UI/Build Showcase Main Menu")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Exit Play Mode",
                    "Stop Play Mode before rebuilding the Main Menu.", "OK");
                return;
            }

            MiningMainMenu menu = Object.FindFirstObjectByType<MiningMainMenu>(
                FindObjectsInactive.Include);
            if (menu == null)
            {
                EditorUtility.DisplayDialog("Main Menu Not Found",
                    "Open the gameplay scene and run Mining Simulator > Setup > " +
                    "Create Or Update Main Menu first.", "OK");
                return;
            }

            SerializedObject fields = new(menu);
            RectTransform card = Reference<RectTransform>(fields, "card");
            GameObject mainViewObject = Reference<GameObject>(fields, "mainView");
            GameObject settingsViewObject = Reference<GameObject>(fields, "settingsView");
            Button play = Reference<Button>(fields, "playButton");
            Button shop = Reference<Button>(fields, "shopButton");
            Button settings = Reference<Button>(fields, "settingsButton");
            Button exit = Reference<Button>(fields, "exitButton");
            TextMeshProUGUI title = Reference<TextMeshProUGUI>(fields, "titleLabel");
            TextMeshProUGUI subtitle = Reference<TextMeshProUGUI>(fields, "subtitleLabel");
            TextMeshProUGUI gemAmount = Reference<TextMeshProUGUI>(fields, "gemAmountLabel");

            if (card == null || mainViewObject == null || settingsViewObject == null ||
                play == null || settings == null || exit == null)
            {
                EditorUtility.DisplayDialog("Main Menu Wiring Incomplete",
                    "Run Mining Simulator > Setup > Create Or Update Main Menu, then run this tool again.",
                    "OK");
                return;
            }

            RectTransform mainView = mainViewObject.transform as RectTransform;
            RectTransform settingsView = settingsViewObject.transform as RectTransform;
            Image rootImage = menu.GetComponent<Image>() ?? Undo.AddComponent<Image>(menu.gameObject);
            Undo.RecordObject(rootImage, "Style Main Menu Backdrop");
            rootImage.sprite = null;
            rootImage.color = new Color(0.025f, 0.012f, 0.01f, 1f);
            rootImage.raycastTarget = true;

            Sprite backgroundSprite = LoadFirstSprite(BackgroundPath);
            RectTransform background = EnsureImage(menu.transform, "Showcase Background");
            Stretch(background, 24f);
            Image backgroundImage = background.GetComponent<Image>();
            Undo.RecordObject(backgroundImage, "Assign Showcase Background");
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.color = Color.white;
            backgroundImage.preserveAspect = false;
            backgroundImage.raycastTarget = false;
            background.SetAsFirstSibling();

            MiningMainMenuShowcaseMotion motion = menu.GetComponent<MiningMainMenuShowcaseMotion>() ??
                                                   Undo.AddComponent<MiningMainMenuShowcaseMotion>(menu.gameObject);
            Undo.RecordObject(motion, "Configure Main Menu Motion");
            motion.Configure(background);

            Undo.RecordObject(card, "Expand Main Menu Card");
            Stretch(card);
            Image cardImage = card.GetComponent<Image>();
            if (cardImage != null)
            {
                Undo.RecordObject(cardImage, "Make Main Menu Card Transparent");
                cardImage.color = Color.clear;
                cardImage.raycastTarget = false;
            }
            Transform accent = card.Find("Accent");
            SetActive(accent != null ? accent.gameObject : null, false,
                "Hide Old Main Menu Accent");

            Stretch(mainView);
            Stretch(settingsView);
            RectTransform veil = EnsureImage(mainView, "Showcase Veil");
            Stretch(veil);
            StyleFlatImage(veil, new Color(0.025f, 0.005f, 0.04f, 0.12f));
            veil.SetAsFirstSibling();

            RectTransform bottomShade = EnsureImage(mainView, "Showcase Bottom Shade");
            AnchorBottom(bottomShade, 300f);
            StyleGradient(bottomShade, new Color(0f, 0f, 0f, 0.03f),
                new Color(0.015f, 0.004f, 0.008f, 0.88f));
            bottomShade.SetSiblingIndex(Mathf.Min(1, mainView.childCount - 1));

            // The background art already contains the polished Mining Simulator logo.
            SetActive(title != null ? title.gameObject : null, false, "Hide Duplicate Menu Title");
            SetActive(subtitle != null ? subtitle.gameObject : null, false,
                "Hide Duplicate Menu Subtitle");

            StyleButton(play, new Vector2(0f, 138f), new Vector2(400f, 94f),
                new Color(0.91f, 0.31f, 0.055f, 1f),
                new Color(0.43f, 0.055f, 0.018f, 1f), 36f, true);
            StyleButton(shop, new Vector2(-220f, 45f), new Vector2(200f, 58f),
                new Color(0.39f, 0.20f, 0.08f, 1f),
                new Color(0.13f, 0.055f, 0.025f, 1f), 22f, false);
            StyleButton(settings, new Vector2(0f, 45f), new Vector2(200f, 58f),
                new Color(0.39f, 0.20f, 0.08f, 1f),
                new Color(0.13f, 0.055f, 0.025f, 1f), 22f, false);
            StyleButton(exit, new Vector2(220f, 45f), new Vector2(200f, 58f),
                new Color(0.39f, 0.20f, 0.08f, 1f),
                new Color(0.13f, 0.055f, 0.025f, 1f), 22f, false);

            StyleGemBalance(mainView, gemAmount);
            StyleSettings(settingsView);

            EditorUtility.SetDirty(menu);
            EditorUtility.SetDirty(motion);
            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);

            // Rebuild the presentation handoff last so its references follow the restyled menu.
            MiningCinematicTransitionSetupMenu.Build();
            Selection.activeGameObject = menu.gameObject;
            EditorGUIUtility.PingObject(menu.gameObject);
            Debug.Log("Showcase Main Menu built from the existing project background. " +
                      "Play now uses a dark covered handoff with no white/pink flash. " +
                      "Settings, Shop, Exit, localization and gameplay logic remain wired. Save the scene.",
                menu.gameObject);
        }

        [MenuItem("Mining Simulator/UI/Build Showcase Main Menu", true)]
        private static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static void StyleGemBalance(RectTransform mainView, TextMeshProUGUI amount)
        {
            Transform iconTransform = Find(mainView, "Gem Icon");
            RectTransform icon = iconTransform as RectTransform;
            if (icon == null || amount == null)
            {
                return;
            }

            RectTransform plate = EnsureImage(mainView, "Gem Balance Backplate");
            SetRect(plate, new Vector2(1f, 1f), new Vector2(-166f, -56f),
                new Vector2(286f, 72f));
            StyleGradient(plate, new Color(0.20f, 0.08f, 0.025f, 0.96f),
                new Color(0.055f, 0.018f, 0.01f, 0.96f));
            AddOrStyleOutline(plate.gameObject, new Color(1f, 0.57f, 0.18f, 0.75f),
                new Vector2(2f, -2f));
            plate.SetSiblingIndex(Mathf.Max(0, icon.GetSiblingIndex()));

            SetRect(icon, new Vector2(1f, 1f), new Vector2(-265f, -56f), new Vector2(54f, 54f));
            Undo.RecordObject(amount.rectTransform, "Position Main Menu Gem Amount");
            SetRect(amount.rectTransform, new Vector2(1f, 1f), new Vector2(-130f, -56f),
                new Vector2(180f, 58f));
            Undo.RecordObject(amount, "Style Main Menu Gem Amount");
            amount.fontSize = 25f;
            amount.fontStyle = FontStyles.Bold;
            amount.alignment = TextAlignmentOptions.Center;
            amount.color = new Color(1f, 0.93f, 0.73f, 1f);
            amount.enableAutoSizing = true;
            amount.fontSizeMin = 16f;
            amount.fontSizeMax = 25f;
        }

        private static void StyleSettings(RectTransform settingsView)
        {
            RectTransform plate = EnsureImage(settingsView, "Showcase Settings Backplate");
            SetRect(plate, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060f, 650f));
            StyleGradient(plate, new Color(0.20f, 0.085f, 0.035f, 0.97f),
                new Color(0.035f, 0.012f, 0.012f, 0.98f));
            AddOrStyleOutline(plate.gameObject, new Color(0.95f, 0.54f, 0.18f, 0.9f),
                new Vector2(3f, -3f));
            plate.SetAsFirstSibling();
        }

        private static void StyleButton(Button button, Vector2 position, Vector2 size,
            Color top, Color bottom, float fontSize, bool primary)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rect = button.transform as RectTransform;
            SetRect(rect, new Vector2(0.5f, 0f), position, size);
            Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image != null)
            {
                Undo.RecordObject(image, "Style Main Menu Button");
                image.color = Color.white;
                image.raycastTarget = true;
            }

            MiningUiGradient gradient = button.GetComponent<MiningUiGradient>() ??
                                        Undo.AddComponent<MiningUiGradient>(button.gameObject);
            Undo.RecordObject(gradient, "Style Main Menu Button Gradient");
            gradient.SetColors(top, bottom);

            JuicyButtonTrim trim = button.GetComponent<JuicyButtonTrim>() ??
                                   Undo.AddComponent<JuicyButtonTrim>(button.gameObject);
            Undo.RecordObject(trim, "Style Main Menu Button Trim");
            trim.SetMedalRivets(false);

            Shadow shadow = button.GetComponent<Shadow>() ?? Undo.AddComponent<Shadow>(button.gameObject);
            Undo.RecordObject(shadow, "Style Main Menu Button Shadow");
            shadow.effectColor = new Color(0.08f, 0.015f, 0.005f, 0.9f);
            shadow.effectDistance = new Vector2(0f, primary ? -8f : -5f);
            shadow.useGraphicAlpha = true;
            AddOrStyleOutline(button.gameObject,
                primary ? new Color(1f, 0.67f, 0.27f, 1f) : new Color(0.78f, 0.43f, 0.16f, 1f),
                new Vector2(2f, -2f));

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.93f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.75f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                Undo.RecordObject(label, "Style Main Menu Button Label");
                label.fontSize = fontSize;
                label.fontStyle = FontStyles.Bold;
                label.color = new Color(1f, 0.96f, 0.83f, 1f);
                label.alignment = TextAlignmentOptions.Center;
                label.enableAutoSizing = true;
                label.fontSizeMin = Mathf.Max(14f, fontSize * 0.6f);
                label.fontSizeMax = fontSize;
            }
        }

        private static RectTransform EnsureImage(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing is RectTransform existingRect)
            {
                _ = existingRect.GetComponent<Image>() ?? Undo.AddComponent<Image>(existingRect.gameObject);
                return existingRect;
            }

            GameObject obj = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            obj.transform.SetParent(parent, false);
            return obj.GetComponent<RectTransform>();
        }

        private static void StyleFlatImage(RectTransform rect, Color color)
        {
            Image image = rect.GetComponent<Image>();
            Undo.RecordObject(image, "Style " + rect.name);
            image.sprite = null;
            image.color = color;
            image.raycastTarget = false;
        }

        private static void StyleGradient(RectTransform rect, Color top, Color bottom)
        {
            StyleFlatImage(rect, Color.white);
            MiningUiGradient gradient = rect.GetComponent<MiningUiGradient>() ??
                                        Undo.AddComponent<MiningUiGradient>(rect.gameObject);
            Undo.RecordObject(gradient, "Style " + rect.name + " Gradient");
            gradient.SetColors(top, bottom);
        }

        private static void AddOrStyleOutline(GameObject obj, Color color, Vector2 distance)
        {
            Outline outline = obj.GetComponent<Outline>() ?? Undo.AddComponent<Outline>(obj);
            Undo.RecordObject(outline, "Style " + obj.name + " Outline");
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position,
            Vector2 size)
        {
            if (rect == null)
            {
                return;
            }
            Undo.RecordObject(rect, "Layout " + rect.name);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect, float overscan = 0f)
        {
            if (rect == null)
            {
                return;
            }
            Undo.RecordObject(rect, "Stretch " + rect.name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(-overscan, -overscan);
            rect.offsetMax = new Vector2(overscan, overscan);
            rect.localScale = Vector3.one;
        }

        private static void AnchorBottom(RectTransform rect, float height)
        {
            Undo.RecordObject(rect, "Anchor " + rect.name);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
        }

        private static Sprite LoadFirstSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return sprite != null
                ? sprite
                : AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }

        private static Transform Find(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(child =>
                child.name == name);
        }

        private static T Reference<T>(SerializedObject fields, string name) where T : Object
        {
            return fields.FindProperty(name)?.objectReferenceValue as T;
        }

        private static void SetActive(GameObject obj, bool value, string undoName)
        {
            if (obj == null || obj.activeSelf == value)
            {
                return;
            }
            Undo.RecordObject(obj, undoName);
            obj.SetActive(value);
        }
    }
}
#endif
