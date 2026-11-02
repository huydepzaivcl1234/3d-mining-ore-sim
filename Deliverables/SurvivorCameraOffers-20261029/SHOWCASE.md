# Camera, nearest-target auto attacks and upgrade offers

Project: D:/3d mining sim. Verified 2026-10-29.

- Hold right mouse to orbit. Yaw/pitch now ease towards mouse targets (0.085 s smoothing); movement uses the displayed camera yaw. Camera follow retains interpolated player translation.
- Mouse wheel zoom works with both normalized wheel ticks and 120-unit backend notches. Target distance 3-20 m; zoom eases over 0.14 s. Collision retracts immediately and recovers gradually to avoid clipping.
- Nearest living, unobstructed monster inside the existing attack radius is selected every frame, including during cooldown. All bolts in a volley prioritize that monster. Switching targets does not restart cooldown; next encounters fire immediately once ready. Existing damage, attack-rate, range, projectile-speed, sounds, damage popups and culling are preserved.
- Existing upgrade modal restyled as three stacked offers, with current combat totals and collected bonuses at left, green header and existing font. Existing reward callbacks and save format retained. Skip dismisses without a reward. Refresh is shown unavailable at zero; reroll mechanics were not added.
- InputManager initializes action subscriptions in Start, after PlayerInput has initialized its action state; enable/disable guard the pre-initialization case. This fixes InputActionMap startup assertions seen during repeated Play sessions.

## Checks

- Validation.json: 600 actual game frames with injected Input System mouse input. Orbit smoothing, wheel zoom 9.5 -> 8.7 -> 9.5 m, nearest selection, closer-target switch during cooldown, and range entry passed. Expected and measured cadence 1.0 s. User-authored attack range remains 4 m.
- OffersValidation.json: each of three actual runtime button listeners grants the right bonus, pauses/resumes correctly; Skip resumes; font unchanged; damage/projectile-count/projectile-speed upgrades do not alter attack rate. Uses a copied, isolated save profile.
- UpgradeOffers.png and UpgradeOffers-AfterUpgrade.png: actual 1920x1080 Game view captures, personally inspected for layout and readability. Test bonuses are confined to test-profile.json.
- Initial UI test incorrectly resolved button references via SerializedObject; repeated against the runtime fields passed. One evaluation had a coroutine syntax error; corrected test code. Pipeline requests occasionally timed out; longer eval timeout used. These are tooling diagnostics, not successful gameplay checks.
- Unity compiled successfully. Existing obsolete API/unused-field compile warnings are outside this change. No standalone build or subjective hardware-mouse playtest performed.

## Modified files

- Assets/GameData/Survivor/Scripts/TopDownSurvivor.cs
- Assets/GameData/Survivor/Scripts/SurvivorAutoCombat.cs
- Assets/GameData/Cards/Scripts/MiningCardSystem.cs
- Assets/GameData/Player/Scripts/InputManager.cs
- Assets/Scenes/SampleScene.unity (existing card-panel presentation and icon references)

Backups: C:/Users/huyancut/Documents/ChatGPT/mining simulator 3d/Deliverables/SurvivorCameraOffers-20261029/Before, plus BeforeScene.unity here. No new gameplay scripts or packages.
- Final harness rerun explicitly resets timescale left at zero by modal testing; the paused failed run is retained as Validation-PausedHarness.json. The final run passes after reset. InputValidation.json separately checks W reception and movement after the input initialization fix.
- Final input test: W received and player moved 1.04 m over 65 frames. Background test explicitly enabled keyboard because the Editor focus policy disables background devices; normal focus policy restored. Unity returned to Edit Mode; isolated save override cleared, timescale 1 and audio unpaused.
