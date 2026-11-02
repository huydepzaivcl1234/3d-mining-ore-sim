# Survivor camera and legacy mining cleanup — 2026-10-28

Implemented in D:/3d mining sim; scene Assets/Scenes/SampleScene.unity.

- Perspective third-person framing based on supplied Megabonk screenshot: 18 degree downward pitch, 65 degree vertical FOV, distance 9.5, look-ahead 1.8. Character appears lower-center, horizon remains visible.
- RMB drag orbits, mouse wheel zooms; WASD follows camera yaw. UI remains clickable. Controls pause while modal gameplay blockers are active. Sphere sweep shortens camera distance around solid geometry.
- Removed weekly rebirth quest definition and obsolete authored UI rows for rebirth, hiring miners, mining 500 and 5000 ore. Seven combat quests remain with existing IDs and rewards. Corrected compact quest row top-pivot spacing.
- Renamed scene HUD to Survivor HUD Canvas; coordinator supports both new and legacy names. Updated stale authored item description. No font or general HUD redesign.
- Internal MiningSimulator namespaces and shared wallet/health/inventory/card types retain their names for compatibility. Unused old mining source/assets remain in project; this is gameplay/UI cleanup, not wholesale deletion of every file named Mining.

## Evidence
- Gameplay.png: actual 1920x1080 Game view, daylight preview; personally inspected.
- Quests.png: actual 1920x1080 quest panel after cleanup and spacing fix; personally inspected.
- Validation.json: 140 actual frames, perspective, framing, 90-degree orbit rotation and camera-relative movement direction passed. Player viewport center was (0.5, 0.4254); stale mining UI text count zero, auto combat enabled.
- Final Editor Console ground truth: no compilation failure, zero current errors/warnings. Pipeline history retained earlier eval diagnostics (one harness used an incorrect PlayerRespawn namespace, corrected without changing game source) and project deprecation warnings.
- Opening final test UI briefly exceeded Pipeline's 5 second response timeout; inspection confirmed it executed, frames continued advancing and final screenshot was captured.
- Mouse gestures were implemented but not manually playtested. Camera obstruction behavior and extended combat/card balance were not exhaustively tested this turn. No standalone build performed.

Backups: Before/ contains prior scene, camera source, quest data and modified UI sources.

Final Console inspection identified one Pipeline timeout log from opening the test panel (not game runtime code). Recorded above, then cleared tool diagnostics before handoff.
