using UnityEngine;

namespace GameData.Player.Script
{
    /// <summary>Visual-only, per-weapon tuning. Does not change input, movement or authored poses.</summary>
    [CreateAssetMenu(menuName = "Chest Defense/Weapons/FPS Motion Profile", fileName = "PistolMotionProfile")]
    public sealed class FpsWeaponMotionProfile : ScriptableObject
    {
        [Header("Look sway - camera angular velocity, degrees/second")]
        [Min(0f)] public float lookPositionScale = .000035f;
        [Min(0f)] public float lookRotationScale = .012f;
        [Min(0f)] public float maxLookSpeed = 240f;
        [Min(0f)] public float maxPosition = .018f;
        [Min(0f)] public float maxRotation = 2.5f;
        [Header("Movement sway - existing blend tree velocity units")]
        [Min(.1f)] public float walkMoveSpeed = 2f;
        [Min(.1f)] public float fullMoveSpeed = 6f;
        public Vector2 movementPosition = new Vector2(.005f, .002f);
        [Min(0f)] public float strafeRoll = 1.2f;
        [Header("Step bob")]
        public Vector2 bobAmplitude = new Vector2(.004f, .006f);
        [Min(0f)] public float walkFrequency = 1.5f;
        [Min(0f)] public float runFrequency = 2.1f;
        [Min(0f)] public float runMultiplier = 1.3f;
        [Min(0f)] public float bobRoll = .35f;
        [Header("Blending")]
        [Tooltip("0 keeps the sight locked in ADS; 1 preserves all hip sway.")]
        [Range(0f, 1f)] public float adsMotionMultiplier = .12f;
        [Tooltip("Seconds to settle approximately 90% after an input change.")]
        [Min(.02f)] public float responseSeconds = .16f;
    }
}
