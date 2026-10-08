# Monster combat GameData

Each species uses its existing asset under `Assets/GameData/Monsters/`:
`BatRewards`, `GolemRewards`, `MushranonRewards`, and `MushroomRewards`.
The asset GUIDs and prefab bindings are unchanged. The monster component now labels
its existing reference **Monster Game Data**.

## Authoring

Expand **Combat** on the asset. Health, regeneration, damage, attack animation
states/contact moments, sweep/area volumes, second attack, tracking, move/chase
settings, ground warnings, death delay, ranged state/range/release timing and
projectile parameters are authored here. Boss skills, defenses, status effects,
level scaling, encounter expiry and rewards remain in the same GameData asset.
Animator, muzzle and health-bar scene bindings remain on their components.

**Attack Speed** is a multiplier: 1 is original playback and cooldown, 2 doubles
attack playback and halves the authored attack cooldown. Boss haste multiplies it.
Idle, walk, damage and death playback retain their original animator speed.
The Inspector displays effective cooldown before boss buffs. Hit/release moments
remain normalized clip fractions, so faster attacks retain contact synchronization.

Combat stays bound to the original species even when a boss uses different reward
data. Spawned health/damage and temporary boss buffs are instance state; they never
write to shared GameData. New species assets expose an explicit enable button.

## Compatibility

Migration copied the four authored prefabs' values into their own assets. Hidden
legacy backing fields use `FormerlySerializedAs` to preserve prefab serialization
and provide a fallback for unmigrated assets. `combatSettingsVersion` distinguishes
migrated data from Unity's implicit default inline-serialized objects. Do not remove
this fallback until all custom species/assets are migrated. Changes to migrated
species must be made on GameData, not on hidden legacy fields.

## Validation performed

- Compared every migrated melee/navigation field against its original prefab value.
- Isolated Editor Play Mode: four species' health, regeneration, hit range, attack
  playback/cooldown, unaffected Idle, haste composition, invalid speed safety,
  boss reward override ownership and teardown; ranged/projectile data for Mushranon.
- Invoked nine combat-data regression cases and five existing boss-skill tests in
  the connected Editor. All passed; this was not a full project Test Runner suite.
- Editor compile and console: no errors. SampleScene file hash unchanged and its
  existing unsaved changes preserved. Temporary diagnostic scene removed.
