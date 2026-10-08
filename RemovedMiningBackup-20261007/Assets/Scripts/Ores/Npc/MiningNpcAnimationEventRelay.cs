using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Forwards animation events from the model Animator to its parent MiningNpc.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningNpcAnimationEventRelay : MonoBehaviour
    {
        // Locomotion clips are shared with Player. Player consumes these events
        // through ThirdPersonController; miners intentionally have no movement
        // SFX configured. Receive them here without forwarding to the player or
        // duplicating its footsteps. Mining impact events still use the path below.
        public void OnFootstep(AnimationEvent animationEvent) { }

        public void OnLand(AnimationEvent animationEvent) { }

        public void OnMiningImpact()
        {
            Transform current = transform.parent;
            while (current != null)
            {
                current.gameObject.SendMessage(nameof(OnMiningImpact), SendMessageOptions.DontRequireReceiver);
                current = current.parent;
            }
        }
    }
}
