using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using MiningSimulator.Ores;

namespace GameData.Player.Script
{
    [DefaultExecutionOrder(10000), DisallowMultipleComponent]
    public sealed class TopDownSurvivor : MonoBehaviour
    {
        public Camera viewCamera;
        [Range(10,55)] public float cameraAngle=18f;
        [Range(5,14)] public float cameraDistance=9.5f;
        [Range(50,80)] public float fieldOfView=65f;
        public float cameraYaw;
        public float pivotHeight=1.2f;
        public float lookAhead=1.8f;
        private GameData.Player.Manager.InputManager input;
        private SurvivorAutoCombat combat;
        private CharacterHealth health;
        private Vector3 smoothedFocus;
        private bool initialized, orbiting;
        private readonly RaycastHit[] cameraHits=new RaycastHit[64];

        public Vector3 MovementDirection(Vector2 move)
        {
            return Quaternion.Euler(0,cameraYaw,0)*new Vector3(move.x,0,move.y);
        }
        private void Awake()
        {
            input=GetComponent<GameData.Player.Manager.InputManager>();
            combat=GetComponent<SurvivorAutoCombat>();
            health=GetComponent<CharacterHealth>();
            if(!viewCamera)viewCamera=Camera.main;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        private void LateUpdate()
        {
            if(!viewCamera)return;
            float dt=Time.deltaTime;
            var mouse=Mouse.current;
            bool controls=dt>0 && (!health||health.Health>0) && (!combat||combat.CanAttack);
            if(!controls || mouse==null || !mouse.rightButton.isPressed) orbiting=false;
            else if(mouse.rightButton.wasPressedThisFrame)
                orbiting=EventSystem.current==null || !EventSystem.current.IsPointerOverGameObject();
            if(controls && mouse!=null)
            {
                if(orbiting)
                {
                    Vector2 delta=mouse.delta.ReadValue();
                    cameraYaw=Mathf.Repeat(cameraYaw+delta.x*.12f,360);
                    cameraAngle=Mathf.Clamp(cameraAngle-delta.y*.12f,10,55);
                }
                if(EventSystem.current==null || !EventSystem.current.IsPointerOverGameObject())
                    cameraDistance=Mathf.Clamp(cameraDistance-mouse.scroll.ReadValue().y*.008f,5,14);
                Vector3 facing=input?MovementDirection(input.Move):Vector3.zero;
                if(facing.sqrMagnitude<.001f && combat && combat.CurrentTarget)
                    facing=Vector3.ProjectOnPlane(combat.CurrentTarget.transform.position-transform.position,Vector3.up);
                if(facing.sqrMagnitude>.001f)
                    transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(facing),1-Mathf.Exp(-14*dt));
            }
            Quaternion rotation=Quaternion.Euler(cameraAngle,cameraYaw,0);
            Vector3 focus=transform.position+Vector3.up*pivotHeight+Quaternion.Euler(0,cameraYaw,0)*Vector3.forward*lookAhead;
            smoothedFocus=initialized?Vector3.Lerp(smoothedFocus,focus,1-Mathf.Exp(-18*Mathf.Max(0,dt))):focus;
            initialized=true;
            Vector3 backwards=-(rotation*Vector3.forward);
            float distance=cameraDistance;
            int count=Physics.SphereCastNonAlloc(smoothedFocus,.22f,backwards,cameraHits,distance,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var collider=cameraHits[i].collider;
                if(collider.transform.IsChildOf(transform) || collider.GetComponentInParent<CharacterHealth>())continue;
                distance=Mathf.Min(distance,Mathf.Max(.35f,cameraHits[i].distance-.12f));
            }
            viewCamera.transform.SetPositionAndRotation(smoothedFocus+backwards*distance,rotation);
            viewCamera.orthographic=false;viewCamera.fieldOfView=fieldOfView;
            viewCamera.nearClipPlane=.1f;viewCamera.farClipPlane=250;
        }
    }
}
