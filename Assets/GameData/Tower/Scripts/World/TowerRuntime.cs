using System.Collections.Generic;
using UnityEngine;
namespace MiningSimulator.Ores
{
    [RequireComponent(typeof(MiningCharacterHealth), typeof(BoxCollider))]
    public sealed class TowerRuntime : MonoBehaviour, IMiningInteractable
    {
        public static readonly HashSet<TowerRuntime> Active = new();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => Active.Clear();
        [SerializeField] private TowerData data;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Animator animator;
        [SerializeField] private float paidPrice;
        private MiningCharacterHealth health;
        private float nextScan, nextShot, releaseAt;
        private MiningCharacterHealth target, committedTarget;
        public TowerData Data => data;
        public MiningCharacterHealth Health => health;
        public float PaidPrice => paidPrice;
        public int Level { get; private set; }
        public float Damage => data.Scaled(data.damage, data.damageGrowth, Level);
        public float Range => data.Scaled(data.range, data.rangeGrowth, Level);
        public float AttackSpeed => data.Scaled(data.attackSpeed, data.attackSpeedGrowth, Level);
        public float MaximumHealth => data.Scaled(data.health, data.healthGrowth, Level);
        public float Armor => data.armor + data.armorPerLevel * (Level - Mathf.Max(1, data.level));
        public float MagicResistance => data.magicResistance + data.magicResistancePerLevel * (Level - Mathf.Max(1, data.level));
        public bool CanUpgrade => data != null && Level < Mathf.Max(data.level, data.maximumLevel);
        public float UpgradeCost => CanUpgrade ? data.UpgradeCost(Level) : 0;
        public bool IsAlive => isActiveAndEnabled && health != null && health.Health > 0;
        private TowerStatsPanel statsPanel;
        private Renderer foundation;
        private Transform visualMount;
        private Vector3 foundationFoot;
        private bool hasFoundationFoot;
        private MiningPlayerStats collisionPlayer;
        private float nextPlayerCollisionCheck;
        private readonly List<Collider> towerColliders = new();
        private readonly List<Collider> playerColliders = new();
        public string PlacementId { get; private set; }
        public void MarkDeployed(string id) => PlacementId = id;
        public string InteractionLabel => $"{data?.displayName} • Stats";
        public bool CanInteract => IsAlive;
        public void SetInteractionFocused(bool focused) { }
        public void Interact()
        {
            var player = FindFirstObjectByType<MiningPlayerStats>();
            if (!IsAlive || player == null) return;
            statsPanel ??= GetComponent<TowerStatsPanel>();
            if (statsPanel == null) statsPanel = gameObject.AddComponent<TowerStatsPanel>();
            statsPanel.Bind(this);statsPanel.Show(player.transform);
        }
        public void Sell()
        {
            var wallet = FindFirstObjectByType<PlayerWallet>();
            if (!IsAlive || wallet == null) return;
            var player = FindFirstObjectByType<MiningPlayerStats>();
            if (player == null || Vector3.Distance(player.transform.position, transform.position) > 4f) return;
            TowerWorldSave.Remove(this);
            wallet.AddMoney(paidPrice * .35f);
            gameObject.SetActive(false); Destroy(gameObject);
        }
        public void Initialize(TowerData definition, float purchasedPrice, int savedLevel = 0)
        {
            data = definition; paidPrice = Mathf.Max(0, purchasedPrice);
            Level = data.ClampLevel(savedLevel);
            health ??= GetComponent<MiningCharacterHealth>();
            health.ConfigureSpawnHealth(MaximumHealth);
            health.ConfigureDefenses(Armor, MagicResistance);
            health.ConfigureRegeneration(0, 5);
        }
        public bool TryUpgrade(PlayerWallet wallet, Transform player)
        {
            if (!IsAlive || !CanUpgrade || wallet == null || player == null ||
                Vector3.Distance(player.position, transform.position) > 4f ||
                player.GetComponent<MiningCharacterHealth>()?.Health <= 0) return false;
            float cost = UpgradeCost;
            if (float.IsNaN(cost) || float.IsInfinity(cost) || cost < 0 || !wallet.TrySpend(cost)) return false;
            Level++;
            // Grant only the added HP capacity, not a free full repair.
            health.ConfigureMaximumHealth(MaximumHealth, true);
            health.ConfigureDefenses(Armor, MagicResistance);
            if (!string.IsNullOrEmpty(PlacementId)) { TowerWorldSave.Capture(this); TowerWorldSave.Flush(); }
            return true;
        }
        private void Awake()
        {
            health = GetComponent<MiningCharacterHealth>();
            if (data != null) Initialize(data, paidPrice);
            animator ??= GetComponentInChildren<Animator>();
            if (Application.isPlaying) WorldNavigationObstacle.Ensure(this);
            foreach (var shape in GetComponentsInChildren<Renderer>())
                if (shape.name == "BaseMesh") { foundation=shape;break; }
            if (animator != null) visualMount=animator.transform.parent!=transform?animator.transform.parent:animator.transform;
            var mesh = foundation != null ? foundation.GetComponent<MeshFilter>()?.sharedMesh : null;
            if (mesh != null && mesh.isReadable && visualMount != null)
            {
                float bottom = float.PositiveInfinity;
                foreach (var vertex in mesh.vertices)
                {
                    float y = foundation.transform.TransformPoint(vertex).y;
                    if (y < bottom) { bottom = y; foundationFoot = vertex; }
                }
                hasFoundationFoot = bottom < float.PositiveInfinity;
            }
            AlignToGround();
            if(Application.isPlaying)
                (GetComponent<TowerDamageHealthBar>()??gameObject.AddComponent<TowerDamageHealthBar>()).Bind(this);
        }
        // The animated FBX must never lift the stationary foundation off its placement plane.
        public void AlignToGround()
        {
            // Renderer.bounds is a transformed AABB: its corners need not be on the mesh.
            // Cache a real foot vertex once; yaw/recoil must not accumulate a false lift.
            if(hasFoundationFoot && foundation!=null && visualMount!=null)
                visualMount.position+=Vector3.up*(transform.position.y-foundation.transform.TransformPoint(foundationFoot).y);
        }
        private void LateUpdate()=>AlignToGround();
        private void OnEnable()
        {
            Active.Add(this); health.Died += OnDeath; health.Damaged += SaveDamage;
            // Distribute scans/collision refreshes instead of waking every cannon on the same frame.
            float phase = (GetEntityId().GetHashCode() & 1023) / 1024f;
            nextScan = Time.time + phase * .2f;
            if (Application.isPlaying) RefreshPlayerCollision();
            nextPlayerCollisionCheck = Time.unscaledTime + phase * .5f;
        }
        private void RefreshPlayerCollision()
        {
            nextPlayerCollisionCheck = Time.unscaledTime + .5f;
            if (collisionPlayer == null || !collisionPlayer.gameObject.activeInHierarchy)
                collisionPlayer = FindFirstObjectByType<MiningPlayerStats>() ?? FindFirstObjectByType<MiningPlayerStats>(FindObjectsInactive.Include);
            if (collisionPlayer == null) return;
            GetComponentsInChildren(true, towerColliders);
            collisionPlayer.GetComponentsInChildren(true, playerColliders);
            // Per-pair suppression preserves solid tower geometry for monsters,
            // targeting, projectiles and placement validation. Reapply after respawn
            // or ragdoll colliders are enabled; do not change the global layer matrix.
            foreach (var towerCollider in towerColliders)
                foreach (var playerCollider in playerColliders)
                    if (towerCollider != null && playerCollider != null && towerCollider != playerCollider &&
                        towerCollider.enabled && playerCollider.enabled &&
                        towerCollider.gameObject.activeInHierarchy && playerCollider.gameObject.activeInHierarchy)
                        Physics.IgnoreCollision(towerCollider, playerCollider, true);
        }
        private void SaveDamage() => TowerWorldSave.Capture(this);
        private void OnDisable() { TowerWorldSave.Capture(this); Active.Remove(this); if (health != null) { health.Died -= OnDeath; health.Damaged -= SaveDamage; } }
        private void OnDeath() { TowerWorldSave.Remove(this); gameObject.SetActive(false); Destroy(gameObject); }
        private void Update()
        {
            if (Time.unscaledTime >= nextPlayerCollisionCheck) RefreshPlayerCollision();
            if (!IsAlive || data == null || RuneStation.PlayerUsesRuneTime) return;
            if (committedTarget != null && Time.time >= releaseAt)
            {
                var victim = committedTarget; committedTarget = null;
                if (victim.Health > 0 && victim.gameObject.activeInHierarchy) Fire(victim);
            }
            if (Time.time >= nextScan)
            {
                nextScan = Time.time + .2f; target = null;
                float best = Range * Range;
                foreach (var monster in MushroomMonster.Monsters)
                {
                    if (monster == null || monster.Health.Health <= 0 || !monster.isActiveAndEnabled) continue;
                    float d = (monster.transform.position - transform.position).sqrMagnitude;
                    if (d < best) { best = d; target = monster.Health; }
                }
            }
            if (target == null || target.Health <= 0 || committedTarget != null || Time.time < nextShot) return;
            Vector3 toward = target.transform.position - transform.position; toward.y = 0;
            if (toward.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(toward);
            nextShot = Time.time + 1f / Mathf.Max(.01f, AttackSpeed);
            if (data is CannonTowerData cannon)
            {
                animator?.SetTrigger(cannon.fireTrigger);
                releaseAt = Time.time + cannon.shotDelay;
            }
            else releaseAt = Time.time;
            committedTarget = target;
        }
        private void Fire(MiningCharacterHealth victim)
        {
            Vector3 start = muzzle != null ? muzzle.position : transform.position + Vector3.up;
            Vector3 aim = victim.GetComponent<Collider>() != null ? victim.GetComponent<Collider>().bounds.center : victim.transform.position + Vector3.up*.5f;
            var ball = SceneProjectilePool.Rent<TowerProjectile>(data.projectile,start,Quaternion.identity,gameObject.scene,data.projectilePoolCapacity);
            ball.Initialize(this, aim - start, data);
            ball.gameObject.SetActive(true);
        }
    }
}
