using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Keeps an edge-anchored HUD element inside Android/iOS display cutouts.</summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class MiningSafeAreaInset : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private bool protectLeft;
        [SerializeField] private bool protectRight;
        [SerializeField] private bool protectTop;
        [SerializeField] private bool protectBottom;
        [SerializeField] private float extraPadding = 8f;
        [SerializeField] private Vector2 authoredPosition;
        [SerializeField] private bool hasAuthoredPosition;

        private Rect lastSafeArea;
        private int lastScreenWidth;
        private int lastScreenHeight;

        public void Configure(RectTransform rect, float padding)
        {
            bool changedTarget = target != rect;
            target = rect;
            extraPadding = Mathf.Max(0f, padding);
            protectLeft = rect != null && rect.anchorMax.x <= 0.5f;
            protectRight = rect != null && rect.anchorMin.x >= 0.5f;
            protectBottom = rect != null && rect.anchorMax.y <= 0.5f;
            protectTop = rect != null && rect.anchorMin.y >= 0.5f;
            Rect safe = Screen.safeArea;
            bool fullScreenSafeArea = Mathf.Approximately(safe.xMin, 0f) &&
                                      Mathf.Approximately(safe.yMin, 0f) &&
                                      Mathf.Approximately(safe.xMax, Screen.width) &&
                                      Mathf.Approximately(safe.yMax, Screen.height);
            if (rect != null && (!hasAuthoredPosition || changedTarget || fullScreenSafeArea))
            {
                authoredPosition = rect.anchoredPosition;
                hasAuthoredPosition = true;
            }
            Apply();
        }

        private void OnEnable()
        {
            if (target == null)
            {
                target = transform as RectTransform;
            }
            if (!hasAuthoredPosition && target != null)
            {
                authoredPosition = target.anchoredPosition;
                hasAuthoredPosition = true;
            }
            Apply();
        }

        private void Update()
        {
            if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height ||
                lastSafeArea != Screen.safeArea)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (target == null || !hasAuthoredPosition || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Canvas canvas = target.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null || canvasRect.rect.width <= 0f || canvasRect.rect.height <= 0f)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            float scaleX = canvasRect.rect.width / Screen.width;
            float scaleY = canvasRect.rect.height / Screen.height;
            float left = safeArea.xMin * scaleX;
            float right = (Screen.width - safeArea.xMax) * scaleX;
            float bottom = safeArea.yMin * scaleY;
            float top = (Screen.height - safeArea.yMax) * scaleY;
            Vector2 offset = Vector2.zero;
            if (protectLeft && left > 0.01f) offset.x += left + extraPadding;
            if (protectRight && right > 0.01f) offset.x -= right + extraPadding;
            if (protectBottom && bottom > 0.01f) offset.y += bottom + extraPadding;
            if (protectTop && top > 0.01f) offset.y -= top + extraPadding;
            target.anchoredPosition = authoredPosition + offset;

            lastSafeArea = safeArea;
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }
    }
}
