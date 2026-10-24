#if UNITY_INCLUDE_TESTS
using System.Reflection;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEngine;

public sealed class NecklaceEquipmentTests
{
    private GameObject root;
    private MiningItemSystem items;
    private MiningItemData red, green, potion;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Isolated necklace test");
        items = root.AddComponent<MiningItemSystem>();
        red = CreateItem("necklace_red", MiningItemUseType.Equipment);
        green = CreateItem("necklace_green", MiningItemUseType.Equipment);
        potion = CreateItem("test_potion", MiningItemUseType.TimedEffect);
        red.EquipmentBonuses.lifeStealPercent = 10f;
        green.EquipmentBonuses.healingBonusPercent = 20f;
        green.EquipmentBonuses.regenIntervalReductionPercent = 15f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(red);
        Object.DestroyImmediate(green);
        Object.DestroyImmediate(potion);
    }

    [Test]
    public void EquipSwapAndRemoveNeverDuplicateItems()
    {
        Assert.IsTrue(items.TryAddItem(red));
        Assert.IsTrue(items.TryAddItem(green));
        Assert.IsTrue(items.TryUseSlot(0));
        Assert.AreSame(red, items.EquippedNecklace);
        Assert.AreEqual(0, items.GetItemCount(red));
        Assert.AreEqual(.1f, items.EquipmentBonuses.LifeStealFraction, .0001f);
        Assert.IsTrue(items.TryUseSlot(1));
        Assert.AreSame(red, items.EquippedNecklace);
        Assert.AreSame(green, items.GetSlot(MiningItemSystem.NecklaceSlotIndex + 1).Item);
        Assert.AreEqual(0, items.GetItemCount(red));
        Assert.AreEqual(.1f, items.EquipmentBonuses.LifeStealFraction, .0001f);
        Assert.AreEqual(.85f, items.EquipmentBonuses.RegenIntervalMultiplier, .0001f);
        Assert.IsTrue(items.TryUnequipNecklace());
        Assert.IsTrue(items.TryUnequipNecklace(MiningItemSystem.NecklaceSlotIndex + 1));
        Assert.IsNull(items.EquipmentBonuses);
        Assert.AreEqual(1, items.GetItemCount(green));
    }

    [Test]
    public void ConsumablesCannotEnterEquipmentSocket()
    {
        items.TryAddItem(potion);
        Assert.IsFalse(items.TryMoveSlot(0, MiningItemSystem.NecklaceSlotIndex, potion));
        Assert.AreEqual(1, items.GetItemCount(potion));
        Assert.IsNull(items.EquippedNecklace);
    }

    [Test]
    public void FullInventoryKeepsEquippedItemOnFailedRemoval()
    {
        items.TryAddItem(red);
        items.TryUseSlot(0);
        Assert.IsTrue(items.TryAddItem(potion, items.Capacity * potion.MaximumStack));
        Assert.IsFalse(items.TryUnequipNecklace(MiningItemSystem.NecklaceSlotIndex));
        Assert.AreSame(red, items.EquippedNecklace);
        Assert.IsTrue(items.OwnsEquipment(red));
    }

    [Test]
    public void EquipmentSaveFieldIsAdditiveToLegacyInventory()
    {
        var savedType = typeof(MiningItemSystem).GetNestedType("SavedInventory", BindingFlags.NonPublic);
        var legacy = JsonUtility.FromJson("{\"version\":1,\"slots\":[]}", savedType);
        Assert.IsNull(savedType.GetField("necklaceId").GetValue(legacy));
        savedType.GetField("necklaceId").SetValue(legacy, red.ItemId);
        var restored = JsonUtility.FromJson(JsonUtility.ToJson(legacy), savedType);
        Assert.AreEqual(red.ItemId, savedType.GetField("necklaceId").GetValue(restored));
    }

    [Test]
    public void PurchaseChargesOnlyGemsAndRejectsDuplicates()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        string key = "NecklaceTests.Gems." + System.Guid.NewGuid();
        var data = ScriptableObject.CreateInstance<MiningGameData>();
        var wallet = root.AddComponent<PlayerWallet>();
        typeof(MiningGameData).GetField("gemSaveKey", flags).SetValue(data, key);
        typeof(PlayerWallet).GetField("gameData", flags).SetValue(wallet, data);
        try
        {
            wallet.SetGems(5f);
            Assert.IsFalse(items.TryBuyEquipment(red, wallet));
            Assert.AreEqual(5f, wallet.CurrentGems);
            wallet.SetGems(30f);
            float money = wallet.CurrentMoney;
            Assert.IsTrue(items.TryBuyEquipment(red, wallet));
            Assert.AreEqual(30f - red.EquipmentGemPrice, wallet.CurrentGems);
            Assert.AreEqual(money, wallet.CurrentMoney);
            Assert.IsFalse(items.TryBuyEquipment(red, wallet));
            Assert.IsTrue(items.TryUseSlot(0));
            Assert.IsFalse(items.TryBuyEquipment(red, wallet));
            Assert.AreEqual(30f - red.EquipmentGemPrice, wallet.CurrentGems);
        }
        finally
        {
            PlayerPrefs.DeleteKey(key);
            Object.DestroyImmediate(data);
        }
    }

    [Test]
    public void LoadOldBagAndNewEquippedNecklaceWithoutResettingItems()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        string key = "NecklaceTests.Inventory." + System.Guid.NewGuid();
        var database = ScriptableObject.CreateInstance<MiningItemDatabase>();
        typeof(MiningItemDatabase).GetField("inventorySaveKey", flags).SetValue(database, key);
        typeof(MiningItemDatabase).GetField("items", flags).SetValue(database,
            new System.Collections.Generic.List<MiningItemData> { red, green, potion });
        typeof(MiningItemSystem).GetField("database", flags).SetValue(items, database);
        try
        {
            PlayerPrefs.SetString(key, "{\"version\":1,\"slots\":[{\"itemId\":\"test_potion\",\"count\":3}]}");
            typeof(MiningItemSystem).GetMethod("LoadInventory", flags).Invoke(items, null);
            Assert.AreEqual(3, items.GetItemCount(potion));
            Assert.IsNull(items.EquippedNecklace);
            PlayerPrefs.SetString(key, "{\"version\":2,\"necklaceId\":\"necklace_red\",\"slots\":[{\"itemId\":\"test_potion\",\"count\":3}]}");
            typeof(MiningItemSystem).GetMethod("LoadInventory", flags).Invoke(items, null);
            Assert.AreSame(red, items.EquippedNecklace);
            Assert.AreEqual(3, items.GetItemCount(potion));
            PlayerPrefs.SetString(key, "{\"version\":3,\"necklaceIds\":[\"necklace_red\",\"necklace_green\",\"\"],\"slots\":[{\"itemId\":\"test_potion\",\"count\":3}]}");
            typeof(MiningItemSystem).GetMethod("LoadInventory", flags).Invoke(items, null);
            Assert.AreSame(green, items.GetSlot(MiningItemSystem.NecklaceSlotIndex + 1).Item);
            Assert.AreEqual(.1f, items.EquipmentBonuses.LifeStealFraction, .0001f);
            Assert.AreEqual(20f, items.EquipmentBonuses.healingBonusPercent);
        }
        finally
        {
            PlayerPrefs.DeleteKey(key);
            Object.DestroyImmediate(database);
        }
    }

    private static MiningItemData CreateItem(string id, MiningItemUseType useType)
    {
        var item = ScriptableObject.CreateInstance<MiningItemData>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(MiningItemData).GetField("itemId", flags).SetValue(item, id);
        typeof(MiningItemData).GetField("useType", flags).SetValue(item, useType);
        return item;
    }

    [Test]
    public void ThreeNecklacesApplyTogetherAndFourthStaysInBag()
    {
        var orange = CreateItem("orange", MiningItemUseType.Equipment);
        var fourth = CreateItem("fourth", MiningItemUseType.Equipment);
        try
        {
            orange.EquipmentBonuses.burnDamagePerTick = 4f;
            orange.EquipmentBonuses.burnDurationSeconds = 5f;
            items.TryAddItem(red); items.TryAddItem(green); items.TryAddItem(orange); items.TryAddItem(fourth);
            Assert.IsTrue(items.TryUseSlot(0)); Assert.IsTrue(items.TryUseSlot(1)); Assert.IsTrue(items.TryUseSlot(2));
            Assert.IsFalse(items.TryUseSlot(3));
            Assert.AreEqual(.1f, items.EquipmentBonuses.LifeStealFraction, .0001f);
            Assert.AreEqual(20f, items.EquipmentBonuses.healingBonusPercent);
            Assert.AreEqual(4f, items.EquipmentBonuses.burnDamagePerTick);
            Assert.AreEqual(1, items.GetItemCount(fourth));
            Assert.IsTrue(items.TryMoveSlot(MiningItemSystem.NecklaceSlotIndex, MiningItemSystem.NecklaceSlotIndex + 2, red));
            Assert.AreSame(red, items.GetSlot(MiningItemSystem.NecklaceSlotIndex + 2).Item);
            Assert.IsTrue(items.TryUnequipNecklace(MiningItemSystem.NecklaceSlotIndex));
            Assert.AreEqual(0f, items.EquipmentBonuses.burnDamagePerTick);
        }
        finally { Object.DestroyImmediate(orange); Object.DestroyImmediate(fourth); }
    }

    [Test]
    public void GreenNecklaceChangesHealingAndIntervalOnlyWhileEquipped()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var data = ScriptableObject.CreateInstance<MiningPlayerStatsData>();
        var stats = root.AddComponent<MiningPlayerStats>();
        typeof(MiningPlayerStats).GetField("data", flags).SetValue(stats, data);
        var health = root.AddComponent<MiningCharacterHealth>();
        typeof(MiningCharacterHealth).GetField("equipmentItems", flags).SetValue(health, items);
        float initialInterval = health.RegenInterval;
        float initialHealing = health.HealingMultiplier;
        try
        {
            items.TryAddItem(green);
            items.TryUseSlot(0);
            Assert.AreEqual(initialInterval * .85f, health.RegenInterval, .0001f);
            Assert.AreEqual(initialHealing + .2f, health.HealingMultiplier, .0001f);
            items.TryUnequipNecklace();
            Assert.AreEqual(initialInterval, health.RegenInterval, .0001f);
            Assert.AreEqual(initialHealing, health.HealingMultiplier, .0001f);
            Assert.AreEqual(data.regenAmount, health.RegenAmount);
        }
        finally { Object.DestroyImmediate(data); }
    }
}
#endif
