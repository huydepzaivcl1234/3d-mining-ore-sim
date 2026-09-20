WANDERING TRADER UI V5 - INSTALL

1. Exit Play Mode in Unity.
2. Extract this ZIP into the root of the Unity project.
3. Choose Replace for matching files.
4. Wait for Unity to compile and check that the Console has no red errors.
5. Run:
   Mining Simulator > Setup > Rebuild Wandering Trader Full Scene UI
6. Save SampleScene with Ctrl+S.

V5 CHANGES

- Every restock creates exactly 3 Buy offers and 3 Sell offers.
- If fewer than 3 eligible items exist, an item can repeat with a new random quantity/price.
- Sell offers include all sell-enabled database items, even when the player owns none.
- A Sell button stays disabled until the player owns the requested quantity.
- Offers remain unchanged when opening the trader or completing a trade.
- Both pages reroll together only when the restock timer expires.
- Opening, closing and page switching are now scale-only animations.
- UI opacity stays at 100%, preventing flashes and color shifts.

REQUIREMENT

At least one item in MiningItemDatabase must have Trader Can Buy enabled to fill the Buy page.
At least one item must have Trader Can Sell enabled to fill the Sell page.

LOCAL MAIN COMMIT

37bf6a3 Keep trader offers full between restocks
