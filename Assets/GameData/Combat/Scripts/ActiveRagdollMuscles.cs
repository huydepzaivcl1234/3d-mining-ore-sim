using UnityEngine;
namespace MiningSimulator.Ores
{
    // Physical PD motors resist pose error briefly; gravity, impacts and inherited velocity remain active.
    [DisallowMultipleComponent]
    public sealed class ActiveRagdollMuscles:MonoBehaviour
    {
        Rigidbody[] bodies,parents;
        Quaternion[] targets;
        float elapsed;
        public bool Driving=>enabled&&bodies!=null;
        public void Begin(Rigidbody[] source)
        {
            bodies=source;
            if(parents==null||parents.Length!=source.Length){parents=new Rigidbody[source.Length];targets=new Quaternion[source.Length];}
            for(int i=0;i<source.Length;i++){
                var joint=source[i]?source[i].GetComponent<Joint>():null;parents[i]=joint?joint.connectedBody:null;
                targets[i]=parents[i]?Quaternion.Inverse(parents[i].rotation)*source[i].rotation:Quaternion.identity;
            }
            elapsed=0;enabled=RagdollSettings.Current;
        }
        public void Stop(){enabled=false;bodies=null;}
        void FixedUpdate()
        {
            var data=RagdollSettings.Current;
            if(!data||bodies==null){Stop();return;}
            elapsed+=Time.fixedDeltaTime;float strength=Mathf.Clamp01(1-elapsed/Mathf.Max(.01f,data.muscleSeconds));
            if(strength<=0){Stop();return;}
            for(int i=0;i<bodies.Length;i++){
                var body=bodies[i];var parent=parents[i];if(!body||!parent||body.isKinematic)continue;
                var delta=(parent.rotation*targets[i])*Quaternion.Inverse(body.rotation);
                delta.ToAngleAxis(out float angle,out Vector3 axis);if(angle>180)angle-=360;
                if(!float.IsFinite(axis.x)||!float.IsFinite(axis.y)||!float.IsFinite(axis.z))continue;
                var torque=axis*(angle*Mathf.Deg2Rad*data.spring)-(body.angularVelocity-parent.angularVelocity)*data.damping;
                torque=Vector3.ClampMagnitude(torque*strength,data.maximumAngularAcceleration);
                body.AddTorque(torque,ForceMode.Acceleration);if(!parent.isKinematic)parent.AddTorque(-torque,ForceMode.Acceleration);
            }
        }
    }
}
