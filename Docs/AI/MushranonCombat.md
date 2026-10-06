# Mushranon, relative levels and falling Lucky Blocks

## Configuration

- Existing and new MonsterRewardData assets now use player-relative levels: lower offsets 1–4, upper offsets 1–5. Level never drops below 1. The existing daily higher-level probability and its cap are unchanged; these ranges remain Inspector-editable.
- `Assets/Prefabs/Monsters/MushranonMonster.prefab` reuses the shared monster AI, health bar, navigation, death rewards and boss rules. Its new loadout switches between `Fire` and `Headbutt` based on the existing melee reach.
- `MonsterRangedAttack`: muzzle reference, projectile prefab, range and animation release fraction. Default ranged reach is 8m; default detection is 10m. A blocked line of sight requests pursuit rather than repeatedly firing into a wall. Ore is ignored by sight/projectiles, matching monster ore traversal.
- `MushranonProjectile.prefab`: speed, turning rate, hit radius, lifetime and collision layers. A projectile tracks its selected living player/miner, hits once, stops at solid obstructions and expires if its target/owner is gone or owner despawns.
- `MushranonRewards.asset`: independent editable stats, defenses, rewards, drops and boss settings. Initially copied from MushroomRewards so the existing economy remains intact.
- `Assets/Resources/MonsterSpawnRoster.asset`: Mushranon entry weight 25, unlock level 1, colored preview icon. Weight is relative to the combined roster, not an absolute 25% chance. Change these values here; no scene setup is needed.
- Animation copies and controller belong only to Mushranon. Walk/Idle loop; attack/reaction/death do not. The source FBX was not rewritten. Imported cameras/lights are excluded from the combat prefab.
- LuckyBlock `Fatal Player Crush` is enabled by default. Falling sweeps/contact kill through the existing true-damage/death path, ignore actor colliders and do not mark the block landed. Only upward supporting contact freezes the body. Collision-pair overrides are restored on pool disable/reinitialization; pre-existing ignored pairs are preserved.

## Validation in the connected Unity Editor

- Script compilation passed; final Console error query was empty.
- Sampled 120,012 spawn results across all four reward assets at day 1/15/10000: player level 10 always yielded -4..-1 or +1..+5.
- Isolated temporary objects verified moving-player homing, one-hit damage, moving-miner homing/damage, near/far attack selection, wall interception and line-of-sight gating.
- Isolated player verified fatal block contact, pre-solver falling sweep, continued dynamic falling and collision-pair restoration. Invoked the actual simulation/contact methods in Edit Mode; no purchased NPC or player save was used.
- Full live Play Mode animation/physics soak was not performed. SampleScene was neither edited nor saved; its on-disk SHA256 stayed `03001E88ABA4D7E07B250E2BEE1C47F597187F1A356862CCCB8DC69009A157F1`.
- ZIP contains root-relative changed files and Unity-generated metadata, not SampleScene. It depends on the already-present `Assets/Prefabs/Monsters/Mushranon.fbx` and existing Mushroom assets.
