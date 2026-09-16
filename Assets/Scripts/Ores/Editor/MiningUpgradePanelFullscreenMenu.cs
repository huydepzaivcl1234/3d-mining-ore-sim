using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>
    /// Applies only the full-canvas anchors to the existing scene-authored Upgrade Panel.
    /// Child layout, styling, and animation references are intentionally left untouched.
    /// </summary>
    public static class MiningUpgradePanelFullscreenMenu
    {
        private const string MenuPath = "Mining Simulator/UI/Make Upgrade Panel Fullscreen";
        private const string PanelName = "Upgrade Panel";

        [MenuItem(MenuPath, priority = 220)]
        private static void MakeFullscreen()
        {
            RectTransform panel = FindUpgradePanel();
            if (panel == null)
            {
                EditorUtility.DisplayDialog(
                    "Upgrade Panel not found",
                    "Open the gameplay scene and make sure a RectTransform named 'Upgrade Panel' exists.",
                    "OK");
                return;
            }

            Undo.RecordObject(panel, "Make Upgrade Panel Fullscreen");
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = Vector2.zero;

            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            Selection.activeTransform = panel;
            SceneView.FrameLastActiveSceneView();

            Debug.Log(
                "Upgrade Panel now fills its parent Canvas. Child layout and animation components were not changed.",
                panel);
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateMakeFullscreen()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static RectTransform FindUpgradePanel()
        {
            if (Selection.activeTransform is RectTransform selected &&
                selected.name == PanelName &&
                selected.gameObject.scene.IsValid())
            {
                return selected;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            RectTransform[] transforms = Resources.FindObjectsOfTypeAll<RectTransform>();
            foreach (RectTransform candidate in transforms)
            {
                if (candidate == null ||
                    candidate.name != PanelName ||
                    candidate.gameObject.scene != activeScene ||
                    EditorUtility.IsPersistent(candidate))
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }
    }
}
