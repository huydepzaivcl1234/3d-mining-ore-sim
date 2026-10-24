#if UNITY_EDITOR
using NUnit.Framework;
using MiningSimulator.Ores;

public sealed class BossSkillTests
{
    [Test] public void MushroomHealsHalfMaximumAcrossFiveSecondsOnlyOnce()
    {
        var state = new BossSkillRuntime();
        state.Configure(new MonsterBossSettings { skill = BossSkillKind.MushroomHealing });
        state.NotifyHealth(21f, 100f);
        Assert.AreEqual(0f, state.TickHealing(1f, 100f));
        state.NotifyHealth(20f, 100f);
        Assert.AreEqual(10f, state.TickHealing(1f, 100f), .001f);
        Assert.AreEqual(40f, state.TickHealing(8f, 100f), .001f);
        state.NotifyHealth(10f, 100f);
        Assert.AreEqual(0f, state.TickHealing(5f, 100f));
    }
    [Test] public void DeadMushroomCannotStartHealAndResetAllowsNewSpawn()
    {
        var state = new BossSkillRuntime();
        var settings = new MonsterBossSettings { skill = BossSkillKind.MushroomHealing };
        state.Configure(settings); state.NotifyHealth(0f, 100f);
        Assert.AreEqual(0f, state.TickHealing(5f, 100f));
        state.NotifyHealth(10f, 100f); state.TickHealing(5f, 100f);
        state.Configure(settings); state.NotifyHealth(10f, 100f);
        Assert.AreEqual(50f, state.TickHealing(5f, 100f), .001f);
    }
    [Test] public void GolemProcsEveryThirdCommittedStrike()
    {
        var state = new BossSkillRuntime();
        state.Configure(new MonsterBossSettings { skill = BossSkillKind.GolemSlow });
        for (int i = 1; i <= 9; i++) Assert.AreEqual(i % 3 == 0, state.NotifyStrike());
    }
    [Test] public void BatUsesActualCumulativePlayerDamageAndDoesNotStack()
    {
        var state = new BossSkillRuntime();
        state.Configure(new MonsterBossSettings { skill = BossSkillKind.BatHaste });
        Assert.IsFalse(state.NotifyPlayerDamage(0f, 100f));
        Assert.IsFalse(state.NotifyPlayerDamage(2f, 100f));
        Assert.IsTrue(state.NotifyPlayerDamage(3f, 100f));
        Assert.AreEqual(2f, state.AttackSpeedMultiplier);
        Assert.IsFalse(state.NotifyPlayerDamage(50f, 100f));
        Assert.AreEqual(2f, state.AttackSpeedMultiplier);
    }
    [Test] public void NormalMonstersHaveNoBossAbilities()
    {
        var state = new BossSkillRuntime(); state.Configure(null);
        state.NotifyHealth(10f, 100f);
        Assert.AreEqual(0f, state.TickHealing(5f, 100f));
        Assert.IsFalse(state.NotifyStrike());
        Assert.IsFalse(state.NotifyPlayerDamage(100f, 100f));
        Assert.AreEqual(1f, state.AttackSpeedMultiplier);
    }
}
#endif
