#if UNITY_EDITOR
using System;
using System.Reflection;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;

/// <summary>Opt-in, temporary Play Mode probe. Never installed on a scene or saved.</summary>
public sealed class MonsterNavigationPlayProbe : MonoBehaviour
{
    private WorldNavigationGrid oldGrid, grid;
    private MushroomMonster monster;
    private MiningCharacterHealth player;
    private MonsterRewardData data;
    private GameObject wall;
    private float elapsed, maximumSide;
    private string prefabPath = "Assets/Prefabs/Monsters/MushroomMonster.prefab";
    private bool oldGridEnabled;
    private bool inserted, sawAvoidance, sawTarget, enteredAttackRange;
    private static readonly Vector3 Center = new Vector3(10000f, 0f, 10000f);
    private static readonly PropertyInfo InstanceProperty = typeof(WorldNavigationGrid).GetProperty("Instance",
        BindingFlags.Public | BindingFlags.Static);
    public string Result { get; private set; } = "Running";
public static MonsterNavigationPlayProbe Begin(string prefabPath = "Assets/Prefabs/Monsters/MushroomMonster.prefab")
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Play Mode required");
        var root = new GameObject("Temporary Monster A* Play Probe");
        var probe = root.AddComponent<MonsterNavigationPlayProbe>();
        probe.prefabPath = prefabPath;
        return probe;
    }
    private void Start()
    {
        oldGrid = WorldNavigationGrid.Instance;
        if (oldGrid != null) { oldGridEnabled = oldGrid.enabled; oldGrid.enabled = false; }
        InstanceProperty.SetValue(null, null);
        var floor = Box("Probe ground", Center + Vector3.down * .5f, new Vector3(20f, 1f, 20f));
        int ground = LayerMask.NameToLayer("Ground"); if (ground >= 0) floor.layer = ground;
        grid = gameObject.AddComponent<WorldNavigationGrid>();
        grid.ConfigureArea(Center, new Vector2(20f,20f));
        grid.Configure(.5f,.25f,1.8f,45f,.5f,~0,512,1024);
        Physics.SyncTransforms(); grid.Rebake();
        var playerObject = new GameObject("Probe player"); playerObject.transform.SetParent(transform);
        playerObject.transform.position = Center + Vector3.forward*6f;
        playerObject.AddComponent<CapsuleCollider>().center = Vector3.up;
        player = playerObject.AddComponent<MiningCharacterHealth>(); player.SetDamageEnabled(false);
        var prefab = AssetDatabase.LoadAssetAtPath<MushroomMonster>(prefabPath);
        monster = Instantiate(prefab,Center+Vector3.back*6f,Quaternion.identity,transform);
        data = Instantiate(prefab.RewardData);
        data.combat.moveSpeed=3f; data.combat.detectionRange=30f;
        data.combat.attackRange=1.4f; data.combat.chaseRepathSeconds=.2f;
        typeof(MushroomMonster).GetField("rewards",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(monster,data);
        typeof(MushroomMonster).GetField("speciesData",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(monster,data);
        monster.Initialize(null,player);
        monster.Health.SetDamageEnabled(false);
    }
    private GameObject Box(string name, Vector3 position, Vector3 scale)
    {
        var obj = new GameObject(name); obj.transform.SetParent(transform);
        obj.transform.position=position; obj.transform.localScale=scale; obj.AddComponent<BoxCollider>();
        return obj;
    }
    private void Update()
    {
        if (monster == null || Result != "Running") return;
        elapsed += Time.deltaTime;
        var local = monster.transform.position-Center;
        maximumSide = Mathf.Max(maximumSide,Mathf.Abs(local.x));
        sawTarget |= monster.HasPlayerTarget;
        sawAvoidance |= monster.NavigationStatus == "Avoiding obstacle";
        if (!inserted && elapsed >= .65f)
        {
            // Appears after the monster is already following a straight A* route.
            wall=Box("Wall appeared during movement",Center+Vector3.up*1.5f,new Vector3(6f,3f,1f));
            Physics.SyncTransforms(); inserted=true; // Deliberately no MarkDirty: the forward probe must discover it.
        }
        enteredAttackRange = Vector3.ProjectOnPlane(monster.transform.position-player.transform.position,Vector3.up).magnitude <= 1.5f;
        if (enteredAttackRange || elapsed >= 20f)
            Result = (enteredAttackRange && sawTarget && maximumSide > 3f ? "PASS" : "FAIL")+
                " dynamic-wall chase: time="+elapsed.ToString("F2")+" side="+maximumSide.ToString("F2")+
                " detected="+sawTarget+" avoidance="+sawAvoidance+" state="+monster.NavigationStatus;
    }
    public string Snapshot() => Result+" position="+(monster != null ? (monster.transform.position-Center).ToString() : "none")+
        " state="+(monster!=null ? monster.NavigationStatus : "none");
    private void OnDestroy()
    {
        if (monster != null) Destroy(monster.gameObject);
        if (grid != null) grid.enabled=false;
        InstanceProperty.SetValue(null,oldGrid);
        if (oldGrid != null) oldGrid.enabled=oldGridEnabled;
        if (data != null) Destroy(data);
    }
}
#endif