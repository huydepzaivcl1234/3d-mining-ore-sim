# Rune upgrades

The existing RuneStoneShrine prefab owns a world-space, camera-facing panel. No SampleScene changes are required. Approach to reveal the translucent boundary; enter to show five SVG bars side-by-side and pause world simulation. Player locomotion, camera and UI use unscaled time during the session. Leaving, disabling the shrine, death or opening the main menu restores session-owned state.

The horizontal panel uses a 2921 x 104 canvas at scale 0.0025, a 3 metre height offset and a 2.5 metre camera-facing horizontal offset to avoid the central stone. All values are editable in RuneUpgradeData. Each bar retains its eleven progress cells and existing purchase logic.

Static MeshColliders under the shrine use their authored mesh triangles at runtime, not convex hulls that fill platform recesses and block walking. Rigidbody-attached colliders are left unchanged. Near the rune, player stepOffset is raised to the editable nearbyStepHeight (default 0.45 metres), then restored when leaving or disabling the shrine. This accommodates the existing approximately 0.34 metre stair risers without globally changing player movement. Scene-added colliders and transform overrides are preserved; no SampleScene save is performed.

Edit Assets/Resources/RuneUpgradeData.asset to change radius, reveal distance, offsets, scale, ambience, purchase SFX, artwork, bonuses, caps and prices. Defaults: attack speed/damage/health +5 percentage points per rank, MR/armor +5 flat points, eleven ranks per track. First price is 10 Gem; later prices are ceil(base * growth^rank + rank), growth 1.5. Click a row to purchase one rank. Purchased trapezoids use that row's color.

Ranks use MiningSimulator.RuneRanks.v1, separate from XP/cards. Normal Rebirth retains ranks; full Reset Data clears them. Unsupported or malformed rune saves block purchasing rather than overwriting unknown data. Lowering a rank cap does not delete purchased ranks. Currency deductions use the existing wallet, never a duplicate balance.

Keep gameplay values in data and preserve serialized names/references. Keep simulation, progression and row presentation separate; avoid new per-frame scene searches. Starter Assets exposes an unscaled-time option without depending on game assemblies. Actors/projectiles do not resolve gameplay hits during a rune session.

Validation: Unity compilation without errors; 60 rank/cost/cap/save-roundtrip cases; isolated wallet purchase and insufficient-funds rejection using a temporary test key; isolated session enter/exit restores time and animator mode. Preview capture checks original SVG gradients, eleven progress cells and layout. No live player purchases, reset or full Play session performed. SampleScene file hash remains unchanged.

Follow-up validation: horizontal prefab preview has five bars at equal Y with 55 progress cells; isolated enter/exit confirms pause and restoration. A walking test using a separate local-physics scene was not run because Unity disallows creating that scene in Edit Mode. In-game stair traversal remains to be verified in Play; no live save was exposed to test gameplay.
