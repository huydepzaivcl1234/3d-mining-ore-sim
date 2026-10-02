#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MiningSimulator.Ores.Editor
{
    public static class MiningGameplayTuningSetup
    {
        [MenuItem("Mining Simulator/Setup/GameManager XP and Boss Debug")]
        public static void Apply()
        {
            if (Application.isPlaying) return;
            var manager = GameObject.Find("GameManager");
            if (manager == null) { Debug.LogWarning("Select a scene containing the existing GameManager."); return; }
            var tuning = manager.GetComponent<MiningGameplayTuning>();
            if (tuning == null) tuning = Undo.AddComponent<MiningGameplayTuning>(manager);
            var fields = new SerializedObject(tuning);
            var player = Object.FindAnyObjectByType<MiningPlayerStats>(FindObjectsInactive.Include);
            if (fields.FindProperty("player").objectReferenceValue == null) fields.FindProperty("player").objectReferenceValue = player;
            if (fields.FindProperty("playerData").objectReferenceValue == null && player != null) fields.FindProperty("playerData").objectReferenceValue = player.Data;
            if (fields.FindProperty("monsterSpawner").objectReferenceValue == null) fields.FindProperty("monsterSpawner").objectReferenceValue = Object.FindAnyObjectByType<MonsterSpawnZone>(FindObjectsInactive.Include);
            if (fields.FindProperty("debugBossPrefab").objectReferenceValue == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monsters/GolemMonster.prefab");
                if (prefab != null) fields.FindProperty("debugBossPrefab").objectReferenceValue = prefab.GetComponent<MushroomMonster>();
            }
            fields.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(manager.scene);
            Selection.activeGameObject = manager;
        }
    }
}
#endif
