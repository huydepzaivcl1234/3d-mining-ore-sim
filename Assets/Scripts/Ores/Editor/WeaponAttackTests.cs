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
    private WeaponAttackData weapon;
    [SetUp] public void SetUp()
    {
        player = new GameObject("Weapon test player");
        objects.Add(player);
        player.transform.position = new Vector3(12000, 10000, 12000);
        combat = player.AddComponent<PlayerCombatInput>();
        typeof(PlayerCombatInput).GetField("targetLayers", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(combat, (LayerMask)(1 << 30));
        weapon = ScriptableObject.CreateInstance<WeaponAttackData>();
        weapon.range = 2.5f;
        weapon.hitOriginOffset = Vector3.zero;
        combat.TryEquipWeapon(weapon);
    }
    [TearDown] public void TearDown()
    {
        foreach (var obj in objects) Object.DestroyImmediate(obj);
        objects.Clear();
        Object.DestroyImmediate(weapon);
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
    [Test] public void FistsHitOnlyClosestForwardTarget()
    {
        weapon.hitMode = WeaponHitMode.StraightSingleTarget;
        weapon.angle = 30;
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
    [Test] public void SwordHitsForwardTargetsOncePerCharacter()
    {
        weapon.hitMode = WeaponHitMode.ForwardSweep;
        weapon.angle = 110;
        var center = Target(Vector3.forward, true);
        var diagonal = Target(new Vector3(1, 0, 1.5f));
        var behind = Target(Vector3.back);
        var distant = Target(Vector3.forward * 4);
        Strike();
        Assert.That(center.Health, Is.EqualTo(center.MaxHealth - combat.Damage));
        Assert.That(diagonal.Health, Is.EqualTo(diagonal.MaxHealth - combat.Damage));
        Assert.That(behind.Health, Is.EqualTo(behind.MaxHealth));
        Assert.That(distant.Health, Is.EqualTo(distant.MaxHealth));
    }
    [Test] public void UnequippingRestoresSingleTargetMode()
    {
        weapon.hitMode = WeaponHitMode.ForwardSweep;
        Assert.That(combat.HitsMultipleTargets, Is.True);
        Assert.That(combat.TryEquipWeapon(null), Is.True);
        Assert.That(combat.HitsMultipleTargets, Is.False);
    }
    [Test] public void ShortEnemyInFrontIsNotRejectedByVerticalAngle()
    {
        weapon.hitOriginOffset = Vector3.up;
        weapon.angle = 30;
        var low = Target(new Vector3(0, 0.35f, 1.1f));
        var overhead = Target(new Vector3(0, 3, 1));
        Strike();
        Assert.That(low.Health, Is.EqualTo(low.MaxHealth - combat.Damage));
        Assert.That(overhead.Health, Is.EqualTo(overhead.MaxHealth));
    }
}
#endif
