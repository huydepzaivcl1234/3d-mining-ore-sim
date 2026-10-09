#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Isolated, bounded check. Original roots are restored; no wallet/save logic runs.
[InitializeOnLoad]
public static class TowerPlayValidation
{
    private const string Key = "Mining.TowerPlayValidation";
    [Serializable] private class RootState { public string id; public bool active; }
    [Serializable] private class Roots { public RootState[] roots; }
    private static TowerRuntime tower;
    private static MiningCharacterHealth victim;
    private static Animator animator;
    private static float start;
    private static double wallStart;
    private static int phase;
    private static bool recoil;
    private static Transform recoilPivot;
    private static Vector3 restingPivot;
    private static bool recoilMoved;
    private static MiningItemDatabase testDatabase;
    private static TowerStatsPanel towerStats;
    private static Transform statsPlayer;
    private static MiningItemSystem placementInventory;
    private static Camera testCamera;
    private static Vector3 originalCameraPosition;
    private static Quaternion originalCameraRotation;
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static TowerPlayValidation() { EditorApplication.playModeStateChanged += Changed; EditorApplication.update += Tick; }
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first");
        if (UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene != null) throw new InvalidOperationException("Clear Play start override");
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        SessionState.SetString(Key+".roots", JsonUtility.ToJson(new Roots { roots=roots.Select(x=>new RootState { id=GlobalObjectId.GetGlobalObjectIdSlow(x).ToString(), active=x.activeSelf }).ToArray() }));
        SessionState.SetBool(Key,true); SessionState.SetString(Key+".result","Running");
        foreach (var root in roots) root.SetActive(false);
        EditorApplication.isPlaying=true;
    }
    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key,false)) return;
        if (state==PlayModeStateChange.EnteredEditMode)
        {
            var saved=JsonUtility.FromJson<Roots>(SessionState.GetString(Key+".roots",""));
            foreach (var root in saved.roots) if(GlobalObjectId.TryParse(root.id,out var id)) {
                var go=GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject; if(go!=null) go.SetActive(root.active);
            }
            SessionState.SetBool(Key,false); testDatabase=null; return;
        }
        if(state!=PlayModeStateChange.EnteredPlayMode) return;
        try {
            var d=AssetDatabase.LoadAssetAtPath<CannonTowerData>("Assets/GameData/Tower/Cannon/CannonData.asset");
            tower=UnityEngine.Object.Instantiate(d.prefab,new Vector3(10000,0,10000),Quaternion.identity).GetComponent<TowerRuntime>();
            tower.Initialize(d,100); animator=tower.GetComponentInChildren<Animator>(); animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Placement support";
            floor.transform.position=tower.transform.position+Vector3.down*.5f;floor.transform.localScale=new Vector3(60,1,60);
            var playerObject=new GameObject("Stats distance fixture");statsPlayer=playerObject.transform;
            statsPlayer.position=tower.transform.position+Vector3.right*2;
            var playerHealth=playerObject.AddComponent<MiningCharacterHealth>();playerHealth.ConfigureSpawnHealth(100);
            Physics.SyncTransforms();
            var body=tower.GetComponent<BoxCollider>();var foot=tower.transform.position+Vector3.forward*3;
            if(!TowerPlacementGeometry.Validate(ref foot,body,statsPlayer,12)||Mathf.Abs(foot.y)>.001f)throw new Exception("Supported placement rejected");
            var blocked=tower.transform.position;
            if(TowerPlacementGeometry.Validate(ref blocked,body,statsPlayer,12))throw new Exception("Occupied placement accepted");
            if(TowerPlacementGeometry.IsSupport(tower.GetComponent<BoxCollider>()))throw new Exception("Tower treated as ground");
            var snapped=TowerPlacementGeometry.Snap(foot+new Vector3(.23f,0,.41f),body,1);
            var cells=TowerPlacementGeometry.FootprintCells(body,1);
            if(cells.x<1||cells.y<1)throw new Exception("Empty footprint");
            if(Mathf.Abs((snapped.x+body.center.x)*2-Mathf.Round((snapped.x+body.center.x)*2))>.001f)throw new Exception("Grid alignment");
            recoilPivot=tower.GetComponentsInChildren<Transform>().First(t=>t.name=="RecoilPivot");restingPivot=recoilPivot.position;recoilMoved=false;
            var target=new GameObject("Tower test victim"); target.SetActive(false);
            target.transform.position=tower.transform.position+new Vector3(0,0,5);
            var shape=target.AddComponent<BoxCollider>(); shape.center=Vector3.up; shape.size=new Vector3(1,2,1);
            victim=target.AddComponent<MiningCharacterHealth>();
            var monster=target.AddComponent<MushroomMonster>(); monster.enabled=false;
            typeof(MushroomMonster).GetField("health",Flags).SetValue(monster,victim);
            target.SetActive(true); victim.ConfigureSpawnHealth(100); victim.ConfigureRegeneration(0,5);
            var camera=new GameObject("Tower test camera").AddComponent<Camera>(); camera.transform.position=tower.transform.position+new Vector3(5,4,-6);camera.transform.LookAt(tower.transform.position+Vector3.up);
            camera.tag="MainCamera";
            testCamera=camera;
            towerStats=tower.gameObject.AddComponent<TowerStatsPanel>();towerStats.Bind(tower);towerStats.Show(statsPlayer);
            if(!towerStats.IsOpen||towerStats.Panel.GetComponent<Canvas>().renderMode!=RenderMode.WorldSpace)throw new Exception("World stats did not open");
            towerStats.Panel.Find("Close").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            if(towerStats.IsOpen)throw new Exception("Close button failed");towerStats.Show(statsPlayer);
            var light=new GameObject("Tower test light").AddComponent<Light>(); light.type=LightType.Directional;
            animator.SetTrigger("Fire"); typeof(TowerRuntime).GetMethod("Fire",Flags).Invoke(tower,new object[]{victim});
            start=Time.time;wallStart=EditorApplication.timeSinceStartup;phase=0;recoil=false;
        } catch(Exception e) { Finish("FAIL setup "+e.GetBaseException().Message); }
    }
    private static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||tower==null) return;
        if(EditorApplication.timeSinceStartup-wallStart>15) { Finish("FAIL timeout");return; }
        if(phase==0)
        {
            recoil |= animator.GetCurrentAnimatorStateInfo(0).IsName("Cannon_Fire");
            recoilMoved |= Vector3.Distance(recoilPivot.position,restingPivot)>.01f;
            if(Time.time-start<1.2f) return;
            if(!recoil||!recoilMoved||Mathf.Abs(victim.Health-(100-tower.Data.damage))>.01f) {Finish("FAIL recoil/damage HP="+victim.Health+" recoil="+recoil+" moved="+recoilMoved);return;}
            var foundation=tower.GetComponentsInChildren<Renderer>().First(r=>r.name=="BaseMesh");
            if(Mathf.Abs(foundation.bounds.min.y-tower.transform.position.y)>.01f){Finish("FAIL floating foundation");return;}
            if(!towerStats.Panel.gameObject.activeSelf){Finish("FAIL stats visibility");return;}
            statsPlayer.position=tower.transform.position+Vector3.right*7;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=tower.transform.position+new Vector3(0,1,2.5f);wall.transform.localScale=new Vector3(3,3,.5f);
            Physics.SyncTransforms();typeof(TowerRuntime).GetMethod("Fire",Flags).Invoke(tower,new object[]{victim});phase=1;start=Time.time;
        }
        else if(phase==1 && Time.time-start>1.2f)
        {
            if(towerStats.IsOpen||towerStats.Panel.gameObject.activeSelf){Finish("FAIL stats distance hide");return;}
            if(Mathf.Abs(victim.Health-(100-tower.Data.damage))>.01f) {Finish("FAIL shot crossed wall");return;}
            victim.ConfigureSpawnHealth(100);victim.DealDamage(50,CombatDamageType.True);
            testDatabase=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<MiningItemDatabase>("Assets/GameData/Items/MiningItemDatabase.asset"));
            typeof(MiningItemDatabase).GetField("inventorySaveKey",Flags).SetValue(testDatabase,"");
            var go=new GameObject("Isolated consumable inventory");go.SetActive(false);
            var inventory=go.AddComponent<MiningItemSystem>();typeof(MiningItemSystem).GetField("database",Flags).SetValue(inventory,testDatabase);
            go.SetActive(true);typeof(MiningItemSystem).GetField("consumablePlayer",Flags).SetValue(inventory,victim);
            var apple=AssetDatabase.LoadAssetAtPath<MiningItemData>("Assets/GameData/Items/Apple.asset");
            inventory.TryAddItem(apple);inventory.TryUseSlot(0);start=Time.time;phase=2;
        }
        else if(phase==2 && Time.time-start>5.3f)
        {
            if(Mathf.Abs(victim.Health-60)>.05f){Finish("FAIL apple total HP="+victim.Health);return;}
            var playerStats=statsPlayer.gameObject.AddComponent<MiningPlayerStats>();
            typeof(MiningPlayerStats).GetField("saveBlocked",Flags).SetValue(playerStats,true);
            placementInventory=UnityEngine.Object.FindObjectsByType<MiningItemSystem>(FindObjectsSortMode.None).First(x=>x.name=="Isolated consumable inventory");
            var item=tower.Data.inventoryItem;placementInventory.TryAddItem(item);
            originalCameraPosition=testCamera.transform.position;originalCameraRotation=testCamera.transform.rotation;
            if(!TowerPlacement.BeginPlacement(placementInventory,item,0)){Finish("FAIL placement entry");return;}
            start=Time.time;phase=3;
        }
        else if(phase==3 && Time.time-start>.8f)
        {
            if(Vector3.Angle(testCamera.transform.forward,Vector3.down)>.1f){Finish("FAIL top down camera");return;}
            if(!placementInventory.GetComponent<TowerPlacement>().PreviewAt(new Ray(tower.transform.position+new Vector3(5,20,5),Vector3.down),out _)){Finish("FAIL preview valid cell");return;}
            var overlay=GameObject.Find("Tower placement overlays");
            if(overlay==null||overlay.GetComponentsInChildren<LineRenderer>(true).Length!=52){Finish("FAIL grid/range/footprint overlays");return;}
            if(overlay.GetComponentsInChildren<Collider>(true).Any(x=>x.enabled)){Finish("FAIL ghost collision");return;}
            if(!overlay.GetComponentInChildren<TowerStatsPanel>(true).IsOpen){Finish("FAIL preview stats");return;}
            placementInventory.GetComponent<TowerPlacement>().Cancel();
            if(TowerPlacement.IsPlacing||Vector3.Distance(originalCameraPosition,testCamera.transform.position)>.001f||Quaternion.Angle(originalCameraRotation,testCamera.transform.rotation)>.1f||placementInventory.GetItemCount(tower.Data.inventoryItem)!=1){Finish("FAIL cancel restoration/receipt");return;}
            Finish("PASS support, footprint blocking, grid snap, grounded foundation, stats X/distance hide, top-down entry, grid/range/ghost/stats overlays, cancel camera/receipt restore, recoil, damage, blocking, healing; isolated roots restored");
        }
    }
    private static void Finish(string result) { if(testDatabase!=null) UnityEngine.Object.DestroyImmediate(testDatabase);SessionState.SetString(Key+".result",result);EditorApplication.isPlaying=false; }
}
#endif
