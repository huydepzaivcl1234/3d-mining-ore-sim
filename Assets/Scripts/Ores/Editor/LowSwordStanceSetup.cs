using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using MiningSimulator.Ores;

// Asset-only migration for the requested sword feature; never edits/saves a scene.
[InitializeOnLoad]
public static class LowSwordStanceSetup
{
    const string Output = "Assets/GameData/Weapons/LowSwordCombat.controller";
    static LowSwordStanceSetup() { EditorApplication.delayCall += ConfigureIfMissing; }
    static void ConfigureIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += ConfigureIfMissing; return; }
        var data = AssetDatabase.LoadAssetAtPath<WeaponAttackData>("Assets/GameData/Weapons/Sword.asset");
        var controller = data != null ? data.weaponController as AnimatorController : null;
        bool configured = false;
        if (controller != null) foreach (var parameter in controller.parameters)
            if (parameter.name == "SwordPose") configured = true;
        if (data != null && !configured)
        {
            Setup();
        }
    }
    [MenuItem("Mining Simulator/Setup/Setup Low Sword Draw And Sheath %#&k")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var data = AssetDatabase.LoadAssetAtPath<WeaponAttackData>("Assets/GameData/Weapons/Sword.asset");
        var draw = Clip("Assets/GameData/Player/Animations/Withdrawing Sword (1).fbx");
        var sheath = Clip("Assets/GameData/Player/Animations/Sheathing Sword.fbx");
        var slash = Clip("Assets/GameData/Player/Animations/Sword And Shield Slash.fbx");
        if (data == null || draw == null || sheath == null || slash == null)
        { Debug.LogWarning("Low sword setup requires Sword data and the three imported Humanoid clips."); return; }
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Output);
        bool created = controller == null;
        if (created)
        {
            var player = Object.FindFirstObjectByType<PlayerCombatInput>(FindObjectsInactive.Include);
            var animator = player != null ? player.GetComponentInChildren<Animator>(true) : null;
            RuntimeAnimatorController source = animator != null ? animator.runtimeAnimatorController : null;
            if (source is AnimatorOverrideController overrides) source = overrides.runtimeAnimatorController;
            if (source == null) source = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                "Assets/GameData/Player/Animations/MiningCombat/Player Combat Respawn Hit Reaction.controller");
            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(sourcePath) || !AssetDatabase.CopyAsset(sourcePath, Output))
            { Debug.LogWarning("Cannot copy Player's authored controller for sword states."); return; }
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Output);
        }
        AnimatorStateMachine machine = null;
        foreach (var layer in controller.layers) if (layer.name == "combat layer") machine = layer.stateMachine;
        if (machine == null) { Debug.LogWarning("Low sword controller needs the existing combat layer."); return; }
        var idle = Clip("Assets/GameData/Player/Animations/Sword And Shield Idle.fbx");
        var run = Clip("Assets/GameData/Player/Animations/Run With Sword.fbx");
        if (idle == null || run == null) { Debug.LogWarning("Sword idle/run clips are required."); return; }
        State(machine, "DrawSword", draw);
        State(machine, "SheathSword", sheath);
        State(machine, "WeaponEmpty", null);
        bool migrated = false;
        foreach (var parameter in controller.parameters) if (parameter.name == "SwordPose") migrated = true;
        if (!migrated)
        {
            ConfigureUnarmed(data);
            controller.AddParameter("SwordPose", AnimatorControllerParameterType.Float);
            var locomotion = FindState(controller.layers[0].stateMachine, "Idle Walk Run Blend");
            if (locomotion == null || !(locomotion.motion is BlendTree normal))
                throw new System.InvalidOperationException("Player locomotion blend tree is missing.");
            var sword = new BlendTree { name = "Sword Idle Walk Run", blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(sword, controller);
            sword.AddChild(idle, 0);
            // No sword walk clip is supplied: slow the sword run at walking speed.
            sword.AddChild(run, 2); sword.AddChild(run, 6);
            var children = sword.children; children[1].timeScale = 0.45f; sword.children = children;
            var stances = new BlendTree { name = "Normal To Sword Locomotion", blendType = BlendTreeType.Simple1D,
                blendParameter = "SwordPose", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(stances, controller);
            stances.AddChild(normal, 0); stances.AddChild(sword, 1);
            locomotion.motion = stances; EditorUtility.SetDirty(locomotion);
            AnimationClip footstepSource = null;
            foreach (var child in normal.children)
                if (child.threshold > 2 && child.motion is AnimationClip clip) footstepSource = clip;
            ConfigureClip(idle, true, null); ConfigureClip(run, true, footstepSource);
            ConfigureClip(draw, false, null); ConfigureClip(sheath, false, null);
            ConfigureClip(slash, false, null);
            foreach (var child in machine.states)
            {
                if (child.state.name == "Attack") child.state.motion = slash;
                if (child.state.name == "CombatIdle" && idle != null) child.state.motion = idle;
                EditorUtility.SetDirty(child.state);
            }
            var fullBody = new AvatarMask { name = "Sword Stance Full Body" };
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                fullBody.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, i != (int)AvatarMaskBodyPart.Root);
            AssetDatabase.AddObjectToAsset(fullBody, controller);
            var stanceMachine = new AnimatorStateMachine { name = "Sword Stance" };
            AssetDatabase.AddObjectToAsset(stanceMachine, controller);
            State(stanceMachine, "DrawSword", draw); State(stanceMachine, "SheathSword", sheath);
            State(stanceMachine, "WeaponEmpty", null);
            stanceMachine.defaultState = FindState(stanceMachine, "WeaponEmpty");
            controller.AddLayer(new AnimatorControllerLayer { name = "Sword Stance", avatarMask = fullBody,
                stateMachine = stanceMachine, defaultWeight = 0, blendingMode = AnimatorLayerBlendingMode.Override });
        }
        Undo.RecordObject(data, "Configure Low sword stance");
        if (data.modelPrefab == null) data.modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameData/Player/Animations/Low.prefab");
        data.weaponController = controller;
        data.drawClip = draw;
        data.sheathClip = sheath;
        ConfigureArms(controller, draw, sheath);
        RepairSwordLocomotion(controller, idle, run);
        ConfigureVisibleEquipGraph(controller);
        ConfigureContactEvent(draw, "OnDrawSword", data.drawAttachTime);
        ConfigureContactEvent(sheath, "OnSheathSword", data.sheathAttachTime);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(machine);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssetIfDirty(controller);
        AssetDatabase.SaveAssetIfDirty(data);
        Debug.Log("LOW SWORD READY: E draws/sheathes; editable DrawSword/SheathSword/CombatIdle/Attack states in " + Output);
    }
    static void RepairSwordLocomotion(AnimatorController controller, AnimationClip idle, AnimationClip run)
    {
        var locomotion = FindState(controller.layers[0].stateMachine, "Idle Walk Run Blend");
        if (locomotion == null || !(locomotion.motion is BlendTree stances) || stances.blendParameter != "SwordPose")
            throw new System.InvalidOperationException("SwordPose locomotion tree must be present; no authored locomotion was replaced.");
        var children = stances.children;
        if (children.Length != 2) throw new System.InvalidOperationException("Expected normal/sword locomotion branches.");
        if (!(children[1].motion is BlendTree sword) || sword.blendParameter != "Speed")
        {
            sword = new BlendTree { name = "Sword Idle Walk Run", blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(sword, controller);
            sword.AddChild(idle, 0); sword.AddChild(run, 2); sword.AddChild(run, 6);
            var speeds = sword.children; speeds[1].timeScale = 0.45f; sword.children = speeds;
            children[1].motion = sword; stances.children = children;
            EditorUtility.SetDirty(stances);
            EditorUtility.SetDirty(sword);
        }
        ConfigureClip(idle, true, null);
        ConfigureClip(run, true, null);
    }
    static void ConfigureVisibleEquipGraph(AnimatorController controller)
    {
        foreach (string name in new[] { "DrawingSword", "SheathingSword" })
        {
            bool exists = false;
            foreach (var parameter in controller.parameters) if (parameter.name == name) exists = true;
            if (!exists) controller.AddParameter(name, AnimatorControllerParameterType.Bool);
        }
        foreach (var layer in controller.layers)
        {
            if (layer.name != "Sword Arms" && layer.name != "Sword Stance") continue;
            var machine = layer.stateMachine;
            var empty = FindState(machine, "WeaponEmpty");
            var draw = FindState(machine, "DrawSword");
            var sheath = FindState(machine, "SheathSword");
            if (empty == null || draw == null || sheath == null) throw new System.InvalidOperationException("Incomplete sword equip graph.");
            machine.entryPosition = new Vector3(60, 40);
            machine.anyStatePosition = new Vector3(60, 280);
            var states = machine.states;
            for (int i = 0; i < states.Length; i++)
                states[i].position = states[i].state == empty ? new Vector3(280, 40) :
                    states[i].state == draw ? new Vector3(80, 160) : new Vector3(480, 160);
            machine.states = states;
            EquipTransition(empty, draw, "DrawingSword", true);
            EquipTransition(empty, sheath, "SheathingSword", true);
            EquipTransition(draw, empty, "DrawingSword", false);
            EquipTransition(sheath, empty, "SheathingSword", false);
            EditorUtility.SetDirty(machine);
        }
    }
    static void EquipTransition(AnimatorState source, AnimatorState destination, string parameter, bool enabled)
    {
        foreach (var existing in source.transitions) if (existing != null && existing.destinationState == destination) return;
        var transition = source.AddTransition(destination);
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0.15f;
        transition.AddCondition(enabled ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, parameter);
        EditorUtility.SetDirty(source);
    }
    static void ConfigureUnarmed(WeaponAttackData sword)
    {
        const string path = "Assets/GameData/Weapons/UnarmedCombat.controller";
        var unarmed = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (unarmed == null)
        {
            if (!AssetDatabase.CopyAsset("Assets/GameData/Player/Animations/MiningCombat/Player Combat Respawn Hit Reaction.controller", path))
                throw new System.InvalidOperationException("Cannot create unarmed controller.");
            unarmed = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        }
        sword.unarmedController = unarmed;
        var fists = AssetDatabase.LoadAssetAtPath<WeaponAttackData>("Assets/GameData/Weapons/Fists.asset");
        if (fists != null)
        {
            Undo.RecordObject(fists, "Configure unarmed controller");
            fists.weaponController = unarmed; EditorUtility.SetDirty(fists); AssetDatabase.SaveAssetIfDirty(fists);
        }
    }
    static void ConfigureArms(AnimatorController controller, AnimationClip draw, AnimationClip sheath)
    {
        var layers = controller.layers;
        foreach (var layer in layers) if (layer.name == "Sword Arms") return;
        var mask = new AvatarMask { name = "Sword Equip Arms Only" };
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
        {
            var part = (AvatarMaskBodyPart)i;
            mask.SetHumanoidBodyPartActive(part, part == AvatarMaskBodyPart.LeftArm ||
                part == AvatarMaskBodyPart.RightArm || part == AvatarMaskBodyPart.LeftHandIK ||
                part == AvatarMaskBodyPart.RightHandIK);
        }
        AssetDatabase.AddObjectToAsset(mask, controller);
        var machine = new AnimatorStateMachine { name = "Sword Arms" };
        AssetDatabase.AddObjectToAsset(machine, controller);
        State(machine, "WeaponEmpty", null);
        State(machine, "DrawSword", draw);
        State(machine, "SheathSword", sheath);
        machine.defaultState = FindState(machine, "WeaponEmpty");
        var arms = new AnimatorControllerLayer { name = "Sword Arms", avatarMask = mask,
            stateMachine = machine, defaultWeight = 0, blendingMode = AnimatorLayerBlendingMode.Override };
        // Full-body stance must be above the arm overlay while stationary.
        var ordered = new System.Collections.Generic.List<AnimatorControllerLayer>(layers);
        int stanceIndex = ordered.FindIndex(layer => layer.name == "Sword Stance");
        ordered.Insert(stanceIndex >= 0 ? stanceIndex : ordered.Count, arms);
        controller.layers = ordered.ToArray();
        EditorUtility.SetDirty(machine);
    }
    static void ConfigureContactEvent(AnimationClip clip, string callback, float contact)
    {
        var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as ModelImporter;
        if (importer == null) return;
        var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
        foreach (var settings in clips)
        {
            var events = new System.Collections.Generic.List<AnimationEvent>(settings.events);
            events.RemoveAll(e => e.functionName == callback);
            events.Add(new AnimationEvent { functionName = callback, time = Mathf.Clamp01(contact) });
            settings.events = events.ToArray();
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }
    static AnimatorState FindState(AnimatorStateMachine machine, string name)
    {
        foreach (var state in machine.states) if (state.state.name == name) return state.state;
        foreach (var child in machine.stateMachines)
        { var found = FindState(child.stateMachine, name); if (found != null) return found; }
        return null;
    }
    [MenuItem("Mining Simulator/Tools/Preview Player Draw Or Sheath")]
    static void PreviewStance()
    {
        if (!EditorApplication.isPlaying) return;
        var player = Object.FindFirstObjectByType<PlayerCombatInput>();
        if (player == null) return;
        var animator = player.GetComponent<Animator>();
        player.RequestCombat(!animator.GetBool("CombatMode"));
        Debug.Log("Sword preview requested on Player.");
    }
    [MenuItem("Mining Simulator/Tools/Preview Player Sword Run")]
    static void PreviewRun()
    {
        if (!EditorApplication.isPlaying) return;
        var player = Object.FindFirstObjectByType<PlayerCombatInput>();
        if (player == null) return;
        var inputs = player.GetComponent<StarterAssets.StarterAssetsInputs>();
        if (inputs == null) return;
        var previousMove = inputs.move;
        bool previousSprint = inputs.sprint;
        // Preview the requested equip animation and locomotion together.
        inputs.MoveInput(Vector2.up);
        inputs.SprintInput(true);
        player.RequestCombat(!player.GetComponent<Animator>().GetBool("CombatMode"));
        double finishAt = EditorApplication.timeSinceStartup + 5;
        EditorApplication.CallbackFunction tick = null;
        tick = () => {
            if (!EditorApplication.isPlaying || inputs == null || EditorApplication.timeSinceStartup >= finishAt)
            {
                EditorApplication.update -= tick;
                if (inputs != null) { inputs.MoveInput(previousMove); inputs.SprintInput(previousSprint); }
                return;
            }
            inputs.MoveInput(Vector2.up); inputs.SprintInput(true);
        };
        EditorApplication.update += tick;
    }
    [MenuItem("Mining Simulator/Tools/Preview Player Switch Weapon")]
    static void PreviewEquipment()
    {
        if (!EditorApplication.isPlaying) return;
        var player = Object.FindFirstObjectByType<PlayerCombatInput>();
        if (player == null) return;
        bool sword = player.Weapon != null && player.Weapon.drawClip != null;
        var data = AssetDatabase.LoadAssetAtPath<WeaponAttackData>(sword ?
            "Assets/GameData/Weapons/Fists.asset" : "Assets/GameData/Weapons/Sword.asset");
        if (player.TryEquipWeapon(data)) player.RequestCombat(true);
    }
    static void ConfigureClip(AnimationClip clip, bool loop, AnimationClip footsteps)
    {
        var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as ModelImporter;
        if (importer == null) return;
        var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
        foreach (var settings in clips)
        {
            settings.loopTime = loop; settings.loopPose = loop;
            settings.lockRootRotation = true; settings.lockRootHeightY = true; settings.lockRootPositionXZ = true;
            if (footsteps != null && settings.events.Length == 0)
            {
                var events = new System.Collections.Generic.List<AnimationEvent>();
                foreach (var source in AnimationUtility.GetAnimationEvents(footsteps))
                    if (source.functionName == "OnFootstep") events.Add(new AnimationEvent {
                        functionName = "OnFootstep", time = source.time / footsteps.length,
                        floatParameter = source.floatParameter });
                settings.events = events.ToArray();
            }
        }
        importer.clipAnimations = clips; importer.SaveAndReimport();
    }
    static void State(AnimatorStateMachine machine, string name, AnimationClip motion)
    {
        foreach (var child in machine.states) if (child.state.name == name) return;
        var state = machine.AddState(name);
        state.motion = motion;
        state.writeDefaultValues = false;
        EditorUtility.SetDirty(state);
    }
    static AnimationClip Clip(string path)
    {
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__")) return clip;
        return null;
    }
}
