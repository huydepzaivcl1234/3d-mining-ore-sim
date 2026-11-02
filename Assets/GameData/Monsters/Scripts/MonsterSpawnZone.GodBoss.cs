using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MonsterSpawnZone
    {
        [Header("God Boss encounter")]
        [SerializeField] private GodBossData godBossData;
        private GodBossController godBoss;
        private float godRetry;
        private readonly List<MushroomMonster> godSummons = new();
        private const string GodDefeatedDayKey = "World.GodBoss.DefeatedDay.v1";
        private const string GodEligibleDayKey="World.GodBoss.EligibleDay.v2",GodRollDayKey="World.GodBoss.RollDay.v2",GodRollResultKey="World.GodBoss.RollResult.v2",GodAppearanceKey="World.GodBoss.Appearance.v2",GodAppearanceDayKey="World.GodBoss.AppearanceDay.v2";
        private int godRolledDay=-1;
        private bool godSelected;
        public bool IsGodDay => godBossData&&godRolledDay==SpawnDayNumber&&godSelected;
        public float GodSpawnChancePercent=>godBossData?godBossData.SpawnChancePercent(SpawnDayNumber,GameSave.GetInt(GodEligibleDayKey),GameSave.GetInt(GodAppearanceDayKey)):0;
        private void PrepareGodRoll()
        {
            if(!godBossData||!player||player.Health<=0||godRolledDay==SpawnDayNumber)return;
            var stats=player.GetComponent<PlayerStats>();
            if(!stats||stats.Level<godBossData.minimumPlayerLevel)return;
            int day=SpawnDayNumber,first=GameSave.GetInt(GodEligibleDayKey);
            if(first<1){first=day;GameSave.SetInt(GodEligibleDayKey,first);}
            if(GameSave.GetInt(GodRollDayKey)!=day){
                GameSave.SetInt(GodRollDayKey,day);
                float chance=godBossData.SpawnChancePercent(day,first,GameSave.GetInt(GodAppearanceDayKey));
                GameSave.SetInt(GodRollResultKey,chance>=100||Random.value*100<chance?1:0);
                GameSave.Save();
            }
            godRolledDay=day;godSelected=GameSave.GetInt(GodRollResultKey)==1;
            ForecastChanged?.Invoke();
        }
        public GodBossController ActiveGodBoss => godBoss;
        private void TickGodEncounter()
        {
            if (DebugSpawningPaused) return;
            PrepareGodRoll();
            if (!IsGodDay && !godBoss) return;
            if (godBoss || !IsGodDay) return;
            if (Time.time < godRetry) return;
            godRetry = Time.time + 2;
            // One primary spawner owns the encounter if the scene contains multiple zones.
            foreach (var other in FindObjectsByType<MonsterSpawnZone>(FindObjectsSortMode.InstanceID))
                if (other.IsGodDay) { if (other != this) return; break; }
            if (GameSave.GetInt(GodDefeatedDayKey) == SpawnDayNumber || !player || player.Health <= 0) return;
            if (!godBossData.prefab) return;
            for (int i = 0; i < 16; i++)
            {
                Vector3 offset = Quaternion.Euler(0, Random.Range(0,360), 0) * Vector3.forward * godBossData.spawnDistance;
                if (!TryFindGround(player.transform.position + offset, out var hit)) continue;
                var prefabMotor = godBossData.prefab.GetComponent<CharacterController>();
                float radius = prefabMotor.radius, height = Mathf.Max(radius * 2, godBossData.height);
                if (Physics.CheckCapsule(hit.point + Vector3.up * (radius + .15f),
                    hit.point + Vector3.up * (height - radius), radius * .95f, ~0, QueryTriggerInteraction.Ignore)) continue;
                godBoss = Instantiate(godBossData.prefab, hit.point + Vector3.up * .1f, Quaternion.identity, transform);
                int appearance=GameSave.GetInt(GodAppearanceKey);
                if(GameSave.GetInt(GodAppearanceDayKey)!=SpawnDayNumber){appearance++;GameSave.SetInt(GodAppearanceKey,appearance);GameSave.SetInt(GodAppearanceDayKey,SpawnDayNumber);GameSave.Save();}
                godBoss.Initialize(this, player, SpawnDayNumber,Mathf.Max(1,appearance));
                GameData.Player.Script.SurvivorMonsterDiscovery.Announce(godBoss,godBossData.prefab,"God Boss",true);
                godBoss.Defeated += OnGodDefeated;
                ForecastChanged?.Invoke(); break;
            }
            return;
        }
        private void OnGodDefeated(GodBossController boss)
        {
            boss.Defeated -= OnGodDefeated;
            GameSave.SetInt(GodDefeatedDayKey, boss.EncounterDay); GameSave.Save();
            NotifyDefeated(boss.transform.position);
            godBoss = null; ForecastChanged?.Invoke();
        }
        public void SummonGodMinions(GodBossController boss, float missingHealth)
        {
            if (DebugSpawningPaused) return;
            if (!boss || boss != godBoss || !godBossData) return;
            godSummons.RemoveAll(m => !m || !m.Health || m.Health.Health <= 0 || m.IsDespawning);
            var entries = new List<MonsterSpawnEntry>(monsters);
            if (additionalRoster)
            { entries.Add(additionalRoster.nightThief); entries.Add(additionalRoster.nightSkullclaw); }
            var seen = new HashSet<MushroomMonster>();
            foreach (var entry in entries)
            {
                if (entry == null || !entry.prefab || !seen.Add(entry.prefab)) continue;
                for (int variant = 0; variant < (godBossData.summonBossVariants ? 2 : 1); variant++)
                {
                    if (godSummons.Count >= godBossData.maxLivingSummons || LivingCountOnField() >= LivingMonsterLimit) return;
                    if (variant == 1 && (entry.prefab.RewardData == null || entry.prefab.RewardData.boss == null)) continue;
                    for (int attempt = 0; attempt < 12; attempt++)
                    {
                        Vector3 point = boss.transform.position + Quaternion.Euler(0,Random.Range(0,360),0) * Vector3.forward * Random.Range(Mathf.Max(0,godBossData.summonMinimumDistance),Mathf.Max(godBossData.summonMinimumDistance,godBossData.summonMaximumDistance));
                        if (!TryFindGround(point, out var hit)) continue;
                        if ((player.transform.position-hit.point).sqrMagnitude < godBossData.summonMinimumPlayerDistance * godBossData.summonMinimumPlayerDistance) continue;
                        if (Physics.CheckSphere(hit.point + Vector3.up * 1.2f, .8f, ~0, QueryTriggerInteraction.Ignore)) continue;
                        var minion = Instantiate(entry.prefab, hit.point + Vector3.up * .05f, Quaternion.identity, transform);
                        minion.Initialize(this, player, variant == 1, true);
                        GameData.Player.Script.SurvivorMonsterDiscovery.Announce(minion,entry,variant==1);
                        minion.ApplyEncounterModifiers(1, Mathf.Lerp(1, godBossData.summonDamageAtZeroBossHP, missingHealth),
                            Mathf.Lerp(1, godBossData.summonHealthAtZeroBossHP, missingHealth));
                        alive.Add(minion); liveSpecies[minion] = entry; godSummons.Add(minion);
                        break;
                    }
                }
            }
            ForecastChanged?.Invoke();
        }
        private void ResetGodEncounter()
        {
            if (godBoss) { godBoss.Defeated -= OnGodDefeated; godBoss.gameObject.SetActive(false); Destroy(godBoss.gameObject); }
            godBoss = null; godRolledDay=-1;godSelected=false; godRetry = 0; godSummons.Clear();
        }
    }
}
