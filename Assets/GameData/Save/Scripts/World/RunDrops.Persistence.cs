using System;
using System.Collections.Generic;
using UnityEngine;
using MiningSimulator.Ores;
namespace GameData.Player.Script
{
    public sealed partial class SurvivorEnemyStatus
    {
        [Serializable] public sealed class SavedCombo {public string owner;public List<float> hitAges=new();}
        public List<SavedCombo> CaptureCombos(){var saved=new List<SavedCombo>();foreach(var c in combos){if(!c.Key)continue;var s=new SavedCombo{owner=RunStateFields.Id(c.Key.GetComponent<CharacterHealth>())};foreach(float hit in c.Value.hits)s.hitAges.Add(Time.time-hit);saved.Add(s);}return saved;}
        public void RestoreCombos(List<SavedCombo> saved,Dictionary<string,CharacterHealth> actors){combos.Clear();if(saved==null)return;foreach(var s in saved){if(s.owner==null||!actors.TryGetValue(s.owner,out var h))continue;var ability=h.GetComponent<SurvivorAbilities>();if(!ability)continue;var c=new Combo();foreach(var age in s.hitAges)c.hits.Enqueue(Time.time-age);combos[ability]=c;}}
    }
}
namespace MiningSimulator.Ores
{
    public sealed partial class MonsterItemPickup
    {
        [Serializable] public sealed class SavedDrop {public string settings,item;public int count;public Vector3 position,velocity;public Quaternion rotation;public float groundY,landedAge,pullSpeed;public bool landed;}
        public SavedDrop CaptureRuntime()=>new(){settings=settings?settings.name:null,item=item?item.ItemId:null,count=amount,position=transform.position,rotation=transform.rotation,velocity=velocity,groundY=groundY,landedAge=Time.time-landedTime,pullSpeed=pullSpeed,landed=landed};
        public static void RestoreRuntime(SavedDrop s,CharacterHealth player){
            var inventory=UnityEngine.Object.FindAnyObjectByType<MiningItemSystem>();if(!inventory)return;
            var database=RunStateFields.Get<MiningItemDatabase>(inventory,"database");var item=database?database.FindById(s.item):null;
            var shared=MonsterLootSettings.Current;
            MonsterRewardData settings=shared&&shared.shared&&shared.shared.name==s.settings?shared.shared:null;foreach(var asset in Resources.FindObjectsOfTypeAll<MonsterRewardData>())if(asset.name==s.settings){settings=asset;break;}
            if(!item||!settings)throw new InvalidOperationException("Missing saved loot asset: "+s.item+" / "+s.settings);
            // No drop/pickup sound during loading.
            Spawn(settings,new MonsterItemDrop{item=item},s.count,s.position,player,inventory,~0,null);
            MonsterItemPickup pickup=null;foreach(var p in UnityEngine.Object.FindObjectsByType<MonsterItemPickup>(FindObjectsSortMode.None))if(p.item==item&&p.transform.position==s.position&&p.audio==null)pickup=p;
            if(!pickup)return;pickup.audio=UnityEngine.Object.FindAnyObjectByType<MiningAudioManager>();pickup.transform.rotation=s.rotation;pickup.velocity=s.velocity;pickup.groundY=s.groundY;pickup.landedTime=Time.time-s.landedAge;pickup.pullSpeed=s.pullSpeed;pickup.landed=s.landed;
        }
    }
    public sealed partial class WorldItemPickup
    {
        [Serializable] public sealed class SavedDrop {public string item;public Vector3 position,velocity,angular;public Quaternion rotation;public float age,retry;public int bounces;public bool landed,kinematic;}
        public SavedDrop CaptureRuntime()=>new(){item=item?item.ItemId:null,position=transform.position,rotation=transform.rotation,velocity=body&&!body.isKinematic?body.linearVelocity:Vector3.zero,angular=body&&!body.isKinematic?body.angularVelocity:Vector3.zero,age=Time.time-spawnedAt,retry=nextPickupAttempt-Time.time,bounces=remainingBounces,landed=hasLanded,kinematic=body&&body.isKinematic};
        public static void RestoreRuntime(SavedDrop s){var inventory=UnityEngine.Object.FindAnyObjectByType<MiningItemSystem>();if(!inventory)return;var database=RunStateFields.Get<MiningItemDatabase>(inventory,"database");var item=database?database.FindById(s.item):null;if(!item)throw new InvalidOperationException("Missing saved world item: "+s.item);var go=new GameObject("Dropped "+item.DisplayName);go.transform.SetPositionAndRotation(s.position,s.rotation);var p=go.AddComponent<WorldItemPickup>();p.Initialize(inventory,item,database);p.spawnedAt=Time.time-s.age;p.nextPickupAttempt=Time.time+s.retry;p.remainingBounces=s.bounces;p.hasLanded=s.landed;if(p.body){p.body.isKinematic=s.kinematic;if(!s.kinematic){p.body.linearVelocity=s.velocity;p.body.angularVelocity=s.angular;}}}
    }
}
