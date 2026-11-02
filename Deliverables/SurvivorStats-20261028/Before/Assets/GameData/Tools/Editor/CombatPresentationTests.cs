#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEngine;

public sealed class CombatPresentationTests
{
    [Test]
    public void PeriodicContextSurvivesNestedDirectDamageAndDoesNotLeak()
    {
        var root = new GameObject("Periodic context test"); root.SetActive(false);
        var health = root.AddComponent<MiningCharacterHealth>(); health.Respawn();
        bool nested = false; var events = new List<bool>();
        System.Action listener = () => { if (!nested) { nested = true; health.DealDamage(1, CombatDamageType.True, null); } };
        System.Action<MiningCharacterHealth, float, GameObject, bool, CombatDamageType> observer = (victim, amount, source, basic, type) => { if (victim == health) events.Add(victim.IsPeriodicDamage); };
        try
        {
            health.Damaged += listener; MiningCharacterHealth.AnyDamaged += observer;
            health.DealPeriodicDamage(2, CombatDamageType.True, null);
            Assert.That(events, Is.EqualTo(new[] { false, true }));
            Assert.That(health.IsPeriodicDamage, Is.False);
            Assert.That(health.Health, Is.EqualTo(97));
        }
        finally { health.Damaged -= listener; MiningCharacterHealth.AnyDamaged -= observer; Object.DestroyImmediate(root); }
    }
    [Test]
    public void PeriodicContextRestoresWhenObserverThrows()
    {
        var root = new GameObject("Periodic exception test"); root.SetActive(false);
        var health = root.AddComponent<MiningCharacterHealth>(); health.Respawn();
        System.Action listener = () => throw new System.InvalidOperationException("Test listener");
        try
        {
            health.Damaged += listener;
            Assert.Throws<System.InvalidOperationException>(() => health.DealPeriodicDamage(1, CombatDamageType.True, null));
            Assert.That(health.IsPeriodicDamage, Is.False);
        }
        finally { health.Damaged -= listener; Object.DestroyImmediate(root); }
    }
    [Test]
    public void PopupTotalsRetainEditableSizeLimitsAndThreeSecondDefault()
    {
        var settings = new MiningDamagePopupSettings { minimumFontSize = 10, maximumFontSize = 40, damageForMinimumSize = 1, damageForMaximumSize = 100 };
        Assert.That(settings.periodicQuietSeconds, Is.EqualTo(3));
        Assert.That(settings.FontSizeForDamage(1), Is.EqualTo(10));
        Assert.That(settings.FontSizeForDamage(100000), Is.EqualTo(40));
        Assert.That(MiningDamagePopup.EvaluateHeight(0, settings), Is.Zero);
        Assert.That(MiningDamagePopup.EvaluateHeight(1, settings), Is.EqualTo(-settings.fallDistance).Within(.0001f));
        Assert.That(settings.accumulateMagicAndTrueDamage, Is.True);
        Assert.That(settings.minimumPixelSpacing, Is.GreaterThan(0));
    }
    [Test]
    public void OnlyGunBasicHitsCarryGunContextAndNestedEffectsDoNotInheritIt()
    {
        var target = new GameObject("Gun context target"); target.SetActive(false);
        var source = new GameObject("Gun context source"); source.SetActive(false);
        var health = target.AddComponent<MiningCharacterHealth>(); health.ConfigureSpawnHealth(1000);
        source.AddComponent<GameData.Player.Script.PistolShooting>();
        var values = new List<bool>(); bool nested = false;
        System.Action listener = () =>
        {
            values.Add(health.IsGunShot);
            if (health.IsGunShot && !nested) { nested = true; health.DealDamage(1, CombatDamageType.True, source); values.Add(health.IsGunShot); }
        };
        try
        {
            health.Damaged += listener;
            health.DealDamageWithTrueBonus(1, CombatDamageType.Physical, source, 0, true);
            health.DealPeriodicDamage(1, CombatDamageType.Magic, source);
            health.DealDamageWithTrueBonus(1, CombatDamageType.Physical, null, 0, true);
            Assert.That(values, Is.EqualTo(new[] { true, false, true, false, false }));
            Assert.That(health.IsGunShot, Is.False);
        }
        finally { health.Damaged -= listener; Object.DestroyImmediate(target); Object.DestroyImmediate(source); }
    }
}
#endif
