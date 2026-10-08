# Necklace socket regression fix

## Confirmed causes

- The authored first necklace frame had Image.raycastTarget disabled. Binding a Button did not restore this, so the EventSystem could not target that socket for clicks or dragging.
- The serialized placeholder Sprite was Unity-null but not CLR-null. The null-coalescing assignment skipped Resources.Load in the authored scene; the earlier synthetic test did not reproduce this serialization condition.

## Changes

MiningInventoryPanel now uses Unity-aware null comparison to load the placeholder and enables raycasts only on frames bound as necklace sockets. Decorative armor frames, item icon raycasts, inventory ownership, save format, and scene layout remain unchanged.

## Verified through Unity MCP

- Compiled without Console errors.
- Opened the actual inventory in Play Mode and captured TempCaptures/necklace-hints-drag-fixed.png: both empty necklace sockets show the supplied image at 25% opacity.
- EventSystem raycasts at all three socket centers resolve to the correct socket frames.
- Dispatched begin-drag/drop/end-drag through the actual UI bindings using an isolated item-system owner and unique test save key. Socket-to-bag and bag-to-empty-socket succeeded; the real equipped item remained unchanged. Restored the real owner and removed the test key.
- Returned the Editor to its initial stopped state. SampleScene was not saved or edited.
