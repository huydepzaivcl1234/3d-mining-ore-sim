using UnityEngine;
namespace MiningSimulator.Ores
{
    public sealed class TowerProjectile : MonoBehaviour
    {
        private TowerRuntime owner;
        private Vector3 velocity;
        private float damage, remaining;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        public void Initialize(TowerRuntime source, Vector3 heading, TowerData data)
        { owner=source; velocity=heading.normalized*data.projectileSpeed;damage=data.damage;remaining=data.range/data.projectileSpeed+1; }
        private void Update()
        {
            if (RuneStation.PlayerUsesRuneTime) return;
            remaining -= Time.deltaTime;
            if (remaining <= 0) { Destroy(gameObject);return; }
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
                var monster=closest.collider.GetComponentInParent<MushroomMonster>();
                if(monster!=null)monster.Health.DealDamage(damage,CombatDamageType.Physical,owner!=null?owner.gameObject:null);
                Destroy(gameObject);return;
            }
            transform.position+=step;
        }
    }
}
