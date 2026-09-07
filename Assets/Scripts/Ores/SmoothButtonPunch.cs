using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Interruptible, unscaled-time scale feedback for selectable UI buttons.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SmoothButtonPunch : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler,
        ISubmitHandler
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private float hoverScale = 1.035f;
        [SerializeField] private float hoverPunchScale = 1.075f;
        [SerializeField] private float pressedScale = 0.96f;
        [SerializeField] private float clickBounceScale = 1.08f;
        [SerializeField] private float hoverPunchDuration = 0.08f;
        [SerializeField] private float hoverSettleDuration = 0.10f;
        [SerializeField] private float pressDuration = 0.06f;
        [SerializeField] private float clickBounceDuration = 0.09f;
        [SerializeField] private float clickSettleDuration = 0.12f;

        private Button button;
        private Vector3 restingScale;
        private Coroutine animationRoutine;
        private bool initialized;
        private bool hovered;
        private bool pressed;
        private bool acceptedPointerPress;

        public void Configure(float configuredHoverScale, float configuredHoverPunchScale,
            float configuredPressedScale, float configuredClickBounceScale,
            float configuredHoverPunchDuration, float configuredHoverSettleDuration,
            float configuredPressDuration, float configuredClickBounceDuration,
            float configuredClickSettleDuration)
        {
            target = GetComponent<RectTransform>();
            CenterPivotPreservingPosition(target);
            hoverScale = configuredHoverScale;
            hoverPunchScale = configuredHoverPunchScale;
            pressedScale = configuredPressedScale;
            clickBounceScale = configuredClickBounceScale;
            hoverPunchDuration = configuredHoverPunchDuration;
            hoverSettleDuration = configuredHoverSettleDuration;
            pressDuration = configuredPressDuration;
            clickBounceDuration = configuredClickBounceDuration;
            clickSettleDuration = configuredClickSettleDuration;

            if (!Application.isPlaying)
            {
                restingScale = target.localScale;
                initialized = true;
            }
        }

        private void Awake()
        {
            target ??= GetComponent<RectTransform>();
            CenterPivotPreservingPosition(target);
            button = GetComponent<Button>();
            restingScale = target.localScale;
            initialized = true;
        }

        private void OnEnable()
        {
            target ??= GetComponent<RectTransform>();
            button ??= GetComponent<Button>();
            if (!initialized)
            {
                restingScale = target.localScale;
                initialized = true;
            }
        }

        private void OnDisable()
        {
            StopAnimation();
            hovered = false;
            pressed = false;
            acceptedPointerPress = false;
            if (initialized && target != null)
            {
                target.localScale = restingScale;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            if (CanAnimate())
            {
                PlayHoverPunch();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
            acceptedPointerPress = false;
            PlaySingle(1f, hoverSettleDuration);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanAnimate() || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            pressed = true;
            acceptedPointerPress = true;
            PlaySingle(pressedScale, pressDuration);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            pressed = false;
            PlaySingle(hovered && CanAnimate() ? hoverScale : 1f, hoverSettleDuration);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && acceptedPointerPress)
            {
                PlayClickBounce();
            }
            acceptedPointerPress = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            hovered = true;
            if (CanAnimate())
            {
                PlayHoverPunch();
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            hovered = false;
            pressed = false;
            PlaySingle(1f, hoverSettleDuration);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (isActiveAndEnabled)
            {
                PlayClickBounce();
            }
        }

        private bool CanAnimate()
        {
            return isActiveAndEnabled && (button == null || button.IsInteractable());
        }

        private static void CenterPivotPreservingPosition(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            Vector2 centeredPivot = new(0.5f, 0.5f);
            Vector2 pivotDelta = centeredPivot - rect.pivot;
            rect.anchoredPosition += Vector2.Scale(pivotDelta, rect.rect.size);
            rect.pivot = centeredPivot;
        }

        private void PlayHoverPunch()
        {
            StartAnimation(AnimateSequence(hoverPunchScale, hoverPunchDuration,
                hoverScale, hoverSettleDuration));
        }

        private void PlayClickBounce()
        {
            float settleScale = hovered ? hoverScale : 1f;
            StartAnimation(AnimateSequence(clickBounceScale, clickBounceDuration,
                settleScale, clickSettleDuration));
        }

        private void PlaySingle(float scale, float duration)
        {
            StartAnimation(AnimateScale(scale, duration));
        }

        private void StartAnimation(IEnumerator routine)
        {
            StopAnimation();
            if (target != null && gameObject.activeInHierarchy)
            {
                animationRoutine = StartCoroutine(routine);
            }
        }

        private void StopAnimation()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }
        }

        private IEnumerator AnimateSequence(float firstScale, float firstDuration,
            float secondScale, float secondDuration)
        {
            yield return AnimateScale(firstScale, firstDuration);
            yield return AnimateScale(secondScale, secondDuration);
            animationRoutine = null;
        }

        private IEnumerator AnimateScale(float scale, float duration)
        {
            if (target == null)
            {
                yield break;
            }

            Vector3 start = target.localScale;
            Vector3 destination = restingScale * scale;
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / safeDuration);
                float eased = 1f - Mathf.Pow(1f - normalized, 3f);
                target.localScale = Vector3.LerpUnclamped(start, destination, eased);
                yield return null;
            }

            target.localScale = destination;
        }
    }
}
