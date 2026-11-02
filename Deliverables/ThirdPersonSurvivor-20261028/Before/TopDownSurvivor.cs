using UnityEngine;
using UnityEngine.InputSystem;
using MiningSimulator.Ores;

namespace GameData.Player.Script
{
    [DefaultExecutionOrder(10000), DisallowMultipleComponent]
    public sealed class TopDownSurvivor : MonoBehaviour
    {
        public Camera viewCamera;
        [Range(60,90)] public float cameraAngle=85f;
        [Min(5)] public float cameraHeight=20f;
        [Min(3)] public float cameraSize=9.5f;
        public float cameraYaw;
        private GameData.Player.Manager.InputManager input;
        private SurvivorAutoCombat combat;
        private MiningCharacterHealth health;
        private Vector3 lastCamera;
        private bool initialized;
        public Vector3 MovementDirection(Vector2 move)
        {
            var yaw=Quaternion.Euler(0,cameraYaw,0);
            return yaw*new Vector3(move.x,0,move.y);
        }
        private void Awake()
        {
            input=GetComponent<GameData.Player.Manager.InputManager>();
            combat=GetComponent<SurvivorAutoCombat>();health=GetComponent<MiningCharacterHealth>();
            if(!viewCamera)viewCamera=Camera.main;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        private void LateUpdate()
        {
            if(!viewCamera)return;
            float dt=Time.deltaTime;
            if(dt>0 && (!health||health.Health>0))
            {
                Vector3 facing=input?MovementDirection(input.Move):Vector3.zero;
                if(facing.sqrMagnitude<.001f && combat && combat.CurrentTarget)
                    facing=Vector3.ProjectOnPlane(combat.CurrentTarget.transform.position-transform.position,Vector3.up);
                if(facing.sqrMagnitude>.001f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(facing),1-Mathf.Exp(-14*dt));
            }
            Quaternion rotation=Quaternion.Euler(cameraAngle,cameraYaw,0);
            Vector3 focus=transform.position+Vector3.up*.7f;
            Vector3 desired=focus-rotation*Vector3.forward*(cameraHeight/Mathf.Sin(cameraAngle*Mathf.Deg2Rad));
            lastCamera=initialized?Vector3.Lerp(lastCamera,desired,1-Mathf.Exp(-18*Mathf.Max(0,dt))):desired;
            initialized=true;viewCamera.transform.SetPositionAndRotation(lastCamera,rotation);
            viewCamera.orthographic=true;viewCamera.orthographicSize=cameraSize;
            viewCamera.nearClipPlane=.1f;viewCamera.farClipPlane=250;
        }
    }
}
