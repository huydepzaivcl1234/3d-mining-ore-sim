using System.Collections.Generic;
using UnityEngine;
using MiningSimulator.Ores;

namespace GameData.Player.Script
{
    public enum ProjectileElement { None, Fire, Ice }
    [DisallowMultipleComponent,RequireComponent(typeof(SurvivorAbilities))]
    public sealed partial class SurvivorAutoCombat : MonoBehaviour
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
        public float BaseFireRate => Core ? Mathf.Max(.01f,Core.attackSpeed) : data ? Mathf.Max(.01f,data.attackSpeed) : 1f/.65f;
        public float BaseProjectileSpeed => Core ? Mathf.Max(.1f, Core.projectileSpeed) : data ? data.projectileSpeed : 22f;
        private CharacterHealth health;
        private MiningCardSystem cards;
        private UiPanelCoordinator ui;
        private GameMainMenu menu;
        private readonly RaycastHit[] hits=new RaycastHit[96];
        private readonly List<Bolt> bolts=new();
        private readonly List<CharacterHealth> splash=new();
        private List<CharacterHealth> volleyTargets;
        private Material material,impactMaterial;
        private readonly List<ImpactFlash> impacts=new();
        private sealed class ImpactParticles {public GameObject go;public ParticleSystem[] particles;public float remaining,duration;}
        private readonly List<ImpactParticles> impactParticles=new();
        public int ImpactParticlesPlayed {get;private set;}
        public int ImpactFlashesPlayed {get;private set;}
        public int ActiveImpactFlashes {get {int count=0;foreach(var flash in impacts)if(flash.go.activeSelf)count++;return count;}}
        private sealed class ImpactFlash {public GameObject go;public Transform transform;public Renderer renderer;public Color color;public float remaining,duration,size;}
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
        public float AttackInterval=>1f/ShotsPerSecond;
        public float ShotsPerSecond=>Stats?Stats.CalculateAttacksPerSecond():data?data.AttacksPerSecond():BaseFireRate;
        public int TrueProjectileCount=>Stats&&Stats.UpgradeLevel(SurvivorUpgradeId.TrueBarrage)>0?Mathf.Clamp(Mathf.RoundToInt(Stats.AbilityData.epic.trueBarrage.projectileCount.At(Stats.UpgradeLevel(SurvivorUpgradeId.TrueBarrage))),0,SurvivorUpgradeData.LevelCap):0;
        private int ElementalProjectileCount=>(Stats&&Stats.UpgradeLevel(SurvivorUpgradeId.Burn)>0?1:0)+(Stats&&Stats.UpgradeLevel(SurvivorUpgradeId.Frostbite)>0?1:0);
        public int ProjectileCount=>1+ElementalProjectileCount+(Stats&&Stats.UpgradeLevel(SurvivorUpgradeId.TwinMagic)>0?1:0)+TrueProjectileCount;
        private int PoolLimit=>Mathf.Max(data.poolCapacity,ProjectileCount*Mathf.CeilToInt(AttackRange/Mathf.Max(.1f,ProjectileSpeed)/AttackInterval+2)+(Stats?Mathf.Clamp(Stats.AbilityData.common.orbBarrage.orbCount,1,99):5));
        public float ProjectileSpeed=>BaseProjectileSpeed*(Stats?Stats.GunBulletSpeedMultiplier:1);
        public bool CanAttack=>enabled&&data&&health&&health.Health>0&&Time.timeScale>0&&!(cards&&cards.IsChoosing)&&!(ui&&ui.BlocksGameplay)&&!(menu&&menu.IsOpen);
        private sealed class Bolt {public GameObject go,visual,prefab;public Transform transform;public Renderer renderer;public Color color;public TrailRenderer trail;public CharacterHealth target;public float damage,remaining;public bool active,secondary,canBounce,abilityProc;public CombatDamageType type;public ProjectileElement element;public int spawnFrame;}
        private void Awake()
        {
            audioManager=FindAnyObjectByType<MiningAudioManager>();
            abilities=GetComponent<SurvivorAbilities>();stats=GetComponent<PlayerStats>();if(stats&&stats.CoreData)data=stats.CoreData;health=GetComponent<CharacterHealth>();
            cards=FindAnyObjectByType<MiningCardSystem>();ui=FindAnyObjectByType<UiPanelCoordinator>();menu=FindAnyObjectByType<GameMainMenu>();
            if(!data){enabled=false;return;}
            pool=new GameObject("Survivor Energy Projectiles");
            var shader=Shader.Find("Universal Render Pipeline/Unlit");
            material=new Material(shader){name="Survivor projectile runtime"};material.SetColor("_BaseColor",data.projectileColor);
            impactMaterial=new Material(shader){name="Survivor impact runtime"};
            impactMaterial.SetOverrideTag("RenderType","Transparent");impactMaterial.SetFloat("_Surface",1);impactMaterial.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);impactMaterial.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.One);impactMaterial.SetFloat("_ZWrite",0);impactMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");impactMaterial.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        private Vector3 Origin=>transform.position+Vector3.up*1.0f;
        private static Vector3 Point(CharacterHealth target)
        {
            var collider=target.GetComponent<Collider>();return collider?collider.bounds.center:target.transform.position+Vector3.up*.7f;
        }
        private void Update()
        {
            StepImpactFlashes(Time.deltaTime);
            if(!CanAttack){nextAttack=Time.time;CurrentTarget=null;if(!health||health.Health<=0)ClearBolts();return;}
            StepBolts(Time.deltaTime);
            var alune=GetComponent<AlunePlayerEffect>();if(alune&&alune.IsStunned){nextAttack=Time.time;CurrentTarget=null;return;}
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
            int extras=TrueProjectileCount+(twin>0?1:0)+ElementalProjectileCount;
            Launch(CurrentTarget,EffectiveDamage,origin:Origin);
            if(twin>0)Launch(volleyTargets.Count>1?volleyTargets[1]:CurrentTarget,Stats.AbilityData.rare.twinMagic.flatDamage+Stats.AbilityData.rare.twinMagic.baseDamagePercent*.01f*BaseDamage,true,CombatDamageType.Magic,false,BonusProjectileOrigin(0,extras,CurrentTarget));
            int extra=TrueProjectileCount;
            for(int i=0;i<extra;i++){
                var target=volleyTargets.Count>1?volleyTargets[1+i%(volleyTargets.Count-1)]:CurrentTarget;
                Launch(target,EffectiveDamage*Stats.AbilityData.epic.trueBarrage.attackDamagePercent*.01f,true,CombatDamageType.True,false,BonusProjectileOrigin(i+(twin>0?1:0),extras,CurrentTarget));
            }
            int elementIndex=TrueProjectileCount+(twin>0?1:0);
            if(Stats&&Stats.UpgradeLevel(SurvivorUpgradeId.Burn)>0)Launch(CurrentTarget,EffectiveDamage,true,origin:BonusProjectileOrigin(elementIndex++,extras,CurrentTarget),element:ProjectileElement.Fire);
            if(Stats&&Stats.UpgradeLevel(SurvivorUpgradeId.Frostbite)>0)Launch(CurrentTarget,EffectiveDamage,true,origin:BonusProjectileOrigin(elementIndex,extras,CurrentTarget),element:ProjectileElement.Ice);
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
        public GameObject ProjectilePrefab
        {
            get{
                var style=data.projectileAppearance;
                if(style==ProjectileAppearance.Automatic)style=ProjectileAppearance.Normal;
                return style==ProjectileAppearance.Ice?data.iceProjectilePrefab:style==ProjectileAppearance.Fire?data.fireProjectilePrefab:style==ProjectileAppearance.Toxic?data.toxicProjectilePrefab:data.normalProjectilePrefab;
            }
        }
        public void Launch(CharacterHealth target,float damage,bool secondary=false,CombatDamageType type=CombatDamageType.Physical,bool canBounce=true,Vector3? origin=null,bool abilityProc=false,ProjectileElement element=ProjectileElement.None)
        {
            if(!target||!target.IsTargetable||!pool)return;
            var prefab=element==ProjectileElement.Fire?data.fireProjectilePrefab:element==ProjectileElement.Ice?data.iceProjectilePrefab:ProjectilePrefab;
            Bolt bolt=null;foreach(var b in bolts)if(!b.active&&b.prefab==prefab){bolt=b;break;}
            if(bolt==null)foreach(var b in bolts)if(!b.active){bolt=b;break;}
            if(bolt==null)
            {
                if(bolts.Count>=PoolLimit)return;
                var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Energy bolt";go.transform.SetParent(pool.transform);
                var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
                go.transform.localScale=Vector3.one*data.projectileRadius*2;go.GetComponent<Renderer>().sharedMaterial=material;
                var trail=go.AddComponent<TrailRenderer>();trail.sharedMaterial=material;trail.time=data.projectileTrailSeconds;trail.startWidth=data.projectileRadius*data.projectileTrailWidth;trail.endWidth=0;trail.minVertexDistance=.04f;trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                bolt=new Bolt{go=go,transform=go.transform,renderer=go.GetComponent<Renderer>(),trail=trail};bolts.Add(bolt);
            }
            if(bolt.prefab!=prefab){if(bolt.visual)Destroy(bolt.visual);bolt.visual=prefab?Instantiate(prefab,bolt.transform,false):null;bolt.prefab=prefab;}
            bolt.renderer.enabled=!prefab;bolt.trail.enabled=!prefab;
            Vector3 start=origin??Origin;
            bolt.active=true;bolt.spawnFrame=Time.frameCount;bolt.go.SetActive(true);bolt.transform.position=start;var direction=Point(target)-start;if(direction.sqrMagnitude>.0001f)bolt.transform.rotation=Quaternion.LookRotation(direction);
            bolt.target=target;bolt.damage=damage;bolt.secondary=secondary;bolt.type=type;bolt.canBounce=canBounce;bolt.abilityProc=abilityProc;bolt.element=element;
            bolt.transform.localScale=Vector3.one*ProjectileRadius*2;bolt.trail.time=data.projectileTrailSeconds;bolt.trail.startWidth=ProjectileRadius*data.projectileTrailWidth;
            Color color=abilityProc?Color.cyan:type==CombatDamageType.True?Color.white:type==CombatDamageType.Magic?new Color(.55f,.65f,1f):secondary?new Color(1f,.9f,.3f):data.projectileColor;
            if(prefab)color=prefab==data.iceProjectilePrefab?new Color(.5f,.95f,1):prefab==data.fireProjectilePrefab?new Color(1,.6f,.08f):prefab==data.toxicProjectilePrefab?new Color(.5f,1,.08f):new Color(.5f,.7f,1);
            boltColor??=new MaterialPropertyBlock();boltColor.SetColor("_BaseColor",color);bolt.color=color;bolt.renderer.SetPropertyBlock(boltColor);
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
                    target=b.secondary?SurvivorAbilities.Nearest(b.transform.position,AttackRange,target):null;
                    if(!target){b.active=false;b.target=null;b.go.SetActive(false);continue;}
                    b.target=target;b.remaining=Mathf.Max(.03f,Vector3.Distance(Point(target),b.transform.position)/ProjectileSpeed);
                }
                Vector3 destination=Point(target);Vector3 incoming=b.transform.position-destination;if(incoming.sqrMagnitude>.0001f)b.transform.rotation=Quaternion.LookRotation(-incoming);
                // A locked shot meets its target at the scheduled arrival, even after a sharp turn.
                b.transform.position=Vector3.Lerp(b.transform.position,destination,Mathf.Clamp01(dt/Mathf.Max(.001f,b.remaining)));
                b.remaining-=dt;
                if(b.remaining>0 && Vector3.SqrMagnitude(b.transform.position-destination)>ProjectileRadius*ProjectileRadius)continue;
                Vector3 hitPoint=b.transform.position;Vector3 flashPoint=ImpactSurface(target,destination,incoming);
                // Damage/death callbacks may remove the victim; release the slot before spawning a bounce.
                float damage=b.damage;bool secondary=b.secondary,canBounce=b.canBounce,proc=b.abilityProc;var type=b.type;var element=b.element;var impactColor=b.color;
                b.active=false;b.target=null;b.go.SetActive(false);
                float dealt=target.DealDamageWithTrueBonus(damage,type,gameObject,0,!secondary,false,proc);
                if(dealt>0)
                {
                    HitsLanded++;PlayContactImpact(flashPoint,incoming,impactColor);
                    if(abilities)abilities.ProjectileHit(target,damage,dealt,secondary,canBounce,hitPoint,element);
                    // Larger projectiles damage every target overlapping their impact sphere.
                    if(Stats&&Stats.UpgradeLevel(SurvivorUpgradeId.ProjectileSize)>0)
                    {
                        splash.Clear();foreach(var other in CharacterHealth.ActiveMonsters)if(other&&other!=target&&other.IsTargetable&&(Point(other)-hitPoint).sqrMagnitude<=ProjectileRadius*ProjectileRadius)splash.Add(other);
                        foreach(var other in splash)if(other&&other.IsTargetable)other.DealDamage(damage,type,gameObject);
                    }
                }
            }
        }
        private static Vector3 ImpactSurface(CharacterHealth target,Vector3 center,Vector3 incoming)
        {
            var direction=incoming.sqrMagnitude>.0001f?incoming.normalized:Vector3.back;
            var collider=target.GetComponent<Collider>();
            if(collider)return collider.ClosestPoint(center+direction*(collider.bounds.extents.magnitude+1));
            // GPU actors have no collider; place feedback on their visible front, not inside the mesh.
            return center+direction*(.45f*Mathf.Max(.1f,target.transform.lossyScale.x));
        }
        public void PlaySwordImpact(CharacterHealth target,Vector3 contact,Vector3 incoming,Color color)
        {
            PlayContactImpact(contact,incoming,color,.24f);
        }
        private void PlayContactImpact(Vector3 point,Vector3 incoming,Color color,float size=0)
        {
            ShowImpactFlash(point,color,size);ShowImpactParticles(point,incoming);
            if(audioManager&&impactSfx.Length>0){int sample=impactSfx.Length==1?0:Random.Range(0,impactSfx.Length-1);if(impactSfx.Length>1&&sample>=lastImpact)sample++;lastImpact=sample;if(impactSfx[sample]){audioManager.PlayWorldSfx(impactSfx[sample],point,impactVolume);ImpactSoundsPlayed++;}}
        }
        private void ShowImpactParticles(Vector3 point,Vector3 incoming)
        {
            if(!data.impactParticlePrefab||!pool)return;
            ImpactParticles slot=null;foreach(var item in impactParticles)if(!item.go.activeSelf){slot=item;break;}
            if(slot==null){
                if(impactParticles.Count>=Mathf.Max(8,data.poolCapacity))return;
                var go=Instantiate(data.impactParticlePrefab,pool.transform);go.name="Attack hit particles";go.SetActive(false);
                slot=new ImpactParticles{go=go,particles=go.GetComponentsInChildren<ParticleSystem>(true)};
                foreach(var particle in slot.particles){var main=particle.main;main.loop=false;main.stopAction=ParticleSystemStopAction.None;slot.duration=Mathf.Max(slot.duration,main.startDelay.constantMax+main.duration+main.startLifetime.constantMax);}
                impactParticles.Add(slot);
            }
            slot.go.transform.SetPositionAndRotation(point,Quaternion.LookRotation(incoming.sqrMagnitude>.0001f?incoming.normalized:Vector3.up));slot.go.transform.localScale=Vector3.one*data.impactParticleScale;slot.remaining=slot.duration;slot.go.SetActive(true);
            foreach(var particle in slot.particles){particle.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);particle.Play(false);}ImpactParticlesPlayed++;
        }
        private void ShowImpactFlash(Vector3 point,Color color,float sizeOverride=0)
        {
            if(data.impactFlashSeconds<=0||!impactMaterial)return;
            ImpactFlash flash=null;foreach(var item in impacts)if(!item.go.activeSelf){flash=item;break;}
            if(flash==null){
                if(impacts.Count>=Mathf.Max(8,data.poolCapacity))return;
                var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Projectile hit flash";go.transform.SetParent(pool.transform,false);
                var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
                var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=impactMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
                flash=new ImpactFlash{go=go,transform=go.transform,renderer=renderer};impacts.Add(flash);
            }
            flash.color=Color.Lerp(color,Color.white,.65f);flash.duration=flash.remaining=data.impactFlashSeconds;flash.size=sizeOverride>0?sizeOverride:ProjectileRadius*2*data.impactFlashSize;
            flash.transform.position=point;flash.transform.localScale=Vector3.one*flash.size;flash.go.SetActive(true);SetImpactColor(flash,1);ImpactFlashesPlayed++;
        }
        private void SetImpactColor(ImpactFlash flash,float alpha)
        {
            boltColor??=new MaterialPropertyBlock();var color=flash.color;color.a=alpha;boltColor.SetColor("_BaseColor",color);flash.renderer.SetPropertyBlock(boltColor);
        }
        private void StepImpactFlashes(float dt)
        {
            foreach(var effect in impactParticles)if(effect.go.activeSelf){effect.remaining-=dt;if(effect.remaining<=0)effect.go.SetActive(false);}
            foreach(var flash in impacts){
                if(!flash.go.activeSelf)continue;flash.remaining=Mathf.Max(0,flash.remaining-dt);
                if(flash.remaining<=0){flash.go.SetActive(false);continue;}
                float progress=1-flash.remaining/flash.duration;flash.transform.localScale=Vector3.one*(flash.size*Mathf.Lerp(1,1.45f,progress));SetImpactColor(flash,1-progress);
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
        private void ClearBolts(){foreach(var effect in impactParticles)if(effect.go)effect.go.SetActive(false);foreach(var flash in impacts)if(flash.go)flash.go.SetActive(false);foreach(var b in bolts){b.active=false;b.target=null;if(b.go)b.go.SetActive(false);}}
        private void OnDisable(){CurrentTarget=null;ClearBolts();if(frostVoice)frostVoice.Stop();}
        private void OnDestroy(){if(pool)Destroy(pool);if(material)Destroy(material);if(impactMaterial)Destroy(impactMaterial);}
    }
}
