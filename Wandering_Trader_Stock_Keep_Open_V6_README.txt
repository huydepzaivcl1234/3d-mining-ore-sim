WANDERING TRADER V6 - STOCK + KEEP PANEL OPEN
================================================

Install
-------
1. Finish or abort any Git rebase currently in progress.
2. Close Unity.
3. Extract this ZIP into the Unity project root (the folder containing Assets).
4. Allow overwrite for the two included C# files.
5. Reopen Unity and wait for script compilation.

Changes
-------
- The trader panel stays open after a successful Buy or Sell transaction.
- Each Buy and Sell offer has finite stock for the current restock period.
- A successful transaction consumes one stock unit (one offer bundle).
- The action button displays remaining stock, for example: BUY 7/8.
- At 0 stock the button is disabled, so that offer cannot be bought or sold again.
- A timed offer refresh creates new offers and fully resets their stock.

Default stock by rarity
-----------------------
- Common: 8
- Uncommon: 6
- Rare: 4
- Epic: 2
- Legendary: 1

These five values are editable on WanderingTraderSystem in the Inspector under
"Offer Stock Per Restock". OnValidate keeps Common > Uncommon > Rare > Epic >
Legendary and keeps every value at least 1.

Files included
--------------
- Assets/Scripts/Ores/Trader/WanderingTraderPanel.cs
- Assets/Scripts/Ores/Trader/WanderingTraderSystem.cs

Suggested Unity test
--------------------
1. Open the trader and repeatedly buy one Common offer.
2. Confirm the panel remains open and its stock decreases after every purchase.
3. Confirm the button becomes disabled at 0 stock.
4. Repeat on the Sell page and confirm sell stock behaves the same way.
5. Wait for the restock timer and confirm all new offers receive fresh stock.
6. Test Common, Uncommon, Rare, Epic, and Legendary items.

Validation performed before packaging
--------------------------------------
- git diff --check passed.
- Constructor usages and stock guards were statically reviewed.
- Package contents were compared byte-for-byte with the edited source files.
- Unity Play Mode/compilation was unavailable in the packaging environment.
