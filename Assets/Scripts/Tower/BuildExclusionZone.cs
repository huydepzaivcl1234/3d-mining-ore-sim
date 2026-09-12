using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Circular no-build zone on the XZ ground plane. Use this for the Main Base,
/// spawn portals, rocks, props, or any other object that towers must keep away from.
/// Registered statically so placement checks do not scan the whole scene every frame.
/// </summary>
public class BuildExclusionZone : MonoBehaviour
{
    public static readonly HashSet<BuildExclusionZone> ActiveZones = new HashSet<BuildExclusionZone>();

    [Min(0f)]
    [Tooltip("Ground-plane radius that towers must stay outside of.")]
    public float radius = 6f;

    [Tooltip("Optional local-space offset for the center of the exclusion zone.")]
    public Vector3 centerOffset = Vector3.zero;

    public Vector3 WorldCenter => transform.TransformPoint(centerOffset);

    private void OnEnable() => ActiveZones.Add(this);
    private void OnDisable() => ActiveZones.Remove(this);
    private void OnDestroy() => ActiveZones.Remove(this);

    public bool Contains(Vector3 worldPoint)
    {
        Vector3 diff = worldPoint - WorldCenter;
        diff.y = 0f;
        return diff.sqrMagnitude < radius * radius;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.15f, 0.05f, 0.45f);
        Gizmos.DrawWireSphere(WorldCenter, radius);
    }
}
