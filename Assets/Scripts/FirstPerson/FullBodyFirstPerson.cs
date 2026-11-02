using UnityEngine;
using UnityEngine.Rendering;
using GameData.Player.Script;

// Based on the supplied script. One pose writer, no viewmodel Animator.
[DefaultExecutionOrder(10000), DisallowMultipleComponent]
public class FullBodyFirstPerson : MonoBehaviour
{
    public Animator animator;
    public Transform bodyRoot;
    public Camera cam;
    public Renderer[] headRenderers;
    public SkinnedMeshRenderer bindSkin;
    public WeaponAnchor weapon;
    public PistolShooting shooting;
    public bool readLegacyMouse;
    public float sensitivity=2f, minPitch=-80f, maxPitch=80f;
    public float Yaw {get;private set;}
    public float Pitch {get;private set;}
    public Vector3 eyeOffset=new Vector3(0,.04f,.14f);
    public float nearClip=.02f;
    public bool hideHeadByScale;
    public float spine=.2f,chest=.3f,upperChest=.2f,neck=.1f,head=.1f;
    public float drawSeconds=.65f;
    [Tooltip("Keep the pistol grip frame consistent with the shoulders when looking down. Does not change the authored animation or the approved upward pose.")]
    public bool stabilizeDownwardWeaponPose=true;
    public bool Drawing => drawRemaining>0;
    [Header("Independent world body gaze")]
    public Animator worldAnimator;
    [Range(10f, 80f)] public float headTurnLimit=65f;
    public float bodyTurnSpeed=240f;
    Transform worldHead;
    Transform[] worldChain;
    Quaternion[] worldPrevious;
    Quaternion worldHeadBind, worldRootRest;
    float worldYaw;
    bool worldApplied, worldTurning;
    public float WorldGazeError {get;private set;}
    public float WorldBodyYaw => worldYaw;
    public float WorldHeadYaw => Mathf.DeltaAngle(worldYaw,Yaw);
    Transform headBone;Transform[] chain,bones;
    Quaternion[] previous;
    Quaternion headBind;
    Vector3 headScale;
    bool applied,wasEquipped;
    float drawRemaining;
    static readonly int DrawingId=Animator.StringToHash("Drawing");
    void Awake()
    {
        if(!animator)animator=GetComponent<Animator>();
        if(!bodyRoot)bodyRoot=transform;
        if(!cam)cam=Camera.main;
        if(!shooting)shooting=GetComponent<PistolShooting>();
        if(!animator||!animator.isHuman||!cam){enabled=false;return;}
        headBone=animator.GetBoneTransform(HumanBodyBones.Head);
        chain=new[]{animator.GetBoneTransform(HumanBodyBones.Spine),animator.GetBoneTransform(HumanBodyBones.Chest),animator.GetBoneTransform(HumanBodyBones.UpperChest),animator.GetBoneTransform(HumanBodyBones.Neck),headBone};
        var list=new System.Collections.Generic.List<Transform>();
        for(int i=0;i<(int)HumanBodyBones.LastBone;i++){var b=animator.GetBoneTransform((HumanBodyBones)i);if(b&&!list.Contains(b))list.Add(b);}
        bones=list.ToArray();previous=new Quaternion[bones.Length];headScale=headBone.localScale;
        headBind=Quaternion.Inverse(bodyRoot.rotation)*headBone.rotation;
        if(bindSkin){var mesh=bindSkin.sharedMesh;var skinBones=bindSkin.bones;for(int i=0;i<skinBones.Length;i++)if(skinBones[i]==headBone){headBind=Quaternion.Inverse(bodyRoot.rotation)*bindSkin.transform.rotation*mesh.bindposes[i].inverse.rotation;break;}}
        if(headRenderers!=null)foreach(var r in headRenderers)if(r)r.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
        cam.nearClipPlane=nearClip;InitializeWorldGaze();ResetLook(bodyRoot.eulerAngles.y,0);
    }
    public void ResetLook(float yaw,float pitch){Yaw=yaw;Pitch=Mathf.Clamp(pitch,minPitch,maxPitch);worldYaw=yaw;worldTurning=false;}
    public void Look(Vector2 delta){Yaw+=delta.x;Pitch=Mathf.Clamp(Pitch-delta.y,minPitch,maxPitch);}
    void Update(){RestorePose();RestoreWorldGaze();}
    void RestorePose(){if(!applied)return;for(int i=0;i<bones.Length;i++)if(bones[i])bones[i].localRotation=previous[i];if(headBone)headBone.localScale=headScale;applied=false;}
    void OnDisable(){RestorePose();RestoreWorldGaze();if(worldAnimator)worldAnimator.transform.localRotation=worldRootRest;if(weapon)weapon.SetHandsFree(true);}
    void LateUpdate()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if(readLegacyMouse)Look(new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"))*sensitivity);
#endif
        if(!headBone)return;
        for(int i=0;i<bones.Length;i++)previous[i]=bones[i].localRotation;applied=true;
        bodyRoot.rotation=Quaternion.Euler(0,Yaw,0);
        Vector4 values=new Vector4(spine,chest,upperChest,neck);
        // The reference grip and elbow hints share the camera frame. During a
        // downward look the shoulder frame must reach the same pitch before IK;
        // otherwise the targets orbit the eyes while the shoulders lag behind.
        // Blend only below the horizon: preserve the approved level/upward pose.
        float downFollow=stabilizeDownwardWeaponPose && shooting && shooting.Equipped && !shooting.RifleEquipped
            ? Mathf.SmoothStep(0f,1f,Mathf.Clamp01(Pitch/20f)) : 0f;
        float shoulderPitch=spine+chest+upperChest;
        if(shoulderPitch>0.0001f)
        {
            float scale=Mathf.Lerp(1f,1f/shoulderPitch,downFollow);
            values.x*=scale;values.y*=scale;values.z*=scale;
            values.w*=1f-downFollow;
        }
        for(int i=0;i<chain.Length-1;i++)if(chain[i])chain[i].rotation=Quaternion.AngleAxis(Pitch*values[i],bodyRoot.right)*chain[i].rotation;
        headBone.rotation=Quaternion.Euler(Pitch,Yaw,0)*headBind;
        if(hideHeadByScale)headBone.localScale=Vector3.zero;
        // Reuse the existing shot spring only on the view; never bake recoil into
        // mouse look, spine pose, or the authored animation clips.
        Vector3 viewKick=shooting && shooting.Equipped ? shooting.CameraShotKick : Vector3.zero;
        Quaternion viewRotation=Quaternion.Euler(Pitch,Yaw,0);
        // Rotate the eye offset with the coupled head/shoulder frame as well.
        // A yaw-only offset adds a growing translation error as pitch increases.
        Quaternion eyeFrame=Quaternion.Slerp(Quaternion.Euler(0,Yaw,0),viewRotation,downFollow);
        cam.transform.SetPositionAndRotation(headBone.position+eyeFrame*eyeOffset,viewRotation*Quaternion.Euler(viewKick));
        PresentWorldGaze();
        if(shooting){
            bool equipped=shooting.isActiveAndEnabled && shooting.Equipped;
            if(equipped&&!wasEquipped)drawRemaining=drawSeconds;
            if(!equipped)drawRemaining=0;
            drawRemaining=Mathf.Max(0,drawRemaining-Time.deltaTime);wasEquipped=equipped;
            animator.SetBool(DrawingId,Drawing);
            if(weapon){weapon.aiming=shooting.Aiming;weapon.externalAimWeight=shooting.AimWeight;weapon.SetHandsFree(!equipped||Drawing||shooting.Reloading);}
        }
    }

    void InitializeWorldGaze()
    {
        if(!worldAnimator || worldAnimator==animator || !worldAnimator.isHuman)return;
        worldHead=worldAnimator.GetBoneTransform(HumanBodyBones.Head);
        if(!worldHead)return;
        worldRootRest=worldAnimator.transform.localRotation;
        worldChain=new[]{worldAnimator.GetBoneTransform(HumanBodyBones.Spine),worldAnimator.GetBoneTransform(HumanBodyBones.Chest),worldAnimator.GetBoneTransform(HumanBodyBones.UpperChest),worldAnimator.GetBoneTransform(HumanBodyBones.Neck),worldHead};
        worldPrevious=new Quaternion[worldChain.Length];
        worldHeadBind=Quaternion.Inverse(worldAnimator.transform.rotation)*worldHead.rotation;
        // Use bind-pose anatomical forward, not the current animation's head tilt.
        foreach(var skin in worldAnimator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var skinBones=skin.bones;var binds=skin.sharedMesh.bindposes;
            for(int i=0;i<skinBones.Length;i++)if(skinBones[i]==worldHead){worldHeadBind=Quaternion.Inverse(worldAnimator.transform.rotation)*skin.transform.rotation*binds[i].inverse.rotation;return;}
        }
    }
    void RestoreWorldGaze()
    {
        if(!worldApplied)return;
        for(int i=0;i<worldChain.Length;i++)if(worldChain[i])worldChain[i].localRotation=worldPrevious[i];
        worldApplied=false;
    }
    void PresentWorldGaze()
    {
        if(!worldHead || !worldAnimator.isActiveAndEnabled)return;
        for(int i=0;i<worldChain.Length;i++)if(worldChain[i])worldPrevious[i]=worldChain[i].localRotation;
        worldApplied=true;
        float delta=Mathf.DeltaAngle(worldYaw,Yaw);
        bool moving=Mathf.Abs(animator.GetFloat("X_velocity"))+Mathf.Abs(animator.GetFloat("Y_velocity"))>.15f;
        if(Mathf.Abs(delta)>headTurnLimit || moving)worldTurning=true;
        if(worldTurning)worldYaw=Mathf.MoveTowardsAngle(worldYaw,Yaw,bodyTurnSpeed*Time.deltaTime);
        // Even an instantaneous mouse turn cannot twist the neck beyond its limit.
        delta=Mathf.DeltaAngle(worldYaw,Yaw);
        worldYaw=Yaw-Mathf.Clamp(delta,-headTurnLimit,headTurnLimit);
        delta=Mathf.DeltaAngle(worldYaw,Yaw);
        if(!moving && Mathf.Abs(delta)<2f)worldTurning=false;
        worldAnimator.transform.rotation=Quaternion.Euler(0,worldYaw,0);
        Vector4 yawShares=new Vector4(.06f,.09f,.10f,.15f);
        Vector4 pitchShares=new Vector4(spine,chest,upperChest,neck);
        for(int i=0;i<4;i++)if(worldChain[i])
        {
            worldChain[i].rotation=Quaternion.AngleAxis(delta*yawShares[i],Vector3.up)*worldChain[i].rotation;
            worldChain[i].rotation=Quaternion.AngleAxis(Pitch*pitchShares[i],Quaternion.Euler(0,Yaw,0)*Vector3.right)*worldChain[i].rotation;
        }
        worldHead.rotation=Quaternion.Euler(Pitch,Yaw,0)*worldHeadBind;
        WorldGazeError=Vector3.Angle(worldHead.rotation*Quaternion.Inverse(worldHeadBind)*Vector3.forward,Quaternion.Euler(Pitch,Yaw,0)*Vector3.forward);
    }
}


