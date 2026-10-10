using System;
using StarterAssets;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Keep only a grounded, living pose, never an airborne/death-ragdoll transform.</summary>
    [DefaultExecutionOrder(31000)]
    [DisallowMultipleComponent]
    public sealed class PlayerCheckpointSave : MonoBehaviour
    {
        [Serializable] private sealed class Snapshot
        {
            public int version = 1;
            public string scene;
            public Vector3 position;
            public float yaw, health;
        }
        private const string Key = "ChestDefense.PlayerCheckpoint.v1";
        private ThirdPersonController movement;
        private CharacterController capsule;
        private MiningCharacterHealth health;
        private PlayerKnockbackRagdoll ragdoll;
        private float nextSample;
        private bool blocked;
        private Vector3 sceneSpawn;
        private Quaternion sceneRotation;
        private void Awake() { sceneSpawn = transform.position; sceneRotation = transform.rotation; }
        public static void ResetLoadedCheckpoints()
        {
            GameSave.DeleteKey(Key);
            foreach (var player in FindObjectsByType<PlayerCheckpointSave>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                player.blocked = false;
                if (player.capsule == null || player.health == null || !player.SupportedAndClear(player.sceneSpawn) ||
                    player.ragdoll != null && player.ragdoll.IsIncapacitated) continue;
                bool wasEnabled = player.capsule.enabled;
                player.capsule.enabled = false;
                player.transform.SetPositionAndRotation(player.sceneSpawn, player.sceneRotation);
                player.capsule.enabled = wasEnabled;
                player.movement.ResetMotionAfterRespawn();
                player.health.Respawn();
            }
        }
        private void Start()
        {
            movement = GetComponent<ThirdPersonController>();
            capsule = GetComponent<CharacterController>();
            health = GetComponent<MiningCharacterHealth>();
            ragdoll = GetComponent<PlayerKnockbackRagdoll>();
            if (!GameSave.HasKey(Key)) return;
            try
            {
                var saved = JsonUtility.FromJson<Snapshot>(GameSave.GetString(Key));
                if (saved == null || saved.version != 1 || !Finite(saved.position.x) || !Finite(saved.position.y) ||
                    !Finite(saved.position.z) || !Finite(saved.yaw) || !Finite(saved.health) || saved.health <= 0)
                { blocked = true; Debug.LogWarning("Invalid player checkpoint retained; using scene spawn.", this); return; }
                if (saved.scene != gameObject.scene.path || !SupportedAndClear(saved.position)) return;
                bool enabledBefore = capsule.enabled;
                capsule.enabled = false;
                transform.SetPositionAndRotation(saved.position, Quaternion.Euler(0, saved.yaw, 0));
                capsule.enabled = enabledBefore;
                Physics.SyncTransforms();
                health.RestoreSavedHealth(saved.health);
            }
            catch (ArgumentException) { blocked = true; Debug.LogWarning("Unreadable checkpoint retained.", this); }
        }
        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        private bool SupportedAndClear(Vector3 position)
        {
            if (capsule == null || movement == null || health == null) return false;
            // Never restore onto a chest, actor or cosmetic surface.
            if (!Physics.Raycast(position + Vector3.up * .25f, Vector3.down, out var hit, .65f,
                movement.GroundLayers, QueryTriggerInteraction.Ignore) ||
                hit.collider.GetComponentInParent<MiningCharacterHealth>() != null) return false;
            Vector3 scale = transform.lossyScale;
            float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float height = Mathf.Max(radius * 2, capsule.height * Mathf.Abs(scale.y));
            Vector3 center = position + transform.TransformVector(capsule.center);
            float half = height * .5f - radius;
            // Raise the lower sample slightly to avoid treating support contact as penetration.
            foreach (var overlap in Physics.OverlapCapsule(center + Vector3.up * half,
                center - Vector3.up * half + Vector3.up * .05f, Mathf.Max(.01f, radius - .03f), ~0, QueryTriggerInteraction.Ignore))
                if (overlap != hit.collider && !overlap.transform.IsChildOf(transform)) return false;
            return true;
        }
        private void Update()
        {
            if (Time.unscaledTime < nextSample) return;
            nextSample = Time.unscaledTime + 1f;
            if (blocked && !GameSave.HasKey(Key)) blocked = false; // explicit reset
            if (blocked || movement == null || !movement.enabled || !movement.Grounded || capsule == null ||
                !capsule.enabled || health == null || health.Health <= 0 || ragdoll != null && ragdoll.IsIncapacitated) return;
            Vector3 position = transform.position;
            if (!SupportedAndClear(position)) return;
            GameSave.SetString(Key, JsonUtility.ToJson(new Snapshot {
                scene = gameObject.scene.path, position = position, yaw = transform.eulerAngles.y, health = health.Health
            }));
        }
    }
}
