using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Animation-only hover and press feedback for the Gem HUD plus button.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class JuicyGemPlusButton : MonoBehaviour, IPointerEnterHandler,
        IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform visual;
        [Min(1f), SerializeField] private float hoverScale = 1.10f;
        [Range(0.5f, 1f), SerializeField] private float pressScale = 0.94f;
        [Min(1f), SerializeField] private float response = 22f;

        private Button button;
        private Vector3 authoredScale = Vector3.one;
        private float targetMultiplier = 1f;
        private bool pointerInside;

        private void Awake()
        {
            button = GetComponent<Button>();
            visual ??= transform as RectTransform;
            if (visual != null)
            {
                authoredScale = visual.localScale;
            }
        }

        private void Update()
        {
            if (visual == null)
            {
                return;
            }
            float blend = 1f - Mathf.Exp(-response * Time.unscaledDeltaTime);
            visual.localScale = Vector3.Lerp(visual.localScale,
                authoredScale * targetMultiplier, blend);
        }

        private void OnDisable()
        {
            targetMultiplier = 1f;
            pointerInside = false;
            if (visual != null)
            {
                visual.localScale = authoredScale;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            if (CanAnimate()) targetMultiplier = hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            targetMultiplier = 1f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (CanAnimate() && eventData.button == PointerEventData.InputButton.Left)
                targetMultiplier = pressScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            targetMultiplier = pointerInside && CanAnimate() ? hoverScale : 1f;
        }

        private bool CanAnimate() => button != null && button.IsInteractable();

#if UNITY_EDITOR
        private void OnValidate()
        {
            hoverScale = Mathf.Max(1f, hoverScale);
            pressScale = Mathf.Clamp(pressScale, 0.5f, 1f);
            response = Mathf.Max(1f, response);
        }
#endif
    }
}
