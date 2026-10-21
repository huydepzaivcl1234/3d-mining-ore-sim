using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns one rune session; restores time, animation, cursor and ambience on every exit path.</summary>
    [DisallowMultipleComponent]
    public sealed class RuneStation : MonoBehaviour
    {
        [SerializeField] private RuneUpgradeData data;
        [SerializeField] private Canvas panel;
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private RuneUpgradeRow[] rows;
        [SerializeField] private GameObject boundary;
        private MiningPlayerStats player;
        private MiningCharacterHealth health;
        private PlayerWallet wallet;
        private MiningAudioManager audioManager;
        private MiningOrbitCamera orbit;
        private MiningMainMenu menu;
        private Camera viewCamera;
        private Animator playerAnimator;
        private StarterAssets.ThirdPersonController movement;
        private CharacterController playerController;
        private float previousStepHeight;
        private bool stepHeightOverridden;
        private bool previousMovementTime;
        private AnimatorUpdateMode previousAnimatorMode;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible, previousShiftLock;
        private float previousTimeScale;
        private bool inside;
        private static RuneStation active;
        public static bool PlayerUsesRuneTime => active != null && active.inside;
        public static float PlayerDeltaTime => PlayerUsesRuneTime ? Time.unscaledDeltaTime : Time.deltaTime;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => active = null;
        private void Start()
        {
            player = FindFirstObjectByType<MiningPlayerStats>();
            if (player != null) player.RuneUpgrades.Configure(data);
            health = player != null ? player.GetComponent<MiningCharacterHealth>() : null;
            playerAnimator = player != null ? player.GetComponent<Animator>() : null;
            movement = player != null ? player.GetComponent<StarterAssets.ThirdPersonController>() : null;
            playerController = player != null ? player.GetComponent<CharacterController>() : null;
            // Convex hulls fill the stairs/platform recesses and create a sloping invisible wall.
            // This shrine is static; keep its actual triangles and retain all authored colliders.
            foreach (var collider in GetComponentsInChildren<MeshCollider>(true))
                if (collider.attachedRigidbody == null) collider.convex = false;
            wallet = FindFirstObjectByType<PlayerWallet>();
            audioManager = FindFirstObjectByType<MiningAudioManager>();
            orbit = FindFirstObjectByType<MiningOrbitCamera>();
            menu = FindFirstObjectByType<MiningMainMenu>(FindObjectsInactive.Include);
            viewCamera = Camera.main;
            ConfigurePanel();
            panel.worldCamera = viewCamera;
            panelGroup.alpha = 0f;
            panelGroup.interactable = panelGroup.blocksRaycasts = false;
            boundary.SetActive(false);
            for (int i = 0; i < rows.Length; i++) rows[i].Bind(this, i);
            if (player != null) player.RuneUpgrades.Changed += Refresh;
            if (wallet != null) wallet.GemsChanged += GemsChanged;
            MiningLocalization.LanguageChanged += Refresh;
            Refresh();
        }
        private void ConfigurePanel()
        {
            // Keep authored rows/references; only replace their presentation container at runtime.
            foreach (var layout in panel.GetComponents<UnityEngine.UI.LayoutGroup>()) layout.enabled = false;
            var rect = (RectTransform)panel.transform;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(data.barSize.x + 64, data.visibleRows * data.barSize.y + (data.visibleRows - 1) * data.panelSpacing);
            var scroll = panel.GetComponent<RuneUpgradeScrollRect>();
            if (scroll == null) scroll = panel.gameObject.AddComponent<RuneUpgradeScrollRect>();
            scroll.Initialize(rows,data);
        }
        private void Update()
        {
            if (data == null || player == null || panel == null) return;
            float distance = Vector3.ProjectOnPlane(player.transform.position - (transform.position + data.zoneOffset), Vector3.up).magnitude;
            bool available = player.isActiveAndEnabled && (health == null || health.Health > 0f) && (menu == null || !menu.IsOpen);
            bool shouldEnter = available && distance <= data.radius;
            UpdateStepHeight(available && distance <= data.revealDistance);
            if (inside && !shouldEnter) Exit();
            else if (!inside && shouldEnter && active == null && Time.timeScale > 0f) Enter();
            boundary.SetActive(available && distance <= data.revealDistance);
            boundary.transform.position = transform.position + data.zoneOffset;
            boundary.transform.localScale = Vector3.one * (data.radius * 2f);
            panelGroup.alpha = Mathf.MoveTowards(panelGroup.alpha, inside ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(.01f, data.fadeSeconds));
            panelGroup.interactable = panelGroup.blocksRaycasts = inside;
        }
        private void LateUpdate()
        {
            if (panel != null && viewCamera != null && data != null)
            {
                panel.transform.position = transform.position + data.panelOffset;
                Vector3 towardView = Vector3.ProjectOnPlane(viewCamera.transform.position - (transform.position + data.zoneOffset), Vector3.up);
                if (towardView.sqrMagnitude > .001f)
                    panel.transform.position += towardView.normalized * data.panelForwardOffset;
                // Shrine model scale must not resize the readable world-space interface.
                Vector3 inherited = panel.transform.parent != null ? panel.transform.parent.lossyScale : Vector3.one;
                panel.transform.localScale = new Vector3(
                    data.scrollCanvasScale / Mathf.Max(.0001f, Mathf.Abs(inherited.x)),
                    data.scrollCanvasScale / Mathf.Max(.0001f, Mathf.Abs(inherited.y)),
                    data.scrollCanvasScale / Mathf.Max(.0001f, Mathf.Abs(inherited.z)));
                panel.transform.rotation = viewCamera.transform.rotation;
            }
            if (inside) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }
        private void Enter()
        {
            inside = true; active = this;
            previousTimeScale = Time.timeScale;
            if (movement != null) { previousMovementTime = movement.UseUnscaledMovementTime; movement.UseUnscaledMovementTime = true; }
            previousShiftLock = orbit != null && orbit.IsShiftLocked;
            if (orbit != null) orbit.SetShiftLocked(false);
            previousCursorLock = Cursor.lockState; previousCursorVisible = Cursor.visible;
            if (playerAnimator != null)
            { previousAnimatorMode = playerAnimator.updateMode; playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime; }
            Time.timeScale = 0f;
            if (audioManager != null) audioManager.BeginRuneAmbience(data.ambience);
            Refresh();
        }
        private void Exit()
        {
            if (!inside) return;
            inside = false;
            if (active == this) active = null;
            if (Mathf.Approximately(Time.timeScale, 0f)) Time.timeScale = previousTimeScale;
            if (playerAnimator != null) playerAnimator.updateMode = previousAnimatorMode;
            if (movement != null) movement.UseUnscaledMovementTime = previousMovementTime;
            Cursor.lockState = previousCursorLock; Cursor.visible = previousCursorVisible;
            if (orbit != null && (health == null || health.Health > 0f) && (menu == null || !menu.IsOpen)) orbit.SetShiftLocked(previousShiftLock);
            if (audioManager != null) audioManager.EndRuneAmbience();
            if (panelGroup != null) panelGroup.interactable = panelGroup.blocksRaycasts = false;
        }
        public void Buy(int index)
        {
            if (!inside || player == null || !player.RuneUpgrades.TryBuy(index, wallet)) return;
            if (audioManager != null) audioManager.PlayWorldSfx(data.purchaseSfx, transform.position, 1f);
            Refresh();
        }
        private void GemsChanged(float _) => Refresh();
        private void Refresh()
        {
            if (player == null || wallet == null) return;
            foreach (var row in rows) if (row != null) row.Refresh(player.RuneUpgrades, wallet.CurrentGems);
        }
        private void UpdateStepHeight(bool nearby)
        {
            if (playerController == null) return;
            if (nearby && !stepHeightOverridden)
            {
                previousStepHeight = playerController.stepOffset;
                stepHeightOverridden = true;
                playerController.stepOffset = Mathf.Min(playerController.height, Mathf.Max(previousStepHeight, data.nearbyStepHeight));
            }
            else if (!nearby && stepHeightOverridden)
            {
                playerController.stepOffset = previousStepHeight;
                stepHeightOverridden = false;
            }
        }
        private void OnDisable() { Exit(); UpdateStepHeight(false); }
        private void OnDestroy()
        {
            Exit();
            UpdateStepHeight(false);
            if (player != null) player.RuneUpgrades.Changed -= Refresh;
            if (wallet != null) wallet.GemsChanged -= GemsChanged;
            MiningLocalization.LanguageChanged -= Refresh;
        }
        private void OnDrawGizmosSelected()
        {
            if (data == null) return;
            Gizmos.color = new Color(.2f, .8f, 1f, .6f);
            Gizmos.DrawWireSphere(transform.position + data.zoneOffset, data.radius);
        }
    }
}
