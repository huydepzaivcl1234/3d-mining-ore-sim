# Blue Box upgrades — 2026-10-16

Added Faster Matter and Bullet Fly to the existing permanent gun-special upgrade pool. The Blue Box still offers at most three distinct, randomly selected, unowned upgrades. Fewer available upgrades show only the remaining choices. Red cards retain their original three actions. Blue Box labels now use semantic Lean Localization keys in English/Vietnamese.

Faster Matter increases firing frequency by 20% by dividing the gun's shot interval by 1.2. It applies to the existing pistol/rifle and future guns using PistolShooting. The existing minimum shot interval remains a numerical floor.

Bullet Fly observes actual MiningCharacterHealth damage events on ordinary monsters and God Boss. Any external source/type, including DOT, can trigger it while the victim survives. It displays five cyan rounds above the victim and health bar, then launches them sequentially at that same moving target. They deal 1, 2, 3, 4 and 5 true damage on arrival. A lethal or despawned target cancels remaining rounds. Pausing freezes both flight and damage. Damage from this effect, including synchronous nested procs caused by it, cannot create another Bullet Fly volley.

The effect reuses line-renderer visuals and volley objects. The visible-round budget limits rendering only; active volleys continue delivering their damage. Bullets are stylized short glowing rounds, not a new imported ammunition model. All timings, spacing, color, width, visual budget, damage sequence and fire-rate bonus are editable in `Assets/GameData/Player/Weapons/BlueGunUpgradeData.asset`.

Existing `Player.GunSpecialUpgrades.v1` saves remain compatible: the three old flags are preserved and the two new flags default false. Save schema/version/key are unchanged. Each upgrade is unlocked once, matching existing blue upgrades.

PlayerWeaponProgression refills both equipped and stowed loadouts when the clock enters Day (end of night). It restores the current magazine capacity, including card bonuses, and the weapon data's configured reserve ammo. Reload in progress is canceled/reset as part of the refill. Entering Night does not refill.

Validation used a separate BlueUpgradeFixture.json store and runtime-only changes. Checked old/new save loading, both new choices, the last remaining choice, pause/resume, exact 1.2 firing-rate ratio, no night refill, pistol refill and stowed rifle refill. Runtime damage sequence was 10 external followed by 1,2,3,4,5 despite high defenses; exactly one volley resulted. Other-source and burn/DOT triggers, paused flight and destroyed-target cleanup passed. Captured and inspected the live choice panel and five-round effect. Editor compilation passed.
