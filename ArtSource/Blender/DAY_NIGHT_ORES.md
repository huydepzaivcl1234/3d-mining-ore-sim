# Day/Night Special Ores

Editable Blender source and Unity exports for `Light Stone` and `Dark Stone`.

- `day_night_ores.blend`: both models together for art editing.
- `light_stone.blend` / `dark_stone.blend`: working source snapshots.
- Unity FBX outputs: `Assets/Ores/Models/light_stone.fbx` and `dark_stone.fbx`.
- Pivot: ground center. Blender Z-up is exported as Unity Y-up.
- Total geometry: 884 triangles across both ores.
- Materials: a body slot and a core slot on each model.

Rebuild from the project root with Blender 4.5 LTS or newer:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 4.5\blender.exe' --background --python scripts/create_day_night_ore_assets.py
```

The halo and shadow mist are Unity URP shader effects. They are intentionally
not baked into the mesh and do not use purple particle dots.
