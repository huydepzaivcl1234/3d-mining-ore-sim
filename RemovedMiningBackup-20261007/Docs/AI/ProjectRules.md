# Project rules for AI contributors

## Code structure and change workflow

- Inspect the actual runtime owner and serialized references before editing. Implement a focused change, compile in the connected Editor, then verify normal, failure, save/load and teardown paths.
- Prefer small cohesive classes, clear names, early returns and composition. Use inheritance only for a genuine shared contract; do not add interfaces, managers or wrappers merely to appear object-oriented.
- Keep item/equipment bonuses in item GameData, purchases in the inventory/economy owner, and UI in presentation components. Player baseline data must not duplicate equipment effects.
- Preserve serialized field identities, component GUIDs, animation events, localization and existing saves. Migrate additive save fields safely; do not reset user data during validation.
- Bind and unbind events symmetrically; cache references; avoid per-frame scene-wide searches. Public entry points must validate ownership, capacity and currency before mutating state.
- Do not rewrite unrelated systems or change SampleScene as part of a code cleanup. Report what was actually verified rather than claiming every feature was tested.

- Work on `main`. Do not create or publish feature branches for this project.
- Deliver changes as a ZIP preserving paths relative to the project root. Do not include `Assets/Scenes/SampleScene.unity` in a ZIP.
- Preserve the user's authored scene layout and prefab values. Change a scene only when explicitly requested; use opt-in Editor setup tools for scene authoring.
- Keep exactly one repository README: `/README.md`. Keep project instructions and AI context in `Docs/AI/`. Keep Unity runtime `.txt` assets, package metadata and vendor assets needed by the project in place.
- Localize game UI through Lean Localization and the matching `Assets/GameData/Localization/English.txt` and `Vietnamese.txt` entries. Keep formatting placeholders identical.
- For handed-off Unity changes, let the Unity Editor import and compile. Do not use a command-line Unity build in place of the user's requested Editor compile. Report when Play Mode validation is unavailable.
- On world-swap work, respect the user's separate instruction to leave the commit and push to them.
- Do not hardcode designer-facing gameplay values or eligibility gates. Expose levels, XP, spawn chances, rewards, timing, ranges and effect settings in the existing GameData/Inspector configuration. Keep numerical safety validation (finite values, nonnegative amounts, valid indices), but never use it to secretly override an authored gameplay setting. Defaults are editable defaults, not mandatory rules. Provide explicit opt-in debug/test controls for gated features (for example boss spawning below the normal level requirement); keep debug controls disabled by default, show them clearly, and never silently overwrite player saves while testing.
