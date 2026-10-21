# Camera-space rifle hands — 2026-10-15

## Current correction: complete world model

The body-only mesh swap made Scene view appear armless. It is no longer used:
FirstPersonArmsRig always retains the original full-body mesh. LocalPlayerBody
layer 8 filters only the body renderer and a mesh-only world rifle from the
player camera. Scene view retains the full character and authored Rifle layer.
The camera-space rig is hidden only in Scene view except while authoring; Game
view still renders it. No extra controller, Animator or runtime loop was added.
The world rifle is an Editor-created visual child of the original right hand.
This supersedes the older body-only mesh and zero-world-layer notes below.

All 11 motion/ADS/holster cases passed again over 623 observed frames. The
authoring bone cache was also made nonserialized and refreshes destroyed entries
after Play/domain transitions. Generated body-only assets are retained for
rollback, not deleted. This is not a claim of exact FPS Framework parity.

## Confirmed cause and correction

The previous first-person pose solved against the full-body character's
world-space shoulders. Camera pitch and locomotion moved those shoulder reach
spheres relative to the desired screen pose. The common-grip clamp therefore
changed weapon depth, scale and framing. Before correction, editor pitch
-40 / 0 / +70 produced rifle ADS positions Z=.2676 / .3672 / .3978 m.

`FirstPersonArmsRig` now supplies a separate camera-parented arm skeleton and
arms-only skin mesh to the SAME `PistolPresentation` solver. There is no second
input owner, Animator, Update loop, controller, physics component or gameplay
authority on the viewmodel. The original player's Animator/controller/input
remain intact. Full-body mesh is restored on holster; armed mode uses a
complementary body-only mesh to prevent double arms.

Original FBX, mesh and pose assets were not overwritten. Model Read/Write was
temporarily enabled for mesh partitioning and restored immediately afterwards.
Generated meshes are prepared once in the Editor, not created while playing.
Arms mesh: 18,484 vertices / 28,224 triangles; original full mesh: 29,936 vertices
/ 47,884 triangles. This is a visual separation, not a claim of fewer total
rendered triangles for the entire player.

## Motion / code cleanup

- Rifle reuses existing `FpsWeaponMotion`, with its own profile copied from the
  pistol: maximum displacement .01 m, maximum rotation 1.5 degrees, ADS factor
  .05. Changing this asset does not change the pistol profile.
- Shared grip clamps were consolidated into one helper.
- Legacy bob calculations are skipped when a motion profile owns the motion.
- Bone references are cached; viewmodel visibility changes only when needed.
- Export/build and scenario-test scripts stay outside Assets in AgentPreviews.
- No source controller or unrelated quest/shop/save/enemy code was deleted.

## Validation

11 Play-mode scenarios using the existing InputManager/controller and real
right-mouse-button state passed: neutral/up/down hip, strafe walk, backward run,
run+jump, neutral/up/down ADS, ADS run+jump and holster. 965 frames advanced.
Camera-angle-only drift: less than .01 mm; motion drift: up to 5.23 mm hip and
.337 mm ADS. This bounded movement is intentional sway/bob. About 8,150–8,169
left-arm vertices remained inside the camera frustum in armed scenarios; both
jump scenarios rose about 1.52 m. Vertex visibility is a frustum check, not a
per-pixel occlusion assertion; screenshots also inspected separately.

A 500-call pose measurement including reflection harness overhead took about
16 microseconds/call and allocated 0 bytes. This is NOT whole-project FPS or GPU
telemetry. A general project-wide performance/unused-code audit remains separate
from this scoped weapon correction; no broad deletions were made on guesses.

Main SampleScene remains unsaved by the agent. Use Ctrl+S to keep the scene
wiring after inspecting it. Scene handoff copies are stored outside Assets.

## Authoring

## Independent FPS presentation follow-up

When the camera-space rig is ready, PistolPresentation now keeps the world-body
weapon layer and PistolLocomotion parameter at zero. The saved arm pose, grip,
ADS and gentle motion are evaluated only on the local FPS skeleton. The source
body Animator continues to own locomotion/jump; its arm bones are not used as
viewmodel targets. This is a Valorant-inspired presentation contract, not a claim
to reproduce Valorant's internal implementation or use its assets.

Recompiled without errors. All 11 input/controller Play scenarios passed again
(521 observed frames); world-body Rifle layer weight was verified at zero.
Existing screen framing and source assets were retained.

Weapon View Authoring and Lưu pose tay now use `GetPoseBone()` to edit/capture
the FPS rig when it exists. Do not keyframe the full-body arm bones and expect
them to drive this camera-space viewmodel. Reload still requires explicit
integration with the existing reload presentation; this change does not enable
rifle firing or create a new reload animation.
