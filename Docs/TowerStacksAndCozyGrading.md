# Tower stacks and cozy grading

- Towers stack by inventory item/type, up to 999 per slot. Existing count labels show `xN`.
- Inventory save version 4 stores FIFO `towerReceipts`. Version 3 single-tower slots merge on load without losing purchase prices. Gifts have zero refundable cost; selling removes one tower for 35% of its own original receipt.
- Placement remains top-down while the same tower type remains in inventory. A bottom-center quantity card displays the authoritative remaining count. Occupied/invalid clicks do not consume a tower. Escape/right-click cancels; placing the last unit restores the camera automatically.
- Committed placements still use TowerWorldSave, with the consumed unit's purchase receipt. Preview ghosts are never saved.

## Cozy/cinematic grading

Authoring profile: `Assets/Settings/SampleSceneProfile.asset` on the existing Global Volume. Main Camera already has HDR and post-processing enabled. The scene file is not rewritten by this change.

Bloom: threshold .9, intensity .55, scatter .72, warm tint, high-quality filtering. Tonemapping: ACES. Color Adjustments: exposure +.18 EV, contrast 15, saturation -8, subtle warm filter. Split Toning separates warm highlights from cool shadows (balance 10). Vignette intensity .24, smoothness .6; motion blur remains disabled for gameplay readability.

Runtime day/night settings live in `Assets/GameData/DayNight/DayNightData.asset`, Cinematic Post Processing. Day/night bloom .55/.6; golden-hour boost .2; exposure .18/.12; contrast 15/16; saturation -8/-10; vignette .24/.28. Nights keep a gentle cool filter.

DayNightSystem clones individual profile overrides for runtime grading, so changing time does not mutate authored profile sub-assets. All values remain editable in the Inspector.

## Verification

TowerConsumableTests covers mixed-price stacks, gifts, moves, per-unit sales, capacity, and legacy/current receipt loading. TowerPlayValidation includes three sequential placements, invalid occupied clicks, remaining-label updates, cancel/resume, last-unit exit, and existing grounding/save/health-bar regressions. Fixtures use temporary save keys, not player progress.

Do not treat test launch/Play entry as a pass: read the final test result and verify Time.frameCount advances before accepting Play Mode captures.

## Tower fire and player collision regression

- Tower damage previously played `Damage` after an attack's contact frame, replacing the state required by Skullclaw recovery. Tower-source damage now preserves movement/committed attack animation while retaining damage, death and hit feedback. Player-source reactions are unchanged.
- TowerRuntime ignores only collider pairs belonging to the active player and that tower. It prefers the active player over inactive objects and refreshes pairs after collider toggles. Monster collision, query geometry and occupied-placement checks remain intact.
- Connected Editor verification: 19 EditMode tests passed (16 TowerConsumableTests, 3 MonsterTowerReactionTests). Isolated Play validation passed actual CharacterController traversal twice, including disable/re-enable; retained monster collision and placement exclusion; actual Skullclaw right/left contacts and jump travel/landing/recovery under repeated tower-source damage. Existing save, UI, grounding and placement regressions also passed. Original roots restored without saving the scene or resetting player progress.
- Stronger grading camera capture `Assets/Screenshots/screenshot-20261010-112844.png` was inspected. Day/night authoring settings updated together; no standalone build was performed.

### Earlier cozy-grading baseline — 2026-10-10

- Unity 6000.5.3f1, actual project `D:/3d mining sim`: recompilation completed with no errors.
- `TowerConsumableTests`: 16 passed, 0 failed (including version 3/4 migration and mixed-price/gift receipts).
- Isolated Play fixture passed twice: three placements, count/label updates, occupied-click rejection, cancel/resume, last-unit exit, camera restore, grounding at five yaws, tower save/load, and existing combat/UI checks. Pipeline `wait_for` confirmed frame advancement.
- Runtime grading probe passed: day bloom/exposure `.25/.1`, night `.32/.05`, ACES, distinct cloned overrides, authored Bloom unchanged, original profile restored.
- URP HDR, Main Camera HDR/post-processing, and volume layer mask were checked. Camera capture `Assets/Docs/Validation/CozyScene.png` was visually inspected; it excludes overlay HUD.
- No new gameplay errors during these checks. Existing missing-script warning remains outside this change. Earlier Pipeline timeout errors predate this verification.
- Play Mode stopped, original SampleScene roots and background setting restored. No player-save reset, scene save, or standalone build performed.
