#if UNITY_EDITOR
using System;
using System.Linq;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SkullclawSetup
{
    public const string Folder = "Assets/GameData/Monsters/Skullclaw";
    public const string PrefabPath = Folder + "/Skullclaw.prefab";
    public const string DataPath = Folder + "/SkullclawData.asset";

    [MenuItem("Mining Simulator/Setup/Skullclaw Mutant Animations")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var data = AssetDatabase.LoadAssetAtPath<SkullclawData>(DataPath);
        bool first = data == null;
        if (first)
        {
            data = ScriptableObject.CreateInstance<SkullclawData>();
            // Independent species defaults, not an override of Golem/Forest Golem data.
            data.combatSettingsVersion = 1;
            data.combat.maxHealth = 80f; data.combat.damage = 20f;
            data.combat.regenAmount = 0f; data.combat.moveSpeed = 1.3f;
            data.combat.attackRange = 2.1f; data.combat.attackArc = 120f;
            data.combat.attackState = "Swipe Right"; data.combat.attackSpeed = 1f;
            data.combat.animationBlendSeconds = .12f; data.combat.hitHeight = 2.5f;
            data.combat.hitShape = MushroomMonster.HitShape.Sweep;
            data.combat.warningShader = Shader.Find("MiningSimulator/MonsterAttackWarning");
            if (data.combat.warningShader == null)
                data.combat.warningShader = AssetDatabase.LoadAssetAtPath<MonsterRewardData>("Assets/GameData/Monsters/GolemRewards.asset").combat.warningShader;
            data.boss.enabled = false;
            AssetDatabase.CreateAsset(data, DataPath);
        }
        data.idleClip = MakeClip("Mutant Breathing Idle.fbx", "Idle", true, data);
        data.walkClip = MakeClip("Mutant Walking.fbx", "Walk", true, data);
        data.rightSwipeClip = MakeClip("Mutant Swiping (1).fbx", "Swipe Right", false, data);
        data.leftSwipeClip = MakeClip("Mutant Swiping.fbx", "Swipe Left", false, data);
        data.jumpClip = MakeClip("Mutant Jump Attack.fbx", "Jump Attack", false, data);
        string controllerPath = Folder + "/Skullclaw.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine = controller.layers[0].stateMachine;
        foreach (var child in machine.states.Where(s => s.state.name == "Swipe_Right" || s.state.name == "Swipe_Left" || s.state.name == "Jump_Attack").ToArray())
            machine.RemoveState(child.state);
        var clips = new[]{data.idleClip, data.walkClip, data.rightSwipeClip, data.leftSwipeClip, data.jumpClip,
            MakeGolemReaction("Damage"), MakeGolemReaction("Down")};
        for (int i = 0; i < clips.Length; i++)
        {
            var state = machine.states.FirstOrDefault(x => x.state.name == clips[i].name).state;
            if (state == null) state = machine.AddState(clips[i].name, new Vector3(280 + (i % 3) * 220, 80 + (i / 3) * 130));
            state.motion = clips[i]; state.writeDefaultValues = true;
            if (i == 0) machine.defaultState = state;
        }
        EditorUtility.SetDirty(controller); AssetDatabase.SaveAssetIfDirty(controller);
        EditorUtility.SetDirty(data); AssetDatabase.SaveAssetIfDirty(data);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            var root = new GameObject("Skullclaw");
            try
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Skullclaw.fbx"));
                visual.transform.SetParent(root.transform, false); visual.name = "Skullclaw Visual";
                var animator = visual.GetComponent<Animator>();
                if (animator == null) animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var health = root.AddComponent<MiningCharacterHealth>();
                var motor = root.AddComponent<CharacterController>();
                motor.height = 1.8f; motor.radius = .55f; motor.center = new Vector3(0, .9f, 0);
                motor.skinWidth = .03f; motor.minMoveDistance = 0f; motor.stepOffset = .25f;
                var monster = root.AddComponent<MushroomMonster>();
                var serialized = new SerializedObject(monster);
                serialized.FindProperty("animator").objectReferenceValue = animator;
                serialized.FindProperty("rewards").objectReferenceValue = data;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var healthSerialized = new SerializedObject(health);
                healthSerialized.FindProperty("displayName").stringValue = "Skullclaw";
                healthSerialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        Debug.Log("Skullclaw configured: right swipe -> left swipe -> collision-safe jump area. Spawn roster and scene unchanged.");
    }

    private static AnimationClip MakeClip(string file, string state, bool loop, SkullclawData data)
    {
        var source = AssetDatabase.LoadAllAssetsAtPath(Folder + "/Animations/" + file)
            .OfType<AnimationClip>().First(x => x.name == "mixamo.com");
        var clip = UnityEngine.Object.Instantiate(source); clip.name = state;
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (binding.path != "Skullclaw_Rig" || !binding.propertyName.StartsWith("m_LocalPosition.")) continue;
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (binding.propertyName.EndsWith(".y") && state == "Jump Attack")
            {
                float baseline = curve.Evaluate(0f);
                var height = new AnimationCurve(); var residual = new AnimationCurve();
                // Dense samples preserve the original takeoff/landing arc while moving its collider too.
                int count = Mathf.CeilToInt(source.length * 60f);
                for (int i = 0; i <= count; i++)
                {
                    float t = source.length * i / count; float y = curve.Evaluate(t);
                    height.AddKey(t / source.length, Mathf.Max(0, y - baseline));
                    residual.AddKey(t, Mathf.Min(y, baseline));
                }
                data.jumpHeight = height;
                AnimationUtility.SetEditorCurve(clip, binding, residual);
            }
            else if (!binding.propertyName.EndsWith(".y"))
                AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0, source.length, 0));
        }
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop; AnimationUtility.SetAnimationClipSettings(clip, settings);
        clip.events = Array.Empty<AnimationEvent>(); // runtime contact is authoritative and single-fire
        return SaveClip(clip);
    }

    private static AnimationClip MakeGolemReaction(string name)
    {
        var source = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/MonsterPack3D/Animations/Golem/SA_Golem_" + name + ".anim");
        var clip = new AnimationClip { name = name, frameRate = source.frameRate };
        foreach (var binding in AnimationUtility.GetCurveBindings(source))
        {
            var remapped = binding;
            // Golem's skeleton uses the same deform bones, beneath Skullclaw_Rig in this model.
            remapped.path = "Skullclaw_Rig/" + binding.path;
            AnimationUtility.SetEditorCurve(clip, remapped, AnimationUtility.GetEditorCurve(source, binding));
        }
        return SaveClip(clip);
    }

    private static AnimationClip SaveClip(AnimationClip clip)
    {
        string path = Folder + "/Animations/" + clip.name.Replace(" ", "_") + ".anim";
        var old = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (old == null)
        {
            string name = clip.name;
            AssetDatabase.CreateAsset(clip, path);
            clip.name = name;
            EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip); return clip;
        }
        EditorUtility.CopySerialized(clip, old); UnityEngine.Object.DestroyImmediate(clip);
        EditorUtility.SetDirty(old); AssetDatabase.SaveAssetIfDirty(old); return old;
    }
}
#endif
