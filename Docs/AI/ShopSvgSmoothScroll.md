# Shop SVG and smooth scrolling

- Existing product cards and buy buttons are retained, including all text, icons, RectTransforms, references, localization and purchase logic. The wheel and main shop panel are not restyled.
- `Group 2.svg` is the product card background, `base.svg` is the Buy background, and `Rectangle 13.svg` is a non-interactive frame behind each existing item icon.
- Original SVG source files are retained unchanged in `Assets/GameData/Shop/UI`. `ShopProductCard.svg` and `ShopBuyButton.svg` are compatibility copies that replace single white shape masks with equivalent clip paths. Unity imports these as high-resolution Textured Sprite for the existing uGUI Image components. No external PNG replacement or package is required.
- ShopSmoothScrollRect interpolates wheel input using unscaled time, accumulates repeated wheel steps, clamps the destination, and cancels interpolation for dragging or external position changes. Drag, scrollbars, callbacks and authored layouts remain supported.
- On Product Scroll View, adjust Wheel Step (64 UI units) and Wheel Smooth Time (0.12 seconds) in the Inspector.
- Applying `Mining Simulator/UI/Apply Shop SVG and Smooth Scroll` is opt-in and repeatable. It does not rebuild the hierarchy or save the scene automatically. Save with Ctrl+S after inspection. The ZIP deliberately excludes SampleScene.unity.
- Runtime cosmetic rows clone the existing card and inherit its new styling automatically.

## Validation

- Unity Editor compilation: no errors.
- All 78 original shop RectTransforms preserved exactly; four original product bindings remain intact.
- Play Mode: wheel input does not jump immediately, reaches the expected scroll position, stays within bounds under repeated input, and cancels smoothly when dragging.
- Close/reopen: one scroll component, five runtime rows, no duplicate cosmetic rows or icon frames.
- SVG card gradients and teal border, Buy label and existing item icons checked using a rendered preview.
- Existing missing-script and deprecated-API warnings are unrelated to this change. No purchase was made and no player currency/save data was modified by validation.
