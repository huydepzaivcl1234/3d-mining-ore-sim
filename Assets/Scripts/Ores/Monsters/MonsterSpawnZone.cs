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
        // Retain old scene fields for serialization; chest GameData now owns the ring.
        [HideInInspector, SerializeField] private float minimumSpawnDistance = 4f;
        [HideInInspector, SerializeField] private float maximumSpawnDistance = 7f;
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
        [HideInInspector, SerializeField] private int minimumPerDay = 3;
        [HideInInspector, SerializeField] private int maximumPerDay = 12;
        [Tooltip("Extra daily monsters per current day number. 1 means roll + current day.")]
        [HideInInspector, SerializeField] private int extraMonstersPerDayNumber = 1;
        [Tooltip("Total daily cap across every species, including boss variants.")]
        [HideInInspector, SerializeField] private int dailyMonsterCap = 50;
        [Range(1, 12), SerializeField] private int maximumPerWave = 3;
        [Range(0.01f, 0.99f), SerializeField] private float largerWaveRelativeWeight = 0.35f;
        [SerializeField] private bool showDailyForecast = true;
        [HideInInspector, SerializeField] private int maximumAlive = 6;
        public const int MaximumLivingMonsters = 50;
        [Min(0.1f), SerializeField] private float minimumSpacing = 2f;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private MiningCharacterHealth player;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MiningItemSystem inventory;
        [SerializeField] private MiningUpgradeSystem upgradeSystem;
        [SerializeField] private CurrencyRewardPopup moneyPopupPrefab;
        [SerializeField] private MiningUiData moneyPopupUiData;
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
        private MiningAudioManager audioManager;
        private readonly List<MushroomMonster> alive = new();
        private readonly Dictionary<MushroomMonster, MonsterSpawnEntry> liveSpecies = new();
        private MiningUiPanelCoordinator uiCoordinator;
        private bool dailyAnnouncementPending;
        private DailyEncounterEvent? forcedDailyEvent;
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
        public int SpawnDayNumber => dayNight != null ? Mathf.Max(1, dayNight.DayNumber) : 1;
        public event Action ForecastChanged;
        public int AliveCount => alive.Count;
        public DayNightSystem DayNight => dayNight;
        public bool HasForecastBoss
        {
            get
            {
                if (CurrentDailyEvent == DailyEncounterEvent.BossInvasion) return true;
                foreach (var monster in alive)
                    if (monster != null && monster.IsBoss && !monster.IsDespawning &&
                        monster.Health != null && monster.Health.Health > 0f) return true;
                return false;
            }
        }
        public IReadOnlyList<MonsterSpawnEntry> MonsterEntries => monsters;
        public int GetAliveCount(MonsterSpawnEntry entry)
        {
            int count = 0;
            foreach (var monster in alive)
                if (monster != null && !monster.IsDespawning && monster.Health != null && monster.Health.Health > 0f &&
                    liveSpecies.TryGetValue(monster, out var species) && species == entry) count++;
            return count;
        }
        private bool TryGetMineSurface(out NavMeshSurface surface)
        {
            if (miningSurface == null && WorldNavigationBootstrap.Instance != null)
                miningSurface = WorldNavigationBootstrap.Instance.GetComponent<NavMeshSurface>();
            surface = miningSurface;
            // Use the authored mining bake, never a separate monster rectangle.
            return surface != null && surface.isActiveAndEnabled && surface.navMeshData != null &&
                surface.collectObjects == CollectObjects.Volume;
        }

        private static NavMeshQueryFilter MineFilter(NavMeshSurface surface) =>
            new NavMeshQueryFilter { agentTypeID = surface.agentTypeID, areaMask = NavMesh.AllAreas };

public bool IsInsideMine(Vector3 point)
        {
            if (miningSurface == null && WorldNavigationBootstrap.Instance != null)
                miningSurface = WorldNavigationBootstrap.Instance.GetComponent<NavMeshSurface>();
            if (miningSurface == null) return true;
            Vector3 local = miningSurface.transform.InverseTransformPoint(point) - miningSurface.center;
            return Mathf.Abs(local.x) <= miningSurface.size.x * .5f &&
                Mathf.Abs(local.z) <= miningSurface.size.z * .5f;
        }

public bool TryGetMineEntryPoint(Vector3 origin, out Vector3 point)
        {
            point = origin;
            var grid = WorldNavigationGrid.Instance;
            if (grid == null || !grid.HasBaked) return false;
            if (miningSurface == null && WorldNavigationBootstrap.Instance != null)
                miningSurface = WorldNavigationBootstrap.Instance.GetComponent<NavMeshSurface>();
            if (miningSurface == null) return grid.TryProject(origin, grid.StandProjectionRadius, out point);
            Vector3 local = miningSurface.transform.InverseTransformPoint(origin) - miningSurface.center;
            local.x = Mathf.Clamp(local.x, -miningSurface.size.x * .45f, miningSurface.size.x * .45f);
            local.z = Mathf.Clamp(local.z, -miningSurface.size.z * .45f, miningSurface.size.z * .45f);
            return grid.TryProject(miningSurface.transform.TransformPoint(local + miningSurface.center),
                grid.StandProjectionRadius, out point);
        }

private bool TryGetSpawnCandidate(out Vector3 position)
        {
            position = default;
            var chest = TreasureChest.Active;
            var grid = WorldNavigationGrid.Instance;
            if (chest == null || !chest.IsAlive || chest.Data == null ||
                grid == null || !grid.isActiveAndEnabled || !grid.HasBaked) return false;
            var data = chest.Data;
            float min = Mathf.Max(.5f, data.monsterSpawnMinimumDistance);
            float max = Mathf.Max(min, data.monsterSpawnMaximumDistance);
            float radius = Mathf.Sqrt(UnityEngine.Random.Range(min * min, max * max));
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            position = chest.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            return true;
        }

private bool IsValidSpawnDistance(Vector3 position)
        {
            var chest = TreasureChest.Active;
            var grid = WorldNavigationGrid.Instance;
            if (chest == null || chest.Data == null || grid == null ||
                !grid.isActiveAndEnabled || !grid.HasBaked || !grid.Contains(position)) return false;
            Vector3 offset = position - chest.transform.position;
            offset.y = 0f;
            float min = Mathf.Max(.5f, chest.Data.monsterSpawnMinimumDistance);
            float max = Mathf.Max(min, chest.Data.monsterSpawnMaximumDistance);
            return offset.sqrMagnitude >= min * min && offset.sqrMagnitude <= max * max;
        }


public bool TryGetPatrolPoint(Vector3 origin,out Vector3 point)
        {
            point=origin;
            var grid=WorldNavigationGrid.Instance;var terrain=Terrain.activeTerrain;
            if(grid==null || !grid.HasBaked || terrain==null)return false;
            Vector3 start=terrain.transform.position,size=terrain.terrainData.size;
            for(int attempt=0;attempt<12;attempt++)
            {
                var candidate=start+new Vector3(UnityEngine.Random.Range(size.x*.1f,size.x*.9f),0,UnityEngine.Random.Range(size.z*.1f,size.z*.9f));
                if(grid.TryProject(candidate,grid.StandProjectionRadius,out point))return true;
            }
            return false;
        }


        private void Start()
        {
            if(WorldNavigationGrid.Instance==null) gameObject.AddComponent<WorldNavigationGrid>();
            var chestData=Resources.Load<TreasureChestData>("TreasureChestData");
            if(TreasureChest.Active==null && chestData!=null && chestData.prefab!=null)
            {
                Vector3 point=chestData.spawnPosition;
                if(Terrain.activeTerrain!=null)point.y=Terrain.activeTerrain.SampleHeight(point)+Terrain.activeTerrain.transform.position.y;
                Instantiate(chestData.prefab,point,Quaternion.identity);
            }
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
            uiCoordinator = FindFirstObjectByType<MiningUiPanelCoordinator>(FindObjectsInactive.Include);
            if (wallet == null) wallet = FindAnyObjectByType<PlayerWallet>();
            if (inventory == null) inventory = FindAnyObjectByType<MiningItemSystem>();
            if (upgradeSystem == null) upgradeSystem = FindAnyObjectByType<MiningUpgradeSystem>();
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
            // Start runs once; refresh scheduling after re-enabling the zone.
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
                float awarded = wallet.CurrentMoney - previousMoney;
                if (awarded > 0f && moneyPopupPrefab != null && moneyPopupUiData != null)
                    Instantiate(moneyPopupPrefab).Initialize(awarded, origin, moneyPopupUiData);
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
            if (alive.RemoveAll(RemoveInactiveMonster) > 0) ForecastChanged?.Invoke();
            if (dayNight == null || !dayNight.isActiveAndEnabled) return;
            if (scheduledDay != dayNight.DayNumber) BuildDailySchedule();
            AnnounceDailyEvent();
            if (TreasureChest.Active == null || !TreasureChest.Active.IsAlive) return;
            if (dayNight.CurrentPeriod != SpawnPeriod)
            {
                // Night-only waves are deferred during the day, not discarded.
                return;
            }
            float progress = dayNight.CurrentPeriodProgress;
            // A blocked spawn or full field defers this wave; it must not consume the daily budget.
            if (nextWave >= waves.Count || progress < waves[nextWave].Progress ||
                Time.time < nextSpawnRetry) return;
            nextSpawnRetry = Time.time + 1f;
            SpawnWave wave = waves[nextWave];
            while (wave.Completed < wave.Members.Count && alive.Count < MaximumLivingMonsters)
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
            int limit = Mathf.Min(Mathf.Clamp(maximumPerWave, 1, 12), MaximumLivingMonsters, remaining);
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
            dailyEvents ??= additionalRoster != null ? additionalRoster.DailyEvents : null;
            dailyEvents ??= Resources.Load<DailyEncounterEventData>("DailyEncounterEvents");
            bool bossEligible = monsters.Exists(CanSpawnEventBoss);
            CurrentDailyEvent = dailyEvents != null ? dailyEvents.Roll(UnityEngine.Random.value, bossEligible) : DailyEncounterEvent.Normal;
            if (forcedDailyEvent.HasValue) CurrentDailyEvent = forcedDailyEvent.Value;
            forcedDailyEvent = null;
            dailyAnnouncementPending = CurrentDailyEvent != DailyEncounterEvent.Normal;
            bool invasion = CurrentDailyEvent == DailyEncounterEvent.BossInvasion;
            int remaining = CalculateDailyMonsterCount(0, scheduledDay);
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
            // The first wave is due immediately; later waves are spread through the period.
            // In particular, Day 1 must not look empty for half of the daytime.
            for (int i = 0; i < waves.Count; i++)
                waves[i].Progress = Mathf.Lerp(start, 1f, i / (float)waves.Count);
            ForecastChanged?.Invoke();
        }

        private bool RemoveInactiveMonster(MushroomMonster monster)
        {
            bool remove = monster == null || monster.IsDespawning || monster.Health == null || monster.Health.Health <= 0f;
            if (remove && !ReferenceEquals(monster, null)) liveSpecies.Remove(monster);
            return remove;
        }
        private void AnnounceDailyEvent()
        {
            if (!dailyAnnouncementPending || (uiCoordinator != null && uiCoordinator.BlocksGameplay)) return;
            if (unlockNotifier == null) unlockNotifier = FindFirstObjectByType<MiningUnlockNotifier>(FindObjectsInactive.Include);
            if (unlockNotifier == null || !unlockNotifier.isActiveAndEnabled) return;
            unlockNotifier.ShowToast(DailyEventLabel);
            dailyAnnouncementPending = false;
        }
        public bool DebugStartDailyEvent(DailyEncounterEvent selected)
        {
            if (!Application.isPlaying || !started || dayNight == null || !isActiveAndEnabled) return false;
            if (selected == DailyEncounterEvent.BossInvasion && !monsters.Exists(CanSpawnEventBoss)) return false;
            // Explicit preview replaces future slots, not living monsters or saved progress.
            forcedDailyEvent = selected;
            BuildDailySchedule();
            AnnounceDailyEvent();
            return true;
        }

        private void SkipCurrentWave()
        {
            SpawnWave wave = waves[nextWave++];
            for (int i = wave.Completed; i < wave.Members.Count; i++) wave.Members[i].Skipped++;
            ForecastChanged?.Invoke();
        }

        public int CalculateDailyMonsterCount(int rolled, int day)
        {
            // Keep the legacy signature for callers; the quota is now exactly the day number.
            return Mathf.Clamp(day, 1, MaximumLivingMonsters);
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
            liveSpecies.Clear();
        }
        public bool DebugSpawnBoss(MushroomMonster prefab)
        {
            if (!Application.isPlaying || prefab == null) return false;
            var entry = monsters.Find(m => m != null && m.prefab == prefab);
            return entry != null && SpawnOne(entry, true);
        }
        private bool SpawnOne(MonsterSpawnEntry entry, bool forceBoss = false, bool invasion = false)
        {
            alive.RemoveAll(RemoveInactiveMonster);
            if (LivingCountOnField() >= MaximumLivingMonsters || !CanSpawnSpecies(entry)) return false;
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
                    instance.ApplyEncounterModifiers(extraScale, 1f + Mathf.Max(0, dailyEvents.bossExtraDamagePercent) * 0.01f,
                        1f + Mathf.Max(0, dailyEvents.bossExtraHealthPercent) * .01f);
                alive.Add(instance);
                liveSpecies[instance] = entry;
                ForecastChanged?.Invoke();
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
                    surface.GetComponentInParent<MiningChest>() != null ||
                    surface.GetComponentInParent<LuckyBlock>() != null ||
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

        private static int LivingCountOnField()
        {
            int count = 0;
            // Spawn-time only; multiple enabled spawners share the same hard field cap.
            foreach (var zone in FindObjectsByType<MonsterSpawnZone>(FindObjectsSortMode.None))
                foreach (var monster in zone.alive)
                    if (monster != null && !monster.IsDespawning && monster.Health != null && monster.Health.Health > 0f)
                        count++;
            return count;
        }
    

public static void ResetLoadedEncounters()
        {
            foreach (var zone in FindObjectsByType<MonsterSpawnZone>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (zone.started) zone.ResetEncounter();
                else zone.scheduledDay = -1;
            }
        }


public void ResetEncounter()
        {
            // Reset can return to the same day: day-number comparison alone misses it.
            foreach (var monster in alive)
                if (monster != null)
                {
                    monster.gameObject.SetActive(false);
                    Destroy(monster.gameObject);
                }
            alive.Clear();
            liveSpecies.Clear();
            announcedSpecies.Clear();
            announcedBosses.Clear();
            forcedDailyEvent = null;
            lastKnownPlayerLevel = playerStats != null ? playerStats.Level : 1;
            BuildDailySchedule();
        }
}
}
