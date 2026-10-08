#if UNITY_EDITOR
using System.Linq;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ForestGolemTests
{
    [Test] public void PrefabUsesSpeciesDataAndChargeAnimation()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monsters/ForestGolem.prefab");
        var data = AssetDatabase.LoadAssetAtPath<MonsterRewardData>("Assets/GameData/Monsters/ForestGolemRewards.asset");
        Assert.That(prefab.GetComponent<MushroomMonster>().CombatData, Is.SameAs(data.combat));
        Assert.That(data.combat.forestGolem.enabled, Is.True);
        Assert.That(prefab.GetComponent<ForestGolemAbility>(), Is.Not.Null);
        var controller = (UnityEditor.Animations.AnimatorController)prefab.GetComponent<Animator>().runtimeAnimatorController;
        Assert.That(controller.layers[0].stateMachine.states.Any(s => s.state.name == data.combat.forestGolem.chargeState && s.state.motion != null), Is.True);
        Assert.That(data.combat.forestGolem.waveMaterial, Is.Not.Null);
    }

    [Test] public void RosterIncludesForestGolemOnlyOnce()
    {
        var roster = AssetDatabase.LoadAssetAtPath<MonsterSpawnRoster>("Assets/Resources/MonsterSpawnRoster.asset");
        Assert.That(roster.Entries.Count(e => e.prefab != null && e.prefab.GetComponent<ForestGolemAbility>() != null), Is.EqualTo(1));
    }

    [Test] public void NewAbilityDoesNotEnableOnExistingSpecies()
    {
        foreach (string species in new[] { "Bat", "Golem", "Mushranon", "Mushroom" })
        {
            var data = AssetDatabase.LoadAssetAtPath<MonsterRewardData>($"Assets/GameData/Monsters/{species}Rewards.asset");
            Assert.That(data.combat.forestGolem.enabled, Is.False, species);
        }
    }
}
#endif
