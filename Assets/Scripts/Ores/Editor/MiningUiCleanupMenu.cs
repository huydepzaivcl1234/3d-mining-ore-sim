#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Removes obsolete UI effects from the user's open scene without replacing it.</summary>
    public static class MiningUiCleanupMenu
    {
        [MenuItem("Mining Simulator/Fixes/Remove Candy UI From Open Scene")]
        private static void RemoveCandyUi()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("UI Cleanup", "Exit Play Mode first.", "OK");
                return;
            }

            Canvas canvas = null;
            foreach (Canvas candidate in Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name == "Mining HUD Canvas")
                {
                    canvas = candidate;
                    break;
                }
            }
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("UI Cleanup",
                    "Open your gameplay scene with Mining HUD Canvas first.", "OK");
                return;
            }

            int effectsRemoved = 0;
            foreach (MiningCandyGradient effect in
                canvas.GetComponentsInChildren<MiningCandyGradient>(true))
            {
                Undo.DestroyObjectImmediate(effect);
                effectsRemoved++;
            }

            int highlightsRemoved = 0;
            foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child.name == "Candy Highlight")
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    highlightsRemoved++;
                }
            }

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorUtility.DisplayDialog("UI Cleanup",
                $"Removed {effectsRemoved} gradients and {highlightsRemoved} highlights. " +
                "Review the UI and save your scene.", "OK");
        }
    }
}
#endif
