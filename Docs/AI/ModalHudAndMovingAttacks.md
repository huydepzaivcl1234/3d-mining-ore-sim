# Modal HUD visibility and moving attacks

## HUD fix

The coordinator previously faded only serialized, fixed HUD references. New stamina/stat UI was outside that list. It now also captures active graphic-bearing roots under the gameplay HUD Canvas, excluding modal/menu/transition views. Their CanvasGroup alpha and input flags are restored to the original state after closing. Inactive HUD is not activated. Nested groups that ignore parent groups are included. Interrupted close/open transitions retain snapshots until restoration completes.

Trader and portal views register modal ownership while retaining their own animations. MainMenu registers HUD suppression separately, preserving the existing Shop-from-menu return flow and pause owner. A stale panel close cannot reveal HUD behind a different active panel. No scene/prefab layout or saved progression is changed.

The previous fixed HUD references retain their existing smooth fades. New HUD content should live under the same HUD Canvas. New custom modals must use OpenPanel/ClosePanel or NotifyExternalPanelOpened/Closed.

World-space player, monster and ore health-bar renderers are also temporarily suppressed and restored without disabling their health components or changing their authored Renderer.enabled values. While a modal is open, newly spawned world bars are captured every .25 seconds.

Validation: runtime/StarterAssets/Editor source compilation; regression tests added for stamina/new HUD restoration and modal/inactive-view exclusions. Unity Play Mode/Test Runner execution is unavailable. Verify opening/closing Upgrade, Stats, Shop, Inventory, Quest, Settings, Trader and Portal, rapidly switching panels, and opening Shop from MainMenu then closing it. Inspect stamina, Stats button, Docker/background, currencies and progress UI. No desktop UI was controlled.

## Moving attack configuration (inspection only)

`Player Combat Respawn Hit Reaction.controller` contains a Combat Upper Body mask with Humanoid Root enabled and every transform under Skeleton/Hips and both leg chains enabled. PlayerCombatInput already keeps locomotion running and raises the combat layer weight only for an attack or stationary combat idle. The mask configuration can admit unwanted root/lower-body animation contributions; this is a supported hypothesis, not a Play Mode confirmed diagnosis.

Keep Idle/Walk/Run on Base Layer. Combat attack uses an Override upper-body layer. In its mask turn OFF Humanoid Root, both legs and foot IK; exclude the root transform, Skeleton/Hips itself, and all leg/foot/toe transforms. Keep Spine/Chest/shoulders/arms/hands as appropriate. Apply Root Motion should remain disabled for character-controller movement. Do not set the whole combat layer weight to zero during attack: that removes the attack along with unwanted lower-body contributions.

A mask selects which bones contribute; it does not rewrite the arm trajectory or eliminate all torso rotation authored into a stationary boxing clip. For a forward punch while running, use an in-place moving/forward-punch clip on the upper-body layer, or a separate moving Attack state chosen from locomotion Speed. If an upper-body clip still points sideways, inspect retargeted Avatar orientation and the clip's chest/shoulder motion rather than locking the legs. No animation controller or source clip is changed in this patch.

Reference: https://docs.unity3d.com/6000.0/Documentation/Manual/class-AvatarMask.html
