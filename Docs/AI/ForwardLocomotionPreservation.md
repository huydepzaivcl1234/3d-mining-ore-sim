# Preserve original forward locomotion

Only PlayerCombatInput.Locomotion.cs changes at runtime. No scene, controller, mask, animation clip, attack, save or economy changes.

- Forward input in shift lock releases the directional footwork override. Original Base Layer Walk/Run and existing arm setup remain in control.
- Forward intent comes from input rather than a temporarily diagonal body-relative vector during camera smoothing.
- Locked stationary turns follow actual body rotation and stop when rotation settles. Stop-speed threshold remains editable.
- Side/back movement and unlocked reversal behavior remain unchanged.

Unity MCP Play validation on an isolated clone of the current player: eight forward combinations (armed/unarmed, locked/unlocked, walk/run) retained zero footwork override; both locked turn directions activated and stopped; side-to-forward restored Run; disabling combat reset the movement modifier. All assertions passed.

The isolated clone produced a SheathWeapon receiver warning because presentation components were stripped for testing; it is not evidence of a source-scene regression. Temporary diagnostics were removed after testing, and the play-mode scene override was cleared. SampleScene SHA256 remained 44D8B49100B91F6FFC5CE469B2FC6CD65525DA37ADA5E02B78552EE913272639. Editor returned to stopped state.
