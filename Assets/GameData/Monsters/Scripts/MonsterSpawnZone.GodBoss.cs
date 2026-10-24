using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MonsterSpawnZone
    {
        [Header("God Boss encounter")]
        [SerializeField] private GodBossData godBossData;
        private GodBossController godBoss;
        private int godPreparedDay = -1;
        private float godRetry;
        private readonly List<MushroomMonster> godSummons = new();
        private const string GodDefeatedDayKey = "World.GodBoss.DefeatedDay.v1";
        public bool IsGodDay => godBossData && godBossData.IsEncounterDay(SpawnDayNumber);
        public GodBossController ActiveGodBoss => godBoss;
        private bool TickGodEncounter()
        {
            if (DebugSpawningPaused) return true;
            if (!IsGodDay && !godBoss) return false;
            if (godBoss || !IsGodDay) return true;
            if (Time.time < godRetry) return true;
            godRetry = Time.time + 2;
            // One primary spawner owns the encounter if the scene contains multiple zones.
            foreach (var other in FindObjectsByType<MonsterSpawnZone>(FindObjectsSortMode.InstanceID))
                if (other.IsGodDay) { if (other != this) return true; break; }
            if (godPreparedDay != SpawnDayNumber)
            {
                godPreparedDay = SpawnDayNumber;
                foreach (var monster in new List<MushroomMonster>(MushroomMonster.Monsters))
                    if (monster) monster.BeginDespawn();
                alive.Clear(); liveSpecies.Clear(); godSummons.Clear();
                ForecastChanged?.Invoke();
            }
            if (GameSave.GetInt(GodDefeatedDayKey) == SpawnDayNumber || !player || player.Health <= 0) return true;
            if (!godBossData.prefab) return true;
            for (int i = 0; i < 16; i++)
            {
                Vector3 offset = Quaternion.Euler(0, Random.Range(0,360), 0) * Vector3.forward * godBossData.spawnDistance;
                if (!TryFindGround(player.transform.position + offset, out var hit)) continue;
                var prefabMotor = godBossData.prefab.GetComponent<CharacterController>();
                float radius = prefabMotor.radius, height = Mathf.Max(radius * 2, godBossData.height);
                if (Physics.CheckCapsule(hit.point + Vector3.up * (radius + .15f),
                    hit.point + Vector3.up * (height - radius), radius * .95f, ~0, QueryTriggerInteraction.Ignore)) continue;
                godBoss = Instantiate(godBossData.prefab, hit.point + Vector3.up * .1f, Quaternion.identity, transform);
                godBoss.Initialize(this, player, SpawnDayNumber);
                godBoss.Defeated += OnGodDefeated;
                ForecastChanged?.Invoke(); break;
            }
            return true;
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
                    if (godSummons.Count >= godBossData.maxLivingSummons || LivingCountOnField() >= MaximumLivingMonsters) return;
                    if (variant == 1 && (entry.prefab.RewardData == null || entry.prefab.RewardData.boss == null)) continue;
                    for (int attempt = 0; attempt < 12; attempt++)
                    {
                        Vector3 point = boss.transform.position + Quaternion.Euler(0,Random.Range(0,360),0) * Vector3.forward * Random.Range(8,16);
                        if (!TryFindGround(point, out var hit)) continue;
                        if ((player.transform.position-hit.point).sqrMagnitude < 9) continue;
                        if (Physics.CheckSphere(hit.point + Vector3.up * 1.2f, .8f, ~0, QueryTriggerInteraction.Ignore)) continue;
                        var minion = Instantiate(entry.prefab, hit.point + Vector3.up * .05f, Quaternion.identity, transform);
                        minion.Initialize(this, player, variant == 1, true);
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
            godBoss = null; godPreparedDay = -1; godRetry = 0; godSummons.Clear();
        }
    }
}
