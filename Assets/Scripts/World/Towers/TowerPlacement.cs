using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace MiningSimulator.Ores
{
    public sealed class TowerPlacement : MonoBehaviour
    {
        static TowerPlacement active;
        public static bool IsPlacing=>active!=null&&active.item!=null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetPlacement()=>active=null;
        MiningItemSystem inventory;MiningItemData item;TowerPlacementVisuals visuals;
        Transform player;Camera cameraView;MiningOrbitCamera orbit;
        Vector3 savedPosition,focus;Quaternion savedRotation;
        float savedFov,started;bool savedOrthographic,savedCursorVisible;CursorLockMode savedCursor;int slotIndex;
        public static bool BeginPlacement(MiningItemSystem inventory,MiningItemData item,int index)
        {
            if(item?.Tower?.prefab==null||inventory==null||inventory.GetItemCount(item)==0)return false;
            var player=FindFirstObjectByType<MiningPlayerStats>();var camera=Camera.main;var orbit=FindFirstObjectByType<MiningOrbitCamera>();
            if(player==null||camera==null||player.GetComponent<MiningCharacterHealth>()?.Health<=0||orbit!=null&&orbit.CinematicOverrideActive)return false;
            if(active!=null)active.Cancel();
            var p=inventory.GetComponent<TowerPlacement>()??inventory.gameObject.AddComponent<TowerPlacement>();
            p.inventory=inventory;p.item=item;p.slotIndex=index;p.player=player.transform;p.cameraView=camera;p.orbit=orbit;p.focus=player.transform.position;
            p.savedPosition=camera.transform.position;p.savedRotation=camera.transform.rotation;p.savedFov=camera.fieldOfView;p.savedOrthographic=camera.orthographic;
            p.savedCursor=Cursor.lockState;p.savedCursorVisible=Cursor.visible;p.started=Time.unscaledTime;active=p;
            if(orbit!=null){orbit.BeginCinematicOverride();orbit.SetInputLocked(true);}
            camera.orthographic=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            p.visuals=new TowerPlacementVisuals(item.Tower,player.transform,camera);p.visuals.Hide();return true;
        }
        public void Cancel()
        {
            if(item==null)return;
            visuals?.Dispose();visuals=null;item=null;if(active==this)active=null;
            if(cameraView!=null){cameraView.transform.SetPositionAndRotation(savedPosition,savedRotation);cameraView.fieldOfView=savedFov;cameraView.orthographic=savedOrthographic;}
            if(orbit!=null){orbit.EndCinematicOverride(focus);var c=FindFirstObjectByType<MiningUiPanelCoordinator>();orbit.SetInputLocked(c!=null&&c.BlocksGameplay);}
            Cursor.lockState=savedCursor;Cursor.visible=savedCursorVisible;
        }
        void OnDisable()=>Cancel();
        void LateUpdate()
        {
            if(item==null||cameraView==null)return;
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.unscaledTime-started)/.35f));
            var position=Vector3.Lerp(savedPosition,focus+Vector3.up*25,t);var rotation=Quaternion.Slerp(savedRotation,Quaternion.Euler(90,0,0),t);
            if(orbit!=null)orbit.SetCinematicPose(position,rotation,60);else{cameraView.transform.SetPositionAndRotation(position,rotation);cameraView.fieldOfView=60;}
        }
        void Update()
        {
            if(item==null)return;
            if(player==null||player.GetComponent<MiningCharacterHealth>()?.Health<=0||cameraView==null||Keyboard.current?.escapeKey.wasPressedThisFrame==true||Mouse.current?.rightButton.wasPressedThisFrame==true){Cancel();return;}
            if(Mouse.current==null||Time.unscaledTime-started<.35f)return;
            bool valid=PreviewAt(cameraView.ScreenPointToRay(Mouse.current.position.ReadValue()),out var foot);
            if(valid&&Mouse.current.leftButton.wasPressedThisFrame&&(EventSystem.current==null||!EventSystem.current.IsPointerOverGameObject())&&inventory.TryTakeTowerAt(slotIndex,item,out float paid))
            {var placed=Instantiate(item.Tower.prefab,foot,Quaternion.identity);var runtime=placed.GetComponent<TowerRuntime>();runtime.Initialize(item.Tower,paid);runtime.AlignToGround();Cancel();}
        }
        public bool PreviewAt(Ray ray,out Vector3 foot)
        {
            foot=default;
            if(item==null)return false;
            if(!TowerPlacementGeometry.RayGround(ray,100,out var hit)){visuals.Hide();return false;}
            var body=item.Tower.prefab.GetComponent<BoxCollider>();foot=TowerPlacementGeometry.Snap(hit.point,body,1);
            bool valid=TowerPlacementGeometry.Validate(ref foot,body,player,12);
            visuals.Show();visuals.Move(foot,valid);return valid;
        }
    }
}
