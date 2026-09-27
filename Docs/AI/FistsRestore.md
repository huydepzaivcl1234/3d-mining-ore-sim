# Fists restored

Player uses its existing Animator controller, E to toggle combat and right mouse to punch. No equipment, sword draw/sheath, controller switching, or target locking remains in PlayerCombatInput. Health, hit reaction, respawn, stamina, progression and assigned SFX are preserved.

Low sword models and imported animation clips are retained for manual setup. Generated sword assets/tools were removed; legacy Fists data and punch VFX remain preserved but do not drive equipment.

For ZIP installation: close Unity, extract into the project root, then run Docs/AI/RemoveOldSwordFiles.ps1 with that project root. It moves only the listed obsolete files outside Assets into a recoverable backup. Reopen Unity to import. SampleScene is not included.

Validation: source reference scan and git diff check passed. The running Unity Editor imported and recompiled the change successfully (Logs/Editor.log: forced synchronous recompile, assembly reload). No compiler errors appeared in the inspected import log. Interactive fist/animation Play Mode testing was not completed; desktop input was interrupted by concurrent user input.
