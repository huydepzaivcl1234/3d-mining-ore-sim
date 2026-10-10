# Save system — 2026-10-10

## Tutorial review

Source: supplied `MASSIVE Unity Save System Tutorial!.mp4` (22m40s). Reviewed the complete automatic transcript and 136 sampled frames at 10-second intervals across the full timeline. This is not uninterrupted frame-by-frame playback.

Applied: serializable DTOs/JSON/file persistence (01:20–07:07), subsystem Save/Load ownership and composition (07:15–14:58), dynamic-object collections (15:00–20:07), player health/position and world state (20:08–22:00). The existing tower collection retains stable IDs and duplicate-safe restoration. No paid toolkit was imported. Demo T/Y shortcuts and writing inside Assets were not copied.

## Architecture

`GameSaveStore` owns one versioned main JSON document with independent subsystem sections and SHA-256 corruption detection. Existing owners keep their DTOs and migrations. Unknown subsystem payloads are retained. This is not encryption/anti-cheat.

`GameSave` replaces gameplay PlayerPrefs calls. Legacy values migrate on read without deleting original keys. Tombstones prevent deleted values being reimported. Explicit full reset archives old files and disables legacy migration; normal rebirth retains its existing semantics. Graphics/audio/language/keybind preferences remain separate in PlayerPrefs.

`GameSaveHost` installs automatically before scene load, persists between scenes, and writes dirty data once per unscaled second. It flushes on pause, focus loss and quit. An Editor hook flushes on assembly reload and after leaving Play Mode. Existing owners still capture state in their callbacks. No scene/prefab modifications required.

Write: serialize → flush `.tmp` → replace primary, retaining `.bak`. Load: primary → backup → validated temp. Future versions block writes instead of downgrading. Unrecoverable files are preserved; Console reports the error. Failed writes retain dirty state for retry. New Game/reset archives the old files first and surfaces failures.

Actual Editor profile:
`C:/Users/huyancut/AppData/LocalLow/DefaultCompany/3d mining sim/Saves/profile.json`

General path: `Application.persistentDataPath/Saves/profile.json`. Company/Product changes can change this path. Reset archives have `.reset-<guid>` suffixes. Do not replace a running profile manually.

## Saved scope

- Money (new), gems, player progression/card/rune bonuses.
- Inventory/equipment/tower receipts with existing item migrations.
- Rebirth, quests, achievements, computers, day/night and existing auto-upgrade data.
- Chest level, XP, HP and broken state; loading does not replay destruction/rewards.
- Placed towers: catalog/placement IDs, scene, pose, HP and paid price.
- Player checkpoint: scene, grounded living pose, yaw and HP. Airborne/death-ragdoll poses are not recorded. Unsupported/blocked or wrong-scene positions fall back to scene spawn. Full reset clears checkpoints and restores safe scene spawn for a normal player.

Not persisted: active monsters/wave-in-flight state, temporary consumable duration, particles, animation playback, ragdoll pose or payout sub-tick timer. One local profile, no cloud sync/multiple-slot UI. Settings survive gameplay reset.

## Verification and limitations

Live checkout verified: `D:/3d mining sim`, Unity 6000.5.3f1. Unity CLI used for compilation, tests and runtime checks.

- **10/10 GameSaveTests passed**: round-trip, tombstones, backup recovery, corruption preservation, future version, unknown section retention, reset/archive, interrupted write, failed-write retry and legacy int/float/JSON migration. Unique temporary files/keys only; no real progress reset.
- **173/178 broader EditMode tests passed**. Five unresolved failures: two TreasureChestTests artwork/fill expectations and three WeaponAttackTests hit expectations. No pre-change full-suite result exists to prove their origin. Tests were not disabled or changed to manufacture a green run.
- Two SampleScene Play sessions at menu timeScale 0: money 25, chest level 1/HP 100, one save host; second session restored money/checkpoint (0, 0.028, 23.12). Profile existed, writable, LastError null. No runtime/compilation Console errors during smoke. Play Mode stopped; scene not saved/replaced.
- Latest compilation: no compile errors. No Windows/IL2CPP build, extended gameplay soak, process-kill fault injection or other-platform persistence validation. Whole-project release readiness is not claimed.

Commands: `unity command run_tests --project-path 'D:/3d mining sim' --mode editor --filter GameSaveTests --async_tests true`; omit filter for all EditMode tests. Latest results: `Temp/pipeline_test_status.json` (overwritten each run).

## Adding a persistent system

Own a `[Serializable]` versioned DTO and stable unique key. Restore after dependencies initialize; validate/default missing fields and preserve unsupported DTOs. Capture changes with `GameSave.SetString(key, JsonUtility.ToJson(dto))`, plus owner lifecycle capture. Dynamic objects require persistent IDs and catalog IDs, never GameObjects/scene instance IDs. Reconcile before instantiating. `GameSave.Save()` leaves normal coalesced persistence to the host; `GameSave.Flush()` forces a write. Do not clear saves to conceal migration errors.
