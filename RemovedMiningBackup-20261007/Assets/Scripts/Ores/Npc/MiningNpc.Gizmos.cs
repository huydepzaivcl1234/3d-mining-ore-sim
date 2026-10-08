using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MiningSimulator.Ores
{
    public sealed partial class MiningNpc
    {
        private void OnDrawGizmos()
        {
            if (!drawPathGizmos)
            {
                return;
            }

            Vector3 currentPosition = body != null ? body.position : transform.position;
            Vector3 routePosition = currentPosition + Vector3.up * 0.08f;
            Color routeColor = inFinalApproach
                ? Color.green
                : currentPathSource == MiningPathSource.NavMesh
                    ? Color.cyan
                    : currentPathSource == MiningPathSource.Grid ? Color.yellow : Color.gray;

            Gizmos.color = routeColor;
            Gizmos.DrawSphere(routePosition, 0.08f);

            if (currentPath.Count > 0)
            {
                for (int index = 0; index < currentPath.Count; index++)
                {
                    Vector3 waypoint = currentPath[index] + Vector3.up * 0.08f;
                    Gizmos.color = index < pathWaypointIndex
                        ? new Color(routeColor.r, routeColor.g, routeColor.b, 0.28f)
                        : routeColor;
                    Gizmos.DrawLine(routePosition, waypoint);
                    Gizmos.DrawSphere(waypoint, index == pathWaypointIndex ? 0.16f : 0.09f);
                    routePosition = waypoint;
                }

                if (pathWaypointIndex < currentPath.Count)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(currentPath[pathWaypointIndex] + Vector3.up * 0.08f,
                        0.22f);
                }
            }
            else if (hasMoveTarget)
            {
                // Gray line is the requested destination, not a movement fallback.
                Gizmos.color = inFinalApproach ? Color.green : Color.gray;
                Gizmos.DrawLine(routePosition, desiredMoveTarget + Vector3.up * 0.08f);
            }

            if (hasMoveTarget)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(desiredMoveTarget + Vector3.up * 0.08f,
                    Vector3.one * 0.22f);
            }

#if UNITY_EDITOR
            string sourceLabel = inFinalApproach
                ? $"{currentPathSource} final approach"
                : currentPathSource == MiningPathSource.None
                ? (pathPending ? "A* queued" : "Waiting for valid A* route")
                : currentPathSource.ToString();
            Handles.Label(currentPosition + Vector3.up * 1.25f,
                $"Path: {sourceLabel}\nWaypoint: {pathWaypointIndex}/{currentPath.Count}");
#endif
        }

        private void OnDrawGizmosSelected()
        {
            if (npcData == null) return;
            var profile = Profile;
            Vector3 foot = profile.Foot(RootPosition);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(foot + Vector3.up * profile.Radius, profile.Radius);
            Gizmos.DrawWireSphere(foot + Vector3.up * (profile.Height - profile.Radius), profile.Radius);
            Gizmos.DrawLine(foot + Vector3.right * profile.Radius, foot + Vector3.up * profile.Height + Vector3.right * profile.Radius);
            if (standLease != null)
            {
                Gizmos.color = standLease.IsWaiting ? Color.gray : Color.green;
                Gizmos.DrawWireSphere(standLease.Position, profile.Radius);
            }
            Gizmos.color = Color.magenta; Gizmos.DrawLine(RootPosition, RootPosition + PreferredVelocity);
            Gizmos.color = Color.green; Gizmos.DrawLine(RootPosition, RootPosition + AcceptedVelocity);
#if UNITY_EDITOR
            Handles.Label(RootPosition + Vector3.up * 2,
                $"{NavigationStatus} | pending {PendingPathAge:F1}s | remaining {RemainingRouteDistance(RootPosition):F2}m\nRecovery {RecoveryAttempts}: {MovementRejection}");
#endif
        }

    }
}
