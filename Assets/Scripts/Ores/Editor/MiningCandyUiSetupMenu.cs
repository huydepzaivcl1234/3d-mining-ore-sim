#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Applies one cohesive candy theme to the authored mining UI in the open scene.</summary>
    public static class MiningCandyUiSetupMenu
    {
        private const string CanvasName = "Mining HUD Canvas";
        private const string GeneratedFolder = "Assets/Generated/MiningUI";
        private const string RoundedSpritePath = GeneratedFolder + "/CandyRoundedRect.png";
        private const string HighlightName = "Candy Highlight";
        private const string PromptKey = "MiningSimulator.CandyUiPrompt.v1";

        private readonly struct Palette
        {
            public readonly Color Top;
            public readonly Color Bottom;

            public Palette(Color top, Color bottom)
            {
                Top = top;
                Bottom = bottom;
            }
        }

        [InitializeOnLoadMethod]
        private static void OfferThemeAfterImport()
        {
            if (EditorPrefs.GetBool(PromptKey, false))
            {
                return;
            }
            EditorApplication.delayCall += ShowImportPrompt;
        }

        private static void ShowImportPrompt()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode ||
                FindHudCanvas() == null)
            {
                return;
            }

            bool apply = EditorUtility.DisplayDialog("Mining Candy UI Ready",
                "Apply the new PC, Android and iOS candy theme to the currently open scene?",
                "Apply Theme", "Later");
            EditorPrefs.SetBool(PromptKey, true);
            if (apply)
            {
                ApplyCandyTheme();
            }
        }

        [MenuItem("Mining Simulator/Setup/Apply Candy UI Theme (PC Android iOS)")]
        public static void ApplyCandyTheme()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Candy UI",
                    "Exit Play Mode before applying the authored UI theme.", "OK");
                return;
            }

            Canvas canvas = FindHudCanvas();
            MiningUiData data = FindFirstAsset<MiningUiData>();
            if (canvas == null || data == null)
            {
                EditorUtility.DisplayDialog("Candy UI",
                    $"Open the gameplay scene and make sure '{CanvasName}' and MiningUiData exist.",
                    "OK");
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Mining Candy UI Theme");
            Sprite roundedSprite = LoadOrCreateRoundedSprite();
            ConfigureCanvas(canvas, data);

            Image[] images = canvas.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                ApplyImageStyle(image, roundedSprite, data);
            }

            TextMeshProUGUI[] labels = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI label in labels)
            {
                ApplyTextStyle(label, data);
            }

            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                ApplyButtonFeedback(button, data);
            }

            ConfigureSafeArea(canvas, data);
            MiningButtonSfxSetupMenu.AssignAllButtonSfx(false);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = canvas.gameObject;
            EditorGUIUtility.PingObject(canvas.gameObject);
            Debug.Log($"Candy UI applied to {images.Length} graphics, {labels.Length} labels and " +
                      $"{buttons.Length} buttons. Save the scene after reviewing it.", canvas);
            EditorUtility.DisplayDialog("Candy UI",
                "Candy theme applied for PC, Android and iOS. Review the Scene/Game views, " +
                "then save the scene.", "OK");
        }

        private static void ConfigureCanvas(Canvas canvas, MiningUiData data)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ??
                                  Undo.AddComponent<CanvasScaler>(canvas.gameObject);
            Undo.RecordObject(scaler, "Configure Cross-platform Canvas");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = data.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = data.MatchWidthOrHeight;
            scaler.referencePixelsPerUnit = 100f;
            EditorUtility.SetDirty(scaler);
        }

        private static void ApplyImageStyle(Image image, Sprite roundedSprite, MiningUiData data)
        {
            if (image == null || image.name == HighlightName ||
                IsTransitionGraphic(image.name))
            {
                return;
            }

            RectTransform rect = image.rectTransform;
            if (IsFullScreenOverlay(rect))
            {
                Undo.RecordObject(image, "Style Candy Overlay");
                Color backdrop = data.CandyNeutralBottom;
                backdrop.a = image.name == "Main Menu" ? 1f : 0.80f;
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.color = backdrop;
                image.raycastTarget = true;
                EditorUtility.SetDirty(image);
                return;
            }

            bool isButton = image.GetComponent<Button>() != null;
            bool isIcon = IsIcon(image) && !isButton;
            if (isIcon)
            {
                ApplyOutline(image.gameObject, data.CandyOutlineColor,
                    Mathf.Max(1.5f, data.CandyOutlineThickness * 0.55f));
                return;
            }
            if (image.type == Image.Type.Filled &&
                image.fillMethod != Image.FillMethod.Horizontal)
            {
                ApplyOutline(image.gameObject, data.CandyOutlineColor,
                    Mathf.Max(1.5f, data.CandyOutlineThickness * 0.55f));
                return;
            }

            Palette palette = ResolvePalette(image.transform, data);
            Undo.RecordObject(image, "Style Candy Graphic");
            if (image.type == Image.Type.Filled)
            {
                image.sprite = roundedSprite;
            }
            else
            {
                image.sprite = roundedSprite;
                image.type = Image.Type.Sliced;
            }
            image.color = Color.white;
            EditorUtility.SetDirty(image);

            MiningCandyGradient gradient = image.GetComponent<MiningCandyGradient>() ??
                                            Undo.AddComponent<MiningCandyGradient>(
                                                image.gameObject);
            Undo.RecordObject(gradient, "Configure Candy Gradient");
            gradient.SetColors(palette.Top, palette.Bottom);
            EditorUtility.SetDirty(gradient);

            ApplyShadow(image.gameObject, data.CandyOutlineColor,
                data.CandyOutlineThickness * 1.35f);
            ApplyOutline(image.gameObject, data.CandyOutlineColor,
                data.CandyOutlineThickness);

            if (ShouldHaveGloss(image))
            {
                EnsureGloss(rect, roundedSprite, data);
            }
        }

        private static void ApplyTextStyle(TextMeshProUGUI label, MiningUiData data)
        {
            if (label == null)
            {
                return;
            }

            Undo.RecordObject(label, "Style Candy Text");
            label.text = label.text.Replace("✓", string.Empty).TrimEnd();
            if (!Contains(label.name, "fallback"))
            {
                label.color = Color.white;
            }
            label.fontStyle |= FontStyles.Bold;
            label.raycastTarget = false;

            if (!IsInsideInventorySlot(label.transform))
            {
                if (Contains(label.name, "title") || Contains(label.name, "header"))
                {
                    label.fontSize = Mathf.Max(label.fontSize, 30f);
                }
                else if (label.GetComponentInParent<Button>() != null)
                {
                    label.fontSize = Mathf.Max(label.fontSize, 21f);
                }
            }
            EditorUtility.SetDirty(label);

            float textOutline = Mathf.Lerp(1.5f, 3f, data.CandyTextOutlineWidth);
            ApplyOutline(label.gameObject, data.CandyOutlineColor, textOutline);
        }

        private static void ApplyButtonFeedback(Button button, MiningUiData data)
        {
            if (button == null)
            {
                return;
            }

            Undo.RecordObject(button, "Configure Candy Button");
            button.transition = Selectable.Transition.None;
            EditorUtility.SetDirty(button);

            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                Undo.RecordObject(rect, "Configure Touch Target");
                Vector2 size = rect.sizeDelta;
                if (size.x > 0f && size.x < data.CandyMinimumTouchTarget)
                {
                    size.x = data.CandyMinimumTouchTarget;
                }
                if (size.y > 0f && size.y < data.CandyMinimumTouchTarget)
                {
                    size.y = data.CandyMinimumTouchTarget;
                }
                rect.sizeDelta = size;
                EditorUtility.SetDirty(rect);
            }

            SmoothButtonPunch punch = button.GetComponent<SmoothButtonPunch>() ??
                                      Undo.AddComponent<SmoothButtonPunch>(button.gameObject);
            Undo.RecordObject(punch, "Configure Button Animation");
            punch.Configure(data.ButtonHoverScale, data.ButtonHoverPunchScale,
                data.ButtonPressedScale, data.ButtonClickBounceScale,
                data.ButtonHoverPunchDuration, data.ButtonHoverSettleDuration,
                data.ButtonPressDuration, data.ButtonClickBounceDuration,
                data.ButtonClickSettleDuration);
            EditorUtility.SetDirty(punch);
        }

        private static void ConfigureSafeArea(Canvas canvas, MiningUiData data)
        {
            string[] protectedObjects =
            {
                "NPC Shop",
                "Rebirth HUD",
                "Audio Menu Button",
                "NPC Progress HUD",
                "Inventory Menu Button",
                "Gem HUD",
                "Shop Menu Button"
            };

            foreach (string objectName in protectedObjects)
            {
                Transform target = canvas.transform.Find(objectName);
                RectTransform rect = target as RectTransform;
                if (rect == null)
                {
                    continue;
                }

                MiningSafeAreaInset inset = rect.GetComponent<MiningSafeAreaInset>() ??
                                            Undo.AddComponent<MiningSafeAreaInset>(
                                                rect.gameObject);
                Undo.RecordObject(inset, "Configure Safe Area");
                inset.Configure(rect, data.CandySafeAreaPadding);
                EditorUtility.SetDirty(inset);
            }
        }

        private static Palette ResolvePalette(Transform target, MiningUiData data)
        {
            string ownName = target.name.ToLowerInvariant();
            string context = BuildContext(target);
            bool isButton = target.GetComponent<Button>() != null;
            bool isHeader = ownName.Contains("header");

            if (isButton && ContainsAny(ownName, "rebirth", "exit", "close", "reset",
                    "cancel", "delete", "danger"))
            {
                return new Palette(data.CandyDangerTop, data.CandyDangerBottom);
            }
            if (isButton && ContainsAny(ownName, "play", "buy", "confirm", "upgrade",
                    "back", "main menu", "spin"))
            {
                return new Palette(data.CandyPrimaryTop, data.CandyPrimaryBottom);
            }
            if (isButton && ContainsAny(ownName, "shop", "settings", "inventory",
                    "audio", "language"))
            {
                return new Palette(data.CandyGemTop, data.CandyGemBottom);
            }
            if (ownName == "gem hud")
            {
                return new Palette(data.CandyGemTop, data.CandyGemBottom);
            }
            if (ContainsAny(ownName, "money badge", "coin badge", "gold badge"))
            {
                return new Palette(data.CandyGoldTop, data.CandyGoldBottom);
            }
            if (isHeader && ContainsAny(context, "rebirth", "exit", "reset"))
            {
                return new Palette(data.CandyDangerTop, data.CandyDangerBottom);
            }
            if (isHeader && ContainsAny(context, "shop", "settings", "inventory",
                    "progress", "audio", "gem"))
            {
                return new Palette(data.CandyGemTop, data.CandyGemBottom);
            }
            if (isHeader && ContainsAny(context, "upgrade"))
            {
                return new Palette(data.CandyPrimaryTop, data.CandyPrimaryBottom);
            }
            if (ContainsAny(ownName, "fill", "progress"))
            {
                return new Palette(data.CandyPrimaryTop, data.CandyPrimaryBottom);
            }
            return new Palette(data.CandyNeutralTop, data.CandyNeutralBottom);
        }

        private static void EnsureGloss(RectTransform parent, Sprite roundedSprite,
            MiningUiData data)
        {
            Transform existing = parent.Find(HighlightName);
            GameObject highlightObject;
            if (existing == null)
            {
                highlightObject = new GameObject(HighlightName, typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image), typeof(MiningCandyGradient));
                Undo.RegisterCreatedObjectUndo(highlightObject, "Create Candy Highlight");
                highlightObject.transform.SetParent(parent, false);
            }
            else
            {
                highlightObject = existing.gameObject;
            }

            RectTransform rect = highlightObject.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Layout Candy Highlight");
            rect.anchorMin = new Vector2(0.06f, 0.57f);
            rect.anchorMax = new Vector2(0.94f, 0.91f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.SetAsFirstSibling();

            Image image = highlightObject.GetComponent<Image>();
            Undo.RecordObject(image, "Style Candy Highlight");
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;

            MiningCandyGradient gradient = highlightObject.GetComponent<MiningCandyGradient>();
            Color top = new(1f, 1f, 1f, data.CandyGlossAlpha);
            Color bottom = new(1f, 1f, 1f, data.CandyGlossAlpha * 0.08f);
            Undo.RecordObject(gradient, "Configure Candy Highlight");
            gradient.SetColors(top, bottom);
            EditorUtility.SetDirty(highlightObject);
        }

        private static void ApplyOutline(GameObject target, Color color, float thickness)
        {
            Outline outline = target.GetComponent<Outline>() ??
                              Undo.AddComponent<Outline>(target);
            Undo.RecordObject(outline, "Configure Candy Outline");
            outline.effectColor = color;
            outline.effectDistance = new Vector2(thickness, -thickness);
            outline.useGraphicAlpha = true;
            EditorUtility.SetDirty(outline);
        }

        private static void ApplyShadow(GameObject target, Color outlineColor, float distance)
        {
            Shadow shadow = FindPlainShadow(target);
            if (shadow == null)
            {
                shadow = Undo.AddComponent<Shadow>(target);
            }
            Undo.RecordObject(shadow, "Configure Candy Shadow");
            shadow.effectColor = new Color(outlineColor.r * 0.35f, outlineColor.g * 0.35f,
                outlineColor.b * 0.35f, 0.58f);
            shadow.effectDistance = new Vector2(0f, -Mathf.Max(2f, distance));
            shadow.useGraphicAlpha = true;
            EditorUtility.SetDirty(shadow);
        }

        private static Shadow FindPlainShadow(GameObject target)
        {
            Shadow[] shadows = target.GetComponents<Shadow>();
            foreach (Shadow shadow in shadows)
            {
                if (shadow.GetType() == typeof(Shadow))
                {
                    return shadow;
                }
            }
            return null;
        }

        private static bool IsFullScreenOverlay(RectTransform rect)
        {
            if (rect == null)
            {
                return false;
            }
            bool stretched = Vector2.Distance(rect.anchorMin, Vector2.zero) < 0.001f &&
                             Vector2.Distance(rect.anchorMax, Vector2.one) < 0.001f &&
                             rect.sizeDelta.sqrMagnitude < 4f;
            return stretched && ContainsAny(rect.name, "panel", "menu", "confirmation",
                "overlay", "flash");
        }

        private static bool IsTransitionGraphic(string objectName)
        {
            return ContainsAny(objectName, "transition flash", "transition bar",
                "rebirth flash");
        }

        private static bool IsIcon(Image image)
        {
            return image.sprite != null &&
                   ContainsAny(image.name, "icon", "item", "sprite", "portrait");
        }

        private static bool ShouldHaveGloss(Image image)
        {
            return image.GetComponent<Button>() != null ||
                   ContainsAny(image.name, "header", "gem hud", "rebirth hud",
                       "npc progress hud", "npc shop");
        }

        private static bool IsInsideInventorySlot(Transform target)
        {
            for (Transform current = target; current != null; current = current.parent)
            {
                if (ContainsAny(current.name, "slot", "rare gift product"))
                {
                    return true;
                }
            }
            return false;
        }

        private static string BuildContext(Transform target)
        {
            string context = string.Empty;
            for (Transform current = target; current != null; current = current.parent)
            {
                context += " " + current.name;
                if (current.name == CanvasName)
                {
                    break;
                }
            }
            return context.ToLowerInvariant();
        }

        private static bool Contains(string value, string fragment)
        {
            return value != null && value.ToLowerInvariant().Contains(fragment);
        }

        private static bool ContainsAny(string value, params string[] fragments)
        {
            string lower = value != null ? value.ToLowerInvariant() : string.Empty;
            foreach (string fragment in fragments)
            {
                if (lower.Contains(fragment))
                {
                    return true;
                }
            }
            return false;
        }

        private static Sprite LoadOrCreateRoundedSprite()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
            if (sprite != null)
            {
                return sprite;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Generated"))
            {
                AssetDatabase.CreateFolder("Assets", "Generated");
            }
            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                AssetDatabase.CreateFolder("Assets/Generated", "MiningUI");
            }

            const int size = 64;
            const float radius = 15f;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false, true);
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nearestX = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float nearestY = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f),
                        new Vector2(nearestX, nearestY));
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    pixels[y * size + x] = new Color32(255, 255, 255,
                        (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(RoundedSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(RoundedSpritePath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(RoundedSpritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = 100f;
                importer.spriteBorder = new Vector4(18f, 18f, 18f, 18f);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        }

        private static Canvas FindHudCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.gameObject.scene.IsValid() && canvas.name == CanvasName)
                {
                    return canvas;
                }
            }
            return null;
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
