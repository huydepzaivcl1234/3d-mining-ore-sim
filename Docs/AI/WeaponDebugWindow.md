# Weapon test panel

Open Tools -> Debug -> Weapon Test, or F8. This is an Editor-only window; enter Play Mode and start gameplay first.

- Pistol / Rifle (test): selects the player's existing authored loadout and equips it immediately, including its gun, hand pose, animation layer, sight, muzzle and weapon data.
- Refill ammo: fills magazine and reserve for both existing loadouts using their current data.
- Resume: focuses Game view and locks the cursor again. The debug window releases the cursor while focused.

The rifle button bypasses the unlock gate for that selection only. It never grants the God reward, changes RifleUnlocked, writes the unlock save key, or changes the normal 1/2 keyboard gate. DebugSelect is compiled only with UNITY_EDITOR. Stopping Play restores the normal pistol-first startup. The window disables its controls outside Play Mode. No scene component or save-format change is needed.

Verified in Play Mode: normal locked rifle selection rejected, debug rifle selected while runtime unlock remained false, saved unlock key unchanged, pistol selected again, magazine/reserve refilled, and rifle button laid out at approximately 329 x 30 pixels. The test's temporary runtime unlock fixture was restored in finally; persistent progression was not reset.
