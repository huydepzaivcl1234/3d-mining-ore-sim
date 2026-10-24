using UnityEngine;
namespace MiningSimulator.Ores
{
    /// <summary>Only invalidates cells occupied by this obstacle; never rebuilds terrain heights.</summary>
    public sealed class WorldNavigationObstacle : MonoBehaviour
    {
        [Min(.02f)] [SerializeField] private float updateInterval = .15f;
        private Collider[] colliders;
        private MiningChest chest; private LuckyBlock block;
        private Bounds previous;
        private bool hadBounds, wasBlocking;
        private float nextUpdate;
        public static void Ensure(Component target)
        {
            if (target != null && target.GetComponent<WorldNavigationObstacle>() == null) target.gameObject.AddComponent<WorldNavigationObstacle>();
        }
        private void Awake()
        {
            colliders = GetComponentsInChildren<Collider>(true);
            chest = GetComponent<MiningChest>(); block = GetComponent<LuckyBlock>();
        }
        private void OnEnable() { NotifyGeometryChanged(); }
        public void NotifyGeometryChanged()
        { colliders = GetComponentsInChildren<Collider>(true); Refresh(true); }
        private void OnDisable() { if (hadBounds) WorldNavigationGrid.Instance?.MarkDirty(previous); hadBounds = false; }
        private void Update()
        {
            if (Time.unscaledTime < nextUpdate) return;
            nextUpdate = Time.unscaledTime + updateInterval; Refresh(false);
        }
        private void Refresh(bool force)
        {
            bool blocking = chest != null ? chest.CanMine : block == null || !block.IsResolved;
            Bounds bounds = default; bool found = false;
            foreach (Collider c in colliders)
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy || c.isTrigger) continue;
                if (!found) bounds = c.bounds; else bounds.Encapsulate(c.bounds); found = true;
            }
            bool changed = force || found != hadBounds || blocking != wasBlocking || (found &&
                ((bounds.center - previous.center).sqrMagnitude > .0025f || (bounds.size - previous.size).sqrMagnitude > .0025f));
            if (changed)
            {
                if (hadBounds) WorldNavigationGrid.Instance?.MarkDirty(previous);
                if (found) WorldNavigationGrid.Instance?.MarkDirty(bounds);
            }
            previous = bounds; hadBounds = found; wasBlocking = blocking;
        }
    }
}
