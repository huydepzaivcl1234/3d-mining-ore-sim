# Chest break, repair and daily encounters

## Behaviour

- Chest XP now comes only from monsters killed by the player (including the player's strongest active burn), using monster XP times boss XP multiplier and `killExperienceMultiplier`. Gold ticks never grant XP. Existing level/XP saves are retained.
- `moneyPopupScale`, font/icon size, lifetime, pop duration and rise distance are editable independently of the HP/XP panel. The popup eases outward, rises and fades.
- A non-interactive camera-facing world-space stats panel sits to the viewer's left of the chest. It appears within `statsNearDistance` (4 m by default), fades/shrinks away beyond `statsHideDistance` (6 m), and exposes `statsWorldOffset`, `statsWorldScale` and the size multiplier in TreasureChestData. Legacy screen field identities are retained; screen pixel offsets are no longer used.
- Opening and surviving hits punch a dedicated visual pivot around renderer bounds. Imported model colliders are disabled; the outer anchored body remains authoritative.
- Open/close/payout/hit/break/repair clips and individual volumes, overall volume, pitch, spatial range and hit cooldown are in TreasureChestData. Explosion SFX is emitted on death.
- Strike and approach visibility ignore other monsters, but still reject walls. Saturated raycast buffers use an all-hit fallback. Path arrival tolerance is included in the attack-distance budget.

- Base-chest health reaching zero stops income, clears pending payouts, emits the imported explosion and shows the damaged model.
- Repair appears only in the chest's world-space billboard, under HP/XP, with a coin icon and price. It follows the same camera-facing/distance fade as the HUD and never registers a gameplay-blocking modal.
- Repair costs the editable GameData `repairCost` (default 100 Money), restores full health, clears burn and restarts the income interval. Level and XP are preserved. Insufficient money leaves the chest broken; repeated clicks cannot charge again after repair.
- Player contact, sweep and direct strike paths ignore the player's base chest. Monster damage remains enabled.
- The chest GameData owns the spawn ring, initially 12–16 metres. Daily encounters plan exactly clamp(day, 1, 50) monsters, including boss encounters. Enabled spawners share a hard cap of 50 living monsters.
- Pending waves retry rather than expiring when a spawn is blocked or the chest is broken. A new day builds that day's budget; it does not accumulate previous days' unspawned quota.
- The first wave is due at the start of its allowed period; subsequent waves are staggered. Night-only encounters still wait for night.

## Asset admission

Source: user-supplied `ChestBreak_Explosion_DamagedModel.zip`.
The user explicitly confirmed usage rights and approved import despite the missing licence manifest/receipt. Canonical game-dev receipt verification was unavailable; this is a documented exception, not a verified catalogue package.
Imported the FBX, its mapped URP materials, explosion prefab, shader and textures. Omitted duplicate GLB, Blender sources, previews, archive README and the obsolete MiningChest setup/adapter. The reviewed burst script retains the supplied prefab GUID and emits at most 47 particles per explosion.
Imported asset bytes were compared against the archive; Unity subsequently migrated the material serialization. No existing asset paths were overwritten.

## Validation

### HP / XP proximity correction

- Confirmed in the connected Editor that HP/XP still used Near=12 m / Hide=30 m while world stats used 4 m / 6 m. This made the health panel remain visible well after leaving the nearby stats panel.
- Updated the HP/XP GameData and defaults to Near=4 m / Hide=6 m. Initial visibility is hidden; the existing eased fade/scale/downward slide remains. At zero visibility the HP/XP canvas object is disabled, including Repair and popup children, and is re-enabled by the HUD owner when approached again.
- Added `HealthPanelHidesWhenFarAndReturnsWhenNear`: start far without a flash, approach, leave to 10 m (previously still visible), check zero alpha/no raycast, then approach again. Thresholds remain editable; no scene edits are required.
- Editor compiled without Console errors before starting the new test job. MCP disconnected repeatedly while awaiting that job; the on-disk TestResults.xml still belongs to the earlier 39-test run and does not include this regression. The new test result and a fresh Play Mode check remain unverified; do not treat the earlier pass as validation of this correction.

### Nearby world stats / moving-target lunge revision

- Connected Editor compilation and 39 selected EditMode tests passed (TreasureChestTests and FreeFlowCombatTests). World-space stats near/far visibility and bounded moving-target steps have regression coverage.
- Isolated Play Mode chest probe passed again, including movement/interception, chest damage, gold, kill-XP, break/repair and quota/cap guards. Live stats inspection returned WorldSpace, alpha 1 near the chest and alpha 0/inactive at 20 m. Captured `Captures/chest-world-stats-validation.png`.
- A separate cloned player in Play Mode used the real Animator, footwork and CharacterController against a laterally moving target. Root gap closed from 2.75 m to 1.30 m, lateral following was positive, the selected target was retained, and inserting a wall immediately cancelled tracking and zeroed step velocity. This bounded scripted probe is not a full keyboard/mouse combat playthrough.
- Lunge tracking uses one selected target, an editable additional travel budget, existing damage range/contact events, and the existing collision motor. Shift-lock camera facing yields only during tracking. Contact, invalid target, wall or attack interruption ends tracking; ordinary walk/run and Animator assets were not rewritten.
- Restored all 22 authored scene roots and closed the unsaved validation scene. No explicit scene save, production purchase, data reset, or Play Mode Start Scene override was performed.

### Current feedback / kill-XP revision

- Connected Unity Editor compiled without errors. The 24 selected EditMode cases passed (TreasureChestTests and ArmorAndRelativeLevelTests); Test Runner temporarily disconnected MCP during reload, then returned its complete result.
- Added regression coverage for killer attribution/strongest burn, crowd-vs-wall strike visibility, static colliders under centred punch, and no XP from payouts. Updated the isolated opt-in Play probe to require a player kill for chest level-up.
- Isolated Play probe passed movement, interception, return to chest, chest damage, gold without XP, XP/level-up from a player kill, coin VFX, daily quota/cap guards and two break/repair cycles. It uses a cloned GameData asset, an unsaved clock and a separate wallet; no production purchases or data resets were made.
- A paused Game View capture verified popup icon/amount and the left-screen stats. This is not a dense-crowd soak or an audible listening test.
- SampleScene disk SHA-256 remains `B5930F16B4D2D41B2220086DF961EC874802B9843B0913A80DE19D0A997B9892`. No scene save was performed; the temporary additive validation scene is closed after testing. Play Mode Start Scene stays None.
- The historical results below describe the earlier repair revision, not proof that the new revision passes.

### Earlier repair revision

- Unity compilation: no errors.
- 15 TreasureChestTests Edit Mode cases cover artwork, world-space Repair, daily quotas and 12–16 metre sampling.
- Isolated opt-in Play Mode probe passed movement, player interception, chest damage, income/XP, explosion emission, two break/repair cycles, friendly-fire rejection and the spawn-cap guard.
- Real EventSystem raycast at the world button's projected position hit Repair; its pointer-click handler restored the chest and deducted exactly 100 Money.
- World-space button, broken model and coin/price visually inspected in a Game View capture. The Play probe checks insufficient-money rejection, balance-driven button enablement, no modal ownership and unchanged cursor lock.
- SampleScene restored; its disk SHA-256 remained unchanged and Play Mode Start Scene remained None. No production progression reset or purchases.
- The cap-guard probe uses an injected full owned list; it is not a 50-monster performance soak.

Re-run the opt-in probe after gameplay changes. Never set a validation scene as the editor's permanent Play Mode Start Scene.

### Combined chest status panel — 2026-10-08

- One 530 × 144 world-space billboard now contains chest name/icon, level badge, HP, thin XP bar, income per interval, Armor, MR and XP percentage. Removed the separate side stats canvas.
- Reuses existing HP/XP art, payout popup and Repair button. New compact frame/icon/badge use textured SVG import for uGUI. English and Vietnamese labels added.
- Unified visibility uses Panel Near/Hide Distance, Panel Offset, Panel World Scale and transition time in TreasureChestData; old separate-stats serialized fields are retained but hidden.
- Connected Editor compilation: no errors. All 14 parameterless TreasureChestTests methods invoked directly in Editor passed, including one-canvas layout, Repair, bar bindings and far/near visibility. This is not a fresh Test Runner or Play Mode run; parameterized cases were not rerun in this revision.
- Camera-rendered isolated additive preview visually inspected. Preview scene closed without saving; SampleScene active and Play Mode Start Scene remains None. No production saves, currency or gameplay settings modified.
