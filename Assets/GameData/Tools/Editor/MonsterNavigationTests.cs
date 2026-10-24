#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using MiningSimulator.Ores;
using NUnit.Framework;
using UnityEngine;

public sealed class MonsterNavigationTests
{
    private GameObject root;
    private WorldNavigationGrid grid;
    private static readonly Vector3 Center = new Vector3(10000f, 0f, 10000f);
    private readonly List<Vector3> route = new();
    [SetUp] public void SetUp()
    {
        route.Clear();
        root = new GameObject("Temporary A* test arena");
        var floor = Box("Ground", Center + Vector3.down * .5f, new Vector3(20f, 1f, 20f));
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0) floor.layer = groundLayer;
        grid = root.AddComponent<WorldNavigationGrid>();
        grid.ConfigureArea(Center, new Vector2(20f, 20f));
        grid.Configure(.5f, .25f, 1.8f, 45f, .5f, ~0, 512, 256);
        Physics.SyncTransforms();
        grid.Rebake();
    }
    [TearDown] public void TearDown() { Object.DestroyImmediate(root); }
    private GameObject Box(string name, Vector3 position, Vector3 scale)
    {
        var obj = new GameObject(name); obj.transform.SetParent(root.transform);
        obj.transform.position = position; obj.transform.localScale = scale;
        obj.AddComponent<BoxCollider>(); return obj;
    }
    private bool Find(Vector3 start, Vector3 end, float radius = .25f) =>
        grid.TryFindPath(start, end, new NavigationProfile(radius, 1.8f), route);
    private void PumpGrid() => typeof(WorldNavigationGrid).GetMethod("Update",
        BindingFlags.Instance | BindingFlags.NonPublic).Invoke(grid, null);
    private void AssertClear(Vector3 start, float radius = .25f)
    {
        foreach (var corner in route)
        { Assert.That(grid.IsSegmentClear(start, corner, radius, 1.8f), Is.True); start = corner; }
    }
    [Test] public void DirectRouteOnSupportedGround()
    {
        Vector3 start = Center + Vector3.back * 6f, goal = Center + Vector3.forward * 6f;
        Assert.That(Find(start, goal), Is.True); AssertClear(start);
    }
    [Test] public void WallForcesAStarAroundItsEnd()
    {
        var wall = Box("Wall", Center + Vector3.up * 1.5f, new Vector3(6f, 3f, 1f));
        Physics.SyncTransforms(); grid.MarkDirty(wall.GetComponent<Collider>().bounds);
        Vector3 start = Center + Vector3.back * 6f, goal = Center + Vector3.forward * 6f;
        Assert.That(Find(start, goal), Is.True); AssertClear(start);
        Assert.That(route.Exists(p => Mathf.Abs(p.x - Center.x) > 3f), Is.True);
    }
    [Test] public void NewObstacleInvalidatesOldRouteAndReplacementIsClear()
    {
        Vector3 start = Center + Vector3.back * 6f, goal = Center + Vector3.forward * 6f;
        Assert.That(Find(start, goal), Is.True);
        var wall = Box("Appeared wall", Center + Vector3.up * 1.5f, new Vector3(6f, 3f, 1f));
        Physics.SyncTransforms(); grid.MarkDirty(wall.GetComponent<Collider>().bounds);
        Assert.That(grid.IsSegmentClear(start, goal, .25f, 1.8f), Is.False);
        Assert.That(Find(start, goal), Is.True); AssertClear(start);
    }
    [Test] public void SealedTargetReturnsFailureWithoutCrossingWalls()
    {
        foreach (var p in new[]{new Vector3(0,1.5f,2),new Vector3(0,1.5f,-2)})
            Box("Horizontal wall",Center+p,new Vector3(5f,3f,.5f));
        foreach (var p in new[]{new Vector3(2,1.5f,0),new Vector3(-2,1.5f,0)})
            Box("Vertical wall",Center+p,new Vector3(.5f,3f,5f));
        Physics.SyncTransforms(); grid.Rebake();
        Assert.That(Find(Center + Vector3.back*6f, Center), Is.False);
    }
    [Test] public void ScaledControllerProfileUsesActualCenterAndFeet()
    {
        var obj = new GameObject("Capsule"); obj.transform.SetParent(root.transform);
        obj.transform.position = Center; obj.transform.localScale = new Vector3(2f, 3f, 2f);
        var motor = obj.AddComponent<CharacterController>(); motor.radius=.3f; motor.height=2f; motor.center=new Vector3(0,1f,0);
        var profile = MonsterPathFollower.ProfileFor(motor);
        Assert.That(profile.Radius, Is.EqualTo(.6f).Within(.001f));
        Assert.That(profile.Height, Is.EqualTo(6f).Within(.001f));
        Assert.That(profile.Foot(obj.transform.position).y, Is.EqualTo(Center.y).Within(.001f));
    }
    [Test] public void DisabledGridCompletesRequestAsUnavailable()
    {
        grid.enabled = false; bool completed = false;
        grid.RequestPath(root,Center,Center+Vector3.forward,
            new NavigationProfile(.25f,1.8f), result =>
            { completed=true; Assert.That(result.Status, Is.EqualTo(PathStatus.Unavailable)); });
        Assert.That(completed,Is.True);
    }

    // Inject a baked cost fixture without changing project layers or real Terrain assets.
    private void AddExpensivePatch()
    {
        var field = typeof(WorldNavigationGrid).GetField("cells", BindingFlags.Instance | BindingFlags.NonPublic);
        var cells = (System.Array)field.GetValue(grid);
        var cellType = cells.GetType().GetElementType();
        for (int i = 0; i < cells.Length; i++)
        {
            object cell = cells.GetValue(i);
            Vector3 point = (Vector3)cellType.GetField("point").GetValue(cell) - Center;
            if (Mathf.Abs(point.x) >= 1.5f || Mathf.Abs(point.z) >= 2f) continue;
            cellType.GetField("cost").SetValue(cell, 30f);
            cells.SetValue(cell, i);
        }
    }
    private void AssertAvoidsExpensivePatch(Vector3 start)
    {
        foreach (Vector3 end in route)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(start, end) / .05f));
            for (int i = 0; i <= steps; i++)
            {
                Vector3 p = Vector3.Lerp(start, end, (float)i / steps) - Center;
                Assert.That(Mathf.Abs(p.x) < 1.49f && Mathf.Abs(p.z) < 1.99f, Is.False,
                    "Smoothing must not undo A*'s cheap route: " + p);
            }
            start = end;
        }
    }
    [Test] public void SmoothingDoesNotCrossExpensiveCellsBetweenCheapEndpoints()
    {
        AddExpensivePatch();
        Vector3 start = Center + Vector3.back * 6f, end = Center + Vector3.forward * 6f;
        Assert.That(grid.IsSegmentClear(start, end, .25f, 1.8f), Is.True);
        Assert.That(grid.IsShortcutClear(start, end, .25f, 1.8f), Is.False);
        Assert.That(Find(start, end), Is.True); AssertClear(start); AssertAvoidsExpensivePatch(start);
    }
    [Test] public void QueuedPathDoesNotUseDirectShortcutAcrossExpensiveGround()
    {
        AddExpensivePatch(); bool completed = false;
        Vector3 start = Center + Vector3.back * 6f, end = Center + Vector3.forward * 6f;
        grid.RequestPath(root, start, end, new NavigationProfile(.25f, 1.8f), result =>
        { completed = true; Assert.That(result.Success, Is.True); route.AddRange(result.Route); });
        for (int i = 0; i < 100 && !completed; i++) PumpGrid();
        Assert.That(completed, Is.True); AssertClear(start); AssertAvoidsExpensivePatch(start);
    }
    [Test] public void ThinObstacleCannotBeSkippedBySmoothing()
    {
        var wall = Box("Thin wall", Center + new Vector3(0f, 1.5f, .031f), new Vector3(4f, 3f, .02f));
        Physics.SyncTransforms(); grid.MarkDirty(wall.GetComponent<Collider>().bounds);
        Vector3 start = Center + Vector3.back * 6f, end = Center + Vector3.forward * 6f;
        Assert.That(grid.IsSegmentClear(start, end, .25f, 1.8f), Is.False);
        Assert.That(Find(start, end), Is.True); AssertClear(start);
    }
    [Test] public void RemovedObstacleReopensRoute()
    {
        var wall = Box("Removed wall", Center + Vector3.up * 1.5f, new Vector3(20f, 3f, .5f));
        Physics.SyncTransforms(); grid.MarkDirty(wall.GetComponent<Collider>().bounds);
        Vector3 start = Center + Vector3.back * 6f, end = Center + Vector3.forward * 6f;
        Assert.That(Find(start, end), Is.False);
        Bounds bounds = wall.GetComponent<Collider>().bounds;
        Object.DestroyImmediate(wall); Physics.SyncTransforms(); grid.MarkDirty(bounds);
        Assert.That(Find(start, end), Is.True); AssertClear(start);
    }
    [Test] public void ReplacedRequestCannotInstallObsoleteDestination()
    {
        int oldCalls = 0, newCalls = 0;
        grid.RequestPath(root, Center, Center + Vector3.left * 5f, new NavigationProfile(.25f, 1.8f), _ => oldCalls++);
        grid.RequestPath(root, Center, Center + Vector3.right * 5f, new NavigationProfile(.25f, 1.8f), result =>
        { newCalls++; Assert.That(result.Success, Is.True); Assert.That(result.Route[result.Route.Count - 1], Is.EqualTo(Center + Vector3.right * 5f)); });
        for (int i = 0; i < 10 && newCalls == 0; i++) PumpGrid();
        Assert.That(oldCalls, Is.Zero); Assert.That(newCalls, Is.EqualTo(1));
    }
    [Test] public void ManyQueuedOwnersCompleteWithSmallSearchBudget()
    {
        grid.Configure(.5f, .25f, 1.8f, 45f, .5f, ~0, 8, 32);
        var wall = Box("Queue wall", Center + Vector3.up * 1.5f, new Vector3(6f, 3f, 1f));
        Physics.SyncTransforms(); grid.MarkDirty(wall.GetComponent<Collider>().bounds);
        int completed = 0;
        for (int i = 0; i < 16; i++)
        {
            var owner = new GameObject("Queued owner"); owner.transform.SetParent(root.transform);
            grid.RequestPath(owner, Center + Vector3.back * 6f, Center + Vector3.forward * 6f,
                new NavigationProfile(.25f, 1.8f), result =>
                { Assert.That(result.Success, Is.True); completed++; });
        }
        for (int i = 0; i < 2000 && completed < 16; i++) PumpGrid();
        Assert.That(completed, Is.EqualTo(16)); Assert.That(grid.PendingRequests, Is.Zero);
    }
    [Test] public void HighSpeedStepCannotOvershootIntermediateCorner()
    {
        var instance = typeof(WorldNavigationGrid).GetProperty("Instance");
        var previous = instance.GetValue(null);
        try
        {
            instance.SetValue(null, grid);
            var obj = new GameObject("Inactive follower fixture"); obj.SetActive(false); obj.transform.SetParent(root.transform);
            obj.transform.position = Center;
            var motor = obj.AddComponent<CharacterController>(); motor.radius = .25f; motor.height = 1.8f; motor.center = Vector3.up * .9f;
            var monster = obj.AddComponent<MushroomMonster>();
            var follower = new MonsterPathFollower(monster, motor);
            var type = typeof(MonsterPathFollower);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            ((List<Vector3>)type.GetField("route", flags).GetValue(follower)).AddRange(new[]
            { Center + Vector3.forward * .8f, Center + new Vector3(6f, 0f, .8f) });
            type.GetField("routeOrigin", flags).SetValue(follower, Center);
            type.GetField("revision", flags).SetValue(follower, grid.Revision);
            type.GetField("nextRequest", flags).SetValue(follower, float.MaxValue);
            var config = new MonsterCombatSettings { moveSpeed = 20f, waypointTolerance = .05f };
            Vector3 direction = follower.Tick(Center + new Vector3(6f, 0f, .8f), null, 1f, config, .1f);
            Vector3 step = direction * config.moveSpeed * .1f;
            Assert.That(step.z, Is.EqualTo(.8f).Within(.003f));
            Assert.That(step.x, Is.EqualTo(0f).Within(.003f));
            obj.transform.position += step;
            direction = follower.Tick(Center + new Vector3(6f, 0f, .8f), null, 1f, config, .1f);
            Assert.That(direction.x, Is.GreaterThan(.9f));
            Assert.That(Mathf.Abs(direction.z), Is.LessThan(.01f), "Must turn forward along the next edge, not chase the old corner");
        }
        finally { instance.SetValue(null, previous); }
    }
}
#endif
