# Horde navigation frame budget (2026-10-16)

## Findings and changes

Live Editor profiling with 46 naturally spawned monsters identified WorldNavigationGrid.Update as the main bottleneck: approximately 21 ms of the 24 ms BehaviourUpdate workload. SRP rendering was approximately 1.6 ms. SRP Batcher was already enabled.

A temporary Play Mode static-batching experiment combined 529 small decoration meshes (250,192 vertices). Median frame time changed only from 31.42 to 31.29 ms, and total Editor draw calls remained about 5,146. This experiment was discarded on Stop; no static flags, mesh import settings, prefab or scene changes were persisted. Large tree batching would additionally duplicate substantial vertex data. No third-party draw-call reducer was installed.

Changes retained:
- Fast-path CharacterController results and cache repeated actor classification within a frame. Cache is cleared when the owning navigation grid is destroyed.
- Crowd avoidance no longer marks moving characters as changed world geometry. Static obstacle detection and replanning remain active.
- GameplayPerformanceData.navigationBudgetMilliseconds defaults to 2 ms. Bake, dirty-cell processing and queued path searches share this soft frame budget. A search expands at most eight nodes per slice before checking time again. Work continues on subsequent frames; zero disables the time limit. Individual native probes, route smoothing and completion callbacks can overrun the budget.
- Added Mining.NavigationUpdate and Mining.MonsterUpdate profiler markers for future diagnosis.

Designer setting: Assets/GameData/Resources/GameplayPerformanceData.asset -> Navigation Budget Milliseconds. Existing spawn caps and wave counts are unchanged.

## Verification

Unity 6000.5.3f1 Editor, existing scene, 46 naturally spawned monsters, same session with alternating unlimited/2 ms budget. Four phases, 240 captured frames each after a 2-second warmup. Enemies continued moving; player damage was temporarily disabled during measurement and reverted on Stop.

| Phase | Budget | Median frame ms | p95 frame ms | Mean navigation ms | Monsters moved >5 cm |
| --- | --- | --- | --- | --- | --- |
| A1 | unlimited | 28.69 | 39.34 | 18.22 | 44/46 |
| B1 | 2 ms | 13.34 | 16.55 | 2.39 | 44/46 |
| A2 | unlimited | 30.96 | 40.41 | 19.13 | 46/46 |
| B2 | 2 ms | 14.18 | 15.64 | 2.48 | 45/46 |

This is Editor telemetry, not a standalone build benchmark or a guarantee for arbitrary enemy counts. Moving actors and Editor activity vary between phases. The game-dev sealed-run harness was not available; no sealed hardware-performance claim is made.

All 13 existing MonsterNavigationTests assertions passed when invoked with fresh fixtures and TearDown in finally, including thin/new/removed walls, sealed targets, costly-ground smoothing, obsolete requests, 16 queued owners, scaled controllers and high-speed corners. Editor compilation passed. The authored scene was not saved and diagnostic objects were cleaned up.

External reproduction scripts are in the agent workspace AgentPreviews: BatchProbe.cs, HordeMarkers.cs, NavigationBudgetAB.cs, NavigationRegression.cs. Never run mutating load tests without confirming actual Play state. Do not serialize runtime fixtures into SampleScene.
