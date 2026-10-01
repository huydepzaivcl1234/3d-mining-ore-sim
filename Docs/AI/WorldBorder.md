# Editable world border

MiningWorldBorder prefab creates four invisible solid BoxCollider walls around the
playable rectangle. No floor, ceiling, visible mesh, player teleport or input changes.
Default interior size is 198 x 198 metres and wall height 40 metres, centred on the
current terrain (7.79, -0.01, 20.15). Walls extend 3 metres below the root and are 2
metres thick. Ordinary physics collisions also block NPCs and monsters.

## Scene editing

Select MiningWorldBorder in Hierarchy, enable Scene Gizmos, then drag the cyan face
handles. Each dragged edge changes size and moves the centre so the opposite edge
stays fixed. Inspector Size X/Z controls width/depth; Y controls height. Transform
Move/Rotate/Scale applies to the entire border. Keep its scale positive and Y rotation
upright for a normal map boundary. Increasing Y prevents jumping over the walls.

Disable the component/object to turn off its walls. Delete the whole border object
to remove it. Create / repair border walls restores missing owned colliders without
creating duplicates. An additional border can be created from Mining Simulator >
Setup > Create World Border, or by dragging Assets/Prefabs/Systems/MiningWorldBorder.prefab.

The prefab and code are saved. The border instance was added to the existing open
Scene, but the Scene was intentionally not saved automatically: press Ctrl+S to retain
it with your other pending edits. SampleScene.unity is excluded from the delivery ZIP.

## Validation

Unity MCP compiled both scripts. Focused Play Mode CharacterController checks passed
for +X, -X, +Z, -Z and the diagonal corner of a 10 metre test border: stops around
4.62 metres from centre. Resizing width to 20 metres moved the stop to 9.62 metres.
Repeated rebuild kept exactly four colliders. Disabling the component permitted crossing.
Temporary test objects were destroyed. No Console errors remained after testing.
This is a physical wall, not a NavMesh bake or a teleport/flight containment system.
