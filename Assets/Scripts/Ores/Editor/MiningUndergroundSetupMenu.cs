#if UNITY_EDITOR
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Editor
{
    /// <summary>
    /// Adds the Underground controller to the currently open gameplay scene without editing the
    /// sample scene. The generated cave geometry remains runtime-owned, while the controller and
    /// named child objects stay visible and editable in the Hierarchy.
    /// </summary>
    public static class MiningUndergroundSetupMenu
    {
        private const string MenuPath = "Mining Simulator/Setup/Create Underground World In Open Scene";
        private const string CopyMenuPath =
            "Mining Simulator/Setup/Create Gameplay Scene Copy With Underground";
        private const string GameManagerName = "GameManager";

        [MenuItem(CopyMenuPath)]
        public static void CreateGameplaySceneCopyWithUnderground()
        {
            Scene sourceScene = SceneManager.GetActiveScene();
            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                EditorUtility.DisplayDialog("Underground Setup",
                    "Open a gameplay scene first, then run this command again.", "OK");
                return;
            }

            string defaultName = sourceScene.name == "SampleScene"
                ? "MiningGameplay"
                : sourceScene.name + "_Gameplay";
            string copyPath = EditorUtility.SaveFilePanelInProject(
                "Create Gameplay Scene Copy", defaultName, "unity",
                "Choose where to save the new gameplay scene.");
            if (string.IsNullOrEmpty(copyPath))
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(copyPath) != null)
            {
                EditorUtility.DisplayDialog("Underground Setup",
                    $"A scene already exists at:\n{copyPath}\n\nChoose another name.", "OK");
                return;
            }

            // saveAsCopy preserves the protected source scene and also includes any current
            // in-memory edits without changing the source scene's asset path.
            if (!EditorSceneManager.SaveScene(sourceScene, copyPath, true))
            {
                EditorUtility.DisplayDialog("Underground Setup",
                    "Unity could not create the gameplay scene copy.", "OK");
                return;
            }

            AssetDatabase.Refresh();
            Scene copiedScene = EditorSceneManager.OpenScene(copyPath, OpenSceneMode.Single);
            if (!copiedScene.IsValid())
            {
                EditorUtility.DisplayDialog("Underground Setup",
                    "The gameplay scene copy was created, but Unity could not open it.", "OK");
                return;
            }

            CreateUndergroundWorldInOpenScene();
            EditorSceneManager.SaveScene(copiedScene);
        }

        [MenuItem(MenuPath)]
        public static void CreateUndergroundWorldInOpenScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                EditorUtility.DisplayDialog("Underground Setup",
                    "Open your gameplay scene first, then run this setup command again.", "OK");
                return;
            }
            if (activeScene.name == "SampleScene")
            {
                EditorUtility.DisplayDialog("Underground Setup",
                    "SampleScene is protected. Open your own gameplay scene and run this command there.",
                    "OK");
                return;
            }

            MiningWorldAreaController controller = Object.FindFirstObjectByType<MiningWorldAreaController>(
                FindObjectsInactive.Include);
            if (controller == null)
            {
                GameObject owner = GameObject.Find(GameManagerName);
                if (owner == null)
                {
                    owner = new GameObject(GameManagerName);
                    Undo.RegisterCreatedObjectUndo(owner, "Create Underground GameManager");
                }

                controller = Undo.AddComponent<MiningWorldAreaController>(owner);
            }

            Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Create Underground Hierarchy");
            CreateNamedChild(controller, "Underground Environment");
            GameObject undergroundSystem = CreateNamedChild(controller, "Underground Ore System");
            if (undergroundSystem.GetComponent<OreSpawner>() == null)
            {
                Undo.AddComponent<OreSpawner>(undergroundSystem);
            }
            controller.PrepareEditorHierarchy();

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Selection.activeGameObject = controller.gameObject;
            EditorGUIUtility.PingObject(controller.gameObject);
            SceneView.lastActiveSceneView?.FrameSelected();

            EditorUtility.DisplayDialog("Underground Setup",
                "Underground hierarchy is ready. Select MiningWorldAreaController to edit " +
                "the area size, center, rebirths and coin requirement.", "OK");
        }

        private static GameObject CreateNamedChild(MiningWorldAreaController controller, string name)
        {
            Transform existing = controller.transform.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject child = new(name);
            Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
            child.transform.SetParent(controller.transform, false);
            child.SetActive(false);
            return child;
        }
    }
}
#endif
