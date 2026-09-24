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

        public MiningUiData UiData => uiData;

        public void RegisterInventoryUi(RectTransform menuButton, RectTransform panel)
        {
            inventoryMenuButton = menuButton;
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

        public void RegisterQuestUi(RectTransform questButton)
        {
            if (questButton == null) return;
            questMenuButton = questButton;
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
            activeModal = FindActiveModal();
            if (activeModal != null)
            {
                SetBasePanelsImmediately(false);
                activeModal.anchoredPosition = GetHomePosition(activeModal);
                SetCanvasAlpha(activeModal, 1f, true);
            }
            else
            {
                SetBasePanelsImmediately(true);
                HideModalImmediately(upgradePanel);
                HideModalImmediately(rebirthPanel);
                HideModalImmediately(audioSettingsPanel);
                HideModalImmediately(inventoryPanel);
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
            if (activeModal != null && activeModal != panel)
            {
                HideModalImmediately(activeModal);
            }

            activeModal = panel;
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
            StopAllCoroutines();
            SetInteraction(panel, false);
            AnimateBasePanels(true);
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
                HideModalImmediately(upgradePanel);
                HideModalImmediately(rebirthPanel);
                HideModalImmediately(audioSettingsPanel);
                HideModalImmediately(inventoryPanel);
                activeModal = null;
                orbitCamera?.SetInputLocked(false);
                HideBackdrop();
            }
            AnimateBasePanels(visible);
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
            // The two shop buttons can be scene-authored outside the compact status panel.
            if (pcQuickActions == null)
            {
                Transform quickCanvas = transform.Find("Mining HUD Canvas");
                if (quickCanvas != null)
                    pcQuickActions = quickCanvas.Find("PC Quick Actions") as RectTransform;
            }
            if (gemHud != null && shopMenuButton != null && questMenuButton != null)
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
            return null;
        }

        private void AnimateBasePanels(bool visible)
        {
            FadeBasePanel(shopPanel, visible);
            FadeBasePanel(pcQuickActions, visible);
            FadeBasePanel(rebirthHud, visible);
            FadeBasePanel(audioMenuButton, visible);
            FadeBasePanel(npcProgressHud, visible);
            FadeBasePanel(inventoryMenuButton, visible);
            FadeBasePanel(effectToast, visible);
            FadeBasePanel(gemHud, visible);
            FadeBasePanel(shopMenuButton, visible);
            FadeBasePanel(questMenuButton, visible);
        }

        private void FadeBasePanel(RectTransform panel, bool visible)
        {
            if (panel == null) return;
            panel.gameObject.SetActive(true);
            // The HUD must not receive a click while fading, including the first visible frame.
            SetInteraction(panel, false);
            CanvasGroup group = GetCanvasGroup(panel);
            StartCoroutine(AnimateCanvasGroupAlpha(group, visible ? 1f : 0f,
                TransitionDuration, () => { if (visible) SetInteraction(panel, true); }));
        }

        private void SetBasePanelsImmediately(bool visible)
        {
            SetBasePanelImmediately(shopPanel, visible);
            SetBasePanelImmediately(pcQuickActions, visible);
            SetBasePanelImmediately(rebirthHud, visible);
            SetBasePanelImmediately(audioMenuButton, visible);
            SetBasePanelImmediately(npcProgressHud, visible);
            SetBasePanelImmediately(inventoryMenuButton, visible);
            SetBasePanelImmediately(effectToast, visible);
            SetBasePanelImmediately(gemHud, visible);
            SetBasePanelImmediately(shopMenuButton, visible);
            SetBasePanelImmediately(questMenuButton, visible);
        }

        private static void SetBasePanelImmediately(RectTransform panel, bool visible)
        {
            if (panel == null) return;
            panel.gameObject.SetActive(true);
            SetCanvasAlpha(panel, visible ? 1f : 0f, visible);
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
