#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>One-shot authoring tool for the leather Quest panel; it does not own quest data.</summary>
    public static class MiningJuicyQuestSetupMenu
    {
        private const string SpriteDirectory = "Assets/Generated/MiningUI";
        private static Sprite roundedSprite;
        private static Sprite circleSprite;

        [MenuItem("Mining Simulator/UI/Build Juicy Quest Panel")]
        public static void Build()
        {
            MiningQuestPanel panel = Object.FindFirstObjectByType<MiningQuestPanel>(
                FindObjectsInactive.Include);
            if (panel == null)
            {
                Debug.LogWarning("Open the mining scene first: MiningQuestPanel was not found. No scene was changed.");
                return;
            }

            SerializedObject panelFields = new(panel);
            RectTransform panelRoot = panelFields.FindProperty("panelRoot")
                .objectReferenceValue as RectTransform;
            MiningQuestData data = panelFields.FindProperty("data")
                .objectReferenceValue as MiningQuestData;
            MiningQuestSystem system = panelFields.FindProperty("questSystem")
                .objectReferenceValue as MiningQuestSystem;
            SerializedProperty rows = panelFields.FindProperty("rows");
            RectTransform card = panelRoot != null
                ? panelRoot.Find("Quest Card") as RectTransform
                : null;
            if (panelRoot == null || card == null || data == null || system == null ||
                rows == null || rows.arraySize == 0)
            {
                Debug.LogWarning("The existing Quest panel/data/rows are incomplete. Run the normal Daily Weekly Quest setup first; no scene was changed.");
                return;
            }
            RectTransform existingHeader = card.Find("Header") as RectTransform;
            if (existingHeader == null ||
                panelFields.FindProperty("titleLabel").objectReferenceValue == null ||
                panelFields.FindProperty("resetTimerLabel").objectReferenceValue == null ||
                panelFields.FindProperty("closeButton").objectReferenceValue == null)
            {
                Debug.LogWarning("The existing Quest header/title/timer/close wiring is incomplete. No scene was changed.");
                return;
            }

            roundedSprite = EnsureSprite("QuestLeatherRounded.png", false);
            circleSprite = EnsureSprite("QuestBrassCircle.png", true);
            MiningUiData uiData = AssetDatabase.LoadAssetAtPath<MiningUiData>(
                "Assets/GameData/UI/MiningUiData.asset");

            StylePanelRoot(panelRoot);
            StyleCard(card);
            RectTransform header = StyleHeader(card, panelFields);
            StyleRows(rows, data, system, uiData);
            UnityEngine.UI.Button bottomClose = BuildBottomClose(card);
            StyleStatus(panelFields, card);

            JuicyQuestPanel presentation = card.GetComponent<JuicyQuestPanel>() ??
                                           Undo.AddComponent<JuicyQuestPanel>(card.gameObject);
            SerializedObject presentationFields = new(presentation);
            Set(presentationFields, "panelBody", card);
            Set(presentationFields, "questPanel", panel);
            Set(presentationFields, "bottomCloseButton", bottomClose);
            Set(presentationFields, "bottomCloseLabel",
                bottomClose.transform.Find("Label")?.GetComponent<TextMeshProUGUI>());
            presentationFields.ApplyModifiedProperties();

            header.SetAsLastSibling();
            bottomClose.transform.SetAsLastSibling();
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(card.gameObject.scene);
            Selection.activeGameObject = card.gameObject;
            EditorGUIUtility.PingObject(card.gameObject);
            Debug.Log("Juicy Quest panel built. Existing daily/weekly data, progress, claim rewards, reset timers, save data and localization remain authoritative. Edit the authored RectTransforms freely and save your scene.");
        }

        private static void StylePanelRoot(RectTransform panelRoot)
        {
            UnityEngine.UI.Image overlay = panelRoot.GetComponent<UnityEngine.UI.Image>() ??
                                           Undo.AddComponent<UnityEngine.UI.Image>(panelRoot.gameObject);
            Undo.RecordObject(overlay, "Style Quest overlay");
            overlay.sprite = null;
            overlay.type = UnityEngine.UI.Image.Type.Simple;
            overlay.color = new Color(0.02f, 0.015f, 0.012f, 0.82f);
            overlay.raycastTarget = true;
        }

        private static void StyleCard(RectTransform card)
        {
            Undo.RecordObject(card, "Style Quest leather card");
            card.sizeDelta = new Vector2(1040f, 680f);
            card.localScale = Vector3.one;
            Style(card, Hex("#8F4422"), Hex("#2D0F04"), roundedSprite);
            Border(card.gameObject, Hex("#210A03"), new Vector2(3f, -3f));
            DropShadow(card.gameObject, new Color(0f, 0f, 0f, 0.72f),
                new Vector2(0f, -10f));

            RectTransform seam = Child(card, "Stitched_Seam", new Vector2(1008f, 648f),
                Vector2.zero);
            UnityEngine.UI.Image seamImage = EnsureImage(seam);
            seamImage.sprite = roundedSprite;
            seamImage.type = UnityEngine.UI.Image.Type.Sliced;
            seamImage.color = new Color(1f, 1f, 1f, 0.001f);
            Border(seam.gameObject, Hex("#F3D4A6"), new Vector2(2f, -2f));
            seam.SetAsFirstSibling();

            AddRivet(card, "Rivet_TL", new Vector2(-497f, 317f));
            AddRivet(card, "Rivet_TR", new Vector2(497f, 317f));
            AddRivet(card, "Rivet_BL", new Vector2(-497f, -317f));
            AddRivet(card, "Rivet_BR", new Vector2(497f, -317f));
        }

        private static RectTransform StyleHeader(RectTransform card, SerializedObject panelFields)
        {
            RectTransform header = card.Find("Header") as RectTransform;
            TextMeshProUGUI title = panelFields.FindProperty("titleLabel")
                .objectReferenceValue as TextMeshProUGUI;
            TextMeshProUGUI timer = panelFields.FindProperty("resetTimerLabel")
                .objectReferenceValue as TextMeshProUGUI;
            UnityEngine.UI.Button close = panelFields.FindProperty("closeButton")
                .objectReferenceValue as UnityEngine.UI.Button;
            if (header == null || title == null || timer == null || close == null)
            {
                throw new System.InvalidOperationException(
                    "The existing Quest Header, title, timer or close button is missing.");
            }

            Place(header, new Vector2(960f, 72f), new Vector2(0f, 286f));
            Style(header, Hex("#AA5A2E"), Hex("#461C09"), roundedSprite);
            Border(header.gameObject, Hex("#2B1003"), new Vector2(2f, -2f));

            RectTransform medal = Child(header, "Quest_Medal", new Vector2(56f, 56f),
                new Vector2(-442f, 0f));
            Style(medal, Hex("#FFF0A8"), Hex("#A16207"), circleSprite);
            Border(medal.gameObject, Hex("#321A07"), new Vector2(1f, -1f));
            TextMeshProUGUI emblem = Text(medal, "Emblem", "Q", 27f, Hex("#4A1D08"),
                new Vector2(50f, 45f), Vector2.zero, TextAlignmentOptions.Center);
            emblem.fontStyle = FontStyles.Bold;

            if (title.transform.parent != header)
            {
                Undo.SetTransformParent(title.transform, header, "Move Quest title into header");
            }
            Place(title.rectTransform, new Vector2(420f, 50f), new Vector2(-155f, 0f));
            title.fontSize = 28f;
            title.enableAutoSizing = true;
            title.fontSizeMin = 18f;
            title.fontSizeMax = 28f;
            title.color = Hex("#FFF0CA");
            title.alignment = TextAlignmentOptions.Center;

            if (timer.transform.parent != header)
            {
                Undo.SetTransformParent(timer.transform, header, "Move Quest timer into header");
            }
            Place(timer.rectTransform, new Vector2(310f, 38f), new Vector2(220f, 0f));
            timer.fontSize = 14f;
            timer.enableAutoSizing = true;
            timer.fontSizeMin = 10f;
            timer.fontSizeMax = 14f;
            timer.color = Hex("#A5F3FC");
            timer.alignment = TextAlignmentOptions.Center;

            RectTransform closeRect = close.transform as RectTransform;
            Place(closeRect, new Vector2(54f, 54f), new Vector2(448f, 0f));
            UnityEngine.UI.Image closeImage = close.GetComponent<UnityEngine.UI.Image>() ??
                                              Undo.AddComponent<UnityEngine.UI.Image>(close.gameObject);
            closeImage.sprite = roundedSprite;
            closeImage.type = UnityEngine.UI.Image.Type.Sliced;
            closeImage.color = Color.white;
            closeImage.raycastTarget = true;
            close.targetGraphic = closeImage;
            close.transition = UnityEngine.UI.Selectable.Transition.None;
            (close.GetComponent<MiningUiGradient>() ??
             Undo.AddComponent<MiningUiGradient>(close.gameObject)).SetColors(
                Hex("#B91C1C"), Hex("#580D0D"));
            Border(close.gameObject, Hex("#FFDF78"), new Vector2(2f, -2f));
            TextMeshProUGUI closeLabel = close.GetComponentInChildren<TextMeshProUGUI>(true);
            if (closeLabel != null)
            {
                closeLabel.text = "X";
                closeLabel.fontSize = 23f;
                closeLabel.color = Color.white;
                closeLabel.alignment = TextAlignmentOptions.Center;
            }
            return header;
        }

        private static void StyleRows(SerializedProperty rows, MiningQuestData data,
            MiningQuestSystem system, MiningUiData uiData)
        {
            for (int index = 0; index < rows.arraySize; index++)
            {
                SerializedProperty rowFields = rows.GetArrayElementAtIndex(index);
                string questId = rowFields.FindPropertyRelative("questId").stringValue;
                GameObject rowObject = rowFields.FindPropertyRelative("root")
                    .objectReferenceValue as GameObject;
                MiningQuestDefinition definition = data.GetDefinition(questId);
                if (rowObject == null || definition == null)
                {
                    continue;
                }

                RectTransform row = rowObject.transform as RectTransform;
                Style(row, Hex("#8C4721"), Hex("#321306"), roundedSprite);
                Border(rowObject, Hex("#381507"), new Vector2(2f, -2f));
                DropShadow(rowObject, new Color(0f, 0f, 0f, 0.58f), new Vector2(0f, -4f));

                RectTransform seam = Child(row, "Stitched_Seam", new Vector2(922f, 92f),
                    Vector2.zero);
                UnityEngine.UI.Image seamImage = EnsureImage(seam);
                seamImage.sprite = roundedSprite;
                seamImage.type = UnityEngine.UI.Image.Type.Sliced;
                seamImage.color = new Color(1f, 1f, 1f, 0.001f);
                Border(seam.gameObject, Hex("#F5D6AA"), new Vector2(1f, -1f));
                seam.SetAsFirstSibling();

                TextMeshProUGUI period = rowFields.FindPropertyRelative("periodLabel")
                    .objectReferenceValue as TextMeshProUGUI;
                RectTransform badge = period != null ? period.transform.parent as RectTransform : null;
                if (badge != null)
                {
                    Place(badge, new Vector2(145f, 30f), new Vector2(-380f, 27f));
                    bool daily = definition.Period == MiningQuestPeriod.Daily;
                    Style(badge, daily ? Hex("#5A2C10") : Hex("#163047"),
                        daily ? Hex("#241006") : Hex("#0B1824"), roundedSprite);
                    Border(badge.gameObject, daily ? Hex("#B45309") : Hex("#0891B2"),
                        new Vector2(1f, -1f));
                    Place(period.rectTransform, new Vector2(135f, 25f), Vector2.zero);
                    period.fontSize = 12f;
                    period.color = daily ? Hex("#FBBF24") : Hex("#67E8F9");
                    period.alignment = TextAlignmentOptions.Center;
                }

                TextMeshProUGUI questName = rowFields.FindPropertyRelative("nameLabel")
                    .objectReferenceValue as TextMeshProUGUI;
                if (questName != null)
                {
                    Place(questName.rectTransform, new Vector2(500f, 31f),
                        new Vector2(-53f, 27f));
                    questName.fontSize = 17f;
                    questName.enableAutoSizing = true;
                    questName.fontSizeMin = 12f;
                    questName.fontSizeMax = 17f;
                    questName.color = Hex("#FFF7ED");
                    questName.alignment = TextAlignmentOptions.Left;
                }

                UnityEngine.UI.Image progressFill = rowFields.FindPropertyRelative("progressFill")
                    .objectReferenceValue as UnityEngine.UI.Image;
                RectTransform progressBackground = progressFill != null
                    ? progressFill.transform.parent as RectTransform
                    : null;
                TextMeshProUGUI progress = rowFields.FindPropertyRelative("progressLabel")
                    .objectReferenceValue as TextMeshProUGUI;
                if (progressBackground != null)
                {
                    Place(progressBackground, new Vector2(650f, 31f), new Vector2(-84f, -26f));
                    Style(progressBackground, Hex("#241006"), Hex("#120703"), roundedSprite);
                    Border(progressBackground.gameObject, Hex("#482410"), new Vector2(2f, -2f));
                }
                if (progressFill != null)
                {
                    progressFill.sprite = roundedSprite;
                    progressFill.type = UnityEngine.UI.Image.Type.Filled;
                    progressFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                    progressFill.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
                    progressFill.color = Color.white;
                    progressFill.raycastTarget = false;
                    (progressFill.GetComponent<MiningUiGradient>() ??
                     Undo.AddComponent<MiningUiGradient>(progressFill.gameObject)).SetColors(
                        Hex("#86EFAC"), Hex("#15803D"));
                    RectTransform fillRect = progressFill.rectTransform;
                    fillRect.anchorMin = Vector2.zero;
                    fillRect.anchorMax = Vector2.one;
                    fillRect.offsetMin = new Vector2(3f, 3f);
                    fillRect.offsetMax = new Vector2(-3f, -3f);
                }
                if (progress != null)
                {
                    Place(progress.rectTransform, new Vector2(625f, 27f), Vector2.zero);
                    progress.fontSize = 13f;
                    progress.color = Color.white;
                    progress.alignment = TextAlignmentOptions.Center;
                    progress.transform.SetAsLastSibling();
                }

                UnityEngine.UI.Button claim = rowFields.FindPropertyRelative("claimButton")
                    .objectReferenceValue as UnityEngine.UI.Button;
                TextMeshProUGUI claimLabel = rowFields.FindPropertyRelative("claimLabel")
                    .objectReferenceValue as TextMeshProUGUI;
                TextMeshProUGUI reward = rowFields.FindPropertyRelative("rewardLabel")
                    .objectReferenceValue as TextMeshProUGUI;
                RectTransform claimBody = StyleClaimButton(row, claim, claimLabel, reward, uiData);

                if (claim == null)
                {
                    continue;
                }
                JuicyQuestItem presentation = claim.GetComponent<JuicyQuestItem>() ??
                                                Undo.AddComponent<JuicyQuestItem>(claim.gameObject);
                SerializedObject fields = new(presentation);
                fields.FindProperty("questId").stringValue = questId;
                Set(fields, "data", data);
                Set(fields, "questSystem", system);
                Set(fields, "rowBody", row);
                Set(fields, "progressFill", progressFill);
                Set(fields, "claimButton", claim);
                Set(fields, "claimButtonBody", claimBody);
                Set(fields, "claimButtonBackground",
                    claimBody != null ? claimBody.GetComponent<UnityEngine.UI.Image>() : null);
                Set(fields, "claimLabel", claimLabel);
                fields.ApplyModifiedProperties();
            }
        }

        private static RectTransform StyleClaimButton(RectTransform row,
            UnityEngine.UI.Button claim, TextMeshProUGUI claimLabel, TextMeshProUGUI reward,
            MiningUiData uiData)
        {
            if (claim == null)
            {
                return null;
            }
            RectTransform claimRoot = claim.transform as RectTransform;
            Place(claimRoot, new Vector2(180f, 72f), new Vector2(370f, 0f));
            UnityEngine.UI.Image rootImage = claim.GetComponent<UnityEngine.UI.Image>() ??
                                             Undo.AddComponent<UnityEngine.UI.Image>(claim.gameObject);
            rootImage.sprite = null;
            rootImage.type = UnityEngine.UI.Image.Type.Simple;
            rootImage.color = new Color(1f, 1f, 1f, 0.001f);
            rootImage.raycastTarget = true;
            claim.targetGraphic = rootImage;
            claim.transition = UnityEngine.UI.Selectable.Transition.None;

            RectTransform shadow = Child(claimRoot, "Base_Shadow", new Vector2(180f, 68f),
                new Vector2(0f, -6f));
            Style(shadow, Hex("#140602"), Hex("#140602"), roundedSprite);
            shadow.SetAsFirstSibling();
            RectTransform body = Child(claimRoot, "Button_Body", new Vector2(180f, 68f),
                Vector2.zero);
            Style(body, Hex("#4A3B33"), Hex("#211914"), roundedSprite);
            Border(body.gameObject, Hex("#6B4A35"), new Vector2(2f, -2f));

            RectTransform coin = Child(body, "Coin_Icon", new Vector2(22f, 22f),
                new Vector2(-68f, -16f));
            UnityEngine.UI.Image coinImage = EnsureImage(coin);
            coinImage.sprite = uiData != null ? uiData.MoneyIconSprite : null;
            coinImage.color = Color.white;
            coinImage.preserveAspect = true;

            if (claimLabel != null)
            {
                if (claimLabel.transform.parent != body)
                {
                    Undo.SetTransformParent(claimLabel.transform, body,
                        "Move Quest claim label into button body");
                }
                Place(claimLabel.rectTransform, new Vector2(164f, 28f),
                    new Vector2(0f, 15f));
                claimLabel.fontSize = 14f;
                claimLabel.enableAutoSizing = true;
                claimLabel.fontSizeMin = 10f;
                claimLabel.fontSizeMax = 14f;
                claimLabel.color = Hex("#E7E5E4");
                claimLabel.alignment = TextAlignmentOptions.Center;
            }
            if (reward != null)
            {
                if (reward.transform.parent != body)
                {
                    Undo.SetTransformParent(reward.transform, body,
                        "Move Quest reward into button body");
                }
                Place(reward.rectTransform, new Vector2(137f, 28f),
                    new Vector2(12f, -16f));
                reward.fontSize = 11f;
                reward.enableAutoSizing = true;
                reward.fontSizeMin = 8f;
                reward.fontSizeMax = 11f;
                reward.color = Hex("#FDE68A");
                reward.alignment = TextAlignmentOptions.Center;
            }
            body.SetAsLastSibling();
            return body;
        }

        private static UnityEngine.UI.Button BuildBottomClose(RectTransform card)
        {
            RectTransform root = Child(card, "Bottom_Close_Button", new Vector2(280f, 48f),
                new Vector2(0f, -302f));
            UnityEngine.UI.Image image = Style(root, Hex("#A85526"), Hex("#4E220C"),
                roundedSprite);
            image.raycastTarget = true;
            Border(root.gameObject, Hex("#FFDF78"), new Vector2(2f, -2f));
            UnityEngine.UI.Button button = root.GetComponent<UnityEngine.UI.Button>() ??
                                           Undo.AddComponent<UnityEngine.UI.Button>(root.gameObject);
            button.targetGraphic = image;
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            TextMeshProUGUI label = Text(root, "Label", "CLOSE QUEST PANEL", 14f,
                Color.white, new Vector2(260f, 38f), Vector2.zero,
                TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            return button;
        }

        private static void StyleStatus(SerializedObject panelFields, RectTransform card)
        {
            TextMeshProUGUI status = panelFields.FindProperty("statusLabel")
                .objectReferenceValue as TextMeshProUGUI;
            if (status == null)
            {
                return;
            }
            if (status.transform.parent != card)
            {
                Undo.SetTransformParent(status.transform, card, "Move Quest status");
            }
            Place(status.rectTransform, new Vector2(620f, 34f), new Vector2(0f, 222f));
            status.fontSize = 17f;
            status.color = Hex("#FDE68A");
            status.alignment = TextAlignmentOptions.Center;
            status.transform.SetAsLastSibling();
        }

        private static void AddRivet(Transform parent, string name, Vector2 position)
        {
            RectTransform rivet = Child(parent, name, new Vector2(12f, 12f), position);
            Style(rivet, Hex("#B6B3B1"), Hex("#292524"), circleSprite);
        }

        private static void Set(SerializedObject fields, string name, Object value)
        {
            SerializedProperty property = fields.FindProperty(name);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static RectTransform Child(Transform parent, string name, Vector2 size,
            Vector2 position)
        {
            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing == null)
            {
                obj = new GameObject(name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(obj, "Build Quest " + name);
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
            Undo.RecordObject(rect, "Layout Quest " + rect.name);
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
            foreach (UnityEngine.UI.Shadow candidate in
                     obj.GetComponents<UnityEngine.UI.Shadow>())
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
