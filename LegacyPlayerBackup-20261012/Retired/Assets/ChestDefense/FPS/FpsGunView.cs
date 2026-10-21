using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using MiningSimulator.Ores;

namespace ChestDefense.Fps
{
    [DisallowMultipleComponent]
    public sealed class FpsGunView : MonoBehaviour
    {
        public Animator rig;
        public Transform muzzle;
        [Tooltip("Iron-sight reference points, attached to the animated gun rather than the camera.")]
        public Transform rearSight, frontSight;
        public ParticleSystem muzzleFlash;
        public AudioSource audioSource;
        [Tooltip("Keep the animated hand bones, but hide their mesh for the gun-only trial.")]
        public Renderer[] handRenderers;
        public bool showHands;
        FpsWeaponData data;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable idle, shoot, reload;
        MiningAudioManager audioManager;
        int playing;
        float actionTime, weight;
        bool initialized;
        Vector3 alignedPosition;
        Quaternion alignedRotation;

        public bool Initialize(FpsWeaponData weapon, MiningAudioManager audioOwner)
        {
            data = weapon; audioManager = audioOwner;
            if (rig == null || muzzle == null) { Debug.LogError("FPS view needs a rig and muzzle.", this); return false; }
            rig.applyRootMotion = false;
            rig.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (handRenderers != null) foreach (var hand in handRenderers)
                if (hand != null) hand.enabled = showHands;
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                if (audioManager != null) audioManager.RegisterSfxSource(audioSource, 1f);
            }
            initialized = true; BuildGraph(); return true;
        }
        void BuildGraph()
        {
            DisposeGraph();
            graph = PlayableGraph.Create("FPS " + data.weaponId);
            // One clock: evaluate explicitly after changing action/pose weights.
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer = AnimationMixerPlayable.Create(graph, 3);
            idle = MakeClip(data.idle, 0); shoot = MakeClip(data.shoot, 1); reload = MakeClip(data.reload, 2);
            var output = AnimationPlayableOutput.Create(graph, "Gun and arms", rig);
            output.SetSourcePlayable(mixer); mixer.SetInputWeight(0, 1);
            ResetAnimation(); graph.Play(); graph.Evaluate(0);
            alignedPosition=data.aimPosition; alignedRotation=Quaternion.Euler(data.aimEuler);
        }
        AnimationClipPlayable MakeClip(AnimationClip clip, int index)
        {
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false); playable.SetApplyPlayableIK(false);
            graph.Connect(playable, 0, mixer, index); return playable;
        }
        void OnEnable() { if (initialized) BuildGraph(); }
        void OnDisable() { DisposeGraph(); if (audioSource != null) audioSource.Stop(); }
        void OnDestroy() => DisposeGraph();
        void DisposeGraph() { if (graph.IsValid()) graph.Destroy(); }
        public void ResetAnimation()
        {
            playing = 0; weight = 0; actionTime = 0;
            if (!graph.IsValid()) return;
            idle.SetTime(0); shoot.SetTime(0); reload.SetTime(0);
            mixer.SetInputWeight(0, 1); mixer.SetInputWeight(1, 0); mixer.SetInputWeight(2, 0);
        }
        public void PlayShot()
        {
            if (!graph.IsValid()) return;
            playing = 1; actionTime = 0; shoot.SetTime(0); shoot.SetSpeed(1);
            mixer.SetInputWeight(2, 0);
            weight = 1; mixer.SetInputWeight(0, 0); mixer.SetInputWeight(1, 1);
            graph.Evaluate(0);
            // Unity's unassigned serialized objects are not CLR null; ?. still throws.
            if (muzzleFlash != null) muzzleFlash.Play(true);
            PlaySound(data.shotSound);
        }
        public void PlayReload()
        {
            if (!graph.IsValid()) return;
            playing = 2; actionTime = 0; reload.SetTime(0);
            reload.SetSpeed(data.reload.length / data.reloadSeconds);
            mixer.SetInputWeight(1, 0); PlaySound(data.reloadSound);
            weight = 0; mixer.SetInputWeight(0, 1); mixer.SetInputWeight(2, 0);
        }
        public void PlayEmpty() => PlaySound(data.emptySound);
        void PlaySound(AudioClip clip)
        {
            // Never bypass the existing master/SFX settings when the audio owner is missing.
            if (clip != null && audioSource != null && audioManager != null) audioSource.PlayOneShot(clip, data.soundGain);
        }
        public void Tick(bool aiming, float dt)
        {
            if (!initialized || !graph.IsValid()) return;
            float blend = data.poseBlendSeconds <= 0 ? 1 : 1 - Mathf.Exp(-dt / data.poseBlendSeconds);
            if (idle.GetTime() >= data.idle.length && data.idle.length > 0) idle.SetTime(idle.GetTime() % data.idle.length);
            actionTime += dt;
            float duration = playing == 2 ? data.reloadSeconds : data.shoot.length;
            float fade = data.animationBlendSeconds;
            float target = playing != 0 && actionTime < duration - fade ? 1 : 0;
            weight = fade <= 0 ? target : Mathf.MoveTowards(weight, target, dt / fade);
            mixer.SetInputWeight(0, 1 - weight);
            mixer.SetInputWeight(1, playing == 1 ? weight : 0);
            mixer.SetInputWeight(2, playing == 2 ? weight : 0);
            if (weight == 0 && target == 0) playing = 0;
            graph.Evaluate(dt);
            // Idle clips include hand sway. Solve from the sights *after* animation,
            // otherwise a correct authoring pose still drifts off the center in Play Mode.
            // Keep the last idle solution during actions so recoil/reload remain visible.
            if(data.alignIronSights && rearSight!=null && frontSight!=null && playing==0)
            {
                var rear=transform.InverseTransformPoint(rearSight.position);
                var line=transform.InverseTransformPoint(frontSight.position)-rear;
                var up=transform.InverseTransformDirection(rearSight.up);
                if(line.sqrMagnitude>.000001f)
                {
                    alignedRotation=Quaternion.Inverse(Quaternion.LookRotation(line,up));
                    alignedPosition=new Vector3(data.aimSightOffset.x,data.aimSightOffset.y,data.aimEyeRelief)-alignedRotation*rear;
                }
            }
            var aimPosition=data.alignIronSights?alignedPosition:data.aimPosition;
            var aimRotation=data.alignIronSights?alignedRotation:Quaternion.Euler(data.aimEuler);
            transform.localPosition=Vector3.Lerp(transform.localPosition,aiming?aimPosition:data.hipPosition,blend);
            transform.localRotation=Quaternion.Slerp(transform.localRotation,aiming?aimRotation:Quaternion.Euler(data.hipEuler),blend);
        }
    }
}
