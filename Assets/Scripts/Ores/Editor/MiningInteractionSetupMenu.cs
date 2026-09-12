#if UNITY_EDITOR
using MiningSimulator.Ores;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiningSimulator.Editor
{
    /// <summary>Authors the reusable world-interaction input and TMP prompt into the open scene.</summary>
    public static class MiningInteractionSetupMenu
    {
        private const string DataFolder = "Assets/GameData/Interaction";
        private const string DataPath = DataFolder + "/MiningInteractionData.asset";
        private const string SystemObjectName = "Mining Interaction System";
        private const string PromptObjectName = "Interaction Hover Prompt";
        private const string HudCanvasName = "Mining HUD Canvas";

        [MenuItem("Mining Simulator/Setup/Create or Update Interaction System")]
        public static void CreateOrUpdateInteractionSystem()
        {
            if (!EnsureInOpenScene(true))
            {
                EditorUtility.DisplayDialog("Interaction Setup",
                    "Mining HUD Canvas was not found in the open scene.", "OK");
                return;
            }

            EditorUtility.DisplayDialog("Interaction Setup",
                "Hover/F interaction is ready. The prompt is visible and selected for editing; " +
                "it hides automatically in Play Mode until the cursor is over an interactable.",
                "OK");
        }

        public static bool EnsureInOpenScene(bool selectPrompt)
        {
            Canvas canvas = FindHudCanvas();
            if (canvas == null)
            {
                return false;
            }

            MiningInteractionData data = LoadOrCreateData();
            MiningInteractionPrompt prompt = FindPrompt();
            if (prompt == null)
            {
                prompt = CreatePrompt(canvas, data);
            }

            MiningInteractionSystem system = Object.FindFirstObjectByType<
                MiningInteractionSystem>(FindObjectsInactive.Include);
            if (system == null)
            {
                Transform parent = FindTransformByName("UI Systems");
                GameObject systemObject = new(SystemObjectName);
                if (parent != null)
                {
                    systemObject.transform.SetParent(parent, false);
                }
                else
                {
                    SceneManager.MoveGameObjectToScene(systemObject,
                        SceneManager.GetActiveScene());
                }

                Undo.RegisterCreatedObjectUndo(systemObject, "Create Mining Interaction System");
                system = systemObject.AddComponent<MiningInteractionSystem>();
            }

            // Re-running setup must preserve designer-assigned alternate data/prompt assets.
            system.ConfigureIfMissing(data, prompt);
            data = system.Data != null ? system.Data : data;
            prompt = system.Prompt != null ? system.Prompt : prompt;
            prompt.ShowEditorPreview(data.GetPrompt(
                MiningLocalization.Text("Portal", "Cổng dịch chuyển")));
            EditorUtility.SetDirty(system);
            EditorUtility.SetDirty(prompt);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

            if (selectPrompt)
            {
                Selection.activeGameObject = prompt.gameObject;
                EditorGUIUtility.PingObject(prompt.gameObject);
                SceneView.lastActiveSceneView?.FrameSelected();
            }

            return true;
        }

        private static MiningInteractionData LoadOrCreateData()
        {
            MiningInteractionData data = AssetDatabase.LoadAssetAtPath<
                MiningInteractionData>(DataPath);
            if (data != null)
            {
                return data;
            }

            EnsureFolder("Assets", "GameData");
            EnsureFolder("Assets/GameData", "Interaction");
            data = ScriptableObject.CreateInstance<MiningInteractionData>();
            AssetDatabase.CreateAsset(data, DataPath);
            AssetDatabase.SaveAssets();
            return data;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string fullPath = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static MiningInteractionPrompt CreatePrompt(Canvas canvas,
            MiningInteractionData data)
        {
            GameObject root = new(PromptObjectName, typeof(RectTransform), typeof(Image),
                typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            Undo.RegisterCreatedObjectUndo(root, "Create Interaction Hover Prompt");

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = data.PromptSize;

            Image background = root.GetComponent<Image>();
            background.color = data.BackgroundColor;
            background.raycastTarget = false;

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;

            GameObject labelObject = new("Label", typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root.transform, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = data.FontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = data.TextColor;
            label.raycastTarget = false;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 6f);
            labelRect.offsetMax = new Vector2(-10f, -6f);

            MiningInteractionPrompt prompt = root.AddComponent<MiningInteractionPrompt>();
            prompt.Configure(canvas, rect, group, label);
            root.transform.SetAsLastSibling();
            return prompt;
        }

        private static MiningInteractionPrompt FindPrompt()
        {
            return Object.FindFirstObjectByType<MiningInteractionPrompt>(
                FindObjectsInactive.Include);
        }

        private static Canvas FindHudCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas != null && canvas.name == HudCanvasName)
                {
                    return canvas;
                }
            }

            return null;
        }

        private static Transform FindTransformByName(string objectName)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform candidate in transforms)
            {
                if (candidate != null && candidate.name == objectName)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
#endif
