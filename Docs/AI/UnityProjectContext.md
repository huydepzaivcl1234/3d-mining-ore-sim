# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `C:/Users/huyancut/3d mining sim`
- Last analyzed: 2026-09-06
- Last analyzed commit: `9b33ee3`
- Early-stage 3D mining simulator. The initial gameplay content is Stone, Coal, and Copper.

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

- Data-driven ScriptableObject configuration, modeled after the user's Tower Defense project.
- Each ore owns an independent OreData asset and prefab; runtime state lives on the Ore component.
- Each `OreData` contains only ore-owned values, including durability, rewards, required power, and NPC mining slots.
- NPC-only tuning lives in `Assets/GameData/NPC/NpcData.asset`.
- `OreSpawner` is the spawn manager and reads ratios, spawn speed, limits, and placement from `Assets/GameData/Spawning/OreSpawnData.asset`.
- Ore placement keeps collider bounds above the spawn surface; rotation range and surface clearance remain designer-configurable.
- Ore health bars use collider bounds in world space, so ore rotation cannot rotate the bar anchor into the ground; per-ore offset and scale live in each `OreData`.
- The editable TMP upgrade panel buys money, rare-ore weight, and ore-damage stacks from `Assets/GameData/Upgrades/MiningUpgradeData.asset`.
- Upgrade panel presentation is authored as real prefab UI and seeded from `Assets/GameData/UI/MiningUiData.asset`.
- Shared economy, HUD animation, click, and camera tuning lives in `Assets/GameData/MiningGameData.asset`.
- NPC miners reserve limited slots around compatible ores and use Rigidbody/CapsuleCollider collision.
- The editable runtime HUD uses TextMeshPro components and is created as serialized prefab content by the setup menu.
- No global managers or save system exists yet.

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
