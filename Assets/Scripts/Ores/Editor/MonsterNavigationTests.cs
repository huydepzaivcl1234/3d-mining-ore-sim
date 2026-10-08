#if UNITY_EDITOR
using System.Collections.Generic;
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
}
#endif