using UnityEngine;
namespace MiningSimulator.Ores
{
    public sealed class PotionMonsterProjectile : MonoBehaviour,IPooledProjectile
    {
        private MushroomMonster owner;
        private CharacterHealth victim;
        private PotionMonsterData data;
        private Vector3 start;
        private float age;
        private float yellowDamage;
        private int kind;
        private bool resolved;
        public void Launch(MushroomMonster attacker,CharacterHealth target,int potion){owner=attacker;victim=target;data=attacker.SpeciesData as PotionMonsterData;start=transform.position;age=0;kind=potion;yellowDamage=data?data.YellowDamage(attacker.Level):0;resolved=false;PotionMonsterAbility.FitPotion(gameObject,data?data.potionSize:.24f);} 
        private void Update(){if(!RuneStation.PlayerUsesRuneTime&&Step(Time.deltaTime))SceneProjectilePool.Release(gameObject);}
        public bool Step(float seconds){
            if(resolved||!data||!victim||!victim.IsTargetable)return true;
            age+=Mathf.Max(0,seconds);float t=Mathf.Clamp01(age/Mathf.Max(.1f,data.flightSeconds));
            var collider=victim.GetComponent<Collider>();var end=collider&&collider.enabled?collider.bounds.center:victim.transform.position+Vector3.up*.5f;
            transform.position=Vector3.Lerp(start,end,t)+Vector3.up*(4*data.arcHeight*t*(1-t));transform.Rotate(Vector3.right,360*seconds,Space.Self);
            if(t<1)return false;resolved=true;
            if(kind==0)victim.Heal(data.healingFlat+victim.MaxHealth*data.healingMaxHealthPercent*.01f);
            else if(!victim.IsMonster){var effect=victim.GetComponent<PotionPlayerEffect>();if(!effect)effect=victim.gameObject.AddComponent<PotionPlayerEffect>();if(kind==1)effect.ApplyRed(data,owner?owner.gameObject:null);else effect.ApplyYellow(data,owner?owner.Level:1,owner?owner.gameObject:null,yellowDamage);}
            return true;
        }
        public void ResetForPool(){owner=null;victim=null;data=null;age=0;resolved=false;}
        private void OnDisable()=>ResetForPool();
    }
}
