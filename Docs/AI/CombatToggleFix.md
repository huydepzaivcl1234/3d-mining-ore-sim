# Rapid equipment toggle fix (2026-09-30)

## Evidence and correction

The authored controller shares DrawWeapon/SheathWeapon triggers between combat layer and Arms Layer. Arms Layer takes longer to finish its draw/sheath clips. Replacing the trigger during those clips let the combat layer return to Default while Arms Layer stayed in Combat, with the sword still drawn. This was reproduced with 2, 4, 6 and 10 toggles spaced 0.04 seconds apart on the actual Player Animator.

PlayerCombatInput now routes E through TryToggleCombat. A toggle is accepted only when both available layers have finished transitions and are in the expected stable Default or Combat state. Presses during draw, sheath or an attack are ignored, not queued. This is state-based, not a fixed delay. SetCombatMode remains available for lifecycle/death resets. Animator structure, masks, clip events, key bindings and locomotion remain authored as before.

## Validation

Unity Editor compiled with no new errors. A controlled Play Mode stress check sent 200 toggle requests over 20 alternating draw/sheath cycles: 20 accepted, zero mismatches between the combat layer, Arms Layer, combat mode and sword holder after settling. These checks explicitly advanced the actual Animator in 0.04-second steps to reproduce rapid input. Normal return to standing and subsequent draw were verified. Existing Missing Script warning remains unrelated.

Zone audio playback, data fields and preloading were removed; spawner AudioSource count was zero in Play Mode. Loot/drop sounds were retained. The forecast sprite has 4,719 colored opaque pixels and its pink mushroom portrait was visually checked in the actual HUD. No original audio, mesh or texture assets were deleted. Temporary icon-render objects were removed; Play Mode exited without saving SampleScene.
