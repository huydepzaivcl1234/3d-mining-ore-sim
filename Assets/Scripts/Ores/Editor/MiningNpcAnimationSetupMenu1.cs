#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MiningSimulator.Editor
{
    /// <summary>
    /// Builds the Animator Controller for the rigged NPC model from the imported mining
    /// animation clip. Creates only this one asset — does not touch existing UI, icons, or
    /// gameplay prefabs. Adjust the two paths below if you imported the files somewhere else.
    /// </summary>
    public static class MiningNpcAnimationSetupMenu
    {
        // Where you imported the FBX from the "Human Crafting Animations FREE" pack.
        private const string MiningClipFbxPath =
            "Assets/Prefabs/NPC/Mining/HumanM@MiningOneHand01_R - Ground.fbx";

        // Output location for the generated controller.
        private const string ControllerPath = "Assets/Prefabs/NPC/HumanM_Animator.controller";

        private const string IsMiningParam = "IsMining";
        private const string IsMovingParam = "IsMoving";

        // Name of the Animator/relay method that applies ore damage AND plays the hit SFX in
        // the same call (see MiningNpc.OnMiningImpact) - both were already wired to fire
        // together in code, but nothing was ever raising this event, so neither happened.
        private const string MiningImpactEventFunctionName = "OnMiningImpact";

        // Normalized position (0-1) inside the swing where the tool visually strikes the ore.
        // Tweak this and re-run the menu command if the hit/SFX timing looks early or late for
        // your clip - it only ever touches this one event, so re-running is always safe.
        private const float MiningImpactNormalizedTime = 0.5f;

        [MenuItem("Mining Simulator/Setup/Create NPC Animator Controller")]
        public static void CreateNpcAnimatorController()
        {
            AnimationClip miningClip = LoadMiningClip();
            if (miningClip == null)
            {
                EditorUtility.DisplayDialog("NPC Animator Setup",
                    $"Couldn't find an AnimationClip inside:\n{MiningClipFbxPath}\n\n" +
                    "Import the FBX there first (or edit MiningClipFbxPath at the top of " +
                    "MiningNpcAnimationSetupMenu.cs to match where you put it), then run this again.",
                    "OK");
                return;
            }

            // The clip is a single mining swing — make sure it loops seamlessly instead of
            // popping back to frame 0 every cycle.
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(miningClip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(miningClip, settings);

            bool addedImpactEvent = EnsureMiningImpactEvent(miningClip);

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                string folder = System.IO.Path.GetDirectoryName(ControllerPath);
                if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
                {
                    System.IO.Directory.CreateDirectory(folder);
                    AssetDatabase.Refresh();
                }
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            if (!controller.parameters.Any(p => p.name == IsMiningParam))
            {
                controller.AddParameter(IsMiningParam, AnimatorControllerParameterType.Bool);
            }
            if (!controller.parameters.Any(p => p.name == IsMovingParam))
            {
                controller.AddParameter(IsMovingParam, AnimatorControllerParameterType.Bool);
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idleState = FindOrAddState(stateMachine, "Idle", null);
            AnimatorState mineState = FindOrAddState(stateMachine, "Mine", miningClip);
            stateMachine.defaultState = idleState;

            EnsureTransition(idleState, mineState, IsMiningParam, true);
            EnsureTransition(mineState, idleState, IsMiningParam, false);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            string impactNote = addedImpactEvent
                ? "\n\nAdded the missing '" + MiningImpactEventFunctionName + "' animation event " +
                  "at " + (MiningImpactNormalizedTime * 100f).ToString("0") + "% through the swing - " +
                  "this is what makes the NPC damage the ore and play the hit SFX at the same time. " +
                  "Open the clip in the Animation window if you want to nudge its exact frame."
                : "\n\nThe '" + MiningImpactEventFunctionName + "' animation event was already " +
                  "present on the clip, so it was left untouched.";

            EditorUtility.DisplayDialog("NPC Animator Setup",
                "Created/updated HumanM_Animator.controller with Idle <-> Mine states driven " +
                "by the IsMining bool." + impactNote + "\n\nLast step (manual): select your NPC " +
                "model's Animator component and drag this controller into its Controller field — " +
                "see the chat reply for the rest of the setup.", "OK");
        }

        private static AnimationClip LoadMiningClip()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(MiningClipFbxPath);
            return assets.OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
        }

        /// <summary>
        /// Idempotently makes sure the mining clip raises <see cref="MiningImpactEventFunctionName"/>
        /// once per swing. MiningNpc.OnMiningImpact (relayed via MiningNpcAnimationEventRelay)
        /// already applies ore damage and plays the hit SFX back-to-back in a single call, so the
        /// only reason they were ever out of sync is that nothing was raising this event - never
        /// touches unrelated events already authored on the clip.
        /// </summary>
        private static bool EnsureMiningImpactEvent(AnimationClip clip)
        {
            AnimationEvent[] existingEvents = AnimationUtility.GetAnimationEvents(clip);
            bool alreadyPresent = existingEvents.Any(
                animationEvent => animationEvent.functionName == MiningImpactEventFunctionName);
            if (alreadyPresent)
            {
                return false;
            }

            AnimationEvent impactEvent = new()
            {
                time = clip.length * MiningImpactNormalizedTime,
                functionName = MiningImpactEventFunctionName
            };

            AnimationEvent[] updatedEvents = existingEvents
                .Append(impactEvent)
                .OrderBy(animationEvent => animationEvent.time)
                .ToArray();
            AnimationUtility.SetAnimationEvents(clip, updatedEvents);
            EditorUtility.SetDirty(clip);
            return true;
        }

        private static AnimatorState FindOrAddState(AnimatorStateMachine stateMachine,
            string stateName, Motion motion)
        {
            AnimatorState existing = stateMachine.states
                .Select(childState => childState.state)
                .FirstOrDefault(state => state.name == stateName);
            if (existing != null)
            {
                existing.motion = motion;
                return existing;
            }

            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = motion;
            return state;
        }

        private static void EnsureTransition(AnimatorState from, AnimatorState to,
            string parameterName, bool parameterValue)
        {
            bool alreadyExists = from.transitions.Any(t => t.destinationState == to &&
                t.conditions.Any(c => c.parameter == parameterName));
            if (alreadyExists)
            {
                return;
            }

            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.15f;
            transition.AddCondition(
                parameterValue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f, parameterName);
        }
    }
}
#endif