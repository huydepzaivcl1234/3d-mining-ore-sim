using System.Collections.Generic;
using UnityEngine;
namespace MiningSimulator.Ores
{
    public sealed partial class SurvivorCrowd
    {
        public void CaptureWorld(MonsterSpawnZone zone,List<MonsterSpawnZone.SavedMonster> destination)
        {
            for(int i=0;i<used;i++){var a=actors[i];if(a==null||!a.living||a.zone!=zone||!a.health||a.health.Health<=0)continue;var s=new MonsterSpawnZone.SavedMonster{kind=0,prefab=a.entry.prefab.name,level=a.level,damage=a.damage,clip=a.clip,clock=a.clock,phase=a.phase,cooldown=a.cooldown,lifetime=a.lifetime};s.CaptureHealth(a.health);s.position=positions[i];destination.Add(s);}
        }
        public bool RestoreWorld(MonsterSpawnZone zone,MonsterSpawnEntry entry,MonsterSpawnZone.SavedMonster s)
        {
            if(!TrySpawn(zone,entry,s.position,true))return false;
            int i=lastSpawnedSlot;var a=actors[i];{a.level=s.level;a.damage=s.damage;a.clip=s.clip;a.clock=s.clock;a.phase=s.phase;a.cooldown=s.cooldown;a.lifetime=s.lifetime;a.scale=s.scale.x;scales[i]=a.scale;s.RestoreHealth(a.health);a.transform.SetPositionAndRotation(s.position,s.rotation);a.transform.localScale=s.scale;return true;}

        }
    }
}

