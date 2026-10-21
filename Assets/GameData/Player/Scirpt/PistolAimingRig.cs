using Kinemation.SightsAligner;
using UnityEngine;

namespace GameData.Player.Script
{
    /// <summary>Uses imported Kinemation IK through one existing presentation owner.</summary>
    public sealed class PistolAimingRig
    {
        private readonly Transform root, rightTarget, leftTarget, rightHint, leftHint;
        public PistolAimingRig(Transform owner)
        {
            root = new GameObject("Pistol Kinemation IK Targets").transform;
            root.SetParent(owner, false); root.gameObject.hideFlags = HideFlags.DontSave;
            rightTarget = Child("Right wrist"); leftTarget = Child("Left wrist");
            rightHint = Child("Right elbow"); leftHint = Child("Left elbow");
        }
        private Transform Child(string name)
        { var value = new GameObject(name).transform; value.SetParent(root, false); return value; }

        public void Solve(bool right, Transform upper, Transform lower, Transform hand,
            Vector3 position, Quaternion rotation, Vector3 elbow, float weight)
        {
            float a = Vector3.Distance(upper.position, lower.position), b = Vector3.Distance(lower.position, hand.position);
            if (a < .0001f || b < .0001f || (position-upper.position).sqrMagnitude < .000001f) return;
            Vector3 direction = position - upper.position;
            position = upper.position + direction.normalized * Mathf.Clamp(direction.magnitude, Mathf.Abs(a-b)+.0001f, a+b-.001f);
            var target = right ? rightTarget : leftTarget; var hint = right ? rightHint : leftHint;
            target.SetPositionAndRotation(position, rotation); hint.position = elbow;
            AnimToolkitLib.SolveTwoBoneIK(upper, lower, hand, target, hint, Mathf.Clamp01(weight), Mathf.Clamp01(weight), 1f);
        }
        public void Dispose()
        {
            if (!root) return;
            if (Application.isPlaying) Object.Destroy(root.gameObject);
            else Object.DestroyImmediate(root.gameObject);
        }
    }
}
