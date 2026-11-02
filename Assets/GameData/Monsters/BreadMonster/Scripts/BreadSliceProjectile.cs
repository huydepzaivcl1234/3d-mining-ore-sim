using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed class BreadSliceProjectile : MonoBehaviour,IPooledProjectile
    {
        [SerializeField] private Transform visual;
        private MushroomMonster owner;
        private CharacterHealth victim;
        private BreadMonsterData data;
        private Vector3 start,end,side;
        private float age,healing;
        private bool healingFlight;
        private bool resolved;
        private readonly RaycastHit[] hits=new RaycastHit[32];
        public Vector3 EndPoint=>end;
        public Vector3 Point(float progress)=>Vector3.Lerp(start,end,progress)+Vector3.up*(4*data.arcHeight*progress*(1-progress))+side*(Mathf.Sin(Mathf.PI*progress)*data.arcSideways);
        public void Launch(MushroomMonster attacker,Transform target){
            healingFlight=false;healing=0;owner=attacker;data=attacker.SpeciesData as BreadMonsterData;victim=target?target.GetComponent<CharacterHealth>():null;
            start=transform.position;
            var collider=target?target.GetComponent<Collider>():null;
            end=collider&&collider.enabled?collider.bounds.center:target?target.position:start;
            side=Vector3.Cross(Vector3.up,(end-start).normalized).normalized;age=0;resolved=false;
        }
        public void LaunchHealing(MushroomMonster healer,CharacterHealth ally,float amount){
            Launch(healer,ally.transform);healingFlight=true;healing=Mathf.Max(0,amount);
        }
        private void Update(){if(!RuneStation.PlayerUsesRuneTime&&Step(Time.deltaTime))SceneProjectilePool.Release(gameObject);}
        public bool Step(float seconds){
            if(resolved)return true;
            // Subdivide the curved sweep so low frame rates cannot cut through its apex.
            seconds=Mathf.Max(0,seconds);
            while(seconds>.04f){if(Advance(.04f)){resolved=true;return true;}seconds-=.04f;}
            resolved=Advance(seconds);return resolved;
        }
        private bool Advance(float seconds){
            if(!data||!owner||owner.IsDespawning||!victim||victim.Health<=0)return true;
            var targetCollider=victim.GetComponent<Collider>();
            end=targetCollider&&targetCollider.enabled?targetCollider.bounds.center:victim.transform.position;
            age+=Mathf.Max(0,seconds);float t=Mathf.Clamp01(age/Mathf.Max(.1f,healingFlight?data.healingFlightSeconds:data.flightSeconds));
            Vector3 next=Point(t),delta=next-transform.position;
            if(healingFlight){transform.position=next;if(visual)visual.Rotate(Vector3.right,data.spinDegreesPerSecond*seconds,Space.Self);if(t>=1){victim.Heal(healing);return true;}return false;}
            int count=delta.sqrMagnitude>.000001f?Physics.SphereCastNonAlloc(transform.position,data.combat.projectileRadius,delta.normalized,hits,delta.magnitude,data.combat.projectileCollisionLayers,QueryTriggerInteraction.Ignore):0;
            RaycastHit nearest=default;float distance=float.PositiveInfinity;
            for(int i=0;i<count;i++){
                var tr=hits[i].transform;
                if(tr.IsChildOf(owner.transform)||tr.IsChildOf(transform))continue;
                var health=tr.GetComponentInParent<CharacterHealth>();
                if(health&&health.IsMonster)continue;
                if(hits[i].distance<distance){nearest=hits[i];distance=hits[i].distance;}
            }
            if(nearest.collider){
                transform.position+=delta.normalized*distance;
                if(nearest.transform.GetComponentInParent<CharacterHealth>()==victim)owner.ApplyProjectileHit(victim);
                return true;
            }
            if(count==hits.Length)return true;
            transform.position=next;
            if(visual)visual.Rotate(Vector3.right,data.spinDegreesPerSecond*seconds,Space.Self);
            if(t>=1){var c=victim.GetComponent<Collider>();if(c&&(c.ClosestPoint(next)-next).sqrMagnitude<=data.combat.projectileRadius*data.combat.projectileRadius)owner.ApplyProjectileHit(victim);return true;}
            return false;
        }
        public void ResetForPool(){owner=null;victim=null;data=null;age=healing=0;healingFlight=false;resolved=false;System.Array.Clear(hits,0,hits.Length);if(visual)visual.localRotation=Quaternion.identity;}
        private void OnDisable()=>ResetForPool();
    }
}
