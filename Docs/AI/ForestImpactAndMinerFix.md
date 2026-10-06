# Forest grass impact and miner approach recovery

## Integration

- The inspected `ForestGrassImpact.zip` assets live in `Assets/Art/ForestGolemImpact`.
- `ForestGolemRewards.asset > Combat > Forest Golem > Impact Prefab` references the grass prefab. The existing slam callback starts it at hand contact; do not add a second emitter/event to the monster.
- The ability owns the visual progress. Radius, lifetime, pause and boss scaling match the existing damage wave. Existing charge/burn, attack timings and authored tuning are preserved (extra radius 2 m, wave duration 0.65 s).
- Rings, grass density, blade size, leaf count and colors remain editable on the impact prefab. The standalone prefab retains preview/autoplay. Combat reuses one instance per monster and cancels it on disable/death; no extra damage is emitted by the visual.
- The visual samples ground at the center. Like the supplied ZIP, it is planar, not a mesh conforming to every bump of the Terrain.

## Miner bug

Live capture: miner at `(-1.30,-0.10,-3.62)` had a distant Coal target, `Waypoint 1/1`, and a path destination equal to its current position. Its cached valid stand point measured `0.760000169 m`, just beyond the strict `0.76 m` comparison. Re-selection failed and requested a successful zero-distance route; that reset its progress timeout repeatedly.

- Cache validation now uses the same squared-distance numerical tolerance as stand-point selection.
- A failed approach search does not request a path to the miner itself.
- Path completion no longer resets the movement progress clock; only physical progress and explicit recovery reset it.
- Unreachable automatic targets still retry, then receive a cooldown. Commanded targets retain their existing retry behavior.

## Verification (Unity Editor 6000.5.3f1)

- Captured the live failing miner before recompilation, without resetting saves or purchasing miners.
- Editor compiled; Console returned no errors before and after tests.
- Isolated Play regression: a slightly out-of-boundary cached stand point is retained, miner travels roughly 8 m to Coal and mines durability 20 to 0 using its actual Animator impact relay.
- Failed ground/stand-point search retries and cools down the target instead of getting trapped in zero-length routes.
- 22 Forest Golem Play checks passed: contact timing, one hit per wave, radius boundaries, charge after three real hits, remote burn, cancellation, prefab wiring, generated mesh, pause, and correct 1.5x world scale for boss VFX.
- Rendered and visually inspected the grass-ring effect in Unity (not the offline GIF).
- SampleScene was not saved or changed. Temporary test scene and objects are removed. No save keys/economy/progression were modified by the isolated tests.

## Handoff

ZIP paths are relative to the project root and include metadata. No SampleScene is included. The original ZIP emitter is available for independent authoring but is not attached to Forest Golem; gameplay already triggers the effect.
