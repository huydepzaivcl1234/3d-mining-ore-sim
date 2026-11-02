# Readable player combat stats

- PlayerArmature > Player Stats now exposes editable linked Core Data fields: base damage, attack range (m), base fire rate (volleys/s), projectile flight speed (m/s), plus effective combat totals below.
- Existing balance migrated from SurvivorCombat.asset: 20 base damage, 13 m range, 1.53846154 volleys/s (0.65 s interval), 22 m/s projectile speed. Level/card/buff modifiers still apply afterward. Existing cadence cap remains 12.5 volleys/s.
- Range source is shared by target acquisition, projectile travel budget, Stats UI and selected-player Gizmo. The wire sphere represents the actual 3D acquisition bound; the floor circle makes its horizontal footprint easy to read. Toggle Gizmos in Scene and select PlayerArmature. Gizmo can be disabled in Player Stats.
- Stats UI shows Base / Total DMG, fire rate, range and a dedicated projectile speed row, retaining existing font/style. A row is cloned from the authored Range row at initialization, guarded against duplicate creation.
- Player Stats Inspector labels have Vietnamese descriptions and explicit units. Editing changes the linked shared asset with serialized Undo support. Other health/movement/XP settings remain available through the Open All Stats button. Edit-mode summary is explicitly a preview without loading save progression.
- Runtime and Editor assemblies compiled offline with the installed Unity Roslyn compiler and existing project references; both exit code 0. Compiler outputs/logs are in the workspace Deliverables/ReadableCombatStats-20261028/Compile directory, outside Assets.
- No Unity window automation, CLI Editor commands, or Play Mode execution were used this turn as requested. Runtime targeting behavior and the newly added UI row have not been visually playtested this turn.
- Existing project compilation warnings remain; no new screenshot or live Console claim is made.

Modified sources: PlayerCoreStatsData, PlayerStats, SurvivorAutoCombat, PlayerStatsPanel; added editor-only PlayerStatsInspector. Before/ contains prior versions. Save keys, character rig and movement/camera scripts are unchanged.
