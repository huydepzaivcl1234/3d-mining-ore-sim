using UnityEngine;

using System;
using System.Collections.Generic;

namespace MiningSimulator.Ores
{
    /// <summary>Shared placement bounds for world item drops; contains no ore spawning rules.</summary>
    [CreateAssetMenu(fileName = "WorldSpawnAreaData", menuName = "Game Data/World/Drop Area")]
    public sealed class WorldSpawnAreaData : ScriptableObject
    {
        [SerializeField] private Vector3 areaCenter;
        [SerializeField] private Vector3 areaSize = new(20f, 0f, 20f);
        [SerializeField] private bool alignToGround = true;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float groundRayStartHeight = 20f;
        [SerializeField] private float groundRayDistance = 100f;
        [SerializeField] private float heightOffset;
        public Vector3 AreaCenter => areaCenter;
        public Vector3 AreaSize => areaSize;
        public bool AlignToGround => alignToGround;
        public LayerMask GroundLayers => groundLayers;
        public float GroundRayStartHeight => groundRayStartHeight;
        public float GroundRayDistance => groundRayDistance;
        public float HeightOffset => heightOffset;
    }
    internal static class PercentageChanceSelector
    {
        public static T Choose<T>(IReadOnlyList<T> entries, Func<T, float> chance,
            Predicate<T> eligible, float roll) where T : class
        {
            if (entries == null || chance == null || eligible == null) return null;
            float total = 0f;
            foreach (var entry in entries)
                if (entry != null && eligible(entry)) total += Mathf.Max(0f, chance(entry));
            if (total <= 0f) return null;
            float remaining = Mathf.Clamp01(roll) * total;
            T last = null;
            foreach (var entry in entries)
            {
                if (entry == null || !eligible(entry)) continue;
                float weight = Mathf.Max(0f, chance(entry));
                if (weight <= 0f) continue;
                last = entry;
                remaining -= weight;
                if (remaining <= 0f) return entry;
            }
            return last;
        }
    }
}
