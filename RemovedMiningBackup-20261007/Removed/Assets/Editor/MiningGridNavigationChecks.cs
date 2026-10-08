#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Isolated preview-scene checks. Does not touch SampleScene, prefabs or save data.</summary>
public static class MiningGridNavigationChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [MenuItem("Mining Simulator/Validation/Terrain A Star")]
    public static void MenuRun() => Debug.Log(Run());
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    private static void Require(bool condition, string description)
    { if (!condition) throw new InvalidOperationException(description); }
    public static string Run() => MiningNavigationRepairChecks.Run();
    /// <summary>Checks actual mineable colliders and stopping margins in the user's Play session.</summary>
    public static string RunLiveStandPoints()
    {
        Require(EditorApplication.isPlaying, "Live checks require Play.");
        var grid = MiningNavGrid.Instance;
        Require(grid != null && grid.HasBaked, "Wait for the live grid bake.");
        int checkedTargets = 0;
        foreach (var miner in UnityEngine.Object.FindObjectsByType<MiningNpc>(FindObjectsSortMode.None))
        {
            var data = (NpcData)typeof(MiningNpc).GetField("npcData", Private).GetValue(miner);
            foreach (var ore in UnityEngine.Object.FindObjectsByType<Ore>(FindObjectsSortMode.None))
            {
                if (ore.IsDepleted || !MiningNavigation.TryGetOreApproach(ore, miner.transform.position,
                    miner.NavigationRadius, out Vector3 point, out _, miner.NavigationMiningReach)) continue;
                float gap = Mathf.Sqrt(ore.SqrDistanceToSurface(point));
                Require(gap + data.StoppingDistance <= data.MiningRange + .001f,
                    $"Stand point for {ore.name} stops outside the actual mining collider.");
                Require(grid.IsPointClear(point, miner.NavigationRadius), "Stand point intersects a solid obstacle.");
                checkedTargets++;
            }
        }
        Require(checkedTargets > 0, "No real miner/ore stand points were available to test.");
        return $"PASS: {checkedTargets} live collider stand points with stopping margin and capsule clearance.";
    }
}
#endif
