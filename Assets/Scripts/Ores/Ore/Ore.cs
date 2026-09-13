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
        private bool destroyOnDeplete = true;
        private float damageRemainder;
        private Collider[] miningColliders;
        private MiningHitPunch hitPunch;
        private readonly Dictionary<MiningNpc, int> reservedMiners = new();

        public OreData Data => data;
        public int CurrentDurability => currentDurability;
        public int MaxDurability => data != null ? data.Durability : 0;
        public bool IsDepleted => currentDurability <= 0;
        public bool LastDamageWasNpc { get; private set; }
        public event Action<Ore> Depleted;
        public event Action<Ore> Damaged;
        public event Action<Ore, float> RewardGranted;
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

            bool hasBounds = false;
            Bounds combinedBounds = default;
            foreach (Collider targetCollider in GetMiningColliders())
            {
                if (targetCollider == null || !targetCollider.enabled || targetCollider.isTrigger)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = targetCollider.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(targetCollider.bounds);
                }
            }

            Vector3 standCenter = hasBounds ? combinedBounds.center : transform.position;
            standCenter.y = transform.position.y;
            Vector3 extents = hasBounds ? combinedBounds.extents : Vector3.zero;
            float directionalOreRadius = Mathf.Abs(direction.x) * extents.x +
                                         Mathf.Abs(direction.z) * extents.z;
            float collisionSafeRadius = directionalOreRadius + minerRadius + spacingPadding;
            if (slotCount > 1)
            {
                float halfChordAngle = Mathf.PI / slotCount;
                float slotSafeRadius = (minerRadius + spacingPadding * 0.5f) /
                                       Mathf.Max(Mathf.Sin(halfChordAngle), 0.01f);
                collisionSafeRadius = Mathf.Max(collisionSafeRadius, slotSafeRadius);
            }

            float configuredRadius = data != null ? data.NpcStandDistance : 0f;
            return standCenter + direction * Mathf.Max(configuredRadius, collisionSafeRadius);
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

        public Vector3 GetWorldTopCenter()
        {
            bool hasBounds = false;
            Bounds combinedBounds = default;
            foreach (Collider targetCollider in GetMiningColliders())
            {
                if (targetCollider == null || !targetCollider.enabled || targetCollider.isTrigger)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = targetCollider.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(targetCollider.bounds);
                }
            }

            return hasBounds
                ? new Vector3(combinedBounds.center.x, combinedBounds.max.y, combinedBounds.center.z)
                : transform.position;
        }

        private void Awake()
        {
            miningColliders = GetComponentsInChildren<Collider>();
            ConfigureHitPunch();
            ResetDurability();
        }

        public void SetData(OreData oreData)
        {
            data = oreData;
            ConfigureHitPunch();
            ResetDurability();
        }

        public void Initialize(OreData oreData, PlayerWallet playerWallet,
            MiningUpgradeSystem targetUpgradeSystem, bool shouldDestroyOnDeplete = true)
        {
            data = oreData;
            wallet = playerWallet;
            upgradeSystem = targetUpgradeSystem;
            destroyOnDeplete = shouldDestroyOnDeplete;
            miningColliders = GetComponentsInChildren<Collider>();
            ConfigureHitPunch();
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
            return ApplyExactDamage(damage * multiplier, false);
        }

        public bool ApplyNpcDamage(float damage)
        {
            return data != null && !IsDepleted && damage > 0f && ApplyExactDamage(damage, true);
        }

        private bool ApplyExactDamage(float damage, bool fromNpc)
        {
            LastDamageWasNpc = false;
            float accumulatedDamage = damage + damageRemainder;
            int appliedDamage = Mathf.FloorToInt(accumulatedDamage);
            if (appliedDamage <= 0)
            {
                damageRemainder = accumulatedDamage;
                return true;
            }
            damageRemainder = accumulatedDamage - appliedDamage;
            LastDamageWasNpc = fromNpc;
            currentDurability = Mathf.Max(0, currentDurability - appliedDamage);
            hitPunch?.Play();
            Damaged?.Invoke(this);
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
            hitPunch?.Play();
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
            float reward = upgradeSystem != null
                ? upgradeSystem.CalculateMiningReward(data.BaseSellValue)
                : data.BaseSellValue;
            wallet?.AddMoney(reward);
            RewardGranted?.Invoke(this, reward);
            hitPunch?.PlayBreak(data.BreakSquashAmount, data.BreakStretchAmount,
                data.BreakAnimationDuration);
            Depleted?.Invoke(this);
            if (destroyOnDeplete)
            {
                Destroy(gameObject, Mathf.Max(data.DestroyDelay, data.BreakAnimationDuration));
            }
        }

        private void OnDisable()
        {
            reservedMiners.Clear();
            hitPunch?.ResetImmediately();
        }

        private void ConfigureHitPunch()
        {
            if (data == null)
            {
                return;
            }

            hitPunch ??= GetComponent<MiningHitPunch>();
            hitPunch ??= gameObject.AddComponent<MiningHitPunch>();
            hitPunch.Configure(transform, data.HitPunchScale, data.HitPunchLift,
                data.HitPunchDuration);
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
