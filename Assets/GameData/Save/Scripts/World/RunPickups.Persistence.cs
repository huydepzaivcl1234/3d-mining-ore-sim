using System;
using UnityEngine;
namespace MiningSimulator.Ores
{
    public sealed partial class MiningWorldCard
    {
        [Serializable] public sealed class SavedCard {public int tier;public Vector3 position,scale,velocity,angular;public Quaternion rotation;public bool kinematic,landed,attracting;public Vector3 groundAnchor,landingPosition;public float ready,age,halfHeight;}
        public SavedCard CaptureRuntime(){int index=ReferenceEquals(tier,data.abilityCard)?-1:Array.IndexOf(data.tiers,tier);var body=GetComponent<Rigidbody>();return new(){tier=index,position=transform.position,rotation=transform.rotation,scale=transform.localScale,velocity=body&&!body.isKinematic?body.linearVelocity:Vector3.zero,angular=body&&!body.isKinematic?body.angularVelocity:Vector3.zero,kinematic=body&&body.isKinematic,landed=landed,attracting=attracting,groundAnchor=groundAnchor,landingPosition=landingPosition,ready=ready-Time.time,age=Time.time-landedAt,halfHeight=halfHeight};}
        public static void RestoreRuntime(MiningCardSystem system,SavedCard s){
            var data=RunStateFields.Get<MiningCardData>(system,"data");if(!data)return;var tier=s.tier<0?data.abilityCard:s.tier<data.tiers.Length?data.tiers[s.tier]:null;if(tier==null)return;
            var go=system.Spawn(tier,s.position);if(!go)return;var c=go.GetComponent<MiningWorldCard>();go.transform.SetPositionAndRotation(s.position,s.rotation);go.transform.localScale=s.scale;
            c.landed=s.landed;c.attracting=s.attracting;c.groundAnchor=s.groundAnchor;c.landingPosition=s.landingPosition;c.ready=Time.time+s.ready;c.landedAt=Time.time-s.age;c.halfHeight=s.halfHeight;
            var body=go.GetComponent<Rigidbody>();body.isKinematic=s.kinematic;if(!s.kinematic){body.linearVelocity=s.velocity;body.angularVelocity=s.angular;}
            go.GetComponent<BoxCollider>().enabled=!s.landed;if(c.beam)c.beam.enabled=!s.attracting;
        }
    }
    public sealed partial class CharacterHealth
    {
        public void RestoreEffectVisuals(){
            if(burn.IsActive){var p=burnSource?burnSource.GetComponent<PlayerStats>():null;burnVisual=p?p.AbilityData.common.ignite.visual:GameData.Player.Script.SurvivorAbilityData.Fallback.common.ignite.visual;GameData.Player.Script.SurvivorLoopVfx.Set(ref burnEffect,burnVisual,transform,IsMonster&&health>0);}
        }
    }
}
namespace GameData.Player.Script
{
    public sealed partial class SurvivorLootBox
    {
        [Serializable] public sealed class SavedBox {public Vector3 position,landing;public Quaternion rotation;public bool falling;public float speed,ready;}
        public SavedBox CaptureRuntime()=>new(){position=transform.position,rotation=transform.rotation,landing=LandingPoint,falling=IsFalling,speed=fallSpeed,ready=readyAt-Time.time};
        public void RestoreRuntime(SavedBox s){transform.SetPositionAndRotation(s.position,s.rotation);LandingPoint=s.landing;IsFalling=s.falling;fallSpeed=s.speed;readyAt=Time.time+s.ready;}
    }
    public sealed partial class SurvivorDailyLoot
    {
        public void SaveRuntime(){if(saveBlocked)return;for(int i=0;i<boxes.Count&&i<progress.boxes.Count;i++)if(boxes[i])progress.boxes[i].runtime=boxes[i].CaptureRuntime();Save();}
    }
}
