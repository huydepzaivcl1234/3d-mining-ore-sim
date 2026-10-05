#if UNITY_INCLUDE_TESTS
using System.Reflection;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEngine;

public sealed class AutoUpgradeTraversalTests
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject root;
    private MiningUpgradeData data;
    private MiningGameData gameData;
    private MiningUpgradeSystem upgrades;
    private PlayerWallet wallet;
    private string key;

    [SetUp]
    public void SetUp()
    {
        key = "AutoUpgrade.Tests." + System.Guid.NewGuid();
        root = new GameObject("Auto upgrade isolated tests"); root.SetActive(false);
        data = ScriptableObject.CreateInstance<MiningUpgradeData>();
        gameData = ScriptableObject.CreateInstance<MiningGameData>();
        typeof(MiningUpgradeData).GetField("autoUpgradeSaveKey", Fields).SetValue(data, key);
        typeof(MiningUpgradeData).GetField("autoUpgradeGemCost", Fields).SetValue(data, 10f);
        typeof(MiningGameData).GetField("gemSaveKey", Fields).SetValue(gameData, key + ".Gems");
        wallet = root.AddComponent<PlayerWallet>();
        typeof(PlayerWallet).GetField("gameData", Fields).SetValue(wallet, gameData);
        upgrades = root.AddComponent<MiningUpgradeSystem>();
        typeof(MiningUpgradeSystem).GetField("wallet", Fields).SetValue(upgrades, wallet);
        typeof(MiningUpgradeSystem).GetField("upgradeData", Fields).SetValue(upgrades, data);
    }
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root); Object.DestroyImmediate(data); Object.DestroyImmediate(gameData);
        PlayerPrefs.DeleteKey(key); PlayerPrefs.DeleteKey(key + ".Enabled"); PlayerPrefs.DeleteKey(key + ".Gems");
        PlayerPrefs.Save();
    }
    [Test]
    public void UnlockCostsGemsOnceAndPurchasesUseMoney()
    {
        wallet.SetGems(20); wallet.SetMoney(100);
        Assert.IsTrue(upgrades.TryUnlockAutoUpgrade());
        Assert.AreEqual(10, wallet.CurrentGems);
        Assert.IsTrue(upgrades.TryUnlockAutoUpgrade());
        Assert.AreEqual(10, wallet.CurrentGems);
        Assert.IsTrue(upgrades.TryAutoUpgradeOnce());
        Assert.AreEqual(1, upgrades.GetStacks(MiningUpgradeType.MoneyReward));
        Assert.AreEqual(10, wallet.CurrentGems); Assert.Less(wallet.CurrentMoney, 100);
        upgrades.SetAutoUpgradeEnabled(false);
        Assert.IsFalse(upgrades.TryAutoUpgradeOnce());
    }
    [Test]
    public void InsufficientGemsOrMoneyNeverGrantFreeUpgrades()
    {
        wallet.SetGems(9); Assert.IsFalse(upgrades.TryUnlockAutoUpgrade());
        Assert.AreEqual(9, wallet.CurrentGems);
        wallet.SetGems(10); upgrades.TryUnlockAutoUpgrade(); wallet.SetMoney(0);
        Assert.IsFalse(upgrades.TryAutoUpgradeOnce());
        Assert.AreEqual(0, upgrades.GetStacks(MiningUpgradeType.MoneyReward));
    }
    [Test]
    public void RebirthKeepsUnlockButFullResetClearsIt()
    {
        wallet.SetGems(10); upgrades.TryUnlockAutoUpgrade();
        upgrades.ResetAllUpgrades(); Assert.IsTrue(upgrades.AutoUpgradeUnlocked);
        Assert.AreEqual(1, PlayerPrefs.GetInt(key));
        typeof(MiningUpgradeSystem).GetMethod("LoadAutoUpgrade", Fields).Invoke(upgrades, null);
        Assert.IsTrue(upgrades.AutoUpgradeEnabled);
        upgrades.ResetAutoUpgrade(); Assert.IsFalse(upgrades.AutoUpgradeUnlocked);
        Assert.IsFalse(PlayerPrefs.HasKey(key));
    }
    [Test]
    public void CardOnlyUpgradesAreExcludedFromAutomation()
    {
        typeof(MiningUpgradeData).GetField("autoUpgradeOrder", Fields).SetValue(data,
            new[] { MiningUpgradeType.RegenIntervalReduction, MiningUpgradeType.HealingEffectiveness });
        wallet.SetGems(10); wallet.SetMoney(1000); upgrades.TryUnlockAutoUpgrade();
        Assert.IsFalse(upgrades.TryAutoUpgradeOnce()); Assert.AreEqual(1000, wallet.CurrentMoney);
    }
    [Test]
    public void ActorsPassThroughNewOreWhilePlayerStillCollides()
    {
        var oreObject = new GameObject("Ore collision test", typeof(BoxCollider));
        var actor = new GameObject("AI collision test", typeof(CapsuleCollider));
        var player = new GameObject("Player collision test", typeof(CapsuleCollider));
        var ore = oreObject.AddComponent<Ore>();
        var traversal = typeof(Ore).Assembly.GetType("MiningSimulator.Ores.OreActorTraversal");
        var flags = BindingFlags.Static | BindingFlags.Public;
        try
        {
            traversal.GetMethod("RegisterActor", flags).Invoke(null, new object[] { actor });
            traversal.GetMethod("RegisterOre", flags).Invoke(null, new object[] { ore });
            Assert.IsTrue(Physics.GetIgnoreCollision(actor.GetComponent<Collider>(), oreObject.GetComponent<Collider>()));
            Assert.IsFalse(Physics.GetIgnoreCollision(player.GetComponent<Collider>(), oreObject.GetComponent<Collider>()));
            Assert.IsTrue(MiningNavigation.IsMineableSegmentClear(Vector3.left * 3, Vector3.right * 3, .25f));
            oreObject.SetActive(false); oreObject.SetActive(true);
            Assert.IsTrue(Physics.GetIgnoreCollision(actor.GetComponent<Collider>(), oreObject.GetComponent<Collider>()));
        }
        finally
        {
            traversal.GetMethod("UnregisterActor", flags).Invoke(null, new object[] { actor });
            Object.DestroyImmediate(oreObject); Object.DestroyImmediate(actor); Object.DestroyImmediate(player);
        }
    }
}
#endif
