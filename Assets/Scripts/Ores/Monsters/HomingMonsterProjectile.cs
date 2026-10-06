using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Swept, one-hit projectile. Steering follows the chosen living player or miner.</summary>
    public sealed class HomingMonsterProjectile : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float speed = 8f;
        [SerializeField, Min(0f)] private float turnDegreesPerSecond = 540f;
        [SerializeField, Min(.01f)] private float radius = .12f;
        [SerializeField, Min(.1f)] private float lifetime = 8f;
        [SerializeField] private LayerMask collisionLayers = ~0;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private MushroomMonster owner;
        private Transform victim;
        private MiningCharacterHealth victimHealth;
        private MiningNpc victimMiner;
        private float age;
        private bool launched, resolved;

        public static Vector3 AimPoint(Transform target)
        {
            var collider = target.GetComponent<Collider>();
            return collider != null && collider.enabled ? collider.bounds.center : target.position;
        }
        public void Launch(MushroomMonster attacker, Transform target)
        {
            owner = attacker;
            victim = target;
            victimHealth = target != null ? target.GetComponent<MiningCharacterHealth>() : null;
            victimMiner = target != null ? target.GetComponent<MiningNpc>() : null;
            age = 0f;
            resolved = false;
            launched = true;
        }
        private void Update()
        {
            if (RuneStation.PlayerUsesRuneTime) return;
            if (Step(Time.deltaTime)) Destroy(gameObject);
        }
        // Separate the simulation step from destruction for deterministic validation.
        private bool Step(float seconds)
        {
            if (!launched) return false;
            if (resolved) return true;
            age += seconds;
            if (owner == null || owner.IsDespawning || victim == null || age >= lifetime ||
                victimHealth != null && victimHealth.Health <= 0f ||
                victimMiner != null && victimMiner.IsDead)
            { Resolve(false); return true; }
            Vector3 aim = AimPoint(victim) - transform.position;
            if (aim.sqrMagnitude > .001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(aim), turnDegreesPerSecond * seconds);
            float distance = speed * seconds;
            int count = Physics.SphereCastNonAlloc(transform.position, radius, transform.forward,
                hits, distance, collisionLayers, QueryTriggerInteraction.Ignore);
            RaycastHit nearest = default;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Transform obstacle = hits[i].transform;
                if (obstacle.IsChildOf(owner.transform) || obstacle.IsChildOf(transform) ||
                    obstacle.GetComponentInParent<Ore>() != null) continue;
                if (hits[i].distance < nearestDistance)
                { nearest = hits[i]; nearestDistance = hits[i].distance; }
            }
            if (nearest.collider != null)
            {
                transform.position += transform.forward * nearestDistance;
                Resolve(nearest.transform.IsChildOf(victim));
                return true;
            }
            if (count == hits.Length) { Resolve(false); return true; }
            var targetCollider = victim.GetComponent<Collider>();
            if (targetCollider != null && targetCollider.enabled &&
                (targetCollider.ClosestPoint(transform.position) - transform.position).sqrMagnitude <= radius * radius)
            { Resolve(true); return true; }
            transform.position += transform.forward * distance;
            return false;
        }
        private void Resolve(bool impact)
        {
            if (resolved) return;
            resolved = true;
            if (impact && owner != null) owner.ApplyProjectileHit(victimHealth, victimMiner);
        }
    }
}
