# Forest Golem

Shared `MushroomMonster` AI owns movement, contact timing, level scaling and loot.
The composed `ForestGolemAbility` owns the travelling wave, successful-hit counter
and charged curse. Existing species do not enable this ability.

Authoring: `Assets/GameData/Monsters/ForestGolemRewards.asset` → Combat → Forest Golem.
Editable defaults: extra radius 1.2 m, wave duration 0.65 s, three successful player
hits per charge, ChargeUp completion at normalized time 1, burn 5 magic damage per
1 s for 5 s before species level scaling/MR. Another three hits can charge again.
Failed/zero-damage hits and hits on miners do not count. The slam and its travelling
front can damage each victim only once. The front can hit miners as well.

Hammer impact is phase 0.50, Hit impact is phase 0.54, measured on the imported
humanoid hand poses. Warning offsets follow the corresponding hand contact.
The front expands from that center to the warning radius + extra radius; boss
size multiplies both radii. A wall blocks wave damage; ore is ignored as in normal
monster strikes. Jump height respects the species hit-height setting.

Charge waits for the wave to finish, pauses attacks/movement, plays the imported
ChargeUp clip and burns the living player who triggered it without a distance
check. Death, despawn and disable cancel pending casts and hide the wave. Rune
time pauses the ability through the shared AI tick. Existing health/DOT ownership
handles burn damage, MR, refresh and clearing on player death/respawn.

The existing ForestGolem prefab keeps its model/materials and gains Animator,
CharacterController, health with the existing MicroBar style, shared AI and ability.
Independent Idle/Walk loop clips avoid changing FBX import settings. The Resources
spawn roster adds ForestGolem with editable weight 25 and minimum player level 1.
Forest Golem boss uses the same ability, not the original golem's slow passive.
SampleScene and player saves were not edited.

Validation: connected Unity 6000.5.3f1 compiled the feature; an isolated Play scene
passed 22 checks covering contact timing, wave travel/boundary/deduplication,
zero-damage counting, ChargeUp, remote burn, repeat charge, scaled radius,
dead-player rejection and death/disable cancellation. World-space warning/wave
were captured and visually inspected. Asset-binding/roster/regression tests are
in `ForestGolemTests.cs`; 12 asset/existing-combat checks passed. The unmodified
AI Update loop also completed repeated hits and applied burn in the isolated scene.
Production-world terrain/navigation remains shared
with the existing monsters and was not exhaustively tested across the map.
