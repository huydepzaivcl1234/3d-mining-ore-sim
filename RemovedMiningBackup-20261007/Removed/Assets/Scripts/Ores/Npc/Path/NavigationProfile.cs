using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Queries use ground/foot coordinates; the motor uses Rigidbody root coordinates.</summary>
    public readonly struct NavigationProfile
    {
        public readonly float Radius, Height;
        public readonly Vector3 FootOffset;
        public NavigationProfile(float radius, float height, Vector3 footOffset = default)
        {
            Radius = Mathf.Max(.05f, radius);
            Height = Mathf.Max(Radius * 2, height);
            FootOffset = footOffset;
        }
        public Vector3 Foot(Vector3 root) => root + FootOffset;
        public Vector3 Root(Vector3 foot) => foot - FootOffset;
        public static NavigationProfile From(CapsuleCollider capsule, NpcData data)
        {
            Vector3 scale = capsule.transform.lossyScale;
            float radius = data.ColliderRadius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float height = Mathf.Max(radius * 2, data.ColliderHeight * Mathf.Abs(scale.y));
            Vector3 center = capsule.transform.TransformVector(capsule.center);
            return new NavigationProfile(Mathf.Max(data.ObstacleProbeRadius, radius + data.StandSlotSpacingPadding),
                height, center - Vector3.up * (height * .5f));
        }
    }

    public enum NavigationState { Idle, WaitingForGrid, WaitingForPath, WaitingForSlot, Moving, Mining, Recovering, Unreachable, WaitingForCrowd }
    public enum PathStatus { Success, Unavailable, InvalidStart, InvalidEnd, Unreachable, Stale, Canceled, Crowded }
    public readonly struct PathResult
    {
        public readonly PathStatus Status;
        public readonly List<Vector3> Route;
        public readonly int Generation, Revision;
        public bool Success => Status == PathStatus.Success;
        public PathResult(PathStatus status, List<Vector3> route, int generation, int revision)
        { Status = status; Route = route; Generation = generation; Revision = revision; }
    }
}
