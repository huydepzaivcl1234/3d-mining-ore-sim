using UnityEngine;

namespace MiningSimulator.Ores
{
    [RequireComponent(typeof(MiningCharacterHealth), typeof(CharacterController))]
    public sealed class MushroomMonster : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [Min(0f), SerializeField] private float moveSpeed = 1.5f;
        [Min(0f), SerializeField] private float detectionRange = 6f;
        [Min(0.1f), SerializeField] private float attackRange = 1.5f;
        [Min(0f), SerializeField] private float damage = 5f;
        [Min(0.1f), SerializeField] private float attackCooldown = 2f;
        [Range(0f, 1f), SerializeField] private float hitMoment = 0.45f;
        [Min(0f), SerializeField] private float deathDelay = 3f;
        [SerializeField] private MonsterRewardData rewards;
        private bool rewardsGranted;
        private CharacterController motor;
        private MiningCharacterHealth health;
        private MiningCharacterHealth target;
        private MonsterSpawnZone zone;
        private Vector3 destination;
        private float nextDecision, nextAttack, verticalSpeed;
        private bool hitApplied;
        private int animationState;
        public MiningCharacterHealth Health => health;
        public int Level { get; private set; } = 1;
        private float scaledDamage;
        public void AlignToGround(Vector3 ground)
        {
            // A model's visual pivot need not match the CharacterController bottom.
            motor.enabled = false;
            float scaleY = Mathf.Abs(transform.lossyScale.y);
            float bottomOffset = (motor.center.y - motor.height * 0.5f) * scaleY;
            transform.position = new Vector3(transform.position.x, ground.y - bottomOffset, transform.position.z);
            if (animator != null && animator.transform != transform)
            {
                animator.Update(0f);
                float feetY = float.MaxValue;
                var baked = new Mesh();
                foreach (var renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (renderer.sharedMesh == null) continue;
                    renderer.BakeMesh(baked);
                    foreach (var vertex in baked.vertices)
                        feetY = Mathf.Min(feetY, renderer.transform.TransformPoint(vertex).y);
                    baked.Clear();
                }
                Destroy(baked);
                if (feetY != float.MaxValue)
                    animator.transform.position += Vector3.up * (ground.y - feetY);
            }
            verticalSpeed = -2f;
            motor.enabled = true;
        }
        public void Initialize(MonsterSpawnZone owner, MiningCharacterHealth player)
        {
            zone = owner;
            target = player;
            destination = transform.position;
            Level = rewards != null ? rewards.RollLevel(Random.value) : 1;
            float scale = rewards != null ? rewards.StatMultiplier(Level) : 1;
            float low = rewards != null ? Mathf.Max(0.01f, Mathf.Min(rewards.randomStatMultiplier.x, rewards.randomStatMultiplier.y)) : 1;
            float high = rewards != null ? Mathf.Max(low, Mathf.Max(rewards.randomStatMultiplier.x, rewards.randomStatMultiplier.y)) : 1;
            scaledDamage = damage * scale * Random.Range(low, high);
            health.ConfigureSpawnHealth(health.MaxHealth * scale * Random.Range(low, high));
        }
        private void Awake()
        {
            health = GetComponent<MiningCharacterHealth>();
            motor = GetComponent<CharacterController>();
            scaledDamage = damage;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            health.Damaged += OnDamage;
            health.Died += OnDeath;
        }
        private void OnDestroy()
        {
            if (health == null) return;
            health.Damaged -= OnDamage;
            health.Died -= OnDeath;
        }
        private void OnDamage() { if (health.Health > 0f) Play("Damage"); }
        private void OnDeath()
        {
            if (rewardsGranted) return;
            rewardsGranted = true;
            Play("Down");
            motor.enabled = false;
            if (zone != null) zone.GrantRewards(rewards, transform.position + Vector3.up * 0.6f, rewards != null ? rewards.GoldMultiplier(Level) : 1);
            Destroy(gameObject, deathDelay);
        }
        private void Play(string name)
        {
            int hash = Animator.StringToHash(name);
            if (animationState == hash || animator == null) return;
            animationState = hash;
            animator.CrossFadeInFixedTime(hash, 0.15f);
        }
        private void Update()
        {
            if (health.Health <= 0f || animator == null) return;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            bool attacking = state.IsName("Headbutt");
            bool reacting = state.IsName("Damage");
            if (attacking && !hitApplied && state.normalizedTime >= hitMoment)
            {
                hitApplied = true;
                if (target != null && target.Health > 0f &&
                    Vector3.Distance(transform.position, target.transform.position) <= attackRange + 0.3f)
                    target.ApplyDamage(scaledDamage);
            }
            Vector3 movement = Vector3.zero;
            if (!animator.IsInTransition(0) && ((!attacking && !reacting) || state.normalizedTime >= 1f))
            {
                bool chasing = target != null && target.Health > 0f &&
                    (zone == null || zone.Contains(target.transform.position)) &&
                    Vector3.Distance(transform.position, target.transform.position) <= detectionRange;
                if (chasing) destination = target.transform.position;
                else if (zone != null && Time.time >= nextDecision)
                {
                    destination = zone.RandomPoint();
                    nextDecision = Time.time + Random.Range(3f, 6f);
                }
                Vector3 delta = destination - transform.position;
                delta.y = 0f;
                if (chasing && delta.magnitude <= attackRange && Time.time >= nextAttack)
                {
                    nextAttack = Time.time + attackCooldown;
                    hitApplied = false;
                    Play("Headbutt");
                }
                else if (delta.magnitude > (chasing ? attackRange : 0.3f))
                {
                    movement = delta.normalized * moveSpeed;
                    if (zone != null && !zone.Contains(transform.position + movement * Time.deltaTime)) movement = Vector3.zero;
                    Play(movement.sqrMagnitude > 0f ? "Walk" : "Idle");
                }
                else Play("Idle");
                if (delta.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(delta), 240f * Time.deltaTime);
            }
            verticalSpeed = motor.isGrounded ? -2f : verticalSpeed + Physics.gravity.y * Time.deltaTime;
            motor.Move((movement + Vector3.up * verticalSpeed) * Time.deltaTime);
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
