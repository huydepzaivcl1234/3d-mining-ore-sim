#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEngine;

public sealed class WeaponAttackTests
{
    private readonly List<GameObject> objects = new();
    private GameObject player;
    private PlayerCombatInput combat;
    [SetUp] public void SetUp()
    {
        player = new GameObject("Weapon test player");
        objects.Add(player);
        player.transform.position = new Vector3(12000, 10000, 12000);
        combat = player.AddComponent<PlayerCombatInput>();
        typeof(PlayerCombatInput).GetField("targetLayers", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(combat, (LayerMask)(1 << 30));
        typeof(PlayerCombatInput).GetField("attackRange", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(combat, 2.5f);
        typeof(PlayerCombatInput).GetField("hitOriginOffset", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(combat, Vector3.zero);
    }
    [TearDown] public void TearDown()
    {
        foreach (var obj in objects) Object.DestroyImmediate(obj);
        objects.Clear();
    }
    private MiningCharacterHealth Target(Vector3 offset, bool secondCollider = false)
    {
        var obj = new GameObject("Weapon test target") { layer = 30 };
        objects.Add(obj);
        obj.transform.position = player.transform.position + offset;
        obj.AddComponent<SphereCollider>().radius = 0.1f;
        if (secondCollider) obj.AddComponent<BoxCollider>().size = Vector3.one * 0.1f;
        var health = obj.AddComponent<MiningCharacterHealth>();
        health.Respawn();
        return health;
    }
    private void Strike()
    {
        Physics.SyncTransforms();
        typeof(PlayerCombatInput).GetMethod("ApplyHit", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(combat, null);
    }
    private void Sweep()
    {
        Physics.SyncTransforms();
        typeof(PlayerCombatInput).GetMethod("ApplySweepHit", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(combat, null);
    }
    [Test] public void CombatBindingsMatchRequestedControls()
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        foreach (var binding in new[] {
            ("toggleCombat", "<Keyboard>/e"),
            ("attack", "<Mouse>/leftButton"),
            ("autoAim", "<Keyboard>/f") })
        {
            var action = (UnityEngine.InputSystem.InputAction)typeof(PlayerCombatInput)
                .GetField(binding.Item1, flags).GetValue(combat);
            Assert.That(action.bindings[0].path, Is.EqualTo(binding.Item2));
        }
    }

    [Test] public void SecondCombatToggleReturnsToStandingAndDisableResetsIt()
    {
        combat.SetCombatMode(true);
        Assert.That(combat.IsCombatMode, Is.True);
        combat.SetCombatMode(false);
        Assert.That(combat.IsCombatMode, Is.False);
        combat.SetCombatMode(true);
        combat.enabled = false;
        Assert.That(combat.IsCombatMode, Is.False);
    }

    [Test] public void AutoAimSelectsNearestLivingMonsterNotOrdinaryHealthTarget()
    {
        Target(Vector3.forward * 0.5f);
        var far = Target(Vector3.forward * 4f);
        far.gameObject.AddComponent<MushroomMonster>();
        var near = Target(Vector3.forward * 2f);
        var expected = near.gameObject.AddComponent<MushroomMonster>();
        var dead = Target(Vector3.back);
        dead.gameObject.AddComponent<MushroomMonster>();
        dead.ApplyDamage(dead.MaxHealth);
        Physics.SyncTransforms();
        var result = typeof(PlayerCombatInput).GetMethod("FindNearestMonster",
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(combat, null);
        Assert.That(result, Is.SameAs(expected));
    }
    [Test] public void FistsHitOnlyClosestForwardTarget()
    {
        var near = Target(Vector3.forward);
        var far = Target(Vector3.forward * 2);
        var behind = Target(Vector3.back);
        var side = Target(Vector3.right);
        Strike();
        Assert.That(near.Health, Is.EqualTo(near.MaxHealth - combat.Damage));
        Assert.That(far.Health, Is.EqualTo(far.MaxHealth));
        Assert.That(behind.Health, Is.EqualTo(behind.MaxHealth));
        Assert.That(side.Health, Is.EqualTo(side.MaxHealth));
    }
    [Test] public void ShortEnemyInFrontIsNotRejectedByVerticalAngle()
    {
        typeof(PlayerCombatInput).GetField("hitOriginOffset", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(combat, Vector3.up);
        var low = Target(new Vector3(0, 0.35f, 1.1f));
        var overhead = Target(new Vector3(0, 3, 1));
        Strike();
        Assert.That(low.Health, Is.EqualTo(low.MaxHealth - combat.Damage));
        Assert.That(overhead.Health, Is.EqualTo(overhead.MaxHealth));
    }

    [Test] public void UpswingHitsMultipleTargetsInFrontOnlyOnceEach()
    {
        var left = Target(new Vector3(-0.7f, 0, 1.2f), true);
        var right = Target(new Vector3(0.7f, 0, 1.2f));
        var behind = Target(Vector3.back);
        Sweep();
        Assert.That(left.Health, Is.EqualTo(left.MaxHealth - combat.Damage));
        Assert.That(right.Health, Is.EqualTo(right.MaxHealth - combat.Damage));
        Assert.That(behind.Health, Is.EqualTo(behind.MaxHealth));
    }
}
#endif
