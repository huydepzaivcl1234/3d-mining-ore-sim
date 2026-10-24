#if UNITY_EDITOR
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEngine;
using System.Reflection;

public sealed class MonsterRetaliationTests
{
    [Test] public void OutsideRangeForThreeContinuousSecondsReleasesTarget()
    {
        var clock = new MonsterRetaliationClock(); clock.Acquire();
        Assert.That(clock.Tick(true, false, 2.9f, 3f), Is.False);
        Assert.That(clock.Active, Is.True);
        Assert.That(clock.Tick(true, false, .11f, 3f), Is.True);
        Assert.That(clock.Active, Is.False);
    }
    [Test] public void ReenteringRangeRestartsTheOutsideTimer()
    {
        var clock = new MonsterRetaliationClock(); clock.Acquire();
        clock.Tick(true, false, 2.9f, 3f);
        Assert.That(clock.Tick(true, true, .2f, 3f), Is.False);
        Assert.That(clock.OutsideSeconds, Is.Zero);
        Assert.That(clock.Tick(true, false, 2.9f, 3f), Is.False);
        Assert.That(clock.Tick(true, false, .11f, 3f), Is.True);
    }
    [Test] public void DeadTargetReleasesImmediatelyAndResetClearsPoolingState()
    {
        var clock = new MonsterRetaliationClock(); clock.Acquire();
        Assert.That(clock.Tick(false, false, 0f, 3f), Is.True);
        clock.Acquire(); clock.Tick(true, false, 2f, 3f); clock.Reset();
        Assert.That(clock.Active, Is.False); Assert.That(clock.OutsideSeconds, Is.Zero);
        Assert.That(clock.Tick(true, false, 9f, 3f), Is.False);
    }
    [Test] public void PlayerDamageSelectsAttackerAndExpiryStopsTheOverride()
    {
        var player = new GameObject("Retaliation test player");
        var enemy = new GameObject("Retaliation test monster");
        var other = new GameObject("Retaliation test unrelated source");
        var chestObject = new GameObject("Retaliation test chest");
        var chestData = ScriptableObject.CreateInstance<TreasureChestData>();
        var activeField = typeof(TreasureChest).GetField("<Active>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        var previousChest = activeField.GetValue(null);
        enemy.SetActive(false);
        try
        {
            var playerHealth = player.AddComponent<MiningCharacterHealth>();
            playerHealth.ConfigureSpawnHealth(100f);
            var hp = enemy.AddComponent<MiningCharacterHealth>(); hp.ConfigureSpawnHealth(100f);
            var monster = enemy.AddComponent<MushroomMonster>();
            Set(monster, "health", hp); Set(monster, "playerTarget", playerHealth);
            Set(monster, "legacy_detectionRange", 6f);
            var chestHealth = chestObject.AddComponent<MiningCharacterHealth>(); chestHealth.ConfigureSpawnHealth(100f);
            var chest = chestObject.AddComponent<TreasureChest>();
            Set(chest, "health", chestHealth); Set(chest, "data", chestData); Set(chest, "persistProgress", false);
            activeField.SetValue(null, chest);
            hp.DealDamage(1f, CombatDamageType.Physical, other);
            Call(monster, "RememberPlayerAttacker"); Assert.That(monster.IsRetaliating, Is.False);
            hp.DealDamage(1f, CombatDamageType.Physical, player);
            Call(monster, "RememberPlayerAttacker");
            Assert.That(monster.IsRetaliating, Is.True);
            Assert.That(Call(monster, "TrySelectRetaliationTarget"), Is.EqualTo(true));
            Assert.That(Get(monster, "target"), Is.SameAs(playerHealth));
            player.transform.position = Vector3.right * 20f;
            Call(monster, "TickRetaliation", 2.9f);
            // Further damage outside range must not keep the pursuit alive forever.
            hp.DealDamage(1f, CombatDamageType.Physical, player); Call(monster, "RememberPlayerAttacker");
            Call(monster, "TickRetaliation", .11f);
            Assert.That(monster.IsRetaliating, Is.False);
            Assert.That(Get(monster, "returningFromRetaliation"), Is.EqualTo(true));
            Assert.That(Call(monster, "TrySelectRetaliationTarget"), Is.EqualTo(false));
            Call(monster, "UpdatePlayerTarget", .1f);
            Assert.That(Get(monster, "target"), Is.SameAs(chestHealth));
        }
        finally
        {
            Object.DestroyImmediate(chestObject); activeField.SetValue(null, previousChest);
            Object.DestroyImmediate(chestData);
            Object.DestroyImmediate(enemy); Object.DestroyImmediate(player); Object.DestroyImmediate(other);
        }
    }
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static object Get(object obj, string name) => obj.GetType().GetField(name, Flags).GetValue(obj);
    private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Flags).SetValue(obj, value);
    private static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Flags).Invoke(obj, args);
}
#endif
