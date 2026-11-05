using System.Collections.Generic;
using UnityEngine;
using MiningSimulator.Ores;

namespace GameData.Player.Script
{
    [DisallowMultipleComponent,DefaultExecutionOrder(200)]
    public sealed partial class SurvivorEnemyStatus : MonoBehaviour
    {
        private CharacterHealth health;
        private CharacterController motor;
        private GodBossController god;
        public float ControlDurationMultiplier=>1-(god&&god.Data?Mathf.Clamp01(god.Data.crowdControlResistancePercent*.01f):0);
        private float weakRemaining,damageReduction,airTime,airDuration,airStartY,landingDamage,airHeight;
        private GameObject source;
        private float airCooldownRemaining,airCooldownOnLanding;
        public float AirborneCooldownRemaining=>airCooldownRemaining;
        private sealed class Combo {public readonly Queue<float> hits=new();}
        private readonly Dictionary<SurvivorAbilities,Combo> combos=new();
        private float frostRemaining,frostSlow,frostVulnerability;
        private Renderer[] frostRenderers;
        private MaterialPropertyBlock[] frostOriginal;
        private MaterialPropertyBlock frostBlock;
        public Color FrostTint {get;private set;}=Color.white;
        public bool Frosted=>frostRemaining>0;
        public float MovementMultiplier=>Frosted?1-Mathf.Clamp01(frostSlow*.01f):1;
        public float DamageTakenMultiplier=>1+(Frosted?Mathf.Max(0,frostVulnerability)*.01f:0);
        public bool Chill(float slow,float vulnerability,float duration,Color tint)
        {
            duration*=ControlDurationMultiplier;
            if(Frosted||duration<=0)return false;
            bool began=true;
            FrostTint=tint;frostSlow=Mathf.Clamp(slow,0,100);frostVulnerability=Mathf.Max(0,vulnerability);frostRemaining=duration;
            if(frostRenderers!=null)return began;
            var renderers=new List<Renderer>();foreach(var r in GetComponentsInChildren<Renderer>())if((r is MeshRenderer||r is SkinnedMeshRenderer)&&!r.GetComponent<TMPro.TMP_Text>())renderers.Add(r);
            frostRenderers=renderers.ToArray();frostOriginal=new MaterialPropertyBlock[frostRenderers.Length];frostBlock=new MaterialPropertyBlock();
            for(int i=0;i<frostRenderers.Length;i++){
                var r=frostRenderers[i];frostOriginal[i]=new MaterialPropertyBlock();r.GetPropertyBlock(frostOriginal[i]);r.GetPropertyBlock(frostBlock);
                frostBlock.SetColor("_BaseColor",tint);frostBlock.SetColor("_Color",tint);r.SetPropertyBlock(frostBlock);
            }
            return began;
        }
        private void ClearFrost(){if(frostRenderers!=null)for(int i=0;i<frostRenderers.Length;i++)if(frostRenderers[i])frostRenderers[i].SetPropertyBlock(frostOriginal[i].isEmpty?null:frostOriginal[i]);frostRenderers=null;frostOriginal=null;frostSlow=frostVulnerability=frostRemaining=0;}
        public float DamageReduction=>weakRemaining>0?damageReduction:0;
        public bool Airborne=>airDuration>0;
        private void Awake(){health=GetComponent<CharacterHealth>();if(health&&health.CrowdOwner)health.CrowdStatus=this;motor=GetComponent<CharacterController>();god=GetComponent<GodBossController>();}
        public void Weaken(float amount,float duration){duration*=ControlDurationMultiplier;if(weakRemaining>0||duration<=0)return;damageReduction=Mathf.Max(0,amount);weakRemaining=duration;}
        public void RecordHit(SurvivorAbilities owner,int level)
        {
            if(!owner||Airborne||airCooldownRemaining>0||!health||!health.IsTargetable)return;
            float control=ControlDurationMultiplier;if(control<=0)return;
            var rule=owner.Settings.uncommon.airborne;
            if(!combos.TryGetValue(owner,out var combo)){combo=new Combo();combos.Add(owner,combo);}
            while(combo.hits.Count>0&&Time.time-combo.hits.Peek()>Mathf.Max(.01f,rule.hitWindow))combo.hits.Dequeue();
            combo.hits.Enqueue(Time.time);
            if(combo.hits.Count<Mathf.Max(1,rule.requiredHits))return;
            combos.Clear();airCooldownOnLanding=Mathf.Max(0,rule.cooldown);airTime=0;airDuration=Mathf.Max(.001f,rule.flightDuration*control);airHeight=Mathf.Max(0,rule.height*control);airStartY=transform.position.y;
            landingDamage=health.MaxHealth*rule.enemyMaxHealthPercent*.01f+rule.landingDamage.At(level);source=owner.gameObject;
        }
        private void Update()
        {
            if(health&&health.CrowdOwner&&health.CrowdOwner.IsPaused(health.CrowdSlot)||WorldActorVisibility.ShouldPause(this))return;
            float dt=Time.deltaTime;airCooldownRemaining=Mathf.Max(0,airCooldownRemaining-dt);if(Frosted){frostRemaining=Mathf.Max(0,frostRemaining-dt);if(frostRemaining==0)ClearFrost();}weakRemaining=Mathf.Max(0,weakRemaining-dt);
            if(!Airborne)return;
            if(!health||!health.IsTargetable){RestoreHeight();return;}
            airTime=Mathf.Min(airDuration,airTime+dt);float t=airTime/airDuration;
            float y=airStartY+4*airHeight*t*(1-t);Vector3 delta=Vector3.up*(y-transform.position.y);
            if(motor&&motor.enabled)motor.Move(delta);else transform.position+=delta;
            if(t<1)return;
            airDuration=0;airCooldownRemaining=airCooldownOnLanding;health.DealPeriodicDamage(landingDamage,CombatDamageType.Physical,source);
        }
        private void RestoreHeight(){if(Airborne){var p=transform.position;p.y=airStartY;transform.position=p;}airDuration=airTime=0;}
        private void OnDisable(){ClearFrost();RestoreHeight();weakRemaining=damageReduction=airCooldownRemaining=airCooldownOnLanding=0;combos.Clear();source=null;}
    }
}
