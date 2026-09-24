#if UNITY_EDITOR
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Editor
{
    /// <summary>Creates only an editable portal visual under the selected scene gate.</summary>
    public static class ToxicSlimePortalSetupMenu
    {
        private const string ShaderPath = "Assets/MiningPortalVortex/ToxicSlimePortal.shader";
        private const string MaterialPath = "Assets/MiningPortalVortex/ToxicSlimePortal.mat";
        private const string ChildName = "Toxic Slime Portal Visual";

        [MenuItem("Mining Simulator/Portal/Add Toxic Slime Visual to Selected Gate")]
        private static void CreateVisual()
        {
            Transform gate = Selection.activeTransform;
            if (gate == null || !gate.gameObject.scene.IsValid())
            {
                EditorUtility.DisplayDialog("Portal visual", "Select your Mining Portal Gate object in the scene Hierarchy first.", "OK");
                return;
            }

            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
            {
                EditorUtility.DisplayDialog("Portal visual", "Missing shader at " + ShaderPath + ". Copy the complete ZIP Assets folder into your project.", "OK");
                return;
            }

            Transform existing = gate.Find(ChildName);
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "ToxicSlimePortal" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else if (material.shader != shader)
            {
                EditorUtility.DisplayDialog("Portal visual", "The existing ToxicSlimePortal.mat uses a different shader. Assign the provided shader to it first; the tool did not overwrite your material.", "OK");
                return;
            }

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Undo.RegisterCreatedObjectUndo(visual, "Create Toxic Slime Portal Visual");
            visual.name = ChildName;
            Undo.SetTransformParent(visual.transform, gate, "Parent Toxic Slime Portal Visual");
            visual.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(2.4f, 2.4f, 1f);
            FaceCamera(visual.transform, Camera.main);

            Collider quadCollider = visual.GetComponent<Collider>();
            if (quadCollider != null) Undo.DestroyObjectImmediate(quadCollider);

            visual.GetComponent<MeshRenderer>().sharedMaterial = material;
            Undo.AddComponent<ToxicSlimePortalVisual>(visual);
            EditorSceneManager.MarkSceneDirty(gate.gameObject.scene);
            Selection.activeGameObject = visual;
            EditorGUIUtility.PingObject(visual);
        }

        [MenuItem("Mining Simulator/Portal/Face Selected Toxic Visual Toward Main Camera")]
        private static void FaceSelectedVisual()
        {
            Transform visual = Selection.activeTransform;
            Camera camera = Camera.main;
            if (visual == null || camera == null)
            {
                EditorUtility.DisplayDialog("Portal visual",
                    "Select Toxic Slime Portal Visual and ensure a camera tagged MainCamera exists in the scene.", "OK");
                return;
            }

            Undo.RecordObject(visual, "Face Toxic Portal Visual Toward Camera");
            FaceCamera(visual, camera);
            EditorSceneManager.MarkSceneDirty(visual.gameObject.scene);
        }

        [MenuItem("Mining Simulator/Portal/Face Selected Toxic Visual Toward Main Camera", true)]
        private static bool ValidateFaceSelectedVisual()
        {
            return Selection.activeTransform != null &&
                   Selection.activeTransform.name == ChildName &&
                   Selection.activeGameObject.scene.IsValid();
        }

        private static void FaceCamera(Transform visual, Camera camera)
        {
            if (camera == null) return;
            Vector3 towardCamera = camera.transform.position - visual.position;
            if (towardCamera.sqrMagnitude > 0.0001f)
                visual.rotation = Quaternion.LookRotation(towardCamera.normalized, Vector3.up);
        }

        [MenuItem("Mining Simulator/Portal/Add Toxic Slime Visual to Selected Gate", true)]
        private static bool ValidateCreateVisual()
        {
            return Selection.activeTransform != null && Selection.activeGameObject.scene.IsValid();
        }
    }
}
#endif
