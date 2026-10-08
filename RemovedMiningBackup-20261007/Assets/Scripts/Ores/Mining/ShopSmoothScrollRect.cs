using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Soft mouse-wheel scrolling; keeps normal ScrollRect drag and scrollbar controls.</summary>
    public sealed class ShopSmoothScrollRect : ScrollRect
    {
        [SerializeField, Min(1f)] private float wheelStep = 64f;
        [SerializeField, Min(0.01f)] private float wheelSmoothTime = 0.12f;
        private float target;
        private float wheelVelocity;
        private float lastApplied;
        private bool wheelActive;

        public override void OnScroll(PointerEventData eventData)
        {
            if (!IsActive() || content == null || viewport == null || !vertical)
            {
                base.OnScroll(eventData);
                return;
            }
            float hiddenHeight = HiddenHeight();
            if (hiddenHeight <= 0.01f) { CancelWheel(); return; }
            float delta = eventData.scrollDelta.y;
            if (Mathf.Abs(delta) < 0.001f) return;
            if (!wheelActive)
            {
                target = lastApplied = verticalNormalizedPosition;
                wheelVelocity = 0f;
            }
            // Accumulate against the destination, so fast scrolling never loses wheel steps.
            target = Mathf.Clamp01(target + delta * wheelStep / hiddenHeight);
            wheelActive = true;
            StopMovement();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (!wheelActive || content == null || viewport == null) return;
            float current = verticalNormalizedPosition;
            // A scrollbar, navigation or external script takes control immediately.
            if (Mathf.Abs(current - lastApplied) > 0.001f || HiddenHeight() <= 0.01f)
            {
                CancelWheel();
                return;
            }
            StopMovement();
            float next = Mathf.SmoothDamp(current, target, ref wheelVelocity,
                wheelSmoothTime, Mathf.Infinity, Mathf.Min(Time.unscaledDeltaTime, 0.05f));
            if (Mathf.Abs(next - target) < 0.0001f)
            {
                next = target;
                CancelWheel();
            }
            verticalNormalizedPosition = Mathf.Clamp01(next);
            lastApplied = verticalNormalizedPosition;
        }

        private float HiddenHeight()
        {
            // Includes authored scale; no allocations or layout rebuilds per frame.
            return Mathf.Max(0f, content.rect.height * Mathf.Abs(content.localScale.y)
                - viewport.rect.height);
        }

        private void CancelWheel() { wheelActive = false; wheelVelocity = 0f; }
        public override void OnBeginDrag(PointerEventData eventData)
        { CancelWheel(); base.OnBeginDrag(eventData); }
        protected override void OnEnable() { CancelWheel(); base.OnEnable(); }
        protected override void OnDisable() { CancelWheel(); base.OnDisable(); }
    }
}
