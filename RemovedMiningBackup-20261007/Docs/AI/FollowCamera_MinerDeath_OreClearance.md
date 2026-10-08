# Follow camera, stamina visibility, miner death and ore clearance

## Changes

- MiningOrbitCamera now smoothly recentres behind the player heading. Manual orbit pauses
  recentering for one second after release. Soft aim's ExternalFacing and camera cinematic/menu
  overrides still take precedence. Collision and combat distance/FOV remain unchanged.
- MiningPlayerStaminaHud hides its authored graphics when the main menu or a blocking modal
  is open, even if it was created after the menu opened. It does not overwrite CanvasGroup
  alpha managed by the existing panel coordinator.
- A miner at zero HP is disabled and destroyed, releases its mining reservation, and no longer
  recovers after KnockoutSeconds. Miners spawned by NpcShop have an explicit owner. That owner
  decrements and saves the purchased population through the existing removal API; no refund
  or deduction of earned money occurs. Unowned scene miners do not change purchased counts.
- Ground placement ignores monster/player colliders. A new ore pushes overlapping living
  monsters sideways with their existing CharacterController. If six bounded clearance passes
  cannot separate them safely, the ore is returned to its pool and spawning is deferred.
  Guaranteed ore requests remain queued when placement fails.

## Manual regression checks

1. Walk and turn: camera smoothly returns behind the player. Hold RMB to orbit, release and
   check recentering. Soft aim, menu pause, death spectator and walls must retain their behavior.
2. Open main menu/settings and a blocking inventory/shop panel: stamina graphics disappear.
   Return to gameplay: graphics return with current stamina and low-stamina blink intact.
3. Spawn an ore at a monster's position: ore rests on terrain, monster moves sideways without
   crossing a wall or ending on top. Block all exits: ore spawn must be deferred.
4. Let a monster attack a purchased miner: retain the two-second warning. A fatal hit removes
   the miner immediately, updates the HUD count and frees capacity. Reload preserves the reduced
   purchased count; earned money is unchanged. Do not reset player save data to perform this test.

## Validation status

Unity MCP reconnected and the Editor compiled the changed assembly. Play Mode checks passed:
main menu hid all three stamina graphics and closing restored all three; setting player heading
to 90 degrees let the camera converge to 89.99995 degrees; overlapping ore/monster colliders
separated by 1.58 metres horizontally with zero vertical movement and no remaining penetration.
A temporary miner went from 20 to 15 HP, then a fatal hit disabled it and changed its temporary
shop count and saved count from one to zero. Repeated damage returned zero without another
count decrement. The original saved miner count was restored in the same test call's finally
block. Temporary objects were removed. Console reported zero errors after these checks.
These are focused runtime checks, not a complete gameplay or wall-clearance playthrough.
Existing unrelated Scene/TMP edits were left untouched. The delivery ZIP excludes SampleScene
by project rule.
