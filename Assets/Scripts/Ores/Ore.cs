using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Runtime identity and durability for one spawned ore prefab.
    /// Mining rewards are returned only when this instance is fully depleted.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Ore : MonoBehaviour
    {
        [SerializeField] private OreData data;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MiningUpgradeSystem upgradeSystem;
        [SerializeField, Min(0)] private int currentDurability;

        private bool rewardGranted;
        private float damageRemainder;
        private Collider[] miningColliders;
        private readonly Dictionary<MiningNpc, int> reservedMiners = new();

        public OreData Data => data;
        public int CurrentDurability => currentDurability;
        public int MaxDurability => data != null ? data.Durability : 0;
        public bool IsDepleted => currentDurability <= 0;
        public event Action<Ore> Depleted;
        public event Action<int, int> DurabilityChanged;

        public bool CanAcceptMiner(MiningNpc miner, int miningPower)
        {
            RemoveMissingReservations();
            if (miner == null || data == null || IsDepleted ||
                miningPower < data.MiningPowerRequired)
            {
                return false;
            }

            return reservedMiners.ContainsKey(miner) || reservedMiners.Count < data.MaximumMiningNpcs;
        }

        public bool TryReserveMiner(MiningNpc miner, int miningPower, out int slotIndex)
        {
            slotIndex = -1;
            if (!CanAcceptMiner(miner, miningPower))
            {
                return false;
            }

            if (reservedMiners.TryGetValue(miner, out slotIndex))
            {
                return true;
            }

            bool[] usedSlots = new bool[data.MaximumMiningNpcs];
            foreach (int usedSlot in reservedMiners.Values)
            {
                if (usedSlot >= 0 && usedSlot < usedSlots.Length)
                {
                    usedSlots[usedSlot] = true;
                }
            }

            for (int index = 0; index < usedSlots.Length; index++)
            {
                if (usedSlots[index])
                {
                    continue;
                }

                reservedMiners.Add(miner, index);
                slotIndex = index;
                return true;
            }

            return false;
        }

        public void ReleaseMiner(MiningNpc miner)
        {
            if (miner != null)
            {
                reservedMiners.Remove(miner);
            }
        }

        public Vector3 GetMiningStandPosition(int slotIndex, float minerRadius, float spacingPadding)
        {
            int slotCount = data != null ? Mathf.Max(1, data.MaximumMiningNpcs) : 1;
            float angle = 360f * Mathf.Clamp(slotIndex, 0, slotCount - 1) / slotCount;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

            float oreRadius = 0f;
            foreach (Collider targetCollider in GetMiningColliders())
            {
                if (targetCollider == null || !targetCollider.enabled || targetCollider.isTrigger)
                {
                    continue;
                }

                Vector3 extents = targetCollider.bounds.extents;
                oreRadius = Mathf.Max(oreRadius, extents.x, extents.z);
            }

            float collisionSafeRadius = oreRadius + minerRadius + spacingPadding;
            if (slotCount > 1)
            {
                float halfChordAngle = Mathf.PI / slotCount;
                float slotSafeRadius = (minerRadius + spacingPadding * 0.5f) /
                                       Mathf.Max(Mathf.Sin(halfChordAngle), 0.01f);
                collisionSafeRadius = Mathf.Max(collisionSafeRadius, slotSafeRadius);
            }

            float configuredRadius = data != null ? data.NpcStandDistance : 0f;
            return transform.position + direction * Mathf.Max(configuredRadius, collisionSafeRadius);
        }

        public float SqrDistanceToSurface(Vector3 worldPosition)
        {
            float closestDistance = float.PositiveInfinity;
            bool foundCollider = false;
            foreach (Collider targetCollider in GetMiningColliders())
            {
                if (targetCollider == null || !targetCollider.enabled || targetCollider.isTrigger)
                {
                    continue;
                }

                Vector3 closestPoint = targetCollider.ClosestPoint(worldPosition);
                closestPoint.y = worldPosition.y;
                closestDistance = Mathf.Min(closestDistance, (closestPoint - worldPosition).sqrMagnitude);
                foundCollider = true;
            }

            if (foundCollider)
            {
                return closestDistance;
            }

            Vector3 offset = transform.position - worldPosition;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }

        private void Awake()
        {
            miningColliders = GetComponentsInChildren<Collider>();
            ResetDurability();
        }

        public void SetData(OreData oreData)
        {
            data = oreData;
            ResetDurability();
        }

        public void Initialize(OreData oreData, PlayerWallet playerWallet,
            MiningUpgradeSystem targetUpgradeSystem)
        {
            data = oreData;
            wallet = playerWallet;
            upgradeSystem = targetUpgradeSystem;
            miningColliders = GetComponentsInChildren<Collider>();
            ResetDurability();
        }

        public void ConfigureRuntime(PlayerWallet playerWallet, MiningUpgradeSystem targetUpgradeSystem)
        {
            wallet = playerWallet;
            upgradeSystem = targetUpgradeSystem;
        }

        public bool MineOnce()
        {
            return data != null && ApplyDamage(data.ClickDamage);
        }

        public bool ApplyDamage(int damage)
        {
            if (data == null || IsDepleted || damage <= 0)
            {
                return false;
            }

            float multiplier = upgradeSystem != null
                ? upgradeSystem.GetMultiplier(MiningUpgradeType.OreDamage)
                : 1f;
            float upgradedDamage = damage * multiplier + damageRemainder;
            int appliedDamage = Mathf.Max(1, Mathf.FloorToInt(upgradedDamage));
            damageRemainder = upgradedDamage - appliedDamage;
            currentDurability = Mathf.Max(0, currentDurability - appliedDamage);
            DurabilityChanged?.Invoke(currentDurability, MaxDurability);
            if (currentDurability == 0)
            {
                Deplete();
            }

            return true;
        }

        public bool TryMine(int miningPower, out int moneyEarned)
        {
            moneyEarned = 0;
            if (data == null || IsDepleted || miningPower < data.MiningPowerRequired)
            {
                return false;
            }

            currentDurability = Mathf.Max(0, currentDurability - Mathf.Max(1, miningPower));
            if (currentDurability == 0)
            {
                moneyEarned = data.BaseSellValue;
            }

            return true;
        }

        public void ResetDurability()
        {
            reservedMiners.Clear();
            currentDurability = data != null ? data.Durability : 0;
            damageRemainder = 0f;
            rewardGranted = false;
            DurabilityChanged?.Invoke(currentDurability, MaxDurability);
        }

        private void Deplete()
        {
            if (rewardGranted)
            {
                return;
            }

            rewardGranted = true;
            reservedMiners.Clear();
            int reward = upgradeSystem != null
                ? upgradeSystem.CalculateMiningReward(data.BaseSellValue)
                : data.BaseSellValue;
            wallet?.AddMoney(reward);
            Depleted?.Invoke(this);
            Destroy(gameObject, data.DestroyDelay);
        }

        private void OnDisable()
        {
            reservedMiners.Clear();
        }

        private void RemoveMissingReservations()
        {
            List<MiningNpc> missing = null;
            foreach (MiningNpc miner in reservedMiners.Keys)
            {
                if (miner != null)
                {
                    continue;
                }

                missing ??= new List<MiningNpc>();
                missing.Add(miner);
            }

            if (missing == null)
            {
                return;
            }

            foreach (MiningNpc miner in missing)
            {
                reservedMiners.Remove(miner);
            }
        }

        private Collider[] GetMiningColliders()
        {
            miningColliders ??= GetComponentsInChildren<Collider>();
            return miningColliders;
        }
    }
}
