# Controls, durability visibility and miner pricing

Implemented against the current D:/3d mining sim checkout; existing scene edits are not auto-saved or included in this ZIP.

## Settings

Settings > Controls exposes primary forward/backward/left/right bindings, jump, sprint and camera shift lock. Click a binding, press a keyboard key or mouse button; Esc cancels and timeout cancels after the editable Rebind Timeout Seconds. Conflicts among these displayed controls are rejected. Secondary arrow-key and gamepad bindings remain unchanged; other gameplay actions such as attack/interact retain their existing bindings.

Changes persist in MiningSimulator.PlayerBindings.v1 and MiningSimulator.CameraLockBinding.v1. Reset Keys resets these displayed bindings without deleting gameplay progress. Menu closure cancels an unfinished rebind and keeps the original action enable state. Imported .inputactions files and StarterAssets assembly dependencies are unchanged. Lean Localization English/Vietnamese keys were added.

Opt-in authoring menu: Mining Simulator/Setup/Controls And Miner Health. Adds a Controls submenu to existing Settings, fits its footer alongside Main Menu, and assigns the existing shared MicroBar prefab to NpcData when not already assigned. The authored hierarchy is editable in Scene. Save your scene manually after setup. Existing Settings header, audio/sensitivity sliders, language, reset and close controls remain.

## Health displays

OreHealthBar and LuckyBlockHealthBar start hidden. A durability decrease reveals their renderer group and restarts Hide After Hit Seconds (editable, default 3 seconds). The listener/component stays enabled, so subsequent hits can show a hidden bar; pooled reactivation resets visibility. Damage, rewards and mining behavior are unchanged.

NpcData exposes Miner Health Bar Prefab, Offset and Scale. MiningNpcHealthBar uses the existing MicroBar/TextMeshPro style and follows above the miner. Miner HP remains the existing fixed MinerHealth, not player/combat level. Monster damage emits HealthChanged before lethal removal. No purchased NPC is resurrected by the health display.

## Pricing

Next price = ceil(base price * (1 + purchase increase % / 100)^living purchased miners * (1 + rebirth increase % / 100)^completed rebirths). Both percentages are editable in NpcData. Losing a miner lowers the living-population price tier; losing all returns to this rebirth's base tier. Rebirth clears purchased miners as before but retains its completed count for price scaling. Reset Data clears rebirth count as before. The legacy total-purchases save remains compatible but no longer determines price. No reward refunds or existing wallet deductions were added.

## Verification

Unity MCP compile reported zero compiler errors. Runtime tests: keyboard W -> K, duplicate K rejected for backward, mouse Forward Button accepted for jump, cancelled lock binding stayed Left Shift. User binding preference keys and runtime overrides were restored after tests. Ore and lucky bar initial hidden -> hit visible -> hidden after 3 seconds. A temporary unowned miner's HP changed 20 -> 15 and text became 15 / 20 without changing shop population. Price calculations returned 31/28/25 at living counts 2/1/0 and 25/28/31 at rebirth counts 0/1/2 with the current 10% values. UI screenshots checked the controls rows and settings footer. Existing Missing Script warning remains unrelated. Physical keyboard/gamepad playfeel needs user playtesting.
