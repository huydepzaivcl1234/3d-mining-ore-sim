using System;
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
        private Rigidbody body;
        private Transform visualRoot;
        private Vector3 baseVisualScale = Vector3.one;
        private float lifetime;
        private float punchElapsed;
        private bool punchPlaying;
        private bool resolved;

        public LuckyBlockType Type => type;
        public int CurrentDurability => currentDurability;
        public int MaximumDurability => variant != null ? variant.Durability : 0;
        public LuckyBlockData Settings => settings;
        public bool IsResolved => resolved;
        public event Action<LuckyBlock> Damaged;
        public event Action<int, int> DurabilityChanged;
        public event Action<LuckyBlock, int> RewardGranted;
        public event Action<LuckyBlock> Broken;
        public event Action<LuckyBlock> Expired;

        public void Initialize(LuckyBlockVariantData targetVariant, LuckyBlockData targetSettings,
            PlayerWallet targetWallet, Transform targetVisualRoot, float spinDegreesPerSecond)
        {
            variant = targetVariant;
            settings = targetSettings;
            wallet = targetWallet;
            visualRoot = targetVisualRoot;
            type = variant.Type;
            currentDurability = Mathf.Max(1, variant.Durability);
            resolved = false;
            lifetime = 0f;
            punchElapsed = 0f;
            punchPlaying = false;
            body ??= GetComponent<Rigidbody>();
            if (visualRoot != null)
            {
                baseVisualScale = visualRoot.localScale;
                visualRoot.localScale = baseVisualScale;
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

            currentDurability = Mathf.Max(0, currentDurability - damage);
            DurabilityChanged?.Invoke(currentDurability, MaximumDurability);
            if (currentDurability > 0)
            {
                punchElapsed = 0f;
                punchPlaying = true;
                Damaged?.Invoke(this);
                return true;
            }

            resolved = true;
            int reward = Mathf.Max(0, variant.MoneyReward);
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

            UpdateHitPunch();
        }

        private void UpdateHitPunch()
        {
            if (!punchPlaying || visualRoot == null)
            {
                return;
            }

            punchElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(punchElapsed / settings.HitPunchDuration);
            float punch = Mathf.Sin(progress * Mathf.PI) * settings.HitPunchScale;
            visualRoot.localScale = baseVisualScale * (1f + punch);
            if (progress >= 1f)
            {
                visualRoot.localScale = baseVisualScale;
                punchPlaying = false;
            }
        }

        private void OnDisable()
        {
            if (visualRoot != null)
            {
                visualRoot.localScale = baseVisualScale;
            }

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.Sleep();
            }
        }
    }
}
