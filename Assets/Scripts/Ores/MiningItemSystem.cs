using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns item drops, the 32-slot inventory, saves and active timed effects.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningItemSystem : MonoBehaviour
    {
        [Serializable]
        private sealed class SavedInventory
        {
            public int version = 1;
            public List<SavedSlot> slots = new();
        }

        [Serializable]
        private sealed class SavedSlot
        {
            public string itemId;
            public int count;
        }

        private sealed class RuntimeSlot
        {
            public MiningItemData item;
            public int count;
        }

        private struct RuntimeEffect
        {
            public MiningItemData item;
            public float endTime;
        }

        public readonly struct InventorySlotView
        {
            public InventorySlotView(MiningItemData item, int count)
            {
                Item = item;
                Count = count;
            }

            public MiningItemData Item { get; }
            public int Count { get; }
            public bool IsEmpty => Item == null || Count <= 0;
        }

        public readonly struct ActiveEffectView
        {
            public ActiveEffectView(MiningItemData item, float remainingSeconds)
            {
                Item = item;
                RemainingSeconds = remainingSeconds;
            }

            public MiningItemData Item { get; }
            public float RemainingSeconds { get; }
        }

        [Header("References")]
        [SerializeField] private MiningItemDatabase database;
        [SerializeField] private OreSpawner oreSpawner;
        [SerializeField] private LuckyBlockDropSystem luckyBlockSystem;
        [SerializeField] private MiningUpgradeSystem upgradeSystem;
        [SerializeField] private Transform droppedItemParent;

        private readonly RuntimeSlot[] slots = new RuntimeSlot[MiningItemDatabase.InventoryCapacity];
        private readonly Dictionary<MiningItemEffectType, RuntimeEffect> activeEffects = new();
        private readonly List<MiningItemEffectType> expiredEffects = new(3);

        public MiningItemDatabase Database => database;
        public int Capacity => MiningItemDatabase.InventoryCapacity;
        public int OccupiedSlotCount
        {
            get
            {
                int occupied = 0;
                foreach (RuntimeSlot slot in slots)
                {
                    if (slot != null && slot.item != null && slot.count > 0)
                    {
                        occupied++;
                    }
                }
                return occupied;
            }
        }
        public float NpcDamageMultiplier => GetEffectMultiplier(MiningItemEffectType.NpcDamage);
        public float MoneyRewardMultiplier => GetEffectMultiplier(MiningItemEffectType.MoneyReward);
        public float NpcMoveSpeedMultiplier => GetEffectMultiplier(MiningItemEffectType.NpcMoveSpeed);

        public event Action InventoryChanged;
        public event Action EffectsChanged;
        public event Action<MiningItemData> ItemCollected;
        public event Action<MiningItemData> ItemUsed;

        private void Awake()
        {
            EnsureRuntimeSlots();
            FindReferencesIfMissing();
            LoadInventory();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            // Editor setup may finish references after OnEnable in an already-open Scene.
            FindReferencesIfMissing();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (activeEffects.Count == 0)
            {
                return;
            }

            expiredEffects.Clear();
            foreach (KeyValuePair<MiningItemEffectType, RuntimeEffect> pair in activeEffects)
            {
                if (Time.time >= pair.Value.endTime)
                {
                    expiredEffects.Add(pair.Key);
                }
            }
            if (expiredEffects.Count == 0)
            {
                return;
            }
            foreach (MiningItemEffectType type in expiredEffects)
            {
                activeEffects.Remove(type);
            }
            EffectsChanged?.Invoke();
        }

        public InventorySlotView GetSlot(int index)
        {
            EnsureRuntimeSlots();
            if (index < 0 || index >= slots.Length)
            {
                return new InventorySlotView(null, 0);
            }
            RuntimeSlot slot = slots[index];
            return new InventorySlotView(slot.item, slot.count);
        }

        public bool TryAddItem(MiningItemData item, int amount = 1)
        {
            EnsureRuntimeSlots();
            if (item == null || amount <= 0 || GetAvailableSpace(item) < amount)
            {
                return false;
            }

            int remaining = amount;
            foreach (RuntimeSlot slot in slots)
            {
                if (slot.item != item || slot.count >= item.MaximumStack)
                {
                    continue;
                }
                int added = Mathf.Min(remaining, item.MaximumStack - slot.count);
                slot.count += added;
                remaining -= added;
                if (remaining == 0)
                {
                    break;
                }
            }
            for (int index = 0; index < slots.Length && remaining > 0; index++)
            {
                RuntimeSlot slot = slots[index];
                if (slot.item != null && slot.count > 0)
                {
                    continue;
                }
                int added = Mathf.Min(remaining, item.MaximumStack);
                slot.item = item;
                slot.count = added;
                remaining -= added;
            }

            SaveInventory();
            InventoryChanged?.Invoke();
            ItemCollected?.Invoke(item);
            return true;
        }

        public bool TryUseSlot(int index)
        {
            EnsureRuntimeSlots();
            if (index < 0 || index >= slots.Length)
            {
                return false;
            }
            RuntimeSlot slot = slots[index];
            if (slot.item == null || slot.count <= 0)
            {
                return false;
            }

            MiningItemData item = slot.item;
            slot.count--;
            if (slot.count == 0)
            {
                slot.item = null;
            }
            ActivateEffect(item);
            SaveInventory();
            InventoryChanged?.Invoke();
            ItemUsed?.Invoke(item);
            return true;
        }

        public void GetActiveEffects(List<ActiveEffectView> results)
        {
            if (results == null)
            {
                return;
            }
            results.Clear();
            foreach (RuntimeEffect effect in activeEffects.Values)
            {
                float remaining = Mathf.Max(0f, effect.endTime - Time.time);
                if (effect.item != null && remaining > 0f)
                {
                    results.Add(new ActiveEffectView(effect.item, remaining));
                }
            }
        }

        public void ResetAllData()
        {
            EnsureRuntimeSlots();
            foreach (RuntimeSlot slot in slots)
            {
                slot.item = null;
                slot.count = 0;
            }
            activeEffects.Clear();
            if (database != null && !string.IsNullOrEmpty(database.InventorySaveKey))
            {
                PlayerPrefs.DeleteKey(database.InventorySaveKey);
                PlayerPrefs.Save();
            }

            MiningWorldItem[] worldItems = FindObjectsByType<MiningWorldItem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (MiningWorldItem worldItem in worldItems)
            {
                if (worldItem != null && worldItem.gameObject.scene.IsValid())
                {
                    worldItem.gameObject.SetActive(false);
                    Destroy(worldItem.gameObject);
                }
            }
            InventoryChanged?.Invoke();
            EffectsChanged?.Invoke();
        }

        private void ActivateEffect(MiningItemData item)
        {
            float startTime = Time.time;
            if (activeEffects.TryGetValue(item.EffectType, out RuntimeEffect current))
            {
                startTime = Mathf.Max(startTime, current.endTime);
            }
            activeEffects[item.EffectType] = new RuntimeEffect
            {
                item = item,
                endTime = startTime + item.EffectDurationSeconds
            };
            EffectsChanged?.Invoke();
        }

        private float GetEffectMultiplier(MiningItemEffectType type)
        {
            if (!activeEffects.TryGetValue(type, out RuntimeEffect effect) ||
                effect.item == null || Time.time >= effect.endTime)
            {
                return 1f;
            }
            return 1f + effect.item.EffectPercent * 0.01f;
        }

        private int GetAvailableSpace(MiningItemData item)
        {
            int available = 0;
            foreach (RuntimeSlot slot in slots)
            {
                if (slot.item == item)
                {
                    available += item.MaximumStack - slot.count;
                }
                else if (slot.item == null || slot.count <= 0)
                {
                    available += item.MaximumStack;
                }
            }
            return available;
        }

        private void HandleOreRewardGranted(Ore ore, float reward)
        {
            if (ore != null && database != null && database.TryRoll(false,
                GetItemDropChanceBonus(), out MiningItemData item))
            {
                SpawnWorldItem(item, ore.GetWorldTopCenter());
            }
        }

        private void HandleLuckyBlockRewardGranted(LuckyBlock block, float reward)
        {
            if (block != null && database != null && database.TryRoll(true,
                GetItemDropChanceBonus(), out MiningItemData item))
            {
                SpawnWorldItem(item, block.GetWorldTopCenter());
            }
        }

        private float GetItemDropChanceBonus()
        {
            return upgradeSystem != null
                ? upgradeSystem.GetAddedPercent(MiningUpgradeType.ItemDropChance)
                : 0f;
        }

        private void SpawnWorldItem(MiningItemData item, Vector3 sourcePosition)
        {
            GameObject root = new($"Dropped {item.DisplayName}");
            if (droppedItemParent != null)
            {
                root.transform.SetParent(droppedItemParent, true);
            }
            root.transform.position = sourcePosition + Vector3.up * database.SpawnHeight;
            MiningWorldItem worldItem = root.AddComponent<MiningWorldItem>();
            worldItem.Initialize(this, item, database);
        }

        private void Subscribe()
        {
            if (oreSpawner != null)
            {
                oreSpawner.OreRewardGranted -= HandleOreRewardGranted;
                oreSpawner.OreRewardGranted += HandleOreRewardGranted;
            }
            if (luckyBlockSystem != null)
            {
                luckyBlockSystem.LuckyBlockRewardGranted -= HandleLuckyBlockRewardGranted;
                luckyBlockSystem.LuckyBlockRewardGranted += HandleLuckyBlockRewardGranted;
            }
        }

        private void Unsubscribe()
        {
            if (oreSpawner != null)
            {
                oreSpawner.OreRewardGranted -= HandleOreRewardGranted;
            }
            if (luckyBlockSystem != null)
            {
                luckyBlockSystem.LuckyBlockRewardGranted -= HandleLuckyBlockRewardGranted;
            }
        }

        private void FindReferencesIfMissing()
        {
            if (oreSpawner == null)
            {
                oreSpawner = FindFirstObjectByType<OreSpawner>(FindObjectsInactive.Include);
            }
            if (luckyBlockSystem == null)
            {
                luckyBlockSystem = FindFirstObjectByType<LuckyBlockDropSystem>(
                    FindObjectsInactive.Include);
            }
            if (upgradeSystem == null)
            {
                upgradeSystem = FindFirstObjectByType<MiningUpgradeSystem>(
                    FindObjectsInactive.Include);
            }
        }

        private void LoadInventory()
        {
            EnsureRuntimeSlots();
            if (database == null || string.IsNullOrEmpty(database.InventorySaveKey) ||
                !PlayerPrefs.HasKey(database.InventorySaveKey))
            {
                return;
            }

            SavedInventory save;
            try
            {
                save = JsonUtility.FromJson<SavedInventory>(
                    PlayerPrefs.GetString(database.InventorySaveKey));
            }
            catch (ArgumentException)
            {
                return;
            }
            if (save?.slots == null)
            {
                return;
            }

            int count = Mathf.Min(slots.Length, save.slots.Count);
            for (int index = 0; index < count; index++)
            {
                SavedSlot saved = save.slots[index];
                MiningItemData item = database.FindById(saved.itemId);
                if (item == null || saved.count <= 0)
                {
                    continue;
                }
                slots[index].item = item;
                slots[index].count = Mathf.Clamp(saved.count, 1, item.MaximumStack);
            }
        }

        private void SaveInventory()
        {
            EnsureRuntimeSlots();
            if (database == null || string.IsNullOrEmpty(database.InventorySaveKey))
            {
                return;
            }
            var save = new SavedInventory();
            foreach (RuntimeSlot slot in slots)
            {
                save.slots.Add(new SavedSlot
                {
                    itemId = slot.item != null ? slot.item.ItemId : string.Empty,
                    count = slot.item != null ? slot.count : 0
                });
            }
            PlayerPrefs.SetString(database.InventorySaveKey, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
        }

        private void EnsureRuntimeSlots()
        {
            for (int index = 0; index < slots.Length; index++)
            {
                slots[index] ??= new RuntimeSlot();
            }
        }
    }
}
