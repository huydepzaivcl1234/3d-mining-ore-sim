#if UNITY_EDITOR
using System;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Editor
{
    public static class MiningThirdPersonSetupMenu
    {
        private const string MenuPath = "Mining Simulator/Setup/Third Person Player And Camera";
        private const string StarterPrefabPath =
            "Assets/Starter Assets/Runtime/ThirdPersonController/Prefabs/PlayerArmature.prefab";

        [MenuItem(MenuPath)]
        private static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid() ||
                !scene.isLoaded)
            {
                Debug.LogWarning("Exit Play Mode and open the gameplay scene before setting up the player.");
                return;
            }

            Camera camera = FindMainCamera(scene);
            if (camera == null)
            {
                Debug.LogError("This scene needs its Main Camera before third person setup.");
                return;
            }

            Type starterController = FindStarterController();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StarterPrefabPath);
            if (starterController == null || prefab == null)
            {
                Debug.LogError("The Starter Assets PlayerArmature prefab is missing. Update the project to the latest main branch and let Unity import 'First Person + Third Person | Character Controllers' before running third person setup.");
                return;
            }
            GameObject player = FindScenePlayer(scene, starterController);
            if (player == null)
            {
                player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                Undo.RegisterCreatedObjectUndo(player, "Add Starter Assets player");
                player.name = "Player";
                player.transform.position = FindGroundSpawn(scene);
            }

            // The game clicks ores and opens UI with a visible cursor. Starter Assets
            // otherwise locks that cursor and rotates its unused Cinemachine target.
            Type inputType = starterController.Assembly.GetType("StarterAssets.StarterAssetsInputs");
            Component starterInputs = inputType != null
                ? player.GetComponentInChildren(inputType, true) : null;
            if (starterInputs != null)
            {
                SerializedObject inputSettings = new(starterInputs);
                inputSettings.FindProperty("cursorLocked").boolValue = false;
                inputSettings.FindProperty("cursorInputForLook").boolValue = false;
                inputSettings.FindProperty("look").vector2Value = Vector2.zero;
                inputSettings.ApplyModifiedProperties();
            }

            MiningOrbitCamera orbit = camera.GetComponent<MiningOrbitCamera>();
            if (orbit == null) orbit = Undo.AddComponent<MiningOrbitCamera>(camera.gameObject);
            SerializedObject cameraSettings = new(orbit);
            SerializedProperty target = cameraSettings.FindProperty("followTarget");
            bool firstSetup = target.objectReferenceValue == null ||
                              target.objectReferenceValue != player.transform;
            target.objectReferenceValue = player.transform;
            cameraSettings.FindProperty("controlledCamera").objectReferenceValue = camera;
            if (firstSetup)
            {
                cameraSettings.FindProperty("followOffset").vector3Value =
                    new Vector3(0f, 1.5f, 0f);
                cameraSettings.FindProperty("followDistance").floatValue = 4.5f;
                cameraSettings.FindProperty("followPitch").floatValue = 18f;
            }
            cameraSettings.ApplyModifiedProperties();

            // Assign the active camera explicitly because legacy scenes also have a
            // disabled copy of the orbit component under UI Systems.
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (MiningUiPanelCoordinator panel in
                    root.GetComponentsInChildren<MiningUiPanelCoordinator>(true))
                {
                    SerializedObject settings = new(panel);
                    settings.FindProperty("orbitCamera").objectReferenceValue = orbit;
                    settings.ApplyModifiedProperties();
                }

            // The UI Systems object in older scenes also carries an orbit component.
            // Only the component on the actual Main Camera may drive its transform.
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (MiningOrbitCamera extra in root.GetComponentsInChildren<MiningOrbitCamera>(true))
                {
                    if (extra == orbit || !extra.enabled) continue;
                    Undo.RecordObject(extra, "Disable duplicate camera controller");
                    extra.enabled = false;
                    EditorUtility.SetDirty(extra);
                }

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = player;
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log("Starter Assets PlayerArmature added. WASD moves, Shift runs, Space jumps, right mouse turns the camera; camera collision and wall line-of-sight stay active. Save the scene after positioning your player.", player);
        }

        private static Camera FindMainCamera(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Camera candidate in root.GetComponentsInChildren<Camera>(true))
                    if (candidate.CompareTag("MainCamera")) return candidate;
            return null;
        }

        private static Type FindStarterController()
        {
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType("StarterAssets.ThirdPersonController", false);
                if (type != null && typeof(Component).IsAssignableFrom(type)) return type;
            }
            return null;
        }

        private static GameObject FindScenePlayer(Scene scene, Type starterController)
        {
            GameObject selected = Selection.activeGameObject;
            if (selected != null && selected.scene == scene)
            {
                GameObject root = selected.transform.root.gameObject;
                if (HasPlayerController(root, starterController)) return root;
            }
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "Player" && HasPlayerController(root, starterController))
                    return root;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (HasPlayerController(root, starterController))
                    return root;
            return null;
        }

        private static bool HasPlayerController(GameObject root, Type starterController) =>
            root.GetComponentInChildren(starterController, true) != null;

        private static Vector3 FindGroundSpawn(Scene scene)
        {
            Vector3 position = Vector3.zero;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                MiningOrbitCamera orbit = root.GetComponentInChildren<MiningOrbitCamera>(true);
                if (orbit == null) continue;
                position = orbit.DefaultFocusPoint;
                break;
            }
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Terrain terrain = root.GetComponentInChildren<Terrain>(true);
                if (terrain == null) continue;
                Bounds bounds = terrain.terrainData.bounds;
                Vector3 local = position - terrain.transform.position;
                if (local.x >= bounds.min.x && local.x <= bounds.max.x &&
                    local.z >= bounds.min.z && local.z <= bounds.max.z)
                {
                    position.y = terrain.SampleHeight(position) +
                                 terrain.transform.position.y + 0.1f;
                    break;
                }
            }
            return position;
        }
    }
}
#endif
