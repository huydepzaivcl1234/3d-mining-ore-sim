# Monster animation folders and hit masks

Active gameplay animations now live in `Assets/Animations/Monsters/<Species>/` for Bat, ForestGolem, Golem, Mushnight, Mushranon, Mushroom, GodBoss and Skullclaw.

- `<Species>.controller`: original controller GUID retained when moved.
- `Clips/`: all clips used by that controller, including editable extracted copies of FBX/vendor clips. Original vendor/model files remain intact for meshes, import settings and sample content.
- `<Species>_UpperBody.mask`: per-rig mask, not one humanoid mask reused on incompatible generic rigs.
- `Skullclaw/Sources/`: its animation-only FBX sources, moved with their original GUIDs. SkullclawSetup now reads/writes these new paths.

Base Layer owns Idle/Walk/Run/attacks/death and gameplay contact timing. `Hit Reaction` is an Override layer, weight 1, with an Empty default state and a masked Damage state returning to Empty at clip completion. Generic masks enable the spine subtree and exclude root/hips/legs/tail. Bat enables head and wings, retaining its existing flight/flap base animation. Root motion settings and movement/collider configuration are unchanged.

MushroomMonster sends nonlethal direct gun-basic-hit reactions to this layer instead of replacing the base state. Active reactions are not restarted by rapid shots; magic/true procs, burn, bleed, bombs, Bullet Fly and other non-shot damage never start the skeletal reaction. The health event carries scoped IsGunShot context (including nested-effect restoration), verified against the PistolShooting component shared by pistol and rifle. Committed attack windup, Skullclaw jump, Forest Golem charge and tower-hit suppression keep their existing protection. Mushnight's theft response is preserved. Death immediately clears the overlay. The old base Damage state remains as compatibility fallback for controllers not migrated.

GodBoss has no authored Damage clip: its folder and upper-body layer/mask are prepared but empty. Its existing squash/tint feedback stays intact. No attack/roar was repurposed as a hit animation, and no new GodBoss damage-animation handler was added.

## Edit in Unity

1. Open the species controller in Animator.
2. Select Layers -> Hit Reaction. Its gear uses Override, Weight 1 and that species' UpperBody mask.
3. Click the mask, expand Transform: spine/head/arms enabled; root/hip/legs disabled. Do not enable hips to increase a reaction, or it will also move the legs.
4. Replace the Damage state's Motion with a clip from that species' Clips folder. Change state Speed to tune speed. Keep the exit transition back to Empty and the Empty default state.
5. Leave Walk/Run and attack/death clips in Base Layer. No scene component wiring is required for existing monster prefabs.

## Validation and rollback

Live Editor compiled; eight asset fixture cases check prefab/controller references, per-species clip ownership and masks, and all thirteen existing parameterless Skullclaw fixture tests passed. These fixtures were invoked directly in the live Editor, not via a full Test Runner suite. Play Mode samples compared reference Walk with actual health-event-triggered overlays for all seven species that have Damage clips. Lower-leg local rotation/position differences were zero, while the upper-body reaction changed and returned to Empty. GodBoss's prepared layer retained its Walk motion.

An isolated live navigation probe additionally verified seven repeated damage reactions while moving approximately 3.25 m, never entering base-layer Damage. A preliminary chase probe in the user's active map could not move because of its local target/navigation state, so movement evidence comes from the isolated probe, not a claimed full-map navigation fix. Test objects and the temporary navigation grid were removed/restored. This is not an FPS benchmark or full combat regression suite. The authored scene was not saved.

The task delivery includes the asset move manifest and validation reports. Original moved assets were backed up in the task workspace at `Backups/MonsterAnimationLayers-20261022/`. Moves preserve GUIDs; on another checkout use AssetDatabase.MoveAsset according to the manifest before replacing contents. Do not copy the same .meta GUID to both old and new paths. Vendor source assets were not deleted.
