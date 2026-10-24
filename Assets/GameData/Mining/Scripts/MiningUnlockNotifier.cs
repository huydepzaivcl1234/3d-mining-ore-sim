using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Queues gameplay unlock notifications.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningUnlockNotifier : MonoBehaviour
    {
        [SerializeField] private MiningUiData uiData;

        private readonly Queue<string> pendingMessages = new();
        private RectTransform toastRect;
        private Image toastBackground;
        private CanvasGroup toastGroup;
        private TextMeshProUGUI toastLabel;
        private Sequence toastSequence;
        private Vector2 toastRestPosition;
        private Vector2 toastJumpFromPosition;



        private void Awake()
        {



            if (uiData == null)
            {
                MiningUiPanelCoordinator coordinator = FindFirstObjectByType<MiningUiPanelCoordinator>(
                    FindObjectsInactive.Include);
                uiData = coordinator != null ? coordinator.UiData : null;
            }
        }



        private void OnDisable()
        {

            if (toastSequence.isAlive)
            {
                toastSequence.Stop();
            }
            pendingMessages.Clear();
        }







        /// <summary>Queues a message through the existing unlock-toast presentation.</summary>
        public void ShowToast(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            EnsureToastUi();
            pendingMessages.Enqueue(message);
            if (!toastSequence.isAlive)
            {
                PlayNextToast();
            }
        }

        private void PlayNextToast()
        {
            if (pendingMessages.Count == 0)
            {
                return;
            }

            toastLabel.text = pendingMessages.Dequeue();

            if (toastSequence.isAlive)
            {
                toastSequence.Stop();
            }

            float fadeDuration = uiData != null ? uiData.UnlockToastFadeDuration : 0.25f;
            float holdDuration = uiData != null ? uiData.UnlockToastHoldDuration : 2.2f;

            toastRect.anchoredPosition = toastJumpFromPosition;
            toastRect.localScale = Vector3.one * 0.5f;
            toastGroup.alpha = 0f;

            // Entrance: the toast jumps up into place (Ease.OutBack overshoots past full size,
            // giving the "nhảy lên" pop) while it slides up and fades in together, then eases
            // back down to its normal scale — the zoom-settle. Exit mirrors it in reverse.
            // Everything runs on unscaled time so pausing/time-scale changes don't freeze it.
            toastSequence = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Custom(this, toastJumpFromPosition, toastRestPosition, fadeDuration,
                    static (toast, pos) => toast.toastRect.anchoredPosition = pos, Ease.OutCubic))
                .Group(Tween.Custom(this, 0f, 1f, fadeDuration,
                    static (toast, alpha) => toast.toastGroup.alpha = alpha, Ease.OutCubic))
                .Group(Tween.Scale(toastRect, Vector3.one, fadeDuration, Ease.OutBack))
                .ChainDelay(holdDuration)
                .Chain(Tween.Custom(this, toastRestPosition, toastJumpFromPosition, fadeDuration,
                    static (toast, pos) => toast.toastRect.anchoredPosition = pos, Ease.InCubic))
                .Group(Tween.Custom(this, 1f, 0f, fadeDuration,
                    static (toast, alpha) => toast.toastGroup.alpha = alpha, Ease.InCubic))
                .Group(Tween.Scale(toastRect, Vector3.one * 0.7f, fadeDuration, Ease.InCubic))
                .OnComplete(this, static toast => toast.PlayNextToast());
        }

        private void EnsureToastUi()
        {
            if (toastGroup != null)
            {
                return;
            }

            GameObject canvasObject = new("Unlock Toast Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = uiData != null ? uiData.CanvasSortingOrder + 50 : 150;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = uiData != null ? uiData.ReferenceResolution : new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = uiData != null ? uiData.MatchWidthOrHeight : 0.5f;

            GameObject panelObject = new("Unlock Toast Panel", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            panelObject.transform.SetParent(canvasObject.transform, false);
            toastRect = panelObject.GetComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 1f);
            toastRect.anchorMax = new Vector2(0.5f, 1f);
            toastRect.pivot = new Vector2(0.5f, 1f);
            toastRect.sizeDelta = uiData != null ? uiData.UnlockToastSize : new Vector2(460f, 64f);
            toastRestPosition = uiData != null ? uiData.UnlockToastPosition : new Vector2(0f, -140f);
            toastJumpFromPosition = toastRestPosition + new Vector2(0f, -36f);
            toastRect.anchoredPosition = toastJumpFromPosition;
            toastRect.localScale = Vector3.one * 0.5f;

            toastBackground = panelObject.GetComponent<Image>();
            toastBackground.color = uiData != null
                ? uiData.UnlockToastBackgroundColor
                : new Color(0.08f, 0.09f, 0.11f, 0.95f);
            toastBackground.raycastTarget = false;

            toastGroup = panelObject.GetComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            toastGroup.interactable = false;

            GameObject labelObject = new("Label", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(panelObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 6f);
            labelRect.offsetMax = new Vector2(-16f, -6f);
            toastLabel = labelObject.GetComponent<TextMeshProUGUI>();
            toastLabel.alignment = TextAlignmentOptions.Center;
            toastLabel.fontStyle = FontStyles.Bold;
            toastLabel.fontSize = uiData != null ? uiData.UnlockToastFontSize : 24f;
            toastLabel.color = uiData != null ? uiData.UnlockToastTextColor : new Color(1f, 0.86f, 0.32f, 1f);
            toastLabel.raycastTarget = false;
        }
    }
}
