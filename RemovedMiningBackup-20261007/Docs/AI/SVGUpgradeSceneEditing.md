# Editable SVG upgrade station

Run `Mining Simulator > Setup > Make SVG Upgrades Scene Editable` outside Play Mode.
This opt-in command moves the existing panel into the station and authors its SVG children;
it does not replace the station model, prices, saves, or upgrade definitions.
Save the Scene yourself after checking the result. The delivery ZIP excludes SampleScene.

Edit cards at `GoldenSquareSlab / Upgrade World Canvas / Upgrade Panel /
SVG Upgrade Scroll / Viewport / Content`. Each card exposes SVG Frame, Title, Detail,
Price, Coin and Icon children. Their sizes, font sizes and offsets survive entering Play.
The station's Upgrade Order controls the initial conversion order.
For an already authored view, reorder the card siblings in Content to change the order.
New cards can be duplicated from an existing authored card; select its JuicyUpgradeItem
Upgrade Type and change its SVG Icon sprite. Give every type only one card.
The setup command does not rebuild already authored graphics.

The outer SVG border uses filled rounded layers equivalent to the Figma stroke:
7 px black-to-teal outer edge, 2 px white inner edge. It remains vector artwork.
Keep Canvas additional shader channels TexCoord1 and TexCoord2 enabled for gradients.
Second and third rows are intentionally translucent, so their borders look dimmer.

Validated in Unity 6000.5.3f1: editable Scene hierarchy, 10 cards, 3 visible rows,
wheel index changes, 11 SVG graphics (10 frames + knob), 2 station canvases and
no Console errors during Play. No economy/save changes were made.
