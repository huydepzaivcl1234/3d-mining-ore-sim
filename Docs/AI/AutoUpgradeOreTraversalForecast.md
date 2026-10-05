# Auto upgrade, ore traversal, forecast presentation

## Behaviour
- Auto upgrade is unlocked once using Gem, then purchases with Money via the existing upgrade purchase API (same prices, caps, effects, feedback). The panel button switches it on/off.
- Unlock, enabled state and save key are persisted independently of upgrade stacks. Rebirth retains the unlock; full Reset Data clears it. Existing saves remain compatible.
- MiningUpgradeData exposes Gem unlock price (default 25), tick interval (default 0.5 seconds), and purchase order. One purchase per tick; affordable entries are visited round-robin. Card-only regeneration upgrades are excluded.
- Miner/monster collider pairs ignore Ore only. Registration handles either spawn order and re-enable/pooling. Player collisions and ore mining colliders are preserved.
- Runtime Ore carving, AI segment clearance, miner local steering and fallback A* occupancy no longer treat Ore as blocking. Non-Ore obstacles retain their behaviour. Monster attack obstruction checks ignore Ore too.
- Forecast rolls/slides offscreen at night and returns in the morning. Rows appear in sequence, counters step with reel-style motion, and heading/event text is typed progressively. Boss invasion or a live boss adds a boss-present label. Existing modal HUD visibility remains authoritative.
- Animation timings, row delay, typewriter speed, tilt and offscreen padding are editable on MonsterDailyForecastHud. The UI is generated at runtime; SampleScene is untouched.

## Validation
- Connected Unity Editor compilation completed without errors.
- Five isolated NUnit fixture methods passed: one-time Gem unlock/Money purchases; insufficient funds; persistence/Rebirth/reset; exclusion of Card upgrades; AI collision pairs/pooling with player collisions preserved.
- Isolated forecast presentation stepping validated day/count/boss, offscreen night hide, morning return/typewriter restart and modal visibility.
- Isolated button layout check validated one button after repeated setup and placement within the authored 1920x1080 panel.
- No full Play Mode session was run on the user's dirty SampleScene; saves were tested using unique disposable test keys only.
