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
    public void FootworkIncludesHipsAndLegsWithoutRootMotionMask()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var layer = controller.layers.Single(l => l.name == "Combat Footwork");
        Assert.That(layer.defaultWeight, Is.Zero);
        Assert.That(layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Body), Is.True);
        Assert.That(layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg), Is.True);
        Assert.That(layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg), Is.True);
        Assert.That(layer.avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Root), Is.False);
    }

    [TestCase("Sword Attack 1")]
    [TestCase("Sword Attack 2")]
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
}
#endif
