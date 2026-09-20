WANDERING TRADER UI V3 - INSTALL AND EDIT

INSTALL

1. Exit Play Mode in Unity.
2. Extract this ZIP into the root of your Unity project.
3. Choose Replace when asked about matching files.
4. Wait for Unity to compile and confirm there are no red Console errors.
5. Run:
   Mining Simulator > Setup > Rebuild Wandering Trader Full Scene UI
6. Save SampleScene (Ctrl+S).

EDIT COIN AND GEM ICONS

1. Select Wandering Trader Panel in the Hierarchy.
2. On the WanderingTraderPanel component, open Currency Icons.
3. Drag your Coin sprite into Coin Icon.
4. Drag your Gem sprite into Gem Icon.

If an icon is empty, the UI safely shows COIN or GEM text instead.
Each item icon still comes from that item's Inventory Icon field.

EDIT ONE ITEM'S SHOP SETTINGS

Select an item asset in Assets/GameData/Items, such as Banana.asset.
The Wandering Trader Shop section contains:

- Trader Can Buy: item may appear on the Buy page.
- Trader Can Sell: owned item may appear on the Sell page.
- Trader Minimum Offer Amount: smallest random bundle.
- Trader Maximum Offer Amount: largest random bundle.
- Trader Buy Value: minimum total Coin price for the whole bundle.
- Trader Coin Buy Maximum: maximum total Coin price for the whole bundle.
- Trader Gem Buy Value: minimum Gem price per item.
- Trader Gem Buy Maximum: maximum Gem price per item.
- Trader Sell Value: minimum Gem reward per item sold.
- Trader Gem Sell Maximum: maximum Gem reward per item sold.

BANANA EXAMPLE

Set:

- Minimum Offer Amount = 1
- Maximum Offer Amount = 3
- Trader Buy Value = 4000
- Trader Coin Buy Maximum = 5000
- Trader Gem Buy Value = 4
- Trader Gem Buy Maximum = 5

Results:

- A Coin offer costs about 4,000-5,000 Coin for the complete bundle.
- A 1 Banana Gem offer costs 4-5 Gems.
- A 3 Banana Gem offer costs 12-15 Gems.

ADD MORE BUY OR SELL ITEMS

1. Create or select a MiningItemData asset.
2. Set its item icon, offer quantity and price ranges.
3. Enable Trader Can Buy and/or Trader Can Sell.
4. Add the item to MiningItemDatabase.asset > Items.

The trader automatically includes enabled database items in random offers.
The Sell page only lists sell-enabled items that the player currently owns.

NOTES

- Buy offers randomly choose Coin or Gem.
- Coin prices are bundle totals and are not multiplied by quantity.
- Gem buy and sell values are per item and are multiplied by quantity.
- The panel keeps the full authored child/card hierarchy from V2.
- Trader symbols use font-safe ASCII fallbacks (>, X, COIN and GEM).
