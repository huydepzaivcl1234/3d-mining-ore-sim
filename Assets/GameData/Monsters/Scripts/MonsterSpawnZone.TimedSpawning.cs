using UnityEngine;
namespace MiningSimulator.Ores
{
    public sealed partial class MonsterSpawnZone
    {
        private float spawnElapsedSeconds,spawnCredit=1,nextTimedRetry;
        public float SpawnElapsedSeconds=>spawnElapsedSeconds;
        public float CurrentSpawnsPerSecond=>spawnSettings?spawnSettings.RateAtSeconds(spawnElapsedSeconds,playerStats?playerStats.Level:1):.5f+Mathf.Max(0,spawnElapsedSeconds-60f)*.15f/60f+.15f*Mathf.Max(0,(playerStats?playerStats.Level:1)-1);
        private void TickTimedSpawns(float seconds)
        {
            if(seconds<=0)return;
            spawnCredit+=seconds*CurrentSpawnsPerSecond;
            int group=spawnSettings?spawnSettings.GroupSizeAtLevel(playerStats?playerStats.Level:1):3;
            if(spawnCredit<group)return;
            if(Time.time<nextTimedRetry){spawnCredit=Mathf.Min(1,spawnCredit);return;}
            if(LivingCountOnField()>=LivingMonsterLimit){spawnCredit=Mathf.Min(1,spawnCredit);nextTimedRetry=Time.time+.25f;return;}
            bool invasion=CurrentDailyEvent==DailyEncounterEvent.BossInvasion;
            while(spawnCredit>=group){
                for(int member=0;member<group;member++){
                var entry=RollMonsterEntry(invasion);
                if(entry==null||!SpawnOne(entry,invasion,invasion)){
                    // A full/blocked area must not build a large delayed spawn burst.
                    spawnCredit=Mathf.Min(1,spawnCredit);nextTimedRetry=Time.time+.25f;return;
                }
                spawnCredit-=1;
                }
            }
        }
    }
}


