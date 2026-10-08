# Combat effects and monster audio

Settings remain in GameData, not on the Player Inspector. No scene layout or Animator changes are required.

## Effects

- Player weapon: `Assets/GameData/Player/PlayerStatsData.asset`.
- Mushroom attacks: `Assets/GameData/Monsters/MushroomRewards.asset` (other monster reward assets expose the same fields).
- `burnDamagePerTick`: damage per tick; `burnTickSeconds`: seconds between ticks; `burnDurationSeconds`: total duration. Zero damage or duration disables burning.
- `lifeStealPercent`: percentage of actual HP removed by a direct attack. Overkill does not increase healing. DOT does not recursively trigger life steal.
- `healingBonusPercent`: increases healing amount, including regeneration and life steal. It does not change regeneration timing and cannot resurrect dead characters.
- Repeated burn hits refresh duration without postponing the next tick. Burns do not stack; the strongest DPS is retained. Death/respawn clears burning.
- Mushroom burn damage scales with its rolled level/stat multiplier, like its direct attack damage.

Editable example settings: player burn 1 HP / 1 second for 3 seconds, life steal 10%, healing bonus 20%; mushroom burn 0.5 HP / 1 second for 2 seconds, life steal 5%, healing bonus 10%. Set the values to 0 to disable an effect.

## Audio

In `Assets/GameData/Audio/MiningAudioData.asset`, edit:

- Monster Drop Sfx: icon launched from corpse (assigned existing `pop.mp3`).
- Monster Loot Sfx: inventory accepts the arriving icon (assigned existing bonus clip). No pickup sound when inventory rejects it.
- Monster Zone Enter Sfx: one cue on crossing into a living monster zone (assigned existing `portal.mp3`).
- Monster Zone Fade Seconds: smooth entry/exit and cue-end volume fade (0.6 seconds).
- Monster Zone Loop: optional ambient loop; deliberately unassigned because no monster-zone loop was specified. Assign your desired clip to enable continuous zone ambience.

All sources respect Master/SFX volume, mute, mixer routing and global lost-focus audio pause. Runtime zone sources are created automatically; no scene audio components need to be authored.

## Validation (Unity MCP, 2026-09-30)

- Connected to Unity 6000.5.3f1, project `D:/3d mining sim`.
- Editor import/compile completed; Console had no errors after compilation.
- Executed deterministic health checks inside Editor: healing bonus and unchanged interval, repeated-hit burn timing, multi-tick expiry, strongest burn, respawn cleanup, overkill cap and no healing after death.
- Executed the actual player DamageTarget path: 25 attack damage against 10 remaining HP heals 1.2 HP at 10% life steal and +20% healing.
- Play Mode: real Mushroom Spawn Zone created its entry AudioSource; temporary silent-loop probe confirmed volume fades in and out, then stops. Probe objects were removed and Play Mode exited.
- One existing Missing Script warning remains. AudioListener was paused by the project's lost-focus setting, so these checks validate runtime wiring and volume behavior, not subjective audible quality.
- Scene was not saved/packaged; existing scene/prefab edits and unrelated world/trader/package changes are preserved.
