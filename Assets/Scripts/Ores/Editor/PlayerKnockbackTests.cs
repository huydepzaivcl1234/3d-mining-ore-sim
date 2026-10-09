#if UNITY_EDITOR
using System.Linq;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public sealed class PlayerKnockbackTests
{
    [Test] public void PlayerControllerHasNoDeathOrGetUpAnimation()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/GameData/Player/Animations/Player controller.controller");
        Assert.That(controller.layers[0].stateMachine.states.Any(x => x.state.name == "Death" || x.state.name == "Getting Up"), Is.False);
        Assert.That(controller.layers[0].stateMachine.states.Any(x => x.state.name == "Default"), Is.True);
    }
    [Test] public void OrdinaryMonsterHitsDoNotAutomaticallyKnockDown()
    {
        var settings = new MonsterCombatSettings();
        Assert.That(settings.knockbackSpeed, Is.Zero);
        Assert.That(settings.knockbackLift, Is.Zero);
    }
    [Test] public void EditModeNeverSimulatesKnockbackOrDeath()
    {
        var go = new GameObject("Death ragdoll test"); go.SetActive(false);
        try
        {
            var component = go.AddComponent<PlayerKnockbackRagdoll>();
            Assert.That(component.State, Is.EqualTo(PlayerKnockbackRagdoll.KnockdownState.Normal));
            Assert.That(component.HasLanded, Is.False);
            Assert.That(component.ApplyKnockback(Vector3.forward), Is.False);
            Assert.That(component.BeginDeathRagdoll(Vector3.up), Is.False);
            Assert.That(new SerializedObject(component).FindProperty("getUpClip"), Is.Null);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
#endif
