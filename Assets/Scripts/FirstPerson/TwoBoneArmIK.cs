using UnityEngine;

/// <summary>
/// Analytic two-bone arm IK for a Humanoid body, solved in LateUpdate so it uses the camera and
/// WeaponAnchor of the SAME frame (no one-frame lag, no package needed).
/// Add one component per arm. Weight 0 = animation drives the arm, 1 = hand sits on the target.
/// </summary>
[DefaultExecutionOrder(10020)]
public class TwoBoneArmIK : MonoBehaviour
{
    public Animator animator;               // Humanoid Animator on the body
    public bool rightArm = true;
    public Transform target;                // hand target (position + rotation), child of WeaponAnchor
    public Transform hint;                  // elbow hint, child of WeaponAnchor (optional)
    [Range(0f, 1f)] public float weight = 1f;
    [Range(0f, 1f)] public float rotationWeight = 1f;
    [Tooltip("Euler offset between the target's rotation and the hand bone rotation (fix bone axis mismatch).")]
    public Vector3 handRotationOffset;
    [Tooltip("Use a body-relative downward pole when retargeting disconnected FPS arms onto a full body.")]
    public bool keepElbowDown;

    Transform upper, lower, end;
    public int LastSolvedFrame {get;private set;}=-1;

    void Awake()
    {
        if (!animator) animator = GetComponentInParent<Animator>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if(!animator||!animator.isHuman){enabled=false;return;}
        upper = animator.GetBoneTransform(rightArm ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm);
        lower = animator.GetBoneTransform(rightArm ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm);
        end = animator.GetBoneTransform(rightArm ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
    }

    void LateUpdate()
    {
        if (weight <= 0.0001f || !target || !upper || !lower || !end) return;
        Solve();
        LastSolvedFrame=Time.frameCount;
    }

    void Solve()
    {
        Vector3 a = upper.position, b = lower.position, c = end.position;
        float lenAB = Vector3.Distance(a, b), lenBC = Vector3.Distance(b, c);

        Vector3 toT = target.position - a;
        float dist = Mathf.Clamp(toT.magnitude, Mathf.Abs(lenAB - lenBC) + 0.001f, (lenAB + lenBC) * 0.999f);
        Vector3 dir = toT.sqrMagnitude > 1e-8f ? toT.normalized : (c - a).normalized;

        // elbow position on the circle around the shoulder-target axis, towards the hint
        float x = (lenAB * lenAB - lenBC * lenBC + dist * dist) / (2f * dist);
        float h = Mathf.Sqrt(Mathf.Max(lenAB * lenAB - x * x, 0f));
        Vector3 pole = hint ? hint.position - a : b - a;
        if(keepElbowDown)
            pole = animator.transform.TransformDirection(new Vector3(rightArm ? .35f : -.35f,-.45f,.08f));
        Vector3 side = Vector3.ProjectOnPlane(pole, dir);
        if (side.sqrMagnitude < 1e-8f) side = Vector3.ProjectOnPlane(b - a, dir);
        if (side.sqrMagnitude < 1e-8f) side = Vector3.ProjectOnPlane(Vector3.up,dir);
        if (side.sqrMagnitude < 1e-8f) side = Vector3.ProjectOnPlane(Vector3.right,dir);
        side.Normalize();
        Vector3 elbow = a + dir * x + side * h;
        Vector3 endPos = a + dir * dist;

        Quaternion qU = Quaternion.FromToRotation(b - a, elbow - a);
        upper.rotation = Quaternion.Slerp(upper.rotation, qU * upper.rotation, weight);

        b = lower.position; c = end.position;
        Quaternion qL = Quaternion.FromToRotation(c - b, endPos - b);
        lower.rotation = Quaternion.Slerp(lower.rotation, qL * lower.rotation, weight);

        Quaternion goal = target.rotation * Quaternion.Euler(handRotationOffset);
        end.rotation = Quaternion.Slerp(end.rotation, goal, weight * rotationWeight);
    }

    // Authoring helpers: run in Play mode while the animation holds the pose you want (weight = 0),
    // then Copy Component on the Transform and Paste Component Values after leaving Play mode.
    [ContextMenu("Snap target to current hand")]
    public void SnapTarget()
    {
        if (target && end)
            target.SetPositionAndRotation(end.position, end.rotation * Quaternion.Inverse(Quaternion.Euler(handRotationOffset)));
    }

    [ContextMenu("Snap hint to current elbow")]
    public void SnapHint()
    {
        if (hint && lower) hint.position = lower.position;
    }
    public Vector3 ReachExcess(){
        if(!upper||!lower||!end||!target)return Vector3.zero;
        float reach=Vector3.Distance(upper.position,lower.position)+Vector3.Distance(lower.position,end.position)-.004f;
        Vector3 delta=target.position-upper.position;
        return delta.magnitude>reach?delta.normalized*(delta.magnitude-reach):Vector3.zero;
    }
}
