#if UNITY_EDITOR
using System;
using System.Reflection;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;

// Editor-only opt-in runtime probe. Never attached to a production scene/prefab.
public sealed class MushnightPlayProbe : MonoBehaviour
{
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    public string Result { get; private set; } = "Running";
    private TreasureChest chest;
    private PlayerWallet wallet;
    private MiningCharacterHealth player;
    private MonsterSpawnZone zone;
    private DayNightSystem night;
    private MushroomMonster monster;
    private MushnightThief thief;
    private TreasureChestData chestData;
    private MonsterSpawnRoster roster;
    private float age, stealStarted = -1f, robbedAt, stolen, hp, restStarted;
    private Vector3 spawn, chaseStart, restPosition;
    private bool moved, dayBlocked, cloak, pursuit, earlyBalance;
    private int phase;
    private static void Set(object instance, string field, object value) => instance.GetType().GetField(field, Flags).SetValue(instance, value);
    private static object Call(object instance, string method, params object[] args) => instance.GetType().GetMethod(method, Flags).Invoke(instance, args);

    private void Start()
    {
        wallet = new GameObject("Unsaved thief test wallet").AddComponent<PlayerWallet>();
        wallet.transform.SetParent(transform); wallet.SetMoney(100);
        var source = AssetDatabase.LoadAssetAtPath<TreasureChestData>("Assets/GameData/Base/TreasureChestData.asset");
        chestData = Instantiate(source); chestData.ticksPerSecond = .0001f;
        chest = Instantiate(source.prefab, new Vector3(0, .02f, 8), Quaternion.identity, transform);
        Set(chest, "persistProgress", false); Set(chest, "data", chestData); Set(chest, "wallet", wallet);
        var p = new GameObject("Thief test player"); p.transform.SetParent(transform); p.transform.position = new Vector3(25, .02f, 8);
        p.AddComponent<CapsuleCollider>().center = Vector3.up;
        player = p.AddComponent<MiningCharacterHealth>(); player.ConfigureSpawnHealth(100);
        var clock = new GameObject("Inactive unsaved test night clock"); clock.transform.SetParent(transform); clock.SetActive(false);
        night = clock.AddComponent<DayNightSystem>(); Set(night, "periodElapsed", .2f);
        roster = Instantiate(AssetDatabase.LoadAssetAtPath<MonsterSpawnRoster>("Assets/Resources/MonsterSpawnRoster.asset"));
        zone = gameObject.AddComponent<MonsterSpawnZone>(); zone.enabled = false;
        Set(zone, "player", player); Set(zone, "wallet", wallet); Set(zone, "dayNight", night);
        Set(zone, "additionalRoster", roster); Set(zone, "showDailyForecast", false); Set(zone, "showUnlockNotifications", false);
        Call(zone, "Start");
    }

    private void Update()
    {
        if (Result != "Running") return;
        age += Time.deltaTime;
        try
        {
            if (age > 60) throw new Exception("Timeout state=" + (thief != null ? thief.State.ToString() : "no spawn") + " nav=" + (monster != null ? monster.NavigationStatus : "none"));
            if (WorldNavigationGrid.Instance == null || !WorldNavigationGrid.Instance.HasBaked) return;
            if (phase == 0)
            {
                hp = chest.Health.Health;
                Set(night, "currentPeriod", MiningTimePeriod.Day); Call(zone, "TickNightThieves");
                dayBlocked = zone.AliveCount == 0;
                Set(night, "currentPeriod", MiningTimePeriod.Night); Call(zone, "TickNightThieves");
                monster = zone.GetComponentInChildren<MushroomMonster>();
                if (monster == null) return;
                thief = monster.GetComponent<MushnightThief>(); spawn = monster.transform.position;
                cloak = thief.IsInvisible && !monster.IsTargetVisible;
                // The invisible approach must ignore a living player directly in front.
                player.transform.position = spawn + (chest.transform.position - spawn).normalized;
                Physics.SyncTransforms(); phase = 1;
            }
            moved |= monster != null && (monster.transform.position - spawn).sqrMagnitude > 1f;
            if (phase == 1 && moved)
            { Require(thief.IsInvisible, "revealed merely by approaching player"); player.transform.position = new Vector3(25, .02f, 8); phase = 2; }
            if (phase == 2 && thief.State == MushnightThief.ThiefState.Stealing)
            {
                if (stealStarted < 0f) stealStarted = age;
                if (age - stealStarted > 1.5f) { Require(wallet.CurrentMoney == 100, "money taken before 3 seconds"); earlyBalance = true; }
            }
            if (phase == 2 && thief.State == MushnightThief.ThiefState.Escaping)
            {
                stolen = thief.StolenGold;
                Require(earlyBalance && stolen > 0 && age - stealStarted >= 2.8f, "incorrect theft duration");
                Require(wallet.CurrentMoney == 100 - stolen, "wrong debit");
                Require(!thief.IsInvisible, "still invisible after robbery");
                robbedAt = age; chaseStart = monster.transform.position;
                player.transform.position = chaseStart + (chest.transform.position - chaseStart).normalized;
                Physics.SyncTransforms(); phase = 3;
            }
            if (phase == 3)
            {
                pursuit |= Vector3.Distance(player.transform.position, monster.transform.position) > 2.5f;
                if (age - robbedAt < 2f) return;
                Require(pursuit && moved && cloak && dayBlocked, "movement/night/cloak/flee failed");
                Require(chest.Health.Health == hp && player.Health == player.MaxHealth, "thief attacked a victim");
                var label = monster.transform.Find("Stolen gold");
                Require(label != null && label.gameObject.activeInHierarchy, "stolen gold label missing");
                Require(label.GetComponentInChildren<TMPro.TextMeshProUGUI>().text.Contains(stolen.ToString("0.##")), "wrong gold label amount");
                // Exercise exhaustion without touching authored GameData.
                var testSettings = new MushnightSettings { maxStamina = 1, staminaPerMovingSecond = 100,
                    staminaRecoveryDelay = 1, staminaRecoveryPerSecond = .5f, resumeStaminaFraction = 1 };
                Set(thief, "stamina", new MushnightStamina(testSettings));
                phase = 4;
            }
            if (phase == 4 && thief.IsResting)
            { restPosition = monster.transform.position; restStarted = age; phase = 5; }
            if (phase == 5)
            {
                float distance = Vector3.ProjectOnPlane(monster.transform.position - restPosition, Vector3.up).magnitude;
                if (thief.IsResting)
                { Require(distance < .03f, $"exhausted thief kept moving: {distance:0.000}m after {age-restStarted:0.000}s, stamina {thief.Stamina:0.000}, dt {Time.deltaTime:0.000}"); return; }
                Require(age - restStarted >= 2.8f, "resumed before stamina recovered");
                Set(thief, "stamina", new MushnightStamina(((MushnightData)monster.SpeciesData).mushnight));
                phase = 6;
            }
            if (phase == 6)
            {
                float distance = Vector3.ProjectOnPlane(monster.transform.position - restPosition, Vector3.up).magnitude;
                if (distance < .1f) return;
                monster.Health.DealDamage(100000f, CombatDamageType.True);
                thief.OnKilled();
                Require(wallet.CurrentMoney == 100 && thief.StolenGold == 0, "refund not exactly once");
                Set(zone, "nextThiefRetry", 0f); Call(zone, "TickNightThieves");
                Require((int)typeof(MonsterSpawnZone).GetField("spawnedThieves", Flags).GetValue(zone) == 1, "night quota repeated");
                Result = "PASS night-only spawn, invisible player-ignore A* approach, 3s debit, visible escape/player evasion, no attacks, world-space stolen amount, exhaustion stop/recovery/resume, exact-once refund, one/night quota";
            }
        }
        catch (Exception error) { Result = "FAIL " + (error.InnerException ?? error).Message; }
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private void OnDestroy() { if (chestData != null) Destroy(chestData); if (roster != null) Destroy(roster); }
}
#endif
