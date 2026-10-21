# Responsive jump and shot VFX — 2026-10-14

## Changes

PlayerController1 now receives a latched JumpPressed notification from the existing InputManager action. Update consumes the request, triggers the visual animation and assigns take-off velocity immediately. JumpAddForce remains callable for old clip events, but is idempotent after the request has been consumed. Jump strength still comes from PlayerCoreStatsData, with the same impulse/mass relationship. A held key does not automatically repeat jumps. Air steering, gravity, camera sensitivity and project physics timestep remain unchanged.

Ground movement physics now uses raw normalized movement input and data speed; Animator velocity parameters retain their visual smoothing. No change to input action bindings, player Animator or saved pose.

PistolShooting keeps immediate hitscan damage. A camera ray chooses the aim point, then an own-collider-filtered muzzle ray checks nearby obstruction. PistolShotVfx displays a short moving tracer from the muzzle to that point and emits a small spark impact on arrival. This is a visual tracer, not a physical ballistic projectile: slowing tracerSpeed does not delay damage. Impact triggers on any valid solid collider (including AI/enemies); existing damage eligibility was not expanded to friendly NPCs/towers.

The visual pool is fixed at 12 slots, precreated once. No per-shot object creation/destruction, no permanent decals, no debris accumulation. Effects clear when shooting is disabled and dispose with the owner. Shader/material reference is stored in the weapon asset, not found dynamically during firing.

## Authoring

PistolWeaponData.asset now exposes shotVfxMaterial, tracerSpeed, tracerLength, tracerWidth, tracerColor and impactColor. Current visible travel default is 55 m/s, independently of instant hitscan damage. PistolShotVfx.mat uses the existing URP Particles/Unlit shader. No scene wiring or scene save was required.

## Evidence / validation

- Live actual Space action probe: before fix 565.4 ms; after fix 9.13 ms from action callback to observed positive Rigidbody velocity. This is one Editor-run sample, not hardware-to-display latency or a guarantee of zero milliseconds. Physics timestep remains approximately 20 ms.
- Holding Space through landing and invoking the old clip event once: one take-off, 1.518 m rise, landed, passed in a clean Play session. An earlier chained synthetic-keyboard run had zero take-offs and was not counted as a pass.
- Three actual shots at a temporary solid collider: 3 tracers, 3 impacts, up to 10 live particles, pool 12 before/after, no active tracers after completion. Runtime screenshots were inspected.
- Isolated miss visual check: 1 tracer, 0 impacts, 0 active after completion.
- Combined diagonal run, look, RMB, actual R reload and jump test passed: movement 5.860 m, jump 1.518 m, grip error below 0.001 m, correct ammo transfer for the user's retained seven-round magazine.
- Unity compiled successfully; final Console checked separately. Temporary test geometry was destroyed and Unity returned to Edit Mode.
- SampleScene disk hash unchanged for this turn: 71F231284C28DBF720A50E7149D71055C0BB7EE6A522DAC156D6DF33C0DD889B. Saved hand-pose hash unchanged. No project settings modified.

## Limits

No hardware/controller input latency benchmark, player build, slope/edge matrix, real enemy hit visual capture or wide performance benchmark was performed. The visual impact path is shared for all solid colliders. Transparent tracer appearance is stylized and editable; sparks are generic, not per-surface effects. No promise of literal zero-millisecond control response.
