#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Editor
{
    /// <summary>Remove retired one-off editor menus when updating an existing project from a ZIP.</summary>
    public static class MiningObsoleteToolPruner
    {
        private static readonly string[] RetiredTools =
        {
            "Assets/Scripts/Ores/Editor/MiningPcHudCleanLayoutMenu.cs",
            "Assets/Scripts/Ores/Editor/MiningUiCleanupMenu.cs",
            "Assets/Scripts/Ores/Editor/MiningUiRestyleMenu.cs",
            "Assets/Scripts/Ores/Editor/MiningCandyUiSetupMenu.cs",
            "Assets/Scripts/Ores/Editor/MiningPcHudProgressTextRepair.cs",
            "Assets/Scripts/Ores/Editor/MiningJuicyMinerProgressSetupMenu.cs"
        };

        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.delayCall -= Prune;
            EditorApplication.delayCall += Prune;
        }

        private static void Prune()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                    {
                        if (canvas.name != "Mining HUD Canvas") continue;
                        Transform card = canvas.transform.Find("NPC Progress HUD/Card_Visual");
                        Transform heading = card != null ? card.Find("PC Progress Title") : null;
                        if (heading == null) continue;
                        Undo.DestroyObjectImmediate(heading.gameObject);
                        var presenter = card.GetComponentInParent<MiningSimulator.Ores.JuicyMinerProgress>();
                        if (presenter != null)
                        {
                            SerializedObject fields = new(presenter);
                            SerializedProperty title = fields.FindProperty("titleText");
                            if (title != null) title.objectReferenceValue = null;
                            fields.ApplyModifiedProperties();
                        }
                        EditorSceneManager.MarkSceneDirty(scene);
                    }
                }
            }
            foreach (string path in RetiredTools)
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                    AssetDatabase.DeleteAsset(path);
        }
    }
}
#endif
