# Gun-only reaction, visible flash and non-overlapping totals — 2026-10-22

## Changes

- Monster skeletal Damage animation only starts from a basic gun hit: basicAttack context plus the actual PistolShooting source component (used by pistol/rifle). Nested effects restore the outer context correctly. Effect damage still deals damage and runs its proc rules, but does not start/restart hit animations. An active reaction is not restarted by rapid shots. Existing attack commitment, charge/jump protection, theft behavior and death clearing remain intact.
- Every Magic and True hit aggregates by victim and type, regardless of whether its source uses DealPeriodicDamage. Physical DOT also aggregates; direct physical shots remain individual numbers. Actual resolved damage is summed. New hits refresh the existing three-second timer; quiet expiry releases the held number into the existing pop/arc/fade.
- Popup bounds are separated in screen space just before rendering. Held totals reserve their lanes before transient shot numbers. Minimum Pixel Spacing is editable in DamagePopupData, and font size limits are unchanged. This is a bounded layout over the existing visible-popup budget, not an unbounded pool or new UI manager. Text can be displaced upward under dense overlap; it does not guarantee every number stays inside the viewport under arbitrary density.
- White-base textured monsters could not brighten by lerping white to white. Hit feedback now adds emission to each material slot using an indexed MaterialPropertyBlock, restores the original block afterward and leaves shared material values unchanged at runtime. Flash timing is not reset at zero by rapid ticks. The shader keyword/GI emission variant is prepared in the material assets; idle emission colors/textures are unchanged. No per-hit material instances, lights or extra mesh renderers are created.
- Twenty FBX-embedded materials for ForestGolem, Mushranon and Skullclaw were copied to independent .mat files under each species GameData/Materials folder, remapped through their ModelImporter and assigned to those three prefabs. Original mesh/animation/texture assets remain; model metadata contains the material remap. The exported delivery needs those .meta files and prefabs as well as the .mat assets.

## Designer controls

- `Assets/GameData/Resources/MonsterHitFeedbackData.asset`: Flash Strength (.55), Emission Intensity (.8), Flash Color, Duration and Squash. Emission is additive for a short visible flash, not a mandatory bloom pass.
- `Assets/GameData/UI/DamagePopupData.asset`: Accumulate Magic And True Damage, Accumulate Periodic Damage, Periodic Quiet Seconds (3), Minimum Pixel Spacing (8 pixels), type colors and min/max font size.
- Species Animator `Hit Reaction` layer still owns the masked Damage clip; the shared MushroomMonster code decides whether the event was an actual gun hit and avoids restarting an active reaction.

## Evidence

Initial actual-render probe reproduced no brightness increase: URP validation removed keyword-only emission because its emissive GI bits were absent. The fix preserves the appropriate baked-emissive bit and black idle emission while the property block supplies the transient color. Across eight prefabs every material-slot flash and restoration check passed after a fresh Play entry. Actual 512x512 Mushroom renders had mean brightness .13016 before vs .16733 during the flash, with 59,998 pixels visibly brighter.

63 focused Play Mode checks passed: all material slots, rendered brightness, magic/true effect and DOT rejection by skeletal reaction, actual gun hit acceptance, no rapid-shot restart, base Walk preservation, magic/true accumulation without DOT labels, font-growth overlap separation, repeated-render stability, quiet expiry, cleanup. A separate real-time Play probe refreshed totals (5 magic / 9 true), confirmed both held at 2.85 seconds after the last hit and released by 3.19 seconds. Four Editor NUnit fixture methods were invoked directly and passed; this was not a full Test Runner suite or player build. Temporary cameras, targets and popups were removed; the authored scene was not saved.

Backups: task workspace `Backups/GunHitFlashTotals-20261022/`. Delivery ZIP excludes SampleScene and unrelated project changes. This work does not claim an FPS improvement or a full gameplay regression pass.
