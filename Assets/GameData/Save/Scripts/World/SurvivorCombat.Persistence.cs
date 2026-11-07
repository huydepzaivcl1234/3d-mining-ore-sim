using System;
using System.Collections.Generic;
using UnityEngine;
using MiningSimulator.Ores;
namespace GameData.Player.Script
{
    public sealed partial class SurvivorAutoCombat
    {
        [Serializable] public sealed class SavedBolt {public string target;public Vector3 position,scale;public Quaternion rotation;public float damage,remaining;public bool secondary,canBounce,abilityProc;public CombatDamageType type;public ProjectileElement element;}
        public List<SavedBolt> CaptureBolts(){var list=new List<SavedBolt>();foreach(var b in bolts)if(b.active)list.Add(new(){target=RunStateFields.Id(b.target),position=b.transform.position,rotation=b.transform.rotation,scale=b.transform.localScale,damage=b.damage,remaining=b.remaining,secondary=b.secondary,canBounce=b.canBounce,abilityProc=b.abilityProc,type=b.type,element=b.element});return list;}
        public void RestoreBolts(List<SavedBolt> saved,Dictionary<string,CharacterHealth> actors){
            ClearBolts();if(saved==null)return;
            foreach(var s in saved){if(string.IsNullOrEmpty(s.target)||!actors.TryGetValue(s.target,out var target))continue;
                // Rent through the normal factory; do not play launch sounds or apply damage during load.
                Launch(target,s.damage,s.secondary,s.type,s.canBounce,s.position,s.abilityProc,s.element);
                var b=bolts.FindLast(b=>b.active&&b.spawnFrame==Time.frameCount&&b.target==target&&b.transform.position==s.position);
                if(b!=null){b.remaining=s.remaining;b.transform.SetPositionAndRotation(s.position,s.rotation);b.transform.localScale=s.scale;}
            }
        }
    }
    public sealed partial class SurvivorAbilities
    {
        [Serializable] public sealed class SavedCube {public string target;public Vector3 position,scale,destination;public Quaternion rotation;public float remaining,damage,radius;public CombatDamageType type;}
        [Serializable] public sealed class TimedAttacker {public string id;public float age;}
        [Serializable] public sealed class SavedAbilityVisuals {public SwordState sword;public bool charging;public Quaternion chargeRotation;public List<SavedCube> shots=new();public List<TimedAttacker> attackers=new();}
        public SavedAbilityVisuals CaptureRuntimeVisuals(){
            var s=new SavedAbilityVisuals{sword=JsonUtility.FromJson<SwordState>(JsonUtility.ToJson(swordState)),charging=chargingCube&&chargingCube.activeSelf,chargeRotation=chargingCube?chargingCube.transform.rotation:Quaternion.identity};
            foreach(var shot in cubeShots)if(shot.active&&shot.visual)s.shots.Add(new(){target=RunStateFields.Id(shot.target),position=shot.visual.transform.position,rotation=shot.visual.transform.rotation,scale=shot.visual.transform.localScale,destination=shot.destination,remaining=shot.remaining,damage=shot.damage,radius=shot.radius,type=shot.type});
            foreach(var a in attackers)s.attackers.Add(new(){id=RunStateFields.Id(a.Key),age=Time.time-a.Value});return s;
        }
        public void RestoreRuntimeVisuals(SavedAbilityVisuals s,Dictionary<string,CharacterHealth> actors){
            if(s==null)return;swordState=s.sword??new SwordState();RestoreSwordHits(actors);RenderSword();attackers.Clear();foreach(var a in s.attackers)if(a.id!=null&&actors.TryGetValue(a.id,out var h))attackers[h]=Time.time-a.age;
            SurvivorLoopVfx.Set(ref auraEffect,Settings.uncommon.aura.visual,transform,Level(SurvivorUpgradeId.Aura)>0,AuraRadius/Mathf.Max(.01f,Settings.uncommon.aura.visual.referenceRadius));
            SurvivorLoopVfx.Set(ref shieldEffect,Settings.uncommon.shield.visual,transform,shield>0||Invulnerable);
            foreach(var shot in cubeShots){shot.active=false;if(shot.visual)shot.visual.SetActive(false);}
            if(s.charging){if(!chargingCube)chargingCube=CreateCube("Arcane Cube - charging");chargingCube.SetActive(true);chargingCube.transform.SetPositionAndRotation(transform.TransformPoint(Settings.epic.chargedCube.chargeOffset),s.chargeRotation);chargingCube.transform.localScale=Vector3.one*(Mathf.Lerp(Settings.epic.chargedCube.startingSize,CubeFullSize,CubeChargeProgress)/Mathf.Max(1,Settings.epic.chargedCube.chargeSizeDivisor));}
            foreach(var saved in s.shots){var shot=cubeShots.Find(c=>!c.active);if(shot==null){shot=new CubeShot{visual=CreateCube("Arcane Cube - projectile",true)};cubeShots.Add(shot);}shot.active=true;shot.target=saved.target!=null&&actors.TryGetValue(saved.target,out var target)?target:null;shot.destination=saved.destination;shot.remaining=saved.remaining;shot.damage=saved.damage;shot.radius=saved.radius;shot.type=saved.type;shot.visual.SetActive(true);shot.visual.transform.SetPositionAndRotation(saved.position,saved.rotation);shot.visual.transform.localScale=saved.scale;}
        }
    }
}
