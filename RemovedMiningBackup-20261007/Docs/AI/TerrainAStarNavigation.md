# Terrain A* navigation

## Current repair

The miner/ore repair supersedes the older follower, index-only stand slots and lack of crowd separation described below. See [NavigationRepair.md](NavigationRepair.md) for the current authoritative movement chain, safe placement, shared stand leases, reciprocal crowd constraints, diagnostics and isolated validation fixtures. New grids now default to 0.5 metre cells; existing serialized settings are preserved. The historical investigation and test observations below are retained as earlier evidence, not validation of the new implementation.

This replaces the old NavMesh-first/reactive miner routing and the shared monster chase routing. It does not modify SampleScene, Terrain assets, prices, combat stats or the save format.

## Inspector

Select the scene object already containing **Mining Nav Mesh Builder** (normally Navmeshsurface). Its retained script/GUID now bootstraps the Terrain A* grid at runtime. The NavMeshSurface component remains because other systems use its mining-area bounds; there is no runtime NavMesh bake.

- Grid Cell Size: start with 1 metre. Smaller cells resolve narrow passages but use more memory and build time.
- Grid Actor Radius / Height: default miner clearance for cached occupancy. Each monster search checks its own scaled CharacterController radius and height, including smaller species; skin width is not added to body size.
- Grid Maximum Slope / Step: blocked Terrain slopes and maximum neighbour height difference.
- Grid Obstacle Layers: only blocking scenery/mineables. Actors, triggers and Terrain itself are filtered out of occupancy checks.
- Grid Bake / Search Budget: cells or search nodes processed per frame. Defaults 512 / 256.

For advanced settings, add **MiningNavGrid** to that same object yourself before Play. The bootstrap uses an existing grid without replacing its settings. This exposes manual area bounds, ground layers, difficult-ground cost, slope cost, stand-point sampling, smoothing and Gizmo limits. No package install is needed.

## Behaviour

- Cached ground cells use Terrain.SampleHeight + Terrain origin Y and GetSteepness. Non-Terrain ground can use the configured raycast layers.
- Eight-way A* rejects diagonal corner cutting and excessive height steps. Costs include distance, slope and difficult-ground layers.
- Ore, chest and Lucky Block instances receive a runtime MiningGridObstacle. It invalidates old/new bounds on enable, disable, collider changes and movement. Dirty occupancy updates reuse cached ground heights; there is no timed whole-grid rebuild.
- A queued search spans multiple frames. Replaced/canceled targets discard their previous tickets. Explicit whole-grid rebuilds retain outstanding requests. A bounded restart policy prevents unrelated obstacle motion from endlessly restarting a search.
- Miners reserve nearby eligible ore with a free stand point, then request reachability asynchronously. Failed automatic targets enter a configurable cooldown; commanded targets retry. They stop rather than fall back to steering through a blocked route.
- Stand points are checked against the same actual-collider distance used by mining, not just the AABB. A continuous final endpoint can sit between grid centers, inside mining range with stopping-distance margin. Cached endpoints are invalidated if that margin disappears. Animation impact events and existing reward/mining rules are preserved.
- Rigidbody following uses acceleration/braking, validated look-ahead and a checked next physics step. Monsters use the same queued service with species/boss-sized clearance and stand points inside attack range.
- Existing actor/ore physical collision-ignore rules remain unchanged; navigation now avoids ore as requested. Monsters can still physically queue behind each other at a crowded victim. This is not a crowd-flow/separation solver.

## Debug

In Play, select the navigation object with Gizmos enabled: green outlines are walkable cells, red outlines are blocked cells. Drawing is limited to a configurable radius around the Scene-view pivot and a maximum cell count. Miner route Gizmos show yellow A* waypoints and queued/waiting status.

Run **Mining Simulator > Validation > Terrain A Star** while stopped for isolated preview-scene topology/ground checks. They create no persistent scene objects.

During Play, `MiningGridNavigationChecks.RunLiveStandPoints()` checks real ore colliders,
capsule clearance and the miner's stopping margin. It requires a live miner and a finished grid bake.

## Regression investigation: 2026-10-05

Live reproduction found a miner stationary 1.067 m from Stone with a 1 m mining range.
The chosen grid center was 0.988 m from the real collider, but its AABB distance appeared
closer. The 0.12 m arrival tolerance exhausted the path outside mining range.
A Mushroom also stopped beside the portal: its actual 0.411 m radius / 1.097 m height
was clear, while inflated navigation dimensions reported an obstacle.

Fix: use actual mining-surface queries and continuous, clearance-checked endpoints.
Endpoint connections and eight-way expansion use the requesting actor's dimensions;
default miner occupancy no longer blocks smaller monsters. Portal collisions remain enabled.

Regression checks: Editor compilation passed and the eight topology checks passed.
In the gameplay scene, 14 real collider stand points passed clearance/stopping-margin
validation. A purchased miner travelled 19.18 m and fired 18 actual NPC damage events
over 20 seconds (animation-driven, no manual impact call). Its final mining distance
was 0.918 m against a 1 m range. The original portal start also produced a valid route
with the Mushroom's actual dimensions. An actual Mushroom then moved from that exact
blocked position (-21.920, 0.005, -1.180) to (-15.02, 0.07, -0.93), travelling 6.95 m
in six seconds. Its original runtime position was restored afterwards. A final live
stand-point check passed 15 actual collider endpoints.

During the first unprotected gameplay test, monsters killed the miner and the game's
normal save logic reduced its count. With explicit user approval, only that count was
restored to the original 1, without a purchase or progression reset. Further tests
temporarily suspended existing monsters and protected the miner, then restored all
runtime test changes and paused Play. No scene or prefab was saved.

## Validation performed

Unity Editor compilation and Console checked via Unity MCP. Eight isolated checks passed: open-grid connection, no diagonal corner cutting, disconnected wall, reopened corridor, excessive step rejection, out-of-bounds rejection, regional invalidation, Terrain origin/height and slope sampling (last two share one ground check).

Play verified a 40,000-cell grid on the actual 200x200 Terrain; a temporary miner travelled from outside mining range, entered mining state and applied damage to a test ore without a wallet/reward hookup. Twelve queued routes succeeded, including a batch retained through a full rebuild. Temporary Mushroom, Bat and a 1.35x Golem obtained chase routes and moved towards an isolated target. Temporary objects were removed and Play stopped afterwards.

No standalone player build or large-population performance benchmark was run. Single-height XZ cells do not support overlapping bridge/cave floors; those require separate floor grids or layered navigation. When an area's bounds/ground genuinely changes, use the existing RequestRebuild entry point; ordinary mineable spawn/depletion only marks dirty cells.
