# Rifle ADS transition and combat, 2026-10-15

## ADS correction

The old presentation enabled sight-depth reach projection only when AimWeight
exceeded .999. A 101-sample sweep reproduced a 0.0583158 m discontinuity at raw
aim .99. Now resolve reachable hip and ADS endpoints before interpolation, apply
motion and kick, then retain the final common-grip reach guard. There is no
threshold that changes constraints only on the last ADS frames. The same sweep
now has max step 0.0016104 m at the middle of the transition and a centered sight.
This is a bounded geometric smoothness check, not a frame-rate or latency claim.
Existing aim blend duration, source pose, view distance, controller and input remain.

## Scene/data wiring

- Enabled existing PistolShooting and PistolShotFeedback on PlayerArmature.
- Added Muzzle under the current m4 model via Undo, using the barrel mesh's local
  maximum Z and 1 cm forward clearance: gun-local (0,.05,.7324).
- RifleWeaponData reuses existing PistolMuzzleFlash.prefab and PistolShotVfx.mat,
  with validated URP/Particles/Unlit flash materials. Pistol data is unchanged.
- Display name Rifle; automatic fire, existing .12 s cooldown and 30-round base
  magazine. Player stat/card bonuses still apply. Existing range/falloff retained.
- No pistol slide was bound to this rifle. Slide travel is zero; no fake moving
  receiver or entire gun mechanism. The current rifle has no reload action profile;
  R replenishes ammo after the existing timer, not a newly authored reload clip.
- Shared arm recoil, camera recoil, pooled flash/tracers/impact and damage/popup
  logic are existing project systems. No second combat manager or controller.

Tracers are visual hitscan feedback: damage is immediate, the visible segment
travels at data.tracerSpeed (currently 55 m/s) from Muzzle. It is not a physical
ballistic projectile. The inherited pool is bounded to 12 concurrent slots;
long-distance tracers can recycle during sustained fire. No pool/performance
improvement is claimed.

## Validation

Unity recompiled with zero errors. Live synthetic mouse input over 528 advancing
frames: 5 hip + 5 ADS shots, 10 flashes, 10 tracers, 10 impacts, 141.817 damage to
an isolated monster; max shared hand kickback .0282 m, max camera recoil 1.2253
degrees, support error .000052 m. Cooldown, holster and pause rejected firing;
reload started and completed with magazine full and reserve reduced.

Eleven original full-body regression cases passed over 740 advancing frames,
including pitch, walk/run, jumping, ADS and holster. Armed support error <.000111 m;
ADS sight approximately (.500,.500,.310). The depth reflects the user's current
pose/view settings, which were preserved. Shadow flag remained on.

A live capture was requested while a flash particle and one tracer were active
(26 advancing frames after the capture probe started), and inspected:
AgentPreviews/RifleShotLive.png. Both flash renderers use URP/Particles/Unlit and
the reusable instance is parented to Muzzle. No diagnostic materials were applied.

Tests and results outside Assets in the calling C-drive workspace's AgentPreviews:
RifleAimSweep.cs, RifleCombatPlayChecks.cs, RifleCombatResult.json,
RifleEffectCapture.cs. Tests use isolated GameSave stores, temporary input devices
and a temporary monster; those are removed/restored and Play is stopped afterwards.

Rollback snapshot: AgentPreviews/BeforeRifleCombat-20261015.unity in that workspace.
The live SampleScene is left dirty, not overwritten; Ctrl+S persists muzzle and
enabled component wiring. Only rifle data is asset-saved by this setup step.
