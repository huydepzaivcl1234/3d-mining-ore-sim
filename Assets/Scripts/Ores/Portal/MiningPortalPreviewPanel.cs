using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Scene-authored confirmation UI. Confirm only previews the existing radial animation.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningPortalPreviewPanel : MonoBehaviour
    {
        [Header("Scene UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text enterLabel;
        [SerializeField] private TMP_Text cancelLabel;
        [SerializeField] private Button enterButton;
        [SerializeField] private Button cancelButton;

        [Header("Editable text (Lean Localization)")]
        [SerializeField] private string englishTitle = "UNDERGROUND PORTAL";
        [SerializeField] private string vietnameseTitle = "CỔNG XUỐNG LÒNG ĐẤT";
        [TextArea, SerializeField] private string englishMessage =
            "Preview the portal transition? The Underground world is not available yet.";
        [TextArea, SerializeField] private string vietnameseMessage =
            "Xem thử hiệu ứng chuyển cổng? Thế giới lòng đất hiện chưa có.";
        [SerializeField] private string englishEnter = "ENTER";
        [SerializeField] private string vietnameseEnter = "VÀO CỔNG";
        [SerializeField] private string englishCancel = "CANCEL";
        [SerializeField] private string vietnameseCancel = "HỦY";

        private MiningDynamicRadialMaskTransition transition;
        private bool wasOpened;
        private int openedFrame = -1;

        public bool IsOpen => gameObject.activeInHierarchy && wasOpened;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            enterButton?.onClick.AddListener(Confirm);
            cancelButton?.onClick.AddListener(Hide);
            RefreshText();
        }

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged += RefreshText;
            RefreshText();
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= RefreshText;
            wasOpened = false;
        }

        private void OnDestroy()
        {
            enterButton?.onClick.RemoveListener(Confirm);
            cancelButton?.onClick.RemoveListener(Hide);
        }

        private void Start()
        {
            // The editor creates this active so every element remains visible in Scene view.
            if (!wasOpened) gameObject.SetActive(false);
        }

        private void Update()
        {
            // An Enter press that opens the panel must not also confirm it in this frame.
            if (!wasOpened || Time.frameCount == openedFrame) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) Hide();
            else if (keyboard.enterKey.wasPressedThisFrame ||
                     keyboard.numpadEnterKey.wasPressedThisFrame) Confirm();
        }

        public void Show(MiningDynamicRadialMaskTransition preview)
        {
            if (preview == null) return;
            transition = preview;
            wasOpened = true;
            openedFrame = Time.frameCount;
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            transform.SetAsLastSibling();
            RefreshText();
        }

        public void Confirm()
        {
            if (!IsOpen) return;
            MiningDynamicRadialMaskTransition preview = transition;
            Hide();
            if (preview != null) preview.PlayPreview();
        }

        public void Hide()
        {
            wasOpened = false;
            transition = null;
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            gameObject.SetActive(false);
        }

        private void RefreshText()
        {
            if (titleText != null) titleText.text = MiningLocalization.Text(
                englishTitle, vietnameseTitle);
            if (messageText != null) messageText.text = MiningLocalization.Text(
                englishMessage, vietnameseMessage);
            if (enterLabel != null) enterLabel.text = MiningLocalization.Text(
                englishEnter, vietnameseEnter);
            if (cancelLabel != null) cancelLabel.text = MiningLocalization.Text(
                englishCancel, vietnameseCancel);
        }

        /// <summary>Called once by the editor setup tool; all elements stay editable in the scene.</summary>
        public void Configure(CanvasGroup group, TMP_Text title, TMP_Text message,
            Button enter, TMP_Text enterText, Button cancel, TMP_Text cancelText)
        {
            canvasGroup = group;
            titleText = title;
            messageText = message;
            enterButton = enter;
            enterLabel = enterText;
            cancelButton = cancel;
            cancelLabel = cancelText;
            RefreshText();
        }
    }
}
