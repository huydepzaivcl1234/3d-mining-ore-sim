#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Explicit isolated validation. No real currency, inventory or placed-tower save is used.
[InitializeOnLoad]
public static class TowerConstructionPlayValidation
{
    const string Key="Tower.ConstructionValidation";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    [Serializable] class Root {public string id;public bool active;}
    [Serializable] class Snapshot {public Root[] roots;}
    static TowerBuildHud hud;
    static MiningItemSystem inventory;
    static PlayerWallet wallet;
    static TowerData data;
    static Camera camera;
    static PlayerInput input;
    static Keyboard keyboard;
    static Mouse mouse;
    static MiningOrbitCamera orbit;
    static StarterAssets.StarterAssetsInputs starter;
    static int stage,frame;
    static double started;
    static Vector3 cameraHome;
    static Vector2 placementPointer;
    static int upgradedLevel;
    static float upgradedHealth;
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
    static TowerConstructionPlayValidation(){EditorApplication.playModeStateChanged+=Changed;EditorApplication.update+=Tick;}
    public static void Begin()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene!=null)throw new InvalidOperationException("Stop Play; keep normal scene start.");
        var roots=SceneManager.GetActiveScene().GetRootGameObjects();
        SessionState.SetString(Key+".roots",JsonUtility.ToJson(new Snapshot {roots=roots.Select(r=>new Root {id=GlobalObjectId.GetGlobalObjectIdSlow(r).ToString(),active=r.activeSelf}).ToArray()}));
        SessionState.SetBool(Key+".background",Application.runInBackground);SessionState.SetBool(Key,true);SessionState.SetString(Key+".result","Running");
        foreach(var root in roots)root.SetActive(false);Application.runInBackground=true;EditorApplication.isPlaying=true;
    }
    static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Key,false))return;
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            foreach(var r in JsonUtility.FromJson<Snapshot>(SessionState.GetString(Key+".roots","")).roots)
                if(GlobalObjectId.TryParse(r.id,out var id)&&GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) is GameObject go)go.SetActive(r.active);
            Application.runInBackground=SessionState.GetBool(Key+".background",false);
            typeof(GameSave).GetField("testStore",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
            foreach(var device in InputSystem.devices.ToArray())if(device.name.StartsWith("Construction validation"))InputSystem.RemoveDevice(device);
            SessionState.SetBool(Key,false);hud=null;return;
        }
        if(state!=PlayModeStateChange.EnteredPlayMode)return;
        try
        {
            var isolatedSave=new GameSaveStore(System.IO.Path.GetFullPath("Temp/TowerDrawer-"+Guid.NewGuid().ToString("N")+".json"));
            // Reset only this newly created temporary store: disable fallback reads of
            // real legacy PlayerPrefs without touching those prefs or the player's file.
            Check(isolatedSave.Reset(),"Cannot initialize isolated save");
            typeof(GameSave).GetField("testStore",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,isolatedSave);
            EditorApplication.isPaused=false;
            camera=new GameObject("Construction camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,9,-12);camera.transform.LookAt(Vector3.zero);cameraHome=camera.transform.position;
            camera.backgroundColor=new Color(.13f,.22f,.18f);camera.clearFlags=CameraClearFlags.SolidColor;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=Vector3.down*.5f;floor.transform.localScale=new Vector3(40,1,40);
            var light=new GameObject("Construction light").AddComponent<Light>();light.type=LightType.Directional;
            var player=new GameObject("Construction player");player.SetActive(false);
            player.AddComponent<MiningPlayerStats>().enabled=false;player.AddComponent<MiningCharacterHealth>();
            starter=player.AddComponent<StarterAssets.StarterAssetsInputs>();
            input=player.AddComponent<PlayerInput>();input.actions=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Starter Assets/Runtime/InputSystem/StarterAssets.inputactions"));input.defaultActionMap="Player";
            player.SetActive(true);
            keyboard=InputSystem.AddDevice<Keyboard>("Construction validation keyboard");mouse=InputSystem.AddDevice<Mouse>("Construction validation mouse");
            input.SwitchCurrentControlScheme("KeyboardMouse",keyboard,mouse);
            var rig=new GameObject("Construction orbit");rig.SetActive(false);orbit=rig.AddComponent<MiningOrbitCamera>();
            Set(orbit,"controlledCamera",camera);Set(orbit,"followTarget",player.transform);rig.SetActive(true);
            var bag=new GameObject("Construction inventory");bag.SetActive(false);inventory=bag.AddComponent<MiningItemSystem>();wallet=bag.AddComponent<PlayerWallet>();
            inventory.enabled=wallet.enabled=false;bag.SetActive(true);Set(wallet,"currentMoney",1000f);
            data=AssetDatabase.LoadAssetAtPath<CannonTowerData>("Assets/GameData/Tower/Cannon/CannonData.asset");
            var stockDatabase=ScriptableObject.CreateInstance<MiningItemDatabase>();
            Set(stockDatabase,"items",new System.Collections.Generic.List<MiningItemData>{data.inventoryItem});
            Set(stockDatabase,"inventorySaveKey","ConstructionFixture.Inventory");Set(inventory,"database",stockDatabase);
            hud=TowerBuildHud.Ensure(wallet,null);hud.Open();hud.Shop.Select(data);
            var forecast=new GameObject("Forecast layout validation").AddComponent<MonsterDailyForecastHud>();forecast.enabled=false;
            typeof(MonsterDailyForecastHud).GetMethod("Build",Flags).Invoke(forecast,null);
            var forecastRoot=(RectTransform)typeof(MonsterDailyForecastHud).GetField("root",Flags).GetValue(forecast);
            Check(forecastRoot!=null&&forecastRoot.anchorMin==new Vector2(.5f,1)&&forecastRoot.anchorMax==new Vector2(.5f,1)&&
                forecastRoot.pivot==new Vector2(.5f,1)&&Mathf.Abs(forecastRoot.anchoredPosition.x)<.01f&&forecastRoot.anchoredPosition.y<0,"Forecast not anchored at top center");
            UnityEngine.Object.Destroy(forecast.gameObject);
            stage=0;frame=Time.frameCount;started=EditorApplication.timeSinceStartup;
        }catch(Exception e){Finish("FAIL setup: "+e.GetBaseException().Message);}
    }
    static void Check(bool ok,string error){if(!ok)throw new Exception(error);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||hud==null||frame==Time.frameCount)return;
        frame=Time.frameCount;
        try
        {
            if(EditorApplication.timeSinceStartup-started>25)throw new Exception("timeout stage "+stage);
            if(stage==0&&EditorApplication.timeSinceStartup-started>.8)
            {
                Check(hud.IsOpen,"Drawer did not open");
                var card=hud.Shop.GetComponentInChildren<TowerCardHover>();
                var bounds=((RectTransform)hud.Shop.transform).sizeDelta;
                card.OnPointerEnter(null);Check(hud.Shop.Previewed==data,"Hover did not show tower");
                card.OnPointerExit(null);Check(hud.Shop.Previewed==null,"Hover did not clear");
                Check(bounds==((RectTransform)hud.Shop.transform).sizeDelta,"Hover resized drawer");
                card.OnPointerEnter(null);UnityEngine.ScreenCapture.CaptureScreenshot("Temp/TowerConstructionDrawer.png");
                stage=10;return;
            }
            else if(stage==10)
            {
                Check(hud.Shop.BuySelected(),"Purchase rejected");
                Check(hud.IsOpen&&!TowerPlacement.IsPlacing&&inventory.GetItemCount(data.inventoryItem)==0,"Purchase closed drawer or added to bag");
                Check(hud.Shop.BuySelected()&&hud.Shop.Owned==2,"Repeated purchase failed");
                Check(hud.Shop.BuyOrPlace(),"Stock placement rejected");Check(TowerPlacement.IsPlacing&&!hud.IsOpen,"Modal handoff failed");
                Check(inventory.GetTowerStockCount(data.inventoryItem)==2&&Mathf.Abs(wallet.CurrentMoney-(1000-2*data.price))<.01f,"Duplicate or missing purchase");
                inventory.GetComponent<TowerPlacement>().Cancel();
                Check(!TowerPlacement.IsPlacing&&inventory.GetTowerStockCount(data.inventoryItem)==2,"Cancel consumed stock");
                Check(Vector3.Distance(camera.transform.position,cameraHome)<.001f,"Cancel did not restore camera");
                // Inventory entry can start with shift lock owning the cursor. Reproduce
                // the stale-owner release and focus callback, not just the drawer path.
                Set(orbit,"ownsCursor",true);Set(orbit,"previousCursorLock",CursorLockMode.Locked);
                Set(orbit,"previousCursorVisible",false);Set(orbit,"shiftLocked",true);
                Check(TowerPlacement.BeginPlacement(inventory,data.inventoryItem,-1),"Direct placement rejected");
                Check(!(bool)typeof(MiningOrbitCamera).GetField("ownsCursor",Flags).GetValue(orbit),"Shift-lock cursor owner was not released before placement");
                Check(!starter.cursorLocked,"Focus callback will relock placement cursor");
                typeof(StarterAssets.StarterAssetsInputs).GetMethod("OnApplicationFocus",Flags).Invoke(starter,new object[]{true});
                Check(Cursor.lockState==CursorLockMode.None&&Cursor.visible,"Focus regain locked or hid placement cursor");
                inventory.GetComponent<TowerPlacement>().Cancel();
                Check(starter.cursorLocked,"Placement did not restore Starter cursor policy");orbit.SetShiftLocked(false);
                Check(inventory.TryBuyConstructionTower(data,wallet),"Third purchase failed");hud.Open();
                Check(hud.Shop.Owned==3,"Owned count did not refresh");
                Check(hud.Shop.BuyOrPlace(),"Owned placement rejected");Check(Mathf.Abs(wallet.CurrentMoney-(1000-3*data.price))<.01f,"Owned placement spent gold");
                stage=1;started=EditorApplication.timeSinceStartup;
            }
            else if(stage==1&&EditorApplication.timeSinceStartup-started>.8)
            {
                var placement=inventory.GetComponent<TowerPlacement>();
                Set(placement,"placementRotation",Quaternion.Euler(0,90,0));
                placementPointer=camera.WorldToScreenPoint(new Vector3(3.25f,0,3.25f));
                InputSystem.QueueStateEvent(mouse,new MouseState {position=placementPointer});
                stage=11;started=EditorApplication.timeSinceStartup;
            }
            else if(stage==11&&EditorApplication.timeSinceStartup-started>.15)
            {
                Check(Cursor.lockState==CursorLockMode.None&&Cursor.visible,"Placement cursor did not stay unlocked");
                var ghost=GameObject.Find("Cannon ghost");
                Check(ghost!=null&&Vector3.Distance(ghost.transform.position,new Vector3(3,0,3))<.1f,"Mouse move did not move ghost to the pointed cell: ghost="+(ghost!=null?ghost.transform.position.ToString():"missing")+" pointer="+mouse.position.ReadValue()+" current="+Mouse.current?.position.ReadValue());
                InputSystem.QueueStateEvent(mouse,new MouseState {position=placementPointer}.WithButton(MouseButton.Left));
                stage=12;started=EditorApplication.timeSinceStartup;
            }
            else if(stage==12&&EditorApplication.timeSinceStartup-started>.15)
            {
                InputSystem.QueueStateEvent(mouse,new MouseState {position=placementPointer});
                Check(TowerPlacement.IsPlacing&&inventory.GetTowerStockCount(data.inventoryItem)==2,"Mouse click did not place exactly one tower");
                var placement=inventory.GetComponent<TowerPlacement>();
                placement.Cancel();Check(inventory.GetTowerStockCount(data.inventoryItem)==2,"Cancel lost remaining stock");
                Check(GameSave.HasKey("ConstructionFixture.Inventory"),"Construction stock was not saved");
                typeof(MiningItemSystem).GetMethod("LoadInventory",Flags).Invoke(inventory,null);
                typeof(MiningItemSystem).GetMethod("LoadInventory",Flags).Invoke(inventory,null);
                Check(inventory.GetTowerStockCount(data.inventoryItem)==2&&inventory.OccupiedSlotCount==0,"Reload duplicated or lost stock / filled bag");
                Check(TowerRuntime.Active.Count(t=>t!=null&&t.IsAlive)==1,"Wrong placed tower count");
                Check(Quaternion.Angle(TowerRuntime.Active.First(t=>t!=null&&t.IsAlive).transform.rotation,Quaternion.Euler(0,90,0))<.01f,"Placed rotation lost");
                var action=input.actions.FindAction("Player/Build");action.ApplyBindingOverride(0,"<Keyboard>/y");
                Check(action.GetBindingDisplayString(0).Equals("Y",StringComparison.OrdinalIgnoreCase),"Rebind display failed: "+action.GetBindingDisplayString(0));
                hud.Open();Check(hud.IsOpen,"Reopen failed");hud.CloseImmediately();Check(!hud.IsOpen,"Close failed");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Y));stage=2;started=EditorApplication.timeSinceStartup;
            }
            else if(stage==2&&EditorApplication.timeSinceStartup-started>.15)
            {
                Check(hud.IsOpen,"Rebound key did not open drawer");InputSystem.QueueStateEvent(keyboard,new KeyboardState());hud.CloseImmediately();
                var tower=TowerRuntime.Active.First(t=>t!=null&&t.IsAlive);
                input.transform.position=tower.transform.position+Vector3.back*2;
                tower.Interact();
                var panel=tower.GetComponent<TowerStatsPanel>();Check(panel!=null&&panel.IsOpen,"Tower stats did not open");
                var button=panel.Panel.Find("Upgrade").GetComponent<UnityEngine.UI.Button>();
                float before=wallet.CurrentMoney,cost=tower.UpgradeCost;int level=tower.Level;
                button.onClick.Invoke();
                Check(tower.Level==level+1&&Mathf.Abs(wallet.CurrentMoney-before+cost)<.01f,"Upgrade transaction failed");
                Check(TowerSaveDocument.TryRead(GameSave.GetString("ChestDefense.PlacedTowers.v1"),out var saved)&&saved.towers.Count==1&&saved.towers[0].level==tower.Level,"Upgrade level not saved");
                upgradedLevel=tower.Level;upgradedHealth=tower.Health.Health;
                orbit.BeginCinematicOverride();
                camera.transform.position=tower.transform.position+new Vector3(0,3,-5);
                camera.transform.LookAt(tower.transform.position+Vector3.up*2.2f);
                stage=3;started=EditorApplication.timeSinceStartup;
            }
            else if(stage==3&&EditorApplication.timeSinceStartup-started>.5)
            {
                UnityEngine.ScreenCapture.CaptureScreenshot("Temp/TowerUpgradePanel.png");
                stage=4;started=EditorApplication.timeSinceStartup;
            }
            else if(stage==4&&EditorApplication.timeSinceStartup-started>.1)
            {
                var tower=TowerRuntime.Active.First(t=>t!=null&&t.IsAlive);
                UnityEngine.Object.Destroy(tower.gameObject);
                stage=5;started=EditorApplication.timeSinceStartup;
            }
            else if(stage==5&&EditorApplication.timeSinceStartup-started>.2)
            {
                TowerWorldSave.Restore(SceneManager.GetActiveScene());
                var tower=TowerRuntime.Active.FirstOrDefault(t=>t!=null&&t.IsAlive);
                Check(tower!=null&&tower.Level==upgradedLevel&&Mathf.Abs(tower.Health.Health-upgradedHealth)<.01f,"Reload lost upgraded level or HP");
                Check(TowerRuntime.Active.Count(t=>t!=null&&t.IsAlive)==1,"Reload duplicated tower");
                Finish("PASS hover without resizing, upgrade button, save and actual world restore retaining level/HP, top-center forecast, repeated purchase, no bag slots, stock reload, cancel retention, mouse placement, cursor/focus, rotation, camera restore, rebound shortcut");
            }
        }catch(Exception e){Finish("FAIL "+e.GetBaseException().Message);}
    }
    static void Finish(string result){SessionState.SetString(Key+".result",result);EditorApplication.isPlaying=false;}
}
#endif
