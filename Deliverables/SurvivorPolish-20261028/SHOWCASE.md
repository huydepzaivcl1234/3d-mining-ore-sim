# Survivor movement, audio and culling polish

Project: D:/3d mining sim. Changes started 2026-10-28, verified 2026-10-29.

- Fixed jump rejection caused by the old FPS locked-cursor requirement. Space works with the survivor free cursor; menu/modal blockers still prevent jump. Physics takeoff and ground-contact transitions emit jump/landing SFX events, avoiding the old clip's repeated jump force.
- Eight copied locomotion clips contain two OnFootstep events each. Humanoid foot transforms are bound; blending-weight, grounded/moving checks and minimum interval prevent idle/air footfalls and duplicate blend events. Existing grass clips provide foot, takeoff scuff and landing sounds, adjustable in PlayerFootsteps. Original imported animation curves remain unchanged.
- Imported supplied limbs_pop for each attack volley. Random non-repeating splat/splat2/splat3 at successful monster damage contacts. Uses existing spatial SFX pool and user volume/mute settings. Clips copied into Assets/GameData/Audio/SFX/Survivor with mono/preloaded low-latency import settings.
- Disabled BlueGunBoxReward in scene, suspending both blue and orange box rewards without deleting upgrade data or ordinary card upgrades.
- Shared editor menu entries moved from Mining Simulator to Survivor, including the Assets submenu.
- Disabled Motion Blur/DOF in scene profile and saved graphics preference. Removed extra follow lag by translating camera focus with the interpolated player before smoothing orbit/pivot changes.
- Kept camera frustum/render-distance culling and moved its pass after final camera follow. Added reversible freezeOffscreenSimulation: registered offscreen actors pause AI movement, animation and dynamic rigidbodies; onscreen actors resume with preserved velocity and shifted cooldown/loot deadlines. Health regen/burn update pauses with hidden actors. Colliders and actor GameObjects stay intact. Inactive/zero-sized renderers no longer inflate cached bounds.

## Verification

- Validation.json: 660 frames of input simulation; 10 footfalls, Space jump height ~1.47 m, one takeoff SFX and two landing contacts over the walking/jumping route. Five attack volleys and five impacts requested corresponding sounds. AudioSource playback captured limbs_pop and all three splats plus grass clips.
- Camera relative viewport drift during straight movement: ~0.00000021 normalized units. Motion Blur off; both box rewards disabled.
- Offscreen probe: zero movement, paused/kinematic; resumes visibly with dynamic body restored. MonsterFreeze.json additionally recorded four real MushroomMonster instances with zero movement and animator speed zero over 45 frames.
- AnimationValidation.json: all 8 copied clips retain original lengths and curve counts; 2 footfall events each. Scene Missing Script count zero. User's current attack range remained 4 m.
- Gameplay.png is actual 1920x1080 Game view and was personally inspected.
- Final compile succeeded with zero current Console errors. The final full recompilation reports 136 existing/deprecation/unused-field warnings; runtime verification produced no new errors. No standalone build performed.

## Repairs and limitations

- First controller-edit attempt used Object.Instantiate on BlendTree, which triggered Unity native assertions and lost clone child references. Restored the backed-up controller and repaired by editing its existing owned trees; original clip references/curves remain intact except intended copies with added events. Historical assertions are retained in Pipeline history.
- First verification also used input injection before Input System update; corrected to onAfterUpdate. The next combat test had put its target 5 m away although the current user range was 4 m; final test uses an in-range target and independent physics probe. Failed reports retained for transparency.
- No claim of subjective audio listening or exhaustive testing of every boss action/occlusion case. Frustum visibility is camera bounds based, not wall-occlusion raycast visibility. Static scenery remains renderer/terrain culled as before; freeze applies to the existing registered actor system.
- Tests used an isolated profile and temporary actors. testStore, input injection and capture timing were restored, and Unity returned to Edit Mode. Before/ contains touched-source and scene/controller/profile snapshots.
