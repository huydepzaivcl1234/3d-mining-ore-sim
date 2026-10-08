# EquipmentSystem setup

Add EquipmentSystem to Player (the same object as its Animator). Assign the project's `Assets/GameData/Player/Animations/Low.prefab` to Weapon. Assign Weapon Holder to the right-hand bone or an offset child, and Weapon Sheath to an offset child of the hips/back. Tune the separate local position/rotation fields in Inspector. Optionally assign an existing scene sword as Weapon Instance for the initial sheathed display.

The component starts with one sword under Weapon Sheath. DrawWeapon moves that same sword to Weapon Holder; SheathWeapon moves it back. Repeated animation events cannot create duplicate swords. Death and respawn both reset it to the sheath. If an older setup left more than one matching sword under the two holders, startup keeps one and removes the extras.

Slash VFX reads the animated sword tip and swing direction when the sword is drawn. If this particular sword mesh needs a final rotation correction, edit `PlayerStatsData.slashBladeEulerOffset`; scenes without a drawn sword retain the original player-relative VFX offset.

Add an animation event `DrawWeapon` to the authored DrawSword clip at the frame where the hand takes the sword, and `SheathWeapon` to the PlayerShealth clip where it returns to the holster. `OnDrawWeapon` and `OnSheathWeapon` aliases are also provided. Events must reach the GameObject that carries EquipmentSystem; this is normally Player if Animator is on Player. The existing E input/Animator transitions remain owned by PlayerCombatInput; this component only moves the visual sword.

If using FBX-imported read-only clips, use the Animation import Events list or a copied editable clip. Avoid adding the same event on both halves of a two-part draw/sheath sequence.

The existing `sheath sword 2` clip currently calls `ShealthWeapon` (the authored spelling). EquipmentSystem accepts that name as an alias for `SheathWeapon`; the Animator trigger and the Animation Event are still separate mechanisms. The clip currently contains two identical `ShealthWeapon` events at time 0. The method ignores the second call, but remove the duplicate event in Unity's Animation import settings when convenient.
