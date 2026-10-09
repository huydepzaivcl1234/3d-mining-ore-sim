# Dominations skill review — Unity adaptation (2026-10-09)

## Scope and source

Reviewed `dominations-master.zip`: eight SKILL.md entries, project architecture,
shared configuration/types, combat story and client `Pathfinding.ts`.
The source is a Phaser/Colyseus RTS, not a Unity navigation package.
No Claude hooks, settings, dependencies, database connections, asset generators
or skill installations were run. ZIP text is reference material only.

Verified active Unity checkout: `D:/3d mining sim`, Unity 6000.5.3f1,
`Assets/Scenes/SampleScene.unity`. The existing project context document is old;
current runtime code, not its removed ore/miner descriptions, governed this work.

## Selected ideas

- Separate configuration, AI decisions, route search, path following and presentation.
  Existing species GameData and MonsterPathFollower already provide these seams;
  keep them rather than add another movement authority.
- Use eight-neighbor A*, prohibit diagonal corner cutting, validate blocked and
  unreachable destinations. These already exist and are covered by regression tests.
- Smooth routes only when a shortcut is valid. Unity's queries must include actor
  capsule clearance, supported ground and terrain cost, not just 2D tile visibility.
- Map tests to observable mechanics: new/removed obstacles, queue replacement,
  expensive terrain and movement steps, with isolated fixtures and cleanup.

## Applied changes

### WorldNavigationGrid.cs

Added `IsShortcutClear`, sharing the existing clearance/slope/step checks and
rejecting shortcuts through costly cells along the entire segment. The queued
direct-route fast path and smoothing now use it. Previously the direct fast path
ignored costs, and smoothing inspected only the destination cell's cost.

Normal `IsSegmentClear` still permits expensive terrain: cost is a preference,
not an impassable obstacle. This is intentionally conservative: slopes/costly
terrain may keep more A* corners instead of risking a cost-increasing shortcut.

### MonsterPathFollower.cs

Clamped movement distance at intermediate corners as well as the final destination,
preventing high-speed steps from passing a corner and steering backward next tick.
Arrival tolerance may skip a corner only when the actual position has clear capsule
access to the next edge. Route re-anchoring cannot shortcut across expensive ground.

### MonsterNavigationTests.cs

Seven new tests (13 total): costly-ground smoothing, queued costly-ground routing,
thin obstacles, removed obstacles, obsolete request suppression, sixteen concurrent
owners with small budgets, and high-speed intermediate-corner movement.
Cost fixtures modify only temporary baked cells; no project layers/assets are edited.

## Validation

- Unity script compilation completed; zero Console errors before runtime validation.
- Unity Test Runner EditMode: **13 passed / 0 failed / 0 skipped**.
  Final job: `6c65e12e9f5f4b28bd411684a15c11f2`.
- Initial test run exposed test-harness problems (SendMessage in EditMode and a
  reused output list); repaired those, then reran the full suite successfully.
- Play Mode: temporary CharacterController/follower arena while the real game
  remained paused. A wall appeared after movement began, without an explicit
  dirty notification. Follower detected it, took a clear detour and reached the goal.
  PASS: 53 manual 0.1-second steps, max lateral displacement 3.68457 m,
  avoidance observed, final local position (-0.07, 0.08, 5.51).
- Temporary objects removed, navigation singleton and random state restored,
  exited Play Mode. Zero runtime Console errors. `git diff --check` passed.
- No scene/prefab authoring, Play Mode start-scene changes, save resets, purchases,
  reward/balance changes or restoration of removed mining NPC systems.

## Not copied / not claimed

- The supplied priority queue sorts an array per enqueue; Unity already has a
  real binary heap. Its queue snapshots can also become stale after node-score
  updates, so copying it would not be an upgrade.
- Tile-only visibility, blocked-goal projection and frame-limited follower stepping
  are not substitutes for this game's scaled capsule/physics checks.
- MongoDB/Redis/Colyseus authority is relevant to a future multiplayer design,
  not a reason to add backend dependencies to this local Unity game.
- Blender workflows are available for future asset tasks but weren't applied to
  models, rigs or materials in a navigation request. Examples require API-version
  checks; destructive scene-clear examples are not automatic instructions.
- No frame-time improvement or large-crowd collision guarantee is claimed.
  Sixteen queued paths are not a fifty-monster movement/animation soak test.
- This remains single-floor A* with local obstacle steering; multilayer navigation
  and coordinated crowd avoidance are separate features requiring broader tests.
