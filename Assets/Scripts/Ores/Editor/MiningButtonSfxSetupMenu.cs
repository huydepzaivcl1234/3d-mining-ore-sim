#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Adds the shared click SFX player to every authored Button in the open scene.</summary>
    public static class MiningButtonSfxSetupMenu
    {
        [MenuItem("Mining Simulator/Tools/Assign SFX To All Buttons")]
        public static void AssignAllButtonSfx()
        {
            AssignAllButtonSfx(true);
        }

        public static void AssignAllButtonSfx(bool showResult)
        {
            MiningAudioManager audioManager = Object.FindFirstObjectByType<MiningAudioManager>(
                FindObjectsInactive.Include);
            if (audioManager == null)
            {
                Debug.LogError("Button SFX setup could not find MiningAudioManager in the open scene.");
                return;
            }

            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            int added = 0;
            int repaired = 0;
            foreach (Button button in buttons)
            {
                if (button == null || !button.gameObject.scene.IsValid())
                {
                    continue;
                }

                MiningButtonSfx buttonSfx = button.GetComponent<MiningButtonSfx>();
                if (buttonSfx == null)
                {
                    buttonSfx = Undo.AddComponent<MiningButtonSfx>(button.gameObject);
                    added++;
                }

                SerializedObject serialized = new(buttonSfx);
                SerializedProperty managerProperty = serialized.FindProperty("audioManager");
                if (managerProperty != null && managerProperty.objectReferenceValue == null)
                {
                    managerProperty.objectReferenceValue = audioManager;
                    serialized.ApplyModifiedProperties();
                    repaired++;
                }
                EditorUtility.SetDirty(buttonSfx);
            }

            if (buttons.Length > 0)
            {
                EditorSceneManager.MarkSceneDirty(audioManager.gameObject.scene);
            }

            string result = $"Button SFX ready: {added} added, {repaired} references assigned.";
            Debug.Log(result, audioManager);
            if (showResult)
            {
                EditorUtility.DisplayDialog("Button SFX", result + " Save the scene.", "OK");
            }
        }
    }
}
#endif
