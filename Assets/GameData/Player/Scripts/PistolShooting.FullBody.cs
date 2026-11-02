using System.Collections.Generic;
using UnityEngine;

namespace GameData.Player.Script
{
    public sealed partial class PistolShooting
    {
        [Header("Shared full-body pistol pose")]
        [SerializeField] private bool unifiedPistolBody;
        [SerializeField] private SkinnedMeshRenderer sharedLocalBody;
        [SerializeField] private SkinnedMeshRenderer sharedWorldBody;
        [SerializeField] private Mesh sharedBodyMesh;
        private SharedBodyPose sharedBody;
        public bool SharedBodyActive => sharedBody != null && isActiveAndEnabled && Equipped && weaponMode == 1;
        public float SharedLeftHandError => sharedBody != null ? sharedBody.LeftError : 0f;
        public float SharedRightHandError => sharedBody != null ? sharedBody.RightError : 0f;
        public float HeadGazeError => sharedBody != null ? sharedBody.GazeError : 0f;

        // The hidden Blender animation rig supplies targets only. All visible body
        // renderers use the original body's solved bones; no second rendered arms.
        private sealed class SharedBodyPose
        {
            private readonly PistolShooting owner;
            private readonly Arm left, right;
            private readonly Transform[] spine;
            private readonly Transform head;
            private readonly Quaternion headRestInBody;
            private readonly List<Joint> joints = new();
            private readonly List<PartPair> parts = new();
            private Renderer[] hiddenRenderers;
            private bool[] oldRendererFlags;
            private readonly Mesh oldMesh;
            private readonly float oldFov, oldNear;
            private readonly Vector3 oldCameraPosition;
            private readonly SkinnedMeshRenderer shadowBody;
            private bool active, oldWeaponCamera;
            private Transform sourceGun, visibleGun;
            private Vector3 gunPosition;
            private Quaternion gunRotation;
            private Transform[] gunNodes;
            private int[] gunLayers;
            public Transform Barrel { get; private set; }
            public float LeftError { get; private set; }
            public float RightError { get; private set; }
            public float GazeError { get; private set; }

            private sealed class Joint
            {
                public Transform bone, source;
                public Quaternion previous;
                public bool changed;
            }
            private sealed class Arm
            {
                public Transform upper, elbow, hand, targetUpper, targetElbow, target;
            }
            private struct PartPair { public Transform source, destination; }

            public SharedBodyPose(PistolShooting owner)
            {
                this.owner=owner;
                var body=owner.bodyAnimator; var source=owner.playerAnimator;
                left=MakeArm(body,source,true); right=MakeArm(body,source,false);
                spine=new[]{body.GetBoneTransform(HumanBodyBones.Spine),body.GetBoneTransform(HumanBodyBones.Chest),body.GetBoneTransform(HumanBodyBones.UpperChest)};
                head=body.GetBoneTransform(HumanBodyBones.Head);
                // Bone axes are not anatomical forward/up (Mixamo uses bone Y).
                // Calibrate from the original skin bind, never the current idle pose.
                headRestInBody=Quaternion.identity;
                if(head && owner.sharedWorldBody){
                    var skin=owner.sharedWorldBody;var binds=skin.sharedMesh.bindposes;var bones=skin.bones;
                    for(int i=0;i<bones.Length;i++)if(bones[i]==head){
                        headRestInBody=Quaternion.Inverse(owner.transform.rotation)*skin.transform.rotation*binds[i].inverse.rotation;break;
                    }
                }
                foreach(var bone in spine)Remember(bone,null);
                Remember(head,null);
                Remember(left.upper,null);Remember(left.elbow,null);Remember(left.hand,null);
                Remember(right.upper,null);Remember(right.elbow,null);Remember(right.hand,null);
                for(int i=(int)HumanBodyBones.LeftThumbProximal;i<=(int)HumanBodyBones.RightLittleDistal;i++)
                    Remember(body.GetBoneTransform((HumanBodyBones)i),source.GetBoneTransform((HumanBodyBones)i));
                oldMesh=owner.sharedLocalBody.sharedMesh;
                oldFov=owner.viewCamera.fieldOfView;oldNear=owner.viewCamera.nearClipPlane;
                oldCameraPosition=owner.viewCamera.transform.localPosition;
                if(owner.sharedWorldBody){
                    var original=owner.sharedWorldBody;
                    var go=new GameObject("Shared Body Shadow");go.layer=owner.sharedLocalBody.gameObject.layer;
                    go.transform.SetParent(original.transform.parent,false);
                    go.transform.SetLocalPositionAndRotation(original.transform.localPosition,original.transform.localRotation);go.transform.localScale=original.transform.localScale;
                    shadowBody=go.AddComponent<SkinnedMeshRenderer>();shadowBody.sharedMesh=original.sharedMesh;shadowBody.sharedMaterials=original.sharedMaterials;
                    shadowBody.bones=original.bones;shadowBody.rootBone=original.rootBone;shadowBody.localBounds=original.localBounds;shadowBody.updateWhenOffscreen=true;
                    shadowBody.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;shadowBody.enabled=false;
                }
            }

            private void Remember(Transform bone,Transform source)
            {
                if(!bone)return;
                foreach(var j in joints)if(j.bone==bone)return;
                joints.Add(new Joint{bone=bone,source=source});
            }
            private static Arm MakeArm(Animator body,Animator source,bool left)
            {
                var upper=left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm;
                var elbow=left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm;
                var hand=left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand;
                return new Arm{upper=body.GetBoneTransform(upper),elbow=body.GetBoneTransform(elbow),hand=body.GetBoneTransform(hand),targetUpper=source.GetBoneTransform(upper),targetElbow=source.GetBoneTransform(elbow),target=source.GetBoneTransform(hand)};
            }

            public void BeginFrame()
            {
                // Restore last frame's presentation before Animator evaluation.
                // This also prevents accumulation while time is paused or culled.
                foreach(var joint in joints)if(joint.changed&&joint.bone){joint.bone.localRotation=joint.previous;joint.changed=false;}
            }
            public void Present()
            {
                if(!owner.SharedBodyActive){
                    Restore();
                    if(owner.alive && owner.isActiveAndEnabled){RememberCurrentHead();ApplyHeadGaze();}
                    return;
                }
                if(!active){
                    active=true;oldWeaponCamera=owner.weaponCamera.enabled;
                    hiddenRenderers=owner.viewmodelRoot.GetComponentsInChildren<Renderer>(true);
                    oldRendererFlags=new bool[hiddenRenderers.Length];
                    for(int i=0;i<hiddenRenderers.Length;i++){oldRendererFlags[i]=hiddenRenderers[i].enabled;hiddenRenderers[i].enabled=false;}
                    owner.sharedLocalBody.sharedMesh=owner.sharedBodyMesh;
                    if(shadowBody)shadowBody.enabled=true;
                }
                owner.weaponCamera.enabled=false;
                owner.viewCamera.fieldOfView=owner.weaponCamera.fieldOfView;
                owner.viewCamera.nearClipPlane=.03f;
                foreach(var joint in joints){joint.previous=joint.bone.localRotation;joint.changed=true;}
                Vector3 look=owner.transform.InverseTransformDirection(owner.viewCamera.transform.forward);
                float pitch=Mathf.Atan2(-look.y,new Vector2(look.x,look.z).magnitude)*Mathf.Rad2Deg;
                int count=0;foreach(var bone in spine)if(bone)count++;
                foreach(var bone in spine)if(bone)bone.rotation=Quaternion.AngleAxis(pitch*.7f/Mathf.Max(1,count),owner.transform.right)*bone.rotation;
                ApplyHeadGaze();
                // The eye follows the solved body, not an unrelated camera-space
                // torso. This keeps the collar behind the eye when looking down.
                if(head)owner.viewCamera.transform.position=head.position+owner.transform.up*.04f+owner.transform.forward*.14f;
                // Project both authored grips together into the body's reachable
                // space during jumps/landings. Moving them as one rigid group
                // preserves the support-hand/magazine contact instead of letting
                // an independently clamped wrist detach from the gun.
                for(int i=0;i<6;i++){ConstrainTargetGroup(left);ConstrainTargetGroup(right);}
                Solve(left);Solve(right);
                foreach(var joint in joints)if(joint.source)joint.bone.localRotation=joint.source.localRotation;
                LeftError=Vector3.Distance(left.hand.position,left.target.position);
                RightError=Vector3.Distance(right.hand.position,right.target.position);
                RefreshGun();
                if(visibleGun&&sourceGun){
                    visibleGun.SetPositionAndRotation(right.hand.TransformPoint(right.target.InverseTransformPoint(sourceGun.position)),right.hand.rotation*Quaternion.Inverse(right.target.rotation)*sourceGun.rotation);
                    foreach(var part in parts){part.destination.SetLocalPositionAndRotation(part.source.localPosition,part.source.localRotation);part.destination.localScale=part.source.localScale;part.destination.gameObject.SetActive(part.source.gameObject.activeSelf);}
                }
            }

            private void RememberCurrentHead()
            {
                foreach(var joint in joints)if(joint.bone==head){joint.previous=head.localRotation;joint.changed=true;break;}
            }
            private void ApplyHeadGaze()
            {
                if(!head || !owner.viewCamera)return;
                // Match the centre ray, not an authored chin-down angle. Projecting
                // character-up removes head roll while retaining camera pitch/yaw.
                Vector3 forward=owner.viewCamera.transform.forward;
                Vector3 up=Vector3.ProjectOnPlane(owner.transform.up,forward);
                if(up.sqrMagnitude<.0001f)up=owner.viewCamera.transform.up;
                head.rotation=Quaternion.LookRotation(forward,up)*headRestInBody;
                GazeError=Vector3.Angle(head.rotation*Quaternion.Inverse(headRestInBody)*Vector3.forward,forward);
            }

            // Two-bone position IK with the authored elbow as the bend-plane hint.
            // It never moves the pelvis/root or writes Rigidbody movement.
            private static void Solve(Arm arm)
            {
                Vector3 start=arm.upper.position;
                float upperLength=Vector3.Distance(start,arm.elbow.position),lowerLength=Vector3.Distance(arm.elbow.position,arm.hand.position);
                Vector3 delta=arm.target.position-start;
                float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(upperLength-lowerLength)+.0001f,upperLength+lowerLength-.0001f);
                Vector3 direction=delta.sqrMagnitude>.000001f?delta.normalized:arm.upper.forward;
                Vector3 pole=Vector3.ProjectOnPlane(arm.targetElbow.position-start,direction);
                if(pole.sqrMagnitude<.000001f)pole=Vector3.ProjectOnPlane(arm.elbow.position-start,direction);
                if(pole.sqrMagnitude<.000001f)pole=Vector3.Cross(direction,Vector3.up);
                if(pole.sqrMagnitude<.000001f)pole=Vector3.Cross(direction,Vector3.right);
                float along=(upperLength*upperLength+distance*distance-lowerLength*lowerLength)/(2f*distance);
                float bend=Mathf.Sqrt(Mathf.Max(0,upperLength*upperLength-along*along));
                Vector3 elbow=start+direction*along+pole.normalized*bend;
                arm.upper.rotation=Quaternion.FromToRotation(arm.elbow.position-start,elbow-start)*arm.upper.rotation;
                arm.elbow.rotation=Quaternion.FromToRotation(arm.hand.position-arm.elbow.position,start+direction*distance-arm.elbow.position)*arm.elbow.rotation;
                arm.hand.rotation=arm.target.rotation;
            }

            private void ConstrainTargetGroup(Arm arm)
            {
                float length=Vector3.Distance(arm.upper.position,arm.elbow.position)+Vector3.Distance(arm.elbow.position,arm.hand.position)-.002f;
                Vector3 delta=arm.target.position-arm.upper.position;
                if(delta.magnitude>length)owner.viewmodelRoot.position-=delta.normalized*(delta.magnitude-length);
            }

            private void RefreshGun()
            {
                if(sourceGun==owner.gun&&visibleGun==owner.worldGun)return;
                RestoreGun();sourceGun=owner.gun;visibleGun=owner.worldGun;
                if(!sourceGun||!visibleGun)return;
                gunPosition=visibleGun.localPosition;gunRotation=visibleGun.localRotation;
                gunNodes=visibleGun.GetComponentsInChildren<Transform>(true);gunLayers=new int[gunNodes.Length];
                for(int i=0;i<gunNodes.Length;i++){gunLayers[i]=gunNodes[i].gameObject.layer;gunNodes[i].gameObject.layer=0;}
                foreach(var source in sourceGun.GetComponentsInChildren<Transform>(true)){
                    if(source==sourceGun||source.name.Contains("Flash"))continue;
                    string path=source.name;var parent=source.parent;while(parent&&parent!=sourceGun){path=parent.name+"/"+path;parent=parent.parent;}
                    var destination=visibleGun.Find(path);if(destination)parts.Add(new PartPair{source=source,destination=destination});
                }
                Barrel=owner.muzzle?visibleGun.Find(owner.muzzle.name):null;
            }
            private void RestoreGun()
            {
                if(visibleGun)visibleGun.SetLocalPositionAndRotation(gunPosition,gunRotation);
                if(gunNodes!=null)for(int i=0;i<gunNodes.Length;i++)if(gunNodes[i])gunNodes[i].gameObject.layer=gunLayers[i];
                parts.Clear();gunNodes=null;sourceGun=visibleGun=null;Barrel=null;
            }
            public void Restore()
            {
                BeginFrame();if(!active)return;active=false;
                if(hiddenRenderers!=null)for(int i=0;i<hiddenRenderers.Length;i++)if(hiddenRenderers[i])hiddenRenderers[i].enabled=oldRendererFlags[i];
                owner.sharedLocalBody.sharedMesh=oldMesh;owner.weaponCamera.enabled=oldWeaponCamera;
                owner.viewCamera.fieldOfView=oldFov;owner.viewCamera.nearClipPlane=oldNear;
                owner.viewCamera.transform.localPosition=oldCameraPosition;
                RestoreGun();LeftError=RightError=0;
                if(shadowBody)shadowBody.enabled=false;
            }
            public void Dispose(){Restore();if(shadowBody)Object.Destroy(shadowBody.gameObject);}
        }
    }
}
