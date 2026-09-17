#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiningSimulator.Ores.Editor
{
    /// <summary>Creates the editable middle-click NPC command and its world marker.</summary>
    public static class MiningNpcTargetCommandSetupMenu
    {
        private const string CommandObjectName = "NPC Target Command";
        private const string MarkerObjectName = "Target Marker";

        [MenuItem("Mining Simulator/Setup/Create Or Update NPC Target Command")]
        public static void CreateOrUpdate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("NPC Target Command",
                    "Stop Play Mode before creating the NPC target command.", "OK");
                return;
            }

            MiningNpcTargetCommand command = Object.FindFirstObjectByType<
                MiningNpcTargetCommand>(FindObjectsInactive.Include);
            if (command == null)
            {
                GameObject commandObject = new(CommandObjectName);
                Undo.RegisterCreatedObjectUndo(commandObject, "Create NPC Target Command");
                Transform npcSystem = FindTransformByName("NPC System");
                if (npcSystem != null)
                {
                    commandObject.transform.SetParent(npcSystem, false);
                }
                else
                {
                    SceneManager.MoveGameObjectToScene(commandObject,
                        SceneManager.GetActiveScene());
                }

                command = Undo.AddComponent<MiningNpcTargetCommand>(commandObject);
            }

            SpriteRenderer markerRenderer = EnsureMarker(command.transform);
            MiningGameData gameData = FindFirstAsset<MiningGameData>();
            Undo.RecordObject(command, "Configure NPC Target Command");
            command.ConfigureIfMissing(gameData, markerRenderer);
            EditorUtility.SetDirty(command);
            EditorUtility.SetDirty(markerRenderer);
            EditorSceneManager.MarkSceneDirty(command.gameObject.scene);

            Selection.activeGameObject = command.gameObject;
            EditorGUIUtility.PingObject(command.gameObject);
            EditorUtility.DisplayDialog("NPC Target Command",
                "Middle-click command is ready. Assign your own Sprite to Marker Icon on the " +
                "selected component, then save the scene.", "OK");
        }

        [MenuItem("Mining Simulator/Setup/Create Or Update NPC Target Command", true)]
        private static bool CanCreateOrUpdate() =>
            !EditorApplication.isPlayingOrWillChangePlaymode;

        private static SpriteRenderer EnsureMarker(Transform parent)
        {
            Transform existing = parent.Find(MarkerObjectName);
            GameObject markerObject;
            if (existing == null)
            {
                markerObject = new GameObject(MarkerObjectName, typeof(SpriteRenderer));
                Undo.RegisterCreatedObjectUndo(markerObject, "Create NPC Target Marker");
                markerObject.transform.SetParent(parent, false);
            }
            else
            {
                markerObject = existing.gameObject;
            }

            SpriteRenderer markerRenderer = markerObject.GetComponent<SpriteRenderer>() ??
                                            Undo.AddComponent<SpriteRenderer>(markerObject);
            Undo.RecordObject(markerRenderer, "Configure NPC Target Marker");
            markerRenderer.sortingOrder = 500;
            markerRenderer.enabled = false;
            return markerRenderer;
        }

        private static Transform FindTransformByName(string objectName)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform candidate in transforms)
            {
                if (candidate != null && candidate.name == objectName)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static T FindFirstAsset<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (string guid in guids)
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    return asset;
                }
            }

            return null;
        }
    }
}
#endif
