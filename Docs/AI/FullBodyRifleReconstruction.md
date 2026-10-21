# Full-body rifle reconstruction, 2026-10-15

## Scope and evidence

User requires one complete original player skeleton and mesh, original hand pose,
camera-following torso, shadows, bounded pistol-like motion, no detached viewmodel.
The earlier FirstPersonArmsRig route is no longer referenced by presentation or
the authoring window, and its scene component/clone/world-gun duplicate were
removed with Undo and an external scene snapshot in the preceding pass. Original
meshes/pose assets remain available; no commercial demo assets were imported.

REA opened the exact supplied demo's GameAssembly.dll: SHA-256
ae55260625efb902188e408bf307ac7d92af1eb943647c20047ffcf2398db53e.
Targeted native procedure search for AdsLayer/ViewLayer/PoseSampler/Sway/
FPSAnimator failed before bridge startup. Ghidra 12.1.4 reported `There is not
enough space on the disk`, run 156c1a9f-8256-4234-80df-aacdc05377e2. Session closed;
no recovered method or exact numerical demo tuning is claimed. C had about
0.39 GB free; D about 143.79 GB. No user files were deleted to make room.

Publisher documentation inspected separately, not evidence of this binary:
- https://kinemation.gitbook.io/scriptable-animation-system/workflow/profiles-and-layers
- https://kinemation.gitbook.io/scriptable-animation-system/fundamentals/animator-layers/view-layer
- https://kinemation.gitbook.io/scriptable-animation-system/fundamentals/animator-layers/ads-layer

These describe authored pose, procedural targets/ADS, then final IK. This
implementation is independent, not extracted framework code or exact parity.

## Correction

PistolPresentation now stabilizes the original UpperChest against a cached neutral
player-relative orientation and the camera pitch. The eye position uses the
existing camera socket's cached chest-relative offset after that chest pose.
Otherwise the old controller samples the eye before chest rotation, while the
hands sample shoulders afterwards; sprinting additionally changes the animated
chest frame. Both cases previously pushed the common grip toward the camera.

Animation still owns locomotion/jump. Presentation restores its chest override
before the next animation evaluation, then applies the saved original hand pose,
bounded profile motion, sight alignment, and final two-arm IK. No new runtime
component, Animator or Update owner was added this pass. Controller/input files
and the original pose asset were not edited. CameraAnchor is wired to the
controller's existing Cameraroot; TorsoLookWeight=1 for this full-body setup.
The world-visible gun is the same object under original Right_Hand.

## Validation and limits

The earlier test checked grip/shadow flags but admitted offscreen hands and a
sight closer than the near plane. Regression checks now also require the left
wrist inside the viewport and the ADS sight beyond near clip and within .01 of
viewport center. All 11 cases passed over 849 advanced frames: neutral/up/down
hip, walk, run, run+jump, neutral/up/down ADS, ADS run+jump, holster.
HIP left wrist stayed about (.531,.054,.338) through camera pitch; ADS sight
about (.500,.500,.209). Maximum armed support error below 5 micrometers; both
jump cases rose about 1.52 m. These are bounded scenario tests, not exact demo
visual parity, per-pixel hand visibility, or whole-project performance claims.
One body SkinnedMeshRenderer was observed. Hip and third-person diagnostic
renders were inspected; source body and hands are connected and rifle attached.
The old generated assets/class are retained unused for rollback, not running.

SampleScene remains user-saved: Ctrl+S after reviewing. Further exact-demo native
inspection needs an analysis workspace with sufficient free space, and actual
runtime comparison; it is not marked complete merely from these project tests.

## Resumed native investigation

REA 6.5.0 / Ghidra 12.1.4 now completed analysis using a child-process-only
D-drive temporary workspace. The prior disk-space blocker is resolved.
Full findings, hash-scoped address provenance and Evidence IDs:
`C:/Users/huyancut/Documents/ChatGPT/mining simulator 3d/AgentPreviews/FPSNativePoseInvestigation-20261015.md`.
Native Evidence/snapshot: `D:/REA-Work-FPS-20261015/`.
No framework source, models or clips were imported into Assets.

Static native bodies corroborate pose-space processing, component ADS blending,
separate sway operations, transform cache/post-processing and spine stabilization.
Metadata labels are independently derived; pseudocode is not original C# and
does not establish exact runtime graph ordering, profile numbers or visual parity.

Focused independent correction: sample controller camera rotation before adding
CameraShotKick, so visual recoil does not become another look-sway input.
Baseline camera-only recoil produced 0.151450634 degrees of extra sway over 77
frames; after correction 0 degrees over 34 frames. Unity recompiled with zero
errors. Seven existing motion solver tests passed. All eleven original-body
regression scenarios passed over 812 advancing frames, including pitch, walk,
run, jump, ADS and holster. Armed support error was under 2.2 micrometers; ADS
sight remained around (.500,.500,.209). These are bounded checks of wrist viewport,
grip, sight and shadow flags, not per-pixel visibility or exact demo feel.
Results: `AgentPreviews/FullBodyResumeChecks.json` in the calling C-drive workspace.

Only PistolPresentation's camera-sampling order changed in production this pass.
Controller/input, animation controller and authored pose were not modified.
Scene was not saved; preview was stopped and Unity returned to Edit Mode.
