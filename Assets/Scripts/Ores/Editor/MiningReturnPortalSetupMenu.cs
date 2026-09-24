#if UNITY_EDITOR
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Editor
{
    public static class MiningReturnPortalSetupMenu
    {
        [MenuItem("Mining Simulator/Setup/Create Return Portal From Selected Gate")]
        private static void CreateReturnPortal()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Exit Play Mode before creating the return portal.");
                return;
            }

            MiningPortalGate source = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<MiningPortalGate>()
                : null;
            MiningWorldAreaController controller =
                Object.FindFirstObjectByType<MiningWorldAreaController>(FindObjectsInactive.Include);
            if (source == null || controller == null)
            {
                EditorUtility.DisplayDialog("Return portal",
                    "Select your existing Ground portal with MiningPortalGate, and assign MiningWorldAreaController in the scene.", "OK");
                return;
            }

            SerializedObject world = new SerializedObject(controller);
            GameObject root = world.FindProperty("undergroundEnvironment").objectReferenceValue as GameObject;
            Transform focus = world.FindProperty("undergroundCameraFocus").objectReferenceValue as Transform;
            if (root == null || focus == null || source.transform.IsChildOf(root.transform))
            {
                EditorUtility.DisplayDialog("Return portal",
                    "Assign the UnderGround root and Underground Camera Focus on MiningWorldAreaController; select the Ground portal.", "OK");
                return;
            }

            GameObject copy = Object.Instantiate(source.gameObject, root.transform);
            copy.name = "Return Portal (Underground)";
            copy.transform.position = focus.position;
            copy.transform.rotation = source.transform.rotation;
            Undo.RegisterCreatedObjectUndo(copy, "Create Underground Return Portal");

            MiningPortalGate gate = copy.GetComponent<MiningPortalGate>();
            SerializedObject gateData = new SerializedObject(gate);
            gateData.FindProperty("destination").enumValueIndex = (int)MiningWorldArea.Ground;
            gateData.FindProperty("areaController").objectReferenceValue = controller;
            gateData.FindProperty("maintenancePanel").objectReferenceValue = null;
            gateData.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(copy.scene);
            Selection.activeGameObject = copy;
            EditorGUIUtility.PingObject(copy);
            Debug.Log("Return portal placed at Underground Camera Focus. Move it to your chosen location in the scene and save. Its destination is Ground.", copy);
        }

        [MenuItem("Mining Simulator/Setup/Create Return Portal From Selected Gate", true)]
        private static bool CanCreateReturnPortal()
        {
            return !Application.isPlaying && Selection.activeGameObject != null &&
                   Selection.activeGameObject.GetComponent<MiningPortalGate>() != null;
        }
    }
}
#endif