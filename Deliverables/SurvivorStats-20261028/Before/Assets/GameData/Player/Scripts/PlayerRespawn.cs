using MiningSimulator.Ores;
using UnityEngine;
using UnityEngine.UI;

namespace GameData.Player.Script
{
    [DisallowMultipleComponent, RequireComponent(typeof(MiningCharacterHealth))]
    public sealed class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button respawnButton;
        [SerializeField] private Transform spawnPoint;
        private MiningCharacterHealth health;
        private PlayerController1 controller;
        private PistolShooting presentation;
        private bool dead, controllerWasEnabled, weaponWasEquipped;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private void Awake()
        {
            health = GetComponent<MiningCharacterHealth>();
            controller = GetComponent<PlayerController1>();
            presentation = GetComponent<PistolShooting>();
            initialPosition = transform.position; initialRotation = transform.rotation;
            if (panel) panel.SetActive(false);
        }
        private void OnEnable()
        {
            health.Died += HandleDeath;
            if (respawnButton) respawnButton.onClick.AddListener(RespawnNow);
        }
        private void OnDisable()
        {
            if (health) health.Died -= HandleDeath;
            if (respawnButton) respawnButton.onClick.RemoveListener(RespawnNow);
        }
        private void HandleDeath()
        {
            if (dead) return;
            dead = true; controllerWasEnabled = controller && controller.enabled;
            weaponWasEquipped = presentation && presentation.Equipped;
            if (controller) controller.enabled = false;
            GetComponent<PistolShooting>()?.CancelReload();
            if (presentation) { presentation.SetEquipped(false); presentation.SetCursorReleased(true); }
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            var body = GetComponent<Rigidbody>();
            if (body) body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
            if (panel) panel.SetActive(true);
        }
        public void RespawnNow()
        {
            bool restoreController = !dead ? controller && controller.enabled : controllerWasEnabled;
            bool restoreWeapon = dead ? weaponWasEquipped : presentation && presentation.Equipped;
            if (controller) controller.enabled = false;
            Vector3 position = spawnPoint ? spawnPoint.position : initialPosition;
            Quaternion rotation = spawnPoint ? spawnPoint.rotation : initialRotation;
            var body = GetComponent<Rigidbody>();
            transform.SetPositionAndRotation(position, rotation);
            if (body) { body.position = position; body.rotation = rotation; body.linearVelocity = body.angularVelocity = Vector3.zero; }
            controller?.ResetMotion();
            health.Respawn(); GetComponent<AnimationRunMana>()?.Refill(); GetComponent<PistolShooting>()?.ResetState();
            dead = false;
            if (panel) panel.SetActive(false);
            if (controller) controller.enabled = restoreController;
            var ui = FindAnyObjectByType<MiningUiPanelCoordinator>();
            bool modalOpen = ui && ui.BlocksGameplay;
            if (presentation) { presentation.SetEquipped(restoreWeapon); presentation.SetCursorReleased(modalOpen); }
            Physics.SyncTransforms();
        }
    }
}


