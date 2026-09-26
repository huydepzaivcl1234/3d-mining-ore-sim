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
        private CharacterController motor;
        private MiningCharacterHealth health;
        private MiningCharacterHealth target;
        private MonsterSpawnZone zone;
        private Vector3 destination;
        private float nextDecision, nextAttack, verticalSpeed;
        private bool hitApplied;
        private int animationState;
        public MiningCharacterHealth Health => health;
        public void Initialize(MonsterSpawnZone owner, MiningCharacterHealth player)
        {
            zone = owner;
            target = player;
            destination = transform.position;
        }
        private void Awake()
        {
            health = GetComponent<MiningCharacterHealth>();
            motor = GetComponent<CharacterController>();
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
            Play("Down");
            motor.enabled = false;
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
                    target.ApplyDamage(damage);
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
