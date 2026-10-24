#if UNITY_EDITOR
using System.Linq;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class SkullclawTests
{
    [Test]
    public void DefenseTargetCanArmTheJumpWithoutPlayerDetection()
    {
        var obj = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SkullclawSetup.PrefabPath));
        var tower = new GameObject("Defense jump gate target");
        try
        {
            obj.transform.position = new Vector3(12000, 0, 12000);
            tower.transform.position = obj.transform.position + Vector3.forward * 4f;
            var health = tower.AddComponent<MiningCharacterHealth>(); health.ConfigureSpawnHealth(1000f);
            var shape = tower.AddComponent<BoxCollider>(); shape.center = Vector3.up; shape.size = new Vector3(1, 2, 1);
            var monster = obj.GetComponent<MushroomMonster>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(MushroomMonster).GetField("motor", flags).SetValue(monster, obj.GetComponent<CharacterController>());
            typeof(MushroomMonster).GetField("target", flags).SetValue(monster, health);
            typeof(MushroomMonster).GetField("targetCollider", flags).SetValue(monster, shape);
            typeof(MushroomMonster).GetField("playerDetected", flags).SetValue(monster, false);
            Physics.SyncTransforms();
            typeof(MushroomMonster).GetMethod("TickSkullclawApproach", flags).Invoke(monster, new object[] { .51f });
            Assert.That(typeof(MushroomMonster).GetProperty("CanSkullclawEngage", flags).GetValue(monster), Is.True,
                "A defense target at jump distance must not stall at the navigation stand point.");
            tower.transform.position = obj.transform.position + Vector3.forward * 10f;
            Physics.SyncTransforms();
            typeof(MushroomMonster).GetMethod("TickSkullclawApproach", flags).Invoke(monster, new object[] { .51f });
            Assert.That(typeof(MushroomMonster).GetProperty("CanSkullclawEngage", flags).GetValue(monster), Is.False);
        }
        finally { Object.DestroyImmediate(obj); Object.DestroyImmediate(tower); }
    }

    [Test] public void CloseCombatNeverSchedulesTheJump()
    {
        var combo = new SkullclawCombo();
        for (int i = 0; i < 10; i++) Assert.That(combo.Begin(false), Is.EqualTo(i % 2));
        Assert.That(combo.Begin(true), Is.EqualTo(2));
        Assert.That(combo.Begin(false), Is.EqualTo(0));
    }
    [Test] public void JumpRequiresHalfSecondContinuouslyInsideTheApproachBand()
    {
        var gate = new SkullclawJumpGate();
        gate.Tick(true, .49f); Assert.That(gate.Ready(.5f), Is.False);
        gate.Tick(false, .1f); Assert.That(gate.Ready(.5f), Is.False);
        gate.Tick(true, .49f); Assert.That(gate.Ready(.5f), Is.False);
        gate.Tick(true, .02f); Assert.That(gate.Ready(.5f), Is.True);
        gate.Tick(false, .01f); Assert.That(gate.Ready(.5f), Is.False);
    }
    [Test] public void SkullclawHasTheSameBoundHealthBarAsOtherMonsters()
    {
        var hp = AssetDatabase.LoadAssetAtPath<GameObject>(SkullclawSetup.PrefabPath).GetComponent<MiningCharacterHealth>();
        Assert.That(hp.HealthBar, Is.Not.Null);
        var so = new SerializedObject(hp);
        Assert.That(so.FindProperty("microBar").objectReferenceValue, Is.Not.Null);
        Assert.That(so.FindProperty("healthLabel").objectReferenceValue, Is.Not.Null);
    }
    [Test] public void GapCloserPreservesTheWaitingMeleeAttack()
    {
        var combo = new SkullclawCombo();
        Assert.That(combo.Begin(false), Is.EqualTo(0));
        Assert.That(combo.Next, Is.EqualTo(1));
        Assert.That(combo.Begin(true), Is.EqualTo(2));
        Assert.That(combo.Next, Is.EqualTo(1));
        Assert.That(combo.Begin(false), Is.EqualTo(1));
        Assert.That(combo.Begin(true), Is.EqualTo(2));
        Assert.That(combo.Next, Is.EqualTo(0));
    }
    [Test] public void NightRollNeverRerollsOrReplacesAKilledSpawn()
    {
        var roll = new SkullclawNightRoll();
        roll.Roll(1, 30f, .2f); Assert.That(roll.Pending, Is.True);
        roll.MarkSpawned(); roll.Roll(1, 100f, 0f); Assert.That(roll.Pending, Is.False);
        roll.Roll(2, 30f, .9f); Assert.That(roll.Pending, Is.False);
        roll.Roll(2, 100f, 0f); Assert.That(roll.Pending, Is.False);
        roll.Roll(3, 100f, .999f); Assert.That(roll.Pending, Is.True);
    }
    [Test] public void NightRosterIsWiredWithoutClawTrails()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkullclawSetup.PrefabPath);
        var roster = AssetDatabase.LoadAssetAtPath<MonsterSpawnRoster>("Assets/Resources/MonsterSpawnRoster.asset");
        Assert.That(roster.nightSkullclaw.prefab, Is.EqualTo(prefab.GetComponent<MushroomMonster>()));
        Assert.That(roster.Entries.Any(x => x.prefab == roster.nightSkullclaw.prefab), Is.False);
        Assert.That(prefab.GetComponents<MonoBehaviour>().Any(x => x != null && x.GetType().Name == "SkullclawClawTrails"), Is.False);
    }
    [Test] public void ComboIsRightLeftJumpAndResets()
    {
        var combo = new SkullclawCombo();
        for (int i = 0; i < 12; i++) Assert.That(combo.Begin(), Is.EqualTo(i % 3));
        combo.Reset(); Assert.That(combo.Next, Is.Zero); Assert.That(combo.Current, Is.EqualTo(-1));
    }

    [Test] public void PrefabHasAllConfiguredAnimationStatesAndValidBindings()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkullclawSetup.PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        var data = AssetDatabase.LoadAssetAtPath<SkullclawData>(SkullclawSetup.DataPath);
        Assert.That(prefab.GetComponent<MushroomMonster>().CombatData, Is.SameAs(data.combat));
        var animator = prefab.GetComponentInChildren<Animator>();
        Assert.That(animator.applyRootMotion, Is.False);
        var machine = ((AnimatorController)animator.runtimeAnimatorController).layers[0].stateMachine;
        foreach (string name in new[]{"Idle", "Walk", data.AttackState(0), data.AttackState(1), data.AttackState(2), "Damage", "Down"})
        {
            var state = machine.states.Single(x => x.state.name == name).state;
            Assert.That(state.motion, Is.Not.Null, name);
            foreach (var binding in AnimationUtility.GetCurveBindings((AnimationClip)state.motion))
                if (!string.IsNullOrEmpty(binding.path)) Assert.That(animator.transform.Find(binding.path), Is.Not.Null, binding.path);
        }
    }

    [Test] public void LocomotionLoopsAndAttackClipsDoNot()
    {
        var data = AssetDatabase.LoadAssetAtPath<SkullclawData>(SkullclawSetup.DataPath);
        Assert.That(data.idleClip.isLooping && data.walkClip.isLooping, Is.True);
        foreach (var clip in new[]{data.rightSwipeClip, data.leftSwipeClip, data.jumpClip})
            Assert.That(clip.isLooping, Is.False, clip.name);
        Assert.That(data.jumpHeight.Evaluate(.25f), Is.GreaterThan(1f));
        Assert.That(data.jumpContact, Is.GreaterThanOrEqualTo(data.landing));
    }

    [Test] public void MeshDoesNotTranslateAwayFromMotor()
    {
        var data = AssetDatabase.LoadAssetAtPath<SkullclawData>(SkullclawSetup.DataPath);
        foreach (var clip in new[]{data.idleClip, data.walkClip, data.rightSwipeClip, data.leftSwipeClip, data.jumpClip})
        foreach (var b in AnimationUtility.GetCurveBindings(clip).Where(b=>b.path=="Skullclaw_Rig" &&
            (b.propertyName=="m_LocalPosition.x" || b.propertyName=="m_LocalPosition.z")))
        foreach (var key in AnimationUtility.GetEditorCurve(clip,b).keys)
            Assert.That(key.value, Is.Zero.Within(.0001f), clip.name);
    }

    [Test] public void OtherSpeciesDoNotUseSkullclawData()
    {
        foreach (string name in new[]{"GolemRewards", "ForestGolemRewards", "MushroomRewards"})
            Assert.That(AssetDatabase.LoadAssetAtPath<MonsterRewardData>("Assets/GameData/Monsters/"+name+".asset"),
                Is.Not.InstanceOf<SkullclawData>());
    }

    [Test] public void JumpHitVolumeIsCircularNotTheSwipeCone()
    {
        var obj = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SkullclawSetup.PrefabPath));
        try
        {
            var monster = obj.GetComponent<MushroomMonster>();
            var origin = obj.transform.position;
            Assert.That(monster.ContainsHitPoint(origin - Vector3.forward), Is.False, "Swipe cannot hit behind its cone");
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var combo = (SkullclawCombo)typeof(MushroomMonster).GetField("skullCombo", flags).GetValue(monster);
            combo.Begin(); combo.Begin(); combo.Begin();
            typeof(MushroomMonster).GetField("animationState", flags).SetValue(monster, 1);
            typeof(MushroomMonster).GetField("attackStateHash", flags).SetValue(monster, 1);
            float radius = monster.EffectiveAreaRadius;
            foreach (Vector3 direction in new[]{Vector3.forward, Vector3.back, Vector3.right, Vector3.left})
                Assert.That(monster.ContainsHitPoint(origin + direction * radius * .9f), Is.True, "Landing area covers every direction");
            Assert.That(monster.ContainsHitPoint(origin + Vector3.forward * (radius + .1f)), Is.False);
            Assert.That(monster.ContainsHitPoint(origin + Vector3.up * (monster.CombatData.hitHeight * obj.transform.lossyScale.y + .5f)), Is.False);
        }
        finally { Object.DestroyImmediate(obj); }
    }
}
#endif
