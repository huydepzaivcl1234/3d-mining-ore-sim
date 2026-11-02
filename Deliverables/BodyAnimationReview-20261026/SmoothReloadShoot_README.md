Reload smooth: frames1-145. Shoot:181-221 (one shot).
RightHand selected at183. Pose Mode: R/G then I to key. Gun_Slide controls slide.
ARMS - SHOOT ANIMATION collection shows arms/fingers. Hide this collection to edit only gun. PLAYER collection retains other body bones. No live pose handlers or IK constraints; baked keys can be edited.
Source actions and Before_SmoothReload_Shoot.blend retained. Finger poses unchanged in reload.
Measured gun second-difference RMS reduced 54.92 percent. Straight insertion retained.
Executed SmoothReloadShoot.py and slide-cycle bake via live Blender MCP. BUILD_OK. run_blender.py VERDICT: NOT RUN (live session). Generic mesh validator NOT RUN (no topology changes). Custom animation VALIDATION: fail0, warnings2 (inherited rig scale, no multiplayer/build test).
Viewed reload_0025.png/reload_0034.png and shoot_0000.png/shoot_0002.png/shoot_0010.png. MP4: Reload_Smooth_Preview.mp4; Shoot_Edit_Preview.mp4 (shoot preview half speed).
Reload145frames re-baked into Unity native Humanoid/legacy mechanism assets. Shoot range left in Blender for user editing; gameplay shooting clip unchanged.
