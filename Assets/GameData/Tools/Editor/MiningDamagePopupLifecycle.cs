using UnityEditor;

namespace MiningSimulator.Ores.Editor
{
    [InitializeOnLoad]
    internal static class MiningDamagePopupLifecycle
    {
        static MiningDamagePopupLifecycle()
        {
            EditorApplication.playModeStateChanged+=OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload+=Clear;
            EditorApplication.quitting+=Clear;
            EditorApplication.delayCall+=CleanOldOrphans;
        }
        private static void CleanOldOrphans(){if(!EditorApplication.isPlayingOrWillChangePlaymode)Clear();}
        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.ExitingPlayMode||state==PlayModeStateChange.EnteredEditMode||state==PlayModeStateChange.ExitingEditMode)Clear();
        }
        private static void Clear()=>MiningDamagePopup.ClearAll(true);
    }
}
