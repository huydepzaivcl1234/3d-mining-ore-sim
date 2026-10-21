# Bat hurtboxes and typed damage popups

- BatMonster's existing CharacterController remains unchanged for ground navigation. It is not a weapon hurtbox.
- Two trigger BoxColliders under Visual/root/body and its head bone enclose the baked body/head geometry. Their local centers/sizes can be edited in the prefab. They follow animation and boss scale without per-frame mesh baking.
- MonsterHitbox explicitly lists the allowed damageShapes. PistolShooting's camera and muzzle queries include these triggers, exclude Bat's motor and ignore unrelated triggers. Pistol and rifle share this shooting path.
- Damage type travels as a value in AnyDamaged; nested proc events cannot overwrite the original hit's type. Physical + true-bonus combined strikes retain their primary type, as before they remain one damage event.
- Edit DamagePopupData.asset > Settings > Damage type colors: Physical orange, Magic pink-purple, True white. Font size, spread, pooling, motion and fade are unchanged. Each popup captures its own color for its whole lifetime.

## Validation (2026-10-16)

Connected Unity Editor 6000.5.3f1 compiled successfully. Play Mode fixture tested 32 animation samples across normal and 3x scale: body rays hit, rays beneath body miss, old capsule would hit those low rays, motor settings unchanged. Ordinary colliders remain hittable and unrelated triggers are ignored. Actual physical/magic/true health events produce the three colors; fade preserves RGB; nested magic damage does not recolor the physical parent hit. Test save storage was isolated and detached after Stop. SampleScene was not saved.

This verifies geometry queries and popup event integration, not a manual mouse-aim playthrough. Wings do not have separate hurtboxes.
