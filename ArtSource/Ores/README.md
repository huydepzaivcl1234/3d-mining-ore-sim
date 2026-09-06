# Mining Ore Starter Set

Low-poly Blender assets for the first three progression tiers of the mining game.

| Tier | Ore | Mining power | Base sell value | File |
| ---: | --- | ---: | ---: | --- |
| 1 | Stone | 1 | 1 | `stone_tier1.glb` |
| 2 | Coal | 3 | 4 | `coal_tier2.glb` |
| 3 | Copper | 6 | 9 | `copper_tier3.glb` |

`mining_ores.blend` is the editable source scene. Each GLB is centered at the origin and includes an empty root with the properties `ore_id`, `display_name`, `tier`, `mining_power_required`, and `base_sell_value`.

The assets use meters, flat-shaded low-poly geometry, embedded ore deposits, and reusable named materials. The values are starter balancing placeholders and can be adjusted once the mining loop is implemented.

Rebuild all outputs with Blender 5.2:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python scripts/create_ore_assets.py
```

Validate that every GLB imports and retains its gameplay metadata:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python scripts/validate_ore_assets.py
```
