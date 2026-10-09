# Skullclaw animation integration

## Current behavior: 2026-10-09 death/dawn update

Claw trails and their component/settings have been removed at the user's request.
Earlier trail validation below is historical, not current behavior. Player sword
trails are untouched. At daylight, Skullclaw uses the existing encounter dissolve
and despawn path: combat stops, health bar hides, no kill damage or reward is issued.
An isolated live night-to-day check passed dissolution and removal. Existing melee
range gates, approach delay, retaliation and night-roll limits remain unchanged.

Assets live in `Assets/GameData/Monsters/Skullclaw`.

- `Skullclaw.prefab`: repaired model, CharacterController, existing monster AI/combat.
- `SkullclawData.asset`: independent species configuration. Edit attack speed, damage, range in Combat; contact frames and jump range/radius below.
- `Skullclaw.controller`: Idle, Walk, Swipe Right, Swipe Left, Jump Attack, Damage, Down. Runtime owns state selection; no automatic attack-loop transitions.
- `Animations/*.anim`: derived clips, preserving authored pose curves and removing horizontal visual drift. Idle and Walk loop; attacks do not.

Source mapping verified by hand motion:

| State | Supplied FBX | Contact (normalized) |
| --- | --- | --- |
| Idle | Mutant Breathing Idle | none |
| Walk | Mutant Walking | none |
| Swipe Right | Mutant Swiping (1) | 0.49 |
| Swipe Left | Mutant Swiping | 0.49 |
| Jump Attack | Mutant Jump Attack | 0.44, grounded required |

The animation files already have Skullclaw's 33-bone generic skeleton. Humanoid retargeting is unnecessary. Damage/Down retain Golem motions with binding paths adapted to this skeleton.

The jump commits a target-side stand position at attack start, limited by Jump Range. The original vertical arc is extracted to Jump Height and applied through the same CharacterController that owns movement. Static obstacles can stop the jump. Damage is a single circular area contact centered on the actual landing location, not the hoped-for target location; walls block damage. A dodging target can evade it. Player and chest are checked separately, with no duplicate target damage. Death, disable and state cancellation stop jump movement.

Setup menu: `Mining Simulator / Setup / Skullclaw Mutant Animations`.
Isolated validation: `Mining Simulator / Validation / Skullclaw Three Attacks (isolated)`.
Tests: `SkullclawTests`.

The resource roster now has a separate nightSkullclaw entry, excluded from ordinary daytime waves. SkullclawData defaults to 30% chance, rolled once after 5% of the night. At most one successful spawn per night, shared across spawn zones using this roster; failed placement/full field retries do not reroll, and killing it does not replace it that night. Existing field cap and spawn clearance remain active. Chance and start progress are editable in SkullclawData. Save data and PlayerPrefs are not changed.

Claw ribbons reuse the non-particle sword-ribbon renderer. Six segments bind to the corresponding hand bones using baked claw geometry endpoints. Only the three claws on the attacking hand emit between normalized 0.32 and 0.64; idle, walking, jump, death, disable and rune time clear them. Material, tint, duration and window are editable in SkullclawData.

## Verified results

### Player-hit retaliation

Latest distance-rule correction (supersedes the previous forced retaliation leap and scheduled third attack): close-range combat always alternates right/left swipes. The jump is exclusively an approach attack outside capsule-to-capsule melee range and inside Jump Range, after `jumpApproachDelay` (default 0.5 seconds) continuously in that band. Returning to melee range, leaving jump range, changing targets, starting an attack or disabling/reinitializing the monster clears the approach timer. Player damage changes aggro but never forces a close-range jump. Existing attack recovery, collision-safe landing and three-second pursuit expiry remain intact.

Health bar correction: Skullclaw prefab previously had a null `healthBar` reference. It now uses a copied, explicitly bound Golem MicroBar layout above its own mesh bounds, including health/name/level references. The existing ordinary-monster convention remains: show when damaged, hide at full health. Camera capture inspected at `Temp/SkullclawReview/range-gate-health.png`.

Latest validation: 16 test methods passed through Editor invocation with NUnit assertions. Isolated Play Mode passed right/left/right close combat without a leap, a distance-gated approach delayed at least 0.5 seconds, a visible damaged health bar, and retaliation expiry after 3 seconds outside detection range. SampleScene was restored and Console reported no errors. The earlier forced-close-jump test below describes the previous implementation, not the current expected behavior.

Player-sourced damage temporarily overrides chest interception targeting. Combat monsters pursue the attacker; Skullclaw uses its distance-gated approach jump without consuming the waiting swipe. Mushnight remains a fleeing thief. Committed attacks/landings finish before switching targets; repeated hits do not bypass recovery or restart the outside-range timer. Returning inside Detection Range resets that timer. After continuously leaving range for `combat.retaliationForgetSeconds` (default 3 seconds), or the attacker becoming invalid/dead, the override ends and chest targeting resumes. Rune freezes pause the timer. Each species can edit this duration in its own combat GameData.

Validation: four new retaliation test methods and nine existing Skullclaw methods passed when executed in the Editor with NUnit assertions (not a new Test Runner run). The targeting fixture verified selecting the attacker over the chest and returning to the chest on expiry. Isolated Play Mode passed: actual player-sourced damage forced a close-range approach jump, and pursuit expired after 3 seconds outside range. SampleScene roots were restored, no save/progression systems ran, and no Console errors were reported. A full crowded combat/night-spawn soak was not run for this change.

Combat pacing: each complete Skullclaw attack returns to Idle and begins the configured attack cooldown after the clip, whether hit or miss. No automatic same-frame chain. Out-of-range targets do not consume the pending melee step. A target outside swipe range but within Jump Range can trigger an approach jump immediately after recovery; this preserves a waiting right/left step, while a normally scheduled third attack wraps back to right. Beyond Jump Range, existing navigation closes the distance rather than attacking empty air. Existing attack speed/cooldown data are preserved.

Pacing validation: nine Skullclaw test methods passed in the Editor. Live retreat/re-entry case passed with contact counts 0/1/1: missed right swipe, left swipe after re-entry and recovery, then grounded jump; both claw trails passed. Separate live approach case passed: distant target triggered jump, retained pending right swipe and returned to Idle. No Console errors in these checks.

- Night/trail update: all eight Skullclaw test methods passed when invoked in the Editor with NUnit assertions. The earlier Test Runner run was 7/8: its radius boundary assertion hardcoded 2.3 m while the authoring asset had been changed to 3 m. The test now respects configured radius; the authoring value is preserved.
- Live isolated combo after the trail update: hits 1/1/1, grounded landing, returned to Idle, both swipe ribbons observed and cleared in Idle. Front camera capture inspected at `Temp/SkullclawReview/claw-trail-front.png`.
- Night roll and roster wiring tested; a full live night-cycle spawn was not simulated.

- Edit Mode: 9/9 passed (six Skullclaw tests and three existing ForestGolem regression tests).
- Isolated Play Mode: right/left/jump each caused one hit; leap lift 2.09 m and travel 2.87 m; grounded on landing.
- Blocking wall: travel stopped at 0.67 m, no jump damage through the wall, returned to Idle.
- Target dodge: no jump damage after the target left the committed landing area, returned to Idle.
- Death during flight: CharacterController disabled, jump movement and area damage cancelled, Down state selected.
- Jump pose inspected in a camera capture. Original scene roots restored after isolated checks; no gameplay save systems ran.
- Console still reports an existing missing-script warning; it is not repaired by this animation integration.

Asset admission: the user explicitly authorized the repaired authored model and all five supplied FBXs despite absent license manifest/receipt. The game-dev CLI was unavailable, so admission used a scoped copy to a new folder, verified source/destination SHA-256 pairs, recorded below. This verifies copied bytes, not ownership or runtime suitability.

| Imported file | SHA-256 |
| --- | --- |
| Skullclaw.fbx (Skullclaw_Repaired.fbx) | 946A81BAE1880129F3741B21137FB514B4E07E04145D64F3BDA6CE90CACBCD34 |
| Mutant Breathing Idle.fbx | 7985151B184C9A8E4063C286344F7C2C71735ADF3248E496A41044B8FE17B23E |
| Mutant Swiping.fbx | 532953517581A1568A5798DDDFA16FD58C1E1AB20EB17CFE4472740B5D78F4C6 |
| Mutant Walking.fbx | 8285A5C442DF21B6ECAE6A3B9B7B68DCD429E5BF6BA53D30B02F6C338395C4F1 |
| Mutant Swiping (1).fbx | 5C10D7839D53382E3D44544D0BBB0C26F362B9AD06E3540E2AAA951D2416DA19 |
| Mutant Jump Attack.fbx | FBF363C6C8CA282D9D5DBF6B1CD6C9687962A147F8583FA070F479313C5F7365 |
