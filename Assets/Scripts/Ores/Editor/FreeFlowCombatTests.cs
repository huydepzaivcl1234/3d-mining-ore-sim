#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Linq;

public sealed class FreeFlowCombatTests
{
    private const string ControllerPath = "Assets/GameData/Player/Animations/Player controller.controller";

    [Test]
    public void ThirdStrikeUsesContactEventAfterSwordTrailStarts()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var state = controller.layers.Single(l => l.name == "combat layer").stateMachine.states
            .Single(s => s.state.name == "Special Attack").state;
        var clip = (AnimationClip)state.motion;
        var contact = clip.events.Single(e => e.functionName == "OnSpecialAttackHit");
        var slash = clip.events.Single(e => e.functionName == "OnSpecialSwordTrailStart");
        Assert.That(contact.time, Is.GreaterThan(slash.time));
        Assert.That(clip.events.Any(e => e.functionName == "OnSwordStrikeDown"), Is.False);
        var data = AssetDatabase.LoadAssetAtPath<MiningSimulator.Ores.MiningPlayerStatsData>(
            "Assets/GameData/Player/PlayerStatsData.asset");
        Assert.That(data.swordTrail.material, Is.Not.Null);
        Assert.That(data.attackImpactVfxPrefab, Is.Not.Null);
    }

    [Test]
    public void FootworkIncludesAuthoredHipsAndLegs()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var layer = controller.layers.Single(l => l.name == "Combat Footwork");
        Assert.That(layer.defaultWeight, Is.Zero);
        Assert.That(layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Body), Is.True);
        Assert.That(layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg), Is.True);
        Assert.That(layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg), Is.True);
        // Root mask is designer-authored; motor movement does not require changing it.
    }

    [TestCase("Sword Attack 1")]
    [TestCase("Sword Attack 2")]
    [TestCase("Special Attack")]
    public void FootworkHasSameLengthButNoDuplicateDamageOrVfxEvents(string stateName)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var upper = controller.layers.Single(l => l.name == "combat layer");
        var lower = controller.layers.Single(l => l.name == "Combat Footwork");
        var source = upper.stateMachine.states.Single(s => s.state.name == stateName).state;
        var mirror = lower.stateMachine.states.Single(s => s.state.name == stateName).state;
        Assert.That(((AnimationClip)mirror.motion).length, Is.EqualTo(((AnimationClip)source.motion).length).Within(.001f));
        Assert.That(((AnimationClip)mirror.motion).events, Is.Empty);
        Assert.That(mirror.speedParameterActive, Is.True);
        Assert.That(mirror.speedParameter, Is.EqualTo("AttackSpeed"));
    }

    [TestCase("Sword Attack 1", 1)]
    [TestCase("Sword Attack 2", 2)]
    [TestCase("Special Attack", 0)]
    [TestCase("lunge attack", -1)]
    [TestCase("turn attack", 0)]
    [TestCase("Combat", 0)]
    public void AcceptedComboClickAdvancesOneStrike(string current, int expected)
    {
        var method = typeof(PlayerCombatInput).GetMethod("NextAttackIndex",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.That(method.Invoke(null, new object[] { Animator.StringToHash(current) }), Is.EqualTo(expected));
    }

    [Test]
    public void ComboSequenceMatchesBothAuthoredAnimatorLayers()
    {
        var field = typeof(PlayerCombatInput).GetField("AttackStates",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var names = (string[])field.GetValue(null);
        Assert.That(names, Is.EqualTo(new[] { "Sword Attack 1", "Sword Attack 2", "Special Attack" }));
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        foreach (var layer in controller.layers.Where(l => l.name == "combat layer" || l.name == "Combat Footwork"))
            foreach (var name in names)
                Assert.That(layer.stateMachine.states.Any(s => s.state.name == name), Is.True, layer.name + ": " + name);
    }

    [Test]
    public void SlashAudioHasNoDuplicatePerStrikeConfiguration()
    {
        var fields = typeof(PlayerCombatInput).GetFields(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.That(fields.Any(f => f.Name == "slash1" || f.Name == "slash2" || f.Name == "slash3"), Is.False);
    }

    [Test]
    public void RespawnClearsCombatMovementIntent()
    {
        var obj = new GameObject("Freeflow motor test");
        try
        {
            var motor = obj.AddComponent<StarterAssets.ThirdPersonController>();
            motor.CombatMoveMultiplier = .15f;
            motor.CombatStepVelocity = Vector3.forward * 3f;
            motor.ResetMotionAfterRespawn();
            Assert.That(motor.CombatMoveMultiplier, Is.EqualTo(1f));
            Assert.That(motor.CombatStepVelocity, Is.EqualTo(Vector3.zero));
        }
        finally { Object.DestroyImmediate(obj); }
    }

    [TestCase(30)]
    [TestCase(60)]
    [TestCase(144)]
    public void LungeEnvelopeTravelsSameDistanceAtDifferentFrameRates(int frames)
    {
        float distance = 0f;
        for (int i = 1; i <= frames; i++)
            distance += CombatLungeMotion.TravelBetween((i - 1f) / frames, i / (float)frames, 1.15f);
        Assert.That(distance, Is.EqualTo(1.15f).Within(.0001f));
        Assert.That(CombatLungeMotion.TravelBetween(0f, .01f, 1f),
            Is.LessThan(CombatLungeMotion.TravelBetween(.5f, .51f, 1f)));
        Assert.That(CombatLungeMotion.TravelBetween(.99f, 1f, 1f),
            Is.LessThan(CombatLungeMotion.TravelBetween(.5f, .51f, 1f)));
    }

    [Test]
    public void LungeEnvelopeNeverTravelsBackwardOrBeyondItsBudget()
    {
        Assert.That(CombatLungeMotion.TravelBetween(.7f, .5f, 1f), Is.Zero);
        Assert.That(CombatLungeMotion.TravelBetween(-1f, 2f, 1.15f), Is.EqualTo(1.15f));
        Assert.That(CombatLungeMotion.TravelBetween(1f, 2f, 1f), Is.Zero);
    }

    [Test]
    public void LungeTrackingFollowsMovingTargetWithoutOvershoot()
    {
        Vector3 player = Vector3.zero;
        Vector3 target = new Vector3(0, 0, 2);
        float budget = 2f;
        for (int i = 0; i < 30; i++)
        {
            target.x += .01f;
            var step = CombatLungeMotion.TrackStep(target - player, 1f, .12f, budget, 9f, 1f / 60f);
            Assert.That(step.magnitude, Is.LessThanOrEqualTo(.15f));
            player += step; budget -= step.magnitude;
            Assert.That(Vector3.Distance(player, target), Is.GreaterThanOrEqualTo(.9999f));
        }
        Assert.That(player.x, Is.GreaterThan(0f));
        Assert.That(Vector3.Distance(player, target), Is.EqualTo(1f).Within(.001f));
        Assert.That(CombatLungeMotion.TrackStep(Vector3.forward * 3, 1, 1, 0, 9, .02f), Is.EqualTo(Vector3.zero));
        Assert.That(CombatLungeMotion.TrackStep(Vector3.forward * .5f, 1, 1, 1, 9, .02f), Is.EqualTo(Vector3.zero));
        Assert.That(CombatLungeMotion.TrackStep(Vector3.forward * 3, 1, 1, 1, 9, 0), Is.EqualTo(Vector3.zero));
    }

    [Test]
    public void ExtendedLungeRangeDoesNotExtendDamageRange()
    {
        var player = new GameObject("Lunge range test player");
        var target = new GameObject("Lunge range test target");
        try
        {
            player.transform.position = new Vector3(12000, 10000, 12000);
            target.transform.position = player.transform.position + Vector3.forward * 2.1f;
            var combat = player.AddComponent<PlayerCombatInput>();
            var collider = target.AddComponent<SphereCollider>();
            collider.radius = .1f;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(PlayerCombatInput).GetField("hitOriginOffset", flags).SetValue(combat, Vector3.zero);
            Physics.SyncTransforms();
            var strict = typeof(PlayerCombatInput).GetMethod("IsAimColliderInRange", flags);
            var extended = typeof(PlayerCombatInput).GetMethod("IsColliderInRange", flags);
            Assert.That((bool)strict.Invoke(combat, new object[] { collider, 0f }), Is.False);
            Assert.That((bool)extended.Invoke(combat, new object[] { collider, combat.LungeAcquireRange, 0f }), Is.True);
            target.transform.position += Vector3.forward * 1f;
            Physics.SyncTransforms();
            Assert.That((bool)extended.Invoke(combat, new object[] { collider, combat.LungeAcquireRange, 0f }), Is.False);
        }
        finally { Object.DestroyImmediate(target); Object.DestroyImmediate(player); }
    }
}
#endif
