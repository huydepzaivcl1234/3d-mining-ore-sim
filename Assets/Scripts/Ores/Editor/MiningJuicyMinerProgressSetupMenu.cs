#if UNITY_EDITOR
using System.IO;
using Microlight.MicroBar;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Styles the authored HUD in place and leaves the real MicroBar/controller wired.</summary>
    public static class MiningJuicyMinerProgressSetupMenu
    {
        private const string SpriteDirectory = "Assets/Generated/MiningUI";
        private static Sprite cardSprite;
        private static Sprite medalSprite;

        [MenuItem("Mining Simulator/UI/Build Juicy Miner Progress")]
        public static void Build()
        {
            NpcProgressionHud original = Object.FindFirstObjectByType<NpcProgressionHud>(
                FindObjectsInactive.Include);
            if (original == null)
            {
                Debug.LogWarning("Open a scene with the existing NPC Progress HUD first.");
                return;
            }

            RectTransform root = original.transform as RectTransform;
            Transform existingBar = root?.Find("Experience Bar");
            if (existingBar == null)
            {
                Debug.LogWarning("The existing Experience Bar is missing. Run the normal mining HUD setup first; this scene was not changed.");
                return;
            }
            MicroBar bar = existingBar.GetComponent<MicroBar>();
            if (bar == null)
            {
                Debug.LogWarning("The existing Experience Bar has no MicroBar; this scene was not changed.");
                return;
            }

            SerializedObject hudFields = new(original);
            NpcProgressionSystem progression = hudFields.FindProperty("progressionSystem")
                .objectReferenceValue as NpcProgressionSystem;
            if (progression == null)
                progression = Object.FindFirstObjectByType<NpcProgressionSystem>(FindObjectsInactive.Include);
            OreSpawner spawner = Object.FindFirstObjectByType<OreSpawner>(FindObjectsInactive.Include);
            if (progression == null || spawner == null || spawner.SpawnData == null)
            {
                Debug.LogWarning("NPC progression or the configured ore spawn table is missing; this scene was not changed.");
                return;
            }

            cardSprite = EnsureSprite("MinerLeatherCardRounded.png", false);
            medalSprite = EnsureSprite("MinerBrassCircle.png", true);
            Undo.RecordObject(root, "Fit leather miner progress HUD");
            root.sizeDelta = new Vector2(400f, 270f);
            UnityEngine.UI.Image surface = Image(root);
            Gradient(root, Hex("#8A4823"), Hex("#321406"));
            Round(root);
            JuicyButtonTrim stitching = root.GetComponent<JuicyButtonTrim>() ??
                                         Undo.AddComponent<JuicyButtonTrim>(root.gameObject);
            stitching.SetMedalRivets(false);
            Outline(root.gameObject, Hex("#180A03"), new Vector2(3f, -3f));
            surface.raycastTarget = false;

            foreach (string oldName in new[] { "Header", "Level", "Power" })
            {
                Transform legacy = root.Find(oldName);
                if (legacy == null || !legacy.gameObject.activeSelf) continue;
                Undo.RecordObject(legacy.gameObject, "Hide superseded progress HUD surface");
                legacy.gameObject.SetActive(false);
            }

            RectTransform card = Child(root, "Card_Visual", new Vector2(400f, 270f), Vector2.zero);
            RectTransform header = Child(card, "Header_Pill", new Vector2(378f, 65f),
                new Vector2(0f, 96f));
            Gradient(header, Hex("#945229"), Hex("#59280E"));
            Round(header);
            Outline(header.gameObject, Hex("#2B1003"), new Vector2(1f, -1f));

            RectTransform avatar = Child(header, "Avatar_Group", new Vector2(57f, 57f),
                new Vector2(-157f, 0f));
            RectTransform medal = Child(avatar, "Medal_Brass", new Vector2(56f, 56f), Vector2.zero);
            Gradient(medal, Hex("#FFE285"), Hex("#59390A"));
            Circle(medal);
            (medal.GetComponent<JuicyButtonTrim>() ?? Undo.AddComponent<JuicyButtonTrim>(medal.gameObject))
                .SetMedalRivets(true);
            RectTransform inner = Child(avatar, "Avatar_Well", new Vector2(46f, 46f), Vector2.zero);
            Gradient(inner, Hex("#1B3644"), Hex("#0C1F29"));
            Circle(inner);
            RectTransform minerIcon = Child(avatar, "Miner_Icon", new Vector2(34f, 32f),
                new Vector2(0f, 7f));
            MiningUiData data = AssetDatabase.LoadAssetAtPath<MiningUiData>(
                "Assets/GameData/UI/MiningUiData.asset");
            UnityEngine.UI.Image iconImage = Image(minerIcon);
            iconImage.sprite = data != null ? data.NpcIconSprite : null;
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            TextMeshProUGUI badge = Text(avatar, "Level_Badge", "LV.1", 10f,
                Hex("#FFE285"), new Vector2(54f, 18f), new Vector2(0f, -18f));
            badge.alignment = TextAlignmentOptions.Center;
            badge.fontStyle = FontStyles.Bold;

            TextMeshProUGUI title = Text(header, "Title_Text", "MINER PROGRESS", 20f,
                Hex("#FFF0CA"), new Vector2(268f, 27f), new Vector2(25f, 12f));
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;
            TextMeshProUGUI rank = Text(header, "Rank_Text", "Miner level 1", 12f,
                Hex("#86EFAC"), new Vector2(247f, 22f), new Vector2(17f, -15f));
            rank.alignment = TextAlignmentOptions.Center;
            RectTransform star = Child(header, "Star_Medal", new Vector2(32f, 32f),
                new Vector2(162f, 0f));
            Gradient(star, Hex("#5A3118"), Hex("#301608"));
            Circle(star);
            Text(star, "Star", "★", 18f, Hex("#FFE285"),
                new Vector2(30f, 29f), Vector2.zero).alignment = TextAlignmentOptions.Center;

            RectTransform well = Child(card, "Stats_Well", new Vector2(370f, 79f),
                new Vector2(0f, 11f));
            Gradient(well, Hex("#351A0D"), Hex("#1F0E05"));
            Round(well);
            Outline(well.gameObject, Hex("#5A3118"), new Vector2(1f, -1f));
            TextMeshProUGUI powerLabel = Text(well, "Power_Label", "MINING POWER", 11f,
                Hex("#EBD4AE"), new Vector2(134f, 19f), new Vector2(-107f, 21f));
            powerLabel.alignment = TextAlignmentOptions.Center;
            TextMeshProUGUI powerValue = Text(well, "Power_Value", "6", 22f,
                Hex("#FFE285"), new Vector2(120f, 35f), new Vector2(-107f, -12f));
            powerValue.alignment = TextAlignmentOptions.Center;
            powerValue.fontStyle = FontStyles.Bold;
            RectTransform separator = Child(well, "Divider", new Vector2(2f, 59f),
                new Vector2(-30f, 0f));
            Image(separator).color = Hex("#4D2812");
            TextMeshProUGUI rewardLabel = Text(well, "Reward_Label", "NEXT ORE UNLOCK", 11f,
                Hex("#EBD4AE"), new Vector2(221f, 19f), new Vector2(72f, 21f));
            rewardLabel.alignment = TextAlignmentOptions.Center;
            TextMeshProUGUI reward = Text(well, "Reward_Text", "Copper • power 6/7", 15f,
                Hex("#67E8F9"), new Vector2(224f, 42f), new Vector2(72f, -13f));
            reward.alignment = TextAlignmentOptions.Center;
            reward.fontStyle = FontStyles.Bold;

            TextMeshProUGUI xpLabel = Text(card, "XP_Label", "WORK XP", 12f,
                Hex("#EBD4AE"), new Vector2(214f, 20f), new Vector2(-84f, -51f));
            xpLabel.alignment = TextAlignmentOptions.Left;
            TextMeshProUGUI xpPercent = Text(card, "XP_Percent", "0%", 12f,
                Hex("#86EFAC"), new Vector2(65f, 20f), new Vector2(151f, -51f));
            xpPercent.alignment = TextAlignmentOptions.Right;
            RectTransform trench = Child(card, "XP_Trench", new Vector2(371f, 39f),
                new Vector2(0f, -81f));
            Gradient(trench, Hex("#201108"), Hex("#140B05"));
            Round(trench);
            Outline(trench.gameObject, Hex("#482410"), new Vector2(2f, -2f));

            RectTransform barRect = bar.transform as RectTransform;
            Undo.RecordObject(barRect, "Keep original MicroBar within trench");
            barRect.anchorMin = barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 1f);
            barRect.sizeDelta = new Vector2(346f, 31f);
            barRect.anchoredPosition = new Vector2(27f, -199f);
            barRect.SetAsLastSibling();
            foreach (UnityEngine.UI.Image barImage in bar.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                Undo.RecordObject(barImage, "Color existing XP MicroBar");
                barImage.raycastTarget = false;
                if (barImage.type == UnityEngine.UI.Image.Type.Filled)
                {
                    barImage.color = Hex("#22C55E");
                    MiningUiGradient green = barImage.GetComponent<MiningUiGradient>() ??
                                             Undo.AddComponent<MiningUiGradient>(barImage.gameObject);
                    green.SetColors(Hex("#86EFAC"), Hex("#14532D"));
                }
                else barImage.color = Hex("#140B05");
            }
            SerializedObject barFields = new(bar);
            SerializedProperty primary = barFields.FindProperty("simpleBar")?
                .FindPropertyRelative("_barPrimaryColor");
            if (primary != null)
            {
                primary.colorValue = Hex("#22C55E");
                barFields.ApplyModifiedProperties();
            }

            TextMeshProUGUI xpCounter = root.Find("Experience")?.GetComponent<TextMeshProUGUI>();
            if (xpCounter != null)
            {
                RectTransform xpRect = xpCounter.rectTransform;
                Undo.RecordObject(xpRect, "Keep authoritative XP text above MicroBar");
                xpRect.anchorMin = xpRect.anchorMax = new Vector2(0f, 1f);
                xpRect.pivot = new Vector2(0f, 1f);
                xpRect.sizeDelta = new Vector2(345f, 30f);
                xpRect.anchoredPosition = new Vector2(27f, -199f);
                xpRect.SetAsLastSibling();
                xpCounter.fontSize = 14f;
                xpCounter.fontStyle = FontStyles.Bold;
                xpCounter.color = Color.white;
                xpCounter.alignment = TextAlignmentOptions.Center;
                xpCounter.raycastTarget = false;
            }

            JuicyMinerProgress juicy = root.GetComponent<JuicyMinerProgress>() ??
                                       Undo.AddComponent<JuicyMinerProgress>(root.gameObject);
            SerializedObject fields = new(juicy);
            Set(fields, "progressionSystem", progression);
            Set(fields, "oreSpawner", spawner);
            Set(fields, "experienceBar", bar);
            Set(fields, "cardTransform", root);
            Set(fields, "titleText", title);
            Set(fields, "levelBadgeText", badge);
            Set(fields, "rankTitleText", rank);
            Set(fields, "powerLabelText", powerLabel);
            Set(fields, "powerValueText", powerValue);
            Set(fields, "rewardLabelText", rewardLabel);
            Set(fields, "rewardText", reward);
            Set(fields, "xpLabelText", xpLabel);
            Set(fields, "xpPercentText", xpPercent);
            fields.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log("Juicy Miner Progress built. XP remains owned by NpcProgressionHud; next ore comes from the real spawn table. Save your own scene.");
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
                Undo.RegisterCreatedObjectUndo(obj, "Build miner progress " + name);
                obj.transform.SetParent(parent, false);
            }
            else obj = existing.gameObject;
            RectTransform rect = obj.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Layout miner progress " + name);
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
            (rect.GetComponent<MiningUiGradient>() ??
             Undo.AddComponent<MiningUiGradient>(rect.gameObject)).SetColors(top, bottom);
        }

        private static void Round(RectTransform rect)
        {
            UnityEngine.UI.Image image = Image(rect);
            image.sprite = cardSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
        }

        private static void Circle(RectTransform rect)
        {
            UnityEngine.UI.Image image = Image(rect);
            image.sprite = medalSprite;
            image.type = UnityEngine.UI.Image.Type.Simple;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string value,
            float size, Color color, Vector2 dimensions, Vector2 pos)
        {
            RectTransform rect = Child(parent, name, dimensions, pos);
            TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>() ??
                                   Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.enableAutoSizing = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static void Outline(GameObject obj, Color color, Vector2 offset)
        {
            UnityEngine.UI.Outline border = obj.GetComponent<UnityEngine.UI.Outline>() ??
                                             Undo.AddComponent<UnityEngine.UI.Outline>(obj);
            border.effectColor = color;
            border.effectDistance = offset;
        }

        private static Sprite EnsureSprite(string fileName, bool circle)
        {
            string path = SpriteDirectory + "/" + fileName;
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;
            Directory.CreateDirectory(SpriteDirectory);
            Texture2D texture = new(64, 64, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[64 * 64];
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float u = x - 31.5f;
                    float v = y - 31.5f;
                    float distance = circle ? Mathf.Sqrt(u * u + v * v)
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

        private static Color Hex(string hex) =>
            ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
    }
}
#endif
