using UnityEngine;

namespace MiningSimulator.Ores
{
    // Each articulated body receives collisions from its child collider.
    public sealed class PlayerRagdollGroundContact : MonoBehaviour
    {
        private PlayerKnockbackRagdoll owner;
        internal void Initialize(PlayerKnockbackRagdoll ragdoll) => owner = ragdoll;
        private void OnCollisionEnter(Collision collision) => owner?.RegisterGroundContact(collision);
        private void OnCollisionStay(Collision collision) => owner?.RegisterGroundContact(collision);
    }
}
