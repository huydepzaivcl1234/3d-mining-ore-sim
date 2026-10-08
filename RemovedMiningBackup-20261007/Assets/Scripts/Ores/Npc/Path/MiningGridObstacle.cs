using UnityEngine;
namespace MiningSimulator.Ores
{
    /// <summary>Only invalidates cells occupied by this obstacle; never rebuilds terrain heights.</summary>
    public sealed class MiningGridObstacle : MonoBehaviour
    {
        [Min(.02f)] [SerializeField] private float updateInterval = .15f;
        private Collider[] colliders;
        private Ore ore; private MiningChest chest; private LuckyBlock block;
        private Bounds previous;
        private bool hadBounds, wasBlocking;
        private float nextUpdate;
        public static void Ensure(Component target)
        {
            if (target != null && target.GetComponent<MiningGridObstacle>() == null) target.gameObject.AddComponent<MiningGridObstacle>();
        }
        private void Awake()
        {
            colliders = GetComponentsInChildren<Collider>(true);
            ore = GetComponent<Ore>(); chest = GetComponent<MiningChest>(); block = GetComponent<LuckyBlock>();
        }
        private void OnEnable() { NotifyGeometryChanged(); }
        public void NotifyGeometryChanged()
        { colliders = GetComponentsInChildren<Collider>(true); Refresh(true); }
        private void OnDisable() { if (hadBounds) MiningNavGrid.Instance?.MarkDirty(previous); hadBounds = false; }
        private void Update()
        {
            if (ore != null) return; // Ore placement/depletion notifies immediately; visuals never move its footprint.
            if (Time.unscaledTime < nextUpdate) return;
            nextUpdate = Time.unscaledTime + updateInterval; Refresh(false);
        }
        private void Refresh(bool force)
        {
            bool blocking = ore != null ? !ore.IsDepleted : chest != null ? chest.CanMine : block == null || !block.IsResolved;
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
                if (hadBounds) MiningNavGrid.Instance?.MarkDirty(previous);
                if (found) MiningNavGrid.Instance?.MarkDirty(bounds);
            }
            previous = bounds; hadBounds = found; wasBlocking = blocking;
        }
    }
}
