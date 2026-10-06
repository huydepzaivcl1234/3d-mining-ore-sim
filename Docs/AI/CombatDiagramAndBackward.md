# Combat graph and backward gait

The existing Player controller preserves its states, clips, events, masks' identities
and original Base Layer. Its combat layer now uses one integer route, CombatStrike,
plus the existing attack trigger. The player script selects a valid route only when
its existing input buffering/contact gate allows a follow-up. This removes ambiguous
branches using the same trigger and makes the authored diagram match runtime routing.

Routes:
- Air/close opener: Combat -> Sword Attack 1 -> Sword Attack 2 -> Special Attack.
- Lunge opener: Combat -> lunge attack -> turn attack -> Sword Attack 1.
- The combo cycles back to Sword Attack 1 after Special Attack.
- Every attack has a Move return path; runtime recovery also keeps its explicit return.

Transitions use fixed duration .07 seconds and zero destination offset; each route
has a unique CombatStrike condition. No transition lacks both conditions and exit
time. States are arranged in a compact, named diagram. Existing draw/sheath,
Base Walk/Run, directional gait assets and attack contact events are unchanged.
Controllers without CombatStrike keep the old direct crossfade fallback.

The combat upper-body mask no longer contributes LeftFootIK/RightFootIK. The lower
layer owns backward foot placement using the actual Human backward walk/run clips,
not reversed forward animation. Original forward motion remains on the Base Layer.

Validation used an isolated clone, not the player's gameplay/save scene:
- Eight armed/unarmed, locked/unlocked forward Walk/Run cases preserved.
- Backward walk and sprint used total backward clip weight ~1; sampled knee angles
  bent to about 94 and 77 degrees respectively (not permanently straight).
- Nine graph entries followed the expected normal and lunge sequences.
- Disable reset the lower-layer weight and movement multiplier.
- Clone teardown emitted a SheathWeapon receiver warning because presentation
  components were deliberately stripped; it is not a source-player receiver removal.
- Temporary probe initially required correcting its test assertion to sum the
  blended walk/run backward weights rather than expect one clip at weight 1.

SampleScene was not edited or saved. Existing gameplay damage/VFX events were
preserved; these tests did not constitute a full combat/economy test suite.
