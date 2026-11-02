# Head look and current authored pistol pose — 2026-10-26

## Changes
- Extended existing FullBodyFirstPerson.cs; no new runtime pose script.
- Added an explicit reference to the independent world Animator. World head now follows the stored mouse-look yaw/pitch using anatomical head orientation calibrated from skin bind poses. No camera recoil is fed into the head.
- Head/body yaw separation: head leads within 65 degrees relative to body. Crossing the limit starts body catch-up at 240 degrees/second; continuous movement also aligns the visual body. Small stationary turns retain body orientation. Rotations restore before Animator evaluation and apply once in LateUpdate, preventing accumulated twists.
- Physics/player movement and the FPS camera keep their existing controller. Independent world animation remains independent.
- Replaced world Pistol Hold with the complete upper-body resting pose at frame 181 of the current Blender shooting action. New hold and shooting end have exactly matching authored curves (maximum difference 0).
- Aimed shooting applies the authored recoil delta to the approved ADS pose instead of switching to hip shooting. Shot exits now go directly to Aim or Pistol Hold according to AimWeight.
- FPS target idle/shot now use the current authored two-hand relationship throughout, with current finger pose in Hold/Aim. Established FPS camera framing and shoulder safety remain retained. Reload and approved world ADS clips remain retained.

## Source preservation
Live Blender file: D:/3d mining sim/Deliverables/BodyAnimationReview-20261026/UserShoot_Recoil_Ready.blend
Action: USER_HAND_POSE_WITH_SHOT_RECOIL, frames 181–221.
Saved unsaved live state as UserPose_LiveBeforeLookFix.blend before inspection. Exported current live bones into LiveUserShoot.json; maximum difference against the previously imported shooting export was 0. No Blender source keys changed this task.
Before-scene, controller and runtime-script backups are in this folder.

## Validation
Validation.json is the armed input-driven Play Mode run, 780 frames at 60 Hz:
- Four actual shots: two hip, two ADS.
- Reload immediately after last ADS shot, final ammunition 9/9.
- Maximum head gaze direction error: 0.0279764552 degrees.
- Maximum head/body yaw separation observed: 64 degrees (configured limit 65).
- At stationary 45 degree look, visual body yaw remained 0.
- Look crosses 360 degrees, plus pitch down/up 60 degrees.
- Maximum local rotation difference across both arm chains before shooting versus after final return: 0 degrees.
- Final slide rest-position error: 0.
- Flags: none; ADS was released during reload.
- Also preserved a separate unarmed look run in UnarmedLookValidation.json. That first run could not test weapons because player health was already zero; it is not counted as a shooting pass.
- Armed validation temporarily restored player through existing respawn API and disabled damage for the test, then restored the previous damage flag. Temporary orbit camera, fill light and input overrides were removed.
- Zero project Console errors after execution. Recompilation emitted existing CS0618 deprecation warnings in unrelated audio/achievement scripts; these were not edited.
- Scene and assets saved in Edit Mode after test.

## Reviewed media
HeadLook_CurrentPose.mp4 — 13 seconds, left FPS/right world view; head turn, body following, looking up/down, ADS, shooting, reload and return.
Frames/0041.jpg — small head turn with new holding pose.
Frames/0175.jpg — ADS with current grip.
Frames/0258.jpg — return to new hip hold after reload.
These images were opened and reviewed before handoff. Preview lighting is temporary for night visibility and is not a gameplay lighting change.

## Limits
Editor Play Mode validated. No standalone build, multiplayer or network head-rotation replication was tested. Local FPS and world body remain separate pipelines as previously requested.
