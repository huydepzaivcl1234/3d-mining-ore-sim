# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `C:/Users/huyancut/3d mining sim`
- Last analyzed: 2026-09-06
- Last analyzed commit: `9b33ee3`
- Early-stage 3D mining simulator. Gameplay content covers Stone, Coal, Copper,
  Iron, Gold, and Diamond, with Legendary reserved for future ores.

## Confirmed Environment

- Unity version: 6000.5.3f1
- Render pipeline: Universal Render Pipeline 17.5.0
- Input system: Input System package 1.19.0 with a project input-actions asset
- Target platforms: unresolved; current project settings are the Unity URP starter defaults

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Rendering | URP 17.5.0 | Confirmed | `Packages/manifest.json`, `Assets/Settings` |
| Input | New Input System 1.19.0 | Confirmed | `Packages/manifest.json`, `Assets/InputSystem_Actions.inputactions` |
| Navigation | AI Navigation 2.0.13 | Confirmed | `Packages/manifest.json` |
| Tests | Unity Test Framework 1.7.0 installed; no first-party tests yet | Confirmed | `Packages/manifest.json`, repository search |
| Networking | Multiplayer Center is installed, but no gameplay networking usage exists | Confirmed | `Packages/manifest.json`, repository search |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/Ores/Models` | Source FBX ore models | Confirmed | Starter ore feature |
| `Assets/Prefabs/Ores` | One independent prefab per ore | Confirmed | Starter ore feature |
| `Assets/GameData/Ores` | One OreData ScriptableObject per ore | Confirmed | Starter ore feature |
| `Assets/GameData/NPC` | NPC-only balance and collision data | Confirmed | NPC feature |
| `Assets/GameData/Spawning` | Ore spawn ratios, timing, limits, and placement | Confirmed | Spawn manager feature |
| `Assets/GameData/Upgrades` | Mining upgrade percentages, stack limits, and price progression | Confirmed | Upgrade panel feature |
| `Assets/GameData/UI` | Editable mining HUD layout, colors, typography, and outline values | Confirmed | Upgrade panel presentation |
| `Assets/GameData/Audio` | Background music, SFX clips, volumes, pitch, and playback limits | Confirmed | Audio manager feature |
| `Assets/GameData/Rebirth` | Rebirth requirement growth, permanent money boost, and save key | Confirmed | Rebirth feature |
| `Assets/Scripts/Ores` | First-party ore runtime and editor code | Confirmed | Starter ore feature |
| `Assets/Scenes` | Unity starter scene | Confirmed | Repository inspection |
| `Assets/Settings` | URP renderer and pipeline assets | Confirmed | Repository inspection |

## Assembly Boundaries

- No first-party `.asmdef` exists. Runtime scripts compile into Assembly-CSharp.
- Scripts under `Editor` compile editor-only and may reference UnityEditor.

## Scenes And Startup Flow

- Build scenes: `Assets/Scenes/SampleScene.unity`
- Likely startup scene: `SampleScene`
- Scene loading flow: no custom flow exists yet

## Architecture

- Lucky Block progression includes separate reward and drop-chance upgrades owned by
  `MiningUpgradeData` and applied by the existing `MiningUpgradeSystem`.
- The active Scene upgrade panel can be refreshed through
  `Mining Simulator/Setup/Refresh Lucky Block Upgrade UI`; it adds the two Lucky Block
  cards and their dedicated sprites without replacing designer-assigned existing icons.
- `MiningHitPunch` is reusable smooth scale-and-lift feedback. Ore roots animate with
  their colliders; falling Lucky Blocks animate only their visual child.

- Data-driven ScriptableObject configuration, modeled after the user's Tower Defense project.
- Each ore owns an independent OreData asset and prefab; runtime state lives on the Ore component.
- Each `OreData` contains only ore-owned values, including durability, rewards, required power, and NPC mining slots.
- NPC-only tuning lives in `Assets/GameData/NPC/NpcData.asset`.
- `OreSpawner` is the spawn manager and reads percentages, spawn speed, limits, and placement from `Assets/GameData/Spawning/OreSpawnData.asset`.
- Ore placement keeps collider bounds above the spawn surface; rotation range and surface clearance remain designer-configurable.
- Ore health bars use collider bounds in world space, so ore rotation cannot rotate the bar anchor into the ground; per-ore offset and scale live in each `OreData`.
- The editable TMP upgrade panel buys money, rare-ore chance, and ore-damage stacks from `Assets/GameData/Upgrades/MiningUpgradeData.asset`.
- Upgrade panel presentation is authored as real prefab UI and seeded from `Assets/GameData/UI/MiningUiData.asset`.
- Shared economy, HUD animation, click, and camera tuning lives in `Assets/GameData/MiningGameData.asset`.
- NPC miners reserve limited slots around compatible ores and use Rigidbody/CapsuleCollider collision.
- NPC movement and rotation are owned by `FixedUpdate`. Velocity acceleration/braking, collider-safe
  mining slots, smoothed local NPC separation, ore sight SphereCast, obstacle steering, stuck recovery,
  committed targets, and cooldown-limited blocking-ore switching are configured in `NpcData`.
  Multi-direction clearance probes keep a stable detour around clustered ores; when every sampled
  side route is blocked, the miner reverses to create space before evaluating another route.
  A reserved NPC starts mining immediately when it enters the configured mining range; reaching an
  exact stand-slot coordinate or waiting for stuck recovery is not required.
- NPC colliders still interact with ores and the environment, while optional NPC-to-NPC physical
  pushing is disabled in `NpcData` to prevent Rigidbody contact jitter; separation keeps miners apart.
- Ore depletion raises a reward event. `OreSpawner` creates the authored TMP reward popup prefab at
  the ore collider top, and popup motion, lifetime, scale, text, and colors live in `MiningUiData`.
- `MiningMoneyFormatter` is the single formatter for balance, prices, Rebirth requirements,
  and ore reward popups. Compact values place the suffix at the decimal point
  (`1,000 -> 1K`, `1,500 -> 1K5`) and cover the wallet's full finite float range.
- Rarity is explicit (`Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`). Zero-percent Rare+
  ores unlock progressively through the rare-spawn upgrade using rarity rules in `OreSpawnData`.
- Upgrade data also controls ore-spawn speed and NPC movement speed stacks, costs, and per-stack percentages.
- The editable runtime HUD uses TextMeshPro components and is created as serialized prefab content by the setup menu.
- HUD statistics, shop buttons, and upgrade cards have editable icon slots with TMP fallback symbols;
  icon sprites, size, position, colors, and padding live in `MiningUiData`.
- The five upgrade cards use dedicated transparent sprites from `Assets/UI/Icons/Upgrades`.
  All authored HUD buttons use interruptible, unscaled-time hover punch and click bounce feedback;
  scale and duration values live in `MiningUiData`.
- `PlayerWallet.currentMoney` is the single authoritative money value. It is editable in the Inspector
  during Play Mode and publishes `MoneyChanged` immediately so the HUD and all consumers stay synchronized.
- The Scene `GameManager` keeps only the wallet at its root. Ore, NPC, upgrade, rebirth, audio, UI, and
  camera behaviours are organized on named child objects instead of stacking every component together.
- The obsolete automatic drill runtime, data, prefab, controller, UI panel, and editor setup code were removed.
  The user's source FBX remains untouched for a future drill implementation.
- `MiningAudioManager` owns one looping music source and one shared SFX source. It reacts to ore hits,
  ore breaks, NPC purchases, upgrade purchases, panel navigation, and rebirth. It repairs missing
  AudioSource references at runtime. All clips and playback values live in
  `Assets/GameData/Audio/MiningAudioData.asset`.
- `Mining Simulator/Tools/Audio Manager` opens an editor tool for audio assignment and runtime setup.
- `MiningRebirthSystem` resets money and all temporary upgrade stacks at the configured requirement,
  persists completed rebirths with a versioned PlayerPrefs key, and applies the permanent money
  multiplier before ore rewards are calculated. Its values live in `MiningRebirthData.asset`.
- `MiningRebirthPanel` presents an editable red/white confirmation modal and an event-driven HUD with
  a green UI MicroBar. Rebirth layout, typography, colors, and smooth button animation live in
  `MiningUiData.asset`.
- Ore and Lucky Block selection tables are authored as percentages. Runtime selection normalizes
  valid entries to 100%, and the rare-ore upgrade increases Rare+ percentages before normalization,
  matching the probability model intended for future gacha content.
- `DayNightSystem` is an independent Scene object. `DayNightData` owns cycle duration, transition,
  sun/ambient/fog presentation, day-only Light Stone chance, night-only Dark Stone chance, and aura
  tuning. `OreSpawner` checks the period-specific percentage before falling back to the unchanged
  normal rarity table. Existing timed ores remain in the world after the period changes.
- No unified save system exists yet; Rebirth count and inventory use separate versioned
  PlayerPrefs keys.
- `MiningItemSystem` listens to Ore and Lucky Block reward events, rolls the configured
  source chance and normalized item selection table, and spawns a short-lived Rigidbody
  world drop with a light bounce. Drops enter a saved 32-slot inventory automatically;
  every `MiningItemData` supports a designer-assigned model and icon with safe fallbacks.
- The starter Common items are Apple (NPC damage), Banana (money reward), and Green Apple
  (NPC movement speed). Effects are timed, repeat use extends the timer, and the active-effect
  toast displays live remaining time. Each slot stacks up to 64. Normal Rebirth preserves
  inventory; the Settings data reset clears inventory, active effects, and world drops.

## Coding Conventions

- Namespace: `MiningSimulator.<Feature>` for new first-party code.
- Serialized fields: private `[SerializeField]` fields with read-only public properties.
- Editor automation: idempotent menu commands under `Mining Simulator`.
- Async: no runtime async convention established.

## Testing And Validation

- EditMode tests: none
- PlayMode tests: none
- CI/build validation: none detected

## Available Unity Tooling

- Unity 6000.5.3f1 is installed locally and can be run in batch mode.
- No Unity MCP provider was found in package configuration or available tools.
- Repository Git operations are available through Git and the in-Editor Mining Git window.

## Important Constraints

- Preserve Unity-generated `.meta` GUIDs.
- Binary Blender/image files are managed through Git LFS.
- Do not stage unrelated Unity settings changes when implementing isolated features.
- Preserve designer-authored UI positions, sizes, anchors, pivots, and assigned icon sprites
  unless the user explicitly requests that exact property to change.
- UI refresh/setup tools must reuse and update the existing named UI object in place. They must
  not create a duplicate UI object, reset unrelated layout values, or overwrite a designer icon.
- Keep every UI change limited to the exact requested target; preserve all unrelated UI state.

## Unknowns And Confidence

- Mining controls, player progression, persistence, world generation, and final target platform are not designed yet.
- GLB importing is not installed; gameplay prefabs use separate FBX models.

## Source Files Inspected

- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`
- `.gitignore`, `.gitattributes`
- Tower Defense reference: `TowerData.cs`, `EnemyData.cs`, `TowerDefenseSetupMenu.cs`

<!-- unity-onboarding:generated:end -->
