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
            data.maximumHigherLevelChancePercent = 99f;
            Assert.That(data.RollLevel(0.9999f), Is.GreaterThan(100));
            Assert.That(data.StatMultiplier(3), Is.EqualTo(2.5f));
            Assert.That(data.GoldMultiplier(3), Is.EqualTo(2.5f));
        }
        finally { Object.DestroyImmediate(data); }
    }
    [Test] public void DayGrowthUsesPercentagePointsAndCapsProbability()
    {
        var data = ScriptableObject.CreateInstance<MonsterRewardData>();
        try
        {
            Assert.That(data.HigherLevelProbability(1), Is.EqualTo(.35f).Within(.000001f));
            Assert.That(data.HigherLevelProbability(2), Is.EqualTo(.3501f).Within(.000001f));
            Assert.That(data.HigherLevelProbability(1001), Is.EqualTo(.45f).Within(.000001f));
            Assert.That(data.HigherLevelProbability(1501), Is.EqualTo(.5f).Within(.000001f));
            Assert.That(data.HigherLevelProbability(int.MaxValue), Is.EqualTo(.5f).Within(.000001f));
            Assert.That(data.HigherLevelProbability(0), Is.EqualTo(data.HigherLevelProbability(1)));
            Assert.That(data.RollLevel(.49f, int.MaxValue), Is.EqualTo(1));
            Assert.That(data.RollLevel(.75f, int.MaxValue), Is.EqualTo(3));
            Assert.That(data.GetSpawnLevel(1, 100), Is.EqualTo(1));
            data.addPlayerLevelAtSpawn = true;
            Assert.That(data.GetSpawnLevel(5, 6), Is.EqualTo(11));
        }
        finally { Object.DestroyImmediate(data); }
    }
    [Test] public void LaterDaysIncreaseHighLevelRollsWithoutRemovingLevelOne()
    {
        var data = ScriptableObject.CreateInstance<MonsterRewardData>();
        try
        {
            int earlyHigh = 0, lateHigh = 0, lateLevelThree = 0;
            const int count = 10000;
            for (int i = 0; i < count; i++)
            {
                float sample = (i + .5f) / count;
                if (data.RollLevel(sample, 1) > 1) earlyHigh++;
                int late = data.RollLevel(sample, 10000);
                if (late > 1) lateHigh++;
                if (late >= 3) lateLevelThree++;
            }
            Assert.That(earlyHigh, Is.EqualTo(3500));
            Assert.That(lateHigh, Is.EqualTo(5000));
            Assert.That(lateLevelThree, Is.EqualTo(2500));
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
