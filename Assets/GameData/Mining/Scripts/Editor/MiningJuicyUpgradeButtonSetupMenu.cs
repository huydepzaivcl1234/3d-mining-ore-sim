#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Upgrades only the existing Open Upgrades uGUI Button; does not edit a scene asset.</summary>
    public static class MiningJuicyUpgradeButtonSetupMenu
    {
        private const string SpriteDirectory = "Assets/Generated/MiningUI";
        private static Sprite pillSprite;
        private static Sprite medalSprite;

        [MenuItem("Mining Simulator/UI/Build Juicy Upgrade Button")]
        public static void Build()
        {
            MiningUpgradePanel panel = Object.FindFirstObjectByType<MiningUpgradePanel>(
                FindObjectsInactive.Include);
            if (panel == null)
            {
                Debug.LogWarning("Open a scene with MiningUpgradePanel first. No scene was changed.");
                return;
            }

            SerializedObject panelFields = new(panel);
            UnityEngine.UI.Button button = panelFields.FindProperty("openButton")
                .objectReferenceValue as UnityEngine.UI.Button;
            if (button == null)
            {
                Transform canvas = panel.transform.Find("Mining HUD Canvas");
                button = canvas?.Find("NPC Shop/Open Upgrades")?.GetComponent<UnityEngine.UI.Button>();
                if (button == null)
                {
                    Debug.LogWarning("The existing Open Upgrades Button was not found. No scene was changed.");
                    return;
                }
                panelFields.FindProperty("openButton").objectReferenceValue = button;
                panelFields.ApplyModifiedProperties();
            }

            RectTransform root = button.transform as RectTransform;
            if (root == null) return;
            pillSprite = EnsureSprite("LeatherPillRounded.png", false);
            medalSprite = EnsureSprite("BrassMedalCircle.png", true);
            Undo.RecordObject(root, "Fit Juicy Upgrade Button");
            // The HUD panel is 330 px wide. Keep the new three-column button within it.
            root.sizeDelta = new Vector2(294f, 68f);
            root.anchoredPosition = new Vector2(root.anchoredPosition.x, -278f);
            if (root.parent is RectTransform shopRect && shopRect.sizeDelta.y < 360f)
            {
                Undo.RecordObject(shopRect, "Fit upgrade button inside shop");
                shopRect.sizeDelta = new Vector2(shopRect.sizeDelta.x, 360f);
            }

            // Keep original UI for Undo and scene references, but hide it behind this view.
            foreach (Transform child in root)
            {
                if (child.name == "Base_Shadow" || child.name == "Button_Body") continue;
                if (child.gameObject.activeSelf)
                {
                    Undo.RecordObject(child.gameObject, "Hide legacy upgrade button content");
                    child.gameObject.SetActive(false);
                }
            }

            UnityEngine.UI.Image hitArea = Image(root);
            Undo.RecordObject(hitArea, "Retain upgrade button hit area");
            hitArea.color = new Color(1f, 1f, 1f, 0.001f);
            hitArea.raycastTarget = true;
            Undo.RecordObject(button, "Retain upgrade button click");
            button.targetGraphic = hitArea;
            button.transition = UnityEngine.UI.Selectable.Transition.None;

            RectTransform baseShadow = Child(root, "Base_Shadow", new Vector2(294f, 68f),
                new Vector2(0f, -8f));
            Gradient(baseShadow, Hex("#200D05"), Hex("#1B0A03"));
            Round(baseShadow);
            Outline(baseShadow.gameObject, Hex("#130701"), new Vector2(2f, -2f));

            RectTransform body = Child(root, "Button_Body", new Vector2(294f, 68f), Vector2.zero);
            Gradient(body, Hex("#9C5930"), Hex("#3E1B0B"));
            Round(body);
            Outline(body.gameObject, Hex("#361506"), new Vector2(2f, -2f));

            RectTransform frame = Child(body, "Frame_Border", new Vector2(283f, 57f), Vector2.zero);
            Gradient(frame, Hex("#A35E33"), Hex("#592911"));
            Round(frame);
            Outline(frame.gameObject, Hex("#F3D4A6"), new Vector2(1f, -1f));
            JuicyButtonTrim stitching = frame.GetComponent<JuicyButtonTrim>() ??
                                         Undo.AddComponent<JuicyButtonTrim>(frame.gameObject);
            stitching.SetMedalRivets(false);

            // Draw the shimmer after the frame, below the badge/text/plaque.
            RectTransform clipRect = Child(body, "Mask", new Vector2(278f, 53f), Vector2.zero);
            UnityEngine.UI.Image maskImage = Image(clipRect);
            maskImage.color = Color.white;
            maskImage.raycastTarget = false;
            Round(clipRect);
            UnityEngine.UI.Mask mask = clipRect.GetComponent<UnityEngine.UI.Mask>() ??
                                       Undo.AddComponent<UnityEngine.UI.Mask>(clipRect.gameObject);
            mask.showMaskGraphic = false;
            RectTransform shimmer = Child(clipRect, "Shimmer_FX", new Vector2(38f, 54f),
                new Vector2(-180f, 0f));
            Image(shimmer).color = new Color(1f, 0.95f, 0.82f, 0.22f);

            RectTransform ironLeft = Child(body, "Iron_Band_Left", new Vector2(8f, 55f),
                new Vector2(-142f, 0f));
            Gradient(ironLeft, Hex("#585563"), Hex("#201F25"));
            Dot(ironLeft, "Rivet_Top", new Vector2(0f, 18f));
            Dot(ironLeft, "Rivet_Bottom", new Vector2(0f, -18f));
            RectTransform ironRight = Child(body, "Iron_Band_Right", new Vector2(8f, 55f),
                new Vector2(142f, 0f));
            Gradient(ironRight, Hex("#585563"), Hex("#201F25"));
            Dot(ironRight, "Rivet_Top", new Vector2(0f, 18f));
            Dot(ironRight, "Rivet_Bottom", new Vector2(0f, -18f));

            RectTransform avatar = Child(body, "Avatar_Group", new Vector2(54f, 54f),
                new Vector2(-110f, 0f));
            RectTransform medal = Child(avatar, "Medal_Brass", new Vector2(52f, 52f), Vector2.zero);
            Gradient(medal, Hex("#FFE285"), Hex("#59390A"));
            Circle(medal);
            Outline(medal.gameObject, Hex("#3E290A"), new Vector2(1f, -1f));
            (medal.GetComponent<JuicyButtonTrim>() ?? Undo.AddComponent<JuicyButtonTrim>(medal.gameObject))
                .SetMedalRivets(true);
            RectTransform crystal = Child(avatar, "Crystal_Well", new Vector2(42f, 42f), Vector2.zero);
            Gradient(crystal, Hex("#67E8F9"), Hex("#083344"));
            Circle(crystal);
            Outline(crystal.gameObject, Hex("#0E4457"), new Vector2(1f, -1f));
            TextMeshProUGUI pickaxe = Text(crystal, "Pickaxe_Icon", "⛏", 25f,
                Hex("#FFE285"), new Vector2(34f, 34f), Vector2.zero);
            pickaxe.alignment = TextAlignmentOptions.Center;
            RectTransform gemBadge = Child(avatar, "Crystal_Badge", new Vector2(15f, 15f),
                new Vector2(-18f, -17f));
            Gradient(gemBadge, Hex("#B9FAFF"), Hex("#0891B2"));
            Circle(gemBadge);
            Text(gemBadge, "Gem_Symbol", "◆", 11f, Hex("#063743"),
                new Vector2(13f, 13f), Vector2.zero).alignment = TextAlignmentOptions.Center;
            RectTransform sparkle = Child(avatar, "Sparkle", new Vector2(13f, 13f),
                new Vector2(19f, 19f));
            Gradient(sparkle, Hex("#C3FAFF"), Hex("#37C8DE"));
            Circle(sparkle);
            Text(sparkle, "Star", "✦", 10f, Hex("#004E62"),
                new Vector2(12f, 12f), Vector2.zero).alignment = TextAlignmentOptions.Center;

            RectTransform content = Child(body, "Content_Texts", new Vector2(141f, 53f),
                new Vector2(-12f, 0f));
            TextMeshProUGUI title = Text(content, "Title_Text", "UPGRADES", 14f,
                Hex("#FFF0CA"), new Vector2(108f, 22f), new Vector2(-16f, 13f));
            title.fontStyle = FontStyles.Bold;
            TextMeshProUGUI badge = Text(content, "Expand_Badge", "EXPAND", 9f,
                Hex("#67E8F9"), new Vector2(48f, 16f), new Vector2(47f, 14f));
            badge.alignment = TextAlignmentOptions.Center;
            TextMeshProUGUI subtitle = Text(content, "Subtitle_Text",
                "Pickaxes & Gear | Backpack", 9f, Hex("#EBD4AE"),
                new Vector2(139f, 28f), new Vector2(0f, -12f));

            RectTransform plaque = Child(body, "Action_Plaque", new Vector2(79f, 52f),
                new Vector2(101f, 0f));
            RectTransform plaqueBg = Child(plaque, "Plaque_Bg", new Vector2(78f, 51f), Vector2.zero);
            Gradient(plaqueBg, Hex("#3D1D0C"), Hex("#200D04"));
            Round(plaqueBg);
            Outline(plaqueBg.gameObject, Hex("#633719"), new Vector2(1f, -1f));
            TextMeshProUGUI action = Text(plaque, "Action_Text", "OPEN PANEL", 10f,
                Hex("#FFE285"), new Vector2(52f, 42f), new Vector2(-9f, 0f));
            action.fontStyle = FontStyles.Bold;
            action.alignment = TextAlignmentOptions.Center;
            RectTransform arrow = Child(plaque, "Arrow_Badge", new Vector2(19f, 21f),
                new Vector2(27f, 0f));
            Gradient(arrow, Hex("#1B0D06"), Hex("#261309"));
            Circle(arrow);
            Outline(arrow.gameObject, Hex("#B47A19"), new Vector2(1f, -1f));
            Text(arrow, "Chevron", "▶", 12f, Hex("#67E8F9"),
                new Vector2(17f, 18f), Vector2.zero).alignment = TextAlignmentOptions.Center;

            JuicyOpenPanelButton juicy = button.GetComponent<JuicyOpenPanelButton>() ??
                                         Undo.AddComponent<JuicyOpenPanelButton>(button.gameObject);
            SerializedObject fields = new(juicy);
            Set(fields, "buttonBody", body);
            Set(fields, "shimmerRect", shimmer);
            Set(fields, "titleText", title);
            Set(fields, "expandBadgeText", badge);
            Set(fields, "subtitleText", subtitle);
            Set(fields, "actionText", action);
            fields.ApplyModifiedProperties();

            EditorUtility.SetDirty(panel);
            EditorUtility.SetDirty(button);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log("Juicy Upgrade Button built in the active scene. MiningUpgradePanel still handles opening. Save your scene when satisfied.");
        }

        private static void Set(SerializedObject fields, string name, Object value) =>
            fields.FindProperty(name).objectReferenceValue = value;

        private static RectTransform Child(Transform parent, string name, Vector2 size, Vector2 pos)
        {
            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing == null)
            {
                obj = new GameObject(name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(obj, "Build upgrade button " + name);
                obj.transform.SetParent(parent, false);
            }
            else obj = existing.gameObject;
            RectTransform rect = obj.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Layout upgrade button " + name);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        private static UnityEngine.UI.Image Image(RectTransform rect)
        {
            UnityEngine.UI.Image image = rect.GetComponent<UnityEngine.UI.Image>() ??
                                         Undo.AddComponent<UnityEngine.UI.Image>(rect.gameObject);
            image.raycastTarget = false;
            return image;
        }

        private static void Gradient(RectTransform rect, Color top, Color bottom)
        {
            UnityEngine.UI.Image image = Image(rect);
            image.color = Color.white;
            MiningUiGradient gradient = rect.GetComponent<MiningUiGradient>() ??
                                        Undo.AddComponent<MiningUiGradient>(rect.gameObject);
            gradient.SetColors(top, bottom);
        }

        private static void Round(RectTransform rect)
        {
            UnityEngine.UI.Image image = Image(rect);
            image.sprite = pillSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
        }

        private static void Circle(RectTransform rect)
        {
            UnityEngine.UI.Image image = Image(rect);
            image.sprite = medalSprite;
            image.type = UnityEngine.UI.Image.Type.Simple;
        }

        private static Sprite EnsureSprite(string fileName, bool circle)
        {
            string path = SpriteDirectory + "/" + fileName;
            Sprite asset = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (asset != null) return asset;

            Directory.CreateDirectory(SpriteDirectory);
            const int side = 64;
            Texture2D texture = new(side, side, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[side * side];
            for (int y = 0; y < side; y++)
            {
                for (int x = 0; x < side; x++)
                {
                    float u = x - (side - 1f) * 0.5f;
                    float v = y - (side - 1f) * 0.5f;
                    float distance = circle
                        ? Mathf.Sqrt(u * u + v * v)
                        : Mathf.Sqrt(Mathf.Pow(Mathf.Max(0f, Mathf.Abs(u) - 9.5f), 2f) +
                                     Mathf.Pow(Mathf.Max(0f, Mathf.Abs(v) - 9.5f), 2f));
                    byte alpha = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(
                        (circle ? 31.5f : 22.5f) - distance));
                    pixels[y * side + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
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

        private static void Dot(Transform parent, string name, Vector2 pos)
        {
            RectTransform dot = Child(parent, name, new Vector2(4f, 4f), pos);
            Gradient(dot, Hex("#A8A7AD"), Hex("#1F1E24"));
        }

        private static void Outline(GameObject obj, Color color, Vector2 offset)
        {
            UnityEngine.UI.Outline effect = obj.GetComponent<UnityEngine.UI.Outline>() ??
                                             Undo.AddComponent<UnityEngine.UI.Outline>(obj);
            effect.effectColor = color;
            effect.effectDistance = offset;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string value,
            float fontSize, Color color, Vector2 size, Vector2 pos)
        {
            RectTransform rect = Child(parent, name, size, pos);
            TextMeshProUGUI label = rect.GetComponent<TextMeshProUGUI>() ??
                                    Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
            label.text = value;
            label.fontSize = fontSize;
            label.color = color;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.enableAutoSizing = false;
            label.raycastTarget = false;
            return label;
        }

        private static Color Hex(string hex) =>
            ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
    }
}
#endif
