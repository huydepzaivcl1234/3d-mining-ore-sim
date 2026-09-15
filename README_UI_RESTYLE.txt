Mining Simulator - restyle only UI affected by Candy removal

1. Close Unity and extract this ZIP over the ROOT of your existing project.
   This ZIP has exactly four new files under Assets/Scripts/Ores and this note.
   It does NOT include SampleScene.unity or overwrite your scene.
2. Reopen Unity and open YOUR gameplay scene with Mining HUD Canvas.
3. Exit Play Mode, then choose:
   Mining Simulator > Fixes > Restyle UI After Candy Removal
4. Inspect the panels, Inventory, Quests, Settings, Shop, HUD and buttons.
   Save your own scene only after you like the result.

This menu targets 145 exact Canvas paths that had gradients before cleanup.
It restores their recorded colors with a generic UI gradient, without Candy
highlights. It skips UI that still has its old gradient and does not edit icons,
text, scroll-view hit areas, other UI, gameplay scripts, or project settings.
Run it again safely if necessary; existing generic gradients are not duplicated.
If a target was renamed or deleted in your scene, the dialog reports it as
"no longer found" and leaves that UI untouched.
