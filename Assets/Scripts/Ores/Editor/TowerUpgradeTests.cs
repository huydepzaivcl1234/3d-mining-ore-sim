#if UNITY_EDITOR
using System;
using System.Reflection;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class TowerUpgradeTests
{
    static void Fixture(Action<TowerRuntime,TowerRuntime,PlayerWallet,Transform,CannonTowerData> run)
    {
        var root=new GameObject("Tower upgrade test");root.SetActive(false);
        var data=ScriptableObject.CreateInstance<CannonTowerData>();
        data.level=1;data.maximumLevel=3;data.upgradePrice=75;data.upgradeCostMultiplier=2;
        data.damage=5;data.health=100;data.damageGrowth=.2f;data.healthGrowth=.25f;
        try
        {
            TowerRuntime Make(string name) { var go=new GameObject(name);go.transform.SetParent(root.transform);var t=go.AddComponent<TowerRuntime>();t.Initialize(data,100);t.enabled=true;return t; }
            var first=Make("First");var second=Make("Second");
            var wallet=root.AddComponent<PlayerWallet>();
            typeof(PlayerWallet).GetField("currentMoney",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(wallet,1000f);
            var player=new GameObject("Player");player.transform.SetParent(root.transform);player.AddComponent<MiningCharacterHealth>().ConfigureSpawnHealth(100);
            // IsAlive intentionally requires an active tower; lifecycle must run before exercising upgrade.
            root.SetActive(true);
            wallet.enabled=false;
            run(first,second,wallet,player.transform,data);
        }
        finally { UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(data); }
    }
    [Test] public void UpgradeIsPerInstanceAndPreservesMissingHealthAndReceipt()
    { Fixture((a,b,w,p,d)=> { a.Health.RestoreSavedHealth(70);Assert.IsTrue(a.TryUpgrade(w,p));Assert.AreEqual(2,a.Level);Assert.AreEqual(6,a.Damage,.001f);Assert.AreEqual(125,a.Health.MaxHealth);Assert.AreEqual(95,a.Health.Health);Assert.AreEqual(925,w.CurrentMoney);Assert.AreEqual(150,a.UpgradeCost);Assert.AreEqual(1,b.Level);Assert.AreEqual(5,b.Damage);Assert.AreEqual(5,d.damage);Assert.AreEqual(100,a.PaidPrice); }); }
    [Test] public void UpgradeRejectsDistanceInsufficientGoldAndMaximumLevel()
    { Fixture((a,b,w,p,d)=> { p.position=Vector3.right*10;Assert.IsFalse(a.TryUpgrade(w,p));p.position=Vector3.zero;d.upgradePrice=2000;Assert.IsFalse(a.TryUpgrade(w,p));d.upgradePrice=75;Assert.IsTrue(a.TryUpgrade(w,p));Assert.IsTrue(a.TryUpgrade(w,p));Assert.IsFalse(a.TryUpgrade(w,p));Assert.AreEqual(3,a.Level);Assert.AreEqual(775,w.CurrentMoney); }); }
    [Test] public void RestoredLevelUsesUpgradedMaximumBeforeRestoringHealth()
    { Fixture((a,b,w,p,d)=> { a.Initialize(d,123,3);a.Health.RestoreSavedHealth(135);Assert.AreEqual(3,a.Level);Assert.AreEqual(150,a.Health.MaxHealth);Assert.AreEqual(135,a.Health.Health);a.Initialize(d,100,0);Assert.AreEqual(1,a.Level); }); }
    [Test] public void OldTowerSaveMigratesWithoutDiscardingAndNewSaveKeepsLevel()
    {
        string old="{\"version\":1,\"towers\":[{\"id\":\"abc\",\"towerId\":\"cannon\",\"scene\":\"SampleScene\",\"health\":70,\"paidPrice\":100}]}";
        Assert.IsTrue(TowerSaveDocument.TryRead(old,out var save));Assert.AreEqual(2,save.version);Assert.AreEqual(0,save.towers[0].level);
        save.towers[0].level=3;Assert.IsTrue(TowerSaveDocument.TryRead(JsonUtility.ToJson(save),out var restored));Assert.AreEqual(3,restored.towers[0].level);
        Assert.IsFalse(TowerSaveDocument.TryRead("{\"version\":3,\"towers\":[]}",out _));
    }
    [Test] public void HoverRevealsWithoutSelectingAndDoesNotResizeDrawer()
    {
        var go=new GameObject("Tower hover test",typeof(RectTransform));var data=ScriptableObject.CreateInstance<CannonTowerData>();
        var catalog=ScriptableObject.CreateInstance<TowerCatalog>();catalog.towers=new TowerData[]{data,data,data,data,data};
        try { var panel=go.AddComponent<TowerShopPanel>();
            typeof(TowerShopPanel).GetMethod("Build",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(panel,new object[]{catalog});
            panel.ShowCatalog();var size=((RectTransform)go.transform).sizeDelta;
            var scroll=panel.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            Assert.AreEqual(5,scroll.content.childCount);Assert.Greater(scroll.content.rect.height,scroll.viewport.rect.height);
            Assert.IsFalse(scroll.horizontal);Assert.AreSame(scroll.content.parent,scroll.viewport);
            var card=new GameObject("Card").AddComponent<TowerCardHover>();card.transform.SetParent(go.transform);card.Initialize(panel,data);
            card.OnPointerEnter(null);Assert.AreSame(data,panel.Previewed);Assert.IsNull(panel.Selected);Assert.AreEqual(size,((RectTransform)go.transform).sizeDelta);
            card.OnPointerExit(null);Assert.IsNull(panel.Previewed);Assert.AreEqual(size,((RectTransform)go.transform).sizeDelta);
        } finally {UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(data);UnityEngine.Object.DestroyImmediate(catalog);}
    }
}
#endif
