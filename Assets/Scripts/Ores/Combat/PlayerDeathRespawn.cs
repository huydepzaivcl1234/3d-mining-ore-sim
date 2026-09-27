using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent, RequireComponent(typeof(MiningCharacterHealth))]
    public sealed class PlayerDeathRespawn : MonoBehaviour
    {
        [Header("Death / respawn")]
        [Min(1f), SerializeField] private float respawnSeconds = 10f;
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private Animator animator;
        [SerializeField] private string deathState = "Base Layer.Death";
        [SerializeField] private GameObject respawnPanel;
        [SerializeField] private TMP_Text countdownLabel;
        [Header("Spectator camera")]
        [SerializeField] private Camera spectatorCamera;
        [Tooltip("Camera drivers only (Orbit camera / Cinemachine Brain). Restored after respawn.")]
        [SerializeField] private Behaviour[] cameraDrivers;
        [Min(0.1f), SerializeField] private float cameraSpeed = 5f;
        [Min(0.01f), SerializeField] private float lookSensitivity = 0.12f;
        [Min(0.1f), SerializeField] private float cameraRadius = 0.35f;
        [SerializeField] private LayerMask worldLayers = ~0;
        private MiningCharacterHealth health;
        private ThirdPersonController movement;
        private PlayerCombatInput combat;
        private PlayerInput playerInput;
        private StarterAssetsInputs inputs;
        private CharacterController playerBody;
        private CharacterController cameraBody;
        private GameObject cameraProxy;
        private bool dead, movementEnabled, combatEnabled, inputEnabled, bodyEnabled;
        private bool[] driverEnabled;
        private Vector3 spawnPosition, cameraPosition;
        private Quaternion spawnRotation, cameraRotation;
        private float remaining, yaw, pitch, animatorSpeed, nearClip;
        private float[] layerWeights;
        private CursorLockMode cursorLock;
        private bool cursorVisible, rootMotion;
        private readonly Collider[] overlaps = new Collider[64];

        private void Awake()
        {
            health = GetComponent<MiningCharacterHealth>();
            movement = GetComponent<ThirdPersonController>();
            combat = GetComponent<PlayerCombatInput>();
            playerInput = GetComponent<PlayerInput>();
            inputs = GetComponent<StarterAssetsInputs>();
            playerBody = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (spectatorCamera == null) spectatorCamera = Camera.main;
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            if (respawnPanel != null) respawnPanel.SetActive(false);
        }

        private void OnEnable() => health.Died += Die;
        private void OnDisable()
        {
            if (health != null) health.Died -= Die;
            if (dead) RestoreControls();
            dead = false;
        }

        private void Die()
        {
            if (dead) return;
            dead = true;
            var stats = MiningPlayerStats.For(this);
            remaining = Mathf.Max(1f, stats != null ? stats.respawnSeconds : respawnSeconds);
            movementEnabled = movement != null && movement.enabled;
            combatEnabled = combat != null && combat.enabled;
            inputEnabled = playerInput != null && playerInput.inputIsActive;
            bodyEnabled = playerBody != null && playerBody.enabled;
            if (movement != null) movement.enabled = false;
            if (combat != null) combat.enabled = false;
            if (playerInput != null) playerInput.DeactivateInput();
            ClearInput();
            if (playerBody != null) playerBody.enabled = false;
            if (animator != null)
            {
                animatorSpeed = animator.speed;
                rootMotion = animator.applyRootMotion;
                animator.applyRootMotion = false;
                animator.speed = 1f;
                layerWeights = new float[animator.layerCount];
                for (int i = 0; i < layerWeights.Length; i++)
                {
                    layerWeights[i] = animator.GetLayerWeight(i);
                    if (i > 0) animator.SetLayerWeight(i, 0f);
                }
                int state = Animator.StringToHash(deathState);
                if (animator.HasState(0, state)) animator.CrossFadeInFixedTime(state, 0.15f, 0);
                else Debug.LogWarning("Player death state is missing. Run Setup/Player Death And Respawn.", this);
            }
            if (respawnPanel != null) respawnPanel.SetActive(true);
            BeginCamera();
            UpdateCountdown();
        }

        private void BeginCamera()
        {
            cursorLock = Cursor.lockState;
            cursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            driverEnabled = new bool[cameraDrivers != null ? cameraDrivers.Length : 0];
            for (int i = 0; i < driverEnabled.Length; i++)
                if (cameraDrivers[i] != null)
                {
                    driverEnabled[i] = cameraDrivers[i].enabled;
                    cameraDrivers[i].enabled = false;
                }
            if (spectatorCamera == null) return;
            cameraPosition = spectatorCamera.transform.position;
            cameraRotation = spectatorCamera.transform.rotation;
            nearClip = spectatorCamera.nearClipPlane;
            // The eye sphere must cover the whole near plane, not just its centre.
            float halfHeight = nearClip * Mathf.Tan(spectatorCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float radius = Mathf.Max(cameraRadius, Mathf.Sqrt(nearClip * nearClip +
                halfHeight * halfHeight * (1f + spectatorCamera.aspect * spectatorCamera.aspect)) + 0.05f);
            cameraProxy = new GameObject("Respawn Camera Collision");
            cameraProxy.transform.position = cameraPosition;
            cameraBody = cameraProxy.AddComponent<CharacterController>();
            cameraBody.radius = radius;
            cameraBody.height = radius * 2f;
            cameraBody.center = Vector3.zero;
            cameraBody.skinWidth = 0.02f;
            cameraBody.minMoveDistance = 0f;
            cameraBody.stepOffset = 0f;
            // Resolve existing overlap before the first camera frame.
            for (int pass = 0; pass < 8; pass++)
            {
                int count = Physics.OverlapSphereNonAlloc(cameraProxy.transform.position, radius,
                    overlaps, worldLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    Collider obstacle = overlaps[i];
                    if (obstacle == cameraBody || obstacle.transform.IsChildOf(transform)) continue;
                    if (Physics.ComputePenetration(cameraBody, cameraProxy.transform.position,
                        Quaternion.identity, obstacle, obstacle.transform.position, obstacle.transform.rotation,
                        out Vector3 direction, out float distance))
                        cameraProxy.transform.position += direction * (distance + 0.02f);
                }
            }
            Vector3 euler = cameraRotation.eulerAngles;
            yaw = euler.y;
            pitch = Mathf.DeltaAngle(0f, euler.x);
        }

        private void Update()
        {
            if (!dead)
            {
                // A component re-enabled while its owner is still dead must not unlock gameplay.
                if (health.Health <= 0f) Die();
                return;
            }
            remaining -= Time.deltaTime;
            UpdateCountdown();
            if (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName(deathState) &&
                !animator.IsInTransition(0) && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
                animator.speed = 0f;
            if (remaining <= 0f) TryRespawn();
        }

        private void LateUpdate()
        {
            if (!dead || spectatorCamera == null || cameraBody == null || Time.deltaTime <= 0f) return;
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * lookSensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -85f, 85f);
            }
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 move = Vector3.zero;
            if (keyboard != null)
            {
                move.x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
                move.z = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
                move.y = (keyboard.spaceKey.isPressed ? 1 : 0) - (keyboard.leftCtrlKey.isPressed ? 1 : 0);
            }
            Vector3 motion = rotation * Vector3.ClampMagnitude(move, 1f) * cameraSpeed * Time.deltaTime;
            // Sweep prevents high-speed tunnelling; CharacterController supplies wall sliding.
            if (motion.sqrMagnitude > 0.000001f && Physics.SphereCast(cameraProxy.transform.position, cameraBody.radius, motion.normalized,
                out RaycastHit hit, motion.magnitude + 0.02f, worldLayers, QueryTriggerInteraction.Ignore))
                motion = motion.normalized * Mathf.Max(0f, hit.distance - 0.02f);
            cameraBody.Move(motion);
            spectatorCamera.transform.SetPositionAndRotation(cameraProxy.transform.position, rotation);
        }

        private void UpdateCountdown()
        {
            if (countdownLabel == null) return;
            countdownLabel.text = string.Format(MiningLocalization.TextKey("PLAYER_RESPAWN_COUNTDOWN",
                "Respawn in {0}s"), Mathf.Max(0, Mathf.CeilToInt(remaining))) + "\n" +
                MiningLocalization.TextKey("PLAYER_SPECTATOR_HELP", "WASD: move | Space/Ctrl: up/down | Hold RMB: look");
        }

        private void TryRespawn()
        {
            Vector3 position = respawnPoint != null ? respawnPoint.position : spawnPosition;
            Quaternion rotation = respawnPoint != null ? respawnPoint.rotation : spawnRotation;
            // Never resurrect inside an obstacle. Let the designer clear the marker.
            if (playerBody != null)
            {
                float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
                float radius = playerBody.radius * scale * 0.9f;
                Vector3 center = position + Vector3.up * 0.03f + rotation * Vector3.Scale(playerBody.center, transform.lossyScale);
                float half = Mathf.Max(0f, playerBody.height * Mathf.Abs(transform.lossyScale.y) * 0.5f - radius);
                int count = Physics.OverlapCapsuleNonAlloc(center + Vector3.up * half,
                    center - Vector3.up * half, radius, overlaps, worldLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                    if (!overlaps[i].transform.IsChildOf(transform))
                    {
                        remaining = 1f;
                        return;
                    }
            }
            transform.SetPositionAndRotation(position, rotation);
            if (movement != null) movement.ResetMotionAfterRespawn();
            health.Respawn();
            dead = false;
            RestoreControls();
        }

        private void ClearInput()
        {
            if (inputs == null) return;
            inputs.move = inputs.look = Vector2.zero;
            inputs.jump = inputs.sprint = false;
        }

        private void RestoreControls()
        {
            if (respawnPanel != null) respawnPanel.SetActive(false);
            if (cameraProxy != null) Destroy(cameraProxy);
            if (spectatorCamera != null)
            {
                spectatorCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                spectatorCamera.nearClipPlane = nearClip;
            }
            if (driverEnabled != null)
                for (int i = 0; i < driverEnabled.Length; i++)
                    if (cameraDrivers[i] != null) cameraDrivers[i].enabled = driverEnabled[i];
            if (animator != null)
            {
                animator.Rebind();
                animator.speed = animatorSpeed;
                animator.applyRootMotion = rootMotion;
                if (layerWeights != null)
                    for (int i = 0; i < layerWeights.Length; i++) animator.SetLayerWeight(i, layerWeights[i]);
                animator.Update(0f);
            }
            ClearInput();
            if (playerBody != null) playerBody.enabled = bodyEnabled;
            if (movement != null) movement.enabled = movementEnabled;
            if (combat != null) combat.enabled = combatEnabled;
            if (playerInput != null && inputEnabled) playerInput.ActivateInput();
            Cursor.lockState = cursorLock;
            Cursor.visible = cursorVisible;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 point = respawnPoint != null ? respawnPoint.position : transform.position;
            Gizmos.DrawWireSphere(point + Vector3.up, 0.5f);
            Gizmos.DrawLine(point, point + Vector3.up * 2f);
        }

#if UNITY_EDITOR
        [ContextMenu("Test Death (Play Mode)")]
        private void TestDeath()
        {
            if (Application.isPlaying && isActiveAndEnabled && health != null)
                health.ApplyDamage(health.MaxHealth);
        }
#endif
    }
}
