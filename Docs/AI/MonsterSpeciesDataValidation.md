# Species GameData and Mushnight stamina / loot display

## Data ownership

Each existing species asset now uses its own concrete ScriptableObject type:

| Existing asset in Assets/GameData/Monsters | Type |
| --- | --- |
| BatRewards.asset | BatData |
| GolemRewards.asset | GolemData |
| MushroomRewards.asset | MushroomData |
| MushranonRewards.asset | MushranonData |
| ForestGolemRewards.asset | ForestGolemData |
| MushnightRewards.asset | MushnightData |

The asset paths, GUIDs and file IDs are preserved. Prefabs retain their existing references. Create new data from Mining Simulator / Monsters / species, not a generic reward asset.

MonsterRewardData is now a common stats/rewards base with no Mushnight settings. MonsterCombatSettings has no Forest Golem field. ForestGolemData owns its shockwave, charge/burn and boss pulse/regeneration settings. MushnightData owns theft, flight, stamina and stolen-gold presentation settings. Ordinary species do not expose these two species' skills in their Inspectors. Common combat, health, navigation and loot infrastructure remains shared rather than duplicated across species.

ForestGolemAbility reads the original species data, not a boss loot override. The existing custom Inspector supports derived data types and still displays attack speed.

## Mushnight stamina

Current stamina is instance-only; modifying it never writes GameData or player saves. Defaults in MushnightRewards.asset / Mushnight:

- Max Stamina: 100.
- Stamina Per Moving Second: 10, charged for actual horizontal displacement, not failed path requests.
- Recovery Per Second: 25 after Recovery Delay: 1 second without movement.
- Resume Stamina Fraction: 0.5. Exhausted thieves remain stationary until this threshold is reached, then resume their existing intent.

The stamina clock receives the same gameplay delta as the thief. Rune freezes therefore suspend movement and recovery together. Gravity/ground contact remain active during stamina rest. Standing at the chest can recover stamina; stamina does not consume or create stolen Money.

## Stolen-gold label

MushnightLootLabel creates a non-interactive world-space Canvas above the head. It faces the camera and shows the actual carried amount after a successful theft. It is hidden while cloaked, when carrying zero Money, and after death/escape. Refund logic remains exactly once.

Editable presentation fields live in MushnightRewards.asset / Mushnight: Gold Label Width, Height, Font Size, Head Offset, Color and Font. Uses the existing project HUD font and Lean Localization key Mushnight.StolenGold in English/Vietnamese. No screen-space overlay or input blocking was added.

## Validation, 2026-10-09

- Connected Unity 6000.5.3f1 imported and compiled scripts; final error query returned zero Console errors.
- 24 Editor test cases passed by direct reflection invocation (MushnightTests, ForestGolemTests, BossSkillTests, MonsterCombatSettingsTests). This was not a fresh Unity Test Runner run.
- Compared the before/after serialized asset text, excluding script identity and moved species blocks: all six species' common values were unchanged. Forest Golem's moved skill block was also unchanged, including the user's authored VFX references, damage, radii and boss values.
- Isolated Play Mode probe passed night-only spawn, invisible player-ignore A* approach, 3-second theft, visible escape/evasion, no attacks, correct carried-money label, stationary exhaustion, threshold recovery/resumed movement, exactly-once refund and night quota.
- The first runtime probe attempt reported movement during exhaustion. Reimporting the updated probe and rerunning with timing diagnostics passed; the final probe distinguishes resting from the resumed-movement phase and does not force the exhausted test configuration into repeated one-frame re-exhaustion.
- Rendered and visually inspected the isolated 25-Money label: Captures/Mushnight-Stolen-Gold-20261009.png. This was an explicit isolated preview, not production wallet manipulation.
- Temporary additive scene closed without saving; original active flags restored; active scene SampleScene, Play Mode stopped, Play Mode Start Scene None. No scene file included in delivery, no save reset, commit or push.

Not claimed: a crowded production-map soak or exhaustive review of every animation frame. This change does not redesign the shared monster AI/navigation system.
