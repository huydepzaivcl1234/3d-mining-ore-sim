# Rune movement and horizontal panel correction

RuneStation configures horizontal presentation at Start for both prefab-backed and unpacked scene shrines. Existing VerticalLayoutGroup is disabled, not destroyed. When no HorizontalLayoutGroup exists (Unity disallows adding another LayoutGroup beside the old one), rows are positioned at equal Y using editable barSize and panelSpacing. No scene save or hierarchy replacement is needed.

ThirdPersonController uses its own current speed for acceleration in unscaled movement mode: CharacterController.velocity is tied to the scaled clock and is unsuitable when the rune stops Time.timeScale. Normal gameplay retains the existing velocity-based acceleration.

Rune rows display bonus per rank and accumulated bonus in the current language. Attack speed, damage and health use percent; armor and MR use flat values. Values come from the configured track, not hardcoded bonuses. RuneStation supplies its referenced GameData to the player's existing RuneUpgradeProgress, preserving saved ranks even when the asset is moved outside Resources.

Validation through Unity MCP: compilation and Console without errors; unpacked scene clone gives five rows at Y=0 with X=-1178,-589,0,589,1178; a paused-clock motor regression preserves speed 1.5 instead of dropping to zero; nearby step height changes 0.25 to 0.45 and restores 0.25. Preview verifies five colored bars and bonus labels. Saved-rank sample gives percent totals 15/25/35 and flat MR/armor totals 45/55. No live purchase, save reset, SampleScene save or full Play traversal performed.

ZIP is a focused code/localization patch; it deliberately excludes the former Resources/RuneUpgradeData asset to avoid duplicating the user's moved asset/GUID. The existing scene-assigned RuneUpgradeData is retained.
