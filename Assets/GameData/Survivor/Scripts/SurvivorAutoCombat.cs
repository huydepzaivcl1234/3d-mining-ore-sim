using System.Collections.Generic;
using UnityEngine;
using MiningSimulator.Ores;

namespace GameData.Player.Script
{
    [DisallowMultipleComponent,RequireComponent(typeof(SurvivorAbilities))]
    public sealed class SurvivorAutoCombat : MonoBehaviour
    {
        [HideInInspector] public SurvivorCombatData data;
        [Header("SFX")]
        public AudioClip shotSfx;
        public AudioClip[] impactSfx=System.Array.Empty<AudioClip>();
        [Range(0,1)] public float shotVolume=.7f,impactVolume=.65f;
        private MiningAudioManager audioManager;
        private int lastImpact=-1;
        public int ShotSoundsPlayed{get;private set;}
        public int ImpactSoundsPlayed{get;private set;}
        public int IgniteSoundsPlayed{get;private set;}
        private AudioSource frostVoice;
        private float nextFrostSound;
        public int FrostSoundsPlayed{get;private set;}
        public void PlayFrostbite(Vector3 position)
        {
            var rule=Stats?Stats.AbilityData.epic.frostbite:SurvivorAbilityData.Fallback.epic.frostbite;
            if(!audioManager||!rule.sound||Time.time<nextFrostSound||frostVoice&&frostVoice.isPlaying)return;
            if(!frostVoice){var go=new GameObject("Frostbite SFX - single voice");go.transform.SetParent(pool.transform,false);frostVoice=go.AddComponent<AudioSource>();frostVoice.playOnAwake=false;frostVoice.loop=false;}
            frostVoice.transform.position=position;audioManager.RegisterWorldSfxSource(frostVoice,rule.volume);frostVoice.clip=rule.sound;frostVoice.pitch=1;frostVoice.Play();nextFrostSound=Time.time+rule.soundInterval;FrostSoundsPlayed++;
        }
        public void PlayIgnite(Vector3 position)
        {
            var rule=Stats?Stats.AbilityData.common.ignite:SurvivorAbilityData.Fallback.common.ignite;
            if(!audioManager||rule.sounds==null||rule.sounds.Length==0)return;
            var clip=rule.sounds[Random.Range(0,rule.sounds.Length)];
            if(clip){audioManager.PlayWorldSfx(clip,position,rule.volume);IgniteSoundsPlayed++;}
        }
        [Tooltip("Chọn PlayerArmature trong Scene và bật Gizmos để xem tầm đánh.")]
        public bool drawAttackRange = true;
        public Color attackRangeColor = new Color(1f, .75f, .1f, .8f);
        private PlayerStats stats;
        private PlayerStats Stats => stats ? stats : stats = GetComponent<PlayerStats>();
        private SurvivorCombatData Core => data;
        public float BaseDamage => Core ? Mathf.Max(0f, Core.damage) : data ? data.damage : 20f;
        public float AttackRange => (Core ? Mathf.Max(.1f, Core.range) : data ? data.range : 13f)*(1f+(Stats?Stats.UpgradeBonus(SurvivorUpgradeId.AttackRange):0)*.01f);
        public float BaseFireRate => Core ? 1f / Mathf.Max(.08f, Core.attackInterval) : 1f / Mathf.Max(.08f, data ? data.attackInterval : .65f);
        public float BaseProjectileSpeed => Core ? Mathf.Max(.1f, Core.projectileSpeed) : data ? data.projectileSpeed : 22f;
        private CharacterHealth health;
        private MiningCardSystem cards;
        private UiPanelCoordinator ui;
        private GameMainMenu menu;
        private readonly RaycastHit[] hits=new RaycastHit[96];
        private readonly List<Bolt> bolts=new();
        private readonly List<CharacterHealth> splash=new();
        private List<CharacterHealth> volleyTargets;
        private Material material;
        private MaterialPropertyBlock boltColor;
        private GameObject pool;
        private float nextAttack;
        private SurvivorAbilities abilities;
        public float ProjectileRadius=>(data?data.projectileRadius:.1f)*(1+(Stats?Stats.UpgradeBonus(SurvivorUpgradeId.ProjectileSize):0)*.01f);
        public CharacterHealth CurrentTarget {get;private set;}
        public int VolleysFired {get;private set;}
        public int HitsLanded {get;private set;}
        public int ActiveProjectiles {get {int count=0;foreach(var b in bolts)if(b.active)count++;return count;}}
        public float EffectiveDamage=>Stats?Stats.CalculateSurvivorDamage(BaseDamage):BaseDamage;
        public float AttackInterval=>Mathf.Max(.08f,1f/BaseFireRate/Mathf.Max(.1f,Stats?Stats.AttackSpeed:1));
        public float ShotsPerSecond=>1f/AttackInterval;
        public int TrueProjectileCount=>Stats&&Stats.UpgradeLevel(SurvivorUpgradeId.TrueBarrage)>0?Mathf.Clamp(Mathf.RoundToInt(Stats.AbilityData.epic.trueBarrage.projectileCount.At(Stats.UpgradeLevel(SurvivorUpgradeId.TrueBarrage))),0,SurvivorUpgradeData.LevelCap):0;
        public int ProjectileCount=>1+(Stats&&Stats.UpgradeLevel(SurvivorUpgradeId.TwinMagic)>0?1:0)+TrueProjectileCount;
        private int PoolLimit=>Mathf.Max(data.poolCapacity,ProjectileCount*Mathf.CeilToInt(AttackRange/Mathf.Max(.1f,ProjectileSpeed)/AttackInterval+2)+(Stats?Mathf.Clamp(Stats.AbilityData.common.orbBarrage.orbCount,1,99):5));
        public float ProjectileSpeed=>BaseProjectileSpeed*(Stats?Stats.GunBulletSpeedMultiplier:1);
        public bool CanAttack=>enabled&&data&&health&&health.Health>0&&Time.timeScale>0&&!(cards&&cards.IsChoosing)&&!(ui&&ui.BlocksGameplay)&&!(menu&&menu.IsOpen);
        private sealed class Bolt {public GameObject go;public TrailRenderer trail;public CharacterHealth target;public float damage,remaining;public bool active,secondary,canBounce,abilityProc;public CombatDamageType type;public int spawnFrame;}
        private void Awake()
        {
            audioManager=FindAnyObjectByType<MiningAudioManager>();
            abilities=GetComponent<SurvivorAbilities>();stats=GetComponent<PlayerStats>();if(stats&&stats.CoreData)data=stats.CoreData;health=GetComponent<CharacterHealth>();
            cards=FindAnyObjectByType<MiningCardSystem>();ui=FindAnyObjectByType<UiPanelCoordinator>();menu=FindAnyObjectByType<GameMainMenu>();
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
            float nearest=AttackRange*AttackRange;
            CurrentTarget=null;(volleyTargets??=new()).Clear();
            foreach(var target in CharacterHealth.ActiveMonsters)
            {
                if(!target||!target.isActiveAndEnabled||!target.IsTargetable)continue;
                float distance=(target.transform.position-transform.position).sqrMagnitude;
                if(distance>AttackRange*AttackRange)continue;
                Vector3 path=Point(target)-Origin;
                if(FirstCollision(Origin,path,path.magnitude,out var hit) && hit.collider.GetComponentInParent<CharacterHealth>()!=target)continue;
                volleyTargets.Add(target);if(distance<=nearest){nearest=distance;CurrentTarget=target;}
            }
            if(!CurrentTarget || Time.time<nextAttack)return;
            float interval=AttackInterval;
            // Preserve cadence across target changes; a new encounter fires immediately.
            nextAttack=Time.time-nextAttack>interval?Time.time+interval:nextAttack+interval;
            VolleysFired++;
            if(audioManager&&shotSfx){audioManager.PlayWorldSfx(shotSfx,Origin,shotVolume);ShotSoundsPlayed++;}
            volleyTargets.Sort((a,b)=>(a.transform.position-transform.position).sqrMagnitude.CompareTo((b.transform.position-transform.position).sqrMagnitude));CurrentTarget=volleyTargets[0];
            int twin=Stats?Stats.UpgradeLevel(SurvivorUpgradeId.TwinMagic):0;
            int extras=TrueProjectileCount+(twin>0?1:0);
            Launch(CurrentTarget,EffectiveDamage,origin:Origin);
            if(twin>0)Launch(volleyTargets.Count>1?volleyTargets[1]:CurrentTarget,Stats.AbilityData.rare.twinMagic.flatDamage+Stats.AbilityData.rare.twinMagic.baseDamagePercent*.01f*BaseDamage,true,CombatDamageType.Magic,false,BonusProjectileOrigin(0,extras,CurrentTarget));
            int extra=TrueProjectileCount;
            for(int i=0;i<extra;i++){
                var target=volleyTargets.Count>1?volleyTargets[1+i%(volleyTargets.Count-1)]:CurrentTarget;
                Launch(target,EffectiveDamage*Stats.AbilityData.epic.trueBarrage.attackDamagePercent*.01f,true,CombatDamageType.True,false,BonusProjectileOrigin(i+(twin>0?1:0),extras,CurrentTarget));
            }
        }
        public Vector3 BonusProjectileOrigin(int index,int count,CharacterHealth target)
        {
            Vector3 forward=Vector3.ProjectOnPlane(target?Point(target)-Origin:transform.forward,Vector3.up).normalized;
            if(forward.sqrMagnitude<.001f)forward=transform.forward;
            Vector3 right=Vector3.Cross(Vector3.up,forward);
            float angle=(count<=1?90f:Mathf.Lerp(60f,300f,Mathf.Clamp01((float)index/(count-1))))*Mathf.Deg2Rad;
            float radius=Mathf.Max(.9f,ProjectileRadius*2f)*(Stats?Stats.BodySizeMultiplier:1f);
            return Origin+(right*Mathf.Sin(angle)+forward*Mathf.Cos(angle))*radius+Vector3.up*(.1f*(index%3));
        }
        private bool FirstCollision(Vector3 origin,Vector3 direction,float distance,out RaycastHit closest)
        {
            closest=default;float nearest=distance+.001f;
            int count=Physics.SphereCastNonAlloc(origin,ProjectileRadius,direction.normalized,hits,distance,data.hitLayers,QueryTriggerInteraction.Collide);
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];if(hit.transform.IsChildOf(transform)||MonsterHitbox.IgnoreShot(hit.collider))continue;
                var victim=hit.collider.GetComponentInParent<CharacterHealth>();
                if(hit.collider.isTrigger && (!victim||!victim.IsMonster))continue;
                if(hit.distance<nearest){nearest=hit.distance;closest=hit;}
            }
            return closest.collider;
        }
        public void Launch(CharacterHealth target,float damage,bool secondary=false,CombatDamageType type=CombatDamageType.Physical,bool canBounce=true,Vector3? origin=null,bool abilityProc=false)
        {
            if(!target||!target.IsTargetable||!pool)return;
            Bolt bolt=null;foreach(var b in bolts)if(!b.active){bolt=b;break;}
            if(bolt==null)
            {
                if(bolts.Count>=PoolLimit)return;
                var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Energy bolt";go.transform.SetParent(pool.transform);
                var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
                go.transform.localScale=Vector3.one*data.projectileRadius*2;go.GetComponent<Renderer>().sharedMaterial=material;
                var trail=go.AddComponent<TrailRenderer>();trail.sharedMaterial=material;trail.time=.12f;trail.startWidth=data.projectileRadius;trail.endWidth=0;trail.minVertexDistance=.04f;trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                bolt=new Bolt{go=go,trail=trail};bolts.Add(bolt);
            }
            Vector3 start=origin??Origin;
            bolt.active=true;bolt.spawnFrame=Time.frameCount;bolt.go.SetActive(true);bolt.go.transform.position=start;
            bolt.target=target;bolt.damage=damage;bolt.secondary=secondary;bolt.type=type;bolt.canBounce=canBounce;bolt.abilityProc=abilityProc;
            bolt.go.transform.localScale=Vector3.one*ProjectileRadius*2;bolt.trail.startWidth=ProjectileRadius;
            Color color=abilityProc?Color.cyan:type==CombatDamageType.True?Color.white:type==CombatDamageType.Magic?new Color(.55f,.65f,1f):secondary?new Color(1f,.9f,.3f):data.projectileColor;
            boltColor??=new MaterialPropertyBlock();boltColor.SetColor("_BaseColor",color);bolt.go.GetComponent<Renderer>().SetPropertyBlock(boltColor);
            bolt.trail.SetPropertyBlock(boltColor);bolt.trail.startColor=bolt.trail.endColor=Color.white;
            bolt.remaining=Mathf.Max(.03f,Vector3.Distance(Point(target),start)/ProjectileSpeed);bolt.trail.Clear();
        }
        private void StepBolts(float dt)
        {
            for(int index=0;index<bolts.Count;index++)
            {
                var b=bolts[index];if(!b.active||b.spawnFrame==Time.frameCount)continue;
                var target=b.target;
                if(!target||!target.isActiveAndEnabled||!target.IsTargetable){
                    // Bonus bolts can outlive the enemy killed by an earlier bolt.
                    target=b.secondary?SurvivorAbilities.Nearest(b.go.transform.position,AttackRange,target):null;
                    if(!target){b.active=false;b.target=null;b.go.SetActive(false);continue;}
                    b.target=target;b.remaining=Mathf.Max(.03f,Vector3.Distance(Point(target),b.go.transform.position)/ProjectileSpeed);
                }
                Vector3 destination=Point(target);
                // A locked shot meets its target at the scheduled arrival, even after a sharp turn.
                b.go.transform.position=Vector3.Lerp(b.go.transform.position,destination,Mathf.Clamp01(dt/Mathf.Max(.001f,b.remaining)));
                b.remaining-=dt;
                if(b.remaining>0 && Vector3.SqrMagnitude(b.go.transform.position-destination)>ProjectileRadius*ProjectileRadius)continue;
                Vector3 hitPoint=b.go.transform.position;
                // Damage/death callbacks may remove the victim; release the slot before spawning a bounce.
                float damage=b.damage;bool secondary=b.secondary,canBounce=b.canBounce,proc=b.abilityProc;var type=b.type;
                b.active=false;b.target=null;b.go.SetActive(false);
                float dealt=target.DealDamageWithTrueBonus(damage,type,gameObject,0,!secondary,false,proc);
                if(dealt>0)
                {
                    HitsLanded++;
                    if(audioManager&&impactSfx.Length>0){int sample=impactSfx.Length==1?0:Random.Range(0,impactSfx.Length-1);if(impactSfx.Length>1&&sample>=lastImpact)sample++;lastImpact=sample;if(impactSfx[sample]){audioManager.PlayWorldSfx(impactSfx[sample],hitPoint,impactVolume);ImpactSoundsPlayed++;}}
                    if(abilities)abilities.ProjectileHit(target,damage,dealt,secondary,canBounce,hitPoint);
                    // Larger projectiles damage every target overlapping their impact sphere.
                    if(Stats&&Stats.UpgradeLevel(SurvivorUpgradeId.ProjectileSize)>0)
                    {
                        splash.Clear();foreach(var other in CharacterHealth.ActiveMonsters)if(other&&other!=target&&other.IsTargetable&&(Point(other)-hitPoint).sqrMagnitude<=ProjectileRadius*ProjectileRadius)splash.Add(other);
                        foreach(var other in splash)if(other&&other.IsTargetable)other.DealDamage(damage,type,gameObject);
                    }
                }
            }
        }
        private void OnDrawGizmosSelected()
        {
            if(!drawAttackRange)return;
            Color previous=Gizmos.color;
            Gizmos.color=new Color(attackRangeColor.r,attackRangeColor.g,attackRangeColor.b,.25f);
            Gizmos.DrawWireSphere(transform.position,AttackRange);
#if UNITY_EDITOR
            Color previousHandles=UnityEditor.Handles.color;
            UnityEditor.Handles.color=attackRangeColor;
            UnityEditor.Handles.DrawWireDisc(transform.position+Vector3.up*.03f,Vector3.up,AttackRange);
            UnityEditor.Handles.Label(transform.position+Vector3.right*AttackRange+Vector3.up*.15f,$"Attack range: {AttackRange:0.##} m");
            UnityEditor.Handles.color=previousHandles;
#endif
            if(CurrentTarget){Gizmos.color=attackRangeColor;Gizmos.DrawLine(Origin,Point(CurrentTarget));}
            Gizmos.color=previous;
        }
        private void ClearBolts(){foreach(var b in bolts){b.active=false;b.target=null;if(b.go)b.go.SetActive(false);}}
        private void OnDisable(){CurrentTarget=null;ClearBolts();if(frostVoice)frostVoice.Stop();}
        private void OnDestroy(){if(pool)Destroy(pool);if(material)Destroy(material);}
    }
}
