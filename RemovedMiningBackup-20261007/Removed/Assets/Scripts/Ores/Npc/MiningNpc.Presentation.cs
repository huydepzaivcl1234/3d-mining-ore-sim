using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MiningNpc
    {
        private void LateUpdate()
        {
            if (animator != null && hasMiningBoolParameter)
            {
                if (isMining && npcData != null)
                {
                    itemSystem ??= FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (stateInfo.shortNameHash == mineStateHash && !animator.IsInTransition(0) &&
                        stateInfo.length > 0.0001f)
                    {
                        // Stretch/compress the clip's own playback speed so exactly one loop
                        // takes the same time as one hit (SecondsPerHit). This keeps Unity's
                        // normal Animator update (blending, any IK, avatar masks) completely
                        // intact - unlike forcing the normalized time directly, which can snap
                        // the rig into a broken pose if the state relies on more than a single
                        // plain clip.
                        float miningSpeed = itemSystem != null ? itemSystem.MiningSpeedMultiplier : 1f;
                        animator.speed = stateInfo.length * miningSpeed /
                            Mathf.Max(0.0001f, npcData.SecondsPerHit);
                    }
                }
                else if (!Mathf.Approximately(animator.speed, 1f))
                {
                    animator.speed = 1f;
                }
            }

            ApplyGroundClamp();
        }

        private void SetMiningAnimationState(bool mining)
        {
            if (isMining == mining)
            {
                if (mining)
                {
                    SetMovingAnimationState(false);
                }
                return;
            }

            isMining = mining;
            if (animator != null && hasMiningBoolParameter)
            {
                animator.SetBool(miningAnimatorBoolParameter, mining);
            }

            if (mining)
            {
                SetMovingAnimationState(false);
            }
        }

        private void SetMovingAnimationState(bool moving)
        {
            moving &= !isMining;
            if (isMoving == moving)
            {
                return;
            }

            isMoving = moving;
            if (animator != null && hasMovingBoolParameter)
            {
                animator.SetBool(movingAnimatorBoolParameter, moving);
            }
        }

        /// <summary>
        /// Runtime safety net for floating or clipping feet: pulls the visual model up or down
        /// each frame until the lower of the two assigned foot bones sits exactly on the NPC's
        /// true local ground level - the bottom of the Capsule Collider, NOT local Y = 0 (the
        /// capsule's center sits at its middle by default, so the ground is half the capsule's
        /// height below the transform's origin; see localGroundY below). This does NOT replace
        /// the proper fix - baking "Root Transform Position (Y)" into the animation clip's pose
        /// on the FBX import settings (Based Upon: Original) - it only masks the symptom for
        /// clips that were not baked correctly, or as an extra safeguard.
        /// </summary>
        private void ApplyGroundClamp()
        {
            if (visualModelRoot == null || leftFootBone == null || rightFootBone == null)
            {
                return;
            }

            // The capsule's center sits at the middle of its height (Unity default), not at the
            // NPC transform's local origin - so local Y = 0 is NOT the ground. The true ground,
            // in the same local space as the foot bones below, is the bottom of the capsule
            // (this is exactly the offset NpcData.SpawnHeightOffset lifts the NPC by at spawn so
            // the capsule's bottom touches the real ground).
            float localGroundY = capsule != null ? capsule.center.y - capsule.height * 0.5f : 0f;

            Vector3 localPosition = visualModelRoot.localPosition;
            localPosition.y = visualModelBaseLocalY;
            visualModelRoot.localPosition = localPosition;

            float leftFootY = transform.InverseTransformPoint(leftFootBone.position).y;
            float rightFootY = transform.InverseTransformPoint(rightFootBone.position).y;
            float lowestFootY = Mathf.Min(leftFootY, rightFootY);
            float offsetFromGround = lowestFootY - localGroundY;
            if (Mathf.Abs(offsetFromGround) > groundClampEpsilon)
            {
                localPosition.y = visualModelBaseLocalY - offsetFromGround;
                visualModelRoot.localPosition = localPosition;
            }
        }

        private static bool HasParameter(Animator target, string parameterName,
            AnimatorControllerParameterType parameterType)
        {
            if (target == null || string.IsNullOrEmpty(parameterName))
            {
                return false;
            }

            foreach (AnimatorControllerParameter parameter in target.parameters)
            {
                if (parameter.type == parameterType && parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
