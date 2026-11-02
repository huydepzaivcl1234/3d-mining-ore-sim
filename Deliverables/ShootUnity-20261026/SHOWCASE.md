# User-authored pistol hands and recoil — 2026-10-26

Integrated the live Blender hand edits into Unity project D:/3d mining sim. Original live edits were saved separately as BodyAnimationReview-20261026/UserShoot_LiveEdits_Backup.blend before changes. The editable result is BodyAnimationReview-20261026/UserShoot_Recoil_Ready.blend, action USER_HAND_POSE_WITH_SHOT_RECOIL, shooting frames 181–221. Original reload frames 1–145 and base finger grip were retained.

## Result
The authored grip receives a short 25 mm backward impulse and 3.2 degree upward hand/gun pitch, peaking at 0.035 seconds. Both hands move together; elbow positions are solved and baked. The slide cycles 32 mm and closes. Existing camera recoil is 0.85 degrees with 0.16 second return; ADS uses the existing 0.65 multiplier.

World-body shooting uses the new humanoid clip. FPS uses the authored target motion and 40 authored finger curves while retaining its established shoulder/spine pose and stable elbow hints. This corrects forearm occlusion during ADS. The independent FPS/world pipelines remain intact.

Modified existing runtime scripts: WeaponAnchor.cs (world mechanism shot playback and interruption cleanup), PistolShooting.cs (restore the closed shot-frame mechanism before reload capture). No additional runtime pose scripts were added. New clips are under Assets/GameData/Player/Animations/Pistol/UserShootFinal/. Controller shot motions, scene references and M1911 data were saved.

## Reviewed media
- UserShoot_FPS_World.mp4: 235 captured frames at 30 fps, approximately 7.83 seconds. Left: first person. Right: world-body orbit camera. Three hip shots, three ADS shots, then reload immediately after firing while ADS is held. No audio.
- Frames/0034.jpg: hip shot recoil, reviewed.
- Frames/0107.jpg: ADS framing, reviewed; no forearm crossing the view.
- Frames/0220.jpg: final closed-slide return, reviewed.
- Validation.json: frame-by-frame test telemetry.

The preview used a temporary fill light for night visibility. The light and orbit camera were removed after capture; they were not saved to gameplay.

## Automated Play Mode checks
- Six actual input-driven shots: three hip and three ADS.
- Maximum observed camera kick: hip 0.8378456 degrees; ADS 0.544599652 degrees.
- FPS and world slide stroke: 0.01259852 local units times 2.54 scale = 32 mm.
- Reload input three frames after the last ADS shot; reload observed, final ammo 9/9.
- Final FPS and world slide position error: 0.
- Validation flags: none.
- Console checked after completion: zero errors and zero warnings.

Blender baked reach error: 3.69e-7 m; relative two-hand position error: 5.54e-7 m. Source pose and original actions remain retained. The source shooting range was static before adding the recoil impulse.

## Limits
Validated in Editor Play Mode, not a standalone build or multiplayer session. Recoil strength is a subjective tuning choice. Source helper import scripts predate the final FPS shoulder/hint correction and slide-cycle pass; use the saved final Blender file, exported JSON and current Unity assets rather than rerunning those helper scripts unchanged.
