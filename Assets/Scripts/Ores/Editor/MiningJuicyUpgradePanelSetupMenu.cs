#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Restyles the existing ten wired upgrade buttons without replacing purchases or scenes.</summary>
    public static class MiningJuicyUpgradePanelSetupMenu
    {
        private const string SpritePath = "Assets/Generated/MiningUI/UpgradeLeatherRounded.png";
        private static Sprite roundedSprite;

        private static readonly string[] CardNames =
        {
            "Money Reward Upgrade", "Rare Ore Upgrade", "Ore Damage Upgrade",
            "Ore Spawn Speed Upgrade", "NPC Move Speed Upgrade", "NPC Capacity Upgrade",
            "Lucky Block Reward Upgrade", "Lucky Block Drop Chance Upgrade",
            "NPC Experience Upgrade", "Item Drop Chance Upgrade"
        };

        [MenuItem("Mining Simulator/UI/Build Leather Upgrade Panel")]
        public static void Build()
        {
            MiningUpgradePanel controller = Object.FindFirstObjectByType<MiningUpgradePanel>(
                FindObjectsInactive.Include);
            if (controller == null)
            {
                Debug.LogWarning("Open your scene with the existing MiningUpgradePanel first; no scene was changed.");
                return;
            }
            SerializedObject controllerFields = new(controller);
            GameObject panelObject = controllerFields.FindProperty("upgradePanel")?.objectReferenceValue as GameObject;
            MiningUpgradeSystem system = controllerFields.FindProperty("upgradeSystem")?.objectReferenceValue
                as MiningUpgradeSystem;
            PlayerWallet wallet = controllerFields.FindProperty("wallet")?.objectReferenceValue as PlayerWallet;
            RectTransform root = panelObject != null ? panelObject.transform as RectTransform : null;
            if (root == null || system == null || wallet == null || system.UpgradeData == null ||
                root.Find("Header") == null || root.Find("Close")?.GetComponent<UnityEngine.UI.Button>() == null ||
                root.Find("Back")?.GetComponent<UnityEngine.UI.Button>() == null)
            {
                Debug.LogWarning("Upgrade panel, its controller, ten purchases, or wallet is not configured; no scene was changed.");
                return;
            }
            for (int index = 0; index < CardNames.Length; index++)
            {
                Transform card = root.Find(CardNames[index]);
                if (card == null || card.GetComponent<UnityEngine.UI.Button>() == null ||
                    card.Find("Label")?.GetComponent<TextMeshProUGUI>() == null)
                {
                    Debug.LogWarning("Missing authored upgrade card: " + CardNames[index] + "; no scene was changed.");
                    return;
                }
            }
            roundedSprite = EnsureSprite();
            if (roundedSprite == null)
            {
                Debug.LogWarning("Rounded leather sprite could not be imported; no scene was changed.");
                return;
            }

            // Preserve the existing button roots and serialized MiningUpgradePanel references.
            Layout(root, new Vector2(1000f, 600f), Vector2.zero);
            UnityEngine.UI.Image leather = Surface(root, Hex("#3D1B0B"), Hex("#100401"));
            leather.raycastTarget = true;
            Trim(root);
            Outline(root.gameObject, Hex("#4A1F0A"), new Vector2(4f, -4f));
            Shadow(root.gameObject, Hex("#100401"), new Vector2(0f, -12f));

            RectTransform header = root.Find("Header") as RectTransform;
            Layout(header, new Vector2(840f, 58f), new Vector2(-30f, 236f));
            Surface(header, Hex("#96522A"), Hex("#5A280E"));
            Trim(header);
            Outline(header.gameObject, Hex("#2B1003"), new Vector2(2f, -2f));
            TextMeshProUGUI oldTitle = header.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (oldTitle != null && oldTitle.gameObject.activeSelf)
            {
                Undo.RecordObject(oldTitle.gameObject, "Keep controller title but show new leather heading");
                oldTitle.gameObject.SetActive(false);
            }
            RectTransform emblem = Child(header, "Header_Brass", new Vector2(42f, 42f),
                new Vector2(-384f, 0f));
            Surface(emblem, Hex("#FFE58F"), Hex("#946317"));
            (emblem.GetComponent<JuicyButtonTrim>() ?? Undo.AddComponent<JuicyButtonTrim>(emblem.gameObject))
                .SetMedalRivets(true);
            Text(emblem, "Pickaxe", "⛏", 20f, Hex("#371807"),
                new Vector2(32f, 32f), Vector2.zero, TextAlignmentOptions.Center);
            TextMeshProUGUI title = Text(header, "Leather_Title", "MINING UPGRADES", 23f,
                Hex("#FEF3C7"), new Vector2(500f, 36f), new Vector2(-90f, 0f),
                TextAlignmentOptions.Left);
            RectTransform walletWell = Child(header, "Wallet_Well", new Vector2(245f, 39f),
                new Vector2(284f, 0f));
            Surface(walletWell, Hex("#230F06"), Hex("#1A0903"));
            Outline(walletWell.gameObject, Hex("#683416"), new Vector2(1f, -1f));
            Text(walletWell, "Coin", "●", 17f, Hex("#FFE285"),
                new Vector2(22f, 25f), new Vector2(-105f, 0f), TextAlignmentOptions.Center);
            TextMeshProUGUI money = Text(walletWell, "Money", "0", 16f,
                Hex("#FEF08A"), new Vector2(104f, 28f), new Vector2(-36f, 0f),
                TextAlignmentOptions.Left);
            Text(walletWell, "Gem_Symbol", "◆", 15f, Hex("#67E8F9"),
                new Vector2(22f, 25f), new Vector2(40f, 0f), TextAlignmentOptions.Center);
            TextMeshProUGUI gems = Text(walletWell, "Gems", "0", 13f,
                Hex("#67E8F9"), new Vector2(54f, 27f), new Vector2(89f, 0f),
                TextAlignmentOptions.Left);

            UnityEngine.UI.Button close = root.Find("Close").GetComponent<UnityEngine.UI.Button>();
            Layout(close.transform as RectTransform, new Vector2(54f, 54f), new Vector2(426f, 236f));
            Surface(close.transform as RectTransform, Hex("#A92A20"), Hex("#580D0D"))
                .raycastTarget = true;
            Outline(close.gameObject, Hex("#FFDF78"), new Vector2(2f, -2f));
            TextMeshProUGUI closeX = close.GetComponentInChildren<TextMeshProUGUI>(true);
            if (closeX != null)
            {
                Undo.RecordObject(closeX, "Style upgrade close X");
                closeX.text = "✕";
                closeX.fontSize = 25f;
                closeX.color = Color.white;
            }

            for (int index = 0; index < CardNames.Length; index++)
            {
                RectTransform card = root.Find(CardNames[index]) as RectTransform;
                int column = index % 2;
                int row = index / 2;
                Layout(card, new Vector2(440f, 74f),
                    new Vector2(column == 0 ? -227f : 227f, 151f - 82f * row));
                Surface(card, Hex("#87431E"), Hex("#311306")).raycastTarget = true;
                Trim(card);
                Outline(card.gameObject, Hex("#361405"), new Vector2(2f, -2f));
                Shadow(card.gameObject, new Color(0f, 0f, 0f, 0.7f), new Vector2(0f, -4f));

                Transform legacy = card.Find("Label");
                if (legacy != null && legacy.gameObject.activeSelf)
                {
                    Undo.RecordObject(legacy.gameObject, "Keep original bound upgrade label hidden");
                    legacy.gameObject.SetActive(false);
                }
                RectTransform medal = Child(card, "Brass_Medal", new Vector2(54f, 54f),
                    new Vector2(-179f, 0f));
                Surface(medal, Hex("#FFE58F"), Hex("#59390A"));
                (medal.GetComponent<JuicyButtonTrim>() ?? Undo.AddComponent<JuicyButtonTrim>(medal.gameObject))
                    .SetMedalRivets(true);
                RectTransform inset = Child(medal, "Icon_Well", new Vector2(44f, 44f), Vector2.zero);
                Surface(inset, Hex("#291307"), Hex("#140803"));
                Transform icon = card.Find("Icon");
                if (icon is RectTransform iconRect)
                {
                    Layout(iconRect, new Vector2(34f, 34f), new Vector2(-179f, 0f));
                    icon.SetAsLastSibling(); // Existing sprite/fallback stays on top of the brass medal.
                }
                TextMeshProUGUI itemTitle = Text(card, "Leather_Title", "Upgrade", 14f,
                    Hex("#FEF3C7"), new Vector2(214f, 26f), new Vector2(-53f, 13f),
                    TextAlignmentOptions.Left);
                TextMeshProUGUI itemDetail = Text(card, "Leather_Detail", string.Empty, 11f,
                    Hex("#86EFAC"), new Vector2(214f, 23f), new Vector2(-53f, -12f),
                    TextAlignmentOptions.Left);
                RectTransform plaque = Child(card, "Price_Plaque", new Vector2(126f, 47f),
                    new Vector2(148f, 0f));
                Surface(plaque, Hex("#241108"), Hex("#1D0B04"));
                Outline(plaque.gameObject, Hex("#D97706"), new Vector2(2f, -2f));
                TextMeshProUGUI caption = Text(plaque, "Buy_Caption", "UPGRADE", 9f,
                    Hex("#FCD34D"), new Vector2(106f, 16f), new Vector2(0f, 12f),
                    TextAlignmentOptions.Center);
                TextMeshProUGUI price = Text(plaque, "Cost", "0", 15f,
                    Hex("#FEF08A"), new Vector2(112f, 23f), new Vector2(0f, -7f),
                    TextAlignmentOptions.Center);
                JuicyUpgradeItem item = card.GetComponent<JuicyUpgradeItem>() ??
                                        Undo.AddComponent<JuicyUpgradeItem>(card.gameObject);
                SerializedObject itemFields = new(item);
                itemFields.FindProperty("upgradeType").enumValueIndex = index;
                Set(itemFields, "upgradeSystem", system);
                Set(itemFields, "wallet", wallet);
                Set(itemFields, "titleText", itemTitle);
                Set(itemFields, "detailText", itemDetail);
                Set(itemFields, "priceText", price);
                Set(itemFields, "buyCaptionText", caption);
                itemFields.ApplyModifiedProperties();
            }

            UnityEngine.UI.Button back = root.Find("Back").GetComponent<UnityEngine.UI.Button>();
            Layout(back.transform as RectTransform, new Vector2(258f, 44f), new Vector2(0f, -248f));
            Surface(back.transform as RectTransform, Hex("#8F471F"), Hex("#4E220C"))
                .raycastTarget = true;
            Outline(back.gameObject, Hex("#FFDF78"), new Vector2(2f, -2f));
            TextMeshProUGUI backLabel = back.GetComponentInChildren<TextMeshProUGUI>(true);
            if (backLabel != null)
            {
                Undo.RecordObject(backLabel, "Style leather return button");
                backLabel.fontSize = 16f;
                backLabel.color = Color.white;
            }
            for (int corner = 0; corner < 4; corner++)
            {
                float x = (corner & 1) == 0 ? -458f : 458f;
                float y = (corner & 2) == 0 ? 262f : -262f;
                RectTransform rivet = Child(root, "Iron_Rivet_" + corner, new Vector2(16f, 16f),
                    new Vector2(x, y));
                Surface(rivet, Hex("#A8A7AD"), Hex("#1B1A20"));
            }
            // Existing buttons are still on top, with their original controller listeners.
            close.transform.SetAsLastSibling();
            back.transform.SetAsLastSibling();

            JuicyUpgradePanel juicy = root.GetComponent<JuicyUpgradePanel>() ??
                                      Undo.AddComponent<JuicyUpgradePanel>(root.gameObject);
            SerializedObject panelFields = new(juicy);
            Set(panelFields, "panelBody", root);
            Set(panelFields, "wallet", wallet);
            Set(panelFields, "titleText", title);
            Set(panelFields, "moneyText", money);
            Set(panelFields, "gemsText", gems);
            Set(panelFields, "closeText", backLabel);
            panelFields.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Debug.Log("Leather Upgrade Panel built. Purchases, audio, wallet and close navigation remain owned by MiningUpgradePanel; save your own scene.");
        }

        private static void Set(SerializedObject fields, string property, Object value) =>
            fields.FindProperty(property).objectReferenceValue = value;

        private static RectTransform Child(Transform parent, string name, Vector2 size, Vector2 position)
        {
            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing == null)
            {
                obj = new GameObject(name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(obj, "Create leather upgrade " + name);
                obj.transform.SetParent(parent, false);
            }
            else obj = existing.gameObject;
            RectTransform rect = obj.GetComponent<RectTransform>();
            Layout(rect, size, position);
            return rect;
        }

        private static void Layout(RectTransform rect, Vector2 size, Vector2 position)
        {
            Undo.RecordObject(rect, "Layout leather upgrade element");
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static UnityEngine.UI.Image Surface(RectTransform rect, Color top, Color bottom)
        {
            UnityEngine.UI.Image image = rect.GetComponent<UnityEngine.UI.Image>() ??
                                         Undo.AddComponent<UnityEngine.UI.Image>(rect.gameObject);
            Undo.RecordObject(image, "Style leather surface");
            image.sprite = roundedSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;
            MiningUiGradient gradient = rect.GetComponent<MiningUiGradient>() ??
                                        Undo.AddComponent<MiningUiGradient>(rect.gameObject);
            gradient.SetColors(top, bottom);
            return image;
        }

        private static void Trim(RectTransform rect)
        {
            (rect.GetComponent<JuicyButtonTrim>() ?? Undo.AddComponent<JuicyButtonTrim>(rect.gameObject))
                .SetMedalRivets(false);
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string value,
            float size, Color color, Vector2 dimensions, Vector2 position,
            TextAlignmentOptions alignment)
        {
            RectTransform rect = Child(parent, name, dimensions, position);
            TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>() ??
                                   Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
            Undo.RecordObject(text, "Style leather upgrade label");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static void Outline(GameObject obj, Color color, Vector2 offset)
        {
            UnityEngine.UI.Outline border = obj.GetComponent<UnityEngine.UI.Outline>() ??
                                             Undo.AddComponent<UnityEngine.UI.Outline>(obj);
            Undo.RecordObject(border, "Outline leather upgrade");
            border.effectColor = color;
            border.effectDistance = offset;
        }

        private static void Shadow(GameObject obj, Color color, Vector2 offset)
        {
            UnityEngine.UI.Shadow effect = null;
            foreach (UnityEngine.UI.Shadow candidate in obj.GetComponents<UnityEngine.UI.Shadow>())
            {
                if (candidate.GetType() == typeof(UnityEngine.UI.Shadow))
                {
                    effect = candidate;
                    break;
                }
            }
            effect ??= Undo.AddComponent<UnityEngine.UI.Shadow>(obj);
            Undo.RecordObject(effect, "Shadow leather upgrade");
            effect.effectColor = color;
            effect.effectDistance = offset;
        }

        private static Sprite EnsureSprite()
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (existing != null) return existing;
            Directory.CreateDirectory("Assets/Generated/MiningUI");
            Texture2D texture = new(64, 64, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[64 * 64];
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Abs(x - 31.5f) - 11.5f);
                    float dy = Mathf.Max(0f, Mathf.Abs(y - 31.5f) - 11.5f);
                    float alpha = Mathf.Clamp01(20.5f - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * 64 + x] = new Color32(255, 255, 255,
                        (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SpritePath);
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(20f, 20f, 20f, 20f);
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        private static Color Hex(string html) =>
            ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
    }
}
#endif
