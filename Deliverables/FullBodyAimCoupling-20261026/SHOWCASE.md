# Full-body pistol downward-look correction — 2026-10-26

## Applied change
Only existing `Assets/Scripts/FirstPerson/FullBodyFirstPerson.cs` was changed in this pass. Backup: `FullBodyFirstPerson.before.cs.txt`.
No runtime component added. No Blender file, finger pose, animation clip, Animator Controller, mask, weapon offset, reload profile, or scene was edited/saved.

Cause reproduced: camera-local hand/pole targets receive full look pitch, whereas the shoulders received only 45% of pitch and the camera's eye offset received yaw only. Downward look changed the arm geometry progressively, lifting the elbows into the torso.

Fix: smoothly couple shoulder-chain pitch and eye-offset rotation below the horizon for the equipped pistol. Full coupling by 20 degrees down. Upward/level and rifle/unarmed branches keep their previous calculations. Field `stabilizeDownwardWeaponPose` allows A/B or rollback without animation edits.

## Evidence
- `Comparison.jpg`: side-view before/after hip and ADS at 0 and 65 degrees down.
- `Compare.json`: 16 settled conditions: fix off/on x hip/ADS x -65/0/35/65 pitch. Three captures per condition: FPS, Front, Side, all 1280x720.
- `FullBodyAim-Test.mp4`: 15 seconds, 30 fps, 1280x720, no audio. Same live player camera pose/FOV/culling rendered by a temporary capture camera; UI excluded. This is NOT a 1920x1080 Game-view/Recorder capture.
- `Video-Contact.jpg`: 12 reviewed video frames: idle, walking, turning, ADS, firing, magazine/slide reload, return to right, draw, final idle.
- `Runtime.json`: 450 end-of-frame measurements and event results.
- `Frames/`: all 450 source video frames.
- `Runtime_*_Side.jpg`: selected world-camera snapshots.

## Checks completed
- Compilation: no errors returned by Console. Existing CS0618 warnings across unrelated systems remain after full compile; do NOT describe Console as warning-free.
- A/B: approved level/upward calculations unchanged; visual samples checked.
- Downward hip wrist angles at 0/35/65: right ~52.02/51.99/52.06 degrees, left ~25.00/25.01/24.99. Thus downward pitch no longer progressively changes the grip/bend geometry.
- Downward ADS right wrist ~36.26/36.24/36.25 degrees; left ~16.70/16.65/16.70.
- Runtime: three successful shots (ammo 9 to 6), successful reload, unequip/re-equip draw. Reviewed reload and return-right frames.
- At fully locked IK, maximum hand-target position error 0.000000787 m. Target anchor presented in the same frame for all 450 samples.
- IK faded to 0 for draw and reload, returned to 1. Existing fade timings untouched.
- Largest root step 0.0961 m while walking; no spawn teleport observed.
- Stopped Play Mode after test. Scene not dirty. Fix flag remains enabled in Edit Mode.
- Actual project verified: D:/3d mining sim, Unity 6000.5.3f1.

## Limits / remaining review
- Verified sampled angles through +/-65; extreme +/-80, crouch, jumping, arbitrary body proportions and multiplayer are NOT exhaustively validated.
- Full shoulder follow makes the torso bend farther at deep downward angles. This is intentional to preserve arm geometry; style/comfort still needs the user's review.
- Screenshots were taken in the existing night lighting. No persistent time-of-day/progression reset was performed.
- Reload/fingers were preserved by leaving their source assets untouched; no claim of exhaustive per-finger collision validation.
- Viewed selected full-size captures plus contact sheets; not every one of the 450 video frames individually.
- Research used relevant Look/IK/PoseSampler sections and keyframes of the supplied videos, not an uninterrupted full watch of all three.

## Reference
KINEMATION's official pipeline documents retarget/base pose, procedural layers, then IK, with elbow handling around those layers:
https://kinemation.gitbook.io/fps-animation-framework/fundamentals/animation-system/ik-animation-system
Look layer permits differing up/down distributions:
https://kinemation.gitbook.io/fps-animation-framework/fundamentals/animation-layers/look-layer

This is a targeted correction in the existing local implementation, not an import or reproduction of the paid framework.

