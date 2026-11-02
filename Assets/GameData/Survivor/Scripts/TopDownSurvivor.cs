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
        [Range(3,20)] public float cameraDistance=9.5f;
        [Range(50,80)] public float fieldOfView=65f;
        public float cameraYaw;
        public float pivotHeight=1.2f;
        public float lookAhead=1.8f;
        private GameData.Player.Manager.InputManager input;
        private SurvivorAutoCombat combat;
        private CharacterHealth health;
        [Range(.02f,.3f)] public float followSmoothTime=.085f;
        private Vector3 smoothedFocus, previousPlayerPosition, followVelocity;
        private Rigidbody body;
        private PlayerRagdoll ragdoll;
        private bool initialized, orbiting;
        private float displayedYaw, displayedAngle, displayedDistance, collisionDistance;
        private float yawVelocity, pitchVelocity, zoomVelocity, collisionVelocity;
        private readonly RaycastHit[] cameraHits=new RaycastHit[64];

        public Vector3 MovementDirection(Vector2 move)
        {
            return Quaternion.Euler(0,initialized?displayedYaw:cameraYaw,0)*new Vector3(move.x,0,move.y);
        }
        private void Awake()
        {
            body=GetComponent<Rigidbody>();ragdoll=GetComponent<PlayerRagdoll>();input=GetComponent<GameData.Player.Manager.InputManager>();
            combat=GetComponent<SurvivorAutoCombat>();
            health=GetComponent<CharacterHealth>();
            if(!viewCamera)viewCamera=Camera.main;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        private void FixedUpdate()
        {
            if(Time.timeScale<=0||(health&&health.Health<=0)||(combat&&!combat.CanAttack))return;
            Vector3 facing=input?MovementDirection(input.Move):Vector3.zero;
            if(facing.sqrMagnitude<.001f&&combat&&combat.CurrentTarget)facing=Vector3.ProjectOnPlane(combat.CurrentTarget.transform.position-transform.position,Vector3.up);
            if(facing.sqrMagnitude<.001f)return;
            Quaternion rotation=Quaternion.Slerp(body?body.rotation:transform.rotation,Quaternion.LookRotation(facing),1-Mathf.Exp(-14*Time.fixedDeltaTime));
            if(body)body.MoveRotation(rotation);else transform.rotation=rotation;
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
                {
                    float scroll=mouse.scroll.ReadValue().y;
                    // Input System backends report either wheel ticks or 120-unit notches.
                    float notches=Mathf.Abs(scroll)>=20f?scroll/120f:scroll;
                    cameraDistance=Mathf.Clamp(cameraDistance-notches*.8f,3,20);
                }
            }
            if(dt<=0)return;
            if(!initialized){displayedYaw=cameraYaw;displayedAngle=cameraAngle;displayedDistance=collisionDistance=cameraDistance;}
            displayedYaw=Mathf.SmoothDampAngle(displayedYaw,cameraYaw,ref yawVelocity,.085f,Mathf.Infinity,dt);
            displayedAngle=Mathf.SmoothDampAngle(displayedAngle,cameraAngle,ref pitchVelocity,.085f,Mathf.Infinity,dt);
            displayedDistance=Mathf.SmoothDamp(displayedDistance,cameraDistance,ref zoomVelocity,.14f,Mathf.Infinity,dt);
            Quaternion rotation=Quaternion.Euler(displayedAngle,displayedYaw,0);
            Vector3 playerPosition=ragdoll&&ragdoll.IsRagdoll?ragdoll.FollowPosition-Vector3.up*.9f:transform.position;
            Vector3 focus=playerPosition+Vector3.up*pivotHeight+Quaternion.Euler(0,displayedYaw,0)*Vector3.forward*lookAhead;
            // Rotation stays in the physics step, so Rigidbody interpolation remains valid.
            bool teleport=initialized&&(playerPosition-previousPlayerPosition).sqrMagnitude>64;
            previousPlayerPosition=playerPosition;
            if(!initialized||teleport){smoothedFocus=focus;followVelocity=Vector3.zero;}
            else if(dt>0)smoothedFocus=Vector3.SmoothDamp(smoothedFocus,focus,ref followVelocity,followSmoothTime,Mathf.Infinity,dt);
            initialized=true;
            Vector3 backwards=-(rotation*Vector3.forward);
            float distance=displayedDistance;
            int count=Physics.SphereCastNonAlloc(smoothedFocus,.22f,backwards,cameraHits,distance,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var collider=cameraHits[i].collider;
                if(collider.transform.IsChildOf(transform) || collider.GetComponentInParent<CharacterHealth>())continue;
                distance=Mathf.Min(distance,Mathf.Max(.35f,cameraHits[i].distance-.12f));
            }
            // Retract immediately for safety, but ease back out after an obstruction clears.
            if(distance<collisionDistance){collisionDistance=distance;collisionVelocity=0;}
            else collisionDistance=Mathf.SmoothDamp(collisionDistance,distance,ref collisionVelocity,.18f,Mathf.Infinity,dt);
            viewCamera.transform.SetPositionAndRotation(smoothedFocus+backwards*collisionDistance,rotation);
            viewCamera.orthographic=false;viewCamera.fieldOfView=fieldOfView;
            viewCamera.nearClipPlane=.1f;viewCamera.farClipPlane=250;
        }
    }
}
