using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using GameData.Player.Script;
namespace MiningSimulator.Ores
{
    // Explicit runtime schema: never serializes asset settings or arbitrary scene components.
    // @ fields are remaining deadlines, ~ fields are ages; wall-clock time never advances effects.
    public static class RunStateFields
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        [Serializable] public sealed class Value {
            public string name,text,reference; public float number;public double precise;public int integer;public bool flag;
            public Vector3 vector;public Vector2 vector2;public Quaternion rotation;public Color color;
        }
        [Serializable] public sealed class Bag {public string type;public List<Value> values=new();}
        static readonly Dictionary<Type,string[]> Rules=new(){
            {typeof(CharacterHealth),new[]{"regenTimer","initializedMaxHealth","damageEnabled","burn","burnSource"}},
            {typeof(SurvivorEnemyStatus),new[]{"weakRemaining","damageReduction","airTime","airDuration","airStartY","landingDamage","airHeight","source","airCooldownRemaining","airCooldownOnLanding","frostRemaining","frostSlow","frostVulnerability","<FrostTint>k__BackingField"}},
            {typeof(SurvivorAbilities),new[]{"shieldLevel","incomingHits","orbHits","shield","shieldDelay","lifeTimer","auraTimer","invulnTimer","invulnerable","recoveryCooldown","recoveryRemaining","recoveryPerSecond","defenseDecay","~lastIncoming","cubeCharge"}},
            {typeof(SurvivorAutoCombat),new[]{"@nextAttack","@nextFrostSound"}},
            {typeof(AnimationRunMana),new[]{"secondsSinceRun","exhausted","<CurrentMana>k__BackingField"}},
            {typeof(GodBossBleed),new[]{"source","elapsed","duration","nextTick","percent"}},
            {typeof(BreadSugarEffect),new[]{"effect","source"}},
            {typeof(PotionPlayerEffect),new[]{"burn","source","slowRemaining","slowPercent"}},
            {typeof(BreadMonsterAbility),new[]{"healingClock","thrown"}},
            {typeof(PotionMonsterAbility),new[]{"thrown","prepared","preparedKind","preparedTarget"}},
            {typeof(MushroomMonster),new[]{"@nextAttack","@nextDecision","verticalSpeed","hitApplied","rangedStrike","secondAttack","nextSecondAttack","strikeLocked","strikeForward","strikeCenter","animationState","attackStateHash","target","~lastTargetSeen","playerDetected","lastKnownPlayer"}},
            {typeof(GodBossController),new[]{"elapsed","duration","@nextJump","@nextRoar","@nextRoarCheck","gravity","impacted","running","jumpStart","jumpTarget","waveDistance","waveCenter","waveHit","<CurrentAction>k__BackingField"}},
            {typeof(TopDownSurvivor),new[]{"cameraYaw","cameraAngle","cameraDistance","displayedYaw","displayedAngle","displayedDistance","collisionDistance","yawVelocity","pitchVelocity","zoomVelocity","collisionVelocity","smoothedFocus","previousPlayerPosition","followVelocity","initialized"}},
            {typeof(PlayerController1),new[]{"knockbackVelocity","knockbackRemaining","knockbackDuration","_xRotation","_currentVelocity"}}
        };
        public static string Id(CharacterHealth h){if(!h)return null;if(string.IsNullOrEmpty(h.WorldSaveId))h.WorldSaveId=Guid.NewGuid().ToString("N");return h.WorldSaveId;}
        public static Dictionary<string,CharacterHealth> Actors(){var result=new Dictionary<string,CharacterHealth>();foreach(var h in UnityEngine.Object.FindObjectsByType<CharacterHealth>(FindObjectsSortMode.None))result[Id(h)]=h;return result;}
        public static T Get<T>(object owner,string name){var f=owner.GetType().GetField(name,Flags);return f!=null?(T)f.GetValue(owner):default;}
        public static void Set(object owner,string name,object value){var f=owner.GetType().GetField(name,Flags);if(f!=null)f.SetValue(owner,value);}
        public static Bag Capture(object owner,params string[] fields){
            var bag=new Bag{type=owner.GetType().AssemblyQualifiedName};
            foreach(var spec in fields){string name=spec.TrimStart('@','~');var field=owner.GetType().GetField(name,Flags);if(field==null)throw new MissingFieldException(owner.GetType().Name,name);
                var value=field.GetValue(owner);var v=new Value{name=spec};var type=field.FieldType;
                if(type==typeof(float)){v.number=(float)value;if(spec[0]=='@')v.number-=Time.time;else if(spec[0]=='~')v.number=Time.time-v.number;}
                else if(type==typeof(double))v.precise=(double)value;
                else if(type==typeof(int)||type.IsEnum)v.integer=Convert.ToInt32(value);
                else if(type==typeof(bool))v.flag=(bool)value;
                else if(type==typeof(Vector3))v.vector=(Vector3)value;
                else if(type==typeof(Vector2))v.vector2=(Vector2)value;
                else if(type==typeof(Quaternion))v.rotation=(Quaternion)value;
                else if(type==typeof(Color))v.color=(Color)value;
                else if(type==typeof(DamageOverTime))v.text=JsonUtility.ToJson(value);
                else if(typeof(UnityEngine.Object).IsAssignableFrom(type)){
                    var component=value as Component;var go=value as GameObject;var h=component?component.GetComponentInParent<CharacterHealth>():go?go.GetComponentInParent<CharacterHealth>():null;v.reference=Id(h);
                }else throw new NotSupportedException("Runtime save field "+type.Name);
                bag.values.Add(v);
            }return bag;
        }
        public static void Restore(object owner,Bag bag,Dictionary<string,CharacterHealth> actors){
            foreach(var v in bag.values){string name=v.name.TrimStart('@','~');var f=owner.GetType().GetField(name,Flags);if(f==null)continue;var type=f.FieldType;object value=null;
                if(type==typeof(float)){float n=v.number;if(v.name[0]=='@')n+=Time.time;else if(v.name[0]=='~')n=Time.time-n;value=n;}
                else if(type==typeof(double))value=v.precise;else if(type==typeof(int))value=v.integer;else if(type.IsEnum)value=Enum.ToObject(type,v.integer);
                else if(type==typeof(bool))value=v.flag;else if(type==typeof(Vector3))value=v.vector;else if(type==typeof(Vector2))value=v.vector2;
                else if(type==typeof(Quaternion))value=v.rotation;else if(type==typeof(Color))value=v.color;
                else if(type==typeof(DamageOverTime)){if(!string.IsNullOrEmpty(v.text))JsonUtility.FromJsonOverwrite(v.text,f.GetValue(owner));continue;}
                else if(!string.IsNullOrEmpty(v.reference)&&actors.TryGetValue(v.reference,out var h))value=type==typeof(GameObject)?h.gameObject:type==typeof(Transform)?h.transform:h.GetComponent(type);
                f.SetValue(owner,value);
            }
        }
        public static List<Bag> CaptureComponents(GameObject go){var bags=new List<Bag>();foreach(var rule in Rules){var c=go.GetComponent(rule.Key);if(c)bags.Add(Capture(c,rule.Value));}return bags;}
        public static void RestoreComponents(GameObject go,List<Bag> bags,Dictionary<string,CharacterHealth> actors){
            if(bags==null)return;
            foreach(var bag in bags){var type=Type.GetType(bag.type);if(type==null||!Rules.ContainsKey(type))continue;var c=go.GetComponent(type);if(!c)c=go.AddComponent(type);
                if(c is SurvivorEnemyStatus status){var remaining=bag.values.Find(v=>v.name=="frostRemaining");if(remaining!=null&&remaining.number>0){var tint=bag.values.Find(v=>v.name=="<FrostTint>k__BackingField");status.Chill(0,0,remaining.number/Mathf.Max(.001f,status.ControlDurationMultiplier),tint!=null?tint.color:Color.cyan);}}
                Restore(c,bag,actors);
            }
        }
    }
}
