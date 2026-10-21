using UnityEngine;

namespace Kinemation.SightsAligner
{
    [CreateAssetMenu(fileName = "NewAimData", menuName = "TargetAimData")]
    public class TargetAimData : ScriptableObject
    {
        public Vector3 aimLoc;
        public Quaternion aimRot;
        public AnimationClip staticPose;
        public string stateName;
    }
}