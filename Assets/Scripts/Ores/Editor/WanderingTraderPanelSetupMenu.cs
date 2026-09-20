using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Authors the complete trader hierarchy so every card and child is editable in Scene.</summary>
    public static class WanderingTraderPanelSetupMenu
    {
        private const string MenuPath =
            "Mining Simulator/Setup/Rebuild Wandering Trader Full Scene UI";

        [MenuItem(MenuPath)]
        private static void RebuildFullSceneUi()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Exit Play Mode before rebuilding the Wandering Trader UI.");
                return;
            }

            WanderingTraderPanel panel = Object.FindFirstObjectByType<WanderingTraderPanel>(
                FindObjectsInactive.Include);
            if (panel == null)
            {
                Canvas canvas = FindHudCanvas();
                if (canvas == null)
                {
                    Debug.LogWarning("Mining HUD Canvas was not found.");
                    return;
                }

                GameObject root = new("Wandering Trader Panel", typeof(RectTransform),
                    typeof(CanvasGroup), typeof(Image));
                Undo.RegisterCreatedObjectUndo(root, "Create Wandering Trader Full Scene UI");
                root.transform.SetParent(canvas.transform, false);
                Stretch(root.GetComponent<RectTransform>());
                panel = Undo.AddComponent<WanderingTraderPanel>(root);
            }
            else
            {
                Undo.RegisterFullObjectHierarchyUndo(panel.gameObject,
                    "Rebuild Wandering Trader Full Scene UI");
            }

            bool wasActive = panel.gameObject.activeSelf;
            panel.gameObject.SetActive(true);
            panel.RebuildCompleteSceneLayout();
            panel.gameObject.SetActive(wasActive);
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            Selection.activeGameObject = panel.gameObject;
            EditorGUIUtility.PingObject(panel.gameObject);
            Debug.Log("Wandering Trader full Scene UI rebuilt. Save the Scene to keep every card and child.",
                panel);
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRebuildFullSceneUi() => !Application.isPlaying;

        private static Canvas FindHudCanvas()
        {
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (canvas.name.Contains("Mining HUD Canvas")) return canvas;
            }
            return null;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
