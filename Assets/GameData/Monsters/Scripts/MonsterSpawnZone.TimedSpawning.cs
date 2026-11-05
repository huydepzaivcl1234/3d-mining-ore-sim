using UnityEngine;
namespace MiningSimulator.Ores
{
    public sealed partial class MonsterSpawnZone
    {
        private float spawnElapsedSeconds,spawnCredit,nextTimedRetry;
        private bool initialMonsterSpawned;
        private int spawnKills;
        public float SpawnElapsedSeconds=>spawnElapsedSeconds;
        public int SpawnKills=>spawnKills;
        public float CurrentSpawnInterval=>spawnSettings?spawnSettings.IntervalAfterKills(spawnKills):Mathf.Max(2f,8f-(spawnKills/10)*.5f);
        public float CurrentSpawnsPerSecond=>initialMonsterSpawned&&spawnKills==0?0:1f/CurrentSpawnInterval;
        private void TickTimedSpawns(float seconds)
        {
            if(seconds<=0||Time.time<nextTimedRetry)return;
            if(initialMonsterSpawned){
                if(spawnKills==0)return;
                spawnCredit+=seconds;
                if(spawnCredit<CurrentSpawnInterval)return;
            }
            // Failed/blocked spawns never build a catch-up burst.
            if(LivingCountOnField()>=LivingMonsterLimit){spawnCredit=0;nextTimedRetry=Time.time+.25f;return;}
            bool invasion=spawnKills>0&&CurrentDailyEvent==DailyEncounterEvent.BossInvasion;
            var entry=RollMonsterEntry(invasion);
            if(entry==null||!SpawnOne(entry,invasion,invasion)){
                spawnCredit=0;nextTimedRetry=Time.time+.25f;return;
            }
            initialMonsterSpawned=true;spawnCredit=0;
        }
    }
}
