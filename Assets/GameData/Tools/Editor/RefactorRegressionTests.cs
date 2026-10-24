#if UNITY_INCLUDE_TESTS
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEngine;

public sealed class RefactorRegressionTests
{
    [Test]
    public void ExperienceCarriesAcrossMultipleGrowingLevels()
    {
        var progress = new ExperienceProgression();
        progress.Set(1, 20f, 100f);
        progress.Add(300f, 1.5f);
        Assert.That(progress.Level, Is.EqualTo(3));
        Assert.That(progress.Experience, Is.EqualTo(70f));
        Assert.That(progress.Required, Is.EqualTo(225f));
    }

    [Test]
    public void FlatExperienceGrowthHandlesLargeAwardsWithoutOverflowingLevel()
    {
        var progress = new ExperienceProgression();
        progress.Set(int.MaxValue - 1, 0f, 100f);
        progress.Add(float.MaxValue, 1f);
        Assert.That(progress.Level, Is.EqualTo(int.MaxValue));
        Assert.That(progress.Experience, Is.EqualTo(100f));
    }

    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    public void InvalidExperienceDoesNotChangeProgress(float amount)
    {
        var progress = new ExperienceProgression();
        progress.Set(6, 25f, 200f);
        progress.Add(amount, 1.25f);
        Assert.That(progress.Level, Is.EqualTo(6));
        Assert.That(progress.Experience, Is.EqualTo(25f));
    }

    [TestCase(typeof(JuicyUpgradePanel))]
    [TestCase(typeof(JuicyQuestPanel))]
    [TestCase(typeof(JuicySettingsPanel))]
    public void ClosingPanelRestoresTheDesignerScale(System.Type panelType)
    {
        var root = new GameObject("Panel lifecycle test", typeof(RectTransform));
        root.SetActive(false);
        try
        {
            var panel = root.AddComponent(panelType);
            var body = root.GetComponent<RectTransform>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            panelType.GetField("panelBody", flags).SetValue(panel, body);
            Vector3 authored = new Vector3(0.7f, 0.8f, 1f);
            body.localScale = authored;
            typeof(AnimatedPanel).GetMethod("Awake", flags).Invoke(panel, null);
            body.localScale = authored * 0.5f;
            panelType.GetMethod("OnDisable", flags).Invoke(panel, null);
            Assert.That(body.localScale, Is.EqualTo(authored));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void BurnRefreshDoesNotPostponeNextTick()
    {
        var effect = new DamageOverTime();
        float damage = 0f;
        effect.Apply(4f, 1f, 5f);
        effect.Tick(0.75f, amount => damage += amount);
        effect.Apply(4f, 1f, 5f);
        effect.Tick(0.25f, amount => damage += amount);
        Assert.That(damage, Is.EqualTo(4f));
    }

    [Test]
    public void WeakerBurnOnlyExtendsDuration()
    {
        var effect = new DamageOverTime();
        float damage = 0f;
        effect.Apply(8f, 1f, 1f);
        effect.Apply(1f, 1f, 3f);
        effect.Tick(3f, amount => damage += amount);
        Assert.That(damage, Is.EqualTo(24f));
        Assert.That(effect.IsActive, Is.False);
    }

    [Test]
    public void StrongerBurnPreservesFractionalTickProgress()
    {
        var effect = new DamageOverTime();
        float damage = 0f;
        effect.Apply(2f, 2f, 5f);
        effect.Tick(1f, amount => damage += amount);
        effect.Apply(4f, 1f, 5f);
        effect.Tick(0.5f, amount => damage += amount);
        Assert.That(damage, Is.EqualTo(4f));
    }

    [Test]
    public void BurnCatchesUpOnlyWithinItsLifetime()
    {
        var effect = new DamageOverTime();
        float damage = 0f;
        effect.Apply(3f, 1f, 2.5f);
        effect.Tick(10f, amount => damage += amount);
        effect.Tick(10f, amount => damage += amount);
        Assert.That(damage, Is.EqualTo(6f));
    }

    [Test]
    public void ClearingBurnDuringDamageStopsCatchUp()
    {
        var effect = new DamageOverTime();
        int ticks = 0;
        effect.Apply(3f, 1f, 5f);
        effect.Tick(5f, amount => { ticks++; effect.Clear(); });
        Assert.That(ticks, Is.EqualTo(1));
        Assert.That(effect.IsActive, Is.False);
    }
}
#endif
