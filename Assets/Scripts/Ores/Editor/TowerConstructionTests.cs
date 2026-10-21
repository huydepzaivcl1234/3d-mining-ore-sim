#if UNITY_EDITOR
using System;
using System.Reflection;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class TowerConstructionTests
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
    static void WithInventory(Action<MiningItemSystem,PlayerWallet,CannonTowerData> test)
    {
        var root=new GameObject("Construction test");root.SetActive(false);
        var data=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<CannonTowerData>("Assets/GameData/Tower/Cannon/CannonData.asset"));
        var item=UnityEngine.Object.Instantiate(data.inventoryItem);data.inventoryItem=item;Set(item,"tower",data);
        try {var items=root.AddComponent<MiningItemSystem>();var wallet=root.AddComponent<PlayerWallet>();Set(wallet,"currentMoney",1000f);test(items,wallet,data);}
        finally {UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(item);UnityEngine.Object.DestroyImmediate(data);}
    }
    [Test] public void OwnedTowerIsUsedWithoutBuyingAgain()
    {WithInventory((items,wallet,data)=>{data.price=100;items.TryAddItem(data.inventoryItem,3);Assert.IsTrue(TowerShopPanel.PrepareInventory(data,items,wallet));Assert.AreEqual(3,items.GetItemCount(data.inventoryItem));Assert.AreEqual(1000,wallet.CurrentMoney);});}
    [Test] public void BuyThenCancelRetainsExactlyOneTowerAndItsReceipt()
    {WithInventory((items,wallet,data)=>{data.price=100;Assert.IsTrue(TowerShopPanel.PrepareInventory(data,items,wallet));Assert.AreEqual(1,items.GetItemCount(data.inventoryItem));Assert.AreEqual(900,wallet.CurrentMoney);Assert.IsTrue(TowerShopPanel.PrepareInventory(data,items,wallet));Assert.AreEqual(1,items.GetItemCount(data.inventoryItem));Assert.AreEqual(900,wallet.CurrentMoney);Assert.AreEqual(35,items.TowerRefundAt(0));});}
    [Test] public void UnaffordableSelectionCannotSpendOrAdd()
    {WithInventory((items,wallet,data)=>{data.price=1001;Assert.IsFalse(TowerShopPanel.PrepareInventory(data,items,wallet));Assert.AreEqual(1000,wallet.CurrentMoney);Assert.AreEqual(0,items.GetItemCount(data.inventoryItem));});}
    [Test] public void FullInventoryCannotSpend()
    {WithInventory((items,wallet,data)=>{items.TryAddItem(data.inventoryItem,items.Capacity*data.inventoryItem.MaximumStack);var other=UnityEngine.Object.Instantiate(data.inventoryItem);try{Set(other,"tower",data);data.inventoryItem=other;data.price=100;Assert.IsFalse(TowerShopPanel.PrepareInventory(data,items,wallet));Assert.AreEqual(1000,wallet.CurrentMoney);}finally{UnityEngine.Object.DestroyImmediate(other);}});}
    [Test] public void BuildShortcutBelongsToLivePlayerMapAndCanBeRebound()
    {
        var asset=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Starter Assets/Runtime/InputSystem/StarterAssets.inputactions");
        var copy=UnityEngine.Object.Instantiate(asset);
        try {var build=copy.FindAction("Player/Build",true);Assert.AreEqual("<Keyboard>/t",build.bindings[0].path);build.ApplyBindingOverride(0,"<Keyboard>/y");Assert.AreEqual("<Keyboard>/y",build.bindings[0].effectivePath);Assert.AreEqual("<Keyboard>/t",asset.FindAction("Player/Build").bindings[0].effectivePath);}
        finally{UnityEngine.Object.DestroyImmediate(copy);}
    }
}
#endif
