# Nearest in-range attack aim and camera rotation comfort

## Behavior

- Combat targeting uses AttackRange from PlayerStatsData and the same hit-origin,
  collider closest point and vertical band used by sword contact. It does not use
  the old 8/15 metre aim radii or screen-centre scoring.
- While in combat, the nearest living active mushroom is reacquired. Only an active
  attack rotates the player. No enemy in range means no assisted rotation; movement
  facing is released. F refreshes the closest in-range target rather than locking a
  distant enemy. Damage still occurs exclusively through the existing contact events.
- The non-allocating physics buffer has a registry fallback when full, so large
  crowds do not silently exclude the nearest enemy. Target selection never writes
  camera transforms.
- The single MiningOrbitCamera smooths and caps rendered rotation, including manual
  orbit and behind-player recentering. Existing camera collision remains authoritative.
- A runtime-owned URP Volume contains only CameraOnly MotionBlur, fades with angular
  speed, and clears for menus/pause/cinematics. Its profile does not modify the authored
  shared profile. Disable/destroy removes the runtime Volume and profile components.

## Tuning

Select Assets/GameData/MiningGameData.asset, Camera rotation comfort:

- Camera Maximum Rotation Speed: 180 degrees/second.
- Camera Rotation Smooth Seconds: 0.06.
- Camera Rotation Blur Strength: 0.06; set to 0 to disable.
- Camera Rotation Blur Clamp: 0.012 (maximum screen-space extent).

Attack Range remains in PlayerStatsData. Existing input bindings, Animator states,
weapon attachment, SFX and contact/VFX animation events are preserved.
No scene setup or package installation is required in this project's URP setup.
Motion blur comfort is subjective and is not a guarantee against motion sickness;
leave strength at zero if it feels worse, keeping the speed cap and smoothing.

## Validation

Unity MCP compiled the changed assembly without errors. Focused Play Mode checks:

- Two synthetic enemies: nearest selected, next acquired after nearest left range,
  no target when both left range.
- Seventy colliders: closest selected through overflow fallback; disabled enemy ignored.
- 90-degree rotation request: 1.148744-degree step with a 1.149696-degree frame cap.
- Blur intensity rose to 0.004599255, with CameraOnly, override enabled, clamp 0.012;
  settled to approximately zero after idle.
- Post-processing was enabled on Main Camera. After leaving Play Mode, no temporary
  blur objects remained and the authored profile retained its original four effects.

These are focused runtime checks, not a subjective comfort assessment or a complete
animation/contact playthrough. The existing missing-script warning remains outside
this change. Existing unrelated scene/font edits were not altered or packaged.
