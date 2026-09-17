using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    public static class MiningGemSetupMenu
    {
        private const string CanvasName = "Mining HUD Canvas";
        private const string HudName = "Gem HUD";

        [MenuItem("Mining Simulator/Setup/Add Or Update Gem HUD")]
        public static void AddOrUpdateGemHud()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Exit Play Mode",
                    "Stop Play Mode before rebuilding the Gem HUD.", "OK");
                return;
            }

            PlayerWallet wallet = Object.FindFirstObjectByType<PlayerWallet>(
                FindObjectsInactive.Include);
            if (wallet == null)
            {
                Debug.LogError("Gem setup could not find PlayerWallet in the open scene.");
                return;
            }

            GameObject canvasObject = GameObject.Find(CanvasName);
            if (canvasObject == null || canvasObject.GetComponent<Canvas>() == null)
            {
                Debug.LogError($"Gem setup could not find the Canvas named '{CanvasName}'.");
                return;
            }

            MiningGameData gameData = FindFirstAsset<MiningGameData>();
            MiningUiData uiData = FindFirstAsset<MiningUiData>();
            if (gameData == null || uiData == null)
            {
                Debug.LogError("Gem setup requires the existing MiningGameData and MiningUiData assets.");
                return;
            }

            AssignIfMissing(wallet, "gameData", gameData);

            Transform existing = canvasObject.transform.Find(HudName);
            bool createdHud = existing == null;
            GameObject hudObject = createdHud
                ? new GameObject(HudName, typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(MiningGemHud))
                : existing.gameObject;
            if (createdHud)
            {
                Undo.RegisterCreatedObjectUndo(hudObject, "Create Gem HUD");
                hudObject.transform.SetParent(canvasObject.transform, false);
                ConfigureTopRight(hudObject.GetComponent<RectTransform>(),
                    uiData.GemHudPosition, uiData.GemHudSize);
            }

            RectTransform hudRect = hudObject.GetComponent<RectTransform>();
            Image panelImage = hudObject.GetComponent<Image>() ??
                               Undo.AddComponent<Image>(hudObject);
            Undo.RecordObject(panelImage, "Make Gem HUD Root Transparent");
            panelImage.sprite = null;
            panelImage.color = Color.clear;
            panelImage.raycastTarget = false;

            MiningGemHud gemHud = hudObject.GetComponent<MiningGemHud>() ??
                                   Undo.AddComponent<MiningGemHud>(hudObject);
            RectTransform frame = EnsureFrame(hudObject.transform);
            Image icon = EnsureGemIcon(hudObject.transform, uiData, hudRect);
            TextMeshProUGUI gemText = EnsureGemAmount(hudObject.transform, hudRect);
            Button plusButton = EnsurePlusButton(hudObject.transform, hudRect);
            MiningGemSparkleGraphic sparkle = EnsureSparkles(hudObject.transform);
            MiningShopPanel shopPanel = Object.FindFirstObjectByType<MiningShopPanel>(
                FindObjectsInactive.Include);

            frame.SetAsFirstSibling();
            icon.transform.SetAsLastSibling();
            gemText.transform.SetAsLastSibling();
            plusButton.transform.SetAsLastSibling();
            sparkle.transform.SetAsLastSibling();

            var hudSerialized = new SerializedObject(gemHud);
            hudSerialized.FindProperty("wallet").objectReferenceValue = wallet;
            hudSerialized.FindProperty("gameData").objectReferenceValue = gameData;
            hudSerialized.FindProperty("gemText").objectReferenceValue = gemText;
            SetReference(hudSerialized, "counterRootRect", hudRect);
            SetReference(hudSerialized, "gemIconRect", icon.rectTransform);
            SetReference(hudSerialized, "plusButton", plusButton);
            SetReference(hudSerialized, "shopPanel", shopPanel);
            SetReference(hudSerialized, "sparkleFx", sparkle);
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();

            MiningUiPanelCoordinator coordinator = Object.FindFirstObjectByType<
                MiningUiPanelCoordinator>(FindObjectsInactive.Include);
            if (coordinator != null)
            {
                SerializedObject coordinatorSerialized = new(coordinator);
                coordinatorSerialized.FindProperty("gemHud").objectReferenceValue =
                    hudObject.GetComponent<RectTransform>();
                coordinatorSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(coordinator);
            }

            EditorUtility.SetDirty(wallet);
            EditorUtility.SetDirty(gemHud);
            EditorSceneManager.MarkSceneDirty(hudObject.scene);
            Selection.activeGameObject = hudObject;
            Debug.Log("Gem HUD rebuilt with the supplied amethyst sample style and the project's " +
                      "GemCurrencyIcon. Existing root position and size were preserved. The + button " +
                      "opens the Gem Shop when it exists. Save the scene after editing it.",
                hudObject);
        }

        [MenuItem("Mining Simulator/Setup/Add Or Update Gem HUD", true)]
        private static bool CanAddOrUpdateGemHud() =>
            !EditorApplication.isPlayingOrWillChangePlaymode;

        private static RectTransform EnsureFrame(Transform parent)
        {
            const string name = "Amethyst Frame";
            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing == null)
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(MiningGemHudFrameGraphic));
                Undo.RegisterCreatedObjectUndo(obj, "Create Amethyst Gem HUD Frame");
                obj.transform.SetParent(parent, false);
            }
            else
            {
                obj = existing.gameObject;
            }

            RectTransform rect = obj.GetComponent<RectTransform>();
            Stretch(rect);
            MiningGemHudFrameGraphic graphic = obj.GetComponent<MiningGemHudFrameGraphic>() ??
                                                Undo.AddComponent<MiningGemHudFrameGraphic>(obj);
            Undo.RecordObject(graphic, "Style Amethyst Gem HUD Frame");
            graphic.Configure();
            return rect;
        }

        private static Image EnsureGemIcon(Transform parent, MiningUiData uiData,
            RectTransform hudRect)
        {
            Transform existing = parent.Find("Gem Icon");
            bool created = existing == null;
            GameObject iconObject = created
                ? new GameObject("Gem Icon", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image))
                : existing.gameObject;
            if (created)
            {
                Undo.RegisterCreatedObjectUndo(iconObject, "Create Gem Icon");
                iconObject.transform.SetParent(parent, false);
            }

            Image icon = iconObject.GetComponent<Image>() ?? Undo.AddComponent<Image>(iconObject);
            RectTransform iconRect = icon.rectTransform;
            float height = HudHeight(hudRect);
            SetCentered(iconRect, new Vector2(0.127f, 0.5f),
                Vector2.one * Mathf.Clamp(height * 0.56f, 30f, 52f));
            Undo.RecordObject(icon, "Use Project Gem Icon");
            if (uiData.GemIconSprite != null)
            {
                icon.sprite = uiData.GemIconSprite;
            }
            icon.color = icon.sprite != null ? Color.white : uiData.GemIconColor;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Shadow glow = iconObject.GetComponent<Shadow>() ?? Undo.AddComponent<Shadow>(iconObject);
            Undo.RecordObject(glow, "Style Gem Icon Glow");
            glow.effectColor = new Color(0.86f, 0.27f, 1f, 0.72f);
            glow.effectDistance = new Vector2(2f, -2f);
            glow.useGraphicAlpha = true;

            TextMeshProUGUI fallback = EnsureLabel(iconObject.transform, "Fallback",
                Vector2.zero, Vector2.one * height * 0.56f, uiData.GemFontSize,
                Color.white, uiData.GemIconFallback);
            RectTransform fallbackRect = fallback.rectTransform;
            fallbackRect.anchorMin = Vector2.zero;
            fallbackRect.anchorMax = Vector2.one;
            fallbackRect.offsetMin = Vector2.zero;
            fallbackRect.offsetMax = Vector2.zero;
            fallback.alignment = TextAlignmentOptions.Center;
            fallback.raycastTarget = false;
            fallback.gameObject.SetActive(icon.sprite == null);
            return icon;
        }

        private static TextMeshProUGUI EnsureGemAmount(Transform parent, RectTransform hudRect)
        {
            Transform existing = parent.Find("Gem Amount");
            GameObject obj = existing != null
                ? existing.gameObject
                : new GameObject("Gem Amount", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(obj, "Create Gem Amount");
                obj.transform.SetParent(parent, false);
            }

            RectTransform rect = obj.GetComponent<RectTransform>();
            SetAnchoredRegion(rect, new Vector2(0.235f, 0.22f), new Vector2(0.80f, 0.78f));
            TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>() ??
                                      Undo.AddComponent<TextMeshProUGUI>(obj);
            Undo.RecordObject(label, "Style Gem Amount");
            label.text = "0";
            label.fontSize = Mathf.Clamp(HudHeight(hudRect) * 0.39f, 20f, 30f);
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 30f;
            label.characterSpacing = 1.5f;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.outlineColor = new Color32(32, 3, 48, 255);
            label.outlineWidth = 0.24f;
            label.raycastTarget = false;
            return label;
        }

        private static Button EnsurePlusButton(Transform parent, RectTransform hudRect)
        {
            const string name = "Add Gems Button";
            Transform existing = parent.Find(name);
            GameObject obj = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(MiningGemPlusGraphic), typeof(Button),
                    typeof(JuicyGemPlusButton));
            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(obj, "Create Add Gems Button");
                obj.transform.SetParent(parent, false);
            }

            float diameter = Mathf.Clamp(HudHeight(hudRect) * 0.56f, 30f, 52f);
            RectTransform rect = obj.GetComponent<RectTransform>();
            SetCentered(rect, new Vector2(0.885f, 0.5f), Vector2.one * diameter);
            Image oldImage = obj.GetComponent<Image>();
            if (oldImage != null)
            {
                Undo.DestroyObjectImmediate(oldImage);
            }
            MiningGemPlusGraphic plusGraphic = obj.GetComponent<MiningGemPlusGraphic>() ??
                                                Undo.AddComponent<MiningGemPlusGraphic>(obj);
            Undo.RecordObject(plusGraphic, "Configure Add Gems Visual");
            plusGraphic.color = Color.white;
            plusGraphic.raycastTarget = true;

            Button button = obj.GetComponent<Button>() ?? Undo.AddComponent<Button>(obj);
            Undo.RecordObject(button, "Configure Add Gems Button");
            button.targetGraphic = plusGraphic;
            button.transition = Selectable.Transition.None;
            _ = obj.GetComponent<JuicyGemPlusButton>() ??
                Undo.AddComponent<JuicyGemPlusButton>(obj);

            TextMeshProUGUI plus = EnsureLabel(obj.transform, "Plus", Vector2.zero,
                Vector2.one * diameter, diameter * 0.66f,
                new Color(1f, 0.86f, 0.40f, 1f), "+");
            Stretch(plus.rectTransform);
            plus.alignment = TextAlignmentOptions.Center;
            plus.fontStyle = FontStyles.Bold;
            plus.outlineColor = new Color32(64, 25, 0, 255);
            plus.outlineWidth = 0.18f;
            plus.raycastTarget = false;
            return button;
        }

        private static MiningGemSparkleGraphic EnsureSparkles(Transform parent)
        {
            const string name = "Gem Sparkles";
            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing == null)
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(MiningGemSparkleGraphic));
                Undo.RegisterCreatedObjectUndo(obj, "Create Gem Sparkles");
                obj.transform.SetParent(parent, false);
            }
            else
            {
                obj = existing.gameObject;
            }
            RectTransform rect = obj.GetComponent<RectTransform>();
            Stretch(rect);
            MiningGemSparkleGraphic sparkles = obj.GetComponent<MiningGemSparkleGraphic>() ??
                                                Undo.AddComponent<MiningGemSparkleGraphic>(obj);
            sparkles.raycastTarget = false;
            return sparkles;
        }

        private static TextMeshProUGUI EnsureLabel(Transform parent, string name,
            Vector2 position, Vector2 size, float fontSize, Color color, string text)
        {
            Transform existing = parent.Find(name);
            bool created = existing == null;
            GameObject labelObject = created
                ? new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI))
                : existing.gameObject;
            if (created)
            {
                Undo.RegisterCreatedObjectUndo(labelObject, $"Create {name}");
                labelObject.transform.SetParent(parent, false);
                ConfigureTopLeft(labelObject.GetComponent<RectTransform>(), position, size);
            }

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>() ??
                                      Undo.AddComponent<TextMeshProUGUI>(labelObject);
            if (created)
            {
                label.text = text;
                label.fontSize = fontSize;
                label.color = color;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.enableWordWrapping = false;
                label.raycastTarget = false;
            }
            return label;
        }

        private static void ConfigureTopRight(RectTransform rect, Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void ConfigureTopLeft(RectTransform rect, Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static float HudHeight(RectTransform hudRect)
        {
            if (hudRect == null)
            {
                return 64f;
            }
            float height = hudRect.rect.height;
            if (height <= 1f) height = hudRect.sizeDelta.y;
            return height > 1f ? height : 64f;
        }

        private static void SetCentered(RectTransform rect, Vector2 anchor, Vector2 size)
        {
            Undo.RecordObject(rect, "Layout " + rect.name);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void SetAnchoredRegion(RectTransform rect, Vector2 anchorMin,
            Vector2 anchorMax)
        {
            Undo.RecordObject(rect, "Layout " + rect.name);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }
            Undo.RecordObject(rect, "Stretch " + rect.name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static T FindFirstAsset<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0)
            {
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void AssignIfMissing(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null && property.objectReferenceValue == null)
            {
                property.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetReference(SerializedObject target, string propertyName,
            Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }
    }
}
