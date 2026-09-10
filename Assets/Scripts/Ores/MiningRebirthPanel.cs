using System;
using System.Collections;
using Microlight.MicroBar;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Event-driven HUD and confirmation panel for rebirth progression.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningRebirthPanel : MonoBehaviour
    {
        private const string LegacyWarning =
            "CẢNH BÁO!\n\nBạn sắp Rebirth! Toàn bộ tiền và mọi nâng cấp hiện tại sẽ bị xóa.";
        private const string CompleteWarning =
            "CẢNH BÁO!\n\nRebirth sẽ xóa tiền, mọi nâng cấp, cấp thợ mỏ và toàn bộ NPC trên sân.";

        [SerializeField] private MiningRebirthSystem rebirthSystem;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private MiningUiPanelCoordinator panelCoordinator;
        [SerializeField] private Button openButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI progressLabel;
        [SerializeField] private TextMeshProUGUI boostLabel;
        [SerializeField] private TextMeshProUGUI warningLabel;
        [SerializeField] private TextMeshProUGUI nextBoostLabel;
        [SerializeField] private MicroBar progressBar;
        [SerializeField] private MiningUiData uiData;
        [SerializeField] private Image rebirthFlashImage;

        [Header("Editable Text")]
        [SerializeField] private string progressFormat = "{0:N0} / {1:N0} TIỀN";
        [SerializeField] private string boostFormat = "REBIRTH {0}  •  x{1:0.00} TIỀN";
        [SerializeField] private string warningText =
            "CẢNH BÁO!\n\nRebirth sẽ xóa tiền, mọi nâng cấp, cấp thợ mỏ và toàn bộ NPC trên sân.";
        [SerializeField] private string nextBoostFormat = "Boost vĩnh viễn sau Rebirth: x{0:0.00} tiền";

        private bool barInitialized;
        private int initializedRequirement;
        private Coroutine rebirthSequence;
        private bool isRebirthing;

        public event Action PanelOpened;
        public event Action PanelClosed;

        private void Awake()
        {
            if (uiData == null && panelCoordinator != null)
            {
                uiData = panelCoordinator.UiData;
            }
            EnsureFlashImage();
            SetFlashAlpha(0f, false);
            confirmationPanel?.SetActive(false);
        }

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
            MiningLocalization.LanguageChanged += HandleLanguageChanged;
            AddListeners();
            if (rebirthSystem != null)
            {
                rebirthSystem.StateChanged -= Refresh;
                rebirthSystem.StateChanged += Refresh;
                rebirthSystem.RebirthCompleted -= HandleRebirthCompleted;
                rebirthSystem.RebirthCompleted += HandleRebirthCompleted;
            }
            Refresh();
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
            if (rebirthSequence != null)
            {
                StopCoroutine(rebirthSequence);
                rebirthSequence = null;
            }
            isRebirthing = false;
            SetFlashAlpha(0f, false);
            RemoveListeners();
            if (rebirthSystem != null)
            {
                rebirthSystem.StateChanged -= Refresh;
                rebirthSystem.RebirthCompleted -= HandleRebirthCompleted;
            }
        }

        private void AddListeners()
        {
            openButton?.onClick.RemoveListener(OpenPanel);
            openButton?.onClick.AddListener(OpenPanel);
            confirmButton?.onClick.RemoveListener(ConfirmRebirth);
            confirmButton?.onClick.AddListener(ConfirmRebirth);
            cancelButton?.onClick.RemoveListener(ClosePanel);
            cancelButton?.onClick.AddListener(ClosePanel);
        }

        private void RemoveListeners()
        {
            openButton?.onClick.RemoveListener(OpenPanel);
            confirmButton?.onClick.RemoveListener(ConfirmRebirth);
            cancelButton?.onClick.RemoveListener(ClosePanel);
        }

        private void OpenPanel()
        {
            if (panelCoordinator != null)
            {
                panelCoordinator.OpenPanel(confirmationPanel != null
                    ? confirmationPanel.GetComponent<RectTransform>()
                    : null);
            }
            else
            {
                confirmationPanel?.SetActive(true);
            }
            Refresh();
            PanelOpened?.Invoke();
        }

        private void ClosePanel()
        {
            if (panelCoordinator != null)
            {
                panelCoordinator.ClosePanel(confirmationPanel != null
                    ? confirmationPanel.GetComponent<RectTransform>()
                    : null);
            }
            else
            {
                confirmationPanel?.SetActive(false);
            }
            PanelClosed?.Invoke();
        }

        private void ConfirmRebirth()
        {
            if (isRebirthing || rebirthSystem == null || !rebirthSystem.CanRebirth)
            {
                Refresh();
                return;
            }

            rebirthSequence = StartCoroutine(PlayRebirthSequence());
        }

        private IEnumerator PlayRebirthSequence()
        {
            isRebirthing = true;
            if (confirmButton != null)
            {
                confirmButton.interactable = false;
            }

            ClosePanel();
            EnsureFlashImage();
            if (rebirthFlashImage != null)
            {
                rebirthFlashImage.transform.SetAsLastSibling();
            }

            Color flashColor = uiData != null
                ? uiData.RebirthFlashColor
                : new Color(1f, 1f, 1f, 1f);
            yield return FadeFlash(0f, flashColor.a, uiData != null
                ? uiData.RebirthFlashFadeInDuration
                : 0.18f, flashColor);

            float holdDuration = uiData != null ? uiData.RebirthFlashHoldDuration : 0.08f;
            if (holdDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(holdDuration);
            }

            // Reset only while the screen is fully covered by the flash.
            rebirthSystem.TryRebirth();
            yield return null;

            yield return FadeFlash(flashColor.a, 0f, uiData != null
                ? uiData.RebirthFlashFadeOutDuration
                : 0.35f, flashColor);

            SetFlashAlpha(0f, false);
            isRebirthing = false;
            rebirthSequence = null;
            Refresh();
        }

        private IEnumerator FadeFlash(float from, float to, float duration, Color baseColor)
        {
            if (rebirthFlashImage == null)
            {
                yield break;
            }

            rebirthFlashImage.raycastTarget = true;
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;
            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetFlashColor(baseColor, Mathf.Lerp(from, to,
                    Mathf.Clamp01(elapsed / safeDuration)));
                yield return null;
            }
            SetFlashColor(baseColor, to);
        }

        private void EnsureFlashImage()
        {
            if (rebirthFlashImage != null)
            {
                return;
            }

            Canvas canvas = confirmationPanel != null
                ? confirmationPanel.GetComponentInParent<Canvas>()
                : GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                return;
            }

            Transform existing = canvas.transform.Find("Rebirth Flash");
            if (existing == null)
            {
                GameObject flashObject = new("Rebirth Flash", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                flashObject.transform.SetParent(canvas.transform, false);
                RectTransform rect = flashObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                existing = flashObject.transform;
            }

            rebirthFlashImage = existing.GetComponent<Image>();
            if (rebirthFlashImage == null)
            {
                rebirthFlashImage = existing.gameObject.AddComponent<Image>();
            }
        }

        private void SetFlashAlpha(float alpha, bool blocksInput)
        {
            Color color = uiData != null ? uiData.RebirthFlashColor : Color.white;
            SetFlashColor(color, alpha);
            if (rebirthFlashImage != null)
            {
                rebirthFlashImage.raycastTarget = blocksInput;
            }
        }

        private void SetFlashColor(Color baseColor, float alpha)
        {
            if (rebirthFlashImage != null)
            {
                baseColor.a = Mathf.Clamp01(alpha);
                rebirthFlashImage.color = baseColor;
            }
        }

        private void HandleRebirthCompleted(int count)
        {
            Refresh();
        }

        private void Refresh()
        {
            float money = wallet != null ? wallet.CurrentMoney : 0f;
            int requirement = rebirthSystem != null ? rebirthSystem.CurrentRequirement : 1;

            if (progressLabel != null)
            {
                progressLabel.text = string.Format(MiningLocalization.Text(
                        "{0:N0} / {1:N0} MONEY", progressFormat),
                    MiningMoneyFormatter.Format(money),
                    MiningMoneyFormatter.Format(requirement));
            }
            if (boostLabel != null)
            {
                int count = rebirthSystem != null ? rebirthSystem.CompletedRebirths : 0;
                float multiplier = rebirthSystem != null ? rebirthSystem.PermanentMoneyMultiplier : 1f;
                boostLabel.text = string.Format(MiningLocalization.Text(
                    "REBIRTH {0}  •  x{1:0.00} MONEY", boostFormat), count, multiplier);
            }
            if (warningLabel != null)
            {
                warningLabel.text = MiningLocalization.IsEnglish
                    ? "WARNING!\n\nRebirth resets money, every upgrade, miner level, and all NPCs on the field."
                    : warningText == LegacyWarning ? CompleteWarning : warningText;
            }
            if (nextBoostLabel != null)
            {
                float nextMultiplier = rebirthSystem != null ? rebirthSystem.NextMoneyMultiplier : 1f;
                nextBoostLabel.text = string.Format(MiningLocalization.Text(
                    "Permanent boost after Rebirth: x{0:0.00} money", nextBoostFormat),
                    nextMultiplier);
            }
            if (confirmButton != null)
            {
                confirmButton.interactable = !isRebirthing && rebirthSystem != null &&
                                             rebirthSystem.CanRebirth;
            }

            RefreshProgressBar(money, requirement);
        }

        private void HandleLanguageChanged()
        {
            Refresh();
        }

        private void RefreshProgressBar(float current, int maximum)
        {
            if (progressBar == null)
            {
                return;
            }

            int safeMaximum = Mathf.Max(1, maximum);
            if (!barInitialized)
            {
                progressBar.Initialize(safeMaximum);
                initializedRequirement = safeMaximum;
                barInitialized = true;
            }
            else if (initializedRequirement != safeMaximum)
            {
                initializedRequirement = safeMaximum;
                progressBar.SetNewMaxHP(safeMaximum, true);
            }

            progressBar.UpdateBar(Mathf.Clamp(current, 0, safeMaximum));
        }
    }
}
