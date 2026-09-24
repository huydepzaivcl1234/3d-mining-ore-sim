#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Editor
{
    /// <summary>One-time cleanup of the currently open gameplay scene and obsolete world-swap assets.</summary>
    public static class MiningOverworldOnlyCleanupMenu
    {
        private const string MenuPath = "Mining Simulator/Setup/Remove Underground (Overworld Only)";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string GroundSpawnDataPath = "Assets/GameData/Spawning/OreSpawnData.asset";

        private static readonly string[] UndergroundAssets =
        {
            "Assets/Scripts/Ores/Portal",
            "Assets/Scripts/Ores/Editor/MiningPortalSetupMenu.cs",
            "Assets/Scripts/Ores/Editor/MiningReturnPortalSetupMenu.cs",
            "Assets/Scripts/Ores/Editor/MiningUndergroundSetupMenu.cs",
            "Assets/Scripts/Ores/Editor/MiningWorldSwapAutoSetupMenu.cs",
            "Assets/GameData/Ores/Underground Stone.asset",
            "Assets/GameData/Spawning/UndergroundOreSpawnData.asset",
            "Assets/GameData/Audio/ambience/UnderGround.mp3",
            "Assets/Scenes/SampleScene/NavMesh-UnderGround.asset",
            "Assets/Material/Cave.terrainlayer",
            "Assets/Material/Cave_Dirt_Floor.jpg",
            "Assets/Material/Cave_Rock_Wall.jpg",
            "Assets/Material/Cave_Roof_Ceiling.obj",
            "Assets/Material/Cave_Roof_Ceiling.prefab",
            "Assets/Material/Cave_Wall_Straight.obj",
            "Assets/Material/Cave_Wall_Straight.prefab",
            "Assets/Prefabs/Portal",
            "Assets/portal gun",
            "Assets/FX/portal.fbx"
        };

        [MenuItem(MenuPath)]
        private static void Run()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (Application.isPlaying || !scene.IsValid() || !scene.isLoaded ||
                scene.path != ScenePath)
            {
                Debug.LogError("Open Assets/Scenes/SampleScene.unity in Edit Mode before cleaning Underground.");
                return;
            }

            var groundData = AssetDatabase.LoadAssetAtPath<MiningSimulator.Ores.OreSpawnData>(
                GroundSpawnDataPath);
            if (groundData == null)
            {
                Debug.LogError("The original OreSpawnData.asset is missing. Cleanup stopped to protect Overworld.");
                return;
            }

            GameObject groundOreSystem = FindObject(scene, "Ore System");
            MiningSimulator.Ores.OreSpawner groundSpawner = groundOreSystem != null
                ? groundOreSystem.GetComponent<MiningSimulator.Ores.OreSpawner>() : null;
            if (groundSpawner == null)
            {
                Debug.LogError("Overworld Ore System/OreSpawner was not found. Cleanup stopped.");
                return;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            int removedObjects = 0;
            GameObject underground = FindRoot(scene, "UnderGround") ??
                                     FindRoot(scene, "Underground");
            if (underground != null)
            {
                Undo.DestroyObjectImmediate(underground);
                removedObjects++;
            }

            // The portal visual may be a prefab instance whose scene name is not 'Portal'.
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null) continue;
                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root) ??
                                    string.Empty;
                if (prefabPath.StartsWith("Assets/Prefabs/Portal/", StringComparison.OrdinalIgnoreCase) ||
                    prefabPath.StartsWith("Assets/portal gun/", StringComparison.OrdinalIgnoreCase) ||
                    root.name.Equals("Mining Portal Gate", StringComparison.OrdinalIgnoreCase) ||
                    root.name.Equals("Mining World Areas", StringComparison.OrdinalIgnoreCase))
                {
                    Undo.DestroyObjectImmediate(root);
                    removedObjects++;
                }
            }

            // Existing scenes may contain the portal window and old components on shared
            // GameManager/Canvas objects: remove these without destroying either shared owner.
            foreach (GameObject root in scene.GetRootGameObjects())
                CleanHierarchy(root.transform, ref removedObjects);

            SerializedObject spawner = new SerializedObject(groundSpawner);
            SerializedProperty spawnData = spawner.FindProperty("spawnData");
            if (spawnData != null && spawnData.objectReferenceValue != groundData)
            {
                Undo.RecordObject(groundSpawner, "Restore Overworld ore data");
                spawnData.objectReferenceValue = groundData;
                spawner.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError("Scene could not be saved. Underground assets were left intact.");
                return;
            }

            int deletedAssets = 0;
            foreach (string path in UndergroundAssets)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null &&
                    AssetDatabase.DeleteAsset(path))
                    deletedAssets++;
            }

            // Remove only root documentation/backup names. Never touch the live Unity Library.
            foreach (string file in Directory.GetFiles(projectRoot, "*", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(file);
                if (name.StartsWith("README_", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith("_README.txt", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("Cleanup-Unity-Library-Backups.ps1", StringComparison.OrdinalIgnoreCase) ||
                    (Path.GetExtension(name).Equals(".zip", StringComparison.OrdinalIgnoreCase) &&
                     name.IndexOf("backup", StringComparison.OrdinalIgnoreCase) >= 0))
                    File.Delete(file);
            }

            int backupCount = 0;
            foreach (string directory in Directory.GetDirectories(projectRoot,
                         "Library_backup_*", SearchOption.TopDirectoryOnly))
            {
                Directory.Delete(directory, true);
                backupCount++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"Overworld cleanup complete. Scene objects: {removedObjects}, " +
                      $"Underground asset paths: {deletedAssets}, Library backup folders: {backupCount}. " +
                      "The original ground ore table is restored. Save and reopen the scene to verify.");
        }

        private static void CleanHierarchy(Transform parent, ref int removed)
        {
            for (int index = parent.childCount - 1; index >= 0; index--)
            {
                Transform child = parent.GetChild(index);
                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child.gameObject) ??
                                    string.Empty;
                GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(child.gameObject);
                if (prefabRoot == child.gameObject &&
                    (prefabPath.StartsWith("Assets/Prefabs/Portal/", StringComparison.OrdinalIgnoreCase) ||
                     prefabPath.StartsWith("Assets/portal gun/", StringComparison.OrdinalIgnoreCase)))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    removed++;
                    continue;
                }
                if (child.name.Equals("PortalMaintenancePanel", StringComparison.OrdinalIgnoreCase) ||
                    child.name.Equals("Underground Ore System", StringComparison.OrdinalIgnoreCase) ||
                    child.name.Equals("Under Ground Ore System", StringComparison.OrdinalIgnoreCase) ||
                    child.name.Equals("Portal Slide Transition Overlay", StringComparison.OrdinalIgnoreCase) ||
                    child.name.Equals("Underground Camera Focus", StringComparison.OrdinalIgnoreCase))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    removed++;
                }
                else CleanHierarchy(child, ref removed);
            }

            Component[] components = parent.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null) continue;
                Type type = component.GetType();
                if (type.FullName == "MiningSimulator.Ores.MiningWorldAreaController" ||
                    type.FullName == "MiningSimulator.Ores.MiningPortalSlideTransition" ||
                    type.FullName == "MiningSimulator.Ores.MiningPortalGate" ||
                    type.FullName == "MiningSimulator.Ores.MiningPortalMaintenancePanel")
                {
                    Undo.DestroyObjectImmediate(component);
                    removed++;
                }
            }
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return root;
            return null;
        }

        private static GameObject FindObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] children = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in children)
                    if (child.name == name) return child.gameObject;
            }
            return null;
        }
    }
}
#endif
