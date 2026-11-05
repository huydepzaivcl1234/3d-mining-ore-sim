using UnityEngine;
using MiningSimulator.Ores;

namespace GameData.Player.Script
{
    [DisallowMultipleComponent,RequireComponent(typeof(CharacterHealth))]
    public sealed class PlayerRagdoll : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody pelvis;
        [SerializeField] private Rigidbody[] bones=System.Array.Empty<Rigidbody>();
        [SerializeField] private Collider[] boneColliders=System.Array.Empty<Collider>();
        [Tooltip("A small backwards nudge lets a standing corpse lose its balance naturally.")]
        [SerializeField,Min(0)] private float collapseSpeed=.65f;
        private Rigidbody rootBody;
        private Collider[] rootColliders;
        private bool[] rootColliderEnabled;
        private Vector3[] localPositions;
        private Quaternion[] localRotations;
        private bool rootKinematic,rootGravity,animatorEnabled;
        public bool IsRagdoll{get;private set;}
        public Vector3 FollowPosition=>pelvis?pelvis.position:transform.position;
        public Rigidbody Pelvis=>pelvis;
        public System.Collections.Generic.IReadOnlyList<Rigidbody> Bodies=>bones;

        public void Configure(Animator source,Rigidbody[] bodies,Collider[] colliders)
        {animator=source;bones=bodies;boneColliders=colliders;pelvis=bodies.Length>0?bodies[0]:null;}
        private void Awake()
        {
            rootBody=GetComponent<Rigidbody>();if(!animator)animator=GetComponent<Animator>();
            rootColliders=GetComponents<Collider>();rootColliderEnabled=new bool[rootColliders.Length];
            for(int i=0;i<rootColliders.Length;i++)rootColliderEnabled[i]=rootColliders[i].enabled;
            if(rootBody){rootKinematic=rootBody.isKinematic;rootGravity=rootBody.useGravity;}
            animatorEnabled=animator&&animator.enabled;
            localPositions=new Vector3[bones.Length];localRotations=new Quaternion[bones.Length];
            for(int i=0;i<bones.Length;i++)if(bones[i]){localPositions[i]=bones[i].transform.localPosition;localRotations[i]=bones[i].transform.localRotation;bones[i].isKinematic=true;}
            foreach(var collider in boneColliders)if(collider)collider.enabled=false;
            // Self collisions in an arbitrary animated death pose can inject explosive separation forces.
            for(int i=0;i<boneColliders.Length;i++)for(int j=i+1;j<boneColliders.Length;j++)if(boneColliders[i]&&boneColliders[j])Physics.IgnoreCollision(boneColliders[i],boneColliders[j],true);
        }
        public void Activate()
        {
            if(IsRagdoll||bones.Length==0||!pelvis)return;
            Vector3 velocity=rootBody?rootBody.linearVelocity:Vector3.zero;
            Vector3 angular=rootBody?rootBody.angularVelocity:Vector3.zero;
            if(animator)animator.enabled=false;
            foreach(var collider in rootColliders)if(collider)collider.enabled=false;
            if(rootBody){rootBody.isKinematic=true;rootBody.useGravity=false;}
            IsRagdoll=true;
            foreach(var collider in boneColliders)if(collider)collider.enabled=true;
            for(int i=0;i<boneColliders.Length;i++)for(int j=i+1;j<boneColliders.Length;j++)if(boneColliders[i]&&boneColliders[j])Physics.IgnoreCollision(boneColliders[i],boneColliders[j],true);
            foreach(var bone in bones)if(bone){var joint=bone.GetComponent<CharacterJoint>();if(joint&&joint.connectedBody)joint.connectedAnchor=joint.connectedBody.transform.InverseTransformPoint(bone.transform.TransformPoint(joint.anchor));}
            Physics.SyncTransforms();
            foreach(var bone in bones)if(bone){bone.isKinematic=false;bone.useGravity=true;bone.linearVelocity=velocity;bone.angularVelocity=angular;bone.WakeUp();}
            var muscles=GetComponent<ActiveRagdollMuscles>();if(!muscles)muscles=gameObject.AddComponent<ActiveRagdollMuscles>();muscles.Begin(bones);
            if(velocity.sqrMagnitude<1)pelvis.AddForce(-transform.forward*collapseSpeed,ForceMode.VelocityChange);
        }
        public void AddVelocity(Vector3 velocity)
        {
            if(!IsRagdoll)return;
            foreach(var bone in bones)if(bone&&!bone.isKinematic){bone.linearVelocity+=velocity;bone.WakeUp();}
        }
        public void RestoreAnimatedPose()
        {
            if(!IsRagdoll)return;
            GetComponent<ActiveRagdollMuscles>()?.Stop();
            foreach(var collider in boneColliders)if(collider)collider.enabled=false;
            foreach(var bone in bones)if(bone){if(!bone.isKinematic){bone.linearVelocity=Vector3.zero;bone.angularVelocity=Vector3.zero;}bone.isKinematic=true;}
            for(int i=0;i<bones.Length;i++)if(bones[i]){bones[i].transform.localPosition=localPositions[i];bones[i].transform.localRotation=localRotations[i];}
            if(rootBody){rootBody.isKinematic=rootKinematic;rootBody.useGravity=rootGravity;}
            for(int i=0;i<rootColliders.Length;i++)if(rootColliders[i])rootColliders[i].enabled=rootColliderEnabled[i];
            IsRagdoll=false;
            if(animator){animator.enabled=animatorEnabled;if(animatorEnabled){animator.Rebind();animator.Update(0);}}
            Physics.SyncTransforms();
        }
    }
}
