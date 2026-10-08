using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Coordinates exclusive mining modals and smooth CanvasGroup fades.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningUiPanelCoordinator : MonoBehaviour
    {
        [SerializeField] private MiningUiData uiData;
        [SerializeField] private RectTransform shopPanel;
        [SerializeField] private RectTransform pcQuickActions;
        [SerializeField] private RectTransform rebirthHud;
        [SerializeField] private RectTransform audioMenuButton;
        [SerializeField] private RectTransform upgradePanel;
        [SerializeField] private RectTransform rebirthPanel;
        [SerializeField] private RectTransform audioSettingsPanel;
        [SerializeField] private RectTransform npcProgressHud;
        [SerializeField] private RectTransform inventoryMenuButton;
        [SerializeField] private RectTransform inventoryPanel;
        [SerializeField] private RectTransform effectToast;
        [SerializeField] private RectTransform gemHud;
        [SerializeField] private RectTransform shopMenuButton;
        [SerializeField] private RectTransform questMenuButton;
        [Tooltip("One CanvasGroup fades the authored Docker background and all its buttons together.")]
        [SerializeField] private RectTransform dock;
        [SerializeField] private RectTransform questPanel;
        [Tooltip("Auto-found in Awake when left empty.")]
        [SerializeField] private MiningOrbitCamera orbitCamera;

        private Vector2 upgradeHome;
        private Vector2 rebirthPanelHome;
        private Vector2 audioSettingsHome;
        private Vector2 inventoryPanelHome;
        private Vector2 npcProgressHome;
        private RectTransform activeModal;
        private bool initialized;
        private readonly Dictionary<RectTransform, Vector2> additionalPanelHomes = new();
        private CanvasGroup backdropGroup;
        private RectTransform backdropRect;
        private Coroutine backdropRoutine;
        private readonly Dictionary<CanvasGroup, HudState> additionalHudStates = new();
        private readonly List<RectTransform> additionalHudRoots = new();
        private bool suppressingAdditionalHud;
        private float additionalHudHiddenAt;
        private bool mainMenuOpen;
        private readonly Dictionary<Renderer, bool> worldBarStates = new();
        private float nextWorldBarScan;
        private sealed class HudState
        {
            public float Alpha;
            public bool Interactable, BlocksRaycasts;
        }

        public MiningUiData UiData => uiData;
        public bool BlocksGameplay => mainMenuOpen || activeModal != null;

        /// <summary>The anvil view is a world interaction, not a movement-blocking modal.</summary>
        public void RegisterWorldUpgradePanel(RectTransform panel)
        {
            if (upgradePanel == panel) upgradePanel = null;
            if (activeModal == panel)
            {
                activeModal = null;
                orbitCamera?.SetInputLocked(false);
                HideBackdrop();
                AnimateBasePanels(!mainMenuOpen);
            }
            additionalPanelHomes.Remove(panel);
        }

        public void RegisterInventoryUi(RectTransform menuButton, RectTransform panel)
        {
            inventoryMenuButton = menuButton;
            if (dock == null && menuButton != null && menuButton.parent != null &&
                menuButton.parent.name == "Docker")
                dock = menuButton.parent as RectTransform;
            inventoryPanel = panel;
            inventoryPanelHome = GetPosition(panel);
        }

        /// <summary>Registers optional HUD added by the Gem and Shop setup tools.</summary>
        public void RegisterGemAndShopUi(RectTransform gem, RectTransform shopButton)
        {
            if (gem != null)
            {
                gemHud = gem;
            }
            if (shopButton != null)
            {
                shopMenuButton = shopButton;
            }
        }

        public void RegisterQuestUi(RectTransform questButton, RectTransform panel = null)
        {
            if (questButton != null)
            {
                questMenuButton = questButton;
                if (dock == null && questButton.parent != null && questButton.parent.name == "Docker")
                    dock = questButton.parent as RectTransform;
            }
            if (panel != null) questPanel = panel;
        }

        private float TransitionDuration => uiData != null ? uiData.PanelTransitionDuration : 0.28f;
        private void Awake()
        {
            if (orbitCamera == null)
            {
                orbitCamera = FindFirstObjectByType<MiningOrbitCamera>(FindObjectsInactive.Include);
            }
            ResolveOptionalHudReferences();
            CacheHomePositions();
        }

        private void Start()
        {
            ResolveOptionalHudReferences();
            CacheHomePositions();
            activeModal = activeModal != null ? activeModal : FindActiveModal();
            if (activeModal != null)
            {
                SetBasePanelsImmediately(false);
                activeModal.anchoredPosition = GetHomePosition(activeModal);
                SetCanvasAlpha(activeModal, 1f, true);
            }
            else
            {
                SetBasePanelsImmediately(!mainMenuOpen);
                HideModalImmediately(upgradePanel);
                HideModalImmediately(rebirthPanel);
                HideModalImmediately(audioSettingsPanel);
                HideModalImmediately(inventoryPanel);
                HideModalImmediately(questPanel);
            }
        }

        public void OpenPanel(RectTransform panel)
        {
            if (panel == null)
            {
                return;
            }

            EnsureInitialized();
            CacheAdditionalPanelHome(panel);
            StopAllCoroutines();
            var previousModal = activeModal;
            activeModal = panel;
            if (previousModal != null && previousModal != panel)
            {
                HideModalImmediately(previousModal);
            }
            panel.gameObject.SetActive(true);
            panel.anchoredPosition = GetHomePosition(panel);
            SetCanvasAlpha(panel, 0f, false);
            AnimateBasePanels(false);
            orbitCamera?.SetInputLocked(true);
            ShowBackdrop(panel);
            CanvasGroup group = GetCanvasGroup(panel);
            StartCoroutine(AnimateCanvasGroupAlpha(group, 1f, TransitionDuration,
                () => { if (activeModal == panel) SetInteraction(panel, true); }));
        }

        public void ClosePanel(RectTransform panel)
        {
            if (panel == null)
            {
                return;
            }

            EnsureInitialized();
            if (activeModal != panel) return; // An old panel closing must not reveal HUD behind a new modal.
            StopAllCoroutines();
            SetInteraction(panel, false);
            AnimateBasePanels(!mainMenuOpen);
            orbitCamera?.SetInputLocked(false);
            HideBackdrop();
            CanvasGroup group = GetCanvasGroup(panel);
            StartCoroutine(AnimateCanvasGroupAlpha(group, 0f, TransitionDuration, () =>
            {
                if (activeModal == panel) activeModal = null;
                panel.gameObject.SetActive(false);
            }));
        }

        /// <summary>Fades the HUD in place without changing any authored RectTransform.</summary>
        public void SetBaseHudVisible(bool visible)
        {
            EnsureInitialized();
            StopAllCoroutines();
            if (!visible)
            {
                var previousModal = activeModal;
                activeModal = null;
                HideModalImmediately(previousModal);
                HideModalImmediately(upgradePanel);
                HideModalImmediately(rebirthPanel);
                HideModalImmediately(audioSettingsPanel);
                HideModalImmediately(inventoryPanel);
                HideModalImmediately(questPanel);
                activeModal = null;
                orbitCamera?.SetInputLocked(false);
                HideBackdrop();
            }
            AnimateBasePanels(visible && !mainMenuOpen && activeModal == null);
        }
        public void SetMainMenuOpen(bool open)
        {
            mainMenuOpen = open;
            EnsureInitialized();
            // Main menu remains the return view when Shop is opened from it.
            if (activeModal != null) return;
            StopAllCoroutines();
            AnimateBasePanels(!mainMenuOpen && activeModal == null);
        }
        // Panels with their own animations retain their presentation and use only modal ownership.
        public void NotifyExternalPanelOpened(RectTransform panel)
        {
            if (panel == null) return;
            EnsureInitialized();
            CacheAdditionalPanelHome(panel);
            StopAllCoroutines();
            var previous = activeModal;
            activeModal = panel;
            if (previous != null && previous != panel) HideModalImmediately(previous);
            AnimateBasePanels(false);
            orbitCamera?.SetInputLocked(true);
            HideBackdrop();
        }
        public void NotifyExternalPanelClosed(RectTransform panel)
        {
            if (activeModal != panel) return;
            activeModal = null;
            StopAllCoroutines();
            AnimateBasePanels(!mainMenuOpen);
            orbitCamera?.SetInputLocked(false);
            HideBackdrop();
        }

        private void CacheHomePositions()
        {
            if (initialized)
            {
                return;
            }

            upgradeHome = GetPosition(upgradePanel);
            rebirthPanelHome = GetPosition(rebirthPanel);
            audioSettingsHome = GetPosition(audioSettingsPanel);
            inventoryPanelHome = GetPosition(inventoryPanel);
            npcProgressHome = GetPosition(npcProgressHud);
            initialized = true;
        }

        private void EnsureInitialized()
        {
            if (!initialized)
            {
                CacheHomePositions();
            }
        }

        private void ResolveOptionalHudReferences()
        {
            // The Docker belongs to the Canvas; fading only its children leaves the background visible.
            if (dock == null)
            {
                Transform parent = inventoryMenuButton != null ? inventoryMenuButton.parent :
                    questMenuButton != null ? questMenuButton.parent : null;
                if (parent != null && parent.name == "Docker")
                    dock = parent as RectTransform;
            }
            // The two shop buttons can be scene-authored outside the compact status panel.
            if (pcQuickActions == null)
            {
                Transform quickCanvas = transform.Find("Mining HUD Canvas");
                if (quickCanvas != null)
                    pcQuickActions = quickCanvas.Find("PC Quick Actions") as RectTransform;
            }
            if (dock != null && gemHud != null && shopMenuButton != null && questMenuButton != null)
            {
                return;
            }

            Transform canvas = transform.Find("Mining HUD Canvas");
            if (canvas == null)
            {
                Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
                foreach (Canvas candidate in canvases)
                {
                    if (candidate.name == "Mining HUD Canvas")
                    {
                        canvas = candidate.transform;
                        break;
                    }
                }
            }
            if (canvas == null)
            {
                return;
            }

            dock ??= canvas.Find("Docker") as RectTransform;
            gemHud ??= canvas.Find("Gem HUD") as RectTransform;
            shopMenuButton ??= canvas.Find("Shop Menu Button") as RectTransform;
            questMenuButton ??= canvas.Find("Quest Menu Button") as RectTransform;
        }

        private RectTransform FindActiveModal()
        {
            if (upgradePanel != null && upgradePanel.gameObject.activeSelf) return upgradePanel;
            if (rebirthPanel != null && rebirthPanel.gameObject.activeSelf) return rebirthPanel;
            if (audioSettingsPanel != null && audioSettingsPanel.gameObject.activeSelf)
                return audioSettingsPanel;
            if (inventoryPanel != null && inventoryPanel.gameObject.activeSelf)
                return inventoryPanel;
            if (questPanel != null && questPanel.gameObject.activeSelf)
                return questPanel;
            return null;
        }

        private void AnimateBasePanels(bool visible)
        {
            SetAdditionalHudVisible(visible, false);
            FadeBasePanel(shopPanel, visible);
            FadeBasePanel(pcQuickActions, visible);
            FadeBasePanel(rebirthHud, visible);
            FadeBasePanel(audioMenuButton, visible);
            FadeBasePanel(npcProgressHud, visible);
            FadeBasePanel(dock, visible);
            if (!IsInDock(inventoryMenuButton)) FadeBasePanel(inventoryMenuButton, visible);
            FadeBasePanel(effectToast, visible);
            FadeBasePanel(gemHud, visible);
            if (!IsInDock(shopMenuButton)) FadeBasePanel(shopMenuButton, visible);
            if (!IsInDock(questMenuButton)) FadeBasePanel(questMenuButton, visible);
        }

        private bool IsInDock(RectTransform panel) =>
            panel != null && dock != null && panel != dock && panel.IsChildOf(dock);

        private void FadeBasePanel(RectTransform panel, bool visible)
        {
            if (panel == null) return;
            panel.gameObject.SetActive(true);
            if (panel.TryGetComponent(out MiningUiSmoothFade fade))
            {
                if (visible) fade.Show();
                else fade.Hide();
                return;
            }
            // Existing HUD panels without the reusable component still fade normally.
            SetInteraction(panel, false);
            CanvasGroup group = GetCanvasGroup(panel);
            StartCoroutine(AnimateCanvasGroupAlpha(group, visible ? 1f : 0f,
                TransitionDuration, () => { if (visible) SetInteraction(panel, true); }));
        }

        private void SetBasePanelsImmediately(bool visible)
        {
            SetAdditionalHudVisible(visible, true);
            SetBasePanelImmediately(shopPanel, visible);
            SetBasePanelImmediately(pcQuickActions, visible);
            SetBasePanelImmediately(rebirthHud, visible);
            SetBasePanelImmediately(audioMenuButton, visible);
            SetBasePanelImmediately(npcProgressHud, visible);
            SetBasePanelImmediately(dock, visible);
            if (!IsInDock(inventoryMenuButton)) SetBasePanelImmediately(inventoryMenuButton, visible);
            SetBasePanelImmediately(effectToast, visible);
            SetBasePanelImmediately(gemHud, visible);
            if (!IsInDock(shopMenuButton)) SetBasePanelImmediately(shopMenuButton, visible);
            if (!IsInDock(questMenuButton)) SetBasePanelImmediately(questMenuButton, visible);
        }

        private bool IsExistingHudRoot(Transform root)
        {
            RectTransform[] known = { shopPanel, pcQuickActions, rebirthHud, audioMenuButton,
                npcProgressHud, dock, inventoryMenuButton, effectToast, gemHud, shopMenuButton, questMenuButton };
            foreach (var item in known)
                if (item != null && (item == root || item.IsChildOf(root))) return true;
            return false;
        }
        private bool IsModalRoot(Transform root)
        {
            RectTransform[] modals = { activeModal, upgradePanel, rebirthPanel, audioSettingsPanel, inventoryPanel, questPanel, backdropRect };
            foreach (var panel in modals)
                if (panel != null && (panel == root || panel.IsChildOf(root))) return true;
            foreach (var panel in additionalPanelHomes.Keys)
                if (panel != null && (panel == root || panel.IsChildOf(root))) return true;
            // Startup/menu/transition views must never be treated as gameplay HUD.
            return root.GetComponentInChildren<MiningMainMenu>(true) != null ||
                root.GetComponentInChildren<JuicyPlaySelection>(true) != null ||
                root.GetComponentInChildren<WanderingTraderPanel>(true) != null ||
                root.name.Contains("Transition") || root.name == "Rebirth Flash";
        }
        private void SetAdditionalHudVisible(bool visible, bool immediate)
        {
            SetWorldBarsVisible(visible);
            if (!visible)
            {
                Canvas canvas = shopPanel != null ? shopPanel.GetComponentInParent<Canvas>() :
                    activeModal != null ? activeModal.GetComponentInParent<Canvas>() : null;
                if (canvas == null) return;
                suppressingAdditionalHud = true;
                additionalHudHiddenAt = Time.unscaledTime + (immediate ? 0 : TransitionDuration);
                foreach (Transform child in canvas.transform)
                {
                    if (!child.gameObject.activeInHierarchy || IsExistingHudRoot(child) || IsModalRoot(child)) continue;
                    if (child.GetComponentInChildren<UnityEngine.UI.Graphic>(true) == null) continue;
                    var root = child as RectTransform;
                    if (root == null) continue;
                    if (!additionalHudRoots.Contains(root)) additionalHudRoots.Add(root);
                    CaptureAndHide(GetCanvasGroup(root), immediate);
                    // A nested group can bypass its parent's alpha; suppress it explicitly too.
                    foreach (var nested in root.GetComponentsInChildren<CanvasGroup>(true))
                        if (nested.ignoreParentGroups) CaptureAndHide(nested, immediate);
                }
                return;
            }
            suppressingAdditionalHud = false;
            foreach (var entry in additionalHudStates)
            {
                var group = entry.Key; var state = entry.Value;
                if (group == null) continue;
                if (immediate || !group.gameObject.activeInHierarchy)
                {
                    group.alpha = state.Alpha; group.interactable = state.Interactable; group.blocksRaycasts = state.BlocksRaycasts;
                }
                else StartCoroutine(AnimateCanvasGroupAlpha(group, state.Alpha, TransitionDuration, () =>
                {
                    if (group == null || suppressingAdditionalHud) return;
                    group.interactable = state.Interactable; group.blocksRaycasts = state.BlocksRaycasts;
                }));
            }
            if (immediate) { additionalHudStates.Clear(); additionalHudRoots.Clear(); }
            else StartCoroutine(ReleaseAdditionalHudSnapshot());
        }
        private IEnumerator ReleaseAdditionalHudSnapshot()
        {
            float until = Time.unscaledTime + Mathf.Max(0.01f, TransitionDuration);
            while (Time.unscaledTime <= until) yield return null;
            if (suppressingAdditionalHud) yield break;
            additionalHudStates.Clear(); additionalHudRoots.Clear();
        }
        private void CaptureAndHide(CanvasGroup group, bool immediate)
        {
            if (!additionalHudStates.ContainsKey(group))
                additionalHudStates.Add(group, new HudState { Alpha = group.alpha, Interactable = group.interactable, BlocksRaycasts = group.blocksRaycasts });
            group.interactable = false; group.blocksRaycasts = false;
            if (immediate) group.alpha = 0;
            else StartCoroutine(AnimateCanvasGroupAlpha(group, 0, TransitionDuration));
        }
        private void LateUpdate()
        {
            // Keep already captured HUD hidden if another presenter enables it while a modal is open.
            if (!suppressingAdditionalHud) return;
            if (Time.unscaledTime >= nextWorldBarScan)
            {
                nextWorldBarScan = Time.unscaledTime + 0.25f;
                SetWorldBarsVisible(false);
            }
            foreach (var root in additionalHudRoots)
            {
                if (root == null || !root.gameObject.activeInHierarchy || IsModalRoot(root)) continue;
                var group = root.GetComponent<CanvasGroup>();
                if (group != null)
                {
                    group.interactable = false; group.blocksRaycasts = false;
                    if (Time.unscaledTime >= additionalHudHiddenAt) group.alpha = 0;
                }
            }
        }
        private void OnDisable()
        {
            StopAllCoroutines();
            SetAdditionalHudVisible(true, true);
            orbitCamera?.SetInputLocked(false);
        }
        private void SetWorldBarsVisible(bool visible)
        {
            if (visible)
            {
                foreach (var entry in worldBarStates)
                    if (entry.Key != null) entry.Key.forceRenderingOff = entry.Value;
                worldBarStates.Clear();
                return;
            }
            foreach (var health in FindObjectsByType<MiningCharacterHealth>(FindObjectsSortMode.None))
                HideWorldBar(health.HealthBar);
        }
        private void HideWorldBar(Transform root)
        {
            if (root == null) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!worldBarStates.ContainsKey(renderer)) worldBarStates.Add(renderer, renderer.forceRenderingOff);
                renderer.forceRenderingOff = true;
            }
        }
        private static void SetBasePanelImmediately(RectTransform panel, bool visible)
        {
            if (panel == null) return;
            panel.gameObject.SetActive(true);
            if (panel.TryGetComponent(out MiningUiSmoothFade fade))
            {
                if (visible) fade.CompleteImmediately();
                else fade.HideImmediately();
            }
            else SetCanvasAlpha(panel, visible ? 1f : 0f, visible);
        }

        private static void HideModalImmediately(RectTransform panel)
        {
            if (panel == null) return;
            SetCanvasAlpha(panel, 0f, false);
            panel.gameObject.SetActive(false);
        }

        private Vector2 GetHomePosition(RectTransform panel)
        {
            if (panel == upgradePanel) return upgradeHome;
            if (panel == rebirthPanel) return rebirthPanelHome;
            if (panel == audioSettingsPanel) return audioSettingsHome;
            if (panel == inventoryPanel) return inventoryPanelHome;
            if (panel == npcProgressHud) return npcProgressHome;
            if (panel != null && additionalPanelHomes.TryGetValue(panel, out Vector2 home))
                return home;
            return GetPosition(panel);
        }

        private void CacheAdditionalPanelHome(RectTransform panel)
        {
            if (panel == null || panel == upgradePanel || panel == rebirthPanel ||
                panel == audioSettingsPanel || panel == inventoryPanel ||
                additionalPanelHomes.ContainsKey(panel))
            {
                return;
            }

            additionalPanelHomes.Add(panel, panel.anchoredPosition);
        }

        private static Vector2 GetPosition(RectTransform panel)
        {
            return panel != null ? panel.anchoredPosition : Vector2.zero;
        }

        private static CanvasGroup GetCanvasGroup(RectTransform panel)
        {
            return panel.GetComponent<CanvasGroup>() ?? panel.gameObject.AddComponent<CanvasGroup>();
        }

        private static void SetCanvasAlpha(RectTransform panel, float alpha, bool interactive)
        {
            if (panel == null) return;
            CanvasGroup group = GetCanvasGroup(panel);
            group.alpha = alpha;
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }

        private static void SetInteraction(RectTransform panel, bool interactive)
        {
            if (panel == null) return;
            CanvasGroup group = GetCanvasGroup(panel);
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }

        private void ShowBackdrop(RectTransform panel)
        {
            EnsureBackdrop(panel);
            if (backdropRect == null)
            {
                return;
            }

            backdropRect.gameObject.SetActive(true);
            backdropGroup.blocksRaycasts = uiData == null || uiData.ModalBackdropBlocksClicks;
            if (backdropRoutine != null)
            {
                StopCoroutine(backdropRoutine);
            }
            float targetAlpha = uiData != null ? uiData.ModalBackdropColor.a : 0.6f;
            backdropRoutine = StartCoroutine(
                AnimateCanvasGroupAlpha(backdropGroup, targetAlpha, BackdropFadeDuration));
        }

        private float BackdropFadeDuration => uiData != null && uiData.ModalBackdropFadeDuration > 0f
            ? uiData.ModalBackdropFadeDuration
            : TransitionDuration;

        private void HideBackdrop()
        {
            if (backdropGroup == null)
            {
                return;
            }

            backdropGroup.blocksRaycasts = false;
            if (backdropRoutine != null)
            {
                StopCoroutine(backdropRoutine);
            }
            backdropRoutine = StartCoroutine(AnimateCanvasGroupAlpha(backdropGroup, 0f, BackdropFadeDuration,
                () => backdropRect.gameObject.SetActive(false)));
        }

        /// <summary>Builds a full-screen dim overlay once, lazily, so opening any modal panel
        /// (Shop, Upgrade, Rebirth, Inventory, Audio Settings...) visibly covers the 3D scene
        /// behind it instead of leaving gameplay fully visible/clickable underneath.</summary>
        private void EnsureBackdrop(RectTransform panel)
        {
            if (backdropRect != null)
            {
                return;
            }

            Canvas canvas = panel != null ? panel.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
            {
                return;
            }

            GameObject backdropObject = new("Modal Backdrop", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
            backdropRect = (RectTransform)backdropObject.transform;
            backdropRect.SetParent(canvas.transform, false);
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;

            UnityEngine.UI.Image image = backdropObject.GetComponent<UnityEngine.UI.Image>();
            image.color = uiData != null ? uiData.ModalBackdropColor : new Color(0f, 0f, 0f, 0.6f);
            image.raycastTarget = uiData == null || uiData.ModalBackdropBlocksClicks;

            backdropGroup = backdropObject.GetComponent<CanvasGroup>();
            backdropGroup.alpha = 0f;
            backdropGroup.blocksRaycasts = false;
            backdropGroup.interactable = false;
            backdropRect.gameObject.SetActive(false);
            // Always the very first child of the canvas, so it renders behind every other UI
            // element in it (base HUD, any modal) permanently — no per-open reordering needed.
            backdropRect.SetAsFirstSibling();
        }

        private static IEnumerator AnimateCanvasGroupAlpha(CanvasGroup group, float target,
            float duration, Action completed = null)
        {
            if (group == null)
            {
                yield break;
            }

            float start = group.alpha;
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.SmoothStep(start, target, Mathf.Clamp01(elapsed / safeDuration));
                yield return null;
            }

            group.alpha = target;
            completed?.Invoke();
        }

    }
}
