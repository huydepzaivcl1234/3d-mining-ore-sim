#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MiningSimulator.Ores;

namespace MiningSimulator.Editor
{
    public static class MiningUpgradeStationSetup
    {
        [MenuItem("Mining Simulator/Setup/Place Upgrade Anvil")]
        public static void Place()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var existing = Object.FindFirstObjectByType<MiningUpgradeStation>(FindObjectsInactive.Include);
            if (existing != null) { Selection.activeGameObject = existing.gameObject; return; }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/MiningUpgradeStation.prefab");
            if (prefab == null) { Debug.LogError("Upgrade station prefab missing."); return; }
            var station = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(station, "Place Upgrade Anvil");
            Selection.activeGameObject = station;
            EditorSceneManager.MarkSceneDirty(station.scene);
        }
    }
}
#endif
