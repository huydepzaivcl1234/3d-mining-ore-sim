# Mushnight night thief

## Implementation and configuration

- Native model/rig: `Assets/Art/Mushnight/Mushnight.fbx`. Idle and Walk use trimmed looping source takes, not the original mushroom rig. The thief has no attack animation/state.
- Prefab: `Assets/Prefabs/Monsters/MushnightMonster.prefab`.
- Settings: `Assets/GameData/Monsters/MushnightRewards.asset`, Mushnight section. Defaults: 3 seconds uninterrupted stealing, 10 Money at level 1 plus 5 per additional level, approach speed 2, escape speed 3.5. Ranges and animation state names are editable.
- Night encounter: `Assets/Resources/MonsterSpawnRoster.asset`, Night Thief / Thieves Per Night / Night Thief Start Progress. Default one optional extra thief per night, starting at 5% of night. This encounter does not replace the normal daily wave budget and shares the existing 50-living-monster cap.
- The thief uses the existing WorldNavigationGrid and MonsterPathFollower A* motor. Cloaked approach ignores the player and is excluded from player auto-target/lunge acquisition. Accidental manual damage reveals it; it flees instead of attacking. Once revealed it does not cloak again.
- Reaching the chest reveals it. Player proximity interrupts unfinished stealing and makes it flee; returning to the chest starts a fresh 3-second channel. A completed theft debits only available Money, never fires purchase events, and carries that exact amount until death or escape. Killing it refunds once to the robbed wallet without reward multipliers. A broken chest makes it leave without theft.
- Normal XP and item drops remain on the existing monster reward pipeline. Thief gold reward is zero; stolen-money recovery is separate. No player save migration/reset required.

## Asset admission

Source: user-supplied `C:/Users/huyancut/Downloads/Mushnight_Model.zip`. User explicitly confirmed rights and allowed importing despite the absent license manifest/receipt. Only its 23 Assets/Art entries were admitted; no archive README, Blender source, scripts, or external previews were imported. No existing assets were overwritten. The game-dev vendoring CLI was unavailable; this is an explicit approved import exception, not a canonical package receipt.

Source/imported FBX SHA-256: `00ebf44796a80d90d475f15bcca40341ed285997edd3134c2bb7d4dafdeea219`. Unity importer metadata was deliberately changed to trim animation takes and disable camera/light import.

## Validation, 2026-10-09

Connected Editor: 3d mining sim, Unity 6000.5.3f1, through Unity MCP.

- Scripts imported/compiled; final Console error query returned zero entries.
- Four parameterless MushnightTests passed via direct reflection invocation in the Editor: finite level scaling; clamped theft without MoneySpent; separate non-attacking roster; reveal on damage and exactly-once refund. These are not claimed as a fresh Unity Test Runner run.
- Isolated Play Mode probe passed: daytime spawn rejected; night spawn accepted; invisible A* approach moved past a player; no debit before 3 seconds; visible escape and evasion; no player/chest damage; exact-once gold refund after killing; one-per-night quota.
- Probe uses an isolated wallet, cloned chest data with persistence disabled, inactive unsaved clock, and flat temporary navigation terrain. An initial Editor-assembly component placement could not run; the opt-in probe was moved outside the Editor assembly while guarded with UNITY_EDITOR, then rerun successfully.
- Production scene roots were restored to their original active flags. Temporary additive scene closed without saving. Active scene is SampleScene; Play Mode Start Scene remains None. No SampleScene file was saved or included in the delivery.

Not verified: long crowded-map soak, every production obstacle arrangement, or visual inspection of every animation frame. Escaped loot is intentionally lost; only killing the carrier refunds it.
