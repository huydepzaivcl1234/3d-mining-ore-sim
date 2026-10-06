# Authored combat and locomotion integration

The character motor owns movement. Root motion remains disabled; authored clips are shared by the upper combat layer and the existing full-body footwork layer.

## Attack sequence

- A valid opening lunge plays `lunge attack`. Buffered follow-ups play `turn attack`, then `Sword Attack 1`, `Sword Attack 2`, and `Special Attack`.
- An opening swing without lunge travel plays `Sword Attack 1`.
- `Special Attack` retains the previous third strike's contact timing, downward VFX timing, and damage bonus. Old public animation-event handlers remain compatible.
- Only the initial lunge translates the character. Normal combo strikes retain normal damage range.
- Imported upper-body clips own contact/VFX events. Generated lower-body clips contain no events, preventing duplicate damage.

## Movement

- In armed combat, reversing without shift lock selects walk/run turn 180.
- With shift lock, movement selects front/back walk/run strafe.
- Stationary camera turns select sword turn 1/2 with either camera-lock mode.
- PlayerCombatInput exposes turn threshold, smoothing, replay delay, camera angular-speed threshold and contact phases in the Inspector.

## Assets and compatibility

- Preserve existing controller layers, masks, serialized references and GUIDs. No scene edit/setup is required.
- New Mixamo source clips use their own Humanoid Avatar instead of copying an incompatible skeleton.
- The ZIP includes importer metadata for existing source FBXs; keep the original FBX files already present in the project.

## Validation on 2026-10-06

- Connected Unity Editor compiled the updated scripts.
- Checked 22 upper/lower animation states for valid Humanoid motions and event-free lower clips.
- Isolated Editor checks selected all eight movement/turn states and Combat 1 for an air opener.
- Animator.Update checks for Lunge, Turn, Combat 1 and Special caused no damage before contact and one hit at contact; duplicate event calls did not deal extra damage.
- Temporary test objects were destroyed; no player save reset or scene save was performed.
- The Unity test-runner attempt timed out; it is not a passing NUnit result. Full interactive Play Mode animation feel remains unverified.
