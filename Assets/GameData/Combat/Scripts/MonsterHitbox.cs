using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Separates bone-bound damage shapes from the ground navigation capsule.</summary>
    [DisallowMultipleComponent]
    public sealed class MonsterHitbox : MonoBehaviour
    {
        [SerializeField] private Collider body;
        [SerializeField] private Collider[] damageShapes;
        [SerializeField, Tooltip("Keep the ground navigation motor from blocking the player; damage triggers still receive bullets.")]
        private bool ignorePlayerBlocking;
        private CharacterController motor;
        private Collider[] playerColliders;
        private bool[] originalIgnores;
        public Collider Body => body;

        public void ConfigurePlayer(Component player)
        {
            if (!ignorePlayerBlocking || !player) return;
            RestorePlayerCollision();
            motor = GetComponent<CharacterController>();
            playerColliders = player.GetComponentsInChildren<Collider>(true);
            originalIgnores = new bool[playerColliders.Length];
            for (int i = 0; i < playerColliders.Length; i++)
                originalIgnores[i] = motor && playerColliders[i] && Physics.GetIgnoreCollision(motor, playerColliders[i]);
            ApplyPlayerCollision();
        }
        private void OnEnable() => ApplyPlayerCollision();
        private void OnDisable() => RestorePlayerCollision();
        private void ApplyPlayerCollision()
        {
            if (!motor || playerColliders == null) return;
            foreach (var other in playerColliders)
                if (other) Physics.IgnoreCollision(motor, other, true);
        }
        private void RestorePlayerCollision()
        {
            if (!motor || playerColliders == null || originalIgnores == null) return;
            for (int i = 0; i < playerColliders.Length; i++)
                if (playerColliders[i]) Physics.IgnoreCollision(motor, playerColliders[i], originalIgnores[i]);
        }

        public static bool IgnoreShot(Collider shape)
        {
            var hitbox = shape.GetComponentInParent<MonsterHitbox>();
            if (hitbox)
            {
                if (hitbox.damageShapes != null)
                    foreach (var damageShape in hitbox.damageShapes)
                        if (damageShape == shape) return false;
                return true;
            }
            return shape.isTrigger;
        }
    }
}
