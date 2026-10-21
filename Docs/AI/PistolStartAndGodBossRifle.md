# Starting pistol / day-50 God Boss rifle

- New sessions start with the authored M1911 pistol drawn. Q still draws/holsters.
- 1 selects pistol; 2 selects rifle only after its reward is earned. UI/pause/death block keyboard weapon changes through the existing CanFire gate.
- The original full-body player Animator/controller and input are retained. Both weapon poses use the same body/presentation/shooting components. Pistol and rifle keep separate ammo/reserve while switching, so switching is not free reload.
- PlayerWeaponProgression on PlayerArmature owns the two scene loadouts. RifleGameplayPose.asset is a copy preserving the current rifle pose and scene offsets; original rifle/pistol pose assets are not overwritten.
- Rifle unlock uses GameSave key Player.Weapons.RifleUnlocked.v1, missing = locked. Opening a new session still selects pistol; an earned rifle remains selectable. Reset Data clears the unlock and returns to pistol through WorldProgressReset.

## Hook up the future God Boss

The God Boss model, attack patterns and day-50 spawn encounter are NOT created by this change. Ordinary monsters/bosses are unchanged and cannot unlock the rifle.

When the God Boss is ready:

1. Put GodBossRifleReward on its combat root, alongside MiningCharacterHealth. Add hit colliders to that hierarchy. Do not put this reward component on ordinary bosses.
2. Assign Clock to the scene DayNightSystem (or allow its one-time lookup).
3. Enable/spawn this actor for the day-50 encounter. The reward records the clock day on enable, so a day-50 encounter can finish after midnight without losing its reward. An actor enabled on day 49 or 51 is not eligible.
4. Damage must reach MiningCharacterHealth.DealDamage with the actual player-owned source. The existing gun now accepts a GodBossRifleReward target as well as MushroomMonster. Destroying/despawning the actor, killing without player credit, advancing the clock alone, and ordinary boss death do not grant the rifle.
5. Health.Died grants the unlock once, saves it and selects the rifle. If using a custom health system instead, adapt it to this health/death contract; merely destroying the model is insufficient.

God Boss Day is editable on PlayerWeaponProgression and defaults to 50 as requested. No ordinary boss is relabeled as God Boss; no automatic day-only grant or spawn is hidden in this component.

## Scene handling

Scene wiring uses live Unity Editor operations and Undo. SampleScene remains dirty rather than overwriting unrelated unsaved user changes; Ctrl+S persists the pistol/loadout component wiring. Code and the new rifle gameplay pose asset are already written.

External rollback scene copy: calling workspace AgentPreviews/BeforePistolProgression.unity. Runtime validation uses AgentPreviews/WeaponProgressionChecks.cs and an isolated WeaponProgressionFixture.json save, not the user's profile. Reusable flash/tracer/reload helpers are rebound when selecting weapons; no second controller or FPS arm model.

## Validation

Unity compilation completed with zero errors. Runtime check passed over 345 advancing frames: pistol startup, rejected locked-rifle selection, two pistol shots/flashes, reload completion, support grip error <= .000056 m outside intentional reload release, God Boss enabled on days 49/51 rejected, day-50 uncredited kill rejected, ordinary boss notification rejected, credited day-50 God Boss health death grants rifle and activates its model/data, switching back preserves ammo, unlock survives GameSaveStore flush/readback, and reset relocks/selects pistol. Test used a temporary health/reward actor, not a newly authored God Boss model or attack AI. StartingPistol.png was captured and visually inspected in the calling workspace AgentPreviews. Original source poses were not overwritten. Existing historical audio teardown error remains outside this change.
