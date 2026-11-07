using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Swept, one-hit projectile. Mushranon shots keep their firing direction; other species may steer.</summary>
    public sealed class HomingMonsterProjectile : MonoBehaviour, IPooledProjectile
    {
        [SerializeField, Min(.1f)] [HideInInspector, UnityEngine.Serialization.FormerlySerializedAs("speed")] private float legacy_speed = 8f;
        private float speed => data != null ? data.projectileSpeed : legacy_speed;
        [SerializeField, Min(0f)] [HideInInspector, UnityEngine.Serialization.FormerlySerializedAs("turnDegreesPerSecond")] private float legacy_turnDegreesPerSecond = 540f;
        private float turnDegreesPerSecond => data != null ? data.projectileTurnDegreesPerSecond : legacy_turnDegreesPerSecond;
        [SerializeField, Min(.01f)] [HideInInspector, UnityEngine.Serialization.FormerlySerializedAs("radius")] private float legacy_radius = .12f;
        private float radius => data != null ? data.projectileRadius : legacy_radius;
        [SerializeField, Min(.1f)] [HideInInspector, UnityEngine.Serialization.FormerlySerializedAs("lifetime")] private float legacy_lifetime = 8f;
        private float lifetime => data != null ? data.projectileLifetime : legacy_lifetime;
        [SerializeField] [HideInInspector, UnityEngine.Serialization.FormerlySerializedAs("collisionLayers")] private LayerMask legacy_collisionLayers = ~0;
        private LayerMask collisionLayers => data != null ? data.projectileCollisionLayers : legacy_collisionLayers;
        private MonsterCombatSettings data;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private MushroomMonster owner;
        private Transform victim;
        private CharacterHealth victimHealth;
        private float age,travelled;
        private bool straightFlight;
        private float travelLimit;
        private bool launched, resolved;

        public static Vector3 AimPoint(Transform target)
        {
            var collider = target.GetComponent<Collider>();
            return collider != null && collider.enabled ? collider.bounds.center : target.position;
        }
        public void Launch(MushroomMonster attacker, Transform target)
        {
            owner = attacker;
            data = attacker != null ? attacker.CombatData : null;
            victim = target;
            victimHealth = target != null ? target.GetComponent<CharacterHealth>() : null;
            age = travelled = 0f;
            straightFlight=attacker && attacker.SpeciesData is MushranonData;
            travelLimit=straightFlight?Mathf.Max(.1f,((MushranonData)attacker.SpeciesData).projectileTravelDistance):float.PositiveInfinity;
            resolved = false;
            launched = true;
        }
        private void Update()
        {
            if (RuneStation.PlayerUsesRuneTime) return;
            if (Step(Time.deltaTime)) SceneProjectilePool.Release(gameObject);
        }
        private void OnDisable() => ResetForPool();
        public void ResetForPool()
        {
            owner=null;victim=null;victimHealth=null;data=null;age=travelled=0;straightFlight=false;travelLimit=0;launched=resolved=false;
            System.Array.Clear(hits,0,hits.Length);
        }
        // Separate the simulation step from destruction for deterministic validation.
        private bool Step(float seconds)
        {
            if (!launched) return false;
            if (resolved) return true;
            age += seconds;
            if (owner == null || owner.IsDespawning || age >= lifetime ||
                !straightFlight && (victim == null || victimHealth != null && victimHealth.Health <= 0f))
            { Resolve(false); return true; }
            Vector3 aim = !straightFlight && victim ? AimPoint(victim) - transform.position : Vector3.zero;
            if (aim.sqrMagnitude > .001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                    Quaternion.LookRotation(aim), turnDegreesPerSecond * seconds);
            float distance = Mathf.Min(Mathf.Max(0,speed * seconds),Mathf.Max(0,travelLimit-travelled));
            int count = Physics.SphereCastNonAlloc(transform.position, radius, transform.forward,
                hits, distance, collisionLayers, QueryTriggerInteraction.Ignore);
            RaycastHit nearest = default;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Transform obstacle = hits[i].transform;
                if (obstacle.IsChildOf(owner.transform) || obstacle.IsChildOf(transform)) continue;
                if (hits[i].distance < nearestDistance)
                { nearest = hits[i]; nearestDistance = hits[i].distance; }
            }
            if (nearest.collider != null)
            {
                transform.position += transform.forward * nearestDistance;
                Resolve(victim && nearest.transform.IsChildOf(victim));
                return true;
            }
            if (count == hits.Length) { Resolve(false); return true; }
            var targetCollider = victim ? victim.GetComponent<Collider>() : null;
            if (targetCollider != null && targetCollider.enabled &&
                (targetCollider.ClosestPoint(transform.position) - transform.position).sqrMagnitude <= radius * radius)
            { Resolve(true); return true; }
            transform.position += transform.forward * distance;
            travelled+=distance;
            if(straightFlight&&travelled>=travelLimit){Resolve(false);return true;}
            return false;
        }
        private void Resolve(bool impact)
        {
            if (resolved) return;
            resolved = true;
            if (impact && owner != null) owner.ApplyProjectileHit(victimHealth);
        }
    }
}
