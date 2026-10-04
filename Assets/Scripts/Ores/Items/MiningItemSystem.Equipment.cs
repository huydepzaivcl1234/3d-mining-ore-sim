using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MiningItemSystem
    {
        // Equipment addresses follow the bag; they do not increase bag capacity.
        public const int NecklaceSlotIndex = MiningItemDatabase.InventoryCapacity;
        public const int NecklaceSlotCount = 3;
        private readonly RuntimeSlot[] necklaces = { new(), new(), new() };
        private readonly MiningEquipmentBonuses combinedBonuses = new();
        public MiningItemData EquippedNecklace => necklaces[0].item;
        public MiningEquipmentBonuses EquipmentBonuses
        {
            get
            {
                bool equipped = false;
                combinedBonuses.lifeStealPercent = combinedBonuses.healingBonusPercent = 0f;
                combinedBonuses.regenIntervalReductionPercent = combinedBonuses.burnDamagePerTick = 0f;
                combinedBonuses.burnDurationSeconds = 0f;
                combinedBonuses.burnTickSeconds = 1f;
                foreach (var slot in necklaces)
                {
                    var bonus = slot.item != null ? slot.item.EquipmentBonuses : null;
                    if (bonus == null) continue;
                    equipped = true;
                    combinedBonuses.lifeStealPercent += bonus.lifeStealPercent;
                    combinedBonuses.healingBonusPercent += bonus.healingBonusPercent;
                    combinedBonuses.regenIntervalReductionPercent += bonus.regenIntervalReductionPercent;
                    // Burn is one status: choose the strongest equipped source, not duplicate ticks.
                    if (bonus.burnDamagePerTick / Mathf.Max(.1f, bonus.burnTickSeconds) >
                        combinedBonuses.burnDamagePerTick / Mathf.Max(.1f, combinedBonuses.burnTickSeconds))
                    {
                        combinedBonuses.burnDamagePerTick = bonus.burnDamagePerTick;
                        combinedBonuses.burnTickSeconds = bonus.burnTickSeconds;
                        combinedBonuses.burnDurationSeconds = bonus.burnDurationSeconds;
                    }
                }
                return equipped ? combinedBonuses : null;
            }
        }
        public static bool IsNecklaceSlot(int index) => index >= NecklaceSlotIndex && index < NecklaceSlotIndex + NecklaceSlotCount;
        public bool OwnsEquipment(MiningItemData item)
        {
            if (item == null) return false;
            foreach (var slot in necklaces) if (slot.item == item) return true;
            return GetItemCount(item) > 0;
        }
        private int FirstFreeNecklaceSlot()
        {
            for (int i = 0; i < necklaces.Length; i++) if (necklaces[i].item == null) return NecklaceSlotIndex + i;
            return -1;
        }

        private bool TryMoveEquipment(int from, int to, MiningItemData expected)
        {
            if (from == to || expected == null) return false;
            RuntimeSlot source = EquipmentMoveSlot(from);
            RuntimeSlot destination = EquipmentMoveSlot(to);
            if (source == null || destination == null) return false;
            if (source.item != expected || source.count != 1 || !expected.IsEquipment) return false;
            // A swap back into the necklace socket must never equip a potion or gift.
            if (destination.item != null && (!destination.item.IsEquipment || destination.count != 1)) return false;
            (source.item, destination.item) = (destination.item, source.item);
            (source.count, destination.count) = (destination.count, source.count);
            SaveInventory();
            InventoryChanged?.Invoke();
            EffectsChanged?.Invoke();
            return true;
        }

        private RuntimeSlot EquipmentMoveSlot(int index) => IsNecklaceSlot(index)
            ? necklaces[index - NecklaceSlotIndex] : index >= 0 && index < slots.Length ? slots[index] : null;

        public bool TryUnequipNecklace(int index = NecklaceSlotIndex)
        {
            if (!IsNecklaceSlot(index)) return false;
            var item = necklaces[index - NecklaceSlotIndex].item;
            if (item == null) return false;
            EnsureRuntimeSlots();
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].item == null || slots[i].count <= 0)
                    return TryMoveEquipment(index, i, item);
            return false; // Full bag: keep the item equipped, never delete it.
        }

        public bool TryBuyEquipment(MiningItemData item, PlayerWallet wallet)
        {
            if (item == null || !item.IsEquipment || wallet == null || OwnsEquipment(item) || !CanAddItem(item)) return false;
            float price = item.EquipmentGemPrice;
            if (float.IsNaN(price) || float.IsInfinity(price) || !wallet.TrySpendGems(price)) return false;
            if (TryAddItem(item)) return true;
            wallet.AddGems(price); // Inventory may have changed through a wallet event listener.
            return false;
        }
    }
}
