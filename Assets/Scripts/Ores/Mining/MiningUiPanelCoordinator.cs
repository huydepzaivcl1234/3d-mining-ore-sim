using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Coordinates exclusive mining modals and smooth HUD slide transitions.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningUiPanelCoordinator : MonoBehaviour
    {
        [SerializeField] private MiningUiData uiData;
        [SerializeField] private RectTransform shopPanel;
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

        private Vector2 shopHome;
        private Vector2 rebirthHome;
        private Vector2 audioMenuHome;
        private Vector2 upgradeHome;
        private Vector2 rebirthPanelHome;
        private Vector2 audioSettingsHome;
        private Vector2 inventoryMenuHome;
        private Vector2 inventoryPanelHome;
        private Vector2 effectToastHome;
        private Vector2 gemHudHome;
        private Vector2 shopMenuButtonHome;
        private Vector2 questMenuButtonHome;

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
            inventoryMenuHome = GetPosition(menuButton);
            inventoryPanelHome = GetPosition(panel);
        }

        /// <summary>Registers optional HUD added by the Gem and Shop setup tools.</summary>
        public void RegisterGemAndShopUi(RectTransform gem, RectTransform shopButton)
        {
            if (gem != null)
            {
                gemHud = gem;
                gemHudHome = GetPosition(gemHud);
            }
            if (shopButton != null)
            {
                shopMenuButton = shopButton;
                shopMenuButtonHome = GetPosition(shopMenuButton);
            }
        }

        public void RegisterQuestUi(RectTransform questButton)
        {
            if (questButton == null) return;
            questMenuButton = questButton;
            questMenuButtonHome = GetPosition(questMenuButton);
        }

        private float TransitionDuration => uiData != null ? uiData.PanelTransitionDuration : 0.28f;
        private float SlideExtraDistance => uiData != null ? uiData.PanelSlideExtraDistance : 80f;
        private Vector2 ShopSlideDirection => GetDirection(
            uiData != null ? uiData.ShopSlideDirection : Vector2.left, Vector2.left);
        private Vector2 RebirthHudSlideDirection => GetDirection(
            uiData != null ? uiData.RebirthHudSlideDirection : Vector2.up, Vector2.up);
        private Vector2 AudioMenuSlideDirection => GetDirection(
            uiData != null ? uiData.AudioMenuSlideDirection : Vector2.right, Vector2.right);
        private Vector2 InventoryMenuSlideDirection => GetDirection(
            uiData != null ? uiData.InventoryMenuSlideDirection : Vector2.right, Vector2.right);
        private Vector2 NpcProgressHudSlideDirection => GetDirection(
            uiData != null ? uiData.NpcProgressHudSlideDirection : Vector2.up, Vector2.up);
        private Vector2 GemHudSlideDirection => GetDirection(
            uiData != null ? uiData.GemHudSlideDirection : Vector2.up, Vector2.up);
        private Vector2 ShopMenuButtonSlideDirection => GetDirection(
            uiData != null ? uiData.ShopMenuButtonSlideDirection : Vector2.right, Vector2.right);
        private Vector2 EffectToastSlideDirection => Vector2.left;
        private Vector2 ModalSlideDirection => GetDirection(
            uiData != null ? uiData.ModalSlideDirection : Vector2.down, Vector2.down);

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
                SetInteraction(activeModal, true);
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
            panel.anchoredPosition = GetModalHiddenPosition(panel);
            SetInteraction(panel, true);
            AnimateBasePanels(false);
            orbitCamera?.SetInputLocked(true);
            ShowBackdrop(panel);
            StartCoroutine(AnimateRect(panel, GetHomePosition(panel), TransitionDuration));
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
            StartCoroutine(AnimateRect(panel, GetModalHiddenPosition(panel), TransitionDuration, () =>
            {
                if (activeModal == panel)
                {
                    activeModal = null;
                }
                panel.gameObject.SetActive(false);
            }));
        }

        private void CacheHomePositions()
        {
            if (initialized)
            {
                return;
            }

            shopHome = GetPosition(shopPanel);
            rebirthHome = GetPosition(rebirthHud);
            audioMenuHome = GetPosition(audioMenuButton);
            upgradeHome = GetPosition(upgradePanel);
            rebirthPanelHome = GetPosition(rebirthPanel);
            audioSettingsHome = GetPosition(audioSettingsPanel);
            inventoryMenuHome = GetPosition(inventoryMenuButton);
            inventoryPanelHome = GetPosition(inventoryPanel);
            effectToastHome = GetPosition(effectToast);
            npcProgressHome = GetPosition(npcProgressHud);
            gemHudHome = GetPosition(gemHud);
            shopMenuButtonHome = GetPosition(shopMenuButton);
            questMenuButtonHome = GetPosition(questMenuButton);
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
            AnimateBasePanel(shopPanel, shopHome, ShopSlideDirection, visible);
            AnimateBasePanel(rebirthHud, rebirthHome, RebirthHudSlideDirection, visible);
            AnimateBasePanel(audioMenuButton, audioMenuHome, AudioMenuSlideDirection, visible);
            AnimateBasePanel(npcProgressHud, npcProgressHome, NpcProgressHudSlideDirection, visible);
            AnimateBasePanel(inventoryMenuButton, inventoryMenuHome, InventoryMenuSlideDirection,
                visible);
            AnimateBasePanel(effectToast, effectToastHome, EffectToastSlideDirection, visible);
            AnimateBasePanel(gemHud, gemHudHome, GemHudSlideDirection, visible);
            AnimateBasePanel(shopMenuButton, shopMenuButtonHome, ShopMenuButtonSlideDirection,
                visible);
            AnimateBasePanel(questMenuButton, questMenuButtonHome, ShopMenuButtonSlideDirection,
                visible);
        }

        private void AnimateBasePanel(RectTransform panel, Vector2 home, Vector2 direction,
            bool visible)
        {
            if (panel == null)
            {
                return;
            }

            panel.gameObject.SetActive(true);
            SetInteraction(panel, visible);
            float panelDistance = Mathf.Abs(direction.x) * panel.rect.width +
                                  Mathf.Abs(direction.y) * panel.rect.height;
            Vector2 hidden = home + direction * (panelDistance + SlideExtraDistance);
            StartCoroutine(AnimateRect(panel, visible ? home : hidden, TransitionDuration));
        }

        private void SetBasePanelsImmediately(bool visible)
        {
            SetBasePanelImmediately(shopPanel, shopHome, ShopSlideDirection, visible);
            SetBasePanelImmediately(rebirthHud, rebirthHome, RebirthHudSlideDirection, visible);
            SetBasePanelImmediately(audioMenuButton, audioMenuHome, AudioMenuSlideDirection, visible);
            SetBasePanelImmediately(npcProgressHud, npcProgressHome,
                NpcProgressHudSlideDirection, visible);
            SetBasePanelImmediately(inventoryMenuButton, inventoryMenuHome,
                InventoryMenuSlideDirection, visible);
            SetBasePanelImmediately(effectToast, effectToastHome, EffectToastSlideDirection,
                visible);
            SetBasePanelImmediately(gemHud, gemHudHome, GemHudSlideDirection, visible);
            SetBasePanelImmediately(shopMenuButton, shopMenuButtonHome,
                ShopMenuButtonSlideDirection, visible);
            SetBasePanelImmediately(questMenuButton, questMenuButtonHome,
                ShopMenuButtonSlideDirection, visible);
        }

        private void SetBasePanelImmediately(RectTransform panel, Vector2 home, Vector2 direction,
            bool visible)
        {
            if (panel == null)
            {
                return;
            }

            panel.gameObject.SetActive(true);
            float panelDistance = Mathf.Abs(direction.x) * panel.rect.width +
                                  Mathf.Abs(direction.y) * panel.rect.height;
            panel.anchoredPosition = visible
                ? home
                : home + direction * (panelDistance + SlideExtraDistance);
            SetInteraction(panel, visible);
        }

        private void HideModalImmediately(RectTransform panel)
        {
            if (panel == null)
            {
                return;
            }

            panel.anchoredPosition = GetModalHiddenPosition(panel);
            SetInteraction(panel, false);
            panel.gameObject.SetActive(false);
        }

        private Vector2 GetModalHiddenPosition(RectTransform panel)
        {
            RectTransform canvasRect = panel != null
                ? panel.GetComponentInParent<Canvas>()?.transform as RectTransform
                : null;
            float canvasWidth = canvasRect != null ? canvasRect.rect.width : 1920f;
            float canvasHeight = canvasRect != null ? canvasRect.rect.height : 1080f;
            float horizontalDistance = canvasWidth * 0.5f + panel.rect.width * 0.5f;
            float verticalDistance = canvasHeight * 0.5f + panel.rect.height * 0.5f;
            float distance = Mathf.Abs(ModalSlideDirection.x) * horizontalDistance +
                             Mathf.Abs(ModalSlideDirection.y) * verticalDistance;
            return GetHomePosition(panel) +
                   ModalSlideDirection * (distance + SlideExtraDistance);
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

        private static Vector2 GetDirection(Vector2 configured, Vector2 fallback)
        {
            return configured.sqrMagnitude > 0.0001f ? configured.normalized : fallback;
        }

        private static void SetInteraction(RectTransform panel, bool enabled)
        {
            if (panel == null)
            {
                return;
            }

            CanvasGroup group = panel.GetComponent<CanvasGroup>() ??
                                panel.gameObject.AddComponent<CanvasGroup>();
            group.interactable = enabled;
            group.blocksRaycasts = enabled;
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
                group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / safeDuration));
                yield return null;
            }

            group.alpha = target;
            completed?.Invoke();
        }

        private static IEnumerator AnimateRect(RectTransform panel, Vector2 destination,
            float duration, Action completed = null)
        {
            if (panel == null)
            {
                yield break;
            }

            Vector2 start = panel.anchoredPosition;
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / safeDuration);
                float eased = 1f - Mathf.Pow(1f - normalized, 3f);
                panel.anchoredPosition = Vector2.LerpUnclamped(start, destination, eased);
                yield return null;
            }

            panel.anchoredPosition = destination;
            completed?.Invoke();
        }
    }
}