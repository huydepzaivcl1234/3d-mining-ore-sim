using System;
using System.Collections;
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

        private Vector2 shopHome;
        private Vector2 rebirthHome;
        private Vector2 audioMenuHome;
        private Vector2 upgradeHome;
        private Vector2 rebirthPanelHome;
        private Vector2 audioSettingsHome;
        private RectTransform activeModal;
        private bool initialized;

        private float TransitionDuration => uiData != null ? uiData.PanelTransitionDuration : 0.28f;
        private float SlideExtraDistance => uiData != null ? uiData.PanelSlideExtraDistance : 80f;

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
            }
        }

        public void OpenPanel(RectTransform panel)
        {
            if (panel == null)
            {
                return;
            }

            EnsureInitialized();
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
            return null;
        }

        private void AnimateBasePanels(bool visible)
        {
            AnimateBasePanel(shopPanel, shopHome, Vector2.left, visible);
            AnimateBasePanel(rebirthHud, rebirthHome, Vector2.right, visible);
            AnimateBasePanel(audioMenuButton, audioMenuHome, Vector2.right, visible);
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
            Vector2 hidden = home + direction * (panel.rect.width + SlideExtraDistance);
            StartCoroutine(AnimateRect(panel, visible ? home : hidden, TransitionDuration));
        }

        private void SetBasePanelsImmediately(bool visible)
        {
            SetBasePanelImmediately(shopPanel, shopHome, Vector2.left, visible);
            SetBasePanelImmediately(rebirthHud, rebirthHome, Vector2.right, visible);
            SetBasePanelImmediately(audioMenuButton, audioMenuHome, Vector2.right, visible);
        }

        private void SetBasePanelImmediately(RectTransform panel, Vector2 home, Vector2 direction,
            bool visible)
        {
            if (panel == null)
            {
                return;
            }

            panel.gameObject.SetActive(true);
            panel.anchoredPosition = visible
                ? home
                : home + direction * (panel.rect.width + SlideExtraDistance);
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
            RectTransform canvasRect = panel != null ? panel.GetComponentInParent<Canvas>()?.transform as RectTransform : null;
            float canvasHeight = canvasRect != null ? canvasRect.rect.height : 1080f;
            return GetHomePosition(panel) + Vector2.down *
                   (canvasHeight * 0.5f + panel.rect.height * 0.5f + SlideExtraDistance);
        }

        private Vector2 GetHomePosition(RectTransform panel)
        {
            if (panel == upgradePanel) return upgradeHome;
            if (panel == rebirthPanel) return rebirthPanelHome;
            if (panel == audioSettingsPanel) return audioSettingsHome;
            return GetPosition(panel);
        }

        private static Vector2 GetPosition(RectTransform panel)
        {
            return panel != null ? panel.anchoredPosition : Vector2.zero;
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
