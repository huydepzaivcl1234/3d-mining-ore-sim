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
        private MiningLavaWorldController lavaWorld;
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

        public void Show(MiningDynamicRadialMaskTransition preview, MiningLavaWorldController world = null)
        {
            if (preview == null) return;
            lavaWorld = world;
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
            if (lavaWorld != null && !lavaWorld.CanTravel(out _, out _))
            {
                RefreshText();
                return;
            }
            MiningDynamicRadialMaskTransition preview = transition;
            MiningLavaWorldController world = lavaWorld;
            Hide();
            if (world != null) world.Travel();
            else if (preview != null) preview.PlayPreview();
        }

        public void Hide()
        {
            wasOpened = false;
            transition = null;
            lavaWorld = null;
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            gameObject.SetActive(false);
        }

        private void RefreshText()
        {
            string englishLock = null;
            string vietnameseLock = null;
            bool locked = lavaWorld != null && !lavaWorld.CanTravel(
                out englishLock, out vietnameseLock);
            if (titleText != null) titleText.text = lavaWorld != null
                ? MiningLocalization.Text(lavaWorld.IsInLavaWorld ? "GROUND PORTAL" : "LAVA WORLD PORTAL",
                    lavaWorld.IsInLavaWorld ? "CỔNG VỀ MẶT ĐẤT" : "CỔNG THẾ GIỚI DUNG NHAM")
                : MiningLocalization.Text(englishTitle, vietnameseTitle);
            if (messageText != null) messageText.text = lavaWorld != null
                ? locked ? MiningLocalization.Text(englishLock, vietnameseLock)
                : MiningLocalization.Text(lavaWorld.IsInLavaWorld
                    ? "Return to Ground? Your miners and progress stay with you."
                    : "Enter Lava World? The ground stays; trees and the ore table change.",
                    lavaWorld.IsInLavaWorld
                    ? "Về mặt đất? Thợ mỏ và tiến trình vẫn được giữ."
                    : "Đến thế giới dung nham? Giữ nền đất, ẩn cây và đổi bảng quặng.")
                : MiningLocalization.Text(englishMessage, vietnameseMessage);
            if (enterLabel != null) enterLabel.text = MiningLocalization.Text(
                locked ? "LOCKED" : englishEnter, locked ? "CHƯA MỞ" : vietnameseEnter);
            if (enterButton != null) enterButton.interactable = !locked;
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
