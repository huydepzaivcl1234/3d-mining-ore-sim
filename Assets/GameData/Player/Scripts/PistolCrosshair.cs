using UnityEngine;

namespace GameData.Player.Script
{
    [DisallowMultipleComponent]
    public sealed class PistolCrosshair : MonoBehaviour
    {
        [SerializeField] private PistolShooting presentation;
        [SerializeField] private Color color = Color.black;
        [SerializeField] private Color outlineColor = Color.white;
        [SerializeField, Min(1)] private float armLength = 14, thickness = 3, gap = 4, dotSize = 3;
        [SerializeField, Min(0)] private float outlineWidth = 1;
        private GameObject canvasRoot;
        private RectTransform group;
        private readonly UnityEngine.UI.Image[] fills = new UnityEngine.UI.Image[5];
        private readonly UnityEngine.UI.Image[] outlines = new UnityEngine.UI.Image[5];
        public bool Visible => group != null && group.gameObject.activeSelf;

        private void Awake()
        {
            if (!presentation) presentation = GetComponent<PistolShooting>();
            canvasRoot = new GameObject("Pistol Crosshair Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            var canvas = canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 500;
            var scaler = canvasRoot.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            group = new GameObject("Crosshair", typeof(RectTransform)).GetComponent<RectTransform>();
            group.SetParent(canvasRoot.transform, false); group.anchorMin = group.anchorMax = group.pivot = new Vector2(.5f,.5f); group.sizeDelta = Vector2.zero;
            for (int i = 0; i < 5; i++) outlines[i] = CreateImage("Outline " + i);
            for (int i = 0; i < 5; i++) fills[i] = CreateImage(i == 0 ? "Center dot" : "Arm " + i);
            ApplyStyle(); group.gameObject.SetActive(false);
        }
        private UnityEngine.UI.Image CreateImage(string name)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            image.transform.SetParent(group, false); image.raycastTarget = false;
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = new Vector2(.5f,.5f);
            return image;
        }
        private void ApplyStyle()
        {
            if (!group) return;
            float offset = gap + armLength*.5f;
            for (int i = 0; i < 5; i++)
            {
                Vector2 size = i == 0 ? Vector2.one * dotSize : i < 3 ? new Vector2(armLength, thickness) : new Vector2(thickness, armLength);
                Vector2 position = i == 1 ? Vector2.left * offset : i == 2 ? Vector2.right * offset : i == 3 ? Vector2.up * offset : i == 4 ? Vector2.down * offset : Vector2.zero;
                fills[i].rectTransform.anchoredPosition = outlines[i].rectTransform.anchoredPosition = position;
                fills[i].rectTransform.sizeDelta = size; fills[i].color = color;
                outlines[i].rectTransform.sizeDelta = size + Vector2.one * outlineWidth * 2; outlines[i].color = outlineColor;
                outlines[i].enabled = outlineWidth > 0;
            }
        }
        private void Update()
        {
            bool show = presentation && presentation.ShowCrosshair;
            if (group && group.gameObject.activeSelf != show) group.gameObject.SetActive(show);
        }
        private void OnValidate() => ApplyStyle();
        private void OnDisable() { if (group) group.gameObject.SetActive(false); }
        private void OnDestroy() { if (canvasRoot) Destroy(canvasRoot); }
    }
}


