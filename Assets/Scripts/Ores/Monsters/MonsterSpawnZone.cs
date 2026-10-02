using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace MiningSimulator.Ores
{
    [Serializable]
    public sealed class MonsterSpawnEntry
    {
        public MushroomMonster prefab;
        public Sprite icon;
        [Tooltip("Optional localized name used by unlock notifications. Empty uses the prefab name.")]
        public string displayName;
        [Tooltip("Relative spawn weight. With entries 75 and 25 the probabilities are 75% and 25%. 0 disables this species.")]
        [Min(0f)] public float chance = 100f;
        [Tooltip("This species enters the daily spawn pool at this player level. Boss rules still apply separately.")]
        [Min(1)] public int minimumPlayerLevel = 1;
    }
    public sealed class DailyMonsterForecast
    {
        public MonsterSpawnEntry Entry { get; internal set; }
        public int Planned { get; internal set; }
        public int Spawned { get; internal set; }
        public int Skipped { get; internal set; }
        public int Remaining => Mathf.Max(0, Planned - Spawned - Skipped);
    }
    // Retain the script identity/GUID so existing scenes keep their spawn table.
    // This is a mining encounter spawner, not a bounded spawn zone anymore.
    [AddComponentMenu("Mining Simulator/Mining Monster Spawner")]
    public sealed class MonsterSpawnZone : MonoBehaviour
    {
        [SerializeField] private List<MonsterSpawnEntry> monsters = new();
        [Tooltip("Additional species. Authored scene entries take precedence if the prefab already exists.")]
        [SerializeField] private MonsterSpawnRoster additionalRoster;
        [SerializeField] private NavMeshSurface miningSurface;
        [Min(0.5f), SerializeField] private float minimumSpawnDistance = 4f;
        [Min(0.5f), SerializeField] private float maximumSpawnDistance = 7f;
        [Min(0f), SerializeField] private float minimumPlayerDistance = 3f;
        [Header("Daily encounters")]
        [SerializeField] private DayNightSystem dayNight;
        [SerializeField] private DailyEncounterEventData dailyEvents;
        public DailyEncounterEvent CurrentDailyEvent { get; private set; }
        public MiningTimePeriod SpawnPeriod => CurrentDailyEvent == DailyEncounterEvent.NightOnly
            ? MiningTimePeriod.Night : MiningTimePeriod.Day;
        public Sprite DailyEventIcon => dailyEvents == null ? null :
            CurrentDailyEvent == DailyEncounterEvent.NightOnly ? dailyEvents.nightOnlyIcon : dailyEvents.bossInvasionIcon;
        public string DailyEventLabel => CurrentDailyEvent == DailyEncounterEvent.NightOnly
            ? MiningLocalization.TextKey("DAILY_EVENT_NIGHT_ONLY", "Day off - monsters arrive at night")
            : MiningLocalization.TextKey("DAILY_EVENT_BOSS", "Boss invasion");
        [Min(0), SerializeField] private int minimumPerDay = 3;
        [Min(0), SerializeField] private int maximumPerDay = 12;
        [Tooltip("Extra daily monsters per current day number. 1 means roll + current day.")]
        [Min(0), SerializeField] private int extraMonstersPerDayNumber = 1;
        [Tooltip("Total daily cap across every species, including boss variants.")]
        [Min(1), SerializeField] private int dailyMonsterCap = 50;
        [Range(1, 12), SerializeField] private int maximumPerWave = 3;
        [Range(0.01f, 0.99f), SerializeField] private float largerWaveRelativeWeight = 0.35f;
        [SerializeField] private bool showDailyForecast = true;
        [Min(0), SerializeField] private int maximumAlive = 6;
        [Min(0.1f), SerializeField] private float minimumSpacing = 2f;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private MiningCharacterHealth player;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MiningItemSystem inventory;
        [SerializeField] private MiningUpgradeSystem upgradeSystem;
        [Header("Monster unlock notifications")]
        [SerializeField] private bool showUnlockNotifications = true;
        [SerializeField] private MiningUnlockNotifier unlockNotifier;
        [Header("Debug - opt-in only")]
        [Tooltip("Test locked species without changing the player's saved level. Does not bypass boss gates.")]
        [SerializeField] private bool debugIgnoreSpeciesLevel;
        private MiningPlayerStats playerStats;
        private int lastKnownPlayerLevel;
        private readonly HashSet<MushroomMonster> announcedSpecies = new();
        private readonly HashSet<MushroomMonster> announcedBosses = new();
        private OreSpawner oreSpawner;
        private MiningAudioManager audioManager;
        private readonly List<MushroomMonster> alive = new();
        private readonly RaycastHit[] groundHits = new RaycastHit[64];
        private bool started;
        private readonly List<DailyMonsterForecast> forecast = new();
        private readonly List<SpawnWave> waves = new();
        private int scheduledDay = -1, nextWave;
        private float nextSpawnRetry;
        private MonsterDailyForecastHud forecastHud;
        private sealed class SpawnWave
        {
            public float Progress;
            public readonly List<DailyMonsterForecast> Members = new();
            public int Completed;
        }
        public IReadOnlyList<DailyMonsterForecast> DailyForecast => forecast;
        public int ForecastDay => scheduledDay;
        public event Action ForecastChanged;
        public int AliveCount => alive.Count;
        private bool TryGetMineSurface(out NavMeshSurface surface)
        {
            if (miningSurface == null && MiningNavMeshBuilder.Instance != null)
                miningSurface = MiningNavMeshBuilder.Instance.GetComponent<NavMeshSurface>();
            surface = miningSurface;
            // Use the authored mining bake, never a separate monster rectangle.
            return surface != null && surface.isActiveAndEnabled && surface.navMeshData != null &&
                surface.collectObjects == CollectObjects.Volume;
        }

        private static NavMeshQueryFilter MineFilter(NavMeshSurface surface) =>
            new NavMeshQueryFilter { agentTypeID = surface.agentTypeID, areaMask = NavMesh.AllAreas };

        public bool IsInsideMine(Vector3 point)
        {
            if (!TryGetMineSurface(out NavMeshSurface surface)) return false;
            Vector3 local = surface.transform.InverseTransformPoint(point) - surface.center;
            if (Mathf.Abs(local.x) > surface.size.x * 0.5f ||
                Mathf.Abs(local.z) > surface.size.z * 0.5f) return false;
            return NavMesh.SamplePosition(point, out _, 0.6f, MineFilter(surface));
        }

        public bool TryGetMineEntryPoint(Vector3 origin, out Vector3 point)
        {
            point = origin;
            if (!TryGetMineSurface(out NavMeshSurface surface) ||
                !NavMesh.SamplePosition(origin, out NavMeshHit nearest,
                    Mathf.Max(minimumSpawnDistance, maximumSpawnDistance) + 4f, MineFilter(surface))) return false;
            // Aim slightly inside the bake, not exactly at its unwalkable border.
            Vector3 inward = Vector3.ProjectOnPlane(surface.transform.TransformPoint(surface.center) - nearest.position, Vector3.up).normalized;
            if (!NavMesh.SamplePosition(nearest.position + inward, out NavMeshHit entry, 1.2f, MineFilter(surface))) return false;
            point = entry.position;
            return true;
        }

        private bool TryGetSpawnCandidate(out Vector3 position)
        {
            position = default;
            if (!TryGetMineSurface(out NavMeshSurface surface)) return false;
            Vector3 half = surface.size * 0.5f;
            // Sample the perimeter of the existing bake volume by side length.
            float side = UnityEngine.Random.Range(0f, 2f * (surface.size.x + surface.size.z));
            Vector3 rim, outward;
            if ((side -= surface.size.x) < 0f)
            { rim = new Vector3(UnityEngine.Random.Range(-half.x, half.x), 0f, half.z); outward = Vector3.forward; }
            else if ((side -= surface.size.x) < 0f)
            { rim = new Vector3(UnityEngine.Random.Range(-half.x, half.x), 0f, -half.z); outward = Vector3.back; }
            else if ((side -= surface.size.z) < 0f)
            { rim = new Vector3(half.x, 0f, UnityEngine.Random.Range(-half.z, half.z)); outward = Vector3.right; }
            else
            { rim = new Vector3(-half.x, 0f, UnityEngine.Random.Range(-half.z, half.z)); outward = Vector3.left; }
            float min = Mathf.Max(0.5f, minimumSpawnDistance);
            float max = Mathf.Max(min, maximumSpawnDistance);
            Vector3 boundary = surface.transform.TransformPoint(surface.center + rim);
            // Ground queries start above the bake floor rather than its volume center.
            boundary.y = surface.transform.position.y;
            position = boundary + Vector3.ProjectOnPlane(surface.transform.TransformDirection(outward), Vector3.up).normalized * UnityEngine.Random.Range(min, max);
            return true;
        }

        private bool IsValidSpawnDistance(Vector3 position)
        {
            if (!TryGetMineSurface(out NavMeshSurface surface) ||
                !NavMesh.SamplePosition(position, out NavMeshHit nearest,
                    Mathf.Max(minimumSpawnDistance, maximumSpawnDistance) + 2f, MineFilter(surface))) return false;
            float distance = Vector3.ProjectOnPlane(position - nearest.position, Vector3.up).magnitude;
            return distance >= Mathf.Max(0.5f, minimumSpawnDistance) &&
                distance <= Mathf.Max(minimumSpawnDistance, maximumSpawnDistance);
        }

        public bool TryGetMiningApproachPoint(Vector3 origin, out Vector3 point)
        {
            point = origin;
            if (oreSpawner == null || !oreSpawner.TryGetClosestActiveOre(origin, out Ore ore)) return false;
            Vector3 surface = ore.GetClosestSurfacePoint(origin);
            Vector3 away = Vector3.ProjectOnPlane(origin - ore.transform.position, Vector3.up);
            if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
            // Approach the mine, never the center of its solid ore collider.
            point = surface + away.normalized * 1.5f;
            return true;
        }
        private void Start()
        {
            additionalRoster ??= Resources.Load<MonsterSpawnRoster>("MonsterSpawnRoster");
            if (additionalRoster != null)
                foreach (var entry in additionalRoster.Entries)
                    if (entry != null && entry.prefab != null && !monsters.Exists(e => e != null && e.prefab == entry.prefab))
                        monsters.Add(entry);
            if (player == null)
            {
                var stats = FindAnyObjectByType<MiningPlayerStats>();
                if (stats != null) player = stats.GetComponent<MiningCharacterHealth>();
            }
            if (player != null) playerStats = player.GetComponent<MiningPlayerStats>();
            // Loaded progression is the baseline, not a new unlock on every launch.
            lastKnownPlayerLevel = playerStats != null ? playerStats.Level : 1;
            if (unlockNotifier == null) unlockNotifier = FindFirstObjectByType<MiningUnlockNotifier>();
            if (wallet == null) wallet = FindAnyObjectByType<PlayerWallet>();
            if (inventory == null) inventory = FindAnyObjectByType<MiningItemSystem>();
            if (upgradeSystem == null) upgradeSystem = FindAnyObjectByType<MiningUpgradeSystem>();
            oreSpawner = FindAnyObjectByType<OreSpawner>();
            audioManager = FindAnyObjectByType<MiningAudioManager>();
            if (dayNight == null) dayNight = FindAnyObjectByType<DayNightSystem>();
            BuildDailySchedule();
            started = true;
            if (showDailyForecast)
            {
                forecastHud = gameObject.AddComponent<MonsterDailyForecastHud>();
                forecastHud.Configure(this);
            }
        }
        private void OnEnable()
        {
            // Start runs only once. Recreate Ground monsters after a return from Lava.
            if (!started) return;
            CheckMonsterUnlocks();
            if (dayNight != null && scheduledDay != dayNight.DayNumber) BuildDailySchedule();
            if (forecastHud != null) forecastHud.SetVisible(true);
        }
        public void GrantRewards(MonsterRewardData data, Vector3 origin, float goldMultiplier = 1, float experienceMultiplier = 1)
        {
            if (data == null) return;
            float xp = Mathf.Max(0, data.experience) * Mathf.Max(0f, experienceMultiplier);
            float money = Mathf.Max(0, data.gold) * Mathf.Max(0f, goldMultiplier);
            if (upgradeSystem != null)
            {
                xp = upgradeSystem.CalculatePlayerExperienceReward(xp);
                money = upgradeSystem.CalculateMonsterMoneyReward(money);
            }
            // AddExperience applies GameManager PlayerXpMultiplier exactly once.
            if (playerStats != null) playerStats.AddExperience(xp);
            if (wallet != null)
            {
                float previousMoney = wallet.CurrentMoney;
                wallet.AddMoney(money);
                if (oreSpawner == null) oreSpawner = FindAnyObjectByType<OreSpawner>();
                if (oreSpawner != null)
                    oreSpawner.ShowMoneyRewardPopup(wallet.CurrentMoney - previousMoney, origin);
            }
            if (data.drops == null) return;
            foreach (var drop in data.drops)
            {
                if (drop == null || drop.item == null || drop.chancePercent <= 0) continue;
                if (drop.chancePercent < 100 && UnityEngine.Random.value >= drop.chancePercent / 100) continue;
                int min = Mathf.Clamp(drop.minimumAmount, 1, 10000);
                int max = Mathf.Clamp(drop.maximumAmount, min, 10000);
                int count = UnityEngine.Random.Range(min, max + 1);
                MonsterItemPickup.Spawn(data, drop, count, origin, player, inventory,
                    groundLayers, audioManager);
            }
        }
        private void Update()
        {
            CheckMonsterUnlocks();
            alive.RemoveAll(m => m == null || m.IsDespawning || m.Health == null || m.Health.Health <= 0f);
            if (dayNight == null || !dayNight.isActiveAndEnabled) return;
            if (scheduledDay != dayNight.DayNumber) BuildDailySchedule();
            if (dayNight.CurrentPeriod != SpawnPeriod)
            {
                // Night-only waves are deferred during the day, not discarded.
                if (SpawnPeriod == MiningTimePeriod.Day) SkipWavesBefore(1f);
                return;
            }
            float progress = dayNight.CurrentPeriodProgress;
            // Expired slots are not merged into a huge catch-up wave after a world swap.
            while (nextWave + 1 < waves.Count && progress >= waves[nextWave + 1].Progress)
                SkipCurrentWave();
            if (nextWave >= waves.Count || progress < waves[nextWave].Progress ||
                Time.time < nextSpawnRetry) return;
            nextSpawnRetry = Time.time + 1f;
            SpawnWave wave = waves[nextWave];
            while (wave.Completed < wave.Members.Count && alive.Count < maximumAlive)
            {
                DailyMonsterForecast entry = wave.Members[wave.Completed];
                if (!CanSpawnSpecies(entry.Entry))
                {
                    entry.Skipped++;
                    wave.Completed++;
                    ForecastChanged?.Invoke();
                    continue;
                }
                bool invasion = CurrentDailyEvent == DailyEncounterEvent.BossInvasion;
                if (!SpawnOne(entry.Entry, invasion, invasion)) break;
                entry.Spawned++;
                wave.Completed++;
                ForecastChanged?.Invoke();
            }
            if (wave.Completed == wave.Members.Count) nextWave++;
        }

        private void CheckMonsterUnlocks()
        {
            if (playerStats == null) return;
            int level = playerStats.Level;
            if (level == lastKnownPlayerLevel) return;
            int previous = lastKnownPlayerLevel;
            lastKnownPlayerLevel = level;
            if (level < previous)
            {
                announcedSpecies.Clear();
                announcedBosses.Clear();
                return;
            }
            if (!showUnlockNotifications || unlockNotifier == null) return;
            foreach (var entry in monsters)
            {
                if (entry == null || entry.prefab == null || entry.chance <= 0f) continue;
                string key = string.IsNullOrWhiteSpace(entry.displayName) ? entry.prefab.name : entry.displayName;
                string name = MiningLocalization.Text(key, key);
                int required = Mathf.Max(1, entry.minimumPlayerLevel);
                if (previous < required && level >= required && announcedSpecies.Add(entry.prefab))
                    unlockNotifier.ShowToast(string.Format(MiningLocalization.Text("MONSTER_UNLOCKED",
                        "Đã mở khóa quái: {0} (Lv. {1})!"), name, required));
                var boss = entry.prefab.RewardData != null ? entry.prefab.RewardData.boss : null;
                if (boss == null || !boss.enabled || boss.chancePercent <= 0f) continue;
                int bossRequired = Mathf.Max(required, Mathf.Max(1, boss.minimumPlayerLevel));
                if (previous < bossRequired && level >= bossRequired && announcedBosses.Add(entry.prefab))
                    unlockNotifier.ShowToast(string.Format(MiningLocalization.Text("MONSTER_BOSS_UNLOCKED",
                        "Đã mở khóa boss: {0} (Lv. {1})!"), name, bossRequired));
            }
        }

        public bool CanSpawnSpecies(MonsterSpawnEntry entry)
        {
            return entry != null && entry.prefab != null && (debugIgnoreSpeciesLevel ||
                (playerStats != null ? playerStats.Level : 1) >= Mathf.Max(1, entry.minimumPlayerLevel));
        }

        private bool CanSpawnEventBoss(MonsterSpawnEntry entry)
        {
            if (!CanSpawnSpecies(entry) || entry.chance <= 0f) return false;
            var boss = entry.prefab.RewardData != null ? entry.prefab.RewardData.boss : null;
            var tuning = MiningGameplayTuning.Current;
            return boss != null && boss.CanSpawn(playerStats != null ? playerStats.Level : 1,
                tuning != null && tuning.IgnoreBossLevel);
        }

        private MonsterSpawnEntry RollMonsterEntry(bool bossOnly = false)
        {
            float total = 0f;
            foreach (MonsterSpawnEntry entry in monsters)
                if (CanSpawnSpecies(entry) && (!bossOnly || CanSpawnEventBoss(entry))) total += Mathf.Max(0f, entry.chance);
            if (total <= 0f) return null;
            float roll = UnityEngine.Random.value * total;
            foreach (MonsterSpawnEntry entry in monsters)
                if (CanSpawnSpecies(entry) && (!bossOnly || CanSpawnEventBoss(entry)) && entry.chance > 0f && (roll -= entry.chance) <= 0f)
                    return entry;
            return null;
        }

        private int RollWaveSize(int remaining)
        {
            int limit = Mathf.Min(Mathf.Clamp(maximumPerWave, 1, 12), Mathf.Max(1, maximumAlive), remaining);
            float ratio = Mathf.Clamp(largerWaveRelativeWeight, 0.01f, 0.99f);
            float total = 0f, weight = 1f;
            for (int i = 1; i <= limit; i++) { total += weight; weight *= ratio; }
            float roll = UnityEngine.Random.value * total;
            weight = 1f;
            for (int i = 1; i <= limit; i++)
            { if ((roll -= weight) <= 0f) return i; weight *= ratio; }
            return limit;
        }

        private void BuildDailySchedule()
        {
            forecast.Clear(); waves.Clear(); nextWave = 0; nextSpawnRetry = 0f;
            scheduledDay = dayNight != null ? dayNight.DayNumber : 1;
            if (dayNight == null)
            { ForecastChanged?.Invoke(); return; }
            dailyEvents ??= Resources.Load<DailyEncounterEventData>("DailyEncounterEvents");
            bool bossEligible = monsters.Exists(CanSpawnEventBoss);
            CurrentDailyEvent = dailyEvents != null ? dailyEvents.Roll(UnityEngine.Random.value, bossEligible) : DailyEncounterEvent.Normal;
            int min = Mathf.Clamp(minimumPerDay, 0, 256);
            int rolled = UnityEngine.Random.Range(min, Mathf.Clamp(maximumPerDay, min, 256) + 1);
            bool invasion = CurrentDailyEvent == DailyEncounterEvent.BossInvasion;
            int remaining = invasion ? 1 : CalculateDailyMonsterCount(rolled, scheduledDay);
            while (remaining > 0)
            {
                var wave = new SpawnWave();
                int count = RollWaveSize(remaining);
                for (int i = 0; i < count; i++)
                {
                    MonsterSpawnEntry entry = RollMonsterEntry(invasion);
                    if (entry == null) { remaining = 0; break; }
                    DailyMonsterForecast item = forecast.Find(f => f.Entry == entry);
                    if (item == null) { item = new DailyMonsterForecast { Entry = entry }; forecast.Add(item); }
                    item.Planned++;
                    wave.Members.Add(item);
                    remaining--;
                }
                if (wave.Members.Count > 0) waves.Add(wave);
            }
            float start = dayNight.CurrentPeriod == SpawnPeriod ? dayNight.CurrentPeriodProgress : 0f;
            for (int i = 0; i < waves.Count; i++)
                waves[i].Progress = Mathf.Lerp(start, 1f, (i + 1f) / (waves.Count + 1f));
            ForecastChanged?.Invoke();
        }

        private void SkipCurrentWave()
        {
            SpawnWave wave = waves[nextWave++];
            for (int i = wave.Completed; i < wave.Members.Count; i++) wave.Members[i].Skipped++;
            ForecastChanged?.Invoke();
        }

        public int CalculateDailyMonsterCount(int rolled, int day)
        {
            long total = (long)Mathf.Max(0, rolled) +
                (long)Mathf.Max(1, day) * Mathf.Max(0, extraMonstersPerDayNumber);
            return (int)Math.Min(Mathf.Max(1, dailyMonsterCap), total);
        }
        private void SkipWavesBefore(float progress)
        {
            while (nextWave < waves.Count && waves[nextWave].Progress <= progress) SkipCurrentWave();
        }
        private void OnDisable()
        {
            if (forecastHud != null) forecastHud.SetVisible(false);
            // The spawned monsters are owned by this spawner. Destroy without combat
            // rewards so no invisible Ground enemies survive the world switch.
            foreach (MushroomMonster monster in alive)
                if (monster != null) Destroy(monster.gameObject);
            alive.Clear();
        }
        public bool DebugSpawnBoss(MushroomMonster prefab)
        {
            if (!Application.isPlaying || prefab == null) return false;
            var entry = monsters.Find(m => m != null && m.prefab == prefab);
            return entry != null && SpawnOne(entry, true);
        }
        private bool SpawnOne(MonsterSpawnEntry entry, bool forceBoss = false, bool invasion = false)
        {
            if (!CanSpawnSpecies(entry)) return false;
            MushroomMonster prefab = entry.prefab;
            var bossSettings = prefab.RewardData != null ? prefab.RewardData.boss : null;
            var tuning = MiningGameplayTuning.Current;
            bool ignoreLevel = tuning != null && tuning.IgnoreBossLevel;
            bool force = forceBoss || (tuning != null && tuning.ForceBossSpawns);
            bool boss = bossSettings != null && (force
                ? bossSettings.CanSpawn(playerStats != null ? playerStats.Level : 1, ignoreLevel)
                : bossSettings.Roll(playerStats != null ? playerStats.Level : 1, UnityEngine.Random.value, ignoreLevel));
            if (forceBoss && !boss) return false;
            float bossScale = boss ? bossSettings.ScaleMultiplier : 1f;
            float extraScale = invasion && dailyEvents != null ? 1f + Mathf.Max(0, dailyEvents.bossExtraSizePercent) * 0.01f : 1f;
            bossScale *= extraScale;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                if (!TryGetSpawnCandidate(out Vector3 position)) return false;
                if (!TryFindGround(position, out RaycastHit ground)) continue;
                if (!IsValidSpawnDistance(ground.point)) continue;
                if (player != null && (player.transform.position - ground.point).sqrMagnitude <
                    minimumPlayerDistance * minimumPlayerDistance) continue;
                var capsule = prefab.GetComponent<CharacterController>();
                if (capsule == null) continue;
                Vector3 scale = Vector3.Scale(prefab.transform.lossyScale, transform.lossyScale);
                scale *= bossScale;
                float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) * 0.9f;
                float height = Mathf.Max(radius * 2, capsule.height * Mathf.Abs(scale.y));
                Vector3 bottom = ground.point + Vector3.up * (radius + 0.1f);
                Vector3 top = ground.point + Vector3.up * Mathf.Max(radius + 0.1f, height - radius);
                if (Physics.CheckCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore)) continue;
                bool blocked = false;
                foreach (var other in alive)
                    if (other != null && (other.transform.position - ground.point).sqrMagnitude < minimumSpacing * minimumSpacing)
                        blocked = true;
                if (blocked) continue;
                var instance = Instantiate(prefab, ground.point,
                    Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0), transform);
                instance.Initialize(this, player, boss);
                if (invasion && dailyEvents != null)
                    instance.ApplyEncounterModifiers(extraScale, 1f + Mathf.Max(0, dailyEvents.bossExtraDamagePercent) * 0.01f);
                alive.Add(instance);
                return true;
            }
            return false;
        }

        private bool TryFindGround(Vector3 position, out RaycastHit ground)
        {
            // Mining locations contain ores, chests, monsters and scenery above terrain.
            // A single Raycast would spawn a monster on top of the first such collider.
            int count = Physics.RaycastNonAlloc(position + Vector3.up * 30f,
                Vector3.down, groundHits, 60f, groundLayers,
                QueryTriggerInteraction.Ignore);
            int best = -1;
            bool foundTerrain = false;
            float closest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];
                Collider surface = hit.collider;
                if (surface == null || hit.normal.y < 0.7f ||
                    surface.GetComponentInParent<MiningCharacterHealth>() != null ||
                    surface.GetComponentInParent<Ore>() != null ||
                    surface.GetComponentInParent<MiningChest>() != null ||
                    surface.GetComponentInParent<LuckyBlock>() != null ||
                    surface.GetComponentInParent<MiningNpc>() != null ||
                    surface is CharacterController) continue;

                bool terrain = surface is TerrainCollider;
                if (best >= 0 && (foundTerrain && !terrain ||
                    foundTerrain == terrain && hit.distance >= closest)) continue;
                best = i;
                foundTerrain = terrain;
                closest = hit.distance;
            }
            ground = best >= 0 ? groundHits[best] : default;
            return best >= 0;
        }
    }
}
