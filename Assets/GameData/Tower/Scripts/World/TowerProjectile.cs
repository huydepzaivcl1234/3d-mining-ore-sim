using UnityEngine;
namespace MiningSimulator.Ores
{
    public sealed class TowerProjectile : MonoBehaviour, IPooledProjectile
    {
        private TowerRuntime owner;
        public bool HasPlayerOwner { get; private set; }
        private Vector3 velocity;
        private float damage, remaining;
        private bool launched;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        public void Initialize(TowerRuntime source, Vector3 heading, TowerData data)
        {
            owner = source;
            // Ownership survives the cannon being destroyed while its shot is in flight.
            HasPlayerOwner = source != null;
            velocity=heading.normalized*data.projectileSpeed;
            damage=source!=null?source.Damage:data.damage;
            remaining=(source!=null?source.Range:data.range)/Mathf.Max(.1f,data.projectileSpeed)+1;
            launched = true;
        }
        private void Update()
        {
            if (!launched || RuneStation.PlayerUsesRuneTime) return;
            remaining -= Time.deltaTime;
            if (remaining <= 0) { SceneProjectilePool.Release(gameObject);return; }
            Vector3 step=velocity*Time.deltaTime;
            int count=Physics.SphereCastNonAlloc(transform.position,.085f,velocity.normalized,hits,step.magnitude,~0,QueryTriggerInteraction.Ignore);
            RaycastHit closest=default;float distance=float.PositiveInfinity;
            for(int i=0;i<count;i++)
            {
                var t=hits[i].transform;
                if(t.IsChildOf(transform)||(owner!=null&&t.IsChildOf(owner.transform)))continue;
                if(hits[i].distance<distance){closest=hits[i];distance=hits[i].distance;}
            }
            if(distance<float.PositiveInfinity)
            {
                launched = false;
                var monster=closest.collider.GetComponentInParent<MushroomMonster>();
                if(monster!=null)monster.Health.DealDamage(damage,CombatDamageType.Physical,owner!=null?owner.gameObject:gameObject);
                SceneProjectilePool.Release(gameObject);return;
            }
            transform.position+=step;
        }
        private void OnDisable() => ResetForPool();
        public void ResetForPool()
        {
            launched=false;owner=null;HasPlayerOwner=false;velocity=Vector3.zero;damage=remaining=0;
            System.Array.Clear(hits,0,hits.Length);
        }
    }
}
