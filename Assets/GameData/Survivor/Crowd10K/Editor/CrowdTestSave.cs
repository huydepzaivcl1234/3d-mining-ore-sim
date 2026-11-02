#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace MiningSimulator.Ores.Editor
{
    // Only CLI integration tests set this SessionState key; normal Play always uses the real profile.
    internal static class CrowdTestSave
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Isolate()
        {
            string path=SessionState.GetString("SurvivorCrowd.TestSave","");if(string.IsNullOrEmpty(path))return;
            typeof(GameSave).GetField("testStore",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,new GameSaveStore(path));
        }
    }
}
#endif
