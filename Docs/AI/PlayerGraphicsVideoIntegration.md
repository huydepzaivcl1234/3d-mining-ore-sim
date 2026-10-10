# Player graphics options — local tutorial integration

## Source and review scope

Source: user-supplied `How to Change Unity Graphics Settings Through Code! (Unity Tutorial).mp4`, duration 692.50 seconds.

Reviewed the entire automatically generated speech transcript and 70 sampled frames at ten-second intervals spanning the video. This is not a claim of continuous frame-by-frame watching. Transcription can misrecognize API names; those were checked against the on-screen code and this project's installed URP source. No paid tutorial toolkit was imported.

Relevant sections:

| Approximate time | Tutorial topic | Project implementation |
| --- | --- | --- |
| 0:45–2:16 | Resolution, refresh rate and fullscreen | Supported display-mode cycle; standalone-only OS changes; 15-second unscaled confirmation/revert |
| 2:16–2:56 | VSync | Runtime `QualitySettings.vSyncCount` |
| 2:56–4:35 | Camera post-processing and anti-aliasing | None/FXAA/SMAA, quality, post-processing toggle |
| 4:35–6:15 | URP MSAA, render scale, shadow distance/cascades | Runtime pipeline clone; MSAA disabled for Deferred renderers |
| 6:15–8:20 | Volume effects and Bloom intensity | Runtime profile/component copies; toggle and multiplier |
| 8:20–8:42 | Brightness and gamma | Exposure and gamma offsets |
| 8:42–10:24 | SSAO renderer feature | Discover existing SSAO, toggle and restore its borrowed state |
| 10:24–11:22 | Build demonstration/toolkit | No toolkit dependency; standalone display verification remains outstanding |

## Design adaptations

- Existing Settings now has a Graphics subpage with a scroll view and an unscaled opening animation. Existing main-menu, controls, audio and sensitivity behavior remains in place.
- EN/VN strings use the current localization and the existing menu font/slider appearance.
- Preferences use a dedicated versioned PlayerPrefs key, separate from world/progression saves. Partial/corrupt values fall back safely; numeric values are bounded.
- Day/night lighting recomputes its authored base before player Bloom/exposure offsets are applied, preventing accumulation or the lighting loop undoing toggles.
- Quality and Volume changes use disposable runtime copies. SSAO renderer features are borrowed shared objects: their initial active flags are restored on teardown, not saved to renderer assets.
- Unsupported effects are visibly unavailable. TAA is not offered. Reset restores quality, not an unconfirmed OS display mode.
- Does not rewrite SampleScene, change gameplay progression or rebalance combat.

## Files

- `Assets/Scripts/Ores/Audio/PlayerGraphicsPreferences.cs`: serializable preferences, sanitization and refresh-aware mode matching.
- `Assets/Scripts/Ores/Audio/PlayerGraphicsOptions.cs`: runtime graphics ownership, application, persistence, display watchdog and teardown.
- `Assets/Scripts/Ores/Audio/PlayerGraphicsPanel.cs`: settings subpage.
- `MiningAudioSettingsPanel.cs`: creates graphics controls during runtime startup.
- `DayNightSystem.cs`: explicit player-options overlay on cinematic lighting.
- `Assets/Scripts/Ores/Editor/PlayerGraphicsOptionsTests.cs`: four isolated regression tests.

## Verified on 2026-10-10

Active Editor/project confirmed as `D:/3d mining sim`, Unity 6000.5.3f1. Used Unity CLI against the connected Editor.

- Compilation successful. Four EditMode tests passed, none failed.
- Main-menu Play Mode remained paused (`Time.timeScale == 0`); no reset/purchase/rebirth actions invoked.
- Settings entry and both ends of Graphics scroll content captured and visually inspected at 1280×720; content height 1022, viewport height 398.
- Runtime render scale changed to 0.8; persistence save/read round-trip passed. The original graphics preference key was restored after the check.
- Twenty day/night recomputations did not accumulate exposure; Bloom stayed disabled across twenty recomputations.
- Display preview expired while gameplay was paused; repeated close/open retained one graphics controller.
- Returned Editor to Edit Mode. Current console reported zero errors and one existing warning.

Screenshots: `Assets/Screenshots/graphics-entry.png`, `graphics-options.png`, `graphics-options-bottom.png`.

## Remaining build-only checks

OS resolution/fullscreen/refresh behavior is deliberately not executed inside the Editor. Validate supported modes, confirm/revert, restart persistence and target-device performance in a standalone build before release. The preview watchdog was tested in Play Mode, not the physical monitor switch. This work does not claim to validate every graphics setting on every GPU or screen.
