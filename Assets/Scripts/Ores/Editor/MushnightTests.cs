#if UNITY_EDITOR
using System.Reflection;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MushnightTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test] public void SpeciesDataDoesNotExposeOtherSpeciesSkills()
    {
        Assert.That(typeof(MonsterRewardData).GetField("mushnight"), Is.Null);
        Assert.That(typeof(MonsterCombatSettings).GetField("forestGolem"), Is.Null);
        foreach (string species in new[] { "Bat", "Golem", "Mushranon", "Mushroom", "ForestGolem", "Mushnight" })
        {
            var data = AssetDatabase.LoadAssetAtPath<MonsterRewardData>($"Assets/GameData/Monsters/{species}Rewards.asset");
            Assert.That(data.GetType().Name, Is.EqualTo(species + "Data"));
            var serialized = new SerializedObject(data);
            Assert.That(serialized.FindProperty("mushnight") != null, Is.EqualTo(species == "Mushnight"));
            Assert.That(serialized.FindProperty("forestGolem") != null, Is.EqualTo(species == "ForestGolem"));
        }
    }

    [Test] public void ExhaustionHoldsStillUntilRecoveryThreshold()
    {
        var settings = new MushnightSettings { maxStamina = 10, staminaPerMovingSecond = 10,
            staminaRecoveryDelay = 1, staminaRecoveryPerSecond = 5, resumeStaminaFraction = .5f };
        var stamina = new MushnightStamina(settings);
        stamina.Tick(1, true);
        Assert.That(stamina.Current, Is.Zero);
        Assert.That(stamina.IsResting, Is.True);
        stamina.Tick(.5f, false); stamina.Tick(.5f, false);
        Assert.That(stamina.Current, Is.Zero);
        stamina.Tick(.5f, false);
        Assert.That(stamina.Current, Is.EqualTo(2.5f));
        Assert.That(stamina.IsResting, Is.True);
        stamina.Tick(.5f, false);
        Assert.That(stamina.Current, Is.EqualTo(5));
        Assert.That(stamina.IsResting, Is.False);
        stamina.Tick(.1f, true);
        Assert.That(stamina.Current, Is.EqualTo(4).Within(.0001));
    }

    [Test] public void StaminaRecoveryDoesNotDependOnFrameRate()
    {
        var settings = new MushnightSettings { maxStamina = 10, staminaPerMovingSecond = 10,
            staminaRecoveryDelay = .7f, staminaRecoveryPerSecond = 4, resumeStaminaFraction = 1f };
        var large = new MushnightStamina(settings); var small = new MushnightStamina(settings);
        large.Tick(1, true); small.Tick(1, true);
        large.Tick(2, false);
        for (int i = 0; i < 20; i++) small.Tick(.1f, false);
        Assert.That(large.Current, Is.EqualTo(small.Current).Within(.0001));
        Assert.That(large.IsResting, Is.EqualTo(small.IsResting));
    }

    [Test] public void TheftScalesWithLevelAndRejectsInvalidAmounts()
    {
        var data = new MushnightSettings { goldAtLevelOne = 10, goldPerLevel = 5 };
        Assert.That(data.GoldAtLevel(1), Is.EqualTo(10));
        Assert.That(data.GoldAtLevel(4), Is.EqualTo(25));
        data.goldAtLevelOne = float.NaN; data.goldPerLevel = float.PositiveInfinity;
        Assert.That(data.GoldAtLevel(10), Is.Zero);
    }

    [Test] public void TheftIsClampedAndNeverFiresPurchaseEvents()
    {
        var root = new GameObject("Isolated thief wallet"); root.SetActive(false);
        try
        {
            var wallet = root.AddComponent<PlayerWallet>(); wallet.SetMoney(7);
            int purchases = 0, changes = 0;
            wallet.MoneySpent += _ => purchases++;
            wallet.MoneyChanged += _ => changes++;
            Assert.That(wallet.TakeStolenMoney(20), Is.EqualTo(7));
            Assert.That(wallet.CurrentMoney, Is.Zero);
            Assert.That(wallet.TakeStolenMoney(float.NaN), Is.Zero);
            Assert.That(wallet.TakeStolenMoney(-1), Is.Zero);
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(purchases, Is.Zero);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test] public void NightRosterUsesSeparateNonAttackingSpecies()
    {
        var roster = AssetDatabase.LoadAssetAtPath<MonsterSpawnRoster>("Assets/Resources/MonsterSpawnRoster.asset");
        Assert.That(roster.nightThief.prefab, Is.Not.Null);
        Assert.That(roster.nightThief.prefab.GetComponent<MushnightThief>(), Is.Not.Null);
        var data = roster.nightThief.prefab.RewardData as MushnightData;
        Assert.That(data, Is.Not.Null);
        Assert.That(data.mushnight.enabled, Is.True);
        Assert.That(data.boss.enabled, Is.False);
        Assert.That(data.combat.damage, Is.Zero);
        Assert.That(data.mushnight.stealSeconds, Is.EqualTo(3));
        foreach (var entry in roster.Entries) Assert.That(entry.prefab, Is.Not.SameAs(roster.nightThief.prefab));
    }

    [Test] public void CloakRevealsOnDamageAndDeathRefundsExactlyOnce()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monsters/MushnightMonster.prefab");
        var root = Object.Instantiate(prefab); root.SetActive(false);
        var walletRoot = new GameObject("Isolated refund wallet"); walletRoot.SetActive(false);
        try
        {
            var monster = root.GetComponent<MushroomMonster>();
            typeof(MushroomMonster).GetMethod("Awake", Flags).Invoke(monster, null);
            monster.Initialize(null, null);
            var thief = root.GetComponent<MushnightThief>();
            Assert.That(thief.IsInvisible, Is.True);
            Assert.That(monster.IsTargetVisible, Is.False);
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) Assert.That(renderer.enabled, Is.False);
            thief.OnDamaged();
            Assert.That(thief.IsInvisible, Is.False);
            Assert.That(thief.State, Is.EqualTo(MushnightThief.ThiefState.Evading));
            var wallet = walletRoot.AddComponent<PlayerWallet>(); wallet.SetMoney(50);
            float stolen = wallet.TakeStolenMoney(17);
            typeof(MushnightThief).GetField("robbedWallet", Flags).SetValue(thief, wallet);
            typeof(MushnightThief).GetField("<StolenGold>k__BackingField", Flags).SetValue(thief, stolen);
            thief.OnKilled(); thief.OnKilled();
            Assert.That(wallet.CurrentMoney, Is.EqualTo(50));
            Assert.That(thief.StolenGold, Is.Zero);
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(walletRoot); }
    }
}
#endif
