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
        [Tooltip("A falling block crushes the player, but continues down to a real landing surface.")]
        [SerializeField] private bool fatalPlayerCrush = true;
        private readonly List<Collider> ignoredActors = new();
        private readonly RaycastHit[] fallingHits = new RaycastHit[32];
        private readonly Collider[] fallingOverlaps = new Collider[32];
        private BoxCollider fallingCollider;
        private void OnEnable() { if (Application.isPlaying) WorldNavigationObstacle.Ensure(this); }

        public LuckyBlockType Type => type;
        public LuckyBlockVariantData Variant => variant;
        public int CurrentDurability => currentDurability;
        public int MaximumDurability => variant != null ? variant.Durability : 0;
        public LuckyBlockData Settings => settings;
        public bool IsResolved => resolved;
        public bool LastDamageWasNpc { get; private set; }
        public event Action<LuckyBlock> Damaged;
        public event Action<int, int> DurabilityChanged;
        public event Action<LuckyBlock, float> RewardGranted;
        public event Action<LuckyBlock> Broken;
        public event Action<LuckyBlock> Expired;

        public float TimeRemainingSeconds => settings != null
            ? Mathf.Max(0f, settings.MaximumLifetimeSeconds - lifetime)
            : 0f;








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
            RestoreActorCollisions();
            damageRemainder = 0f;
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
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.useGravity = true;
            body.WakeUp();
            DurabilityChanged?.Invoke(currentDurability, MaximumDurability);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (resolved || hasLanded || collision.collider == null)
            {
                return;
            }

            if (HandleActorContact(collision.collider)) return;
            bool supportingSurface = false;
            for (int i = 0; i < collision.contactCount; i++)
                supportingSurface |= collision.GetContact(i).normal.y > .5f;
            if (!supportingSurface) return; // A wall is not a floor.
            hasLanded = true;
            body ??= GetComponent<Rigidbody>();
            if (body == null) return;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        private void FixedUpdate()
        {
            if (resolved || hasLanded || settings == null || body == null || body.isKinematic || body.linearVelocity.y > 0f) return;
            fallingCollider ??= GetComponent<BoxCollider>();
            Vector3 scale = transform.lossyScale;
            Vector3 half = Vector3.Scale(fallingCollider.size * .5f,
                new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            Vector3 center = transform.TransformPoint(fallingCollider.center);
            // Catch an actor before the physics solver can support the block on its head.
            int count = Physics.OverlapBoxNonAlloc(center, half, fallingOverlaps,
                transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++) HandleActorContact(fallingOverlaps[i]);
            Vector3 travel = (body.linearVelocity + Physics.gravity * Time.fixedDeltaTime) * Time.fixedDeltaTime;
            if (travel.sqrMagnitude < .000001f) return;
            count = Physics.BoxCastNonAlloc(center, half, travel.normalized, fallingHits,
                transform.rotation, travel.magnitude, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++) HandleActorContact(fallingHits[i].collider);
        }

        private bool HandleActorContact(Collider contact)
        {
            var actorHealth = contact.GetComponentInParent<MiningCharacterHealth>();
            if (actorHealth != null)
            {
                if (fatalPlayerCrush && actorHealth != null &&
                    actorHealth.GetComponent<StarterAssets.ThirdPersonController>() != null)
                    actorHealth.DealDamage(actorHealth.Health, CombatDamageType.True);
                // Ignore all colliders on this actor, including a corpse after death.
                Transform actor = actorHealth.transform;
                var blockCollider = GetComponent<BoxCollider>();
                foreach (var collider in actor.GetComponentsInChildren<Collider>())
                {
                    if (collider == blockCollider || ignoredActors.Contains(collider) ||
                        Physics.GetIgnoreCollision(blockCollider, collider)) continue;
                    Physics.IgnoreCollision(blockCollider, collider, true);
                    ignoredActors.Add(collider);
                }
                return true;
            }
            return false;
        }

        public bool MineOnce()
        {
            return variant != null && ApplyDamage(variant.ClickDamage);
        }

        public bool ApplyDamage(int damage)
        {
            return ApplyPlayerDamage(damage);
        }

        public bool ApplyPlayerDamage(float damage)
        {
            if (resolved || variant == null || damage <= 0)
            {
                return false;
            }

            return ApplyExactDamage(damage, false);
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
            RestoreActorCollisions();
            hitPunch?.ResetImmediately();

            if (body != null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.Sleep();
            }
        }

        private void RestoreActorCollisions()
        {
            var collider = GetComponent<BoxCollider>();
            foreach (var actor in ignoredActors)
                if (actor != null && collider != null) Physics.IgnoreCollision(collider, actor, false);
            ignoredActors.Clear();
        }


    }
}
