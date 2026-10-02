# Character inventory and monster-head deflection

## Authoring / applying the patch

- Runtime edits target the live project at `D:/3d mining sim`.
- `Mining Simulator > UI > Setup Character Inventory And Readable Effects` is an opt-in, Undo-aware scene authoring action. It does not save the scene. Run it in the gameplay scene after importing the ZIP, then save manually with Ctrl+S.
- The current open scene has already been authored using that action. `SampleScene.unity` is intentionally excluded from delivery.
- Existing inventory item objects, slot indices, click/consume callbacks, quantities, Gift Box wheel, localization, modal fade and button SFX are retained. No new equipment gameplay has been introduced.

## Inventory

- Character Model Preview renders the current player's authored Geometry and shared meshes/materials into a 512x768 RenderTexture. It follows the assigned player/avatar, not a hardcoded Paladin asset.
- Preview construction copies only transforms, mesh renderers and skin/bone references. It does not clone scripts, physics, audio, VFX or health bars. Its Animator disables events/root motion and masks out combat layers.
- Closing or hiding the panel releases the stage, preview camera and texture. Reopening rebuilds from the current model. The camera is portrait-only and never controls the gameplay camera.
- Preview settings (resolution, layer, FOV, stage position, yaw, padding and idle state) are editable on `Character And Equipment/Character Model Preview`.
- Layer 31 is the default isolated rendering layer; no ProjectSettings file is changed. If this project later uses that layer for other objects, choose a dedicated unused layer on the preview component.
- Five equipment placeholders follow `Group 1.svg` coordinates. They are noninteractive visual reservations; inventory capacity stays 32.
- The provided SVGs embed PNGs through SVG patterns. Unity's importer creates a Sprite but does not render those pattern fills in this project. Originals are retained under GameData/UI/InventoryReferences. The authored UI reproduces the visible gray frames and layout rather than depending on invisible imported fills. `images 1.svg` guides the lower item grid, not a flattened image over interactive items.
- Edit layout through MiningUiData's Inventory fields, then rerun the setup action; or directly edit the authored RectTransforms. The setup action does not run automatically at startup.

## Active effects

- Root size increased from the authored 254x58.24 to 660x150 reference pixels.
- Name/percentage font 20, timer font 24 and header 19, all driven by existing MiningUiData fields instead of the prior hardcoded 6-13 point limits.
- Timer is raised above the duration progress strip; name supports two lines. Card slots, accent colors, countdown and entry animation still derive from the existing effect system.

## Landing on enemy heads

- `PlayerMonsterHeadDeflection` receives CharacterController top-contact callbacks on the player, for living MushroomMonster-based species (including Golem).
- Data settings live in PlayerStatsData: enable flag, sideways speed, downward speed, duration, minimum upward contact normal. Defaults: 7 m/s sideways, 4 m/s down, .45 seconds, normal .5.
- The existing ThirdPersonController owns the impulse, gravity and swept Move. No Rigidbody, teleport, collision-layer change, or disabled enemy collider is introduced.
- Grounded/jump is suppressed while the impulse is active. Sideways displacement clears the monster before gravity returns the player to the floor. Respawn clears the impulse.
- Contact with the side of an enemy and ordinary ground is not treated as a head landing. No damage or progression changes are introduced.

## Validation

- Unity Editor compilation and Play Mode checked through Unity MCP.
- Actual Paladin model visible; five placeholders and 32 original slots verified visually.
- Portrait stage contains zero gameplay MonoBehaviours, colliders or AudioSources and one preview-only camera; stage removed after close.
- Empty-slot click preserves occupied count. Item consumption and Gift Box code paths retained; no saved items consumed during testing.
- Mushroom top landing: player displaced ~1.19m sideways, then grounded at floor, feet y~.01, below monster top y~1.10.
- Golem top landing: player displaced ~1.22m sideways, grounded at floor, feet y~.01, below monster top y~2.39.
- Effects screenshot uses presentation-only preview values. Inventory counts, live timed effects and saves are not modified by that preview.
- Pre-existing missing Behaviour warning remains outside this change.
