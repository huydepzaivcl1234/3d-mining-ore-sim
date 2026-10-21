using System.Collections.Generic;

namespace MiningSimulator.Ores
{
    public sealed partial class MiningItemSystem
    {
        // Construction stock shares the inventory save transaction, not its slots/capacity.
        readonly Dictionary<string, List<float>> towerStock = new();
        public int GetTowerStockCount(MiningItemData item)
            => item != null && towerStock.TryGetValue(item.ItemId, out var receipts) ? receipts.Count : 0;

        public bool TryBuyConstructionTower(TowerData tower, PlayerWallet wallet)
        {
            if (tower == null || tower.prefab == null || tower.inventoryItem == null ||
                string.IsNullOrEmpty(tower.inventoryItem.ItemId) || wallet == null ||
                float.IsNaN(tower.price) || float.IsInfinity(tower.price) || tower.price < 0) return false;
            float price = tower.price;
            if (!wallet.TrySpend(price)) return false;
            StockReceipts(tower.inventoryItem.ItemId).Add(price);
            SaveInventory(); InventoryChanged?.Invoke(); return true;
        }

        public bool TryTakeConstructionTower(MiningItemData item, out float paid)
        {
            paid = 0;
            if (item == null || !towerStock.TryGetValue(item.ItemId, out var receipts) || receipts.Count == 0) return false;
            paid = receipts[0]; receipts.RemoveAt(0);
            SaveInventory(); InventoryChanged?.Invoke(); return true;
        }

        List<float> StockReceipts(string id)
        {
            if (!towerStock.TryGetValue(id, out var receipts)) towerStock.Add(id, receipts = new List<float>());
            return receipts;
        }

        public void TransferLegacyTowersToConstruction()
        {
            EnsureRuntimeSlots(); bool changed = false;
            foreach (var slot in slots)
            {
                if (slot.item == null || slot.item.UseType != MiningItemUseType.Tower || slot.count <= 0) continue;
                var receipts = StockReceipts(slot.item.ItemId);
                while (slot.count > 0) receipts.Add(TakeTowerReceipt(slot));
                changed = true;
            }
            if (changed) { SaveInventory(); InventoryChanged?.Invoke(); }
        }
    }
}
