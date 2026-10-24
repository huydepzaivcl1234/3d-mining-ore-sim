#if UNITY_EDITOR
using System;
using System.Reflection;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class TowerStockTests
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Fixture(Action<MiningItemSystem,PlayerWallet,CannonTowerData> run)
    {
        var root=new GameObject("Tower stock test");root.SetActive(false);
        var data=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<CannonTowerData>("Assets/GameData/Tower/Cannon/CannonData.asset"));
        var item=UnityEngine.Object.Instantiate(data.inventoryItem);data.inventoryItem=item;
        typeof(MiningItemData).GetField("tower",Flags).SetValue(item,data);
        try{var inventory=root.AddComponent<MiningItemSystem>();var wallet=root.AddComponent<PlayerWallet>();typeof(PlayerWallet).GetField("currentMoney",Flags).SetValue(wallet,1000f);run(inventory,wallet,data);}
        finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(item);UnityEngine.Object.DestroyImmediate(data);}
    }
    [Test] public void RepeatedPurchaseUsesNoBagSlotsAndKeepsIndividualReceipts()
    {Fixture((items,wallet,data)=>{data.price=100;Assert.IsTrue(items.TryBuyConstructionTower(data,wallet));data.price=200;Assert.IsTrue(items.TryBuyConstructionTower(data,wallet));Assert.AreEqual(2,items.GetTowerStockCount(data.inventoryItem));Assert.AreEqual(0,items.GetItemCount(data.inventoryItem));Assert.AreEqual(0,items.OccupiedSlotCount);Assert.AreEqual(700,wallet.CurrentMoney);Assert.IsTrue(items.TryTakeConstructionTower(data.inventoryItem,out var first));Assert.AreEqual(100,first);Assert.IsTrue(items.TryTakeConstructionTower(data.inventoryItem,out var second));Assert.AreEqual(200,second);Assert.IsFalse(items.TryTakeConstructionTower(data.inventoryItem,out _));});}
    [Test] public void LegacyTransferIsIdempotentAndPreservesPaidPrice()
    {Fixture((items,wallet,data)=>{data.price=125;items.TryBuyTower(data,wallet);items.TryAddItem(data.inventoryItem,2);items.TransferLegacyTowersToConstruction();items.TransferLegacyTowersToConstruction();Assert.AreEqual(3,items.GetTowerStockCount(data.inventoryItem));Assert.AreEqual(0,items.GetItemCount(data.inventoryItem));items.TryTakeConstructionTower(data.inventoryItem,out var paid);Assert.AreEqual(125,paid);items.TryTakeConstructionTower(data.inventoryItem,out paid);Assert.AreEqual(0,paid);});}
    [Test] public void InvalidOrUnaffordablePurchaseCannotChangeStockOrWallet()
    {Fixture((items,wallet,data)=>{foreach(float price in new[]{-1f,float.NaN,float.PositiveInfinity,1001f}){data.price=price;Assert.IsFalse(items.TryBuyConstructionTower(data,wallet));}Assert.AreEqual(0,items.GetTowerStockCount(data.inventoryItem));Assert.AreEqual(1000,wallet.CurrentMoney);});}
    [Test] public void ConstructionPurchasesWorkWithAFullBag()
    {Fixture((items,wallet,data)=>{items.TryAddItem(data.inventoryItem,items.Capacity*data.inventoryItem.MaximumStack);int before=items.OccupiedSlotCount;data.price=100;Assert.IsTrue(items.TryBuyConstructionTower(data,wallet));Assert.AreEqual(before,items.OccupiedSlotCount);Assert.AreEqual(1,items.GetTowerStockCount(data.inventoryItem));});}
}
#endif
