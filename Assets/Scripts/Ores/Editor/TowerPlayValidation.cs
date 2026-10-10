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
    private static TowerDamageHealthBar damageBar;
    private static bool repeatChecked, expiryChecked;
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
            damageBar=tower.GetComponent<TowerDamageHealthBar>();repeatChecked=expiryChecked=false;
            if(damageBar==null||damageBar.IsVisible)throw new Exception("Damage bar must start hidden");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Placement support";
            floor.transform.position=tower.transform.position+Vector3.down*.5f;floor.transform.localScale=new Vector3(60,1,60);
            var playerObject=new GameObject("Stats distance fixture");statsPlayer=playerObject.transform;
            statsPlayer.position=tower.transform.position+Vector3.right*2;
            var playerHealth=playerObject.AddComponent<MiningCharacterHealth>();playerHealth.ConfigureSpawnHealth(100);
            Physics.SyncTransforms();
            VerifySaveRoundTrip(d);
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
        if(EditorApplication.timeSinceStartup-wallStart>40) { Finish("FAIL timeout");return; }
        if(phase==0)
        {
            recoil |= animator.GetCurrentAnimatorStateInfo(0).IsName("Cannon_Fire");
            recoilMoved |= Vector3.Distance(recoilPivot.position,restingPivot)>.01f;
            if(Time.time-start<1.2f) return;
            if(!recoil||!recoilMoved||Mathf.Abs(victim.Health-(100-tower.Data.damage))>.01f) {Finish("FAIL recoil/damage HP="+victim.Health+" recoil="+recoil+" moved="+recoilMoved);return;}
            var foundation=tower.GetComponentsInChildren<MeshFilter>().First(r=>r.name=="BaseMesh");
            foreach(float yaw in new[]{0f,45f,-76f,90f,180f})
            {
                tower.transform.rotation=Quaternion.Euler(0,yaw,0);tower.AlignToGround();
                if(Mathf.Abs(foundation.sharedMesh.vertices.Min(v=>foundation.transform.TransformPoint(v).y)-tower.transform.position.y)>.001f)
                {Finish("FAIL actual mesh feet float at yaw="+yaw);return;}
            }
            tower.transform.rotation=Quaternion.identity;
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
        else if(phase==2)
        {
            if(Time.time-start<=5.3f)return;
            if(Mathf.Abs(victim.Health-60)>.05f){Finish("FAIL apple total HP="+victim.Health);return;}
            var playerStats=statsPlayer.gameObject.AddComponent<MiningPlayerStats>();
            typeof(MiningPlayerStats).GetField("saveBlocked",Flags).SetValue(playerStats,true);
            placementInventory=UnityEngine.Object.FindObjectsByType<MiningItemSystem>(FindObjectsSortMode.None).First(x=>x.name=="Isolated consumable inventory");
            var item=tower.Data.inventoryItem;placementInventory.TryAddItem(item,3);
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
            try { VerifyRepeatedPlacement(); VerifyPlayerPassThrough(); VerifySkullclawUnderTowerFire(); }
            catch(Exception e) { Finish("FAIL combat/collision "+e.GetBaseException().Message);return; }
            if(TowerPlacement.IsPlacing||Vector3.Distance(originalCameraPosition,testCamera.transform.position)>.001f||Quaternion.Angle(originalCameraRotation,testCamera.transform.rotation)>.1f||placementInventory.GetItemCount(tower.Data.inventoryItem)!=1){Finish("FAIL cancel restoration/receipt");return;}
            if(damageBar.IsVisible){Finish("FAIL HP should be hidden before damage");return;}
            statsPlayer.position=tower.transform.position+Vector3.right*2;towerStats.Show(statsPlayer);
            testCamera.transform.position=tower.transform.position+new Vector3(0,3,-8);testCamera.transform.LookAt(tower.transform.position+Vector3.up*2.5f);
            start=Time.unscaledTime;phase=4;
        }
        else if(phase==4 && Time.unscaledTime-start>1f)
        {
            CaptureFantasyUi();phase=6;
        }
        else if(phase==6)
        {
            tower.Health.DealDamage(tower.Health.MaxHealth*.82f,CombatDamageType.True);
            if(!damageBar.IsVisible){Finish("FAIL damage reveal");return;}
            var criticalFill=damageBar.Panel.Find("HealthTrack/Clip/HealthFill").GetComponent<UnityEngine.UI.Image>();
            if(criticalFill.color.r<.9f||criticalFill.color.g>.5f){Finish("FAIL critical HP color");return;}
            start=Time.unscaledTime;phase=7;
        }
        else if(phase==7 && Time.unscaledTime-start>1f)
        {
            if(!damageBar.IsVisible){Finish("FAIL early HP hide");return;}
            tower.Health.DealDamage(tower.Health.MaxHealth*.01f,CombatDamageType.True);
            start=Time.unscaledTime;phase=5;
        }
        else if(phase==5)
        {
            if(!repeatChecked && Time.unscaledTime-start>.5f && Time.unscaledTime-start<2.8f){repeatChecked=true;if(!damageBar.IsVisible){Finish("FAIL repeated hit did not extend deadline");return;}}
            if(Time.unscaledTime-start<=3.15f)return;
            if(damageBar.IsVisible){Finish("FAIL HP did not hide after three seconds");return;}
            Finish("PASS player tower pass-through/re-enabled controller, retained monster collision/placement exclusion, Skullclaw swipes/jump/contact/recovery under tower fire, placed tower save/load HP/receipt/pose, duplicate prevention/removal, actual mesh feet at 5 yaws, fantasy stats, damage HP reveal/repeated-hit deadline/3-second hide/critical color, support, grid, stats X/distance hide, placement camera/overlays/cancel restore, recoil, damage, blocking, healing; isolated roots restored");
        }
    }
    private static void VerifySaveRoundTrip(CannonTowerData data)
    {
        // A unique temporary prefs key; never read/write the player's placed-tower save.
        var type=typeof(TowerWorldSave);var flags=BindingFlags.NonPublic|BindingFlags.Static;
        var key=type.GetField("Key",flags);var reset=type.GetMethod("ResetSession",flags);
        string scratch="TowerValidation."+Guid.NewGuid().ToString("N");
        reset.Invoke(null,null);key.SetValue(null,scratch);
        TowerRuntime fixture=null,restored=null;
        try
        {
            fixture=UnityEngine.Object.Instantiate(data.prefab,tower.transform.position+Vector3.right*12,Quaternion.Euler(0,76,0)).GetComponent<TowerRuntime>();
            fixture.Initialize(data,123);fixture.Health.RestoreSavedHealth(17);
            TowerWorldSave.Register(fixture);TowerWorldSave.Register(fixture);
            if(!TowerSaveDocument.TryRead(GameSave.GetString(scratch),out var saved)||saved.towers.Count!=1)throw new Exception("Save duplicate/codec");
            string id=fixture.PlacementId;fixture.gameObject.SetActive(false);UnityEngine.Object.DestroyImmediate(fixture.gameObject);fixture=null;
            reset.Invoke(null,null);key.SetValue(null,scratch);
            TowerWorldSave.Restore(tower.gameObject.scene);TowerWorldSave.Restore(tower.gameObject.scene);
            var matches=TowerRuntime.Active.Where(x=>x.PlacementId==id).ToArray();
            if(matches.Length!=1)throw new Exception("Restore missing/duplicated tower");restored=matches[0];
            if(Mathf.Abs(restored.Health.Health-17)>.001f||restored.PaidPrice!=123||Mathf.Abs(Mathf.DeltaAngle(restored.transform.eulerAngles.y,76))>.01f||Vector3.Distance(restored.transform.position,saved.towers[0].position)>.001f)throw new Exception("Restore HP/receipt/pose");
            TowerWorldSave.Remove(restored);
            if(!TowerSaveDocument.TryRead(GameSave.GetString(scratch),out saved)||saved.towers.Count!=0)throw new Exception("Remove left stale placement");
            if(TowerSaveDocument.TryRead("{\"version\":2,\"towers\":[]}",out _)||TowerSaveDocument.TryRead("bad-json",out _))throw new Exception("Invalid save accepted");
        }
        finally
        {
            if(fixture!=null){fixture.MarkDeployed(null);UnityEngine.Object.DestroyImmediate(fixture.gameObject);}
            if(restored!=null){restored.MarkDeployed(null);UnityEngine.Object.DestroyImmediate(restored.gameObject);}
            GameSave.DeleteKey(scratch);GameSave.Save();reset.Invoke(null,null);
        }
    }
    private static void VerifyRepeatedPlacement()
    {
        var placement=placementInventory.GetComponent<TowerPlacement>();var item=tower.Data.inventoryItem;
        var type=typeof(TowerWorldSave);var flags=BindingFlags.NonPublic|BindingFlags.Static;
        var key=type.GetField("Key",flags);var reset=type.GetMethod("ResetSession",flags);
        string scratch="TowerPlacementValidation."+Guid.NewGuid().ToString("N");
        reset.Invoke(null,null);key.SetValue(null,scratch);
        var place=typeof(TowerPlacement).GetMethod("PlaceAt",Flags);
        try {
            var foot=tower.transform.position+new Vector3(5,0,5);
            if(!(bool)place.Invoke(placement,new object[]{foot})||!TowerPlacement.IsPlacing||placementInventory.GetItemCount(item)!=2)throw new Exception("First placement ended mode/count");
            var label=GameObject.Find("Placement quantity").GetComponentsInChildren<TMPro.TMP_Text>().First(x=>x.name=="Remaining");
            if(!label.text.EndsWith("x2"))throw new Exception("Remaining label not refreshed");
            if((bool)place.Invoke(placement,new object[]{foot})||placementInventory.GetItemCount(item)!=2)throw new Exception("Occupied click consumed tower");
            if(!(bool)place.Invoke(placement,new object[]{foot+Vector3.right*2})||!TowerPlacement.IsPlacing||placementInventory.GetItemCount(item)!=1)throw new Exception("Second placement ended mode/count");
            placement.Cancel();
            if(!TowerPlacement.BeginPlacement(placementInventory,item,0))throw new Exception("Resume remaining stack");
            if(!(bool)place.Invoke(placement,new object[]{foot+Vector3.right*4})||TowerPlacement.IsPlacing||placementInventory.GetItemCount(item)!=0)throw new Exception("Empty stack did not exit");
            placementInventory.TryAddItem(item); // Existing cancellation verification checks this final unplaced unit.
        } finally {
            placement.Cancel();
            foreach(var placed in TowerRuntime.Active.ToArray())if(!string.IsNullOrEmpty(placed.PlacementId)){placed.MarkDeployed(null);UnityEngine.Object.DestroyImmediate(placed.gameObject);}
            GameSave.DeleteKey(scratch);GameSave.Save();reset.Invoke(null,null);
        }
    }
    private static void VerifyPlayerPassThrough()
    {
        Vector3 original = statsPlayer.position;
        var controller = statsPlayer.gameObject.AddComponent<CharacterController>();
        controller.height = 1.8f; controller.radius = .2f; controller.center = Vector3.up * .9f;
        try
        {
            for (int pass = 0; pass < 2; pass++)
            {
                controller.enabled = false;
                statsPlayer.position = tower.transform.position + new Vector3(-2,.02f,0);
                controller.enabled = true;
                Physics.SyncTransforms();
                typeof(TowerRuntime).GetMethod("RefreshPlayerCollision", Flags).Invoke(tower,null);
                var body = tower.GetComponent<BoxCollider>();
                if (!body.enabled || body.isTrigger || !Physics.GetIgnoreCollision(body,controller))
                    throw new Exception("Player/tower collision not selectively ignored");
                for (int i=0;i<20;i++) controller.Move(Vector3.right*.2f);
                if (statsPlayer.position.x < tower.transform.position.x+1.5f)
                    throw new Exception("Player could not cross solid tower, pass="+pass);
                if (Physics.GetIgnoreCollision(body,victim.GetComponent<Collider>()))
                    throw new Exception("Monster/tower collision was also ignored");
                var occupied = tower.transform.position;
                if (TowerPlacementGeometry.Validate(ref occupied,body,statsPlayer,12))
                    throw new Exception("Pass-through tower no longer blocks placement");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(controller);statsPlayer.position=original;Physics.SyncTransforms(); }
    }
    private static void VerifySkullclawUnderTowerFire()
    {
        var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameData/Monsters/Skullclaw/Skullclaw.prefab"),
            tower.transform.position+Vector3.right*20,Quaternion.identity);
        var goal=new GameObject("Skullclaw attack fixture");
        try
        {
            var shape=goal.AddComponent<BoxCollider>();shape.center=Vector3.up*.6f;shape.size=new Vector3(.4f,1.2f,.4f);
            var hp=goal.AddComponent<MiningCharacterHealth>();hp.ConfigureSpawnHealth(10000);hp.ConfigureRegeneration(0,5);
            var monster=actor.GetComponent<MushroomMonster>();monster.Initialize(null,hp);monster.enabled=false;
            monster.Health.ConfigureSpawnHealth(10000);monster.Health.ConfigureRegeneration(0,5);
            var anim=actor.GetComponentInChildren<Animator>();anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var begin=typeof(MushroomMonster).GetMethod("BeginAttack",Flags);
            var update=typeof(MushroomMonster).GetMethod("Update",Flags);
            var animation=typeof(MushroomMonster).GetField("animationState",Flags);
            Vector3 initial=actor.transform.position;
            for(int step=0;step<3;step++)
            {
                goal.transform.position=actor.transform.position+Vector3.forward*(step==2?monster.EffectiveAttackRange+1.5f:monster.EffectiveAttackRange*.6f);
                Physics.SyncTransforms();
                begin.Invoke(monster,null);
                if(monster.SkullclawAttackStep!=step)throw new Exception("Skullclaw wrong attack="+monster.SkullclawAttackStep);
                bool contact=false, recovered=false;float maxLift=0;
                float before=hp.Health;
                for(int frame=0;frame<400;frame++)
                {
                    monster.Health.DealDamage(.1f,CombatDamageType.True,tower.gameObject);
                    anim.Update(.025f);update.Invoke(monster,null);
                    maxLift=Mathf.Max(maxLift,actor.transform.position.y-initial.y);
                    contact|=hp.Health<before;
                    if((int)animation.GetValue(monster)==Animator.StringToHash("Idle")){recovered=true;break;}
                }
                if(!contact||!recovered)throw new Exception("Skullclaw tower-fire attack="+step+" contact="+contact+" recovered="+recovered);
                if(step==2&&(maxLift<.1f||Vector3.ProjectOnPlane(actor.transform.position-initial,Vector3.up).magnitude<1f))
                    throw new Exception("Skullclaw jump did not travel/land");
            }
        }
        finally {UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(goal);}
    }
    private static void Finish(string result) { if(testDatabase!=null) UnityEngine.Object.DestroyImmediate(testDatabase);SessionState.SetString(Key+".result",result);EditorApplication.isPlaying=false; }
    private static void CaptureFantasyUi()
    {
        var rt=new RenderTexture(1000,1000,24);var previous=testCamera.targetTexture;var active=RenderTexture.active;
        var png=new Texture2D(1000,1000,TextureFormat.RGB24,false);
        try{testCamera.targetTexture=rt;Canvas.ForceUpdateCanvases();testCamera.Render();RenderTexture.active=rt;png.ReadPixels(new Rect(0,0,1000,1000),0,0);png.Apply();
            System.IO.Directory.CreateDirectory("Docs/Validation");System.IO.File.WriteAllBytes("Docs/Validation/CannonFantasyUI.png",png.EncodeToPNG());}
        finally{testCamera.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(png);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
}
#endif
