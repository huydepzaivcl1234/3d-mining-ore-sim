#if UNITY_INCLUDE_TESTS
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEngine;

public sealed class ArmorAndRelativeLevelTests
{
    [Test]
    public void DamageTypesUseTheirOwnResistance()
    {
        Assert.That(CombatDamage.Resolve(100, CombatDamageType.Physical, 100, 300, 100), Is.EqualTo(50));
        Assert.That(CombatDamage.Resolve(100, CombatDamageType.Magic, 100, 300, 100), Is.EqualTo(25));
        Assert.That(CombatDamage.Resolve(1, CombatDamageType.Physical, 900, 0, 100), Is.EqualTo(.1f).Within(.00001f));
        Assert.That(CombatDamage.Resolve(1, CombatDamageType.Magic, 0, 9900, 100), Is.EqualTo(.01f).Within(.00001f));
        Assert.That(CombatDamage.ReductionFraction(100), Is.EqualTo(.5f));
        Assert.That(CombatDamage.Resolve(100, CombatDamageType.Physical, 100, 0, 200), Is.EqualTo(200f / 3f).Within(.0001f));
        Assert.That(CombatDamage.Resolve(100, CombatDamageType.True, 100, 300, 100), Is.EqualTo(100));
        Assert.That(CombatDamage.Resolve(100, CombatDamageType.Physical, 0, 300, 100), Is.EqualTo(100));
        Assert.That(CombatDamage.Resolve(float.NaN, CombatDamageType.Physical, 0, 0, 100), Is.Zero);
    }

    [Test]
    public void HealthReturnsMitigatedDamageAndOverkillIsCapped()
    {
        var root = new GameObject("Isolated resistance test");
        root.SetActive(false);
        try
        {
            var health = root.AddComponent<MiningCharacterHealth>();
            health.ConfigureDefenses(100, 300);
            health.Respawn();
            Assert.That(health.DealDamage(100, CombatDamageType.Physical), Is.EqualTo(50));
            Assert.That(health.Health, Is.EqualTo(50));
            Assert.That(health.DealDamage(100, CombatDamageType.Magic), Is.EqualTo(25));
            Assert.That(health.DealDamage(1000, CombatDamageType.True), Is.EqualTo(25));
            Assert.That(health.Health, Is.Zero);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void PlayerDefensesFollowCurrentLevel()
    {
        var root = new GameObject("Isolated player defense test");
        root.SetActive(false);
        var data = ScriptableObject.CreateInstance<MiningPlayerStatsData>();
        var player = root.AddComponent<MiningPlayerStats>();
        var field = typeof(MiningPlayerStats).GetField("data", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        try
        {
            data.armor = 10; data.magicResistance = 20;
            data.armorPerLevel = 2; data.magicResistancePerLevel = 3;
            field.SetValue(player, data);
            player.SetProgress(6, 0, 100);
            Assert.That(player.Armor, Is.EqualTo(20));
            Assert.That(player.MagicResistance, Is.EqualTo(35));
            player.SetProgress(1, 0, 100);
            Assert.That(player.Armor, Is.EqualTo(10));
            Assert.That(player.MagicResistance, Is.EqualTo(20));
        }
        finally
        {
            field.SetValue(player, null); // Teardown must not save temporary progress.
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void RelativeLevelsUseTwentySeventyTenWithoutDailyInflation()
    {
        var data = ScriptableObject.CreateInstance<MonsterRewardData>();
        try
        {
            int earlyHigher = 0, lateHigher = 0, equal = 0, weaker = 0, firstLevelHigher = 0;
            for (int i = 0; i < 10000; i++)
            {
                float sample = (i + .5f) / 10000f;
                int early = data.RollSpawnLevel(sample, 30, 1);
                int late = data.RollSpawnLevel(sample, 30, 10000);
                Assert.That(early, Is.InRange(26, 31));
                Assert.That(late, Is.EqualTo(early));
                if (early == 30) equal++;
                if (early < 30) weaker++;
                if (early > 30) earlyHigher++;
                if (late > 30) lateHigher++;
                int first = data.RollSpawnLevel(sample, 1, 1);
                Assert.That(first, Is.InRange(1, 2));
                if (first > 1) firstLevelHigher++;
            }
            Assert.That(weaker, Is.EqualTo(2000));
            Assert.That(equal, Is.EqualTo(7000));
            Assert.That(earlyHigher, Is.EqualTo(1000));
            Assert.That(lateHigher, Is.EqualTo(1000));
            Assert.That(firstLevelHigher, Is.EqualTo(1000));
            Assert.That(data.RollSpawnLevel(0, 30, 1), Is.EqualTo(29));
            Assert.That(data.RollSpawnLevel(1, 30, 1), Is.EqualTo(31));
            Assert.That(data.RollSpawnLevel(1, int.MaxValue, 1), Is.EqualTo(int.MaxValue));
        }
        finally { Object.DestroyImmediate(data); }
    }

    [Test] public void RelativeWeightsAndOffsetsRemainDesignerConfigurable()
    {
        var data = ScriptableObject.CreateInstance<MonsterRewardData>();
        try
        {
            data.weakerLevelWeight = data.equalLevelWeight = data.strongerLevelWeight = 0;
            Assert.That(data.RollSpawnLevel(1, 10, 1), Is.EqualTo(10));
            data.strongerLevelWeight = 1;
            data.maximumLevelsAbovePlayer = 3;
            Assert.That(data.RollSpawnLevel(1, 10, 1), Is.EqualTo(13));
            data.strongerLevelWeight = float.NaN;
            Assert.That(data.RollSpawnLevel(float.NaN, 10, 1), Is.EqualTo(10));
            data.usePlayerRelativeLevels = false;
            Assert.That(data.RollSpawnLevel(.75f, 10, 1), Is.EqualTo(data.RollLevel(.75f, 1)));
        }
        finally { Object.DestroyImmediate(data); }
    }
}
#endif
