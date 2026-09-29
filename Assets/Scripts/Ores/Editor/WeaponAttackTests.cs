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

    [Test] public void MonsterDamageDoesNotCancelCommittedHeadbutt()
    {
        var monster = new GameObject("Monster attack test");
        objects.Add(monster);
        monster.AddComponent<Animator>();
        var health = monster.AddComponent<MiningCharacterHealth>();
        var brain = monster.AddComponent<MushroomMonster>();
        var state = typeof(MushroomMonster).GetField("animationState",
            BindingFlags.Instance | BindingFlags.NonPublic);
        int headbutt = Animator.StringToHash("Headbutt");
        state.SetValue(brain, headbutt);

        health.ApplyDamage(1f);

        Assert.That(state.GetValue(brain), Is.EqualTo(headbutt));
    }

    [Test] public void MonsterStrikeUsesColliderAndRejectsTargetsBehindIt()
    {
        var monster = new GameObject("Monster range test");
        objects.Add(monster);
        var brain = monster.AddComponent<MushroomMonster>();
        var playerHealth = player.AddComponent<MiningCharacterHealth>();
        var playerCollider = player.AddComponent<CapsuleCollider>();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(MushroomMonster).GetField("target", flags).SetValue(brain, playerHealth);
        typeof(MushroomMonster).GetField("targetCollider", flags).SetValue(brain, playerCollider);
        monster.transform.position = player.transform.position - Vector3.forward * 1.8f;
        Physics.SyncTransforms();
        var canHit = typeof(MushroomMonster).GetMethod("CanHitTarget", flags);
        Assert.That(canHit.Invoke(brain, null), Is.EqualTo(true));

        monster.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        Assert.That(canHit.Invoke(brain, null), Is.EqualTo(false));
    }

    [Test] public void SwordDrawSheathAndRespawnResetKeepExactlyOneSword()
    {
        var hand = new GameObject("Hand Holder");
        var sheath = new GameObject("Sheath Holder");
        var swordPrefab = new GameObject("Test Sword");
        objects.Add(swordPrefab);
        hand.transform.SetParent(player.transform);
        sheath.transform.SetParent(player.transform);
        var equipment = player.AddComponent<EquipmentSystem>();
        var fields = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(EquipmentSystem).GetField("weaponHolder", fields).SetValue(equipment, hand);
        typeof(EquipmentSystem).GetField("weaponSheath", fields).SetValue(equipment, sheath);
        typeof(EquipmentSystem).GetField("weapon", fields).SetValue(equipment, swordPrefab);

        equipment.ResetToSheath();
        equipment.DrawWeapon();
        equipment.DrawWeapon(); // repeated animation event
        Assert.That(equipment.IsDrawn, Is.True);
        Assert.That(hand.transform.childCount, Is.EqualTo(1));
        equipment.SheathWeapon();
        equipment.SheathWeapon(); // repeated animation event
        Assert.That(equipment.IsDrawn, Is.False);
        Assert.That(sheath.transform.childCount, Is.EqualTo(1));
        equipment.DrawWeapon();
        equipment.ResetToSheath(); // death and respawn both call this
        equipment.ResetToSheath();
        Assert.That(hand.transform.childCount, Is.Zero);
        Assert.That(sheath.transform.childCount, Is.EqualTo(1));
    }
}
#endif
