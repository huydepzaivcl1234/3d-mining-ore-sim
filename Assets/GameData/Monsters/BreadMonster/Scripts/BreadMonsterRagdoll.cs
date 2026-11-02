using UnityEngine;
namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent,DefaultExecutionOrder(300)]
    public sealed class BreadMonsterRagdoll : MonoBehaviour
    {
        [SerializeField] private Rigidbody[] bones=System.Array.Empty<Rigidbody>();
        [SerializeField] private Collider[] shapes=System.Array.Empty<Collider>();
        private Vector3 previous,velocity;
        public bool Active{get;private set;}
        public void Configure(Rigidbody[] bodies,Collider[] colliders){bones=bodies;shapes=colliders;}
        private void OnEnable(){previous=transform.position;velocity=Vector3.zero;}
        private void LateUpdate(){if(!Active&&Time.deltaTime>0){velocity=(transform.position-previous)/Time.deltaTime;previous=transform.position;}}
        public void Activate()
        {
            if(Active||bones.Length==0)return;
            Active=true;
            var status=GetComponent<GameData.Player.Script.SurvivorEnemyStatus>();if(status){var position=transform.position;status.enabled=false;transform.position=position;}
            foreach(var animator in GetComponentsInChildren<Animator>())animator.enabled=false;
            foreach(var shape in shapes)if(shape)shape.enabled=true;
            for(int i=0;i<shapes.Length;i++)for(int j=i+1;j<shapes.Length;j++)if(shapes[i]&&shapes[j])Physics.IgnoreCollision(shapes[i],shapes[j]);
            foreach(var bone in bones)if(bone){var joint=bone.GetComponent<CharacterJoint>();if(joint&&joint.connectedBody)joint.connectedAnchor=joint.connectedBody.transform.InverseTransformPoint(bone.transform.TransformPoint(joint.anchor));}
            Physics.SyncTransforms();
            foreach(var bone in bones)if(bone){bone.isKinematic=false;bone.interpolation=RigidbodyInterpolation.Interpolate;bone.useGravity=true;bone.linearVelocity=Vector3.ClampMagnitude(velocity,25);bone.WakeUp();}
            bones[0].AddForce((-transform.forward+Vector3.right*.2f)*.65f,ForceMode.VelocityChange);
        }
    }
}

