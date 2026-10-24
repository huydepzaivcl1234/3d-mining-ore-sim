using UnityEditor;

namespace MiningSimulator.Ores
{
    [InitializeOnLoad]
    internal static class GameSaveEditorLifecycle
    {
        static GameSaveEditorLifecycle()
        {
            // Final owner OnDisable callbacks can occur after the persistent host is destroyed.
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) GameSave.Flush();
            };
            AssemblyReloadEvents.beforeAssemblyReload += () => GameSave.Flush();
        }
    }
}
