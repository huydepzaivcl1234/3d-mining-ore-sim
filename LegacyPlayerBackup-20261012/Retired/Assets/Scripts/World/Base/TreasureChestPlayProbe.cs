#if UNITY_EDITOR
using System;
using System.Reflection;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;
// Opt-in validation component, never added to production scenes or builds.
public sealed class TreasureChestPlayProbe : MonoBehaviour
{
    private TreasureChest chest;
    private PlayerWallet wallet;
    private TreasureChestData data;
    private DayNightSystem resetClock;
    private bool resetRecovered;
    
private MonsterSpawnZone zone;
    private MushroomMonster monster;
    private MiningCharacterHealth player;
    private float clock, initialMoney, initialHP;
    private bool spawned, intercepted, returned, hurt, paid, burst, moved;
    private int phase;
    private Vector3 start;
    private static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    public string Result {get;private set;}="Running";
    private void Start()
    {
        wallet=new GameObject("Isolated validation wallet").AddComponent<PlayerWallet>();wallet.transform.SetParent(transform);
        initialMoney=wallet.CurrentMoney;
        var source=AssetDatabase.LoadAssetAtPath<TreasureChestData>("Assets/GameData/Base/TreasureChestData.asset");
        data=Instantiate(source);data.ticksPerSecond=1;data.experienceForFirstLevel=2;
        chest=Instantiate(source.prefab,source.spawnPosition,Quaternion.identity,transform);
        var point=chest.transform.position;point.y=Terrain.activeTerrain.SampleHeight(point)+Terrain.activeTerrain.transform.position.y;chest.transform.position=point;
        Set(chest,"persistProgress",false);Set(chest,"data",data);Set(chest,"wallet",wallet);
        var p=new GameObject("Probe player");p.transform.SetParent(transform);p.transform.position=point+Vector3.right*20;
        p.AddComponent<CapsuleCollider>().center=Vector3.up;
        player=p.AddComponent<MiningCharacterHealth>();player.SetDamageEnabled(false);
        resetClock=new GameObject("Isolated unsaved clock").AddComponent<DayNightSystem>();
        resetClock.transform.SetParent(transform);
        Set(resetClock,"currentPeriod",MiningTimePeriod.Day);
        Set(resetClock,"periodElapsed",0f);
        typeof(DayNightSystem).GetField("<DayNumber>k__BackingField",Flags).SetValue(resetClock,1);
        zone=gameObject.AddComponent<MonsterSpawnZone>();
        Set(zone,"dayNight",resetClock);
        var entry=new MonsterSpawnEntry{prefab=AssetDatabase.LoadAssetAtPath<MushroomMonster>("Assets/Prefabs/Monsters/MushroomMonster.prefab")};
        Set(zone,"monsters",new System.Collections.Generic.List<MonsterSpawnEntry>{entry});
        Set(zone,"player",player);Set(zone,"wallet",wallet);
    }
    private static void Set(object owner,string field,object value)=>owner.GetType().GetField(field,Flags).SetValue(owner,value);
    private void Update()
    {
        if(Result!="Running" || chest==null)return;
        clock+=Time.deltaTime;
        paid|=wallet.CurrentMoney>initialMoney;
        var fx=(MiningGame.Vfx.ChestCoinBurstVfx)typeof(TreasureChest).GetField("coinBurst",Flags).GetValue(chest);
        foreach(var ps in fx.GetComponentsInChildren<ParticleSystem>())burst|=ps.particleCount>0;
        if(monster==null && WorldNavigationGrid.Instance!=null && WorldNavigationGrid.Instance.HasBaked)
        {
            if (!(bool)typeof(MonsterSpawnZone).GetField("started",Flags).GetValue(zone)) return;
            Set(zone,"scheduledDay",resetClock.DayNumber);
            Set(zone,"nextWave",99);
            zone.ResetEncounter();
            resetRecovered=(int)typeof(MonsterSpawnZone).GetField("nextWave",Flags).GetValue(zone)==0 && zone.DailyForecast.Count>0;
            // Deterministic normal-day preview, without changing authored event odds or saves.
            typeof(MonsterSpawnZone).GetField("<CurrentDailyEvent>k__BackingField",Flags).SetValue(zone,DailyEncounterEvent.Normal);
            Set(zone,"dailyAnnouncementPending",false);
            Set(resetClock,"periodElapsed",.99f);
            typeof(MonsterSpawnZone).GetMethod("Update",Flags).Invoke(zone,null);
            spawned=zone.AliveCount>0;
            if(spawned)
            {
                monster=zone.GetComponentInChildren<MushroomMonster>();
                start=monster.transform.position;initialHP=chest.Health.Health;
                float distance=Vector2.Distance(new Vector2(start.x,start.z),new Vector2(chest.transform.position.x,chest.transform.position.z));
                if(distance<data.monsterSpawnMinimumDistance-.001f || distance>data.monsterSpawnMaximumDistance+.001f)Result="FAIL spawn outside ring";
            }
        }
        if(monster!=null)
        {
            moved|=(monster.transform.position-start).sqrMagnitude>1;
            var target=(MiningCharacterHealth)typeof(MushroomMonster).GetField("target",Flags).GetValue(monster);
            if(phase==0 && moved)
            { player.transform.position=monster.transform.position+(chest.transform.position-monster.transform.position).normalized*2;Physics.SyncTransforms();phase=1;}
            else if(phase==1 && target==player)
            { intercepted=true;player.transform.position=chest.transform.position+Vector3.right*25;Physics.SyncTransforms();phase=2;}
            else if(phase==2 && target==chest.Health)returned=true;
            hurt|=chest.Health.Health<chest.Health.MaxHealth-.01f;
        }
        if (spawned && moved && intercepted && returned && hurt && paid && burst)
        {
            try { ValidateBreakAndRepair(); Result = "PASS world-repair/cost/no-modal/break/friendly-fire/quota/cap " + Snapshot(); }
            catch (Exception error) { Result = "FAIL " + error; }
        }
        else if (clock > 50) Result = "FAIL timeout " + Snapshot();
    }

    private void ValidateBreakAndRepair()
    {
        // No real save resets, purchases, player wallet or production clock are touched.
        var attacker = new GameObject("Isolated friendly fire probe");
        attacker.SetActive(false); attacker.transform.SetParent(transform);
        var combat = attacker.AddComponent<PlayerCombatInput>();
        Require(chest.Level == 1 && chest.Experience == 0f, "gold payout granted XP");
        Set(combat, "damage", 100000f);
        float hp = chest.Health.Health;
        typeof(PlayerCombatInput).GetMethod("DamageTarget", Flags).Invoke(combat,
            new object[] { chest.Health, chest.transform.position, 1f });
        Require(chest.Health.Health == hp, "player damaged own chest");

        var live = (System.Collections.Generic.List<MushroomMonster>)typeof(MonsterSpawnZone).GetField("alive", Flags).GetValue(zone);
        int originalCount = live.Count;
        while (live.Count < 50) live.Add(monster);
        var entry = zone.MonsterEntries[0];
        bool beyondCap = (bool)typeof(MonsterSpawnZone).GetMethod("SpawnOne", Flags).Invoke(zone, new object[] { entry, false, false });
        Require(!beyondCap && live.Count == 50, "spawn crossed living cap");
        live.RemoveRange(originalCount, live.Count - originalCount);
        monster.GetComponent<MiningCharacterHealth>().DealDamage(100000f, CombatDamageType.True, attacker);
        Require(chest.Level > 1, "player kill did not grant chest XP");
        Destroy(attacker);
        foreach (int day in new[] { 1, 2, 49, 50, 70 })
        {
            typeof(DayNightSystem).GetField("<DayNumber>k__BackingField", Flags).SetValue(resetClock, day);
            typeof(MonsterSpawnZone).GetField("forcedDailyEvent", Flags).SetValue(zone, DailyEncounterEvent.Normal);
            typeof(MonsterSpawnZone).GetMethod("BuildDailySchedule", Flags).Invoke(zone, null);
            int planned = 0;
            foreach (var item in zone.DailyForecast) planned += item.Planned;
            Require(planned == Mathf.Min(day, 50), "incorrect daily schedule " + day);
        }
        zone.enabled = false;
        int level = chest.Level; float experience = chest.Experience, money = wallet.CurrentMoney;
        var hud = chest.GetComponent<TreasureChestHud>();
        var button = (UnityEngine.UI.Button)typeof(TreasureChestHud).GetField("repairButton", Flags).GetValue(hud);
        Require(button.GetComponentInParent<Canvas>().renderMode == RenderMode.WorldSpace, "Repair is not world space");
        // An isolated coordinator detects accidental modal registration without touching production input.
        var coordinatorObject = new GameObject("Isolated repair modal guard");
        coordinatorObject.SetActive(false); coordinatorObject.transform.SetParent(transform);
        var coordinator = coordinatorObject.AddComponent<MiningUiPanelCoordinator>();
        var inputLockedBefore = Cursor.lockState;
        wallet.SetMoney(0f);
        chest.Health.DealDamage(chest.Health.MaxHealth, CombatDamageType.True);
        Require(!chest.Repair() && chest.IsBroken && wallet.CurrentMoney == 0, "insufficient money repaired chest");
        Require(!button.interactable, "unaffordable Repair is enabled");
        wallet.AddMoney(chest.RepairCost * 3f + 1f);
        Require(button.interactable, "Repair did not react to money change");
        money = wallet.CurrentMoney;
        for (int cycle = 0; cycle < 2; cycle++)
        {
            chest.Health.DealDamage(chest.Health.MaxHealth, CombatDamageType.True);
            Require(chest.IsBroken && !chest.IsAlive, "death did not stop economy");
            var damaged = (GameObject)typeof(TreasureChest).GetField("damagedVisual", Flags).GetValue(chest);
            Require(damaged != null && damaged.activeInHierarchy, "damaged model missing");
            Require(button.gameObject.activeInHierarchy, "Repair button missing");
            Require(!coordinator.BlocksGameplay && Cursor.lockState == inputLockedBefore, "broken chest locked gameplay");
            int particles = 0;
            foreach (var effect in FindObjectsByType<MiningGame.Vfx.ChestBreakBurst>(FindObjectsSortMode.None))
                foreach (var system in effect.GetComponentsInChildren<ParticleSystem>()) particles += system.particleCount;
            Require(particles > 0, "explosion did not emit");
            typeof(TreasureChest).GetMethod("Update", Flags).Invoke(chest, null);
            Require(wallet.CurrentMoney == money, "broken chest paid gold");
            button.onClick.Invoke();
            money -= chest.RepairCost;
            Require(chest.IsAlive && chest.Health.Health == chest.Health.MaxHealth, "Repair did not restore full health");
            Require(!damaged.activeSelf && !button.gameObject.activeSelf, "broken visuals survived repair");
            Require(chest.Level == level && chest.Experience == experience, "repair reset progression");
            Require(wallet.CurrentMoney == money && !chest.Repair(), "incorrect repair charge or repeated repair");
        }
        Destroy(coordinatorObject);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    public string Snapshot()=> " reset="+resetRecovered+" time="+clock.ToString("F1")+" spawn="+spawned+" moved="+moved+" intercept="+intercepted+" return="+returned+" damage="+hurt+" gold="+paid+" coins="+burst+" level="+(chest!=null?chest.Level:0)+" nav="+(monster!=null?monster.NavigationStatus:"waiting grid");
    private void OnDestroy(){if(data!=null)Destroy(data);}
}
#endif
