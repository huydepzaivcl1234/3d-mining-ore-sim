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

        private Vector2 shopHome;
        private Vector2 rebirthHome;
        private Vector2 audioMenuHome;
        private Vector2 upgradeHome;
        private Vector2 rebirthPanelHome;
        private Vector2 audioSettingsHome;
        private Vector2 inventoryMenuHome;
        private Vector2 inventoryPanelHome;
        private Vector2 effectToastHome;

        private Vector2 npcProgressHome;
        private RectTransform activeModal;
        private bool initialized;
        private readonly Dictionary<RectTransform, Vector2> additionalPanelHomes = new();

        public MiningUiData UiData => uiData;

        public void RegisterInventoryUi(RectTransform menuButton, RectTransform panel)
        {
            inventoryMenuButton = menuButton;
            inventoryPanel = panel;
            inventoryMenuHome = GetPosition(menuButton);
            inventoryPanelHome = GetPosition(panel);
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
        private Vector2 EffectToastSlideDirection => Vector2.left;
        private Vector2 ModalSlideDirection => GetDirection(
            uiData != null ? uiData.ModalSlideDirection : Vector2.down, Vector2.down);

        private void Awake()
        {
            CacheHomePositions();
        }

        private void Start()
        {
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
            initialized = true;
        }

        private void EnsureInitialized()
        {
            if (!initialized)
            {
                CacheHomePositions();
            }
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
