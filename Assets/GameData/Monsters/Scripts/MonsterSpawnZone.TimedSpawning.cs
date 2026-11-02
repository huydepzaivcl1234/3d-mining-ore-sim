using UnityEngine;
namespace MiningSimulator.Ores
{
    public sealed partial class MonsterSpawnZone
    {
        private float spawnElapsedSeconds,spawnCredit=1,nextTimedRetry;
        public float SpawnElapsedSeconds=>spawnElapsedSeconds;
        public float CurrentSpawnsPerSecond=>spawnSettings?spawnSettings.RateAtSeconds(spawnElapsedSeconds):Mathf.Min(5,.5f+spawnElapsedSeconds/240f);
        private void TickTimedSpawns(float seconds)
        {
            if(seconds<=0)return;
            int batch=spawnSettings?Mathf.Clamp(spawnSettings.maximumSpawnsPerFrame,1,16):4;
            spawnCredit=Mathf.Min(batch,spawnCredit+seconds*CurrentSpawnsPerSecond);
            if(spawnCredit<1)return;
            if(Time.time<nextTimedRetry){spawnCredit=Mathf.Min(1,spawnCredit);return;}
            if(LivingCountOnField()>=LivingMonsterLimit){spawnCredit=Mathf.Min(1,spawnCredit);nextTimedRetry=Time.time+.25f;return;}
            bool invasion=CurrentDailyEvent==DailyEncounterEvent.BossInvasion;
            for(int i=0;i<batch&&spawnCredit>=1;i++){
                var entry=RollMonsterEntry(invasion);
                if(entry==null||!SpawnOne(entry,invasion,invasion)){
                    // A full/blocked area must not build a large delayed spawn burst.
                    spawnCredit=Mathf.Min(1,spawnCredit);nextTimedRetry=Time.time+.25f;break;
                }
                spawnCredit-=1;
            }
        }
    }
}

