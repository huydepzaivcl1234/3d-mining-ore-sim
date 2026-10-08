# Inventory interactions and death cursor

- Death releases the cursor after camera drivers disable (Orbit restores its previous cursor in OnDisable). StarterAssetsInputs cursor locking is suspended while dead; LateUpdate keeps the spectator cursor visible. Respawn leaves the cursor visible because shift lock was cleared by death.
- Left click keeps the existing use-one / gift-box wheel behavior.
- Right click opens Use 5 / Use 10 / Use all for timed-effect items. Quantity is clamped to the stack; durations extend using the existing effect rules. Gift boxes retain their existing wheel and bulk options are disabled rather than bypassing rewards.
- Left drag moves whole stacks to empty inventory slots or swaps occupied slots. Save format remains version 1. Equipment placeholders do not accept drops; the drag visual returns to its source. Dropping outside also returns it without changing data.
- MiningInventoryInteractions is attached/prepared by MiningInventoryPanel. Menu dimensions, font, colors, rejected-drop duration and source opacity are editable on the inventory panel's component. Existing slots/art/model preview are retained.
- Scene menu hierarchy was prepared in the open SampleScene, without saving the user's other scene changes. Press Ctrl+S to retain this editor-authored hierarchy. Runtime preparation also supports scenes without this hierarchy. SampleScene is excluded from delivery ZIP per project rules.

## Unity MCP validation

Unity 6000.5.3f1, live project D:/3d mining sim:

- Editor compilation: no errors.
- Play test: cursor unlocked/visible across death frames, focus regain and respawn from shift lock.
- Isolated inventory save key: 25 items -> use 5 leaves 20 -> move -> use 10 leaves 10 -> use all empties the slot; effect duration totals 25 times the item duration; v1 save generated.
- Menu button/drag presentation test: 15 -> click Use 5 -> 10; move to slot 2 preserves 10, source empty, source alpha restored to 1; out-of-range equipment destination rejected.
- Console after final tests: no errors. Existing missing-script warning is unrelated to this change.
- Native mouse-driven manual playtest was not performed; input handlers and UI actions were exercised through MCP in Play Mode.
