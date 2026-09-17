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
        private const string BackgroundPath =
            "Assets/Ores/Icons/MainMenuShowcaseBackground.jpg";
        private const string LogoPath = "Assets/Ores/Icons/MainMenuShowcaseLogo.jpg";

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
            TextMeshProUGUI title = Reference<TextMeshProUGUI>(fields, "titleLabel");
            TextMeshProUGUI subtitle = Reference<TextMeshProUGUI>(fields, "subtitleLabel");
            TextMeshProUGUI gemAmount = Reference<TextMeshProUGUI>(fields, "gemAmountLabel");

            if (card == null || mainViewObject == null || settingsViewObject == null)
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

            EnsureSpriteImport(BackgroundPath);
            EnsureSpriteImport(LogoPath);
            Sprite backgroundSprite = LoadFirstSprite(BackgroundPath);
            Sprite logoSprite = LoadFirstSprite(LogoPath);
            RectTransform background = EnsureImage(menu.transform, "Showcase Background");
            Stretch(background, 24f);
            Image backgroundImage = background.GetComponent<Image>();
            Undo.RecordObject(backgroundImage, "Assign Showcase Background");
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.color = Color.white;
            backgroundImage.preserveAspect = false;
            backgroundImage.raycastTarget = false;
            background.SetAsFirstSibling();

            RectTransform ambientFx = EnsureAmbientFx(menu.transform);
            Stretch(ambientFx);
            ambientFx.SetSiblingIndex(Mathf.Min(1, menu.transform.childCount - 1));

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

            RectTransform logo = EnsureImage(mainView, "Showcase Logo");
            SetRect(logo, new Vector2(0.5f, 1f), new Vector2(0f, -220f),
                new Vector2(430f, 430f));
            Image logoImage = logo.GetComponent<Image>();
            Undo.RecordObject(logoImage, "Assign Showcase Logo");
            logoImage.sprite = logoSprite;
            logoImage.color = Color.white;
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;
            logo.SetAsLastSibling();

            MiningMainMenuShowcaseMotion motion = menu.GetComponent<MiningMainMenuShowcaseMotion>() ??
                                                   Undo.AddComponent<MiningMainMenuShowcaseMotion>(menu.gameObject);
            Undo.RecordObject(motion, "Configure Main Menu Motion");
            motion.Configure(background, logo);

            SetActive(title != null ? title.gameObject : null, false, "Hide Duplicate Menu Title");
            SetActive(subtitle != null ? subtitle.gameObject : null, false,
                "Hide Duplicate Menu Subtitle");

            StyleGemBalance(mainView, gemAmount);
            StyleSettings(settingsView);

            EditorUtility.SetDirty(menu);
            EditorUtility.SetDirty(motion);
            EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);

            // Rebuild the presentation handoff last so its references follow the restyled menu.
            MiningCinematicTransitionSetupMenu.Build();
            Selection.activeGameObject = menu.gameObject;
            EditorGUIUtility.PingObject(menu.gameObject);
            Debug.Log("Showcase Main Menu motion upgraded without changing any button. " +
                      "Play now uses the radiant camera-dive transition and hidden gameplay " +
                      "handoff. Settings, Shop, Exit, localization and gameplay logic remain wired. " +
                      "Save the scene.",
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

        private static RectTransform EnsureAmbientFx(Transform parent)
        {
            const string name = "Showcase Amethyst Dust";
            Transform existing = parent.Find(name);
            if (existing is RectTransform existingRect)
            {
                MiningMenuAmbientFx existingFx = existingRect.GetComponent<MiningMenuAmbientFx>() ??
                                                       Undo.AddComponent<MiningMenuAmbientFx>(existingRect.gameObject);
                existingFx.raycastTarget = false;
                return existingRect;
            }

            GameObject obj = new(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(MiningMenuAmbientFx));
            Undo.RegisterCreatedObjectUndo(obj, "Create Showcase Amethyst Dust");
            obj.transform.SetParent(parent, false);
            obj.GetComponent<MiningMenuAmbientFx>().raycastTarget = false;
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

        private static void EnsureSpriteImport(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null ||
                (importer.textureType == TextureImporterType.Sprite &&
                 importer.spriteImportMode == SpriteImportMode.Single &&
                 !importer.mipmapEnabled))
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
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
