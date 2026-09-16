#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Restyles the existing Rebirth confirmation without replacing its game logic.</summary>
    public static class MiningJuicyRebirthConfirmSetupMenu
    {
        private const string MenuPath = "Mining Simulator/UI/Build Juicy Rebirth Confirmation";
        private const string SpriteDirectory = "Assets/Generated/MiningUI";
        private const string UiDataPath = "Assets/GameData/UI/MiningUiData.asset";
        private static Sprite roundedSprite;

        [MenuItem(MenuPath)]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Exit Play Mode",
                    "Stop Play Mode before authoring the Rebirth confirmation.", "OK");
                return;
            }

            MiningRebirthPanel controller = Object.FindFirstObjectByType<MiningRebirthPanel>(
                FindObjectsInactive.Include);
            if (controller == null)
            {
                Debug.LogWarning("Open the mining scene first: MiningRebirthPanel was not found. No scene was changed.");
                return;
            }

            SerializedObject controllerFields = new(controller);
            GameObject modalObject = Reference<GameObject>(controllerFields, "confirmationPanel");
            MiningRebirthSystem system = Reference<MiningRebirthSystem>(controllerFields,
                "rebirthSystem");
            PlayerWallet wallet = Reference<PlayerWallet>(controllerFields, "wallet");
            UnityEngine.UI.Button confirmButton = Reference<UnityEngine.UI.Button>(
                controllerFields, "confirmButton");
            UnityEngine.UI.Button cancelButton = Reference<UnityEngine.UI.Button>(
                controllerFields, "cancelButton");
            TextMeshProUGUI warning = Reference<TextMeshProUGUI>(controllerFields, "warningLabel");
            TextMeshProUGUI nextBoost = Reference<TextMeshProUGUI>(controllerFields,
                "nextBoostLabel");

            RectTransform modal = modalObject != null ? modalObject.transform as RectTransform : null;
            if (modal == null || system == null || wallet == null || confirmButton == null ||
                cancelButton == null || warning == null || nextBoost == null)
            {
                Debug.LogWarning("The existing Rebirth confirmation wiring is incomplete. Run the normal mining HUD setup first; no scene was changed.", controller);
                return;
            }

            roundedSprite = EnsureRoundedSprite();
            MiningUiData uiData = AssetDatabase.LoadAssetAtPath<MiningUiData>(UiDataPath);
            StyleSurface(modal, Hex("#6B3518"), Hex("#2E1005"));
            EnsureOutline(modal.gameObject, Hex("#C8872A"), new Vector2(3f, -3f));
            EnsureShadow(modal.gameObject, new Color(0f, 0f, 0f, 0.8f),
                new Vector2(0f, -10f));
            JuicyButtonTrim modalTrim = modal.GetComponent<JuicyButtonTrim>() ??
                                         Undo.AddComponent<JuicyButtonTrim>(modal.gameObject);
            modalTrim.SetMedalRivets(false);

            RectTransform header = EnsureRect(modal, "Header", typeof(UnityEngine.UI.Image));
            ConfigureTopStretch(header, 78f);
            StyleSurface(header, Hex("#FF6B35"), Hex("#8B0000"));
            EnsureOutline(header.gameObject, Hex("#4A1A00"), new Vector2(0f, -3f));

            TextMeshProUGUI title = EnsureText(header, "Title");
            ConfigureCentered(title.rectTransform, new Vector2(32f, 12f),
                new Vector2(440f, 34f));
            StyleText(title, 25f, FontStyles.Bold, TextAlignmentOptions.Center,
                Hex("#FFF8E0"));

            TextMeshProUGUI subtitle = Text(header, "Subtitle",
                "START AGAIN, RETURN STRONGER", 11f, Hex("#FFDCA0"),
                new Vector2(440f, 22f), new Vector2(32f, -19f),
                TextAlignmentOptions.Center);
            subtitle.fontStyle = FontStyles.Bold;

            RectTransform emblem = EnsureRect(header, "Fire_Emblem",
                typeof(UnityEngine.UI.Image));
            ConfigureCentered(emblem, new Vector2(-262f, 0f), new Vector2(54f, 54f));
            StyleSurface(emblem, Hex("#FFB347"), Hex("#6E1505"));
            EnsureOutline(emblem.gameObject, Hex("#FFD36A"), new Vector2(2f, -2f));
            TextMeshProUGUI emblemText = Text(emblem, "Letter", "R", 30f, Hex("#FFF0C0"),
                new Vector2(50f, 46f), Vector2.zero, TextAlignmentOptions.Center);
            emblemText.fontStyle = FontStyles.Bold;

            AddRivet(header, "Left_Rivet", new Vector2(-294f, 24f));
            AddRivet(header, "Right_Rivet", new Vector2(294f, 24f));

            RectTransform warningBanner = Panel(modal, "Warning_Banner",
                new Vector2(560f, 72f), new Vector2(0f, 111f),
                Hex("#781509"), Hex("#3C0A05"), Hex("#A63A22"));
            warningBanner.SetAsFirstSibling();
            ConfigureCentered(warning.rectTransform, new Vector2(0f, 111f),
                new Vector2(530f, 62f));
            StyleText(warning, 15f, FontStyles.Bold, TextAlignmentOptions.Center,
                Hex("#FFCFA0"));
            warning.textWrappingMode = TextWrappingModes.Normal;
            warning.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform lossCard = Panel(modal, "Loss_Card", new Vector2(270f, 130f),
                new Vector2(-145f, 4f), Hex("#64140A"), Hex("#3C0A05"), Hex("#8B2A1A"));
            TextMeshProUGUI lossTitle = Text(lossCard, "Title", "RESET", 13f,
                Hex("#FF8080"), new Vector2(240f, 25f), new Vector2(0f, 46f),
                TextAlignmentOptions.Center);
            lossTitle.fontStyle = FontStyles.Bold;
            TextMeshProUGUI lossDetails = Text(lossCard, "Details",
                "Money\nAll upgrades\nMiner level and field NPCs", 13f, Hex("#FFB0A0"),
                new Vector2(236f, 88f), new Vector2(0f, -10f),
                TextAlignmentOptions.MidlineLeft);
            lossDetails.lineSpacing = 8f;

            RectTransform gainCard = Panel(modal, "Gain_Card", new Vector2(270f, 130f),
                new Vector2(145f, 4f), Hex("#145014"), Hex("#0A320F"), Hex("#287A34"));
            TextMeshProUGUI gainTitle = Text(gainCard, "Title", "PERMANENT REWARD", 13f,
                Hex("#80FF80"), new Vector2(240f, 25f), new Vector2(0f, 46f),
                TextAlignmentOptions.Center);
            gainTitle.fontStyle = FontStyles.Bold;
            TextMeshProUGUI gainDetails = Text(gainCard, "Details",
                "Money and XP boost\nx1.00  >  x1.10\nRebirth 0  >  1", 13f,
                Hex("#A0FFB0"), new Vector2(236f, 88f), new Vector2(0f, -10f),
                TextAlignmentOptions.MidlineLeft);
            gainDetails.lineSpacing = 8f;

            RectTransform rewardRow = Panel(modal, "Permanent_Reward_Row",
                new Vector2(560f, 48f), new Vector2(0f, -94f), Hex("#6B4C08"),
                Hex("#3E2305"), Hex("#B08A28"));
            // Keep the authored runtime label immediately above its new background.
            rewardRow.SetSiblingIndex(nextBoost.rectTransform.GetSiblingIndex());
            TextMeshProUGUI rewardTitle = Text(rewardRow, "Reward_Title",
                "NEXT PERMANENT MULTIPLIER", 11f, Hex("#D4A843"),
                new Vector2(205f, 34f), new Vector2(-160f, 0f),
                TextAlignmentOptions.MidlineLeft);
            rewardTitle.fontStyle = FontStyles.Bold;
            ConfigureCentered(nextBoost.rectTransform, new Vector2(105f, -94f),
                new Vector2(330f, 38f));
            StyleText(nextBoost, 16f, FontStyles.Bold, TextAlignmentOptions.MidlineRight,
                Hex("#FFD700"));
            nextBoost.overflowMode = TextOverflowModes.Ellipsis;

            ConfigureButton(cancelButton, new Vector2(-145f, -179f),
                new Vector2(260f, 60f), Hex("#6A7480"), Hex("#2E3538"),
                Hex("#1A2025"), uiData);
            ConfigureButton(confirmButton, new Vector2(145f, -179f),
                new Vector2(260f, 60f), Hex("#D4400A"), Hex("#6E1505"),
                Hex("#3A0800"), uiData);

            TextMeshProUGUI cancelLabel = cancelButton.GetComponentInChildren<TextMeshProUGUI>(true);
            TextMeshProUGUI confirmLabel = confirmButton.GetComponentInChildren<TextMeshProUGUI>(true);
            StyleText(cancelLabel, 18f, FontStyles.Bold, TextAlignmentOptions.Center,
                Hex("#D8E0E8"));
            StyleText(confirmLabel, 18f, FontStyles.Bold, TextAlignmentOptions.Center,
                Hex("#FFE0C0"));

            CanvasGroup group = modal.GetComponent<CanvasGroup>() ??
                                Undo.AddComponent<CanvasGroup>(modal.gameObject);
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;

            JuicyRebirthConfirm presentation = modal.GetComponent<JuicyRebirthConfirm>() ??
                                                 Undo.AddComponent<JuicyRebirthConfirm>(modal.gameObject);
            presentation.Configure(system, wallet, modal, group, emblem, subtitle, lossTitle,
                lossDetails, gainTitle, gainDetails, rewardTitle);

            modal.gameObject.SetActive(true);
            modal.SetAsLastSibling();
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(presentation);
            EditorSceneManager.MarkSceneDirty(modal.gameObject.scene);
            Selection.activeGameObject = modal.gameObject;
            EditorGUIUtility.PingObject(modal.gameObject);
            Debug.Log("Juicy Rebirth confirmation built. Existing confirmation buttons, Rebirth validation, reset targets, permanent money/XP reward, localization and screen flash remain authoritative. Root size and position were preserved; save the scene.", modal.gameObject);
        }

        [MenuItem(MenuPath, true)]
        private static bool CanBuild() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static void ConfigureButton(UnityEngine.UI.Button button, Vector2 position,
            Vector2 size, Color top, Color bottom, Color shadowColor, MiningUiData uiData)
        {
            RectTransform rect = button.transform as RectTransform;
            if (rect == null)
            {
                return;
            }
            ConfigureCentered(rect, position, size);
            UnityEngine.UI.Image image = rect.GetComponent<UnityEngine.UI.Image>() ??
                                         Undo.AddComponent<UnityEngine.UI.Image>(rect.gameObject);
            image.sprite = roundedSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            SetGradient(rect.gameObject, top, bottom);
            EnsureOutline(rect.gameObject, Color.Lerp(bottom, Color.black, 0.35f),
                new Vector2(2f, -2f));
            EnsureShadow(rect.gameObject, shadowColor, new Vector2(0f, -6f));
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
            else
            {
                punch.SetTarget(rect);
            }
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 size,
            Vector2 position, Color top, Color bottom, Color outline)
        {
            RectTransform rect = EnsureRect(parent, name, typeof(UnityEngine.UI.Image));
            ConfigureCentered(rect, position, size);
            StyleSurface(rect, top, bottom);
            EnsureOutline(rect.gameObject, outline, new Vector2(1f, -1f));
            return rect;
        }

        private static void AddRivet(Transform parent, string name, Vector2 position)
        {
            RectTransform rivet = EnsureRect(parent, name, typeof(UnityEngine.UI.Image));
            ConfigureCentered(rivet, position, new Vector2(12f, 12f));
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
                Undo.RegisterCreatedObjectUndo(obj, "Build Rebirth Confirm " + name);
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
                if (obj.GetComponent(component) == null)
                {
                    Undo.AddComponent(obj, component);
                }
            }
            return obj.transform as RectTransform;
        }

        private static TextMeshProUGUI EnsureText(Transform parent, string name)
        {
            RectTransform rect = EnsureRect(parent, name, typeof(TextMeshProUGUI));
            TextMeshProUGUI label = rect.GetComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            return label;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string value,
            float size, Color color, Vector2 dimensions, Vector2 position,
            TextAlignmentOptions alignment)
        {
            TextMeshProUGUI label = EnsureText(parent, name);
            ConfigureCentered(label.rectTransform, position, dimensions);
            label.text = value;
            StyleText(label, size, FontStyles.Normal, alignment, color);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        private static void StyleText(TextMeshProUGUI label, float size, FontStyles style,
            TextAlignmentOptions alignment, Color color)
        {
            if (label == null)
            {
                return;
            }
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.enableAutoSizing = false;
            label.raycastTarget = false;
        }

        private static void StyleSurface(RectTransform rect, Color top, Color bottom)
        {
            UnityEngine.UI.Image image = rect.GetComponent<UnityEngine.UI.Image>() ??
                                         Undo.AddComponent<UnityEngine.UI.Image>(rect.gameObject);
            image.sprite = roundedSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = rect.parent != null &&
                                  rect.name == "Rebirth Confirmation";
            SetGradient(rect.gameObject, top, bottom);
        }

        private static void SetGradient(GameObject obj, Color top, Color bottom)
        {
            // Remove a legacy candy gradient when an older project revision still has it,
            // without creating a compile-time dependency on that optional script.
            foreach (Component component in obj.GetComponents<Component>())
            {
                if (component != null && component.GetType().Name == "MiningCandyGradient")
                {
                    Undo.DestroyObjectImmediate(component);
                }
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
            Undo.RecordObject(rect, "Layout Rebirth Confirmation");
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void ConfigureTopStretch(RectTransform rect, float height)
        {
            Undo.RecordObject(rect, "Layout Rebirth Confirmation Header");
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
            rect.localScale = Vector3.one;
        }

        private static Sprite EnsureRoundedSprite()
        {
            string path = SpriteDirectory + "/RebirthConfirmLeatherRounded.png";
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
