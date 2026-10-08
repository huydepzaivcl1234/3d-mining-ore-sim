# Combat animation regression fix - 2026-10-06

## Confirmed defects

1. All locked movement selected Strafe, including forward movement. Forward now leaves the existing Walk/Run blend trees in control. Sideways selects front Strafe; backwards selects back Strafe. Sprint uses the matching run versions. Direction thresholds are exposed on PlayerCombatInput.
2. Imported clips baked horizontal root translation into the pose while CharacterController also moved the player. Play-mode sampling measured hips over 3.2 m from the controller during Lunge. The ten new authored clips now extract XZ root translation; applyRootMotion remains disabled. Their event-free lower-body copies were synchronized without replacing GUIDs.
3. The fixed-duration click buffer could expire before a long opener reached its combo-link frame. Its lifetime now covers the remaining time to the link frame, while preserving the authored minimum buffer.

## Runtime verification

- Reproduced the defects in a temporary Play scene using the user's player hierarchy, Animator controller, movement settings and combat settings. No economy, progression or world systems were instantiated.
- Tested all four locked movement directions both walking and running. Forward left full-body override at zero; sideways/backwards selected the appropriate Strafe state; body yaw stayed aligned forward.
- Forward walking used the original Walk/Run blend; forward sprint used HumanM@Run01_Forward and combat run with weight 1.
- After correcting root import settings, Lunge hips offset stayed below 0.24 m in sampled frames instead of exceeding 3 m. Normal contact damage still occurred once.
- A virtual mouse clicked through Lunge -> Turn -> Combat 1 -> Combat 2 -> Special -> Combat 1 in actual Play frames. The virtual input device was removed afterwards.
- Final successful Play run had no Console errors or warnings. Earlier harness input exceptions were corrected before the successful run.
- SampleScene was not saved, player data was not reset, and temporary Play assets were removed. Test helper sources remain only in the staging workspace, not the ZIP.

Full open-world encounters and every possible wall/slope arrangement are not claimed as tested.
