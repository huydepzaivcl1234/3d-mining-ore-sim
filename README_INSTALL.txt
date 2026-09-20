WANDERING TRADER + NPC + ORE + NAVMESH FIX PACK
================================================

INSTALL
1. Exit Play Mode and close Unity.
2. Extract this ZIP into the root of the Unity project (the folder containing Assets).
3. Allow files to be replaced.
4. Reopen Unity and wait for script compilation.
5. Enter Play Mode. A new NavMesh bake is not required for the ore-rectangle fix.

LATEST ORE-SELECTION PATH COST FIX (V9)
- When an NPC needs an ore, every compatible active ore is tested through the global
  pathfinding backend instead of being ranked only by straight-line distance.
- Unreachable ores are skipped.
- The NPC reserves the ore with the shortest complete routed path.
- Straight-line distance is used only to break an almost-equal path tie, or as a safe fallback
  when neither NavMesh nor the MiningNavGrid A* backend is available.
- Path scoring reuses one waypoint buffer and runs only during target acquisition; it does not
  allocate a new List for every candidate and does not run every frame.

NAVMESH + CIRCULAR OBSTACLE FIXES PRESERVED FROM V8
- Ore and Lucky Block hierarchies are excluded from NavMesh source collection with
  NavMeshModifier Ignore From Build before every build/rebuild.
- NavMesh is no longer baked onto the top surface of rocks.
- Every mineable uses one compact NavMeshObstacle with Shape = Capsule.
- The circular obstacle radius is measured from the ore collider with only 0.04 world-unit
  padding, so shortest-path queries route tightly around each ore.
- Legacy rectangular/box obstacles are forced off before the circular obstacle is enabled.
- Ores spawned or reused from a pool are re-measured and configured automatically.
- Cosmetic ore hit-punch motion stays below the carving movement threshold, reducing NavMesh
  hole rebuilds while an ore is being hit.
- Ore colliders and the existing local avoidance/detour system still prevent NPCs from
  blindly walking through non-target ores.
- The mining target remains the closest point on the ore collider; no dot target is needed.

OTHER INCLUDED FIXES FROM THIS WORK SESSION
- Language/localization updates.
- Wandering Trader panel remains open after buying or selling.
- Trader offers have rarity-based stock and cannot trade when stock is exhausted.
- NPC/ore spawn overlap protection.
- Ore first-hit vertical snap correction after final spawn placement.
- NPC pathing and high-speed mining fixes included in the current working version.

TEST CHECKLIST
- In Play Mode, enable Scene Gizmos/NavMesh display: each ore should have one compact round
  cut-out, never a rectangular cut-out, and no cyan NavMesh surface on top of any ore.
- Watch several miners cross locations where ores were spawned; they should not slow down
  or become stuck, and their Path debug label should remain NavMesh.
- Confirm miners still detour around a different ore when it blocks the route.
- Buy/sell repeatedly in Wandering Trader and confirm the panel stays open and stock updates.

VALIDATION NOTE
- Static checks passed: no Git conflict markers, all V8 circular-obstacle files are preserved,
  and the new path-distance API has no stale call sites.
- Unity Editor/Play Mode is not available in the packaging environment, so perform the
  Play Mode checklist above after importing.
V10 UNDERGROUND AREA
====================
This package now also includes the complete Underground portal update. Read
README_UNDERGROUND_V10.txt for unlock requirements, behavior and the test checklist.
