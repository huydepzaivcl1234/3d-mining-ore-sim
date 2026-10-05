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
    public static string Run()
    {
        if (EditorApplication.isPlaying) return "Stop Play before running isolated grid checks.";
        Scene preview = EditorSceneManager.NewPreviewScene();
        TerrainData terrainData = null;
        try
        {
            Vector3 center = new(1000, 0, 1000);
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(floor, preview);
            floor.transform.position = center - Vector3.up * .1f; floor.transform.localScale = new Vector3(20, .2f, 20);
            GameObject host = new("Grid checks"); SceneManager.MoveGameObjectToScene(host, preview);
            MiningNavGrid grid = host.AddComponent<MiningNavGrid>();
            grid.Configure(1, .2f, 1.8f, 45, .5f, ~0, 512, 16);
            Set(grid, "autoBounds", false); Set(grid, "areaCenter", center); Set(grid, "areaSize", new Vector2(10, 10));
            // Preview scenes have their own physics scene; create explicitly sampled cells below.
            Physics.SyncTransforms(); grid.Rebake();
            var array = (Array)typeof(MiningNavGrid).GetField("cells", Private).GetValue(grid);
            Type cellType = array.GetType().GetElementType();
            for (int i = 0; i < array.Length; i++)
            {
                object cell = array.GetValue(i);
                cellType.GetField("ground").SetValue(cell, true); cellType.GetField("open").SetValue(cell, true);
                cellType.GetField("cost").SetValue(cell, 1f);
                array.SetValue(cell, i);
            }
            // Search topology tests bypass the physics-dependent smoothing and endpoint projection.
            MethodInfo expand = typeof(MiningNavGrid).GetMethod("Expand", Private);
            object search = typeof(MiningNavGrid).GetField("sync", Private).GetValue(grid);
            Type searchType = search.GetType();
            searchType.GetField("radius").SetValue(search, .2f);
            searchType.GetField("height").SetValue(search, 1.8f);
            Func<int, int, bool> reachable = (from, to) =>
            {
                searchType.GetMethod("Reset").Invoke(search, new object[] { array.Length, from, to });
                searchType.GetMethod("Push").Invoke(search, new object[] { from, 0f });
                expand.Invoke(grid, new object[] { search, array.Length * 8 });
                return (bool)searchType.GetField("found").GetValue(search);
            };
            Action<int, bool, float> change = (i, open, y) =>
            {
                object cell = array.GetValue(i); Vector3 p = (Vector3)cellType.GetField("point").GetValue(cell); p.y = y;
                cellType.GetField("point").SetValue(cell, p); cellType.GetField("open").SetValue(cell, open); array.SetValue(cell, i);
            };
            Require(reachable(0, 99), "Open grid should be connected.");
            change(1, false, 0); change(10, false, 0);
            Require(!reachable(0, 11), "Diagonal must not cut a blocked corner.");
            change(1, true, 0); change(10, true, 0);
            for (int z = 0; z < 10; z++) change(z * 10 + 5, false, 0);
            Require(!reachable(0, 99), "A solid wall must disconnect the grid.");
            change(55, true, 0); Require(reachable(0, 99), "Opening one wall cell must restore a route.");
            for (int z = 0; z < 10; z++) change(z * 10 + 5, true, 2);
            Require(!reachable(0, 99), "Excessive height steps must block edges.");
            Require(!grid.TryProject(center + Vector3.right * 100, 2, out _), "Out-of-bounds queries must fail, not clamp.");
            grid.MarkDirty(new Bounds(center, Vector3.one));
            int dirtyCount = (int)typeof(MiningNavGrid).GetField("dirty", Private).GetValue(grid).GetType().GetProperty("Count").GetValue(typeof(MiningNavGrid).GetField("dirty", Private).GetValue(grid));
            Require(dirtyCount > 0 && dirtyCount < grid.CellCount, "Dirty bounds must not invalidate the entire grid.");
            terrainData = new TerrainData { heightmapResolution = 33, size = new Vector3(10, 10, 10) };
            var heights = new float[33, 33];
            for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++) heights[z, x] = x / 32f;
            terrainData.SetHeights(0, 0, heights);
            GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            SceneManager.MoveGameObjectToScene(terrainObject, preview); terrainObject.transform.position = center + Vector3.up * 3;
            Set(grid, "terrains", new[] { terrainObject.GetComponent<Terrain>() });
            object[] sample = { center + new Vector3(5, 0, 5), 0f, 0 };
            Require((bool)typeof(MiningNavGrid).GetMethod("SampleGround", Private).Invoke(grid, sample), "Terrain sampling failed.");
            Require(Mathf.Abs(((Vector3)sample[0]).y - 8) < .02f, "Terrain origin Y must be added to sampled height.");
            Require((float)sample[1] > 40, "Steep terrain must report its slope.");
            return "PASS: open grid, no corner cutting, disconnected wall, reopened corridor, height-step rejection, bounds rejection, regional invalidation, Terrain height + slope (8 checks).";
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
            if (terrainData != null) UnityEngine.Object.DestroyImmediate(terrainData);
        }
    }
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
