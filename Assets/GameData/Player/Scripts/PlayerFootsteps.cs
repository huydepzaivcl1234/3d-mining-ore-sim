using MiningSimulator.Ores;
using UnityEngine;

namespace GameData.Player.Script
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerController1))]
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        [SerializeField] private MiningAudioManager audioManager;
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
        private void PlayStep(bool isRight){
            if(Time.time<nextStep||!audioManager||grassClips.Length==0)return;
            int index=grassClips.Length==1?0:Random.Range(0,grassClips.Length-1);
            if(grassClips.Length>1&&index>=previousClip)index++;
            if(!grassClips[index])return;previousClip=index;
            var foot=isRight?rightFoot:leftFoot;
            audioManager.PlayWorldSfx(grassClips[index],foot?foot.position:transform.position,volume);
            right=!isRight;nextStep=Time.time+minimumInterval;StepsPlayed++;
        }
        private void OnJump(){if(!Active||!audioManager||!jumpClip)return;audioManager.PlayWorldSfx(jumpClip,transform.position,jumpVolume);nextStep=Time.time+.15f;JumpsPlayed++;}
        private void OnLanding(float speed){
            if(!Active||!audioManager||!landingClip||Time.time<nextLanding)return;
            Vector3 point=leftFoot&&rightFoot?(leftFoot.position+rightFoot.position)*.5f:transform.position;
            audioManager.PlayWorldSfx(landingClip,point,landingVolume*Mathf.Clamp(speed/5f,.4f,1f));nextLanding=Time.time+.2f;nextStep=Time.time+.16f;LandingsPlayed++;
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
