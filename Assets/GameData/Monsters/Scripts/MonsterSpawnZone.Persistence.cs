using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameData.Player.Script;
namespace MiningSimulator.Ores
{
    public sealed partial class MonsterSpawnZone
    {
        [Serializable] public sealed class SavedMonster
        {
            public string prefab,id;
            public int kind,level,clip,day,appearance;
            public Vector3 position,scale;
            public Quaternion rotation;
            public float health,maxHealth,armor,resistance,resistanceScale=100,damage,burnDamage,clock,phase,cooldown,lifetime;
            public bool boss,summon;
            public void CaptureHealth(CharacterHealth h){id=RunStateFields.Id(h);health=h.Health;maxHealth=h.MaxHealth;armor=h.Armor;resistance=h.MagicResistance;resistanceScale=h.ResistanceScale;position=h.transform.position;rotation=h.transform.rotation;scale=h.transform.lossyScale;}
            public void RestoreHealth(CharacterHealth h){if(!string.IsNullOrEmpty(id))h.WorldSaveId=id;h.ConfigureSpawnHealth(maxHealth);h.RestoreSavedHealth(health);h.ConfigureDefenses(armor,resistance,resistanceScale);}
        }
        [Serializable] private sealed class SavedWorld
        {
            public int version=1;
            public float elapsed,credit;
            public int spawnPacingVersion,spawnKills;public bool initialMonsterSpawned;
            public string playerId;
            public RunCheckpoint.State runtime;
            public int day,eventKind,thiefDay,thieves;
            public bool eventAnnouncement,skullRolled,skullPending;
            public Vector3 playerPosition;
            public List<SavedMonster> monsters=new();
        }
        private bool restoringWorld,worldLoadFailed;
        private static bool worldStopping;
        private static float nextWorldSave;
        public bool IsRestoringWorld=>restoringWorld;
        public int RestoredMonsterCount {get;private set;}
        public string WorldSaveKey {get {string path=transform.name;for(var p=transform.parent;p;p=p.parent)path=p.name+"/"+path;return "Survivor.WorldMonsters.v1/"+gameObject.scene.path+"/"+path;}}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetWorldSaving(){worldStopping=false;nextWorldSave=15;}
        internal static void ClearResetSchedules(){skullNights.Clear();worldStopping=false;nextWorldSave=Time.unscaledTime+15;}
        public static void CaptureWorldsIfDue(bool stopping=false,bool force=false)
        {
            if(RunCheckpoint.IsRestoring||worldStopping||!Application.isPlaying||!stopping&&!force&&Time.unscaledTime<nextWorldSave)return;
            nextWorldSave=Time.unscaledTime+15;
            bool includeRuntime=true;foreach(var zone in activeZones)if(zone&&zone.started&&!zone.restoringWorld&&!zone.worldLoadFailed){zone.SaveWorld(includeRuntime);includeRuntime=false;}
            if(stopping)worldStopping=true;
        }
        public void SaveWorld(bool includeRuntime=true)
        {
            if(restoringWorld||worldLoadFailed||!started||!player)return;
            var save=new SavedWorld{playerId=RunStateFields.Id(player),elapsed=spawnElapsedSeconds,credit=spawnCredit,spawnPacingVersion=1,spawnKills=spawnKills,initialMonsterSpawned=initialMonsterSpawned,playerPosition=player.transform.position,day=SpawnDayNumber,eventKind=(int)CurrentDailyEvent,eventAnnouncement=dailyAnnouncementPending,thiefDay=thiefDay,thieves=spawnedThieves};
            if(additionalRoster&&skullNights.TryGetValue(additionalRoster,out var roll)){save.skullRolled=roll.IsRolledFor(save.day);save.skullPending=roll.Pending;}
            if(SurvivorCrowd.Current)SurvivorCrowd.Current.CaptureWorld(this,save.monsters);
            foreach(var m in alive)if(m&&m.Health&&m.Health.Health>0&&!m.IsDespawning&&liveSpecies.TryGetValue(m,out var entry))save.monsters.Add(m.CaptureWorld(entry.prefab.name));
            foreach(var god in GetComponentsInChildren<GodBossController>())if(god.Health&&god.Health.Health>0){var record=new SavedMonster{kind=2,day=god.EncounterDay,appearance=god.AppearanceNumber};record.CaptureHealth(god.Health);save.monsters.Add(record);}
            if(includeRuntime)save.runtime=RunCheckpoint.Capture(player);
            GameSave.SetString(WorldSaveKey,JsonUtility.ToJson(save));
        }
        private void StartWorldRestore()
        {
            // Upgrade old profiles once: already-unlocked normal species are not new discoveries.
            const string migration="Survivor.DiscoveryMigrated.v1";
            if(GameSave.GetInt(migration)==0){if(playerStats&&playerStats.Level>1)foreach(var e in monsters)if(e?.prefab&&e.RequiredPlayerLevel<=playerStats.Level&&e.RequiredSpawnDay<=SpawnDayNumber)SurvivorMonsterDiscovery.Remember(e.prefab);GameSave.SetInt(migration,1);}
            string json=GameSave.GetString(WorldSaveKey);if(string.IsNullOrEmpty(json))return;
            SavedWorld saved;
            try{saved=JsonUtility.FromJson<SavedWorld>(json);if(saved==null||saved.version!=1||saved.monsters==null)throw new Exception("Unsupported monster world save");}
            catch(Exception e){worldLoadFailed=true;Debug.LogWarning("Monster world save preserved: "+e.Message);return;}
            restoringWorld=true;StartCoroutine(RestoreWorld(saved));
        }
        private IEnumerator RestoreWorld(SavedWorld saved)
        {
            float previousScale=Time.timeScale;if(saved.runtime!=null){RunCheckpoint.IsRestoring=true;Time.timeScale=0;}
            if(player&&!string.IsNullOrEmpty(saved.playerId))player.WorldSaveId=saved.playerId;
            while(!WorldNavigationGrid.Instance||!WorldNavigationGrid.Instance.HasBaked)yield return null;
            if(player){var rb=player.GetComponent<Rigidbody>();if(rb){rb.position=saved.playerPosition;rb.linearVelocity=Vector3.zero;}player.transform.position=saved.playerPosition;}
            spawnElapsedSeconds=Mathf.Max(0,saved.elapsed);
            if(saved.spawnPacingVersion==1){spawnKills=Mathf.Max(0,saved.spawnKills);initialMonsterSpawned=saved.initialMonsterSpawned;spawnCredit=Mathf.Clamp(saved.credit,0,CurrentSpawnInterval);}
            else{initialMonsterSpawned=saved.monsters.Count>0||saved.elapsed>0;spawnKills=initialMonsterSpawned&&saved.monsters.Count==0?1:0;spawnCredit=0;} // Older saves keep their monsters, discard old burst credits.
            thiefDay=saved.thiefDay;spawnedThieves=saved.thieves;
            if(saved.day==SpawnDayNumber){scheduledDay=saved.day;CurrentDailyEvent=(DailyEncounterEvent)saved.eventKind;dailyAnnouncementPending=saved.eventAnnouncement;if(additionalRoster&&saved.skullRolled){var roll=new SkullclawNightRoll();roll.Roll(saved.day,saved.skullPending?100:0,0);skullNights[additionalRoster]=roll;}}
            foreach(var record in saved.monsters){
                if(record==null||record.health<=0)continue;
                if(record.kind==2){
                    if(!godBossData||!godBossData.prefab){worldLoadFailed=true;continue;}
                    var god=Instantiate(godBossData.prefab,record.position,record.rotation,transform);god.Initialize(this,player,record.day,Mathf.Max(1,record.appearance));record.RestoreHealth(god.Health);god.transform.localScale=Vector3.Scale(record.scale,new Vector3(1/transform.lossyScale.x,1/transform.lossyScale.y,1/transform.lossyScale.z));god.Defeated+=OnGodDefeated;godBoss=god;RestoredMonsterCount++;continue;
                }
                var entry=monsters.Find(e=>e?.prefab&&e.prefab.name==record.prefab);
                if(entry==null&&additionalRoster){foreach(var special in new[]{additionalRoster.nightThief,additionalRoster.nightSkullclaw})if(special?.prefab&&special.prefab.name==record.prefab){entry=special;break;}}
                if(entry==null){worldLoadFailed=true;continue;}
                if(record.kind==0){if(!SurvivorCrowd.Current||!SurvivorCrowd.Current.RestoreWorld(this,entry,record)){worldLoadFailed=true;continue;}}
                else{var m=Instantiate(entry.prefab,record.position,record.rotation,transform);m.Initialize(this,player,record.boss,record.summon);m.RestoreWorld(record);alive.Add(m);liveSpecies[m]=entry;if(record.summon)godSummons.Add(m);}
                RestoredMonsterCount++;
            }
            restoringWorld=false;
            if(saved.runtime!=null){yield return null;while(System.Array.Exists(FindObjectsByType<MonsterSpawnZone>(FindObjectsSortMode.None),z=>z.IsRestoringWorld))yield return null;
                try{RunCheckpoint.Restore(saved.runtime,player);}catch(Exception e){worldLoadFailed=true;Debug.LogError("Run checkpoint restore failed; original save preserved: "+e);}
                finally{RunCheckpoint.IsRestoring=false;Time.timeScale=previousScale;}}
            nextTimedRetry=Time.time+1;
            if(worldLoadFailed)Debug.LogWarning("Some saved monsters could not be restored; original checkpoint is preserved.");
            ForecastChanged?.Invoke();
        }
    }
}



