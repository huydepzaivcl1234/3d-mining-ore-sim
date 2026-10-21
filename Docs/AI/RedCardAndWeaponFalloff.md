# Red Card common and weapon falloff — 2026-10-14

## Behavior

Only the existing RedCard/Common tier drops for now. Existing monster/boss drop probabilities and model assets remain; green/gold models were not deleted. Their selection weights are disabled. The old five rewards (melee damage, attack speed, max health, regen interval, healing effectiveness) no longer grant or load effects.

On pickup, the existing uGUI choice panel presents exactly three cards: gun damage +1%, magazine capacity +1 round, bullet speed +1%. One selection consumes the pending reward once. Gameplay time is frozen, normal listener audio paused and the player controller disabled until selection; UI transitions use the existing unscaled coordinator. Cursor/controller/prior time scale/audio pause are restored on selection or system disable. A dead player cannot collect cards. Missing old player reference now resolves the current MiningPlayerStats owner at runtime.

Rewards are additive player-owned bonuses, not mutations of weapon assets. The +1-round choice increases capacity; it does not create a free live round. Reload fills the new capacity from reserve. Bonuses apply to current and future weapons using this PistolWeaponData/PistolShooting path. Other future weapon controllers must consume the same GunDamageMultiplier, GunCardMagazineBonus and GunBulletSpeedMultiplier API.

Player progression save is now version 5. Versions 1–4 retain level/XP but retired card effects are ignored; missing new bonus fields default to zero. Legacy numeric choice IDs are reserved and rejected rather than reassigned. New bonus values save with player progression. No profile reset or unrelated-data deletion was performed.

The current pistol retains its authored base damage, magazine size, reserve and cooldown. Range is set to 500 m, full damage through 300 m, then linear falloff to zero at 500 m. Damage fraction examples: 200/300 m = 1; 400 m = .5; 499 m = .005; 500+ m = 0. Both regular and consumable true-bonus damage respect falloff. Camera aim and muzzle obstruction ray are bounded; visual tracer endpoints do not extend beyond weapon range. Tracer remains visual hitscan feedback, not delayed ballistic damage.

## Editable assets

- Assets/GameData/Cards/CardDrops.asset: Red reward amounts and existing drop/pickup presentation settings.
- Assets/GameData/Player/Weapons/PistolWeaponData.asset: Range and Damage Falloff Start, plus original weapon parameters.

## Validation

Live helper tests outside Assets, not an NUnit suite:

- Real world-card landing/pickup: player moved into pickup radius after random physical scatter. Exactly 3 visible buttons; time/player position frozen; controller disabled during choice.
- Three button paths grant +1% damage, +1 capacity and +1% speed separately. Repeated selection after consuming a card rejected.
- Isolated GameSave test store: v4 synthetic legacy bonuses ignored while level 4/XP 20 preserved; v5 save/load roundtrip retains all three new values. Actual profile bonus values restored after testing.
- Swapping a temporary different weapon data instance: 20-round capacity becomes 21, 100 m/s visual speed becomes 101. Original weapon asset untouched by grants.
- Actual shooting at isolated temporary colliders: 550 m collider not hit, 400 m collider hit; largest tracer travel below 500 m. Formula boundaries verified separately.
- Choosing resumes time/controller; disabling the card system while paused restores prior .5 time scale and prior controller state.
- Final UI screenshot inspected: AgentPreviews/RedCardChoices.png in the calling workspace. Missing translation keys use readable English fallback rather than raw key strings.
- Temporary test HP changes, player relocation, ammo and masks were restored. Unity returned to Edit Mode. Scene hash unchanged: 71F231284C28DBF720A50E7149D71055C0BB7EE6A522DAC156D6DF33C0DD889B. Saved hand pose unchanged.

Earlier probe failures were not counted as passes: HP zero correctly blocked collection; a scattered card outside pickup radius required approaching it. A temporary compile error from a shadowed local variable was fixed and compilation rerun.

## Limits

No player build, full language translation table, direct far-range enemy-health capture, controller navigation matrix or persistence of an unfinished choice across quitting was tested/added. Time-scale pause freezes scaled gameplay/physics/normal Animators, not arbitrary third-party unscaled scripts; AudioSources explicitly configured to ignore listener pause can still play (for example UI sounds). World-card model/drop physics are preserved.
