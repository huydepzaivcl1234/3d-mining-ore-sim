using UnityEngine;
namespace MiningSimulator.Ores
{
    /// <summary>Placement and overlay sampling share the same supported-ground rules.</summary>
    public static class TowerPlacementGeometry
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[64];
        private static readonly Collider[] Overlaps = new Collider[64];
        public static bool IsSupport(Collider collider)
        {
            return collider != null && !collider.isTrigger &&
                collider.GetComponentInParent<MiningCharacterHealth>() == null &&
                collider.GetComponentInParent<CharacterController>() == null &&
                collider.GetComponentInParent<LuckyBlock>() == null &&
                (collider.attachedRigidbody == null || collider.attachedRigidbody.isKinematic);
        }
        public static bool RayGround(Ray ray, float distance, out RaycastHit hit)
        {
            int count = Physics.RaycastNonAlloc(ray, Hits, distance, ~0, QueryTriggerInteraction.Ignore);
            hit = default; float best = float.PositiveInfinity;
            if (count == Hits.Length) return false;
            for (int i = 0; i < count; i++)
                if (Hits[i].distance < best && Hits[i].normal.y >= .92f && IsSupport(Hits[i].collider))
                { hit = Hits[i]; best = hit.distance; }
            return best < float.PositiveInfinity;
        }
        public static bool GroundBelow(Vector3 point, out Vector3 ground)
        {
            bool found = RayGround(new Ray(point + Vector3.up * 8f, Vector3.down), 24f, out var hit);
            ground = found ? hit.point : point; return found;
        }
        public static Vector2Int FootprintCells(BoxCollider body, float cell)
            => new Vector2Int(Mathf.CeilToInt((body.size.x + .1f) / cell), Mathf.CeilToInt((body.size.z + .1f) / cell));
        public static Vector3 Snap(Vector3 point, BoxCollider body, float cell)
        {
            var cells = FootprintCells(body, cell);
            point.x = (Mathf.Floor(point.x / cell) + (cells.x % 2) * .5f) * cell - body.center.x;
            point.z = (Mathf.Floor(point.z / cell) + (cells.y % 2) * .5f) * cell - body.center.z;
            return point;
        }
        public static bool Validate(ref Vector3 foot, BoxCollider body, Transform player, float reach)
        {
            if (!GroundBelow(foot, out var ground)) return false;
            foot = ground;
            if (Vector3.ProjectOnPlane(foot - player.position, Vector3.up).sqrMagnitude > reach * reach) return false;
            Vector3 half = body.size * .5f;
            // All four feet need continuous, nearly level support; do not bridge a ledge.
            for (int x = -1; x <= 1; x += 2) for (int z = -1; z <= 1; z += 2)
            {
                Vector3 probe = foot + new Vector3(body.center.x + x * half.x, 0, body.center.z + z * half.z);
                if (!GroundBelow(probe, out var support) || Mathf.Abs(support.y - foot.y) > .12f) return false;
            }
            Vector3 center = foot + body.center + Vector3.up * .03f;
            half.x += .05f; half.z += .05f; half.y = Mathf.Max(.01f, half.y - .03f);
            int count = Physics.OverlapBoxNonAlloc(center, half, Overlaps, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            if (count == Overlaps.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var obstacle = Overlaps[i];
                if (IsSupport(obstacle) && obstacle.bounds.max.y <= foot.y + .025f) continue;
                return false;
            }
            return true;
        }
    }
}
