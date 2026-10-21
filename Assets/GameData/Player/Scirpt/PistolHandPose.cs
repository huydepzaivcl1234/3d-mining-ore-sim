using System;
using UnityEngine;

namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName = "Chest Defense/Player/Pistol Hand Pose")]
    public sealed class PistolHandPose : ScriptableObject
    {
        [Serializable]
        public struct BonePose
        {
            public HumanBodyBones bone;
            public Vector3 position;
            public Quaternion rotation;
        }

        [HideInInspector] public bool valid;
        [Tooltip("Keep the Animator's arm/finger pose instead of replacing it with the saved static bones.")]
        public bool useAnimationPose;
        [HideInInspector] public Vector3 animationReferencePosition, animationWristEuler;
        [HideInInspector] public Quaternion animationReferenceRotation = Quaternion.identity;
        [Tooltip("Support wrist sampled from the Blender weapon grip instead of Humanoid limb proportions.")]
        public bool useAuthoredSupport;
        public bool useAuthoredFingers;
        public Vector3 supportPosition, supportEuler;
        public Vector3 handsPosition, handsEuler;
        [Range(0f, .15f)] public float viewDistance;
        public Vector3 gunLocalPosition, gunLocalEuler;
        public float gunScale = 1f;
        public Vector3 rightElbowHint, leftElbowHint;
        [HideInInspector] public bool hasAimSettings;
        public Vector3 aimHandsPosition = new Vector3(0, 0, .20f), aimHandsEuler;
        public bool separateAimGunOffsets;
        public Vector3 aimGunLocalPosition, aimGunLocalEuler, aimPointLocalPosition;
        [HideInInspector] public BonePose[] bones = Array.Empty<BonePose>();

        public static bool IsArmBone(HumanBodyBones bone)
        {
            string name = bone.ToString();
            return (name.StartsWith("Left") || name.StartsWith("Right")) &&
                (name.Contains("Shoulder") || name.Contains("UpperArm") || name.Contains("LowerArm") ||
                 name.EndsWith("Hand") || name.Contains("Thumb") || name.Contains("Index") ||
                 name.Contains("Middle") || name.Contains("Ring") || name.Contains("Little"));
        }
    }
}
