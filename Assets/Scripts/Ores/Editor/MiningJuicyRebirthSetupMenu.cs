#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>One-shot scene authoring tool. Runtime layout remains fully editable in the scene.</summary>
    public static class MiningJuicyRebirthSetupMenu
    {
        private const string SpriteDirectory = "Assets/Generated/MiningUI";
        private static Sprite roundedSprite;
        private static Sprite circleSprite;

        [MenuItem("Mining Simulator/UI/Build Juicy Rebirth Card")]
        public static void Build()
        {
            MiningRebirthPanel panel = Object.FindFirstObjectByType<MiningRebirthPanel>(
                FindObjectsInactive.Include);
            if (panel == null)
            {
                Debug.LogWarning("Open the mining scene first: MiningRebirthPanel was not found. No scene was changed.");
                return;
            }

            SerializedObject panelFields = new(panel);
            Button openButton = panelFields.FindProperty("openButton").objectReferenceValue as Button;
            MiningRebirthSystem system = panelFields.FindProperty("rebirthSystem")
                .objectReferenceValue as MiningRebirthSystem;
            PlayerWallet wallet = panelFields.FindProperty("wallet").objectReferenceValue as PlayerWallet;
            if (openButton == null || system == null || wallet == null)
            {
                Debug.LogWarning("The existing Rebirth button/system/wallet wiring is incomplete. Run the normal mining HUD setup first; no scene was changed.");
                return;
            }

            RectTransform root = openButton.transform.parent as RectTransform;
            RectTransform buttonRoot = openButton.transform as RectTransform;
            if (root == null || buttonRoot == null)
            {
                Debug.LogWarning("The existing Rebirth HUD hierarchy is invalid. No scene was changed.");
                return;
            }

            roundedSprite = EnsureSprite("RebirthLeatherRounded.png", false);
            circleSprite = EnsureSprite("RebirthBrassCircle.png", true);
            MiningUiData uiData = AssetDatabase.LoadAssetAtPath<MiningUiData>(
                "Assets/GameData/UI/MiningUiData.asset");

            Undo.RecordObject(root, "Build Juicy Rebirth card");
            root.sizeDelta = new Vector2(560f, 370f);
            Style(root, Hex("#8F4422"), Hex("#2D0F04"), roundedSprite);
            Outline(root.gameObject, Hex("#210A03"), new Vector2(3f, -3f));
            Shadow(root.gameObject, new Color(0f, 0f, 0f, 0.72f), new Vector2(0f, -10f));

            foreach (string legacyName in new[] { "Header", "Boost", "Progress Bar", "Progress Label" })
            {
                Transform legacy = root.Find(legacyName);
                if (legacy == null || !legacy.gameObject.activeSelf)
                {
                    continue;
                }
                Undo.RecordObject(legacy.gameObject, "Hide superseded Rebirth visual");
                legacy.gameObject.SetActive(false);
            }

            RectTransform seam = Child(root, "Stitched_Seam", new Vector2(528f, 338f), Vector2.zero);
            Image seamImage = Image(seam);
            seamImage.color = new Color(1f, 1f, 1f, 0.001f);
            Outline(seam.gameObject, Hex("#F3D4A6"), new Vector2(2f, -2f));
            seam.SetAsFirstSibling();

            RectTransform header = Child(root, "Header_Pill", new Vector2(500f, 74f),
                new Vector2(0f, 126f));
            Style(header, Hex("#AA5A2E"), Hex("#461C09"), roundedSprite);
            Outline(header.gameObject, Hex("#2F1205"), new Vector2(2f, -2f));

            RectTransform medal = Child(header, "Rebirth_Medal", new Vector2(62f, 62f),
                new Vector2(-211f, 0f));
            Style(medal, Hex("#FFF0A8"), Hex("#78350F"), circleSprite);
            Outline(medal.gameObject, Hex("#321A07"), new Vector2(2f, -2f));
            RectTransform medalWell = Child(medal, "Medal_Well", new Vector2(50f, 50f), Vector2.zero);
            Style(medalWell, Hex("#481908"), Hex("#190601"), circleSprite);
            TextMeshProUGUI emblem = Text(medalWell, "Emblem", "R", 28f, Hex("#FFD75A"),
                new Vector2(46f, 38f), new Vector2(0f, 5f), TextAlignmentOptions.Center);
            emblem.fontStyle = FontStyles.Bold;
            TextMeshProUGUI badge = Text(medalWell, "Level_Badge", "LEVEL 0", 8f,
                Hex("#FFF0A8"), new Vector2(48f, 15f), new Vector2(0f, -17f),
                TextAlignmentOptions.Center);
            badge.fontStyle = FontStyles.Bold;

            TextMeshProUGUI title = Text(header, "Title_Text", "REBIRTH", 21f,
                Hex("#FFF0CA"), new Vector2(402f, 31f), new Vector2(31f, 13f),
                TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            TextMeshProUGUI reward = Text(header, "Reward_Text",
                "Permanent reward: x1.00 money & XP", 12f, Hex("#7DD3FC"),
                new Vector2(397f, 24f), new Vector2(31f, -17f), TextAlignmentOptions.Center);
            reward.fontStyle = FontStyles.Bold;

            RectTransform requirementRow = Child(root, "Requirement_Row", new Vector2(500f, 25f),
                new Vector2(0f, 57f));
            RectTransform coin = Child(requirementRow, "Coin_Icon", new Vector2(23f, 23f),
                new Vector2(-238f, 0f));
            Image coinImage = Image(coin);
            coinImage.sprite = uiData != null ? uiData.MoneyIconSprite : null;
            coinImage.color = Color.white;
            coinImage.preserveAspect = true;
            Text(requirementRow, "Requirement_Label", "MONEY REQUIRED FOR REBIRTH", 11f,
                Hex("#F6D6A8"), new Vector2(330f, 22f), new Vector2(-52f, 0f),
                TextAlignmentOptions.Left).fontStyle = FontStyles.Bold;
            TextMeshProUGUI percent = Text(requirementRow, "Percent_Text", "0%", 13f,
                Hex("#FDE047"), new Vector2(72f, 22f), new Vector2(214f, 0f),
                TextAlignmentOptions.Right);
            percent.fontStyle = FontStyles.Bold;

            RectTransform trench = Child(root, "Gold_Trench", new Vector2(500f, 48f),
                new Vector2(0f, 20f));
            Style(trench, Hex("#241006"), Hex("#120703"), roundedSprite);
            Outline(trench.gameObject, Hex("#482410"), new Vector2(2f, -2f));
            RectTransform fill = Child(trench, "Gold_Fill", new Vector2(-8f, -8f), Vector2.zero);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(4f, 4f);
            fill.offsetMax = new Vector2(-4f, -4f);
            Image fillImage = Image(fill);
            fillImage.sprite = roundedSprite;
            fillImage.type = UnityEngine.UI.Image.Type.Filled;
            fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 0f;
            fillImage.color = Color.white;
            (fill.GetComponent<MiningUiGradient>() ?? Undo.AddComponent<MiningUiGradient>(fill.gameObject))
                .SetColors(Hex("#FEF08A"), Hex("#A16207"));
            TextMeshProUGUI requirement = Text(trench, "Counter_Text", "0 / 1,000 MONEY", 15f,
                Hex("#241006"), new Vector2(470f, 38f), Vector2.zero,
                TextAlignmentOptions.Center);
            requirement.fontStyle = FontStyles.Bold;

            Undo.RecordObject(buttonRoot, "Author Rebirth action button");
            buttonRoot.anchorMin = buttonRoot.anchorMax = buttonRoot.pivot = new Vector2(0.5f, 0.5f);
            buttonRoot.anchoredPosition = new Vector2(0f, -99f);
            buttonRoot.sizeDelta = new Vector2(500f, 96f);
            Image rootImage = buttonRoot.GetComponent<Image>() ?? Undo.AddComponent<Image>(buttonRoot.gameObject);
            rootImage.color = new Color(1f, 1f, 1f, 0.001f);
            rootImage.raycastTarget = true;
            openButton.targetGraphic = rootImage;
            openButton.transition = UnityEngine.UI.Selectable.Transition.None;
            foreach (TextMeshProUGUI oldText in buttonRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (oldText.transform.parent == buttonRoot)
                {
                    // MiningRebirthPanel discovers its legacy label at runtime. Keep that exact
                    // direct child alive and invisible so it never grabs one of the new labels.
                    Undo.RecordObject(oldText.gameObject, "Preserve legacy Rebirth label binding");
                    Undo.RecordObject(oldText, "Hide legacy Rebirth label rendering");
                    oldText.gameObject.SetActive(true);
                    oldText.color = new Color(oldText.color.r, oldText.color.g, oldText.color.b, 0f);
                    oldText.raycastTarget = false;
                    oldText.transform.SetAsFirstSibling();
                }
            }

            RectTransform shadow = Child(buttonRoot, "Base_Shadow", new Vector2(500f, 96f),
                new Vector2(0f, -8f));
            Style(shadow, Hex("#1C0701"), Hex("#1C0701"), roundedSprite);
            RectTransform body = Child(buttonRoot, "Button_Body", new Vector2(500f, 96f), Vector2.zero);
            Image bodyImage = Style(body, Hex("#443C39"), Hex("#1F1B19"), roundedSprite);
            Outline(body.gameObject, Hex("#5A524E"), new Vector2(2f, -2f));

            RectTransform fireWell = Child(body, "Rebirth_Icon_Well", new Vector2(62f, 62f),
                new Vector2(-207f, 0f));
            Style(fireWell, Hex("#4B1807"), Hex("#210801"), circleSprite);
            Outline(fireWell.gameObject, Hex("#F59E0B"), new Vector2(1f, -1f));
            TextMeshProUGUI fire = Text(fireWell, "Icon", "R", 30f, Hex("#FFB703"),
                new Vector2(55f, 50f), Vector2.zero, TextAlignmentOptions.Center);
            fire.fontStyle = FontStyles.Bold;
            TextMeshProUGUI actionTitle = Text(body, "Title_Text", "NOT READY YET", 20f,
                Hex("#BEB8B3"), new Vector2(335f, 30f), new Vector2(5f, 17f),
                TextAlignmentOptions.Left);
            actionTitle.fontStyle = FontStyles.Bold;
            TextMeshProUGUI actionSub = Text(body, "Sub_Text", "Need more money to unlock", 12f,
                Hex("#A19B97"), new Vector2(335f, 34f), new Vector2(5f, -18f),
                TextAlignmentOptions.Left);
            RectTransform arrowWell = Child(body, "Arrow_Well", new Vector2(42f, 42f),
                new Vector2(217f, 0f));
            Style(arrowWell, Hex("#3A1306"), Hex("#1A0601"), circleSprite);
            TextMeshProUGUI arrow = Text(arrowWell, "Arrow", ">", 24f, Hex("#FFE6A3"),
                new Vector2(38f, 36f), Vector2.zero, TextAlignmentOptions.Center);
            arrow.fontStyle = FontStyles.Bold;

            JuicyRebirth presentation = openButton.GetComponent<JuicyRebirth>() ??
                                          Undo.AddComponent<JuicyRebirth>(openButton.gameObject);
            SerializedObject fields = new(presentation);
            Set(fields, "rebirthSystem", system);
            Set(fields, "wallet", wallet);
            Set(fields, "buttonBody", body);
            Set(fields, "buttonBackground", bodyImage);
            Set(fields, "progressFill", fillImage);
            Set(fields, "levelBadgeText", badge);
            Set(fields, "titleText", title);
            Set(fields, "currentRewardText", reward);
            Set(fields, "requirementText", requirement);
            Set(fields, "percentText", percent);
            Set(fields, "buttonTitleText", actionTitle);
            Set(fields, "buttonSubText", actionSub);
            fields.ApplyModifiedProperties();

            EditorUtility.SetDirty(panel);
            EditorUtility.SetDirty(openButton);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log("Juicy Rebirth card built. Existing Rebirth rewards, confirmation, reset logic and flash remain authoritative. Adjust the authored RectTransforms freely, then save your scene.");
        }

        private static void Set(SerializedObject fields, string name, Object value) =>
            fields.FindProperty(name).objectReferenceValue = value;

        private static RectTransform Child(Transform parent, string name, Vector2 size, Vector2 position)
        {
            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing == null)
            {
                obj = new GameObject(name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(obj, "Build Rebirth " + name);
                obj.transform.SetParent(parent, false);
            }
            else
            {
                obj = existing.gameObject;
                if (!obj.activeSelf)
                {
                    obj.SetActive(true);
                }
            }
            RectTransform rect = obj.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Layout Rebirth " + name);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static Image Image(RectTransform rect)
        {
            Image image = rect.GetComponent<Image>() ?? Undo.AddComponent<Image>(rect.gameObject);
            image.raycastTarget = false;
            return image;
        }

        private static Image Style(RectTransform rect, Color top, Color bottom, Sprite sprite)
        {
            Image image = Image(rect);
            image.sprite = sprite;
            image.type = sprite == circleSprite
                ? UnityEngine.UI.Image.Type.Simple
                : UnityEngine.UI.Image.Type.Sliced;
            image.color = Color.white;
            (rect.GetComponent<MiningUiGradient>() ?? Undo.AddComponent<MiningUiGradient>(rect.gameObject))
                .SetColors(top, bottom);
            return image;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string value, float size,
            Color color, Vector2 dimensions, Vector2 position, TextAlignmentOptions alignment)
        {
            RectTransform rect = Child(parent, name, dimensions, position);
            TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>() ??
                                   Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static void Outline(GameObject obj, Color color, Vector2 distance)
        {
            UnityEngine.UI.Outline outline = obj.GetComponent<UnityEngine.UI.Outline>() ??
                                              Undo.AddComponent<UnityEngine.UI.Outline>(obj);
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void Shadow(GameObject obj, Color color, Vector2 distance)
        {
            UnityEngine.UI.Shadow shadow = null;
            foreach (UnityEngine.UI.Shadow candidate in obj.GetComponents<UnityEngine.UI.Shadow>())
            {
                if (candidate.GetType() == typeof(Shadow))
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
