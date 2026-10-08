# World audio and equipment layout

- Inventory has seven equipment frames at runtime: original 01 plus new 06/07 form the left necklace triangle; original 02/03/04/05 remain reserved for armour. The 32 bag slots and three saved necklace addresses are unchanged.
- Additional slot offsets are editable on MiningInventoryPanel. Existing scene positions are retained; SampleScene is not modified or included in the patch.
- World SFX now use 3D linear distance attenuation. MiningAudioData exposes minimum distance (default 2 m), maximum distance (default 30 m), and a bounded voice budget (default 16).
- Mining, player attacks/jumps/weapon sounds, monster child audio, loot, footsteps and chest sounds are spatial. UI feedback, music and global ambience remain 2D. Master/SFX sliders and mixer routing remain authoritative.
- Transient sounds use reusable AudioSources at the event position. World-source Doppler is disabled to avoid unwanted pitch changes.
- Verification: connected Unity Editor compilation succeeded; isolated checks passed for seven slots, three bindings, four reserved frames, repeated setup, spatial-source configuration and SFX gain. All eight existing necklace equipment tests passed. An isolated equipment render was inspected. Full gameplay listening/Play Mode was not performed on the user's dirty SampleScene.
