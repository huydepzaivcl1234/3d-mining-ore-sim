#if UNITY_EDITOR
using System;
using System.Reflection;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class ChestDefenseProgressionTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test] public void DefenseDefinitionsAreIdempotentAndKeepExistingQuest()
    {
        var data = ScriptableObject.CreateInstance<MiningQuestData>();
        try
        {
            data.AddDefenseQuests(); data.AddDefenseQuests();
            Assert.That(data.Quests.Count, Is.EqualTo(8));
            Assert.That(data.GetDefinition("weekly_rebirth_1"), Is.Not.Null);
            foreach (var quest in data.Quests) Assert.That(quest.IsValid, Is.True);
            Assert.That(data.GetDefinition("daily_skullclaw").Difficulty, Is.EqualTo(QuestDifficulty.Hard));
        }
        finally { Object.DestroyImmediate(data); }
    }

    [TestCase(typeof(MushroomData), "daily_mushroom")]
    [TestCase(typeof(GolemData), "daily_golem")]
    [TestCase(typeof(BatData), "daily_bat")]
    [TestCase(typeof(MushranonData), "daily_mushranon")]
    [TestCase(typeof(ForestGolemData), "daily_forest_golem")]
    [TestCase(typeof(SkullclawData), "daily_skullclaw")]
    public void SpeciesQuestsDoNotCountOtherSpecies(Type type, string id)
    {
        var data = ScriptableObject.CreateInstance<MiningQuestData>();
        var species = (MonsterRewardData)ScriptableObject.CreateInstance(type);
        try
        {
            data.AddDefenseQuests();
            foreach (var quest in data.Quests)
                if (quest.Objective == MiningQuestObjective.KillMonster)
                    Assert.That(quest.MatchesKill(species, false), Is.EqualTo(quest.QuestId == id));
            Assert.That(data.GetDefinition("weekly_boss").MatchesKill(species, false), Is.False);
            Assert.That(data.GetDefinition("weekly_boss").MatchesKill(species, true), Is.True);
        }
        finally { Object.DestroyImmediate(data); Object.DestroyImmediate(species); }
    }

    [Test] public void TowerKillAddsSpeciesAndBossProgressButUnknownSourceDoesNot()
    {
        var data = ScriptableObject.CreateInstance<MiningQuestData>();
        var species = ScriptableObject.CreateInstance<ForestGolemData>();
        var root = new GameObject("Isolated quest fixture"); root.SetActive(false);
        var towerRoot = new GameObject("Isolated cannon source"); towerRoot.SetActive(false);
        string key = "ChestDefenseTests." + Guid.NewGuid().ToString("N");
        MiningQuestSystem system = null;
        try
        {
            data.AddDefenseQuests();
            typeof(MiningQuestData).GetField("saveKey", Private).SetValue(data, key);
            system = root.AddComponent<MiningQuestSystem>();
            typeof(MiningQuestSystem).GetField("data", Private).SetValue(system, data);
            var wallet = root.AddComponent<PlayerWallet>();
            typeof(MiningQuestSystem).GetField("wallet", Private).SetValue(system, wallet);
            typeof(MiningQuestSystem).GetMethod("OnEnable", Private).Invoke(system, null);
            towerRoot.AddComponent<TowerRuntime>();
            Assert.That(MonsterKillCredit.IsPlayerOwned(towerRoot), Is.True);
            MonsterKillCredit.Notify(null, species, true, 0);
            Assert.That(system.GetProgress("weekly_boss"), Is.Zero);
            MonsterKillCredit.Notify(towerRoot, species, true, 0);
            Assert.That(system.GetProgress("weekly_boss"), Is.EqualTo(1));
            Assert.That(system.GetProgress("daily_forest_golem"), Is.EqualTo(1));
            Assert.That(system.GetProgress("daily_golem"), Is.Zero);
            typeof(MiningQuestSystem).GetMethod("OnDisable", Private).Invoke(system, null);
            typeof(MiningQuestSystem).GetMethod("OnEnable", Private).Invoke(system, null);
            MonsterKillCredit.Notify(towerRoot, species, false, 0);
            Assert.That(system.GetProgress("daily_forest_golem"), Is.EqualTo(2));
            Assert.That(system.GetProgress("weekly_boss"), Is.EqualTo(1));
            float before = wallet.CurrentMoney;
            Assert.That(system.TryClaimReward("daily_forest_golem", out var paid), Is.True);
            Assert.That(paid, Is.EqualTo(400));
            Assert.That(wallet.CurrentMoney, Is.EqualTo(before + 400));
            Assert.That(system.TryClaimReward("daily_forest_golem", out _), Is.False);
            MonsterKillCredit.Notify(towerRoot, species, false, 0);
            Assert.That(system.GetProgress("daily_forest_golem"), Is.EqualTo(2));
            // Discard the runtime cache and reload the persisted claim state.
            typeof(MiningQuestSystem).GetMethod("OnDisable", Private).Invoke(system, null);
            ((System.Collections.IDictionary)typeof(MiningQuestSystem).GetField("states", Private).GetValue(system)).Clear();
            typeof(MiningQuestSystem).GetField("initialized", Private).SetValue(system, false);
            typeof(MiningQuestSystem).GetMethod("OnEnable", Private).Invoke(system, null);
            Assert.That(system.IsClaimed("daily_forest_golem"), Is.True);
            Assert.That(system.GetProgress("weekly_boss"), Is.EqualTo(1));
        }
        finally
        {
            if (system != null) typeof(MiningQuestSystem).GetMethod("OnDisable", Private).Invoke(system, null);
            Object.DestroyImmediate(root); Object.DestroyImmediate(towerRoot);
            Object.DestroyImmediate(species); Object.DestroyImmediate(data);
            GameSave.DeleteKey(key);
        }
    }

    [Test] public void EmergencyRepairCostsNothingRestoresPartialHealthAndKeepsProgress()
    {
        var data = ScriptableObject.CreateInstance<TreasureChestData>();
        var root = new GameObject("Isolated emergency repair"); root.SetActive(false);
        try
        {
            var chest = root.AddComponent<TreasureChest>();
            typeof(TreasureChest).GetField("data", Private).SetValue(chest, data);
            typeof(TreasureChest).GetField("persistProgress", Private).SetValue(chest, false);
            typeof(TreasureChest).GetMethod("Awake", Private).Invoke(chest, null);
            root.SetActive(true);
            chest.Health.ConfigureSpawnHealth(data.MaxHealth(1));
            chest.GrantKillExperience(2);
            chest.Health.DealDamage(10000, CombatDamageType.True);
            Assert.That(chest.CanRepair, Is.False);
            Assert.That(chest.EmergencyRepair(), Is.True);
            Assert.That(chest.Health.Health, Is.EqualTo(data.MaxHealth(1) * .25f));
            Assert.That(chest.Experience, Is.EqualTo(2));
            Assert.That(chest.Level, Is.EqualTo(1));
            Assert.That(chest.EmergencyRepair(), Is.False);
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(data); }
    }

    [TestCase(1, 0)] [TestCase(-1, 0)] [TestCase(0, 1)] [TestCase(0, -1)]
    [TestCase(1, 1)] [TestCase(-1, -1)] [TestCase(0, 0)]
    public void ThreatPositionStaysWithinInsetSafeArea(float x, float y)
    {
        var safe = new Rect(20, 30, 1000, 600);
        var point = TreasureChestDefenseFeedback.EdgePosition(new Vector2(x, y), safe, new Vector2(115, 45));
        Assert.That(point.x, Is.InRange(135, 905));
        Assert.That(point.y, Is.InRange(75, 585));
    }

    [Test] public void BrokenChestStillEarnsXPWithoutAutomaticallyReviving()
    {
        var data = ScriptableObject.CreateInstance<TreasureChestData>();
        var root = new GameObject("Isolated broken chest XP"); root.SetActive(false);
        try
        {
            var chest = root.AddComponent<TreasureChest>();
            typeof(TreasureChest).GetField("data", Private).SetValue(chest, data);
            typeof(TreasureChest).GetField("persistProgress", Private).SetValue(chest, false);
            typeof(TreasureChest).GetMethod("Awake", Private).Invoke(chest, null);
            root.SetActive(true);
            chest.Health.ConfigureSpawnHealth(data.MaxHealth(1));
            chest.Health.DealDamage(10000, CombatDamageType.True);
            chest.GrantKillExperience(data.RequiredExperience(1));
            Assert.That(chest.Level, Is.EqualTo(2));
            Assert.That(chest.IsBroken, Is.True);
            Assert.That(chest.Health.MaxHealth, Is.EqualTo(data.MaxHealth(2)));
        }
        finally { Object.DestroyImmediate(root); Object.DestroyImmediate(data); }
    }

    [Test] public void ProjectileKeepsOwnershipAfterCannonIsDestroyed()
    {
        var root = new GameObject("Cannon ownership snapshot"); root.SetActive(false);
        var shot = new GameObject("In flight cannon shot"); shot.SetActive(false);
        var data = ScriptableObject.CreateInstance<CannonTowerData>();
        try
        {
            var owner = root.AddComponent<TowerRuntime>();
            var projectile = shot.AddComponent<TowerProjectile>();
            Assert.That(MonsterKillCredit.IsPlayerOwned(shot), Is.False);
            projectile.Initialize(owner, Vector3.forward, data);
            Object.DestroyImmediate(root);
            Assert.That(MonsterKillCredit.IsPlayerOwned(shot), Is.True);
            Assert.That(MonsterKillCredit.IsTowerOwned(shot), Is.True);
        }
        finally { if (root != null) Object.DestroyImmediate(root); Object.DestroyImmediate(shot); Object.DestroyImmediate(data); }
    }
}
#endif
