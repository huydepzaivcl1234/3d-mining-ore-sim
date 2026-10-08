using UnityEngine;
using UnityEngine.EventSystems;

namespace MiningSimulator.Ores
{
    // A separate script asset lets Unity persist this component in scenes and prefabs.
    public sealed class UpgradeCardScrollRect : UnityEngine.UI.ScrollRect
    {
        public MiningUpgradeCarousel Owner { get; set; }
        public override void OnScroll(PointerEventData data)
        { if (Owner != null && Mathf.Abs(data.scrollDelta.y) > 0.001f) Owner.ScrollSteps(data.scrollDelta.y < 0f ? 1 : -1); }
        public override void OnBeginDrag(PointerEventData data) { }
        public override void OnDrag(PointerEventData data) { }
        public override void OnEndDrag(PointerEventData data) { }
        protected override void LateUpdate() { }
    }
}
