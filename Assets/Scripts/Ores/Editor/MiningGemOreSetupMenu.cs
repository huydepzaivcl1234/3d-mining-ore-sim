#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    public static class MiningGemOreSetupMenu
    {
        private const string GemDataPath = "Assets/GameData/Ores/Gem.asset";
        private const string GemPrefabPath = "Assets/Prefabs/Ores/GemOre_Purple.prefab";
        private const string SpawnDataPath = "Assets/GameData/Spawning/OreSpawnData.asset";

        [MenuItem("Mining Simulator/Setup/Configure Gem Ore Reward And Spawn")]
        public static void ConfigureGemOreRewardAndSpawn()
        {
            OreData gemData = AssetDatabase.LoadAssetAtPath<OreData>(GemDataPath);
            if (gemData == null)
            {
                Debug.LogError($"Gem Ore setup could not find '{GemDataPath}'.");
                return;
            }

            GameObject gemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GemPrefabPath);
            OreSpawnData spawnData = AssetDatabase.LoadAssetAtPath<OreSpawnData>(SpawnDataPath);
            if (gemPrefab == null || spawnData == null)
            {
                Debug.LogError("Gem Ore setup requires the existing GemOre_Purple prefab and OreSpawnData asset.");
                return;
            }

            ConfigureGemData(gemData, gemPrefab);
            ConfigureGemPrefab(gemPrefab, gemData);
            AddSpawnEntryIfMissing(spawnData, gemData);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = gemData;
            Debug.Log("Gem Ore is ready: Kind = Gem, prefab Data assigned, and spawn entry added at 1%. " +
                      "Adjust Gem Reward and Spawn Chance in GameData as needed.", gemData);
        }

        private static void ConfigureGemData(OreData gemData, GameObject gemPrefab)
        {
            Undo.RecordObject(gemData, "Configure Gem Ore Data");
            var serialized = new SerializedObject(gemData);
            SerializedProperty kind = serialized.FindProperty("kind");
            SerializedProperty prefab = serialized.FindProperty("prefab");
            SerializedProperty gemReward = serialized.FindProperty("gemReward");

            kind.intValue = (int)OreKind.Gem;
            if (prefab.objectReferenceValue == null)
            {
                prefab.objectReferenceValue = gemPrefab;
            }
            if (gemReward.intValue <= 0)
            {
                gemReward.intValue = 1;
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(gemData);
        }

        private static void ConfigureGemPrefab(GameObject gemPrefab, OreData gemData)
        {
            string prefabPath = AssetDatabase.GetAssetPath(gemPrefab);
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Ore ore = contents.GetComponent<Ore>();
                if (ore == null)
                {
                    Debug.LogError("GemOre_Purple prefab does not contain an Ore component.", gemPrefab);
                    return;
                }

                var serialized = new SerializedObject(ore);
                SerializedProperty data = serialized.FindProperty("data");
                if (data.objectReferenceValue == gemData)
                {
                    return;
                }

                data.objectReferenceValue = gemData;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void AddSpawnEntryIfMissing(OreSpawnData spawnData, OreData gemData)
        {
            var serialized = new SerializedObject(spawnData);
            SerializedProperty table = serialized.FindProperty("oreSpawnTable");
            for (int index = 0; index < table.arraySize; index++)
            {
                SerializedProperty entry = table.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("ore").objectReferenceValue == gemData)
                {
                    return;
                }
            }

            Undo.RecordObject(spawnData, "Add Gem Ore Spawn Entry");
            int newIndex = table.arraySize;
            table.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newEntry = table.GetArrayElementAtIndex(newIndex);
            newEntry.FindPropertyRelative("ore").objectReferenceValue = gemData;
            newEntry.FindPropertyRelative("spawnChancePercent").floatValue = 1f;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(spawnData);
        }
    }
}
#endif
