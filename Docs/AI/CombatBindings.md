# Combat input

E toggles DrawWeapon (Bool, or Trigger if authored that way); CombatMode Bool is also supported for the existing fist controller. Left mouse triggers Attack when drawn. F locks the nearest living MushroomMonster within Aim Range; F again releases. Death, modal UI, disable, destroyed target and out-of-range release the lock. Movement remains camera-relative while facing the locked monster.

Input actions remain editable on PlayerCombatInput. Existing scene Attack binding is already left mouse. No scene/controller rewrite or automatic weapon setup was introduced.

Current authored Assets/GameData/Player/Animations/Player controller.controller has only Blend and a locomotion state. Add DrawWeapon and Attack parameters and your draw/sheath/attack transitions in that controller to animate the commands. The input script cannot supply missing animation states. Existing combat layer/Attack state still supplies normalized-time hit detection when using the original fist controller.

Validation: static references and diff check. Added input and nearest-living-monster tests; test execution and interactive validation not performed. No desktop input was requested this turn.
