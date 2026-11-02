using System.Collections.Generic;
using UnityEngine;
using MiningSimulator.Ores;

namespace GameData.Player.Script
{
    [DisallowMultipleComponent]
    public sealed partial class SurvivorAbilities : MonoBehaviour
    {
        private PlayerStats stats;
        private CharacterHealth health;
        private SurvivorAutoCombat combat;
        private int shieldLevel, incomingHits, orbHits;
        private bool processing;
        private float shield, shieldDelay, lifeTimer, auraTimer, invulnTimer, invulnerable;
        private float recoveryCooldown, recoveryRemaining, recoveryPerSecond, defenseDecay, lastIncoming;
        private readonly Dictionary<CharacterHealth,float> attackers=new();
        private readonly List<CharacterHealth> scratch=new();
        private List<CharacterHealth> orbTargets;
        private GameObject auraEffect,shieldEffect;
        public SurvivorAbilityData Settings=>stats?stats.AbilityData:SurvivorAbilityData.Fallback;
        public float Shield=>shield;
        public float ShieldRecoveryRemaining=>shieldDelay;
        public float MaxShield=>Level(SurvivorUpgradeId.Shield)>0?health.MaxHealth*Settings.uncommon.shield.maxHealthPercent*.01f+Settings.uncommon.shield.flatShield.At(Level(SurvivorUpgradeId.Shield)):0;
        public float DefenseBonus=>ArmorBonus;
        public float MissingHealthDefenseBonus=>Level(SurvivorUpgradeId.MissingHealthArmor)>0&&health&&health.Health>0?Mathf.Floor(Mathf.Max(0,health.MaxHealth-health.Health)/Mathf.Max(.01f,Settings.epic.missingHealthArmor.missingHealthPerStack))*Settings.epic.missingHealthArmor.defensesPerStack.At(Level(SurvivorUpgradeId.MissingHealthArmor)):0;
        public float ArmorBonus=>MissingHealthDefenseBonus+(Level(SurvivorUpgradeId.ReactiveArmor)>0?attackers.Count*Settings.rare.reactiveArmor.armorPerEnemy:0);
        public float MagicResistanceBonus=>MissingHealthDefenseBonus+(Level(SurvivorUpgradeId.ReactiveArmor)>0?attackers.Count*Settings.rare.reactiveArmor.magicResistancePerEnemy:0);
        public bool Invulnerable=>invulnerable>0;
        public float AuraRadius=>Mathf.Max(0,Settings.uncommon.aura.radius.At(Level(SurvivorUpgradeId.Aura)));
        private int Level(SurvivorUpgradeId id)=>stats?stats.UpgradeLevel(id):0;
        private void Awake(){stats=GetComponent<PlayerStats>();health=GetComponent<CharacterHealth>();combat=GetComponent<SurvivorAutoCombat>();}
        private void OnEnable(){CharacterHealth.AnyDamaged+=OnDamage;CharacterHealth.AnyDied+=OnDeath;}
        private void Start(){ResetLife();if(health&&health.HealthBar&&!GetComponent<PlayerShieldBar>())gameObject.AddComponent<PlayerShieldBar>();}
        public void ResetLife(){ResetCube();shieldLevel=Level(SurvivorUpgradeId.Shield);shield=MaxShield;shieldDelay=lifeTimer=auraTimer=invulnTimer=invulnerable=recoveryCooldown=recoveryRemaining=0;incomingHits=orbHits=0;attackers.Clear();}
        private void OnDisable(){ResetCube();CharacterHealth.AnyDamaged-=OnDamage;CharacterHealth.AnyDied-=OnDeath;attackers.Clear();if(auraEffect)auraEffect.SetActive(false);if(shieldEffect)shieldEffect.SetActive(false);}
        private void Update()
        {
            if(!health||health.Health<=0){ResetCube();if(auraEffect)auraEffect.SetActive(false);if(shieldEffect)shieldEffect.SetActive(false);return;}
            if(!combat||!combat.CanAttack)return;
            Tick(Time.deltaTime);
        }
        private void Tick(float dt)
        {
            int lv=Level(SurvivorUpgradeId.Shield);
            if(lv!=shieldLevel){shieldLevel=lv;shield=MaxShield;shieldDelay=0;}
            if(shieldDelay>0){shieldDelay=Mathf.Max(0,shieldDelay-dt);if(shieldDelay==0)shield=MaxShield;}
            shield=Mathf.Min(shield,MaxShield);
            invulnerable=Mathf.Max(0,invulnerable-dt);
            lv=Level(SurvivorUpgradeId.Invulnerability);
            if(lv>0){invulnTimer+=dt;if(invulnTimer>=Mathf.Max(.01f,Mathf.Max(Settings.uncommon.invulnerability.minimumCooldown,Settings.uncommon.invulnerability.cooldown.At(lv)))){invulnTimer=0;invulnerable=Settings.uncommon.invulnerability.duration;}}
            lv=Level(SurvivorUpgradeId.LifeSpring);
            if(lv>0){lifeTimer+=dt;var h=Settings.common.lifeSpring;float interval=Mathf.Max(.01f,h.interval);if(lifeTimer>=interval){lifeTimer-=interval;health.Heal(h.flatHealing.At(lv)+h.maxHealthPercent*.01f*health.MaxHealth);}}
            recoveryCooldown=Mathf.Max(0,recoveryCooldown-dt);
            lv=Level(SurvivorUpgradeId.EmergencyHeal);
            if(lv>0&&recoveryCooldown==0&&health.Health<=health.MaxHealth*Settings.rare.emergencyHeal.healthThresholdPercent*.01f){var e=Settings.rare.emergencyHeal;recoveryRemaining=Mathf.Max(.01f,e.duration);recoveryPerSecond=health.MaxHealth*e.healMaxHealthPercent*.01f/recoveryRemaining;recoveryCooldown=Mathf.Max(.01f,Mathf.Max(e.minimumCooldown,e.cooldown.At(lv)));}
            if(recoveryRemaining>0){float step=Mathf.Min(dt,recoveryRemaining);recoveryRemaining-=step;health.Heal(recoveryPerSecond*step);}
            if(attackers.Count>0&&Time.time-lastIncoming>=Settings.rare.reactiveArmor.quietTime){defenseDecay+=dt;if(defenseDecay>=Mathf.Max(.01f,Settings.rare.reactiveArmor.decayInterval)){defenseDecay-=Mathf.Max(.01f,Settings.rare.reactiveArmor.decayInterval);CharacterHealth remove=null;bool found=false;foreach(var pair in attackers)if(!pair.Key||Time.time-pair.Value>=Settings.rare.reactiveArmor.quietTime){remove=pair.Key;found=true;break;}if(found)attackers.Remove(remove);}}
            lv=Level(SurvivorUpgradeId.Aura);
            if(lv>0){auraTimer+=dt;float interval=Mathf.Max(.01f,Settings.uncommon.aura.tickInterval);if(auraTimer>=interval){auraTimer-=interval;Targets(scratch,transform.position,AuraRadius,true);foreach(var target in scratch)if(target&&target.IsTargetable)target.DealPeriodicDamage(Settings.uncommon.aura.damage.At(lv),CombatDamageType.Magic,gameObject);}}
            SurvivorLoopVfx.Set(ref auraEffect,Settings.uncommon.aura.visual,transform,lv>0,AuraRadius/Mathf.Max(.01f,Settings.uncommon.aura.visual.referenceRadius));
            SurvivorLoopVfx.Set(ref shieldEffect,Settings.uncommon.shield.visual,transform,shield>0||Invulnerable);
            TickCube(dt);
        }
        public float ReceiveHit(float amount,GameObject source)
        {
            if(amount<=0||!health||health.Health<=0||Invulnerable)return 0;
            var attacker=source?source.GetComponentInParent<CharacterHealth>():null;
            if(attacker&&attacker.IsMonster){lastIncoming=Time.time;defenseDecay=0;if(Level(SurvivorUpgradeId.ReactiveArmor)>0)attackers[attacker]=Time.time;incomingHits++;}
            if(incomingHits>=Mathf.Max(1,Settings.rare.retaliate.incomingHits)){incomingHits=0;int lv=Level(SurvivorUpgradeId.Retaliate);if(lv>0){var target=Nearest(transform.position,Settings.rare.retaliate.range,null);if(target)target.DealDamage(Settings.rare.retaliate.armorPercent*.01f*health.Armor+Settings.rare.retaliate.magicResistancePercent*.01f*health.MagicResistance+Settings.rare.retaliate.flatDamage.At(lv),CombatDamageType.Physical,gameObject);}}
            float absorbed=Mathf.Min(shield,amount);shield-=absorbed;
            if(absorbed>0&&shield<=0){shieldDelay=Settings.uncommon.shield.brokenRecovery;if(shieldDelay<=0)shield=MaxShield;}
            return amount-absorbed;
        }
        private void OnDamage(CharacterHealth victim,float dealt,GameObject source,bool basic,CombatDamageType type)
        {
            if(!isActiveAndEnabled||processing||!victim||!victim.IsMonster||source!=gameObject||dealt<=0)return;
            processing=true;
            try{
                int weaken=Level(SurvivorUpgradeId.Weaken),air=Level(SurvivorUpgradeId.Airborne),frost=Level(SurvivorUpgradeId.Frostbite);
                if(victim.IsTargetable&&(weaken>0||air>0||frost>0)){
                    var status=victim.GetComponent<SurvivorEnemyStatus>();if(!status)status=victim.gameObject.AddComponent<SurvivorEnemyStatus>();
                    if(weaken>0)status.Weaken(Settings.rare.weaken.damageReduction.At(weaken),Settings.rare.weaken.duration);
                    if(air>0)status.RecordHit(this,air);
                    if(frost>0){var rule=Settings.epic.frostbite;if(status.Chill(rule.slowPercent,rule.damageTakenPercent.At(frost),rule.duration,rule.tint))combat.PlayFrostbite(victim.transform.position);}
                }
                int burn=Level(SurvivorUpgradeId.Burn);
                if(burn>0&&!victim.IsBurnDamage&&victim.IsTargetable){var b=Settings.common.ignite;victim.ApplyBurn(b.flatDamage.At(burn)+b.enemyMaxHealthPercent*.01f*victim.MaxHealth,Mathf.Max(.1f,b.tickInterval),Mathf.Max(.01f,b.duration),gameObject);}
                if(basic){
                    int armor=Level(SurvivorUpgradeId.ArmorStrike);
                    if(armor>0&&victim.IsTargetable)victim.DealDamage(Settings.common.armoredStrike.flatDamage.At(armor)+Settings.common.armoredStrike.armorPercent*.01f*health.Armor+Settings.common.armoredStrike.magicResistancePercent*.01f*health.MagicResistance,CombatDamageType.Physical,gameObject);

                }
                int orb=Level(SurvivorUpgradeId.OrbBarrage);
                if(orb>0&&!victim.IsAbilityProcDamage&&++orbHits>=Mathf.Max(1,Settings.common.orbBarrage.hitsRequired)){
                    orbHits=0;var rule=Settings.common.orbBarrage;Targets(orbTargets??=new(),transform.position,rule.range);
                    for(int n=0;n<Mathf.Clamp(rule.orbCount,1,99)&&orbTargets.Count>0;n++){
                        var other=orbTargets[Random.Range(0,orbTargets.Count)];
                        combat.Launch(other,rule.damage.At(orb),true,CombatDamageType.Physical,false,combat.BonusProjectileOrigin(n,Mathf.Clamp(rule.orbCount,1,99),victim),true);
                    }
                }
            }finally{processing=false;}
        }
        public void ProjectileHit(CharacterHealth target,float originalDamage,float dealt,bool secondary,bool canBounce,Vector3 hitPoint)
        {
            if(!secondary)health.Heal(dealt*(stats?stats.UpgradeBonus(SurvivorUpgradeId.LifeSteal):0)*.01f);
            if(!secondary&&canBounce){
                int split=Level(SurvivorUpgradeId.SplitShot);
                if(split>0){var other=Nearest(hitPoint,combat.AttackRange,target);if(other)combat.Launch(other,Settings.common.chainShot.baseDamagePercent*.01f*combat.BaseDamage+Settings.common.chainShot.flatDamage.At(split),true,CombatDamageType.Physical,false,hitPoint);}
            }
            int lv=Level(SurvivorUpgradeId.Ricochet);
            if(lv>0&&canBounce){var other=Nearest(hitPoint,combat.AttackRange,target);if(other)combat.Launch(other,Settings.uncommon.ricochet.baseDamagePercent*.01f*combat.BaseDamage+Settings.uncommon.ricochet.flatDamage.At(lv),true,CombatDamageType.Physical,false,hitPoint);}
        }
        private void OnDeath(CharacterHealth victim,GameObject source)
        {
            // A dead attacker keeps its granted stack until the normal decay window.

            if(!isActiveAndEnabled||!victim||!victim.IsMonster||source!=gameObject||!stats)return;
            var monster=victim.GetComponent<MushroomMonster>();
            if((monster&&monster.IsBoss)||victim.GetComponent<GodBossController>())stats.RecordBossKill();
        }
        public static CharacterHealth Nearest(Vector3 point,float range,CharacterHealth exclude)
        {
            CharacterHealth result=null;float best=range*range;
            foreach(var t in CharacterHealth.ActiveMonsters){if(!t||t==exclude||!t.isActiveAndEnabled||!t.IsTargetable)continue;float d=(t.transform.position-point).sqrMagnitude;if(d<best){best=d;result=t;}}
            return result;
        }
        private static void Targets(List<CharacterHealth> list,Vector3 point,float range,bool planar=false)
        {
            list.Clear();foreach(var target in CharacterHealth.ActiveMonsters)if(target&&target.isActiveAndEnabled&&target.IsTargetable&&(planar?Vector3.ProjectOnPlane(target.transform.position-point,Vector3.up):target.transform.position-point).sqrMagnitude<=range*range)list.Add(target);
        }
        private void OnDestroy(){DestroyCube();if(auraEffect)Destroy(auraEffect);if(shieldEffect)Destroy(shieldEffect);}
    }
}
