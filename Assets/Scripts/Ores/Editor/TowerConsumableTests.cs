#if UNITY_EDITOR
using System.Reflection;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class TowerConsumableTests
{
    private const string Folder = "Assets/GameData/Tower/Cannon/";
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Flags).SetValue(obj, value);
    [TestCase("Apple", MiningItemEffectType.PlayerHealing, 10f, 5f)]
    [TestCase("Banana", MiningItemEffectType.PlayerMoveSpeed, 5f, 180f)]
    [TestCase("Carrot", MiningItemEffectType.PlayerAttackSpeed, 1f, 180f)]
    [TestCase("Grape", MiningItemEffectType.PlayerMaxHealth, 5f, 180f)]
    [TestCase("Ice cream", MiningItemEffectType.PlayerDamageReduction, 1f, 180f)]
    [TestCase("Pea", MiningItemEffectType.MonsterMaxHealthTrueDamage, .5f, 150f)]
    public void ConsumableValues(string name, MiningItemEffectType effect, float amount, float seconds)
    {
        var item = AssetDatabase.LoadAssetAtPath<MiningItemData>("Assets/GameData/Items/" + name + ".asset");
        Assert.That(item.EffectType, Is.EqualTo(effect));
        Assert.That(item.EffectPercent, Is.EqualTo(amount));
        Assert.That(item.EffectDurationSeconds, Is.EqualTo(seconds));
    }
    [Test] public void CannonAssetsAreComplete()
    {
        var data = AssetDatabase.LoadAssetAtPath<CannonTowerData>(Folder + "CannonData.asset");
        Assert.That(data.prefab, Is.Not.Null); Assert.That(data.projectile, Is.Not.Null);
        Assert.That(data.icon, Is.Not.Null); Assert.That(data.inventoryItem.Tower, Is.SameAs(data));
        Assert.That(data.inventoryItem.MaximumStack, Is.EqualTo(999));
        Assert.That(data.prefab.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips.Length, Is.EqualTo(1));
        var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "CannonIcon.png");
        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        var png = new Texture2D(2, 2);
        try
        {
            png.LoadImage(System.IO.File.ReadAllBytes(Folder + "CannonIcon.png"));
            int transparent = 0, opaque = 0;
            foreach (var pixel in png.GetPixels32()) { if (pixel.a == 0) transparent++; if (pixel.a > 200) opaque++; }
            Assert.That(transparent, Is.GreaterThan(10000)); Assert.That(opaque, Is.GreaterThan(10000));
        }
        finally { Object.DestroyImmediate(png); }
    }
    [Test] public void RefundUsesOriginalReceiptAndCannotSellTwice()
    {
        WithInventory((inv, wallet, data) => {
            data.price = 100; Assert.That(inv.TryBuyTower(data, wallet), Is.True);
            data.price = 500; Assert.That(inv.TowerRefundAt(0), Is.EqualTo(35));
            Assert.That(inv.TrySellTowerSlot(0, wallet), Is.True);
            Assert.That(wallet.CurrentMoney, Is.EqualTo(935));
            Assert.That(inv.TrySellTowerSlot(0, wallet), Is.False);
            Assert.That(wallet.CurrentMoney, Is.EqualTo(935));
        });
    }
    [Test] public void SameTowerStacksAndConsumesEachOriginalReceipt()
    {
        WithInventory((inv, wallet, data) => {
            data.price = 100; inv.TryBuyTower(data, wallet);
            data.price = 200; inv.TryBuyTower(data, wallet);
            Assert.That(inv.OccupiedSlotCount, Is.EqualTo(1));
            Assert.That(inv.GetSlot(0).Count, Is.EqualTo(2));
            Assert.That(inv.TryTakeTowerAt(0, data.inventoryItem, out float paid), Is.True);
            Assert.That(paid, Is.EqualTo(100)); Assert.That(inv.TowerRefundAt(0), Is.EqualTo(70));
            Assert.That(inv.TryTakeTowerAt(0, data.inventoryItem, out paid), Is.True);
            Assert.That(paid, Is.EqualTo(200));
            Assert.That(inv.TryTakeTowerAt(0, data.inventoryItem, out _), Is.False);
        });
    }
    [Test] public void InvalidPriceOrFullInventoryDoesNotChargeWallet()
    {
        WithInventory((inv, wallet, data) => {
            foreach (float price in new[] { float.NaN, float.PositiveInfinity, -1f, 1001f }) {
                data.price = price; Assert.That(inv.TryBuyTower(data, wallet), Is.False);
            }
            data.price = 0;
            Assert.That(inv.TryAddItem(data.inventoryItem,inv.Capacity*data.inventoryItem.MaximumStack), Is.True);
            data.price = 100; Assert.That(inv.TryBuyTower(data, wallet), Is.False);
            Assert.That(wallet.CurrentMoney, Is.EqualTo(1000));
        });
    }
    [Test] public void GiftAndPurchasedReceiptsStayDistinctAfterMoveAndSale()
    {
        WithInventory((inv,wallet,data)=>{
            inv.TryAddItem(data.inventoryItem,2);data.price=100;inv.TryBuyTower(data,wallet);
            Assert.That(inv.TryMoveSlot(0,4,data.inventoryItem),Is.True);
            Assert.That(inv.TryTakeTowerAt(4,data.inventoryItem,out float paid),Is.True);Assert.That(paid,Is.Zero);
            Assert.That(inv.TrySellTowerSlot(4,wallet),Is.True);Assert.That(wallet.CurrentMoney,Is.EqualTo(900));
            Assert.That(inv.TowerRefundAt(4),Is.EqualTo(35));
            Assert.That(inv.TrySellTowerSlot(4,wallet),Is.True);Assert.That(wallet.CurrentMoney,Is.EqualTo(935));
        });
    }
    [TestCase(3)] [TestCase(4)] public void SavedTowerSlotsMergeWithReceipts(int version)
    {
        WithInventory((inv,wallet,data)=>{
            var db=ScriptableObject.CreateInstance<MiningItemDatabase>();string key="TowerStackTest."+System.Guid.NewGuid().ToString("N");
            try {
                Set(db,"items",new System.Collections.Generic.List<MiningItemData>{data.inventoryItem});Set(db,"inventorySaveKey",key);Set(inv,"database",db);
                string id=data.inventoryItem.ItemId;
                string first=version==3?"\"count\":1,\"paidPrice\":100":"\"count\":2,\"paidPrice\":100,\"towerReceipts\":[100,150]";
                PlayerPrefs.SetString(key,"{\"version\":"+version+",\"slots\":[{\"itemId\":\""+id+"\","+first+"},{\"itemId\":\""+id+"\",\"count\":1,\"paidPrice\":200}]}");
                typeof(MiningItemSystem).GetMethod("LoadInventory",Flags).Invoke(inv,null);
                Assert.That(inv.OccupiedSlotCount,Is.EqualTo(1));Assert.That(inv.GetSlot(0).Count,Is.EqualTo(version==3?2:3));
                Assert.That(inv.TryTakeTower(data.inventoryItem,out float paid),Is.True);Assert.That(paid,Is.EqualTo(100));
                if(version==4){inv.TryTakeTower(data.inventoryItem,out paid);Assert.That(paid,Is.EqualTo(150));}
                inv.TryTakeTower(data.inventoryItem,out paid);Assert.That(paid,Is.EqualTo(200));
            } finally {PlayerPrefs.DeleteKey(key);Object.DestroyImmediate(db);}
        });
    }
    private static void WithInventory(System.Action<MiningItemSystem, PlayerWallet, CannonTowerData> run)
    {
        var go = new GameObject("Tower transaction test"); go.SetActive(false);
        var data = Object.Instantiate(AssetDatabase.LoadAssetAtPath<CannonTowerData>(Folder + "CannonData.asset"));
        var item = Object.Instantiate(data.inventoryItem); data.inventoryItem = item; Set(item, "tower", data);
        try {
            var inv = go.AddComponent<MiningItemSystem>(); var wallet = go.AddComponent<PlayerWallet>();
            Set(wallet, "currentMoney", 1000f); run(inv, wallet, data);
        }
        finally { Object.DestroyImmediate(go); Object.DestroyImmediate(item); Object.DestroyImmediate(data); }
    }
    [Test] public void BuffFractionsAndFlatSpeedAreNotConfused()
    {
        WithInventory((inv, wallet, data) => {
            foreach (string name in new[] { "Banana", "Carrot", "Grape", "Ice cream", "Pea" }) {
                var item=AssetDatabase.LoadAssetAtPath<MiningItemData>("Assets/GameData/Items/"+name+".asset");
                Assert.That(inv.TryAddItem(item),Is.True); Assert.That(inv.TryUseSlot(0),Is.True);
            }
            Assert.That(inv.PlayerMoveSpeedBonus,Is.EqualTo(5));
            Assert.That(inv.PlayerAttackSpeedMultiplier,Is.EqualTo(1.01f).Within(.00001f));
            Assert.That(inv.PlayerHealthMultiplier,Is.EqualTo(1.05f).Within(.00001f));
            Assert.That(inv.PlayerDamageReduction,Is.EqualTo(.01f).Within(.00001f));
            Assert.That(inv.MonsterTrueDamageFraction,Is.EqualTo(.005f).Within(.00001f));
        });
    }
    [Test] public void TrueBonusBypassesArmorAndEmitsOnlyOneDamageEvent()
    {
        var go=new GameObject("True damage test");go.SetActive(false);
        try {
            var hp=go.AddComponent<MiningCharacterHealth>();hp.ConfigureSpawnHealth(1000);hp.ConfigureDefenses(100,100);
            int events=0;hp.Damaged+=()=>events++;
            float dealt=hp.DealDamageWithTrueBonus(100,CombatDamageType.Physical,null,5);
            Assert.That(dealt,Is.EqualTo(CombatDamage.Resolve(100,CombatDamageType.Physical,hp.Armor,hp.MagicResistance,hp.ResistanceScale)+5).Within(.001f));
            Assert.That(events,Is.EqualTo(1));
        } finally { Object.DestroyImmediate(go); }
    }
    [Test] public void MonsterChoosesNearestDefenseOnEitherSideAndIgnoresDestroyedTower()
    {
        var enemy=new GameObject("Defense target test monster");enemy.SetActive(false);
        var chestObject=new GameObject("Defense target test chest");
        var towerObject=new GameObject("Defense target test tower");
        var definition=ScriptableObject.CreateInstance<TreasureChestData>();
        var active=typeof(TreasureChest).GetField("<Active>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);
        var previous=active.GetValue(null); TowerRuntime tower=null;
        try {
            var hp=enemy.AddComponent<MiningCharacterHealth>();hp.ConfigureSpawnHealth(100);
            var monster=enemy.AddComponent<MushroomMonster>();Set(monster,"health",hp);
            chestObject.transform.position=Vector3.forward*10;
            var chestShape=chestObject.AddComponent<BoxCollider>();
            var chestHp=chestObject.AddComponent<MiningCharacterHealth>();chestHp.ConfigureSpawnHealth(100);
            var chest=chestObject.AddComponent<TreasureChest>();Set(chest,"health",chestHp);Set(chest,"data",definition);Set(chest,"body",chestShape);Set(chest,"persistProgress",false);active.SetValue(null,chest);
            tower=towerObject.AddComponent<TowerRuntime>();var towerHp=towerObject.GetComponent<MiningCharacterHealth>();
            towerHp.ConfigureSpawnHealth(100);Set(tower,"health",towerHp);TowerRuntime.Active.Add(tower);
            var update=typeof(MushroomMonster).GetMethod("UpdatePlayerTarget",Flags);
            var target=typeof(MushroomMonster).GetField("target",Flags);
            foreach(float side in new[]{-1f,1f}) {
                towerObject.transform.position=new Vector3(side*2,0,3);Physics.SyncTransforms();
                update.Invoke(monster,new object[]{.1f});Assert.That(target.GetValue(monster),Is.SameAs(towerHp));
            }
            towerObject.transform.position=new Vector3(-20,0,3);Physics.SyncTransforms();
            update.Invoke(monster,new object[]{.1f});Assert.That(target.GetValue(monster),Is.SameAs(chestHp));
            towerObject.transform.position=Vector3.forward*2;towerHp.DealDamage(1000);Physics.SyncTransforms();
            update.Invoke(monster,new object[]{.1f});Assert.That(target.GetValue(monster),Is.SameAs(chestHp));
        } finally {
            if(tower!=null)TowerRuntime.Active.Remove(tower);
            Object.DestroyImmediate(enemy);Object.DestroyImmediate(towerObject);Object.DestroyImmediate(chestObject);Object.DestroyImmediate(definition);active.SetValue(null,previous);
        }
    }
}
#endif
