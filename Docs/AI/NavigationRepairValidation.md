# Navigation repair validation

Connected project: `D:/3d mining sim`, Unity `6000.5.3f1`, based on main commit `a35ed2f53d4504d61770ca92e8f8dba72216e6f1`. Implementation was staged in the chat workspace and copied to the connected project with scoped backups. No branch, commit or push was made.

## Implemented areas

| Owner | Responsibility |
| --- | --- |
| Ore / OreSpawner / MiningPlacement | Anchored collision geometry, visual-only hit punch, inactive final-footprint placement, atomic validated relocation, grounded capsule spawning and bounded safe escape |
| NavigationProfile / MiningNavigation / MiningNavGrid | Per-actor dimensions, one A* authority, swept edge/smoothing checks, independent dirty/search budgets, generations, cancellation and current-geometry output validation |
| StandReservation / MiningNpc.Targeting | Shared stable stand leases, unclaimed-capacity preference, globally distinct waiting space, FIFO promotion and target-switch stability |
| MiningNpc.Navigation / MiningNpc / MiningCrowdCoordinator | Current-position route anchoring, remaining-distance braking, progress/watchdog recovery, reciprocal crowd constraints, passage claims and one Rigidbody motor |
| NpcShop / MiningChest / MushroomMonster | Safe asynchronous saved-population restoration, refund preservation and compatibility with shared navigation dimensions/capacity |
| Editor checks / selected-miner Gizmos / isolated soak scene | Repeatable checks and explicit runtime state without economy/save services |

## Verified

- Editor compilation completed with zero Console errors after the last successful source deployment.
- All **28 isolated real-physics checks** passed: connectivity, wall separation/detours, actual-pose overlap rejection, correct support, smoothing, bounds, dirty/search fairness, cancellation, unavailable/re-enabled grid, scaled capsules, rotated inactive footprints, stable stand ownership, distinct local holding, FIFO promotion, request generations, topology churn, safe recovery, sealed-recovery rejection, stationary-worker rerouting, protection against follower shortcuts through parked workers, actor-safe recovery, accepted rotated relocation and atomic rejection of overlapping/unsupported relocation.
- Twelve isolated ore roots/scales/collider bounds were unchanged after ten repeated damage calls per ore and subsequent frames.
- `git diff --check` passed.
- SampleScene was not saved. Its on-disk SHA256 remained `E2753CC55E4D078E7B8214F1D269348E242AF6F69F15BC17164B68880B751572`.
- No purchases, currency deductions, progression changes, PlayerPrefs resets or production-scene Play tests were used for validation.

## Soak iterations

An early fixture incorrectly placed its final spawn row inside ores; those measurements were discarded. A corrected fixture exposed repeated requests to arrived holding points and braking just outside arrival distance; these were fixed. A later dense run completed with no overlap/invalid samples but still had excessive queued retries, so it was not treated as passing crowd acceptance.

After preferring ores with unclaimed capacity and local holding outside approach lanes, a direct 100-miner follow-up drained its path queue. At approximately 218 seconds into that phase: 20 mining, 63 waiting, 10 moving and 7 yielding; no overlap or invalid samples were recorded during the sampled two-minute period. This direct test was not the final five-population soak.

The next 16-miner phase revealed prolonged yielding near stationary miners. Route snapshots and follower corridor checks were extended to respect stationary worker bodies, rather than returning and shortcutting the same static-only route. Two isolated regressions were added before the final soak.

The stationary-crowd-aware five-population soak completed with 590 samples, zero overlap samples, zero invalid-placement samples, 4,968 synthetic mining impacts and a maximum observed queue age of 5.63 seconds. Dense 100-miner traffic did not settle sufficiently (44 requests still pending at the later 650-second snapshot), so this was treated as another congestion finding rather than full crowd acceptance. Holding allocation was then extended to keep mining transit lanes clear and search wider local rings.

The final follow-up was intentionally stopped before claiming acceptance: a dense 100-miner run still showed substantial queue congestion and high frame time while holding-space allocation was being tuned. The last three tuning edits (parked-worker classification in route snapshots/follower checks, plus the bounded stand-allocation queue and its isolated-test quota) are present in the deliverable staging tree, but the connected Unity checkout could not be refreshed after the environment's MCP approval/usage limit was reached. They therefore require one more Editor compile and short 100-miner soak before being called runtime-verified.

## Follow-up after MCP access resumed

The three staged scripts were deployed and all 28 isolated checks passed again. The Play Mode Start Scene override was cleared and the Editor was returned to Assets/Scenes/SampleScene.unity, outside Play Mode. A further FIFO repair removes owners whose allocation has become unnecessary from the stand-allocation queue. The short 100-miner follow-up recorded zero overlap and invalid-placement samples, but 79 pending paths and approximately 82.6 ms average frame time. Dense crowd acceptance remains unresolved; this follow-up supersedes the earlier staged-only deployment limitation and is not a passing performance result.

## Remaining limits

- The opt-in test scene uses primitive actors and synthetic mining impacts. It does not verify authored animation events, production-model leg alignment, sounds, rewards, chest/lucky-block gameplay or monster combat in SampleScene.
- Isolated checks run through the Editor validation entry point; their count is not an NUnit Test Runner count.
- Overlap checks are one-second samples of capsule-center separation; they do not prove every possible transient collision is absent. Historical runs sampled interpolated transforms. The final fixture reports Rigidbody poses in `invalidSamples`/`overlapSamples`, and preserves separate `interpolatedInvalidSamples`/`interpolatedOverlapSamples` for render poses. Physics and rendering must not be conflated at clearance boundaries.
- Passage claims currently coordinate quantized narrow tiles, not a precomputed entire connected narrow corridor. Long opposing single-file traffic and complex corridor bends need dedicated gameplay validation and may require connected-corridor ownership.
- The solver accelerates preferred velocity but safety constraints can override acceleration. Hard acceleration-limited reciprocal planning and full visual smoothness across every upgrade/frame-rate combination are not proven.
- Frame observations include startup/import overhead. No equivalent before-change performance baseline or standalone player build was recorded; no performance improvement is claimed.
- The earlier tool-limit/staged-only statements above describe historical iterations. The October 7 dense-crowd follow-up deployed and compiled the revised scripts successfully; its current results are recorded below.
- Navigation remains one supported surface per X-Z location, not a multilayer bridge/cave solution.

## October 7 dense-crowd repair

All revised source was deployed to the connected Editor. Repairs include shared static-clearance caches, direct-path admission, bounded request admission, approach-side stand selection, waiting-lane revalidation, monotonic crowd-padding escape, route-visibility caching, opposing-traffic-only passage claims and stalled-claim rotation. A budgeted static fallback distinguishes `Crowded` access requests from genuinely unreachable geometry. One parked blocker at a time yields through safe holding allocation with a duration, cooldown and arrival requirement.

The 100-miner ten-minute candidate soak completed with **594 samples, zero overlap samples, zero invalid-placement samples, 19,647 synthetic mining impacts, maximum pending age 2.912 seconds, and zero outstanding requests**. At the post-test snapshot (620.08 seconds), all 36 usable mining positions were occupied and the remaining 64 miners were waiting for slots. There was one transient unreachable/idle sample during acquisition; it recovered. Average frame observation was 15.21 ms with a 1,358 ms maximum including setup/tool pauses; this is not a standalone performance benchmark. Test sampling stops at 600 seconds; the 620-second snapshot is not 620 seconds of collected data.

A read-only source review then identified two boundary cases absent from that normal-speed candidate run: caching a fixed look-ahead point could reverse an upgraded miner that outran it, and a worker yielding for a waiting-slot requester could reclaim its original blocking point. Both were corrected. Corridor visibility is now cached separately from the moving look-ahead; the yielded corridor is retained as an explicit holding-space exclusion. **39 isolated checks passed on the reviewed source**, including both new regressions, crowded-route classification and yield cooldown. The final source receives a separate 100-miner follow-up; it must not be described as the exact revision of the preceding ten-minute run.

An early reviewed-source follow-up counted 14 temporary invalid **render** poses. An additional paired pose probe captured a render-clearance boundary rejection while the corresponding Rigidbody pose was clear. A restarted 158-second probe run counted no invalid one-second samples but also caught one transient render-only rejection between samples. This motivated the final fixture's separate physical and interpolated counters rather than discarding rendering observations. It does not establish that every earlier render rejection was physics-clear; the final physical-pose follow-up supplies that evidence separately.

The next physical-pose follow-up reached 34 mining workers but retained an access retry. Investigation exposed a return-too-early handshake: a blocker could return when its minimum yield timer expired before the requester crossed, while normal no-route stuck recovery could change the requester's destination during clearance. The final source retains the yield until passage completion/destination abandonment and keeps the requester destination stable during bounded access waiting (default 15 seconds). **42 isolated checks passed**, adding passage completion, access-wait stability and timeout recovery. Earlier soak results are historical candidates, not blanket acceptance of this final revision.

No authored scene or ore prefab was saved or rewritten by this follow-up. Existing SampleScene/prefab edits were preserved. The current on-disk SampleScene hash is `081487DFDBE61160E1EA5AD1803F8F2205B2681AF3012D8532502A5A5C33BB5C`; the earlier hash above belongs to a previous validation snapshot, not this follow-up baseline. The test's Play Mode Start Scene override is cleared immediately after entering Play and again after stopping.

### Final deployed-source acceptance run

The final access-handshake revision completed its own ten-minute fixed-population **100-miner** soak. At the 605.60-second post-test snapshot: **36 mining, 64 waiting for slots, zero pending requests**. The 600-second collection window recorded **594 samples, zero physical overlaps, zero invalid physical placements, zero interpolated overlaps, zero invalid interpolated placements, zero stalled samples and 19,382 synthetic mining impacts**. Maximum observed path age was **2.832 seconds**. Frame observations averaged 15.73 ms, with a 1,384 ms maximum including startup/tool pauses; these are Editor fixture observations, not a production benchmark or proof of every transient frame.

This final run supersedes the earlier dense-crowd congestion/staged-only limitations for this fixture. All 42 isolated checks passed again after stopping Play. The Editor was returned to `Assets/Scenes/SampleScene.unity` with `playModeStartScene == null`; no scene save was issued. The remaining production-model, animation-event, sound/reward, complex-corridor and multilayer limitations above still apply.
