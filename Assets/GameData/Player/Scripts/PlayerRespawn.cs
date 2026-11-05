using MiningSimulator.Ores;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace GameData.Player.Script
{
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterHealth))]
    public sealed class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button respawnButton;
        [SerializeField] private Transform spawnPoint;
        [Header("Death presentation (real seconds)")]
        [SerializeField,Min(.1f)] private float deathSeconds=3f;
        [SerializeField,Range(.05f,1f)] private float deathTimeScale=.2f;
        [SerializeField,Min(.1f)] private float respawnPopSeconds=.4f;
        private TopDownSurvivor cameraView;
        private Coroutine deathRoutine;
        private bool ownsSlowMotion;
        private float previousTimeScale,previousFixedStep;
        private Vector3 buttonScale;
        private Vector2 buttonPosition;
        private RectTransform buttonRect;
        private CharacterHealth health;
        private PlayerRagdoll ragdoll;
        private PlayerController1 controller;
        private PistolShooting presentation;
        private bool dead, controllerWasEnabled, weaponWasEquipped;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private void Awake()
        {
            health = GetComponent<CharacterHealth>();ragdoll=GetComponent<PlayerRagdoll>();
            cameraView=GetComponent<TopDownSurvivor>();
            if(respawnButton){buttonRect=respawnButton.transform as RectTransform;buttonScale=respawnButton.transform.localScale;if(buttonRect)buttonPosition=buttonRect.anchoredPosition;}
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
            CancelDeathPresentation();
            if (health) health.Died -= HandleDeath;
            if (respawnButton) respawnButton.onClick.RemoveListener(RespawnNow);
        }
        private void HandleDeath()
        {
            if (dead) return;
            var audio=FindAnyObjectByType<MiningAudioManager>();
            if(!RunCheckpoint.IsRestoring&&audio&&audio.AudioData)audio.PlaySfx(audio.AudioData.PlayerDeathSfx,audio.AudioData.PlayerDeathVolume);
            dead = true; controllerWasEnabled = controller && controller.enabled;
            weaponWasEquipped = presentation && presentation.Equipped;
            if (controller) controller.enabled = false;
            GetComponent<PistolShooting>()?.CancelReload();
            if (presentation) { presentation.SetEquipped(false); presentation.SetCursorReleased(true); }
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            var body = GetComponent<Rigidbody>();
            if(ragdoll)ragdoll.Activate();
            else if(body)body.linearVelocity=new Vector3(0f,body.linearVelocity.y,0f);
            if(RunCheckpoint.IsRestoring){ShowRespawn();return;}
            if(panel)panel.SetActive(false);
            previousTimeScale=Time.timeScale;previousFixedStep=Time.fixedDeltaTime;ownsSlowMotion=true;
            Time.timeScale=deathTimeScale;Time.fixedDeltaTime=previousFixedStep*deathTimeScale;
            cameraView?.BeginDeathView(deathSeconds);
            deathRoutine=StartCoroutine(DeathPresentation());
        }
        private void RestoreClock()
        {
            if(!ownsSlowMotion)return;
            Time.timeScale=previousTimeScale;Time.fixedDeltaTime=previousFixedStep;ownsSlowMotion=false;
        }
        private void ResetButton()
        {
            if(respawnButton)respawnButton.transform.localScale=buttonScale;
            if(buttonRect)buttonRect.anchoredPosition=buttonPosition;
        }
        private void ShowRespawn(){ResetButton();if(panel)panel.SetActive(true);}
        private IEnumerator DeathPresentation()
        {
            yield return new WaitForSecondsRealtime(deathSeconds);
            RestoreClock();ShowRespawn();
            float elapsed=0;
            while(elapsed<respawnPopSeconds){
                float t=Mathf.Clamp01(elapsed/respawnPopSeconds),q=t-1;
                float punch=1+2.70158f*q*q*q+1.70158f*q*q;
                if(respawnButton)respawnButton.transform.localScale=buttonScale*Mathf.LerpUnclamped(.6f,1,punch);
                if(buttonRect)buttonRect.anchoredPosition=buttonPosition+Vector2.down*(12*(1-t)*(1-t));
                elapsed+=Time.unscaledDeltaTime;yield return null;
            }
            ResetButton();deathRoutine=null;
        }
        private void CancelDeathPresentation()
        {
            if(deathRoutine!=null){StopCoroutine(deathRoutine);deathRoutine=null;}
            RestoreClock();ResetButton();cameraView?.EndDeathView();
        }
        public void RespawnNow()
        {
            CancelDeathPresentation();
            bool restoreController = !dead ? controller && controller.enabled : controllerWasEnabled;
            bool restoreWeapon = dead ? weaponWasEquipped : presentation && presentation.Equipped;
            if (controller) controller.enabled = false;
            if(ragdoll)ragdoll.RestoreAnimatedPose();
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
            var ui = FindAnyObjectByType<UiPanelCoordinator>();
            bool modalOpen = ui && ui.BlocksGameplay;
            if (presentation) { presentation.SetEquipped(restoreWeapon); presentation.SetCursorReleased(modalOpen); }
            Physics.SyncTransforms();
        }
    }
}


