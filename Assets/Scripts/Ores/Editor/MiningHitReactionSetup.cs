#if UNITY_EDITOR
using System.Linq;
using MiningSimulator.Ores;
using StarterAssets;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiningSimulator.Editor
{
    public static class MiningHitReactionSetup
    {
        [MenuItem("Mining Simulator/Setup/Player Hit Reaction (Keep Attacking)")]
        private static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Leave Play Mode before setting up the hit reaction.");
                return;
            }
            var player = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<ThirdPersonController>() : null;
            if (player == null) player = Object.FindAnyObjectByType<ThirdPersonController>();
            if (player == null || !player.gameObject.scene.IsValid())
            {
                Debug.LogError("Select the scene Player with ThirdPersonController first.");
                return;
            }
            var animator = player.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController is not AnimatorController source)
            {
                Debug.LogError("Player needs an Animator Controller, not an override controller.", player);
                return;
            }
            var hitParameter = source.parameters.FirstOrDefault(item => item.name == "Hit");
            if (hitParameter != null && hitParameter.type != AnimatorControllerParameterType.Trigger)
            {
                Debug.LogError("The existing Hit parameter must be a Trigger. No changes made.", source);
                return;
            }
            var clip = AssetDatabase.LoadAllAssetsAtPath(
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatDamage01.fbx")
                .OfType<AnimationClip>().FirstOrDefault(item => !item.name.StartsWith("__preview"));
            if (clip == null)
            {
                Debug.LogError("HumanM@CombatDamage01 was not found. No changes made.");
                return;
            }

            // Preserve the currently assigned Death/combat/locomotion graph.
            // Reuse the assigned hit controller on repeat runs; do not replace
            // it with a stale previous copy of another controller.
            var controller = source;
            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (!source.layers.Any(item => item.name == "Hit Reaction"))
            {
                if (string.IsNullOrEmpty(sourcePath) || !sourcePath.EndsWith(".controller"))
                {
                    Debug.LogError("Save the source Animator Controller as an asset first.");
                    return;
                }
                string path = AssetDatabase.GenerateUniqueAssetPath(
                    sourcePath.Substring(0, sourcePath.Length - ".controller".Length) + " Hit Reaction.controller");
                if (!AssetDatabase.CopyAsset(sourcePath, path))
                {
                    Debug.LogError("Could not copy the Player controller.");
                    return;
                }
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            }
            Undo.RegisterCompleteObjectUndo(controller, "Configure Player Hit Reaction");
            if (!controller.parameters.Any(item => item.name == "Hit"))
                controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

            if (!controller.layers.Any(item => item.name == "Hit Reaction"))
            {
                var mask = new AvatarMask { name = "Hit Reaction Upper Body" };
                for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                    mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
                mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
                AssetDatabase.AddObjectToAsset(mask, controller);
                Undo.RegisterCreatedObjectUndo(mask, "Create Hit Reaction mask");

                controller.AddLayer("Hit Reaction");
                var layers = controller.layers;
                var layer = layers[layers.Length - 1];
                layer.defaultWeight = 0f;
                layer.avatarMask = mask;
                layer.blendingMode = AnimatorLayerBlendingMode.Override;
                layer.iKPass = false;
                var machine = layer.stateMachine;
                var empty = machine.AddState("Empty", new Vector3(220f, 60f));
                empty.writeDefaultValues = false;
                machine.defaultState = empty;
                var hit = machine.AddState("HitReaction", new Vector3(460f, 60f));
                hit.motion = clip;
                hit.writeDefaultValues = false;
                var enter = machine.AddAnyStateTransition(hit);
                enter.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
                enter.hasExitTime = false;
                enter.hasFixedDuration = true;
                enter.duration = 0.06f;
                enter.canTransitionToSelf = true;
                var leave = hit.AddTransition(empty);
                leave.hasExitTime = true;
                leave.exitTime = 1f;
                leave.hasFixedDuration = true;
                leave.duration = 0.1f;

                // Later Animator layers win. Put Hit below the existing combat
                // layer so punches keep their pose and damage contact timing.
                int combatIndex = System.Array.FindIndex(layers, item => item.name == "combat layer");
                int insertedIndex = combatIndex >= 1 ? combatIndex : layers.Length - 1;
                var ordered = layers.Take(layers.Length - 1).ToList();
                ordered.Insert(insertedIndex, layer);
                foreach (var item in ordered)
                    if (item.syncedLayerIndex >= insertedIndex)
                        item.syncedLayerIndex++;
                controller.layers = ordered.ToArray();
            }
            Undo.RecordObject(animator, "Assign Hit Reaction controller");
            animator.runtimeAnimatorController = controller;
            if (player.GetComponent<MiningCharacterHealth>() == null)
                Undo.AddComponent<MiningCharacterHealth>(player.gameObject);
            var reaction = player.GetComponent<MiningHitReaction>();
            if (reaction == null) reaction = Undo.AddComponent<MiningHitReaction>(player.gameObject);
            var settings = new SerializedObject(reaction);
            settings.FindProperty("animator").objectReferenceValue = animator;
            settings.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player.gameObject;
            Debug.Log("Hit Reaction ready. Edit Hit Reaction/HitReaction Motion in Animator. Attacks and movement remain active. Save the scene.", player);
        }
    }
}
#endif
