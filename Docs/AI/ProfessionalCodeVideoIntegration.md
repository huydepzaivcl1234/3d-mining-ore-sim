# Professional-code video: review and combat integration

## Review method and scope

Source: the user-supplied `Can you write code like a PROFESSIONAL_.mp4`.
Duration: 3718.59 seconds (about 61:59).

The complete locally generated English transcript was read, including the final
recap and promotional sections. Visuals were inspected on 31 contact sheets,
covering the timeline at approximately ten-second intervals. This is **not**
continuous playback or inspection of every frame. Automatic transcription can
misrecognize words; visual samples were used to check the code demonstrations.
No audio was uploaded, no course purchased, and no external assets imported.

The live Unity Editor uses `D:/3d mining sim`, not the similarly named chat
workspace. Changes are deliberately limited to the current sword-combo work.
This is not a claim that every class in the project has been refactored.

## Lessons applied

| Video section | Practical application in this project |
| --- | --- |
| 01:31–04:22; 11:38–17:46: clear names, restricted access, explicit intent | Named `SwordStrike` values replace unnamed combo indices. Runtime fields remain private; existing tuning remains serialized and editable. |
| 10:09–11:37: preserving Inspector data during renames | `FormerlySerializedAs` migrates `comboLinkTime` and `comboQueueEnd` to `comboLinkStartPhase` and `comboLinkEndPhase`. Keep the attributes for assets that have not been resaved. |
| 18:29–21:59: smaller functions with one identifiable job | `StartStrikeAnimation` handles the existing Animator route; `PlayAttack` retains the existing approach/facing orchestration. |
| 32:40–38:47: typed identifiers and states instead of ambiguous strings/integers | Combo transitions operate on the enum. Conversion to the authored integer `CombatStrike` parameter happens only at the Animator boundary. |
| 44:28–49:26: plain C# for reusable domain rules | `SwordComboRules` has no MonoBehaviour, Animator, input, movement, audio, or VFX dependency. Its transition and acceptance rules can be tested independently. |
| 51:26–57:58: named constants and centralized Animator identifiers | Layer/parameter names, state hashes, and the Animator-entry timeout are named rather than repeated inline. |
| 05:49–07:11: small refactor followed by verification | Added 17 boundary/input tests, retained the existing integration tests, and ran the full EditMode suite. Actual-rig validation is recorded below. |

The video also demonstrates separating shop UI, audio, visuals, and order data
with typed events. This turn applies the separation principle to combo decisions;
it does **not** introduce an event bus or claim that all combat feedback has been
extracted. The existing damage events, sword trail, movement motor, input
rebinding, settings, and save architecture are preserved. In particular, the
video's demo removal of save code is not suitable for this game and was not copied.

## Player-requested behavior retained

- An early press is discarded; there is no remembered combo click.
- A follow-up requires a **fresh** press in the current strike's valid window
  after its contact event has been consumed. Missing the enemy still consumes
  that strike's contact event.
- The default normalized window includes 0.78 and excludes 0.97.
- Strike three has no automatic next strike or wrap to strike one.
- The authored lunge/turn states retain their mappings for compatibility; the
  current free-flow opener remains the existing first sword strike.
- Existing camera-independent facing, directional targeting, full-body sword
  animation, and collision-safe approach movement are not replaced.

## Verification (2026-10-10)

- Live Editor: no script compilation failure after refresh.
- Renamed fields on the scene player: start 0.78, end 0.97.
- `SwordComboRulesTests`: **17/17 passed**.
- Full EditMode suite: **195/200 passed**, zero skipped. The same five failures
  were present in the preceding baseline; they were not hidden or disabled:
  - `TreasureChestTests.RepairUsesWorldSpaceBillboardAndShowsMoneyCost`
  - `TreasureChestTests.SuppliedArtworkAndFillAmountsAreBound`
  - `WeaponAttackTests.FistsHitOnlyClosestForwardTarget`
  - `WeaponAttackTests.ShortEnemyInFrontIsNotRejectedByVerticalAngle`
  - `WeaponAttackTests.UpswingHitsMultipleTargetsInFrontOnlyOnceEach`
- Actual-rig Play Mode: early-spam check passed (only strike one damages the
  target; idle stays idle; a new click can restart).
- Actual-rig Play Mode: all three contact events passed while the camera orbited
  180 degrees and shift lock toggled; strike-three spam did not restart the combo;
  wall rejection and the return to locomotion passed.
- Actual-rig Play Mode: sprint held while moving backward, strafing left, and
  strafing right passed; all three contacts and full-body strike legs remained
  active. All five fixture runs restored original roots and background settings.
- Final Editor state: `SampleScene`, EditMode, no script compilation failure.

No scene YAML or Animator asset was manually rewritten. Unity reserialized the
two renamed scene fields with their unchanged 0.78/0.97 values during validation.
The isolated Play Mode fixture disables normal gameplay/save systems, uses the actual cloned player
rig/clips, then restores the original roots. It is automated validation, not a
claim of a full manual gameplay session or a performance benchmark.
