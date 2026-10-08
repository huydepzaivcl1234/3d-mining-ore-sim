# Miner defense (2026-09-30)

## Behavior

- Mushrooms enter the mining bake first, then prefer an actively mining NPC inside their detection range. Outside the bake they cannot acquire or hit miners. Without an eligible miner inside the mine they pursue nearby players or approach the nearest active ore.
- Pursuit uses the existing MiningNavigation NavMesh/grid routes, with throttled path queries. No new navigation package or scene bake is added.
- At melee range, the mushroom stops and warns for at least 2 seconds before starting Headbutt. A blinking red `!` and remaining time appear above the threatened miner.
- Damage is still applied at the authored Headbutt contact moment, once per strike. Distance, facing and obstacles are checked again; moving out of range, disabling the miner or killing the mushroom prevents the hit. Hitting the mushroom during its warning interrupts the windup.
- Miners have fixed 20 HP by default (NpcData.MinerHealth), no individual combat level or level-based HP scaling. At zero HP, they remain purchased/active but stop moving/mining, release their mining reservation, and show a yellow recovery countdown. After 10 seconds they restore their fixed HP and resume target acquisition.
- Miner capsules ignore monster colliders in both spawn orders and after re-enable, so miners can pass through monsters. Floor/scenery collision stays enabled; explicit combat range/facing/obstruction checks still apply damage.
- User confirmed: no new carrying/selling mechanic for now. No already-earned Money, Gem, XP or inventory is deducted on knockout. The repository currently awards mining income immediately.
- Monster/player reward/progression systems have not otherwise been removed or remapped in this patch.

## Editable GameData

- `Assets/GameData/NPC/NpcData.asset`: Miner Health and Knockout Seconds.
- `Assets/GameData/Monsters/MushroomRewards.asset`: Miner Warning Seconds (minimum 2).
- Existing mushroom prefab Detection Range, Attack Range, Damage and Hit Moment remain authored as before. Only detection range limits target acquisition, not an old spawn rectangle.
- No NPC down-animation clip was provided; this patch uses the existing stationary animation and countdown without rewriting its controller.

## Random mining encounters (replaces rectangular monster zones)

- Existing `MonsterSpawnZone` script name and GUID remain only to preserve the authored scene's prefab table, counts, references and world-switch integration. Its Area Size field, rectangle gizmo, Contains boundary and random rectangular patrol are removed. The object's position no longer determines spawn locations; no scene save/migration is needed.
- The scene's MiningNavMeshBuilder/NavMeshSurface is authoritative (currently Volume, 32x32m). Spawns sample outside that bake volume, not around individual miners/ores and not around the old monster object's Transform. The optional Mining Surface field can specify the mining bake explicitly. An absent, disabled or non-Volume surface defers spawning rather than inventing a fallback zone.
- Every candidate is checked against the actual baked NavMesh: the horizontal distance to its nearest walkable point must be 4-7 metres. Ground slope/height, capsule clearance, existing monster spacing and minimum player distance (default 3m) must also pass. Population limit and prefab weighting remain; the old initial population and endless spawn interval are replaced by the daily schedule below.
- Outside the bake, mushrooms physically walk toward an entry point slightly inside its edge (CharacterController still respects scenery). Once inside, normal MiningNavigation pursuit starts; they may then acquire miners. This avoids asking for an impossible NavMesh path from an off-mesh spawn. Idle mushrooms inside navigate toward the closest active ore's surface, stopping short of its collider. Existing detection radius, obstacle checks, 2-second warnings and knockout rules remain.
- Zone-entry and zone-loop audio have been removed at the user's request. The spawner creates no zone AudioSources, and MiningAudioData no longer exposes or preloads these settings. Loot/drop and other game audio remain unchanged; no source audio files were deleted.
- Edit Mining Surface, Minimum Spawn Distance, Maximum Spawn Distance, Minimum Player Distance and population settings on the existing spawner component. The optional setup menu is now `Mining Simulator/Setup/Create Mining Monster Spawner`.
- Validation (latest revision): Unity Editor compiled without errors. The actual Play Mode population walked from outside into the bake; actual miner HP/max were 20/20 and all miner/monster pairs had ignored collision. A separate runtime probe produced ten monsters: all outside the bake, nearest horizontal NavMesh distances 4.33-6.72m, all collision pairs ignored. Forced target checks confirmed no miner acquisition outside and successful acquisition after entering. Probe removed and Play Mode stopped without saving the scene. No full crowd soak test was performed.

## Daily waves and forecast

- Each in-game morning rolls a daily roster of 3-12 monsters (inclusive). Edit Minimum Per Day / Maximum Per Day on the existing MonsterSpawnZone component. DayNumber advances with the existing DayNightSystem; it is a session clock, not new saved progression.
- Waves are evenly distributed over the remaining daylight, never at night. Maximum Per Wave defaults to 3; Larger Wave Relative Weight defaults to 0.35. Wave sizes have weights 1, 0.35, 0.35 squared, so larger groups are rarer. A 10,000-roll check produced approximately 68% / 24% / 8% for 1 / 2 / 3 monsters.
- The existing Maximum Alive limit and all perimeter safety checks still apply. Blocked spawns retry once a second within their scheduled slot; unfinished entries expire at the next slot or sunset. Missed waves do not accumulate into a burst. Therefore the planned daily roster is an upper bound, not a guarantee when the mine is blocked or population is full.
- Monster entries retain weighted Chance and now accept an editable Icon sprite. Additional monster types can be added to this same list. Mushroom has a fallback resource sprite at Assets/Resources/MiningMonsterIcons/MushroomMonster.asset, rendered with the existing Mushroom mesh and T_Mushroom_C color texture. Its sprite/GUID remain unchanged; the gray preview has been replaced with a colored transparent portrait.
- A read-only forecast appears under Settings: day number, icon and planned quantity per entry, plus total still arriving. It is created at runtime without saving SampleScene, does not block clicks, and uses the existing modal HUD coordinator to disappear while Upgrade or another panel is open.
- English/Vietnamese keys MONSTER_FORECAST_TITLE and MONSTER_FORECAST_REMAINING use identical placeholders. Show Daily Forecast controls this HUD on the spawner.
- Validation: Unity compilation and actual Play Mode had no errors. A nine-monster roster had wave sizes 2/1/1/2/3 at evenly spaced daylight progress 1/6 through 5/6. Night produced no new spawn; the following morning rolled a new roster. Vietnamese labels, icon assignment, modal hiding (alpha 0) and restoration after closing (alpha 1) were checked. Play Mode was stopped without saving the scene.

## Camera ownership

Scene inspection confirmed one Main Camera, MiningOrbitCamera and no active Cinemachine Brain/virtual cameras. ThirdPersonController still had its separate look-input/PlayerCameraRoot rotation branch.

MiningOrbitCamera now claims `ThirdPersonController.ExternalCameraControl` while enabled and releases it on disable. ThirdPersonController skips only its camera rotation in that mode; movement, jumping, footsteps and combat Animator remain unchanged. The Starter Assets assembly does not reference Assembly-CSharp. Its Cinemachine fallback is retained for contexts without the orbit owner.

## Validation

- Unity Editor imported/compiled without errors.
- Runtime camera ownership flag was true with orbit enabled. Injected look input did not rotate the Starter Assets target; disabling/re-enabling the orbit correctly released/reclaimed camera ownership.
- Deterministic miner checks: damage cap, knockout stopping mining, no repeated damage while stunned, command rejection while stunned, recovery with full HP on the same retained NPC.
- Play Mode checks using the actual mushroom prefab: 2-second warning and billboard, no early damage, moving NPC out of range cancels windup, player hit interrupts warning, Headbutt contact damages exactly once, knockout countdown, monster death clears warning.
- Temporary objects were removed and Play Mode exited; no scene or prefab was saved by this work.
- Existing Missing Script warning remains. Kinematic-body warnings from the first temporary probe were addressed by avoiding velocity writes to kinematic bodies.
- Long crowd-play and subjective visual polish are not covered by these scripted checks.

ZIP preserves project-relative paths and excludes SampleScene.unity and unrelated world/trader/package/audio edits.
