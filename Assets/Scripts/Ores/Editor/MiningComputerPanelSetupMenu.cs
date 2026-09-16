#if UNITY_EDITOR
using System.IO;
using MiningSimulator.Ores;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Editor
{
    /// <summary>Restyles the existing Coin Computer panel without replacing its game logic.</summary>
    public static class MiningComputerPanelSetupMenu
    {
        private const string MenuPath = "Mining Simulator/UI/Build Juicy Coin Computer Panel";
        private const string LegacyMenuPath =
            "Mining Simulator/Setup/Create or Update Computer Info Panel";
        private const string SpriteDirectory = "Assets/Generated/MiningUI";
        private const string UiDataPath = "Assets/GameData/UI/MiningUiData.asset";
        private static Sprite roundedSprite;

        [MenuItem(MenuPath)]
        public static void CreateOrUpdate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Exit Play Mode",
                    "Stop Play Mode before authoring the Coin Computer panel.", "OK");
                return;
            }

            MiningComputerPanel controller = Object.FindFirstObjectByType<MiningComputerPanel>(
                FindObjectsInactive.Include);
            if (controller == null)
            {
                Debug.LogWarning("Open the mining scene first: Computer Info Panel was not " +
                                 "found. No scene was changed.");
                return;
            }

            SerializedObject fields = new(controller);
            RectTransform panel = Reference<RectTransform>(fields, "panelRoot") ??
                                  controller.transform as RectTransform;
            UnityEngine.UI.Button closeButton = Reference<UnityEngine.UI.Button>(fields,
                "closeButton");
            UnityEngine.UI.Button upgradeButton = Reference<UnityEngine.UI.Button>(fields,
                "upgradeButton");
            TextMeshProUGUI title = Reference<TextMeshProUGUI>(fields, "titleLabel");
            TextMeshProUGUI level = Reference<TextMeshProUGUI>(fields, "levelLabel");
            TextMeshProUGUI income = Reference<TextMeshProUGUI>(fields, "incomeLabel");
            TextMeshProUGUI nextIncome = Reference<TextMeshProUGUI>(fields, "nextIncomeLabel");
            TextMeshProUGUI cost = Reference<TextMeshProUGUI>(fields, "costLabel");
            TextMeshProUGUI status = Reference<TextMeshProUGUI>(fields, "statusLabel");
            TextMeshProUGUI upgradeLabel = Reference<TextMeshProUGUI>(fields,
                "upgradeButtonLabel");

            if (panel == null || closeButton == null || upgradeButton == null ||
                title == null || level == null || income == null || nextIncome == null ||
                cost == null || status == null || upgradeLabel == null)
            {
                Debug.LogWarning("The existing Coin Computer panel wiring is incomplete. " +
                                 "Restore the normal panel references first; no scene was changed.",
                    controller);
                return;
            }

            roundedSprite = EnsureRoundedSprite();
            MiningUiData uiData = AssetDatabase.LoadAssetAtPath<MiningUiData>(UiDataPath);
            Undo.RecordObject(panel, "Restyle Coin Computer Panel");

            StyleSurface(panel, Hex("#6B3518"), Hex("#2E1005"), true);
            EnsureOutline(panel.gameObject, Hex("#C8872A"), new Vector2(3f, -3f));
            EnsureShadow(panel.gameObject, new Color(0f, 0f, 0f, 0.82f),
                new Vector2(0f, -10f));
            JuicyButtonTrim panelTrim = panel.GetComponent<JuicyButtonTrim>() ??
                                         Undo.AddComponent<JuicyButtonTrim>(panel.gameObject);
            panelTrim.SetMedalRivets(false);

            RectTransform header = EnsureRect(panel, "Header", typeof(UnityEngine.UI.Image));
            ConfigureTopStretch(header, 80f);
            StyleSurface(header, Hex("#9A6225"), Hex("#4A2800"), false);
            EnsureOutline(header.gameObject, Hex("#2A1200"), new Vector2(0f, -3f));

            RectTransform emblem = EnsureRect(header, "Computer_Emblem",
                typeof(UnityEngine.UI.Image));
            ConfigureCentered(emblem, new Vector2(-282f, -1f), new Vector2(54f, 54f));
            StyleSurface(emblem, Hex("#D4A843"), Hex("#60400E"), false);
            EnsureOutline(emblem.gameObject, Hex("#FFE28A"), new Vector2(2f, -2f));
            JuicyButtonTrim emblemTrim = emblem.GetComponent<JuicyButtonTrim>() ??
                                          Undo.AddComponent<JuicyButtonTrim>(emblem.gameObject);
            emblemTrim.SetMedalRivets(true);
            TextMeshProUGUI emblemText = Text(emblem, "Letters", "CC", 18f,
                Hex("#FFF0C0"), new Vector2(50f, 46f), Vector2.zero,
                TextAlignmentOptions.Center);
            emblemText.fontStyle = FontStyles.Bold;

            ConfigureCentered(title.rectTransform, new Vector2(-10f, -1f),
                new Vector2(470f, 54f));
            StyleText(title, 28f, FontStyles.Bold, TextAlignmentOptions.Center,
                Hex("#FFF0C0"));
            title.characterSpacing = 3f;
            title.transform.SetAsLastSibling();

            ConfigureButton(closeButton, new Vector2(-18f, -13f), new Vector2(52f, 52f),
                true, Hex("#A03010"), Hex("#5A1000"), Hex("#2A0800"), uiData);
            TextMeshProUGUI closeLabel = closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (closeLabel != null)
            {
                closeLabel.text = "X";
                StyleText(closeLabel, 23f, FontStyles.Bold, TextAlignmentOptions.Center,
                    Hex("#FFD5B8"));
            }

            RectTransform levelCard = Card(panel, "Level_Card", new Vector2(600f, 78f),
                new Vector2(0f, 136f), Hex("#281607"), Hex("#120A04"));
            ConfigureCentered(level.rectTransform, new Vector2(0f, 151f),
                new Vector2(552f, 31f));
            StyleText(level, 21f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
                Hex("#FFE8B0"));
            level.transform.SetAsLastSibling();

            RectTransform track = EnsureRect(levelCard, "Level_Progress_Track",
                typeof(UnityEngine.UI.Image));
            ConfigureCentered(track, new Vector2(0f, -21f), new Vector2(552f, 14f));
            StyleSurface(track, Hex("#211104"), Hex("#080402"), false);
            EnsureOutline(track.gameObject, Hex("#70461C"), new Vector2(1f, -1f));

            RectTransform fillRect = EnsureRect(track, "Fill", typeof(UnityEngine.UI.Image));
            Stretch(fillRect, new Vector2(2f, 2f), new Vector2(-2f, -2f));
            UnityEngine.UI.Image fill = fillRect.GetComponent<UnityEngine.UI.Image>();
            fill.sprite = roundedSprite;
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0.1f;
            fill.color = Color.white;
            fill.raycastTarget = false;
            SetGradient(fill.gameObject, Hex("#FFD060"), Hex("#C8870A"));

            BuildStatCard(panel, "Income_Card", income, 65f, Hex("#FFE8B0"));
            BuildStatCard(panel, "Next_Income_Card", nextIncome, 4f, Hex("#BFEFFF"));
            BuildStatCard(panel, "Upgrade_Cost_Card", cost, -57f, Hex("#FFD060"));

            RectTransform statusCard = Card(panel, "Status_Card", new Vector2(600f, 46f),
                new Vector2(0f, -116f), Hex("#501400"), Hex("#2E0900"));
            EnsureOutline(statusCard.gameObject, Hex("#9A3A18"), new Vector2(1f, -1f));
            ConfigureCentered(status.rectTransform, new Vector2(0f, -116f),
                new Vector2(552f, 36f));
            StyleText(status, 17f, FontStyles.Italic, TextAlignmentOptions.Center,
                Hex("#FFA070"));
            status.transform.SetAsLastSibling();

            ConfigureButton(upgradeButton, new Vector2(0f, -188f), new Vector2(500f, 68f),
                false, Hex("#D49316"), Hex("#6A4100"), Hex("#2A1A00"), uiData);
            StyleText(upgradeLabel, 23f, FontStyles.Bold, TextAlignmentOptions.Center,
                Hex("#FFF8D0"));
            upgradeLabel.characterSpacing = 2f;
            upgradeLabel.transform.SetAsLastSibling();

            RectTransform shimmerMask = EnsureRect(upgradeButton.transform, "Shimmer_Mask",
                typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Mask));
            Stretch(shimmerMask, Vector2.zero, Vector2.zero);
            UnityEngine.UI.Image maskImage = shimmerMask.GetComponent<UnityEngine.UI.Image>();
            maskImage.sprite = roundedSprite;
            maskImage.type = UnityEngine.UI.Image.Type.Sliced;
            maskImage.color = new Color(1f, 1f, 1f, 0.01f);
            maskImage.raycastTarget = false;
            shimmerMask.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            shimmerMask.SetAsFirstSibling();

            RectTransform shimmer = EnsureRect(shimmerMask, "Shimmer",
                typeof(UnityEngine.UI.Image));
            ConfigureCentered(shimmer, new Vector2(-260f, 0f), new Vector2(85f, 90f));
            shimmer.localRotation = Quaternion.Euler(0f, 0f, -18f);
            UnityEngine.UI.Image shimmerImage = shimmer.GetComponent<UnityEngine.UI.Image>();
            shimmerImage.sprite = roundedSprite;
            shimmerImage.type = UnityEngine.UI.Image.Type.Sliced;
            shimmerImage.color = new Color(1f, 0.94f, 0.65f, 0.24f);
            shimmerImage.raycastTarget = false;

            AddRivet(header, "Left_Rivet", new Vector2(-315f, 26f));
            AddRivet(header, "Right_Rivet", new Vector2(242f, 26f));

            CanvasGroup group = panel.GetComponent<CanvasGroup>() ??
                                Undo.AddComponent<CanvasGroup>(panel.gameObject);
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;

            JuicyCoinComputer presentation = panel.GetComponent<JuicyCoinComputer>() ??
                                              Undo.AddComponent<JuicyCoinComputer>(panel.gameObject);
            presentation.Configure(panel, group, emblem, upgradeButton.transform as RectTransform,
                shimmer, fill);
            controller.ConfigurePresentation(presentation);

            panel.gameObject.SetActive(true);
            panel.SetAsLastSibling();
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(presentation);
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            Selection.activeGameObject = panel.gameObject;
            EditorGUIUtility.PingObject(panel.gameObject);
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log("Juicy Coin Computer panel built. Existing purchase, saved level, coin " +
                      "production, upgrade prices, localization, F interaction and close behavior " +
                      "remain authoritative. Root size and position were preserved; save the scene.",
                panel.gameObject);
        }

        [MenuItem(LegacyMenuPath)]
        private static void CreateOrUpdateFromLegacyMenu() => CreateOrUpdate();

        [MenuItem(MenuPath, true)]
        private static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem(LegacyMenuPath, true)]
        private static bool CanBuildFromLegacyMenu() => CanBuild();

        private static void BuildStatCard(RectTransform panel, string name,
            TextMeshProUGUI label, float y, Color textColor)
        {
            RectTransform card = Card(panel, name, new Vector2(600f, 52f),
                new Vector2(0f, y), Hex("#281607"), Hex("#120A04"));
            ConfigureCentered(label.rectTransform, new Vector2(0f, y),
                new Vector2(552f, 40f));
            StyleText(label, 19f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, textColor);
            label.transform.SetAsLastSibling();
            card.SetSiblingIndex(Mathf.Max(0, label.transform.GetSiblingIndex() - 1));
        }

        private static RectTransform Card(Transform parent, string name, Vector2 size,
            Vector2 position, Color top, Color bottom)
        {
            RectTransform card = EnsureRect(parent, name, typeof(UnityEngine.UI.Image));
            ConfigureCentered(card, position, size);
            StyleSurface(card, top, bottom, false);
            EnsureOutline(card.gameObject, Hex("#72481F"), new Vector2(1f, -1f));
            return card;
        }

        private static void ConfigureButton(UnityEngine.UI.Button button, Vector2 position,
            Vector2 size, bool topRight, Color top, Color bottom, Color shadow,
            MiningUiData uiData)
        {
            RectTransform rect = button.transform as RectTransform;
            if (topRight) ConfigureTopRight(rect, position, size);
            else ConfigureCentered(rect, position, size);
            StyleSurface(rect, top, bottom, true);
            EnsureOutline(rect.gameObject, Color.Lerp(bottom, Color.black, 0.35f),
                new Vector2(2f, -2f));
            EnsureShadow(rect.gameObject, shadow, new Vector2(0f, -6f));
            button.targetGraphic = rect.GetComponent<UnityEngine.UI.Image>();
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            JuicyButtonTrim trim = rect.GetComponent<JuicyButtonTrim>() ??
                                    Undo.AddComponent<JuicyButtonTrim>(rect.gameObject);
            trim.SetMedalRivets(false);
            SmoothButtonPunch punch = rect.GetComponent<SmoothButtonPunch>() ??
                                      Undo.AddComponent<SmoothButtonPunch>(rect.gameObject);
            if (uiData != null)
            {
                punch.Configure(uiData.ButtonHoverScale, uiData.ButtonHoverPunchScale,
                    uiData.ButtonPressedScale, uiData.ButtonClickBounceScale,
                    uiData.ButtonHoverPunchDuration, uiData.ButtonHoverSettleDuration,
                    uiData.ButtonPressDuration, uiData.ButtonClickBounceDuration,
                    uiData.ButtonClickSettleDuration);
            }
            else punch.SetTarget(rect);
        }

        private static void AddRivet(Transform parent, string name, Vector2 position)
        {
            RectTransform rivet = EnsureRect(parent, name, typeof(UnityEngine.UI.Image));
            ConfigureCentered(rivet, position, new Vector2(11f, 11f));
            UnityEngine.UI.Image image = rivet.GetComponent<UnityEngine.UI.Image>();
            image.sprite = roundedSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = Hex("#D4A843");
            image.raycastTarget = false;
            EnsureOutline(rivet.gameObject, Hex("#3A2A08"), new Vector2(1f, -1f));
        }

        private static RectTransform EnsureRect(Transform parent, string name,
            params System.Type[] components)
        {
            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing == null)
            {
                obj = new GameObject(name, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(obj, "Build Coin Computer " + name);
                obj.layer = parent.gameObject.layer;
                obj.transform.SetParent(parent, false);
            }
            else
            {
                obj = existing.gameObject;
                obj.SetActive(true);
            }
            foreach (System.Type component in components)
            {
                if (obj.GetComponent(component) == null) Undo.AddComponent(obj, component);
            }
            return obj.transform as RectTransform;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string value,
            float size, Color color, Vector2 dimensions, Vector2 position,
            TextAlignmentOptions alignment)
        {
            RectTransform rect = EnsureRect(parent, name, typeof(TextMeshProUGUI));
            TextMeshProUGUI label = rect.GetComponent<TextMeshProUGUI>();
            ConfigureCentered(rect, position, dimensions);
            label.text = value;
            StyleText(label, size, FontStyles.Normal, alignment, color);
            return label;
        }

        private static void StyleText(TextMeshProUGUI label, float size, FontStyles style,
            TextAlignmentOptions alignment, Color color)
        {
            if (label == null) return;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.enableAutoSizing = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
        }

        private static void StyleSurface(RectTransform rect, Color top, Color bottom,
            bool raycastTarget)
        {
            UnityEngine.UI.Image image = rect.GetComponent<UnityEngine.UI.Image>() ??
                                         Undo.AddComponent<UnityEngine.UI.Image>(rect.gameObject);
            image.sprite = roundedSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = raycastTarget;
            SetGradient(rect.gameObject, top, bottom);
        }

        private static void SetGradient(GameObject obj, Color top, Color bottom)
        {
            foreach (Component component in obj.GetComponents<Component>())
            {
                if (component != null && component.GetType().Name == "MiningCandyGradient")
                    Undo.DestroyObjectImmediate(component);
            }
            MiningUiGradient gradient = obj.GetComponent<MiningUiGradient>() ??
                                        Undo.AddComponent<MiningUiGradient>(obj);
            gradient.SetColors(top, bottom);
        }

        private static void EnsureOutline(GameObject obj, Color color, Vector2 distance)
        {
            UnityEngine.UI.Outline outline = obj.GetComponent<UnityEngine.UI.Outline>() ??
                                              Undo.AddComponent<UnityEngine.UI.Outline>(obj);
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void EnsureShadow(GameObject obj, Color color, Vector2 distance)
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
            shadow.useGraphicAlpha = true;
        }

        private static void ConfigureCentered(RectTransform rect, Vector2 position, Vector2 size)
        {
            Undo.RecordObject(rect, "Layout Coin Computer");
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void ConfigureTopStretch(RectTransform rect, float height)
        {
            Undo.RecordObject(rect, "Layout Coin Computer Header");
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
            rect.localScale = Vector3.one;
        }

        private static void ConfigureTopRight(RectTransform rect, Vector2 position, Vector2 size)
        {
            Undo.RecordObject(rect, "Layout Coin Computer Close Button");
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            Undo.RecordObject(rect, "Stretch Coin Computer Element");
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = minimum;
            rect.offsetMax = maximum;
            rect.localScale = Vector3.one;
        }

        private static Sprite EnsureRoundedSprite()
        {
            string path = SpriteDirectory + "/CoinComputerLeatherRounded.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            Directory.CreateDirectory(SpriteDirectory);
            Texture2D texture = new(64, 64, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[64 * 64];
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float u = Mathf.Max(0f, Mathf.Abs(x - 31.5f) - 10.5f);
                    float v = Mathf.Max(0f, Mathf.Abs(y - 31.5f) - 10.5f);
                    float distance = Mathf.Sqrt(u * u + v * v);
                    byte alpha = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(22.5f - distance));
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
                importer.spriteBorder = new Vector4(22f, 22f, 22f, 22f);
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static T Reference<T>(SerializedObject fields, string name) where T : Object
        {
            return fields.FindProperty(name)?.objectReferenceValue as T;
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString(value, out Color color) ? color : Color.white;
        }
    }
}
#endif
