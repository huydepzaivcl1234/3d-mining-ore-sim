#if UNITY_EDITOR
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MonsterCombatSettingsTests
{
    [TestCase("Bat")]
    [TestCase("Golem")]
    [TestCase("Mushranon")]
    [TestCase("Mushroom")]
    public void SpeciesUsesExistingGameDataAsset(string species)
    {
        var data = AssetDatabase.LoadAssetAtPath<MonsterRewardData>($"Assets/GameData/Monsters/{species}Rewards.asset");
        Assert.That(data, Is.Not.Null);
        Assert.That(data.HasCombatData, Is.True);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Monsters/{species}Monster.prefab");
        Assert.That(prefab.GetComponent<MushroomMonster>().CombatData, Is.SameAs(data.combat));
    }

    [TestCase(1f, 1f)]
    [TestCase(2f, 2f)]
    [TestCase(0f, .01f)]
    [TestCase(-1f, .01f)]
    public void AttackSpeedIsSafeWithoutChangingAuthoredValue(float authored, float expected)
    {
        var settings = new MonsterCombatSettings { attackSpeed = authored };
        Assert.That(settings.SafeAttackSpeed, Is.EqualTo(expected));
        Assert.That(settings.attackSpeed, Is.EqualTo(authored));
    }

    [Test] public void NonFiniteAttackSpeedCannotBreakAnimationOrCooldown()
    {
        Assert.That(new MonsterCombatSettings { attackSpeed = float.NaN }.SafeAttackSpeed, Is.EqualTo(1f));
        Assert.That(new MonsterCombatSettings { attackSpeed = float.PositiveInfinity }.SafeAttackSpeed, Is.EqualTo(1f));
    }
}
#endif
