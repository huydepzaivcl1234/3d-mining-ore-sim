using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiningSimulator.Ores
{
    /// <summary>Presents the existing upgrade view at a real, scene-editable anvil.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningUpgradeStation : MonoBehaviour, IMiningInteractable
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Transform player;
        [Min(0.5f), SerializeField] private float interactionDistance = 3.5f;
        [Min(0.5f), SerializeField] private float closeDistance = 5.5f;
        [Min(0.01f), SerializeField] private float fadeSeconds = 0.3f;
        [Min(0.1f), SerializeField] private float panelWidthMetres = 7.2f;
        [SerializeField] private Vector3 panelOffset = new(0f, 2f, 0f);
        [SerializeField] private Vector3 promptOffset = new(0f, 1.7f, 0f);
        [Tooltip("First entries appear first. Unlisted card types follow their hierarchy order.")]
        [SerializeField] private MiningUpgradeType[] upgradeOrder = {
            MiningUpgradeType.MoneyReward, 
            
            MiningUpgradeType.LuckyBlockReward, MiningUpgradeType.LuckyBlockDropChance,
            MiningUpgradeType.ItemDropChance };
        public MiningUpgradeType[] UpgradeOrder => upgradeOrder;
        private MiningUpgradePanel presenter;
        private MiningUiPanelCoordinator coordinator;
        private MiningCharacterHealth playerHealth;
        private Unity.AI.Navigation.NavMeshSurface mine;
        private Camera view;
        [SerializeField, HideInInspector] private RectTransform worldRoot, promptRoot;
        private RectTransform panel;
        private CanvasGroup panelGroup, promptGroup;
        private Canvas worldCanvas, promptCanvas;
        private TMP_Text promptText;
        private bool opened, configured;
        private float visibility, promptVisibility;

        public bool IsOpen => opened;
        public string InteractionLabel => MiningLocalization.TextKey("UPGRADE_STATION_NAME", "Mining upgrades");
        public bool CanInteract => configured && isActiveAndEnabled && Available &&
            PlayerDistance <= interactionDistance && !opened;
        private float PlayerDistance => player != null ? Vector3.Distance(player.position, transform.position) : float.PositiveInfinity;
        private bool Available => player != null && (playerHealth == null || playerHealth.Health > 0f) &&
            (coordinator == null || !coordinator.BlocksGameplay) && (mine == null || mine.isActiveAndEnabled);

        public void Initialize(MiningUpgradePanel owner, RectTransform existingPanel, MiningUiPanelCoordinator modalCoordinator)
        {
            if (configured || existingPanel == null) return;
            presenter = owner; coordinator = modalCoordinator; panel = existingPanel;
            var stats = FindFirstObjectByType<MiningPlayerStats>();
            if (player == null && stats != null) player = stats.transform;
            if (player != null) playerHealth = player.GetComponent<MiningCharacterHealth>();
            if (WorldNavigationBootstrap.Instance != null) mine = WorldNavigationBootstrap.Instance.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
            view = Camera.main;
            if (worldRoot == null) worldRoot = CreateCanvas("Upgrade World Canvas", new Vector2(1920f, 1080f), out panelGroup);
            else panelGroup = worldRoot.GetComponent<CanvasGroup>();
            worldCanvas = worldRoot.GetComponent<Canvas>();
            // Reparent the real view, not a duplicate: purchase/save/localization bindings survive.
            panel.gameObject.SetActive(false);
            if (panel.parent != worldRoot)
            {
                panel.SetParent(worldRoot, false);
                panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one;
                panel.pivot = new Vector2(0.5f, 0.5f);
                panel.offsetMin = panel.offsetMax = Vector2.zero;
                panel.localScale = Vector3.one;
            }
            var innerGroup = panel.GetComponent<CanvasGroup>();
            if (innerGroup != null) { innerGroup.alpha = 1f; innerGroup.interactable = innerGroup.blocksRaycasts = true; }
            if (promptRoot == null)
            {
            promptRoot = CreateCanvas("Upgrade Interaction", new Vector2(270f, 66f), out promptGroup);
            promptCanvas = promptRoot.GetComponent<Canvas>();
            var image = promptRoot.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.10f, 0.055f, 0.025f, 0.95f);
            var outline = promptRoot.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(1f, 0.65f, 0.18f); outline.effectDistance = new Vector2(2f, -2f);
            var button = promptRoot.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image; button.onClick.AddListener(Open);
            var label = new GameObject("Label", typeof(RectTransform));
            var rect = (RectTransform)label.transform;
            rect.SetParent(promptRoot, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 4f); rect.offsetMax = new Vector2(-8f, -4f);
            promptText = label.AddComponent<TextMeshProUGUI>();
            promptText.fontSize = 23f; promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = new Color(1f, 0.85f, 0.5f); promptText.raycastTarget = false;
            }
            else
            {
                promptGroup = promptRoot.GetComponent<CanvasGroup>();
                promptCanvas = promptRoot.GetComponent<Canvas>();
                promptText = promptRoot.GetComponentInChildren<TMP_Text>(true);
                promptRoot.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(Open);
            }
            configured = true;
            RefreshLanguage();
            worldRoot.gameObject.SetActive(false);
        }

        /// <summary>Called by the opt-in Editor authoring command; no ExecuteAlways runtime mutations.</summary>
        public void ShowAuthoringPreview()
        {
            if (worldRoot == null || panel == null) return;
            worldRoot.gameObject.SetActive(true); panel.gameObject.SetActive(true);
            panelGroup.alpha = 1f;
            worldRoot.position = transform.position + panelOffset;
            worldRoot.rotation = view != null ? view.transform.rotation : Quaternion.identity;
            worldRoot.localScale = Vector3.one * (panelWidthMetres / 1920f);
            promptRoot.position = transform.position + promptOffset;
            promptRoot.localScale = Vector3.one * 0.006f;
            promptRoot.gameObject.SetActive(false);
        }

        private RectTransform CreateCanvas(string name, Vector2 size, out CanvasGroup group)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster), typeof(CanvasGroup));
            var rect = (RectTransform)root.transform;
            rect.SetParent(transform, false); rect.sizeDelta = size;
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = view;
            // SVG gradients encode lookup coordinates beyond the default UI UV channel.
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
            var raycaster = root.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            raycaster.blockingObjects = UnityEngine.UI.GraphicRaycaster.BlockingObjects.ThreeD;
            group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f; group.interactable = group.blocksRaycasts = false;
            return rect;
        }

        private void OnEnable() => MiningLocalization.LanguageChanged += RefreshLanguage;
        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= RefreshLanguage;
            if (opened) presenter?.NotifyWorldPanelState(false);
            opened = false; visibility = promptVisibility = 0f;
            if (worldRoot != null) worldRoot.gameObject.SetActive(false);
            if (promptRoot != null) promptRoot.gameObject.SetActive(false);
        }
        private void RefreshLanguage()
        {
            if (promptText != null) promptText.text = MiningLocalization.TextKey("UPGRADE_STATION_INTERACT", "CLICK TO INTERACT\nMining upgrades");
        }
        public void Interact() => Open();
        public void SetInteractionFocused(bool focused) { }
        public void Open()
        {
            if (!CanInteract) return;
            opened = true;
            worldRoot.gameObject.SetActive(true); panel.gameObject.SetActive(true);
            presenter.NotifyWorldPanelState(true);
        }
        public void Close()
        {
            if (!opened) return;
            opened = false;
            panelGroup.interactable = panelGroup.blocksRaycasts = false;
            presenter.NotifyWorldPanelState(false);
        }
        private void Update()
        {
            if (!configured) return;
            if (opened && (!Available || PlayerDistance > Mathf.Max(interactionDistance, closeDistance))) Close();
            if (opened && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
            float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds);
            visibility = Mathf.MoveTowards(visibility, opened ? 1f : 0f, step);
            promptVisibility = Mathf.MoveTowards(promptVisibility, CanInteract ? 1f : 0f, step);
            float eased = Mathf.SmoothStep(0f, 1f, visibility);
            panelGroup.alpha = eased;
            panelGroup.interactable = panelGroup.blocksRaycasts = opened && visibility > 0.95f;
            if (!opened && visibility <= 0f && worldRoot.gameObject.activeSelf)
            { panel.gameObject.SetActive(false); worldRoot.gameObject.SetActive(false); }
            promptGroup.alpha = Mathf.SmoothStep(0f, 1f, promptVisibility);
            promptGroup.interactable = promptGroup.blocksRaycasts = CanInteract && promptVisibility > 0.9f;
            promptRoot.gameObject.SetActive(promptVisibility > 0f);
            if (visual != null) visual.gameObject.SetActive(mine == null || mine.isActiveAndEnabled);
        }
        private void LateUpdate()
        {
            if (!configured) return;
            if (view == null) view = Camera.main;
            if (view == null) return;
            float eased = Mathf.SmoothStep(0f, 1f, visibility);
            // Same fixed-world-position/camera-rotation billboard pattern as Celestial Disc.
            worldRoot.position = transform.position + panelOffset + Vector3.down * ((1f - eased) * 0.9f);
            worldRoot.rotation = view.transform.rotation;
            worldRoot.localScale = Vector3.one * (panelWidthMetres / 1920f) * Mathf.Lerp(0.12f, 1f, eased);
            promptRoot.position = transform.position + promptOffset;
            promptRoot.rotation = view.transform.rotation;
            promptRoot.localScale = Vector3.one * 0.006f;
            worldCanvas.worldCamera = view;
            promptCanvas.worldCamera = view;
        }
    }
}
