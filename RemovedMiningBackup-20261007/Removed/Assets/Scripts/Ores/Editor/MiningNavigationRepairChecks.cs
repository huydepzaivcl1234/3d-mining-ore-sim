#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using MiningSimulator.Ores;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Real physics queries in a disposable additive scene. Never writes saves or authored assets.</summary>
public static class MiningNavigationRepairChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [MenuItem("Mining Simulator/Validation/Navigation Repair")]
    public static void MenuRun() => Debug.Log(Run());
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    private static GameObject Cube(Scene scene, string name, Vector3 position, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = position; go.transform.localScale = size; return go;
    }
    public static string Run()
    {
        Require(!EditorApplication.isPlaying, "Run the isolated checks outside Play Mode.");
        Scene previous = SceneManager.GetActiveScene();
        var instance = typeof(MiningNavGrid).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        object oldGrid = instance.GetValue(null);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var center = new Vector3(10000, 0, 10000);
        var data = ScriptableObject.CreateInstance<NpcData>();
        OreData placementData = null;
        OreSpawnData spawnData = null;
        var testOwners = new List<MiningNpc>();
        int passed = 0;
        void Check(bool ok, string message) { Require(ok, message); passed++; }
        try
        {
            Cube(scene, "Test floor", center - Vector3.up * .1f, new Vector3(16, .2f, 16)).layer = Mathf.Max(0, LayerMask.NameToLayer("Ground"));
            var host = new GameObject("Isolated grid"); SceneManager.MoveGameObjectToScene(host, scene);
            instance.SetValue(null, null);
            var grid = host.AddComponent<MiningNavGrid>(); instance.SetValue(null, grid);
            grid.ConfigureArea(center, new Vector2(12, 12));
            grid.Configure(.5f, .2f, 1.8f, 45, .5f, ~0, 32, 32);
            typeof(MiningNavGrid).GetField("standAllocationsPerFrame", Private).SetValue(grid, 1000);
            Physics.SyncTransforms(); grid.Rebake();
            var profile = new NavigationProfile(.2f, 1.8f);
            var route = new List<Vector3>();
            Vector3 from = center + new Vector3(-4, 0, -4), to = center + new Vector3(4, 0, 4);
            Check(grid.TryFindPath(from, to, profile, route), "Open floor must be reachable.");
            Check(!grid.TryProject(center + Vector3.right * 100, 2, out _), "Projection must reject out-of-bounds starts.");
            var wall = Cube(scene, "Blocking wall", center + Vector3.up, new Vector3(.2f, 2, 14));
            Physics.SyncTransforms(); grid.Rebake();
            Check(!grid.TryFindPath(from, to, profile, route), "Full wall must disconnect the grid.");
            Check(!grid.IsCapsulePlacementClear(center, profile.Radius, profile.Height), "Actual capsule inside a wall must not be projected above it.");
            Check(grid.TryGetGroundPoint(center, out var belowWall) && Mathf.Abs(belowWall.y) < .01f,
                "An obstacle must not become ground support.");
            wall.transform.localScale = new Vector3(.2f, 2, 6); Physics.SyncTransforms(); grid.Rebake();
            Check(grid.TryFindPath(from, to, profile, route), "Reachable opposite side must route around a wall.");
            bool safe = true;
            for (int i = 1; i < route.Count; i++) safe &= grid.IsSegmentClear(route[i - 1], route[i], profile.Radius, profile.Height);
            Check(safe, "Every smoothed segment must satisfy the same capsule check as the motor.");
            wall.SetActive(false); Physics.SyncTransforms(); grid.Rebake();
            Check(grid.TryFindPath(from, to, new NavigationProfile(.35f, 1.8f), route),
                "Shared clearance caches must support distinct actor sizes.");
            wall.transform.localScale = new Vector3(.2f, 2, 14); wall.SetActive(true);
            Physics.SyncTransforms(); grid.MarkDirty(wall.GetComponent<Collider>().bounds);
            Check(!grid.TryFindPath(from, to, new NavigationProfile(.35f, 1.8f), route),
                "Dirty footprints must invalidate cached edges for every actor profile immediately.");
            wall.SetActive(false); Physics.SyncTransforms(); grid.MarkDirty(wall.GetComponent<Collider>().bounds);
            Check(grid.TryFindPath(from, to, new NavigationProfile(.35f, 1.8f), route),
                "Removing a blocker must reopen cached routes without a full rebake.");
            var owner = new GameObject("Request owner"); SceneManager.MoveGameObjectToScene(owner, scene);
            PathResult result = default; bool completed = false;
            grid.RequestPath(owner, from, to, profile, r => { result = r; completed = true; });
            var update = typeof(MiningNavGrid).GetMethod("Update", Private);
            for (int i = 0; i < 200 && !completed; i++)
            { grid.MarkDirty(new Bounds(center, Vector3.one * 8)); update.Invoke(grid, null); }
            Check(completed && result.Success, "Dirty work must not starve path requests.");
            completed = false;
            grid.RequestPath(owner, from, to, profile, r => { result = r; completed = true; });
            grid.enabled = false;
            // Runtime-only MonoBehaviours do not receive lifecycle callbacks in Edit Mode.
            typeof(MiningNavGrid).GetMethod("OnDisable", Private).Invoke(grid, null);
            Check(completed && result.Status == PathStatus.Canceled && !MiningNavigation.PathfindingAvailable,
                "Disable must cancel pending work and clear availability.");
            completed = false;
            grid.RequestPath(owner, from, to, profile, r => { result = r; completed = true; });
            Check(completed && result.Status == PathStatus.Unavailable, "Disabled backend must reject requests immediately.");
            grid.enabled = true; grid.Rebake();
            Check(grid.TryFindPath(from, to, profile, route), "Re-enabled backend must recover.");
            var minerObject = new GameObject("Profile miner"); SceneManager.MoveGameObjectToScene(minerObject, scene);
            minerObject.SetActive(false); var capsule = minerObject.AddComponent<CapsuleCollider>();
            capsule.center = Vector3.up * data.ColliderHeight * .5f;
            minerObject.transform.localScale = new Vector3(1.5f, 1.25f, 1.5f);
            var scaled = NavigationProfile.From(capsule, data);
            Check(scaled.Radius >= data.ColliderRadius * 1.5f && scaled.Height >= data.ColliderHeight * 1.25f,
                "Navigation must use the scaled world capsule.");
            var oreObject = Cube(scene, "Inactive footprint", center + Vector3.up, new Vector3(2, 2, 1));
            oreObject.transform.rotation = Quaternion.Euler(0, 45, 0); oreObject.SetActive(false);
            Check(MiningPlacement.TryBounds(oreObject.transform, out Bounds bounds) && bounds.size.x > 2,
                "Inactive placement must use the rotated final collider footprint.");
            var target = Cube(scene, "Stand target", center + Vector3.up * .8f, new Vector3(1.6f, 1.6f, 1.6f));
            var targetCollider = target.GetComponent<Collider>(); Physics.SyncTransforms(); grid.Rebake();
            MiningNpc MakeOwner(string name)
            {
                var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene);
                go.transform.position = from;
                var miner = go.AddComponent<MiningNpc>();
                typeof(MiningNpc).GetField("npcData", Private).SetValue(miner, data);
                typeof(MiningNpc).GetField("capsule", Private).SetValue(miner, go.GetComponent<CapsuleCollider>());
                testOwners.Add(miner); return miner;
            }
            bool Lease(MiningNpc owner, Vector3 start, out StandReservation lease) => MiningStandReservations.TryAcquire(owner,
                target.transform, targetCollider.bounds, start, owner.Profile, .76f, 1, 0,
                p => { Vector3 delta = p - targetCollider.ClosestPoint(p); delta.y = 0; return delta.sqrMagnitude; },
                targetCollider.ClosestPoint, out lease);
            var firstOwner = MakeOwner("First owner"); var waitingOwner = MakeOwner("Waiting owner");
            Check(Lease(firstOwner, from, out var firstLease) && !firstLease.IsWaiting, "First owner must get a mining stand.");
            Vector3 stand = firstLease.Position;
            Check(Vector3.Dot(Vector3.ProjectOnPlane(stand - targetCollider.bounds.center, Vector3.up),
                Vector3.ProjectOnPlane(from - targetCollider.bounds.center, Vector3.up)) > 0,
                "The first mining stand must prefer the worker's approach side.");
            Check(Lease(firstOwner, to, out var stableLease) && stableLease.Position == stand,
                "A lease must remain fixed when its owner moves.");
            Check(Lease(waitingOwner, from, out var waitingLease) && waitingLease.IsWaiting && waitingLease.Position != stand,
                "An excess owner must receive a distinct waiting point.");
            Check(Vector3.Distance(waitingLease.Position, from) < .01f,
                "An excess owner outside the approach lanes should hold locally instead of crossing the ore cluster.");
            MiningStandReservations.Release(firstOwner);
            Check(Lease(waitingOwner, from, out var promotedLease) && !promotedLease.IsWaiting,
                "Releasing capacity must promote the FIFO waiting owner.");
            MiningStandReservations.Release(waitingOwner); target.SetActive(false); Physics.SyncTransforms(); grid.Rebake();
            int obsolete = 0, latest = 0;
            grid.RequestPath(owner, from, to, profile, _ => obsolete++);
            grid.RequestPath(owner, from, to, profile, _ => latest++);
            for (int i = 0; i < 200 && latest == 0; i++) update.Invoke(grid, null);
            Check(obsolete == 0 && latest == 1, "Only the newest request generation may complete.");
            var churn = Cube(scene, "Unrelated changing obstacle", center + new Vector3(4, 1, -4), Vector3.one * 1.5f);
            typeof(MiningNavGrid).GetField("searchNodesPerFrame", Private).SetValue(grid, 1);
            completed = false;
            grid.RequestPath(owner, from, to, profile, r => { result = r; completed = true; });
            for (int i = 0; i < 200 && !completed; i++)
            {
                churn.SetActive(i % 2 == 0); Physics.SyncTransforms();
                grid.MarkDirty(new Bounds(churn.transform.position, Vector3.one * 2)); update.Invoke(grid, null);
            }
            Check(completed && result.Success && result.Revision == grid.Revision,
                "Unrelated topology churn must not starve a route; output must carry its validation revision.");
            churn.SetActive(false);
            var intrusion = Cube(scene, "Recovery blocker", center + Vector3.up, new Vector3(1, 2, 1));
            Physics.SyncTransforms(); grid.Rebake();
            var escapeCapsule = firstOwner.GetComponent<CapsuleCollider>();
            Check(MiningPlacement.TryEscape(escapeCapsule, center, profile, 2, out var escaped) &&
                grid.IsCapsulePlacementClear(escaped, profile.Radius, profile.Height),
                "An invalid start must recover through a clear, monotonically improving connector.");
            intrusion.transform.localScale = new Vector3(8, 2, 8); Physics.SyncTransforms(); grid.Rebake();
            Check(!MiningPlacement.TryEscape(escapeCapsule, center, profile, 2, out _),
                "Sealed placement must fail explicitly rather than cross an arbitrary wall.");
            intrusion.SetActive(false); Physics.SyncTransforms(); grid.Rebake();
            var parked = MakeOwner("Parked worker"); parked.transform.position = center;
            typeof(MiningNpc).GetField("<NavigationStatus>k__BackingField", Private).SetValue(parked, NavigationState.WaitingForSlot);
            typeof(MiningNpc).GetField("isMining", Private).SetValue(parked, true);
            var activeMiners = (List<MiningNpc>)typeof(MiningNpc).GetField("ActiveNpcs", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            if (!activeMiners.Contains(parked)) activeMiners.Add(parked);
            Check(!grid.IsMinerCorridorClear(firstOwner, from, to, firstOwner.Profile),
                "Follower must not shortcut through a stationary worker.");
            Vector3 crowdedStart = center + Vector3.right * (parked.NavigationRadius + firstOwner.NavigationRadius - .02f);
            Check(grid.IsMinerCorridorClear(firstOwner, crowdedStart, crowdedStart + Vector3.right, firstOwner.Profile),
                "A worker inside neighbour clearance padding must be able to move away.");
            Check(!grid.IsMinerCorridorClear(firstOwner, crowdedStart, crowdedStart - Vector3.right * .1f, firstOwner.Profile),
                "Clearance escape must never move farther into a parked worker.");
            completed = false;
            grid.RequestPath(firstOwner, from, to, firstOwner.Profile, r => { result = r; completed = true; });
            for (int i = 0; i < 4000 && !completed; i++) update.Invoke(grid, null);
            safe = completed && result.Success;
            Vector3 previousPoint = from;
            if (safe) foreach (var point in result.Route)
            { safe &= grid.IsMinerCorridorClear(firstOwner, previousPoint, point, firstOwner.Profile); previousPoint = point; }
            Check(safe, "A replan must route around stationary workers, not repeat the blocked straight line.");
            var crowdWall = new List<MiningNpc>();
            for (int x = -6; x <= 6; x++)
            {
                var worker = MakeOwner("Crowd wall " + x);
                worker.transform.position = center + Vector3.right * x;
                typeof(MiningNpc).GetField("isMining", Private).SetValue(worker, true);
                if (!activeMiners.Contains(worker)) activeMiners.Add(worker);
                crowdWall.Add(worker);
            }
            Physics.SyncTransforms(); completed = false;
            grid.RequestPath(firstOwner, from, to, firstOwner.Profile, r => { result = r; completed = true; });
            for (int i = 0; i < 4000 && !completed; i++) update.Invoke(grid, null);
            Check(completed && result.Status == PathStatus.Crowded && !result.Success && result.Route.Count > 0,
                "Parked workers sealing an open world must produce an access request, not a traversable route or permanent unreachability.");
            var accessLease = new StandReservation();
            typeof(StandReservation).GetProperty("Owner").SetValue(accessLease, parked);
            typeof(StandReservation).GetProperty("Target").SetValue(accessLease, parked.transform);
            typeof(StandReservation).GetProperty("Position").SetValue(accessLease, center);
            var sharedLeases = (Dictionary<MiningNpc, StandReservation>)typeof(MiningStandReservations)
                .GetField("leases", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            sharedLeases[parked] = accessLease;
            var requesterLease = new StandReservation();
            typeof(StandReservation).GetProperty("Position").SetValue(requesterLease, to);
            typeof(MiningNpc).GetField("standLease", Private).SetValue(firstOwner, requesterLease);
            Vector3 beforeYield = parked.transform.position;
            Check(MiningStandReservations.YieldBlockingWorker(firstOwner, from, new List<Vector3> { to }, firstOwner.Profile) &&
                parked.transform.position == beforeYield && !sharedLeases.ContainsKey(parked),
                "Access recovery must release one blocker for safe holding allocation without teleporting it.");
            var holdingFree = typeof(MiningStandReservations).GetMethod("HoldingSpaceFree", BindingFlags.Static | BindingFlags.NonPublic);
            Check(!(bool)holdingFree.Invoke(null, new object[] { center, parked.Profile, parked }) &&
                (bool)holdingFree.Invoke(null, new object[] { center + Vector3.right * 4, parked.Profile, parked }),
                "A yielded worker must vacate the requested corridor even when its requester is walking to a waiting slot.");
            var passages = (System.Collections.IDictionary)typeof(MiningStandReservations)
                .GetField("yieldPassages", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            object passage = passages[parked];
            var accessPending = typeof(MiningStandReservations).GetMethod("AccessPending", BindingFlags.Static | BindingFlags.NonPublic);
            var requesterBody = firstOwner.GetComponent<Rigidbody>(); Vector3 requesterStart = requesterBody.position;
            bool heldBeforeCrossing = (bool)accessPending.Invoke(null, new[] { passage });
            firstOwner.transform.position = firstOwner.Profile.Root(to); Physics.SyncTransforms();
            requesterBody.position = firstOwner.transform.position;
            Check(heldBeforeCrossing && !(bool)accessPending.Invoke(null, new[] { passage }),
                "A yielded passage must stay held until the requester has crossed, not merely until a timer expires.");
            firstOwner.transform.position = requesterStart; Physics.SyncTransforms(); requesterBody.position = requesterStart;
            sharedLeases[parked] = accessLease;
            Check(!MiningStandReservations.YieldBlockingWorker(firstOwner, from, new List<Vector3> { to }, firstOwner.Profile),
                "A yielded worker must retain its cooldown instead of flip-flopping every request.");
            MiningStandReservations.Release(parked);
            foreach (var worker in crowdWall) { worker.gameObject.SetActive(false); activeMiners.Remove(worker); }
            intrusion.transform.localScale = new Vector3(1, 2, 1); intrusion.SetActive(true);
            Physics.SyncTransforms(); grid.Rebake();
            Check(!MiningPlacement.TryEscape(escapeCapsule, center, profile, 2, out _),
                "Overlap recovery must not relocate through another actor sharing the invalid start.");
            parked.gameObject.SetActive(false); activeMiners.Remove(parked);
            intrusion.SetActive(false); Physics.SyncTransforms(); grid.Rebake();
            var followerRoute = (List<Vector3>)typeof(MiningNpc).GetField("currentPath", Private).GetValue(firstOwner);
            Vector3 rootFrom = firstOwner.Profile.Root(from), rootTo = firstOwner.Profile.Root(to);
            followerRoute.Clear(); followerRoute.Add(rootTo);
            typeof(MiningNpc).GetField("pathWaypointIndex", Private).SetValue(firstOwner, 0);
            typeof(MiningNpc).GetField("routeStart", Private).SetValue(firstOwner, rootFrom);
            typeof(MiningNpc).GetField("steeringWaypoint", Private).SetValue(firstOwner, -1);
            var steering = typeof(MiningNpc).GetMethod("GetNavigationTarget", Private);
            Vector3 ahead = (Vector3)steering.Invoke(firstOwner, new object[] { rootFrom, rootTo });
            Vector3 advanced = rootFrom + (rootTo - rootFrom).normalized * (Vector3.Distance(ahead, rootFrom) + .25f);
            Vector3 refreshedAhead = (Vector3)steering.Invoke(firstOwner, new object[] { advanced, rootTo });
            Check(Vector3.Dot(refreshedAhead - advanced, rootTo - rootFrom) > 0 &&
                Mathf.Abs(Vector3.Distance(refreshedAhead, advanced) - Vector3.Distance(ahead, rootFrom)) < .01f,
                "Upgraded miners must keep a moving look-ahead, not reverse or stop at the previous cached point.");
            followerRoute.Clear();
            var progress = typeof(MiningNpc).GetMethod("TrackMovementProgress", Private);
            typeof(MiningNpc).GetField("awaitingCrowdAccess", Private).SetValue(firstOwner, true);
            typeof(MiningNpc).GetField("accessWaitStarted", Private).SetValue(firstOwner, Time.time);
            typeof(MiningNpc).GetField("lastProgressTime", Private).SetValue(firstOwner, Time.time - 60f);
            progress.Invoke(firstOwner, new object[] { firstOwner.Profile.Root(from) });
            Check(firstOwner.NavigationStatus == NavigationState.WaitingForCrowd &&
                ReferenceEquals(typeof(MiningNpc).GetField("standLease", Private).GetValue(firstOwner), requesterLease),
                "Normal stuck recovery must not change the destination while a requested passage is being cleared.");
            typeof(MiningNpc).GetField("accessWaitStarted", Private).SetValue(firstOwner, Time.time - data.CrowdAccessTimeout - 1f);
            progress.Invoke(firstOwner, new object[] { firstOwner.Profile.Root(from) });
            Check(!(bool)typeof(MiningNpc).GetField("awaitingCrowdAccess", Private).GetValue(firstOwner) &&
                typeof(MiningNpc).GetField("standLease", Private).GetValue(firstOwner) == null,
                "Crowd access waiting must still have bounded recovery when a passage cannot be cleared.");
            placementData = ScriptableObject.CreateInstance<OreData>();
            spawnData = ScriptableObject.CreateInstance<OreSpawnData>();
            var placementHost = new GameObject("Placement owner"); SceneManager.MoveGameObjectToScene(placementHost, scene);
            placementHost.transform.position = center; placementHost.SetActive(false);
            var spawner = placementHost.AddComponent<OreSpawner>();
            typeof(OreSpawner).GetField("spawnData", Private).SetValue(spawner, spawnData);
            var placed = Cube(scene, "Relocated ore", center + new Vector3(3, 1, 3), Vector3.one).AddComponent<Ore>();
            placed.Initialize(placementData, null, null, false);
            ((HashSet<Ore>)typeof(OreSpawner).GetField("activeOres", Private).GetValue(spawner)).Add(placed);
            Check(spawner.TryRepositionOre(placed, center + new Vector3(3, 0, 3), Quaternion.Euler(0, 45, 0), new Vector3(1.2f, 1, 1.2f)),
                "Rotated final-footprint relocation must accept a supported clear pose.");
            Vector3 acceptedPosition = placed.transform.position, acceptedScale = placed.transform.localScale;
            Quaternion acceptedRotation = placed.transform.rotation;
            intrusion.SetActive(true); Physics.SyncTransforms();
            Check(!spawner.TryRepositionOre(placed, center, Quaternion.identity, Vector3.one) &&
                placed.transform.position == acceptedPosition && placed.transform.localScale == acceptedScale && placed.transform.rotation == acceptedRotation,
                "An overlapping relocation must be rejected atomically, preserving the previous valid pose.");
            Check(!spawner.TryRepositionOre(placed, center + Vector3.right * 100, Quaternion.identity, Vector3.one) && placed.transform.position == acceptedPosition,
                "Unsupported or out-of-grid relocation must leave the ore at its previous valid pose.");
            return $"PASS: {passed} isolated navigation repair checks. No scene, currency or save changes.";
        }
        finally
        {
            var activeMiners = (List<MiningNpc>)typeof(MiningNpc).GetField("ActiveNpcs", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            foreach (var owner in testOwners) { MiningStandReservations.Release(owner); activeMiners.Remove(owner); }
            EditorSceneManager.CloseScene(scene, true); instance.SetValue(null, oldGrid);
            SceneManager.SetActiveScene(previous); UnityEngine.Object.DestroyImmediate(data);
            if (placementData != null) UnityEngine.Object.DestroyImmediate(placementData);
            if (spawnData != null) UnityEngine.Object.DestroyImmediate(spawnData);
            Physics.SyncTransforms();
        }
    }
}
#endif
