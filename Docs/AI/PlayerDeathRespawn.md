# Player death and respawn

Leave Play Mode, select Player and run `Mining Simulator > Setup > Player Death And Respawn`.
Save the scene yourself. The setup is opt-in; no scene is shipped in the ZIP.

- Player: `PlayerDeathRespawn` configures respawn seconds, respawn point, camera speed/radius and camera drivers.
- `Player Respawn Point`: move/rotate this scene marker to choose a safe return position. Keep it outside monsters/walls.
- `Mining HUD Canvas / Player Respawn Panel`: edit the countdown's layout, background and TMP font. The panel is visible for authoring and hidden by Awake in gameplay.
- The assigned Animator Controller is copied to a sibling `... Respawn.controller`; locomotion and combat are preserved. Edit `Base Layer > Death` to change the death motion. It is full-body and freezes at its final pose; do not add outgoing transitions to it.
- The default death clip comes from the existing Kevin Iglesias HumanM@Death01 FBX; no vendor animation or source controller is modified.
- Zero HP disables movement and attacks, clears stored input and pauses camera drivers. The countdown uses scaled gameplay time (pause also pauses respawn).
- Camera: WASD movement, Space/Ctrl vertical, hold RMB to look. A sphere sweep and CharacterController prevent wall crossing. Radius covers the camera near plane. World Layers should include all terrain/walls.
- Respawn restores full health and pre-death enabled/input/camera settings. A small `ResetMotionAfterRespawn` hook in the existing ThirdPersonController clears previous running/jump velocity; serialized movement settings are unchanged. If the marker overlaps a solid obstacle, respawn retries after one second rather than placing Player inside it.
- `Heal` still cannot revive a corpse; only explicit `MiningCharacterHealth.Respawn` restores dead health.

Setup ignores null Behaviour slots from unrelated Missing Script components. It reuses an existing named respawn marker when its reference is empty, rather than creating another marker on retry.

In Play Mode, the PlayerDeathRespawn component menu exposes `Test Death (Play Mode)` to exercise the normal health/death flow without adding a gameplay shortcut.

Validation (2026-09-26): reproduced the Setup null-reference exception in Unity, fixed it, compiled in Editor with zero errors, reran Setup and confirmed panel/label/camera references plus two camera drivers. Observed death animation, ticking countdown and successful respawn in Play Mode; tapped W and Space with Game focus for spectator movement. Scene references were saved in the local project; the ZIP still excludes scenes. RMB look and wall/corner collision need a longer manual test. Existing Missing Script, kinematic velocity and audio channel warnings remain separate.
