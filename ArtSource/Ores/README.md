# Mining Ore Six-Ore Set

Low-poly Blender assets for the first six progression tiers of the mining game.

| Tier | Rarity | Ore | Mining power | Base sell value | File |
| ---: | --- | --- | ---: | ---: | --- |
| 1 | Common | Stone | 1 | 1 | `stone_tier1.glb` |
| 2 | Uncommon | Coal | 3 | 4 | `coal_tier2.glb` |
| 3 | Uncommon | Copper | 6 | 9 | `copper_tier3.glb` |
| 4 | Rare | Iron | 10 | 18 | `iron_tier4.glb` |
| 5 | Rare | Gold | 15 | 35 | `gold_tier5.glb` |
| 6 | Epic | Diamond | 25 | 75 | `diamond_tier6.glb` |

`mining_ores_six.blend` is the generated editable source scene. The original
`mining_ores.blend` is preserved. Each GLB is centered at the origin and includes
an empty root with the properties `ore_id`, `display_name`, `tier`,
`mining_power_required`, and `base_sell_value`.

The assets use meters, flat-shaded low-poly geometry, embedded ore deposits, and
reusable named materials. Gameplay values are copied into independent `OreData`
assets and remain editable in Unity GameData.

Rebuild all outputs with Blender 5.2:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python scripts/create_ore_assets.py
```

Validate that every GLB imports and retains its gameplay metadata:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python scripts/validate_ore_assets.py
```
