#if UNITY_INCLUDE_TESTS
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEngine;

public sealed class MonsterRewardProgressionTests
{
    private GameObject player;
    private MiningPlayerStats stats;
    [SetUp] public void SetUp()
    {
        player = new GameObject("Progression Test");
        stats = player.AddComponent<MiningPlayerStats>();
        stats.SetProgress(1, 0, 100);
    }
    [TearDown] public void TearDown() => Object.DestroyImmediate(player);
    [Test] public void RewardAdvancesProgress()
    {
        stats.AddExperience(25);
        Assert.That(stats.Level, Is.EqualTo(1));
        Assert.That(stats.Experience, Is.EqualTo(25));
        Assert.That(stats.ExperienceProgress, Is.EqualTo(0.25f));
    }
    [Test] public void LevelUpKeepsOverflow()
    {
        stats.AddExperience(135);
        Assert.That(stats.Level, Is.EqualTo(2));
        Assert.That(stats.Experience, Is.EqualTo(35));
        Assert.That(stats.ExperienceRequired, Is.EqualTo(125));
    }
    [Test] public void RewardCanAdvanceMultipleLevels()
    {
        stats.AddExperience(250);
        Assert.That(stats.Level, Is.EqualTo(3));
        Assert.That(stats.Experience, Is.EqualTo(25));
        Assert.That(stats.ExperienceRequired, Is.EqualTo(156.25f));
    }
    [Test] public void InvalidRewardsDoNotChangeProgress()
    {
        stats.AddExperience(-10);
        stats.AddExperience(float.NaN);
        stats.AddExperience(float.PositiveInfinity);
        Assert.That(stats.Level, Is.EqualTo(1));
        Assert.That(stats.Experience, Is.Zero);
    }
    [Test] public void HigherLevelsAreRareAndNotCappedAtSmallLevels()
    {
        var data = ScriptableObject.CreateInstance<MonsterRewardData>();
        try
        {
            data.higherLevelChance = 0.5f;
            Assert.That(data.RollLevel(0), Is.EqualTo(1));
            Assert.That(data.RollLevel(0.5f), Is.EqualTo(2));
            Assert.That(data.RollLevel(0.75f), Is.EqualTo(3));
            data.higherLevelChance = 0.99f;
            Assert.That(data.RollLevel(0.9999f), Is.GreaterThan(100));
            Assert.That(data.StatMultiplier(3), Is.EqualTo(2.5f));
            Assert.That(data.GoldMultiplier(3), Is.EqualTo(2.5f));
        }
        finally { Object.DestroyImmediate(data); }
    }
    [Test] public void PlayerLevelAddsHealthAndDamage()
    {
        var data = ScriptableObject.CreateInstance<MiningPlayerStatsData>();
        try
        {
            typeof(MiningPlayerStats).GetField("data", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(stats, data);
            stats.SetProgress(3, 0, 100);
            Assert.That(stats.MaxHealth, Is.EqualTo(120));
            Assert.That(stats.Damage, Is.EqualTo(3));
        }
        finally { Object.DestroyImmediate(data); }
    }
}
#endif
