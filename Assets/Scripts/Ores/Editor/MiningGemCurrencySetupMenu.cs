using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    public static class MiningGemCurrencySetupMenu
    {
        private const string CanvasName = "Mining HUD Canvas";
        private const string HudName = "Gem HUD";

        [MenuItem("Mining Simulator/Setup/Add Or Update Gem Currency HUD")]
        public static void AddOrUpdateGemCurrencyHud()
        {
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
                Undo.RegisterCreatedObjectUndo(hudObject, "Create Gem Currency HUD");
                hudObject.transform.SetParent(canvasObject.transform, false);
                ConfigureTopRight(hudObject.GetComponent<RectTransform>(),
                    uiData.GemHudPosition, uiData.GemHudSize);
                Image panelImage = hudObject.GetComponent<Image>();
                panelImage.color = uiData.GemHudColor;
                panelImage.raycastTarget = false;
            }

            MiningGemHud gemHud = hudObject.GetComponent<MiningGemHud>() ??
                                   Undo.AddComponent<MiningGemHud>(hudObject);
            TextMeshProUGUI gemText = EnsureLabel(hudObject.transform, "Gem Amount",
                uiData.GemTextPosition, uiData.GemTextSize, uiData.GemFontSize,
                uiData.GemTextColor, "Gems: 0");
            EnsureGemIcon(hudObject.transform, uiData);

            var hudSerialized = new SerializedObject(gemHud);
            hudSerialized.FindProperty("wallet").objectReferenceValue = wallet;
            hudSerialized.FindProperty("gemText").objectReferenceValue = gemText;
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(wallet);
            EditorUtility.SetDirty(gemHud);
            EditorSceneManager.MarkSceneDirty(hudObject.scene);
            Selection.activeGameObject = hudObject;
            Debug.Log("Gem currency is ready. The HUD is selected; save the scene after editing it.",
                hudObject);
        }

        private static void EnsureGemIcon(Transform parent, MiningUiData uiData)
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
                ConfigureTopLeft(iconObject.GetComponent<RectTransform>(),
                    uiData.GemIconPosition, uiData.GemIconSize);
            }

            Image icon = iconObject.GetComponent<Image>() ?? Undo.AddComponent<Image>(iconObject);
            if (uiData.GemIconSprite != null && icon.sprite == null)
            {
                icon.sprite = uiData.GemIconSprite;
            }
            icon.color = icon.sprite != null ? Color.white : uiData.GemIconColor;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TextMeshProUGUI fallback = EnsureLabel(iconObject.transform, "Fallback",
                Vector2.zero, uiData.GemIconSize, uiData.GemFontSize,
                Color.white, uiData.GemIconFallback);
            RectTransform fallbackRect = fallback.rectTransform;
            fallbackRect.anchorMin = Vector2.zero;
            fallbackRect.anchorMax = Vector2.one;
            fallbackRect.offsetMin = Vector2.zero;
            fallbackRect.offsetMax = Vector2.zero;
            fallback.alignment = TextAlignmentOptions.Center;
            fallback.raycastTarget = false;
            fallback.gameObject.SetActive(icon.sprite == null);
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
    }
}
