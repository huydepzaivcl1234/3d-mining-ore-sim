# Extend the original Walk/Run gait

## Evidence and fix

Original Base Layer uses a Speed blend tree: Idle at 0, Walk at 2, Run at 6, all authored rates 1. Directional movement instead used a sprint-button blend and FootworkRate = motor speed / independent cycle speed. Current authored motor speeds made those multipliers 1.75 walking and approximately 1.31 running, producing a faster directional stride than the original.

Directional Movement now copies the original Speed parameter, Idle/Walk/Run thresholds and rates. It keeps the existing eight-direction Human movement clips but no longer owns a separate sprint clock or stride multiplier. Side/back movement shares the same acceleration/deceleration and slow-driven blend as forward movement. Original Base Layer, Arms Layer, attack events, clip assets, masks and movement speeds are unchanged.

The controller removes 14 redundant single-direction Human states added by the previous implementation; their clips remain used by the directional blend trees. Existing authored strafe states remain for compatibility. Combat Footwork states are arranged in compact rows; only added movement-state positions change in the upper combat graph. Script-driven attack/turn routing remains intact. Unused FootworkGait/FootworkRate parameters and the two independent cadence fields are removed.

## Verification

Unity MCP Play test on an isolated clone of the current player:
- 8 armed/unarmed, locked/unlocked, forward Walk/Run cases: original Base Layer active; footwork override zero.
- 12 side/back cases: left/right/back x Walk/Run x normal/30-percent slow. Measured normalized cycle advance matched original Base Layer within 0.025; observed values matched to three decimals. Walk/Run clip weights also matched within 0.025.
- Both locked stationary turn directions activated and stopped.
- Side-to-forward restored original Run; disabling combat reset movement modifier.
- Asset checks confirmed shared Speed, thresholds 0/2/6, rates 1/1/1 and no directional child rate multiplier.

The temporary probe initially failed compilation by assigning a read-only slow property; corrected to ApplyMovementSlow, then completed all assertions. The isolated clone emitted a SheathWeapon receiver warning because presentation components were stripped. Temporary probe assets are not part of the patch and are removed after validation. Final source compilation checked separately.

SampleScene is not saved or changed; original hash is 44D8B49100B91F6FFC5CE469B2FC6CD65525DA37ADA5E02B78552EE913272639. No economy/progression/save tests were run because this patch does not touch those systems.
