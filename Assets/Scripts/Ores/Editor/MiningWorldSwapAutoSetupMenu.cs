#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Editor
{
    /// <summary>
    /// Wires the designer-authored Ground and UnderGround worlds without creating,
    /// moving, or duplicating gameplay objects.
    /// </summary>
    public static class MiningWorldSwapAutoSetupMenu
    {
        private const string MenuPath = "Mining Simulator/Setup/Auto Setup Ground + Underground Worlds";

        [MenuItem(MenuPath)]
        public static void AutoSetupWorlds()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("World setup", "Exit Play Mode before running this tool.", "OK");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                EditorUtility.DisplayDialog("World setup", "Open the gameplay scene first.", "OK");
                return;
            }

            GameObject groundRoot = FindRoot(scene, "ground");
            GameObject undergroundRoot = FindRoot(scene, "underground");
            List<OreSpawner> spawners = FindSceneComponents<OreSpawner>(scene);
            OreSpawner groundSpawner = FindGroundSpawner(spawners, groundRoot, undergroundRoot);
            OreSpawner undergroundSpawner = FindUndergroundSpawner(spawners, undergroundRoot);

            StringBuilder missing = new StringBuilder();
            if (groundRoot == null) missing.AppendLine("- Ground root (name it Ground)");
            if (undergroundRoot == null) missing.AppendLine("- UnderGround root (UnderGround or Underground)");
            if (groundSpawner == null) missing.AppendLine("- Ground OreSpawner");
            if (undergroundSpawner == null) missing.AppendLine("- UnderGround OreSpawner");
            if (groundSpawner != null && groundSpawner == undergroundSpawner)
                missing.AppendLine("- Ground and UnderGround must use two different OreSpawner components");

            if (missing.Length > 0)
            {
                EditorUtility.DisplayDialog(
                    "World setup stopped",
                    "The tool does not create or duplicate scene objects. Add/fix these existing objects, then run it again:\n\n" + missing,
                    "OK");
                return;
            }

            MiningWorldAreaController controller = FindFirstSceneComponent<MiningWorldAreaController>(scene);
            GameObject gameManager = FindSceneObject(scene, "gamemanager");
            if (controller == null && gameManager == null)
            {
                EditorUtility.DisplayDialog(
                    "World setup stopped",
                    "No MiningWorldAreaController or GameManager was found. The tool will not create a new manager.",
                    "OK");
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Auto Setup Ground and Underground Worlds");

            if (controller == null)
                controller = Undo.AddComponent<MiningWorldAreaController>(gameManager);

            Undo.RecordObject(controller, "Wire world area controller");
            SerializedObject controllerObject = new SerializedObject(controller);
            SetReference(controllerObject, "groundSpawner", groundSpawner);
            SetReference(controllerObject, "undergroundSpawner", undergroundSpawner);
            SetReference(controllerObject, "groundEnvironment", groundRoot);
            SetReference(controllerObject, "undergroundEnvironment", undergroundRoot);
            controllerObject.ApplyModifiedProperties();
            controller.PrepareEditorHierarchy();
            EditorUtility.SetDirty(controller);

            int groundMinerCount = 0;
            int undergroundMinerCount = 0;
            int unchangedMinerCount = 0;
            List<MiningNpc> miners = FindSceneComponents<MiningNpc>(scene);
            foreach (MiningNpc miner in miners)
            {
                OreSpawner target = null;
                if (IsChildOf(miner.transform, undergroundRoot.transform))
                {
                    target = undergroundSpawner;
                    undergroundMinerCount++;
                }
                else if (IsChildOf(miner.transform, groundRoot.transform))
                {
                    target = groundSpawner;
                    groundMinerCount++;
                }
                else
                {
                    unchangedMinerCount++;
                }

                if (target == null)
                    continue;

                Undo.RecordObject(miner, "Assign miner world spawner");
                SerializedObject minerObject = new SerializedObject(miner);
                SetReference(minerObject, "oreSpawner", target);
                minerObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(miner);
            }

            // Reuses the project's existing setup command. It bakes the existing
            // Ground and UnderGround surfaces; it does not create a new world.
            MiningNavMeshSetupMenu.EnableNavMeshPathfinding();

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeObject = controller.gameObject;
            EditorGUIUtility.PingObject(controller.gameObject);

            string groundData = groundSpawner.SpawnData != null ? groundSpawner.SpawnData.name : "MISSING";
            string undergroundData = undergroundSpawner.SpawnData != null ? undergroundSpawner.SpawnData.name : "MISSING";
            StringBuilder warnings = new StringBuilder();
            if (groundSpawner.SpawnData == null)
                warnings.AppendLine("\nWARNING: Ground OreSpawner has no OreSpawnData.");
            if (undergroundSpawner.SpawnData == null)
                warnings.AppendLine("\nWARNING: UnderGround OreSpawner has no OreSpawnData.");
            if (groundSpawner.SpawnData != null && groundSpawner.SpawnData == undergroundSpawner.SpawnData)
                warnings.AppendLine("\nWARNING: Both worlds use the same OreSpawnData. Assign separate data assets if their ores should differ.");
            if (unchangedMinerCount > 0)
                warnings.AppendLine($"\nNOTE: {unchangedMinerCount} miner(s) outside Ground/UnderGround were left unchanged.");

            EditorUtility.DisplayDialog(
                "World setup complete",
                $"Ground spawner: {groundSpawner.name}\n" +
                $"Ground data: {groundData}\n" +
                $"UnderGround spawner: {undergroundSpawner.name}\n" +
                $"UnderGround data: {undergroundData}\n\n" +
                $"Ground miners wired: {groundMinerCount}\n" +
                $"UnderGround miners wired: {undergroundMinerCount}\n" +
                "Existing NavMesh surfaces were prepared/baked.\n" +
                "No platform, spawner, NPC, or manager object was duplicated." +
                warnings +
                "\nSave the scene with Ctrl+S.",
                "OK");
        }

        private static void SetReference(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null && property.objectReferenceValue != value)
                property.objectReferenceValue = value;
        }

        private static GameObject FindRoot(Scene scene, string normalizedName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (Normalize(root.name) == normalizedName)
                    return root;
            }

            return null;
        }

        private static GameObject FindSceneObject(Scene scene, string normalizedName)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform candidate in transforms)
            {
                if (candidate.gameObject.scene == scene && Normalize(candidate.name) == normalizedName)
                    return candidate.gameObject;
            }

            return null;
        }

        private static OreSpawner FindUndergroundSpawner(List<OreSpawner> spawners, GameObject undergroundRoot)
        {
            if (undergroundRoot != null)
            {
                foreach (OreSpawner spawner in spawners)
                {
                    if (IsChildOf(spawner.transform, undergroundRoot.transform))
                        return spawner;
                }
            }

            foreach (OreSpawner spawner in spawners)
            {
                if (Normalize(spawner.name).Contains("underground"))
                    return spawner;
            }

            return null;
        }

        private static OreSpawner FindGroundSpawner(
            List<OreSpawner> spawners,
            GameObject groundRoot,
            GameObject undergroundRoot)
        {
            if (groundRoot != null)
            {
                foreach (OreSpawner spawner in spawners)
                {
                    if (IsChildOf(spawner.transform, groundRoot.transform))
                        return spawner;
                }
            }

            foreach (OreSpawner spawner in spawners)
            {
                if (undergroundRoot != null && IsChildOf(spawner.transform, undergroundRoot.transform))
                    continue;

                string name = Normalize(spawner.name);
                if (name == "oresystem" || name == "orespawner" || name == "groundorespawner")
                    return spawner;
            }

            foreach (OreSpawner spawner in spawners)
            {
                if (undergroundRoot != null && IsChildOf(spawner.transform, undergroundRoot.transform))
                    continue;
                if (!Normalize(spawner.name).Contains("underground"))
                    return spawner;
            }

            return null;
        }

        private static bool IsChildOf(Transform candidate, Transform root)
        {
            return candidate != null && root != null && (candidate == root || candidate.IsChildOf(root));
        }

        private static T FindFirstSceneComponent<T>(Scene scene) where T : Component
        {
            List<T> components = FindSceneComponents<T>(scene);
            return components.Count > 0 ? components[0] : null;
        }

        private static List<T> FindSceneComponents<T>(Scene scene) where T : Component
        {
            T[] all = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            List<T> result = new List<T>(all.Length);
            foreach (T component in all)
            {
                if (component != null && component.gameObject.scene == scene)
                    result.Add(component);
            }

            return result;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            StringBuilder normalized = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                if (char.IsLetterOrDigit(character))
                    normalized.Append(char.ToLowerInvariant(character));
            }

            return normalized.ToString();
        }
    }
}
#endif
