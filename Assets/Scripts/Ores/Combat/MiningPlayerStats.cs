using StarterAssets;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [DefaultExecutionOrder(-1000), DisallowMultipleComponent]
    public sealed class MiningPlayerStats : MonoBehaviour
    {
        [SerializeField] private MiningPlayerStatsData data;
        public MiningPlayerStatsData Data => data;
        private ThirdPersonController movement;
        private void Awake() { movement = GetComponent<ThirdPersonController>(); ApplyMovement(); }
        // Movement's public fields are compatibility inputs, not a second authoring source.
        private void Update() => ApplyMovement();
        private void ApplyMovement()
        {
            if (data == null || movement == null) return;
            movement.MoveSpeed = Mathf.Max(0, data.MoveSpeed);
            movement.SprintSpeed = Mathf.Max(0, data.SprintSpeed);
            movement.RotationSmoothTime = Mathf.Max(0.001f, data.RotationSmoothTime);
            movement.SpeedChangeRate = Mathf.Max(0, data.SpeedChangeRate);
            movement.JumpHeight = Mathf.Max(0, data.JumpHeight);
            movement.Gravity = data.Gravity;
            movement.JumpTimeout = Mathf.Max(0, data.JumpTimeout);
            movement.FallTimeout = Mathf.Max(0, data.FallTimeout);
        }
        public static MiningPlayerStatsData For(Component owner)
        {
            var stats = owner.GetComponent<MiningPlayerStats>();
            return stats != null ? stats.Data : null;
        }
    }
}
