using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Scene-authored TMP prompt that follows the hovered world target.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningInteractionPrompt : MonoBehaviour
    {
        [SerializeField] private Canvas ownerCanvas;
        [SerializeField] private RectTransform promptRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI promptLabel;

        private string displayedText;

        private void Awake()
        {
            ResolveReferences();
            HideImmediate();
        }

        public void Configure(Canvas canvas, RectTransform root, CanvasGroup group,
            TextMeshProUGUI label)
        {
            ownerCanvas = canvas;
            promptRoot = root;
            canvasGroup = group;
            promptLabel = label;
        }

        public void Show(string text, Vector2 screenPosition, Vector2 cursorOffset)
        {
            ResolveReferences();
            if (ownerCanvas == null || promptRoot == null || canvasGroup == null)
            {
                return;
            }

            if (promptLabel != null && displayedText != text)
            {
                displayedText = text;
                promptLabel.text = text;
            }

            PositionAt(screenPosition, cursorOffset);
            canvasGroup.alpha = 1f;
        }

        public void HideImmediate()
        {
            ResolveReferences();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void ShowEditorPreview(string text)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                return;
            }

            ResolveReferences();
            if (promptLabel != null)
            {
                promptLabel.text = text;
                displayedText = text;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
#endif
        }

        private void PositionAt(Vector2 screenPosition, Vector2 offset)
        {
            RectTransform canvasRect = ownerCanvas.transform as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            Camera uiCamera = ownerCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : ownerCanvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
                    screenPosition, uiCamera, out Vector2 localPoint))
            {
                return;
            }

            Vector2 desired = localPoint + offset;
            Rect canvasBounds = canvasRect.rect;
            float width = promptRoot.rect.width;
            float height = promptRoot.rect.height;
            desired.x = Mathf.Clamp(desired.x, canvasBounds.xMin,
                canvasBounds.xMax - width);
            desired.y = Mathf.Clamp(desired.y, canvasBounds.yMin + height,
                canvasBounds.yMax);
            promptRoot.anchoredPosition = desired;
        }

        private void ResolveReferences()
        {
            if (ownerCanvas == null) ownerCanvas = GetComponentInParent<Canvas>();
            if (promptRoot == null) promptRoot = transform as RectTransform;
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (promptLabel == null)
                promptLabel = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }
}
