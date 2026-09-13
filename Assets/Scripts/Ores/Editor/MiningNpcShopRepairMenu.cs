using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Ores.Editor
{
    public static class MiningNpcShopRepairMenu
    {
        private const string NpcPrefabPath = "Assets/Prefabs/NPC/MiningNpc.prefab";

        [MenuItem("Mining Simulator/Fixes/Repair NPC Shop Prefab Reference")]
        public static void RepairNpcShopPrefabReference()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Exit Play Mode before repairing the NPC Shop reference.");
                return;
            }

            NpcShop shop = Object.FindFirstObjectByType<NpcShop>(FindObjectsInactive.Include);
            if (shop == null)
            {
                Debug.LogError("Could not find NpcShop in the open scene.");
                return;
            }

            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPrefabPath);
            MiningNpc npcPrefab = prefabRoot != null ? prefabRoot.GetComponent<MiningNpc>() : null;
            if (npcPrefab == null)
            {
                Debug.LogError($"Could not find MiningNpc on '{NpcPrefabPath}'.");
                return;
            }

            var serializedShop = new SerializedObject(shop);
            SerializedProperty prefabProperty = serializedShop.FindProperty("npcPrefab");
            if (prefabProperty == null)
            {
                Debug.LogError("NpcShop no longer contains the serialized npcPrefab field.", shop);
                return;
            }

            if (prefabProperty.objectReferenceValue == npcPrefab)
            {
                Debug.Log("NPC Shop already references the current MiningNpc prefab.", shop);
                Selection.activeObject = shop;
                return;
            }

            Undo.RecordObject(shop, "Repair NPC Shop Prefab Reference");
            prefabProperty.objectReferenceValue = npcPrefab;
            serializedShop.ApplyModifiedProperties();
            EditorUtility.SetDirty(shop);
            EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);
            Selection.activeObject = shop;

            Debug.Log("NPC Shop prefab reference repaired. Save the open scene, then test Buy Miner in Play Mode.",
                shop);
        }
    }
}
