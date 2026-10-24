# Hit feedback, head popups, and camera-frustum culling — 2026-10-22

## Designer controls

- `Assets/GameData/Resources/MonsterHitFeedbackData.asset`: enabled, pulse duration, squash amount, flash strength/color. Scales only the visual child, not the navigation motor. A brief property-block tint brightens the model without editing shared materials; the original block (including null) is restored after the pulse so batching is not permanently affected. This is a subtle tint flash, not a new bloom/light pass.
- `Assets/GameData/UI/DamagePopupData.asset`: existing size/color/arc settings plus Follow Target, Accumulate Periodic Damage, Periodic Quiet Seconds (3), and spacing between damage-type totals. Physical stays orange, magic pink-purple, true white. Font size grows from actual accumulated damage and respects the existing editable min/max.
- `Assets/GameData/Resources/GameplayPerformanceData.asset`: Cull Offscreen Actors, Cull Offscreen Animation, Actor Bounds Padding; existing render-distance settings remain intact. Disable culling to restore original renderer/canvas/Animator settings.

## Damage presentation

Physical direct hits spawn separate numbers above the collider/mesh head height and follow the victim while following the existing rise/fall/fade curve. All magic and true damage (including effects without a DOT flag), plus physical periodic damage, accumulate one held total per victim AND damage type. Every new matching hit adds actual HP removed, pulses the number, and resets the quiet timer. After 3 seconds without another hit of that type, or when the victim dies/disables, the total detaches and takes the normal arc. New hits after release start a fresh total. A bounded render-time screen-space layout reserves totals before shot numbers and separates their actual text rectangles, including growing fonts, using the editable Minimum Pixel Spacing. Budgets/pooling still apply; clearing/reset and scene teardown clear totals too.

Burn, Sharp Rounds bleed, and God bleed explicitly call DealPeriodicDamage. Bullet Fly, bombs, cannon, Nail and direct shots are not relabeled as DOT. No proc rules, resistance calculations, reward gates or save keys changed. Periodic context is restored across nested damage callbacks; nested direct on-hits do not inherit the outer tick's classification.

## Camera culling

Monster health startup and existing item/box factories attach WorldActorVisibility. Main-camera frustum planes are computed once in MiningRenderDistance after the player camera LateUpdate. Cached conservative local bounds follow root movement. Outside the frustum, renderers/world canvases are hidden, offscreen skinning is disabled and Animator uses CullUpdateTransforms. Collision, AI, movement, pickups, damage clocks and death timers remain enabled. All property changes are restored on reentry/disable; Mushnight stealth updates feed the visibility owner so reentry does not reveal an intentionally hidden mesh.

Do NOT disable Animator or use CullCompletely for current monsters: MushroomMonster reads Animator normalized time to apply and finish attacks. CullUpdateTransforms skips skeletal pose/IK while preserving that state clock. Thus this intentionally does not promise zero Animator state-machine CPU. It is frustum/distance culling, not a new occlusion system. Static trees/terrain already use normal camera frustum clipping; no static batching or navigation rewrite was introduced. Objects at a frustum edge remain visible while any padded bound intersects.

## Validation scope

Editor compilation, three focused NUnit fixture methods invoked in the live Editor, and a rendered-frame Play Mode probe cover actual pulse/tint restoration, head anchoring, independent DOT totals, accumulation/quiet timeout/recycling, visibility return, intentional invisibility, and a real monster moving while its skeleton pose is frozen outside the camera. Reports/captures are in the task workspace AgentPreviews, not game Assets. This is not an FPS benchmark, a full game regression suite or a player build. SampleScene was already dirty and is not automatically saved or included in delivery ZIPs.
