using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public static class MiningPlacement
    {
        private static readonly Collider[] overlaps = new Collider[128];
        private static readonly List<Collider> initial = new();
        private static bool Actor(Collider c) => c.GetComponentInParent<MiningNpc>() != null ||
            c.GetComponentInParent<CharacterController>() != null || (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic);
        private static bool Blocking(Collider c, float ground) => !(c is TerrainCollider) && !c.isTrigger &&
            c.bounds.max.y > ground + .08f && !Actor(c);
        private static int Query(Vector3 foot, NavigationProfile profile) => Physics.OverlapCapsuleNonAlloc(
            foot + Vector3.up * (profile.Radius + .08f), foot + Vector3.up * Mathf.Max(profile.Radius + .08f, profile.Height - profile.Radius),
            profile.Radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
        public static bool TryEscape(CapsuleCollider capsule, Vector3 root, NavigationProfile profile, float radius, out Vector3 safe)
        {
            safe = root; var grid = MiningNavGrid.Instance;
            if (!MiningNavigation.PathfindingAvailable) return false;
            Vector3 start = profile.Foot(root); initial.Clear();
            int count = Query(start, profile);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++) if (Blocking(overlaps[i], start.y) && overlaps[i] != capsule) initial.Add(overlaps[i]);
            if (initial.Count == 0) return false;
            for (float distance = profile.Radius * .5f; distance <= radius; distance += profile.Radius * .5f)
            for (int direction = 0; direction < 24; direction++)
            {
                Vector3 end = start + Quaternion.AngleAxis(direction * 15, Vector3.up) * Vector3.forward * distance;
                if (!grid.TryGetGroundPoint(end, out end) || !grid.IsPointClear(end, profile.Radius, profile.Height) ||
                    !MiningStandReservations.SpaceFree(end, profile.Radius, capsule.GetComponent<MiningNpc>())) continue;
                if (!EscapeConnector(start, end, profile, capsule)) continue;
                safe = profile.Root(end); return true;
            }
            return false;
        }
        private static float PenetrationScore(Collider c, Vector3 foot, NavigationProfile profile)
        {
            Vector3 middle = foot + Vector3.up * (profile.Height * .5f);
            Vector3 near = c is MeshCollider mesh && !mesh.convex ? c.bounds.ClosestPoint(middle) : c.ClosestPoint(middle);
            Vector3 gap = middle - near; gap.y = 0;
            float depth = 0;
            if (gap.sqrMagnitude < .000001f && c.bounds.Contains(middle))
                depth = Mathf.Min(middle.x - c.bounds.min.x, c.bounds.max.x - middle.x,
                    middle.z - c.bounds.min.z, c.bounds.max.z - middle.z);
            return Mathf.Max(0, profile.Radius + depth - gap.magnitude);
        }
        private static bool EscapeConnector(Vector3 start, Vector3 end, NavigationProfile profile, CapsuleCollider owner)
        {
            var lastScores = new float[initial.Count];
            for (int i = 0; i < initial.Count; i++) lastScores[i] = PenetrationScore(initial[i], start, profile);
            Vector3 previous = start;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(start, end) / .05f));
            for (int n = 1; n <= steps; n++)
            {
                Vector3 foot = Vector3.Lerp(start, end, (float)n / steps);
                if (!MiningNavGrid.Instance.TryGetRecoveryGround(foot, initial, out foot) ||
                    Mathf.Abs(foot.y - previous.y) > MiningNavGrid.Instance.MaximumStep) return false;
                previous = foot;
                for (int i = 0; i < initial.Count; i++)
                {
                    float score = PenetrationScore(initial[i], foot, profile);
                    if (score > lastScores[i] + .001f) return false;
                    lastScores[i] = score;
                }
                int count = Query(foot, profile);
                if (count == overlaps.Length) return false;
                for (int i = 0; i < count; i++)
                {
                    if (overlaps[i].transform.IsChildOf(owner.transform)) continue;
                    if (Actor(overlaps[i]) || (Blocking(overlaps[i], foot.y) && !initial.Contains(overlaps[i]))) return false;
                }
            }
            foreach (float score in lastScores) if (score >= .001f) return false;
            return true;
        }
        public static bool TryMinerSpawn(Vector3 origin, MiningNpc prefab, NpcData data, out Vector3 root)
        {
            root = origin;
            if (!MiningNavigation.PathfindingAvailable || prefab == null) return false;
            var collider = prefab.GetComponent<CapsuleCollider>();
            if (collider == null) return false;
            var profile = NavigationProfile.From(collider, data);
            for (int attempt = 0; attempt < data.SpawnAttempts; attempt++)
            {
                Vector2 spread = Random.insideUnitCircle * data.SpawnSpread;
                Vector3 foot = origin + new Vector3(spread.x, 0, spread.y);
                if (!MiningNavGrid.Instance.TryGetGroundPoint(foot, out foot) ||
                    !MiningNavGrid.Instance.IsPointClear(foot, profile.Radius, profile.Height) ||
                    !MiningStandReservations.SpaceFree(foot, profile.Radius) ||
                    !MiningNpc.IsSpawnPositionClear(foot, profile.Radius)) continue;
                // Spawn grounded. The old height offset could lift the head into overhead geometry.
                root = profile.Root(foot);
                return true;
            }
            return false;
        }
        /// <summary>Inactive prefab bounds; Collider.bounds is empty until activation.</summary>
        public static bool TryBounds(Component target, out Bounds bounds)
        {
            bool found = false; bounds = default;
            foreach (Collider c in target.GetComponentsInChildren<Collider>(true))
            {
                if (c.isTrigger || !c.enabled) continue;
                Bounds local;
                if (c is BoxCollider box) local = new Bounds(box.center, box.size);
                else if (c is MeshCollider mesh && mesh.sharedMesh != null) local = mesh.sharedMesh.bounds;
                else if (c is SphereCollider sphere) local = new Bounds(sphere.center, Vector3.one * sphere.radius * 2);
                else if (c is CapsuleCollider capsule)
                {
                    Vector3 size = Vector3.one * capsule.radius * 2; size[capsule.direction] = capsule.height;
                    local = new Bounds(capsule.center, size);
                }
                else return false;
                for (int n = 0; n < 8; n++)
                {
                    Vector3 corner = local.center + Vector3.Scale(local.extents, new Vector3((n & 1) == 0 ? -1 : 1, (n & 2) == 0 ? -1 : 1, (n & 4) == 0 ? -1 : 1));
                    corner = c.transform.TransformPoint(corner);
                    if (!found) bounds = new Bounds(corner, Vector3.zero); else bounds.Encapsulate(corner);
                    found = true;
                }
            }
            return found;
        }
        public static bool FootprintFree(Ore ore, Bounds bounds, float passage, float ground)
        {
            Bounds inflated = bounds;
            inflated.Expand(new Vector3(passage * 2, 0, passage * 2));
            int count = Physics.OverlapBoxNonAlloc(inflated.center, inflated.extents, overlaps, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Collider c = overlaps[i];
                if (c.transform.IsChildOf(ore.transform) || c is TerrainCollider || c.bounds.max.y <= ground + .08f) continue;
                return false;
            }
            foreach (var miner in MiningNpc.Miners)
            {
                if (miner == null || !miner.isActiveAndEnabled) continue;
                Vector3 p = miner.transform.position;
                Vector3 closest = inflated.ClosestPoint(p); closest.y = p.y;
                if ((p - closest).sqrMagnitude < miner.NavigationRadius * miner.NavigationRadius) return false;
            }
            return MiningStandReservations.SpaceFree(bounds.center, bounds.extents.magnitude + passage);
        }
    }
}
