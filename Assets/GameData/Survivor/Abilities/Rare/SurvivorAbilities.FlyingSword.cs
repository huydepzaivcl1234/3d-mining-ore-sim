using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using MiningSimulator.Ores;

namespace GameData.Player.Script
{
    public sealed partial class SurvivorAbilities
    {
        [Serializable] public sealed class SwordState
        {
            public int phase,sweepVersion;
            public float time,cooldown,orbit,approach,swing,recovery,damage;
            public Vector3 center,forward,start;
            public bool hit;
            public List<string> hitTargets=new();
        }
        private SwordState swordState=new();
        private GameObject flyingSword;
        private VisualEffect swordSlash;
        private float swordVfxTime;
        private readonly List<CharacterHealth> swordVictims=new();
        private readonly HashSet<CharacterHealth> swordHitTargets=new();
        public int SwordSlashes {get;private set;}
        public int SwordHits {get;private set;}
        public float SwordDamage=>Settings.rare.flyingSword.flatDamage.At(Level(SurvivorUpgradeId.FlyingSword))+combat.EffectiveDamage*Settings.rare.flyingSword.attackDamagePercent*.01f;
        public float SwordAttacksPerSecond=>combat.ShotsPerSecond*Settings.rare.flyingSword.attackSpeedPercent*.01f;

        private void EnsureSword()
        {
            var rule=Settings.rare.flyingSword;
            if(!flyingSword&&rule.prefab){flyingSword=Instantiate(rule.prefab);flyingSword.name="Spectral Sword";}
            if(!swordSlash&&rule.slashPrefab){var go=Instantiate(rule.slashPrefab);go.name="Sword slash VFX";swordSlash=go.GetComponent<VisualEffect>();if(swordSlash){swordSlash.pause=true;go.SetActive(false);}}
        }
        private Vector3 SwordIdlePosition=>transform.position+Vector3.up*Settings.rare.flyingSword.orbitHeight+Quaternion.Euler(0,swordState.orbit,0)*Vector3.forward*Settings.rare.flyingSword.orbitRadius;
        private Vector3 SwordRadial(float t)=>Quaternion.AngleAxis(Mathf.Lerp(-Settings.rare.flyingSword.cleaveDegrees*.5f,Settings.rare.flyingSword.cleaveDegrees*.5f,t),Vector3.up)*swordState.forward;
        private void RenderSword()
        {
            if(Level(SurvivorUpgradeId.FlyingSword)<=0||!health||health.Health<=0)return;
            EnsureSword();if(!flyingSword)return;flyingSword.SetActive(true);
            float t=swordState.phase==1?swordState.time/Mathf.Max(.001f,swordState.approach):swordState.phase==2?swordState.time/Mathf.Max(.001f,swordState.swing):swordState.time/Mathf.Max(.001f,swordState.recovery);
            t=Mathf.Clamp01(t);var rule=Settings.rare.flyingSword;float radius=rule.cleaveRadius*.55f;
            var radial=SwordRadial(swordState.phase==1?0:swordState.phase==3?1:t);var slashPosition=swordState.center+radial*radius;
            var idleRotation=Quaternion.Euler(12,swordState.orbit,-25);
            var slashRotation=Quaternion.LookRotation(Vector3.up,radial);
            flyingSword.transform.localScale=Vector3.one*(rule.cleaveRadius*.5f);
            if(swordState.phase==0)flyingSword.transform.SetPositionAndRotation(SwordIdlePosition,idleRotation);
            else if(swordState.phase==1)flyingSword.transform.SetPositionAndRotation(Vector3.Lerp(swordState.start,slashPosition,Mathf.SmoothStep(0,1,t)),Quaternion.Slerp(idleRotation,slashRotation,t));
            else if(swordState.phase==2)flyingSword.transform.SetPositionAndRotation(slashPosition,slashRotation);
            else flyingSword.transform.SetPositionAndRotation(Vector3.Lerp(swordState.start,SwordIdlePosition,Mathf.SmoothStep(0,1,t)),Quaternion.Slerp(slashRotation,idleRotation,t));
            if(!swordSlash)return;
            if(swordState.phase!=2){swordSlash.gameObject.SetActive(false);return;}
            swordSlash.transform.SetPositionAndRotation(swordState.center,Quaternion.LookRotation(swordState.forward,Vector3.up)*Quaternion.Euler(0,rule.slashYawOffset,0));
            swordSlash.transform.localScale=Vector3.one*(rule.cleaveRadius/Mathf.Max(.01f,rule.slashReferenceRadius));
            float visualTime=t*(rule.cleaveDegrees/360f)/1.8f;
            if(!swordSlash.gameObject.activeSelf||visualTime<swordVfxTime){swordSlash.gameObject.SetActive(true);swordSlash.SetFloat("SlashSpeed",1.8f);swordSlash.pause=true;swordSlash.Reinit();swordVfxTime=0;}
            if(visualTime>swordVfxTime){float delta=visualTime-swordVfxTime;uint steps=(uint)Mathf.Max(1,Mathf.CeilToInt(delta*120));swordSlash.Simulate(delta/steps,steps);swordVfxTime=visualTime;}
        }
        private void TickSword(float dt)
        {
            if(Level(SurvivorUpgradeId.FlyingSword)<=0){ResetSword();return;}
            dt=Mathf.Max(0,dt);swordState.orbit=Mathf.Repeat(swordState.orbit+70*dt,360);swordState.cooldown=Mathf.Max(0,swordState.cooldown-dt);
            var rule=Settings.rare.flyingSword;
            if(swordState.phase==0&&swordState.cooldown<=0&&SwordAttacksPerSecond>0){
                var target=Nearest(transform.position,combat.AttackRange,null);
                if(target){
                    float interval=1/Mathf.Max(.001f,SwordAttacksPerSecond);swordState.cooldown=interval;
                    swordState.approach=Mathf.Min(rule.approachSeconds,interval*.15f);swordState.swing=Mathf.Min(rule.swingSeconds,interval*.65f);swordState.recovery=Mathf.Min(rule.returnSeconds,interval*.2f);
                    var delta=Vector3.ProjectOnPlane(target.transform.position-transform.position,Vector3.up);swordState.forward=delta.sqrMagnitude>.0001f?delta.normalized:transform.forward;
                    swordState.center=target.transform.position+Vector3.up*.8f;swordState.start=flyingSword?flyingSword.transform.position:SwordIdlePosition;swordState.phase=1;swordState.time=0;swordState.hit=false;swordState.damage=SwordDamage;swordState.sweepVersion=1;swordState.hitTargets.Clear();swordHitTargets.Clear();SwordSlashes++;
                }
            }
            if(swordState.phase!=0){
                float previous=swordState.phase==2?swordState.time:0;swordState.time+=dt;
                if(swordState.phase==1&&swordState.time>=swordState.approach){swordState.time-=swordState.approach;swordState.phase=2;}
                if(swordState.phase==2){
                    // Substeps cover the complete blade sweep even at a low frame rate.
                    if(!(swordState.sweepVersion==0&&swordState.hit))SweepSword(Mathf.Clamp01(previous/Mathf.Max(.001f,swordState.swing)),Mathf.Clamp01(swordState.time/Mathf.Max(.001f,swordState.swing)));
                    if(swordState.time>=swordState.swing){swordState.time-=swordState.swing;swordState.start=swordState.center+SwordRadial(1)*rule.cleaveRadius*.55f;swordState.phase=3;}
                }
                if(swordState.phase==3&&swordState.time>=swordState.recovery){swordState.phase=0;swordState.time=0;}
            }
            RenderSword();
        }
        private void RestoreSwordHits(Dictionary<string,CharacterHealth> actors)
        {
            swordHitTargets.Clear();swordState.hitTargets??=new();foreach(var id in swordState.hitTargets)if(id!=null&&actors.TryGetValue(id,out var target)&&target)swordHitTargets.Add(target);
            if(swordSlash)swordSlash.gameObject.SetActive(false);
        }
        private void SweepSword(float from,float to)
        {
            var rule=Settings.rare.flyingSword;int steps=Mathf.Max(1,Mathf.CeilToInt((to-from)*rule.cleaveDegrees/5));swordVictims.Clear();
            foreach(var enemy in CharacterHealth.ActiveMonsters)if(enemy&&enemy.IsTargetable&&enemy.isActiveAndEnabled&&!swordHitTargets.Contains(enemy))swordVictims.Add(enemy);
            foreach(var enemy in swordVictims){
                var collider=enemy.GetComponent<Collider>();var center=collider?collider.bounds.center:enemy.transform.position+Vector3.up*.7f;
                float bodyRadius=collider?collider.bounds.extents.magnitude:.45f*Mathf.Max(.1f,enemy.transform.lossyScale.x);
                if((center-swordState.center).sqrMagnitude>Mathf.Pow(rule.cleaveRadius+bodyRadius+rule.bladeThickness,2))continue;
                for(int i=0;i<=steps;i++){
                    Vector3 radial=SwordRadial(Mathf.Lerp(from,to,(float)i/steps)),heel=swordState.center+radial*(rule.cleaveRadius*.1f);
                    var blade=heel+Vector3.Project(center-heel,radial);blade=heel+radial*Mathf.Clamp(Vector3.Dot(blade-heel,radial),0,rule.cleaveRadius*.9f);
                    var contact=collider?collider.ClosestPoint(blade):center+Vector3.ClampMagnitude(blade-center,bodyRadius);
                    if((contact-blade).sqrMagnitude>rule.bladeThickness*rule.bladeThickness)continue;
                    swordHitTargets.Add(enemy);swordState.hitTargets.Add(RunStateFields.Id(enemy));swordState.hit=true;
                    float dealt=enemy.DealDamageWithTrueBonus(swordState.damage,CombatDamageType.Magic,gameObject,0,true,false,true);
                    if(dealt>0){SwordHits++;combat.PlaySwordImpact(enemy,contact,Vector3.Cross(Vector3.up,radial),rule.slashColor);}break;
                }
            }
        }
        private void ResetSword(){swordState.phase=0;swordState.time=0;swordState.cooldown=0;swordState.hit=false;swordState.hitTargets?.Clear();swordHitTargets.Clear();if(flyingSword)flyingSword.SetActive(false);if(swordSlash)swordSlash.gameObject.SetActive(false);}
        private void DestroySword(){if(flyingSword)Destroy(flyingSword);if(swordSlash)Destroy(swordSlash.gameObject);}
    }
}