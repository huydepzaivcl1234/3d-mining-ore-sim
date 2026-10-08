# Inventory: three necklaces and isolated portrait lighting

- Three equipment addresses follow the 32 bag slots. Left-click a necklace fills the first empty socket; if all are occupied the item stays in the bag. Drag to swap a chosen socket. Click an equipped item to remove it; a full bag never deletes equipment.
- `MiningInventoryPanel` preserves the old `necklaceSocket` reference and binds placeholders 02/03 as additional sockets. Optional `additionalNecklaceSockets` overrides preserve custom layouts; remaining armor positions are reserved.
- Inventory save v3 stores all three necklace IDs. v1 bag-only saves and v2 single-necklace saves migrate without resetting items or currencies.
- Lifesteal, healing and regeneration bonuses combine. Burn remains one status, using the strongest equipped burn source; the existing red/green/orange bonuses work together.
- Portrait lighting uses a bounded point light at the existing distant render-only stage, not a global directional light. Intensity/range are editable. Camera target is detached before releasing its RenderTexture; light and stage are disabled immediately on close.
- The reference `Group 1.svg` originally contained embedded bitmap pattern fills unsupported by the importer. Its imported sprite was 0x0 with a NaN pivot; uGUI divided by zero when building its quad. The replacement preserves the original six rectangles and viewBox using supported native vector fills/outlines. Original asset and GUID are backed up; SampleScene is not modified.

## Validation (Unity MCP, 2026-10-04)

- Unity 6000.5.3f1 imported/compiled the changes without errors. Existing deprecated-search API warnings remain.
- Eight isolated equipment tests passed, including all three simultaneous bonuses, fourth-item rejection, gear swaps, full bag, purchases, and v1/v2/v3 load compatibility. Only unique test save keys were written.
- Isolated UI binding probe confirmed placeholders 01/02/03 map to 32/33/34; 04/05 remain reserved.
- Re-imported sprite is 256x234 with a valid texture; existing scene reference is intact, and generated Image quad has four finite vertices. All scene Image mesh probes were finite after the SVG repair.
- Portrait rendered using the real authored character references in a temporary additive scene. Point light range was 12 at approximately y=-9998. Cleanup cleared output texture and destroyed camera/light without RenderTexture warnings.
- SampleScene SHA256 stayed 17779B00D48236E4E1336EFC3837B4170CAA88B508B2B098D43E278D68F6433D.
- Full gameplay Play Mode was not entered; validation used isolated Editor fixtures to avoid changing active gameplay saves or the dirty scene.
