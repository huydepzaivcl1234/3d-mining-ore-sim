# EquipmentSystem setup

Add EquipmentSystem to Player (the same object as its Animator). Assign the project's `Assets/GameData/Player/Animations/Low.prefab` to Weapon. Assign Weapon Holder to the right-hand bone or an offset child, and Weapon Sheath to an offset child of the hips/back. Tune the separate local position/rotation fields in Inspector. Optionally assign an existing scene sword as Weapon Instance for the initial sheathed display.

As shown in the tutorial, the component starts with a sword instantiated under Weapon Sheath. DrawWeapon instantiates a sword under Weapon Holder and destroys the sheathed copy. SheathWeapon reverses this. Repeated events in the same state are ignored. Unlike the previous ZIP, it does not reparent one persistent sword.

Add an animation event `DrawWeapon` to the authored DrawSword clip at the frame where the hand takes the sword, and `SheathWeapon` to the PlayerShealth clip where it returns to the holster. `OnDrawWeapon` and `OnSheathWeapon` aliases are also provided. Events must reach the GameObject that carries EquipmentSystem; this is normally Player if Animator is on Player. The existing E input/Animator transitions remain owned by PlayerCombatInput; this component only moves the visual sword.

If using FBX-imported read-only clips, use the Animation import Events list or a copied editable clip. Avoid adding the same event on both halves of a two-part draw/sheath sequence.

The existing `sheath sword 2` clip currently calls `ShealthWeapon` (the authored spelling). EquipmentSystem accepts that name as an alias for `SheathWeapon`; the Animator trigger and the Animation Event are still separate mechanisms. The clip currently contains two identical `ShealthWeapon` events at time 0. The method ignores the second call, but remove the duplicate event in Unity's Animation import settings when convenient.
