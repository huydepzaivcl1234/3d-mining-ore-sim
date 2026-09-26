#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor.Animations;

namespace MiningSimulator.Editor
{
    [InitializeOnLoad]
    public static class MiningRestoreLocomotion
    {
        static MiningRestoreLocomotion()
        {
            EditorApplication.delayCall += RepairOnce;
            EditorApplication.delayCall += ConfigureAttackSpeed;
        }
        private static void ConfigureAttackSpeed()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                "Assets/Animations/MiningCombat/Player Combat.controller");
            if (controller == null) return;
            bool hasSpeed = false;
            foreach (var parameter in controller.parameters)
                if (parameter.name == "AttackSpeed")
                {
                    if (parameter.type != AnimatorControllerParameterType.Float)
                    {
                        Debug.LogError("AttackSpeed must be a Float in the player Animator.", controller);
                        return;
                    }
                    hasSpeed = true;
                }
            bool changed = false;
            if (!hasSpeed)
            {
                controller.AddParameter(new AnimatorControllerParameter {
                    name = "AttackSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
                changed = true;
            }
            foreach (var layer in controller.layers)
                if (layer.name == "combat layer")
                    foreach (var child in layer.stateMachine.states)
                        if (child.state.name == "Attack" &&
                            (!child.state.speedParameterActive || child.state.speedParameter != "AttackSpeed"))
                        {
                            child.state.speedParameter = "AttackSpeed";
                            child.state.speedParameterActive = true;
                            EditorUtility.SetDirty(child.state);
                            changed = true;
                        }
            if (changed)
            {
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }
        }
        private static void RepairOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            // Do not change Animator/components during every domain reload.
            // A stale Inspector can still hold targets from before recompilation.
            RepairInspector();
        }
        [MenuItem("Mining Simulator/Fixes/Refresh Stale Inspector")]
        private static void RepairInspector()
        {
            Selection.objects = new Object[0];
            ActiveEditorTracker.sharedTracker.isLocked = false;
            ActiveEditorTracker.sharedTracker.ForceRebuild();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
        [MenuItem("Mining Simulator/Fixes/Connect Player Combat Input")]
        private static void BindPlayer()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            RepairInspector();
            var authored = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Animations/MiningCombat/Player Combat.controller");
            if (authored == null) return;
            foreach (var input in Object.FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (input.gameObject.name != "Player" || !input.gameObject.scene.IsValid()) continue;
                var animator = input.GetComponentInChildren<Animator>(true);
                if (animator == null) continue;
                string path = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
                if (animator.runtimeAnimatorController != authored &&
                    path != "Assets/BroAudio/Samples/Demo/Character/Animations/StarterAssetsThirdPerson.controller") continue;
                Undo.RecordObject(animator, "Connect player Animator");
                animator.runtimeAnimatorController = authored;
                var bridge = input.GetComponent<PlayerCombatInput>();
                if (bridge == null) bridge = Undo.AddComponent<PlayerCombatInput>(input.gameObject);
                var settings = new SerializedObject(bridge);
                settings.FindProperty("animator").objectReferenceValue = animator;
                settings.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(input.gameObject.scene);
                Debug.Log("E/right-click input connected. Save the scene.", input);
            }
        }
    }
}
#endif
