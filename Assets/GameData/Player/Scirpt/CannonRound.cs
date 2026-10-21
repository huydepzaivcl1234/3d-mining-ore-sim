using MiningSimulator.Ores;
using UnityEngine;
namespace GameData.Player.Script
{
    public sealed class CannonRound : MonoBehaviour
    {
        private MiningCharacterHealth target;private GameObject source;private CannonUpgradeData data;
        private Collider shape;
        private float damage,age;private bool finished;
        public void Configure(MiningCharacterHealth victim,GameObject owner,CannonUpgradeData settings,float amount)
        {target=victim;source=owner;data=settings;damage=amount;shape=victim?victim.GetComponent<Collider>():null;}
        private void Update()
        {
            if(!target||!target.isActiveAndEnabled||target.Health<=0||!source){Destroy(gameObject);return;}
            float dt=Time.deltaTime;if(dt<=0)return;age+=dt;
            Vector3 end=shape?shape.bounds.center:target.transform.position+Vector3.up;
            transform.position=Vector3.MoveTowards(transform.position,end,Mathf.Max(.1f,data.projectileSpeed)*dt);
            if((transform.position-end).sqrMagnitude<=.01f||age>=data.flightLimit)Finish();
        }
        public void Finish()
        {
            if(finished)return;finished=true;
            if(target&&target.isActiveAndEnabled&&target.Health>0&&source)target.DealDamage(damage,CombatDamageType.Physical,source);
            gameObject.SetActive(false);Destroy(gameObject);
        }
    }
}
