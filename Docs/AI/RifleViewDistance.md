# Rifle view distance and skinned visibility

2026-10-15. Keep original body, skeleton, controller and input. No separate arms.

## Controls

PlayerArmature -> Pistol Presentation -> View Distance, in meters, 0..0.15.
Or Chest Defense -> Weapons -> Weapon View Authoring -> Khoảng cách tay–súng (m).
Use Lưu & áp dụng vào Play to save the selected preset. Loading it refreshes the
slider without emitting another edit. Current rifle preset stores 0.06 m extra.
All old presets default to zero. Wrist/finger pose arrays were not recaptured.

Distance shifts the armed eye backwards and compensates requested grip depth by
the same amount, so the original arms are not stretched beyond the IK reach limit.
It is an armed camera offset, not a gun-only transform or a separate viewmodel;
it also shifts the world camera origin by that small amount. Hip and ADS share
the distance. ADS retains its sight ray and authored support grip. Camera Anchor
must be assigned; the current player uses Cameraroot. Holstering blends it out.

## Disappearing hand

Before correction at pitch 50, the renderer bounds were outside the camera
frustum while 7042/7532 sampled left forearm/hand vertices were in the viewport
(7313/7532 in ADS), with none behind near clip. Wrist position alone falsely
suggested visibility. Original skinning bounds did not follow the procedural pose.
Enabled updateWhenOffscreen only on PlayerArmature/Geometry/Armature_Mesh via
Undo-aware Editor APIs. Existing mesh, materials, shadow settings and layers remain.
This permits skin/bounds updates for this procedural player even when imported
bounds leave the frustum. It is not a whole-project performance optimization.
Save the scene with Ctrl+S to persist that renderer override; the scene was not
automatically saved.

## Validation

- Unity compilation: zero errors.
- 24 geometric cases: 0/6/12 cm, HIP/ADS, pitch -40/0/50/70. Measured depth change
  equals requested distance within 1 mm; support error under 5 mm; ADS centered.
- Tool save/load round trip passed; pose data unchanged except distance.
- Saved 6 cm distance was observed on fresh Play entry. All eleven existing
  full-body scenarios passed over 492 advancing frames: hip/up/down, walk/run,
  run+jump, ADS/up/down, ADS run+jump and holster. Armed support error <4.5 um;
  both jumps rose ~1.52 m. Evidence: AgentPreviews/DistanceBodyRegression.json.
- Six actual Play/GPU skin cases at 6 cm, HIP/ADS, pitch 0/50/70, over 468 advancing
  frames: visible body skin pixels in sampled lower-screen region remained ~3570
  HIP and ~5047 ADS, frustum true, support error under 2.1 micrometers. A temporary
  unlit diagnostic material was restored, and image inspected after restoration.
- An initial synchronous Edit Mode pixel probe produced zero pixels because its
  skin render did not advance with the procedural pose. It was replaced with
  separate geometry and advancing Play/GPU tests, not treated as a passing render.

External evidence/tests live in the calling workspace's AgentPreviews:
HandVisibilityProbe.cs, HandVisibilityPlayCheck.cs, HandVisibilityResult.json,
WeaponDistanceGeometryChecks.cs, and HandSkinMask.png. Live capture:
D:/3d mining sim/Assets/Screenshots/RifleDistanceLive.png.

Checks are bounded to the current rifle/body and camera setup. Visibility is not
guaranteed behind arbitrary world occluders or for future rigs/presets. No global
near-clip, FOV, renderer-layer, controller/input or Animator change was made.
