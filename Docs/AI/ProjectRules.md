# Project rules for AI contributors

- Work on `main`. Do not create or publish feature branches for this project.
- Deliver changes as a ZIP preserving paths relative to the project root. Do not include `Assets/Scenes/SampleScene.unity` in a ZIP.
- Preserve the user's authored scene layout and prefab values. Change a scene only when explicitly requested; use opt-in Editor setup tools for scene authoring.
- Keep exactly one repository README: `/README.md`. Keep project instructions and AI context in `Docs/AI/`. Keep Unity runtime `.txt` assets, package metadata and vendor assets needed by the project in place.
- Localize game UI through Lean Localization and the matching `Assets/GameData/Localization/English.txt` and `Vietnamese.txt` entries. Keep formatting placeholders identical.
- For handed-off Unity changes, let the Unity Editor import and compile. Do not use a command-line Unity build in place of the user's requested Editor compile. Report when Play Mode validation is unavailable.
- On world-swap work, respect the user's separate instruction to leave the commit and push to them.
