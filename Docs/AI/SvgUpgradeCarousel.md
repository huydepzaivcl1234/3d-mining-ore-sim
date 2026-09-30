# SVG world-space upgrade cards

The user's UpgradeCard SVG is imported as UI SVG (vector geometry, not a PNG). Its imported VectorGradientUI material and Canvas UV1/UV2 channels preserve the gradients. The existing station and real upgrade buttons remain; the old leather panel decorations are hidden at runtime. No upgrade price, save-data or economy rules were changed.

## Authoring

- Select your station (currently GoldenSquareSlab). Reorder **Upgrade Order**: entries at the beginning appear first. Unlisted card types follow their authored hierarchy order. Change Panel Width Metres/Panel Offset to suit the camera and station; the default width is 7.2 m for readable text.
- To add a card for an existing upgrade, add a Button under the original **Upgrade Panel**, add **Juicy Upgrade Item**, choose **Upgrade Type** and optional **Card Icon**. Do not add a purchase onClick listener: the card binds to the existing MiningUpgradeSystem automatically. Existing icon children are reused when Card Icon is empty. Add before entering Play Mode; runtime generation occurs once per scene.
- New gameplay upgrade types/definitions still need to exist in MiningUpgradeData/System; adding a UI button cannot invent a new economy rule.
- Source vectors: Assets/Resources/UpgradeCard.svg and UpgradeScrollKnob.svg. Keep their importer on UI SVG. Existing com.unity.vectorgraphics package is used; no package was added.

## Interaction

Near the station, click its prompt to open. Wheel down/up moves one card; the vertical scrollbar jumps to a card. The first card is full size/opaque with a smooth hover scale, the next two are smaller and dimmer. No fourth card is visible during animation. At the last card only one remains. Movement and free camera remain available; scrolling over the UI is already excluded from camera zoom by MiningOrbitCamera.

Each card subscribes to the original upgrade/wallet/localization events and calls TryPurchase once. The original UpgradePurchased event remains responsible for successful-purchase SFX. Old generic-click/leather/punch decorators are disabled to prevent duplicate SFX and competing animations. Close uses the shared button SFX. Close, Escape or walking away retains the station's smooth fade/shrink/lower animation.

## Validation

Unity imported and compiled. Play Mode: ten original cards, wheel advanced 0 -> 1, scrollbar reached index 9 with one visible row, hover event reached the first card, five close/open cycles retained ten cards/eleven SVG graphics including the scrollbar knob. Purchase listener count was one on tested cards; an unaffordable click did not change money. No successful purchase or reset was made. Screenshots verified the SVG gradients after enabling the Canvas UV channels.

An existing missing-script warning remains outside this feature. The screenshot utility also emitted an internal recursive PlayerLoop warning during capture; no vendor package was modified. SampleScene.unity is excluded from delivery per project rules. The existing panel was reconnected in the live scene without saving it; runtime recovery also finds the authored card parent if that binding is cleared.
