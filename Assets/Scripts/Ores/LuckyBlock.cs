using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Runtime state for one falling, mineable Lucky Block.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public sealed class LuckyBlock : MonoBehaviour
    {
        [SerializeField] private LuckyBlockType type;
        [SerializeField, Min(0)] private int currentDurability;

        private LuckyBlockVariantData variant;
        private LuckyBlockData settings;
        private PlayerWallet wallet;
        private MiningUpgradeSystem upgradeSystem;
        private Rigidbody body;
        private Transform visualRoot;
        private Vector3 authoredVisualLocalPosition;
        private Quaternion authoredVisualLocalRotation = Quaternion.identity;
        private Vector3 authoredVisualLocalScale = Vector3.one;
        private bool hasAuthoredVisualTransform;
        private float lifetime;
        private MiningHitPunch hitPunch;
        private bool resolved;
        private bool hasLanded;
        private float damageRemainder;
        private readonly Dictionary<MiningNpc, int> reservedMiners = new();

        public LuckyBlockType Type => type;
        public int CurrentDurability => currentDurability;
        public int MaximumDurability => variant != null ? variant.Durability : 0;
        public LuckyBlockData Settings => settings;
        public bool IsResolved => resolved;
        public event Action<LuckyBlock> Damaged;
        public event Action<int, int> DurabilityChanged;
        public event Action<LuckyBlock, float> RewardGranted;
        public event Action<LuckyBlock> Broken;
        public event Action<LuckyBlock> Expired;

        public bool CanAcceptMiner(MiningNpc miner, int miningPower)
        {
            RemoveMissingReservations();
            return miner != null && variant != null && hasLanded && !resolved &&
                   isActiveAndEnabled &&
                   miningPower >= variant.MiningPowerRequired &&
                   (reservedMiners.ContainsKey(miner) ||
                    reservedMiners.Count < variant.MaximumMiningNpcs);
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

            bool[] usedSlots = new bool[variant.MaximumMiningNpcs];
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

        public Vector3 GetMiningStandPosition(int slotIndex, float minerRadius,
            float spacingPadding)
        {
            int slotCount = variant != null ? Mathf.Max(1, variant.MaximumMiningNpcs) : 1;
            float angle = 360f * Mathf.Clamp(slotIndex, 0, slotCount - 1) / slotCount;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Collider targetCollider = GetComponent<Collider>();
            Bounds bounds = targetCollider != null ? targetCollider.bounds :
                new Bounds(transform.position, Vector3.zero);
            Vector3 standCenter = bounds.center;
            standCenter.y = transform.position.y;
            float directionalRadius = Mathf.Abs(direction.x) * bounds.extents.x +
                                      Mathf.Abs(direction.z) * bounds.extents.z;
            float safeRadius = directionalRadius + minerRadius + spacingPadding;
            if (slotCount > 1)
            {
                float halfChordAngle = Mathf.PI / slotCount;
                float slotSafeRadius = (minerRadius + spacingPadding * 0.5f) /
                                       Mathf.Max(Mathf.Sin(halfChordAngle), 0.01f);
                safeRadius = Mathf.Max(safeRadius, slotSafeRadius);
            }

            float configuredRadius = variant != null ? variant.NpcStandDistance : 0f;
            return standCenter + direction * Mathf.Max(configuredRadius, safeRadius);
        }

        public float SqrDistanceToSurface(Vector3 worldPosition)
        {
            Collider targetCollider = GetComponent<Collider>();
            if (targetCollider == null || !targetCollider.enabled || targetCollider.isTrigger)
            {
                Vector3 offset = transform.position - worldPosition;
                offset.y = 0f;
                return offset.sqrMagnitude;
            }

            Vector3 closestPoint = targetCollider.ClosestPoint(worldPosition);
            closestPoint.y = worldPosition.y;
            return (closestPoint - worldPosition).sqrMagnitude;
        }

        public void ConfigureVisualRoot(Transform targetVisualRoot)
        {
            visualRoot = targetVisualRoot;
            if (visualRoot == null)
            {
                hasAuthoredVisualTransform = false;
                return;
            }

            authoredVisualLocalPosition = visualRoot.localPosition;
            authoredVisualLocalRotation = visualRoot.localRotation;
            authoredVisualLocalScale = visualRoot.localScale;
            hasAuthoredVisualTransform = true;
        }

        public void RestoreAuthoredVisualTransform()
        {
            if (!hasAuthoredVisualTransform || visualRoot == null)
            {
                return;
            }

            visualRoot.SetLocalPositionAndRotation(authoredVisualLocalPosition,
                authoredVisualLocalRotation);
            visualRoot.localScale = authoredVisualLocalScale;
        }

        public void Initialize(LuckyBlockVariantData targetVariant, LuckyBlockData targetSettings,
            PlayerWallet targetWallet, Transform targetVisualRoot, float spinDegreesPerSecond,
            MiningUpgradeSystem targetUpgradeSystem = null)
        {
            variant = targetVariant;
            settings = targetSettings;
            wallet = targetWallet;
            upgradeSystem = targetUpgradeSystem;
            if (visualRoot != targetVisualRoot || !hasAuthoredVisualTransform)
            {
                ConfigureVisualRoot(targetVisualRoot);
            }
            type = variant.Type;
            currentDurability = Mathf.Max(1, variant.Durability);
            resolved = false;
            lifetime = 0f;
            hasLanded = false;
            damageRemainder = 0f;
            reservedMiners.Clear();
            body ??= GetComponent<Rigidbody>();
            if (visualRoot != null)
            {
                hitPunch ??= GetComponent<MiningHitPunch>();
                hitPunch ??= gameObject.AddComponent<MiningHitPunch>();
                hitPunch.Configure(visualRoot, settings.HitPunchScale, settings.HitPunchLift,
                    settings.HitPunchDuration);
            }

            body.mass = settings.Mass;
            body.linearDamping = settings.LinearDamping;
            body.angularDamping = settings.AngularDamping;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.up * (spinDegreesPerSecond * Mathf.Deg2Rad);
            body.isKinematic = false;
            body.useGravity = true;
            body.WakeUp();
            DurabilityChanged?.Invoke(currentDurability, MaximumDurability);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!resolved && collision.collider != null)
            {
                hasLanded = true;
            }
        }

        public bool MineOnce()
        {
            return variant != null && ApplyDamage(variant.ClickDamage);
        }

        public bool ApplyDamage(int damage)
        {
            if (resolved || variant == null || damage <= 0)
            {
                return false;
            }

            return ApplyExactDamage(damage);
        }

        public bool ApplyNpcDamage(float damage)
        {
            return !resolved && variant != null && damage > 0f && ApplyExactDamage(damage);
        }

        private bool ApplyExactDamage(float damage)
        {
            float accumulatedDamage = damage + damageRemainder;
            int appliedDamage = Mathf.FloorToInt(accumulatedDamage);
            if (appliedDamage <= 0)
            {
                damageRemainder = accumulatedDamage;
                return true;
            }
            damageRemainder = accumulatedDamage - appliedDamage;
            currentDurability = Mathf.Max(0, currentDurability - appliedDamage);
            hitPunch?.Play();
            DurabilityChanged?.Invoke(currentDurability, MaximumDurability);
            if (currentDurability > 0)
            {
                Damaged?.Invoke(this);
                return true;
            }

            resolved = true;
            float reward = upgradeSystem != null
                ? upgradeSystem.CalculateLuckyBlockReward(variant.MoneyReward)
                : Mathf.Max(0, variant.MoneyReward);
            wallet?.AddMoney(reward);
            RewardGranted?.Invoke(this, reward);
            Broken?.Invoke(this);
            return true;
        }

        public Vector3 GetWorldTopCenter()
        {
            Collider targetCollider = GetComponent<Collider>();
            return targetCollider != null
                ? new Vector3(targetCollider.bounds.center.x, targetCollider.bounds.max.y,
                    targetCollider.bounds.center.z)
                : transform.position;
        }

        private void Update()
        {
            if (resolved || settings == null)
            {
                return;
            }

            lifetime += Time.deltaTime;
            if (lifetime >= settings.MaximumLifetimeSeconds ||
                transform.position.y <= settings.RecycleBelowWorldY)
            {
                resolved = true;
                Expired?.Invoke(this);
                return;
            }

        }

        private void OnDisable()
        {
            reservedMiners.Clear();
            hitPunch?.ResetImmediately();

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.Sleep();
            }
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
    }
}
