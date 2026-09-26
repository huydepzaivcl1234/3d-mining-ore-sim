#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Editor
{
    [InitializeOnLoad]
    public static class MiningRestoreLocomotion
    {
        static MiningRestoreLocomotion()
        {
            EditorApplication.delayCall += Restore;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) Restore();
            };
        }

        private static void Restore()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            const string originalPath = "Assets/BroAudio/Samples/Demo/Character/Animations/StarterAssetsThirdPerson.controller";
            var original = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(originalPath);
            // Strip only layers created by the removed combat migration. Keep
            // the copied base locomotion intact for saved scene references.
            foreach (string guid in AssetDatabase.FindAssets("t:AnimatorController",
                new[] { "Assets/Animations/MiningCombat" }))
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (controller == null) continue;
                bool changed = false;
                for (int i = controller.layers.Length - 1; i >= 0; i--)
                    if (controller.layers[i].name == "Combat Full Body" ||
                        controller.layers[i].name == "Combat Upper Body")
                    {
                        controller.RemoveLayer(i);
                        changed = true;
                    }
                if (!changed) continue;
                controller.parameters = controller.parameters.Where(p =>
                    p.name != "CombatActive" && p.name != "CombatAction").ToArray();
                EditorUtility.SetDirty(controller);
            }
            foreach (var animator in Resources.FindObjectsOfTypeAll<Animator>())
            {
                if (EditorUtility.IsPersistent(animator) || !animator.gameObject.scene.IsValid()) continue;
                string path = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
                if (!path.StartsWith("Assets/Animations/MiningCombat/") || original == null) continue;
                Undo.RecordObject(animator, "Restore original locomotion");
                animator.runtimeAnimatorController = original;
                EditorUtility.SetDirty(animator);
                EditorSceneManager.MarkSceneDirty(animator.gameObject.scene);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
#endif

