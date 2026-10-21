//______________________________________________
// Implemented by using the GPT AI
// Designed, tested and published by:
// ALIyerEdon@gmail.com
// https://assetstore.unity.com/publishers/23606
//______________________________________________


#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MissingScriptsFinder
{
    public class MissingScriptsFinderWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private List<GameObject> objectsWithMissingScripts = new List<GameObject>();
        private bool includeInactive = true;

        [MenuItem("Tools/Project Analysis/Missing Scripts Finder")]
        public static void ShowWindow()
        {
            var window = GetWindow<MissingScriptsFinderWindow>("Missing Scripts");
            window.minSize = new Vector2(380, 300);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Missing Scripts Cleaner", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "This tool searches the current scene for all GameObjects with Missing Scripts and allows you to remove them in bulk.",
                MessageType.Info
            );

            EditorGUILayout.Space(5);
            includeInactive = EditorGUILayout.Toggle(
                "Search Inactive Objects",
                includeInactive
            );

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🔍 Find in Scene", GUILayout.Height(28)))
            {
                FindMissingScriptsInScene();
            }

            EditorGUI.BeginDisabledGroup(objectsWithMissingScripts.Count == 0);

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);

            if (GUILayout.Button("🗑️ Remove All", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog(
                    "Confirm Removal",
                    $"Are you sure you want to remove all missing scripts from {objectsWithMissingScripts.Count} objects?",
                    "Yes",
                    "Cancel"))
                {
                    RemoveAllMissingScripts();
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField(
                $"Found Objects: {objectsWithMissingScripts.Count}",
                EditorStyles.boldLabel
            );

            EditorGUILayout.Space(3);

            // Display the list of GameObjects with Missing Scripts
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUI.skin.box);

            if (objectsWithMissingScripts.Count > 0)
            {
                for (int i = 0; i < objectsWithMissingScripts.Count; i++)
                {
                    var go = objectsWithMissingScripts[i];

                    if (go == null)
                        continue;

                    EditorGUILayout.BeginHorizontal();

                    // Click the field to select and focus the object in the Hierarchy
                    EditorGUILayout.ObjectField(
                        go,
                        typeof(GameObject),
                        true
                    );

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = go;
                        EditorGUIUtility.PingObject(go);
                    }

                    if (GUILayout.Button("Remove", GUILayout.Width(50)))
                    {
                        RemoveMissingScriptsFromObject(go);
                        objectsWithMissingScripts.RemoveAt(i);
                        GUIUtility.ExitGUI();
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField(
                    "No missing scripts were found, or the search has not been performed yet.",
                    EditorStyles.miniLabel
                );
            }

            EditorGUILayout.EndScrollView();
        }

        private void FindMissingScriptsInScene()
        {
            objectsWithMissingScripts.Clear();

            var activeScene = SceneManager.GetActiveScene();
            var rootObjects = activeScene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                CheckRecursively(root);
            }

            Debug.Log(
                $"[MissingScriptsFinder] Search completed. " +
                $"{objectsWithMissingScripts.Count} objects with missing components were found."
            );
        }

        private void CheckRecursively(GameObject target)
        {
            if (!includeInactive && !target.activeInHierarchy)
                return;

            // Count the number of missing components
            int missingCount =
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(target);

            if (missingCount > 0)
            {
                objectsWithMissingScripts.Add(target);
            }

            // Traverse child objects
            for (int i = 0; i < target.transform.childCount; i++)
            {
                CheckRecursively(target.transform.GetChild(i).gameObject);
            }
        }

        private void RemoveMissingScriptsFromObject(GameObject go)
        {
            Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");

            int removedCount =
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

            if (removedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(go.scene);

                Debug.Log(
                    $"{removedCount} missing scripts were removed from '{go.name}'.",
                    go
                );
            }
        }

        private void RemoveAllMissingScripts()
        {
            int totalRemoved = 0;
            int affectedObjects = 0;

            Undo.SetCurrentGroupName("Remove All Missing Scripts In Scene");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (var go in objectsWithMissingScripts)
            {
                if (go == null)
                    continue;

                Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");

                int count =
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

                if (count > 0)
                {
                    totalRemoved += count;
                    affectedObjects++;

                    EditorSceneManager.MarkSceneDirty(go.scene);
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            objectsWithMissingScripts.Clear();

            Debug.Log(
                $"[MissingScriptsFinder] Operation completed: " +
                $"{totalRemoved} missing scripts were removed from " +
                $"{affectedObjects} objects in total."
            );
        }
    }
}
#endif
