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
        private Vector3[] localPositions,localScales;
        private Transform[] boneParents;
        private Transform physicsRoot;
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
            localPositions=new Vector3[bones.Length];localRotations=new Quaternion[bones.Length];localScales=new Vector3[bones.Length];boneParents=new Transform[bones.Length];
            for(int i=0;i<bones.Length;i++)if(bones[i]){boneParents[i]=bones[i].transform.parent;localScales[i]=bones[i].transform.localScale;localPositions[i]=bones[i].transform.localPosition;localRotations[i]=bones[i].transform.localRotation;bones[i].isKinematic=true;}
            foreach(var collider in boneColliders)if(collider)collider.enabled=false;
            // Self collisions in an arbitrary animated death pose can inject explosive separation forces.
            for(int i=0;i<boneColliders.Length;i++)for(int j=i+1;j<boneColliders.Length;j++)if(boneColliders[i]&&boneColliders[j])Physics.IgnoreCollision(boneColliders[i],boneColliders[j],true);
        }
        public void Activate()
        {
            if(IsRagdoll||bones.Length==0||!pelvis)return;
            Vector3 velocity=Vector3.ClampMagnitude(rootBody?rootBody.linearVelocity:Vector3.zero,25);
            Vector3 angular=Vector3.ClampMagnitude(rootBody?rootBody.angularVelocity:Vector3.zero,12);
            if(animator)animator.enabled=false;
            foreach(var collider in rootColliders)if(collider)collider.enabled=false;
            if(rootBody){rootBody.isKinematic=true;rootBody.useGravity=false;}
            IsRagdoll=true;
            // Nested interpolated Rigidbodies can write a parent's motion into a child twice.
            // Keep the same skinned bones, but simulate them as siblings until respawn.
            if(!physicsRoot){physicsRoot=new GameObject("Player physics bones").transform;physicsRoot.SetParent(transform,false);}
            physicsRoot.localScale=new Vector3(1/transform.lossyScale.x,1/transform.lossyScale.y,1/transform.lossyScale.z);
            foreach(var bone in bones)if(bone){bone.interpolation=RigidbodyInterpolation.None;bone.transform.SetParent(physicsRoot,true);}

            // A dead player must not be crushed between dozens of moving enemy colliders.
            // Local exclusions preserve ground collision without changing the global layer matrix.
            int crowdMask=LayerMask.GetMask("Enemy","Ragdoll","LocalPlayerBody");
            foreach(var collider in boneColliders)if(collider){collider.excludeLayers |= crowdMask;collider.enabled=true;}
            for(int i=0;i<boneColliders.Length;i++)for(int j=i+1;j<boneColliders.Length;j++)if(boneColliders[i]&&boneColliders[j])Physics.IgnoreCollision(boneColliders[i],boneColliders[j],true);
            foreach(var bone in bones)if(bone){var joint=bone.GetComponent<CharacterJoint>();if(joint){joint.enablePreprocessing=false;joint.enableProjection=true;joint.projectionDistance=.025f;}if(joint&&joint.connectedBody)joint.connectedAnchor=joint.connectedBody.transform.InverseTransformPoint(bone.transform.TransformPoint(joint.anchor));}
            Physics.SyncTransforms();
            foreach(var bone in bones)if(bone){bone.solverIterations=12;bone.solverVelocityIterations=4;bone.maxDepenetrationVelocity=2;bone.maxAngularVelocity=12;bone.isKinematic=false;bone.useGravity=true;bone.linearVelocity=velocity;bone.angularVelocity=angular;bone.WakeUp();}
            var muscles=GetComponent<ActiveRagdollMuscles>();if(!muscles)muscles=gameObject.AddComponent<ActiveRagdollMuscles>();muscles.Begin(bones);
            if(velocity.sqrMagnitude<1)pelvis.AddForce(-transform.forward*collapseSpeed,ForceMode.VelocityChange);
        }
        public void AddVelocity(Vector3 velocity)
        {
            if(!IsRagdoll)return;
            foreach(var bone in bones)if(bone&&!bone.isKinematic){bone.linearVelocity=Vector3.ClampMagnitude(bone.linearVelocity+velocity,25);bone.WakeUp();}
        }
        public void RestoreBodyPose(string path,Vector3 position,Quaternion rotation,Vector3 velocity,Vector3 angular)
        {
            string name=path.Substring(path.LastIndexOf('/')+1);
            foreach(var body in bones)if(body&&body.name==name){body.transform.SetPositionAndRotation(position,rotation);body.position=position;body.rotation=rotation;body.linearVelocity=Vector3.ClampMagnitude(velocity,25);body.angularVelocity=Vector3.ClampMagnitude(angular,12);break;}
        }
        public void CompletePoseRestore()
        {
            GetComponent<ActiveRagdollMuscles>()?.Stop();
            // Older checkpoints may already contain separated joints. Rejoin them parent-first.
            foreach(var body in bones)if(body){var joint=body.GetComponent<CharacterJoint>();if(joint&&joint.connectedBody){Vector3 delta=joint.connectedBody.transform.TransformPoint(joint.connectedAnchor)-body.transform.TransformPoint(joint.anchor);body.transform.position+=delta;body.position=body.transform.position;}body.WakeUp();}
            Physics.SyncTransforms();
            if(WorldNavigationGrid.Instance&&WorldNavigationGrid.Instance.TryGetGroundPoint(FollowPosition,out var ground)){
                float lowest=float.PositiveInfinity;foreach(var collider in boneColliders)if(collider&&collider.enabled)lowest=Mathf.Min(lowest,collider.bounds.min.y);
                float lift=ground.y+.015f-lowest;
                if(lift>0&&float.IsFinite(lift)){foreach(var body in bones)if(body){var position=body.position+Vector3.up*lift;body.transform.position=position;body.position=position;}Physics.SyncTransforms();}
            }
        }
        public void RestoreAnimatedPose()
        {
            if(!IsRagdoll)return;
            GetComponent<ActiveRagdollMuscles>()?.Stop();
            foreach(var collider in boneColliders)if(collider)collider.enabled=false;
            foreach(var bone in bones)if(bone){if(!bone.isKinematic){bone.linearVelocity=Vector3.zero;bone.angularVelocity=Vector3.zero;}bone.isKinematic=true;}
            for(int i=0;i<bones.Length;i++)if(bones[i]){bones[i].transform.SetParent(boneParents[i],false);bones[i].transform.localPosition=localPositions[i];bones[i].transform.localRotation=localRotations[i];bones[i].transform.localScale=localScales[i];}
            if(rootBody){rootBody.isKinematic=rootKinematic;rootBody.useGravity=rootGravity;}
            for(int i=0;i<rootColliders.Length;i++)if(rootColliders[i])rootColliders[i].enabled=rootColliderEnabled[i];
            IsRagdoll=false;
            if(animator){animator.enabled=animatorEnabled;if(animatorEnabled){animator.Rebind();animator.Update(0);}}
            Physics.SyncTransforms();
        }
    }
}
