WANDERING TRADER UI V4 - INSTALL

1. Exit Play Mode in Unity.
2. Extract this ZIP into the root of your Unity project.
3. Choose Replace for matching files.
4. Wait for Unity to compile and check that the Console has no red errors.
5. Run:
   Mining Simulator > Setup > Rebuild Wandering Trader Full Scene UI
6. Save SampleScene with Ctrl+S.

NEW IN V4

- Shared button-click SFX is assigned to every trader button.
- Runtime-created buttons automatically find MiningAudioManager.
- Smooth fade-and-pop animation when opening the trader.
- Smooth fade-and-shrink animation when closing the trader.
- Smooth fade-and-scale transition between Buy and Sell pages.
- Animation duration and scale values are editable on WanderingTraderPanel.
- Coin and Gem icons remain editable on WanderingTraderPanel.
- Each item keeps its editable bundle amount and Coin/Gem price ranges.
- New buy/sell-enabled items in MiningItemDatabase automatically join random offers.

BUTTON SFX SETUP

The trader uses MiningAudioManager and the Button Click SFX assigned in
Assets/GameData/Audio/MiningAudioData.asset. Make sure that clip is assigned.

PRICE EXAMPLE

For Banana:

- Minimum Offer Amount = 1
- Maximum Offer Amount = 3
- Trader Buy Value = 4000
- Trader Coin Buy Maximum = 5000
- Trader Gem Buy Value = 4
- Trader Gem Buy Maximum = 5

Coin is the total bundle price. Gems are per item, so 3 Banana costs 12-15 Gems.

GIT NOTE

The remote-sync issue was resolved locally by rebasing main onto origin/main.
The duplicate local trader commit was automatically detected as already upstream.
The completed local commit is 8664f3d. No force-push was used.
