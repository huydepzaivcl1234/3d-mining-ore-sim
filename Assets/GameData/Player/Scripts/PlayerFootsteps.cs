using MiningSimulator.Ores;
using UnityEngine;

namespace GameData.Player.Script
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerController1))]
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        [SerializeField] private MiningAudioManager audioManager;
        [SerializeField] private SurfaceAudioData surfaceAudio;
        private readonly RaycastHit[] groundHits=new RaycastHit[24];
        public int LastSurfaceLayer{get;private set;}=-1;
        private AudioClip[] previousSet;
        [SerializeField] private AudioClip[] grassClips=System.Array.Empty<AudioClip>();
        [SerializeField] private AudioClip jumpClip,landingClip;
        [SerializeField] private Transform leftFoot,rightFoot;
        [SerializeField] private bool useAnimationEvents=true;
        [SerializeField,Min(.1f)] private float walkStepMeters=1.2f,runStepMeters=1.6f;
        [SerializeField,Min(.05f)] private float minimumInterval=.18f;
        [SerializeField,Range(0,1)] private float volume=.65f,jumpVolume=.5f,landingVolume=.7f;
        private PlayerController1 controller;
        private GameData.Player.Manager.InputManager input;
        private AnimationRunMana stamina;
        private CharacterHealth health;
        private Rigidbody body;
        private Vector3 previousPosition;
        private float travelled,nextStep,nextLanding;
        private int previousClip=-1;
        private bool right;
        public int StepsPlayed{get;private set;}
        public int JumpsPlayed{get;private set;}
        public int LandingsPlayed{get;private set;}
        private void Awake(){
            controller=GetComponent<PlayerController1>();input=GetComponent<GameData.Player.Manager.InputManager>();
            stamina=GetComponent<AnimationRunMana>();health=GetComponent<CharacterHealth>();body=GetComponent<Rigidbody>();
            if(!audioManager)audioManager=FindAnyObjectByType<MiningAudioManager>();
            var animator=GetComponent<Animator>();
            if(animator&&animator.isHuman){if(!leftFoot)leftFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);if(!rightFoot)rightFoot=animator.GetBoneTransform(HumanBodyBones.RightFoot);}
            controller.Jumped+=OnJump;controller.Landed+=OnLanding;
        }
        private void OnDestroy(){if(controller){controller.Jumped-=OnJump;controller.Landed-=OnLanding;}}
        private void OnEnable(){previousPosition=transform.position;travelled=0;nextStep=nextLanding=0;}
        private bool Active=>isActiveAndEnabled&&Time.timeScale>0&&controller&&controller.enabled&&(!health||health.Health>0);
        public void OnFootstep(AnimationEvent evt){
            if(!useAnimationEvents||evt!=null&&evt.animatorClipInfo.weight<.3f)return;
            bool moving=body&&new Vector2(body.linearVelocity.x,body.linearVelocity.z).sqrMagnitude>.04f;
            if(!Active||!controller.IsGrounded||!moving||input&&input.Move.sqrMagnitude<.01f)return;
            PlayStep(evt!=null?evt.intParameter==1:right);
        }
        public SurfaceAudioData.Surface SurfaceAt(Vector3 point)
        {
            int count=Physics.RaycastNonAlloc(point+Vector3.up*.5f,Vector3.down,groundHits,2.5f,GroundSurfaceLayers.Mask,QueryTriggerInteraction.Ignore);
            float nearest=float.PositiveInfinity;LastSurfaceLayer=-1;
            for(int i=0;i<count;i++){var hit=groundHits[i];if(hit.transform.IsChildOf(transform)||hit.distance>=nearest)continue;nearest=hit.distance;LastSurfaceLayer=hit.collider.gameObject.layer;}
            return surfaceAudio&&LastSurfaceLayer>=0?surfaceAudio.ForLayer(LastSurfaceLayer):null;
        }
        private void PlayStep(bool isRight){
            if(Time.time<nextStep||!audioManager)return;
            var foot=isRight?rightFoot:leftFoot;var point=foot?foot.position:transform.position;
            var surface=SurfaceAt(point);var clips=surface!=null&&surface.footsteps.Length>0?surface.footsteps:surfaceAudio&&surfaceAudio.fallbackSteps.Length>0?surfaceAudio.fallbackSteps:grassClips;
            if(clips.Length==0)return;if(previousSet!=clips){previousClip=-1;previousSet=clips;}
            int index=clips.Length==1?0:Random.Range(0,previousClip>=0?clips.Length-1:clips.Length);
            if(clips.Length>1&&index>=previousClip&&previousClip>=0)index++;
            if(!clips[index])return;previousClip=index;
            audioManager.PlayWorldSfx(clips[index],point,volume);
            right=!isRight;nextStep=Time.time+minimumInterval;StepsPlayed++;
        }
        private void OnJump(){
            if(!Active||!audioManager)return;var surface=SurfaceAt(transform.position);var clip=surface!=null&&surface.jump?surface.jump:jumpClip;if(!clip)return;
            audioManager.PlayWorldSfx(clip,transform.position,jumpVolume);nextStep=Time.time+.15f;JumpsPlayed++;
        }
        private void OnLanding(float speed){
            if(!Active||!audioManager||Time.time<nextLanding)return;
            Vector3 point=leftFoot&&rightFoot?(leftFoot.position+rightFoot.position)*.5f:transform.position;
            var surface=SurfaceAt(point);var clip=surface!=null&&surface.landing?surface.landing:landingClip;if(!clip)return;
            audioManager.PlayWorldSfx(clip,point,landingVolume*Mathf.Clamp(speed/5f,.4f,1f));nextLanding=Time.time+.2f;nextStep=Time.time+.16f;LandingsPlayed++;
        }
        private void Update(){
            Vector3 delta=transform.position-previousPosition;previousPosition=transform.position;delta.y=0;
            if(useAnimationEvents)return;
            float distance=delta.magnitude;
            if(!Active||!controller.IsGrounded||input&&input.Move.sqrMagnitude<.01f||distance>Mathf.Max(2,Time.deltaTime*20)){travelled=0;return;}
            travelled+=distance;float stride=input&&input.Run&&(!stamina||stamina.CanRun)?runStepMeters:walkStepMeters;
            if(travelled>=stride){travelled=Mathf.Repeat(travelled,stride);PlayStep(right);}
        }
    }
}
