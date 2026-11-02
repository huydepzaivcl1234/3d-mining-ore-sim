using System.Collections.Generic;
using UnityEngine;
using MiningSimulator.Ores;

namespace GameData.Player.Script
{
    [DisallowMultipleComponent]
    public sealed class SurvivorAutoCombat : MonoBehaviour
    {
        public SurvivorCombatData data;
        private PlayerStats stats;
        private CharacterHealth health;
        private GunSpecialUpgrades specials;
        private MiningCardSystem cards;
        private MiningUiPanelCoordinator ui;
        private MiningMainMenu menu;
        private readonly List<CharacterHealth> targets=new();
        private readonly RaycastHit[] hits=new RaycastHit[96];
        private readonly List<Bolt> bolts=new();
        private Material material;
        private GameObject pool;
        private float nextAttack;
        public CharacterHealth CurrentTarget {get;private set;}
        public int VolleysFired {get;private set;}
        public int HitsLanded {get;private set;}
        public int ActiveProjectiles {get {int count=0;foreach(var b in bolts)if(b.active)count++;return count;}}
        public float EffectiveDamage=>stats?stats.CalculateSurvivorDamage(data?data.damage:20):data?data.damage:20;
        public float AttackInterval=>Mathf.Max(.08f,(data?data.attackInterval:.65f)/Mathf.Max(.1f,stats?stats.AttackSpeed/Mathf.Max(.1f,stats.Data?stats.Data.attackSpeed:1):1)/Mathf.Max(1,specials?specials.FireRateMultiplier:1));
        public int ProjectileCount=>(int)System.Math.Min((long)(data?data.maximumProjectilesPerVolley:8),1L+(stats?stats.GunCardMagazineBonus:0));
        public float ProjectileSpeed=>(data?data.projectileSpeed:22)*(stats?stats.GunBulletSpeedMultiplier:1);
        public bool CanAttack=>enabled&&data&&health&&health.Health>0&&Time.timeScale>0&&!(cards&&cards.IsChoosing)&&!(ui&&ui.BlocksGameplay)&&!(menu&&menu.IsOpen);
        private sealed class Bolt {public GameObject go;public TrailRenderer trail;public Vector3 direction;public float damage,distance;public bool active;}
        private void Awake()
        {
            stats=GetComponent<PlayerStats>();health=GetComponent<CharacterHealth>();specials=GetComponent<GunSpecialUpgrades>();
            cards=FindAnyObjectByType<MiningCardSystem>();ui=FindAnyObjectByType<MiningUiPanelCoordinator>();menu=FindAnyObjectByType<MiningMainMenu>();
            if(!data){enabled=false;return;}
            pool=new GameObject("Survivor Energy Projectiles");
            var shader=Shader.Find("Universal Render Pipeline/Unlit");
            material=new Material(shader){name="Survivor projectile runtime"};material.SetColor("_BaseColor",data.projectileColor);
        }
        private Vector3 Origin=>transform.position+Vector3.up*1.0f;
        private static Vector3 Point(CharacterHealth target)
        {
            var collider=target.GetComponent<Collider>();return collider?collider.bounds.center:target.transform.position+Vector3.up*.7f;
        }
        private void Update()
        {
            if(!CanAttack){CurrentTarget=null;if(!health||health.Health<=0)ClearBolts();return;}
            StepBolts(Time.deltaTime);
            if(Time.time<nextAttack)return;
            targets.Clear();float range=data.range*data.range;
            foreach(var target in CharacterHealth.ActiveMonsters)
            {
                if(!target||!target.isActiveAndEnabled||!target.IsTargetable||Vector3.SqrMagnitude(target.transform.position-transform.position)>range)continue;
                Vector3 path=Point(target)-Origin;
                if(FirstCollision(Origin,path,path.magnitude,out var hit) && hit.collider.GetComponentInParent<CharacterHealth>()!=target)continue;
                int i=0;float d=(target.transform.position-transform.position).sqrMagnitude;
                while(i<targets.Count && (targets[i].transform.position-transform.position).sqrMagnitude<=d)i++;
                targets.Insert(i,target);if(targets.Count>data.maximumProjectilesPerVolley)targets.RemoveAt(targets.Count-1);
            }
            CurrentTarget=targets.Count>0?targets[0]:null;
            if(!CurrentTarget)return;
            nextAttack=Time.time+AttackInterval;VolleysFired++;
            for(int i=0;i<ProjectileCount;i++)Launch(targets[i%targets.Count],i);
        }
        private bool FirstCollision(Vector3 origin,Vector3 direction,float distance,out RaycastHit closest)
        {
            closest=default;float nearest=distance+.001f;
            int count=Physics.SphereCastNonAlloc(origin,data.projectileRadius,direction.normalized,hits,distance,data.hitLayers,QueryTriggerInteraction.Collide);
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];if(hit.transform.IsChildOf(transform)||MonsterHitbox.IgnoreShot(hit.collider))continue;
                var victim=hit.collider.GetComponentInParent<CharacterHealth>();
                if(hit.collider.isTrigger && (!victim||!victim.IsMonster))continue;
                if(hit.distance<nearest){nearest=hit.distance;closest=hit;}
            }
            return closest.collider;
        }
        private void Launch(CharacterHealth target,int index)
        {
            Bolt bolt=null;foreach(var b in bolts)if(!b.active){bolt=b;break;}
            if(bolt==null)
            {
                if(bolts.Count>=data.poolCapacity)return;
                var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Energy bolt";go.transform.SetParent(pool.transform);
                var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
                go.transform.localScale=Vector3.one*data.projectileRadius*2;go.GetComponent<Renderer>().sharedMaterial=material;
                var trail=go.AddComponent<TrailRenderer>();trail.sharedMaterial=material;trail.time=.12f;trail.startWidth=data.projectileRadius;trail.endWidth=0;trail.minVertexDistance=.04f;trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                bolt=new Bolt{go=go,trail=trail};bolts.Add(bolt);
            }
            bolt.active=true;bolt.go.SetActive(true);bolt.go.transform.position=Origin;
            Vector3 direction=(Point(target)-Origin).normalized;
            if(index>=targets.Count)direction=Quaternion.AngleAxis((index%2==0?1:-1)*(index+1)*1.5f,Vector3.up)*direction;
            bolt.direction=direction;bolt.damage=EffectiveDamage;bolt.distance=data.range;bolt.trail.Clear();
        }
        private void StepBolts(float dt)
        {
            foreach(var b in bolts)
            {
                if(!b.active)continue;
                float step=Mathf.Min(b.distance,ProjectileSpeed*dt);
                if(FirstCollision(b.go.transform.position,b.direction,step,out var hit))
                {
                    var target=hit.collider.GetComponentInParent<CharacterHealth>();
                    if(target&&target.IsMonster&&target.IsTargetable)
                    {
                        float dealt=target.DealDamageWithTrueBonus(b.damage,CombatDamageType.Physical,gameObject,stats?target.MaxHealth*stats.ConsumableMonsterTrueDamageFraction:0,true);
                        HitsLanded++;if(specials)specials.OnBulletHit(target,b.damage,dealt,hit.point);
                    }
                    b.active=false;b.go.SetActive(false);continue;
                }
                b.go.transform.position+=b.direction*step;b.distance-=step;
                if(b.distance<=0){b.active=false;b.go.SetActive(false);}
            }
        }
        private void ClearBolts(){foreach(var b in bolts){b.active=false;if(b.go)b.go.SetActive(false);}}
        private void OnDisable(){CurrentTarget=null;ClearBolts();}
        private void OnDestroy(){if(pool)Destroy(pool);if(material)Destroy(material);}
    }
}
