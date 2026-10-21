# Damage popup reset / Play lifecycle regression (2026-10-16)

## Confirmed causes

Resources.FindObjectsOfTypeAll found live and inactive MiningDamagePopup instances
in Edit Mode with HideFlags.DontSave and invalid scene handles, including four
old pool roots. Ordinary scene-only searches omitted them. DontSaveInEditor (part
of DontSave) detached them from the scene. They survived Stop and could not advance
their normal LateUpdate expiry. Existing reset paths did not clear the static pool
or these orphaned instances. Destruction after reload could also encounter a null
nonserialized linked-list node.

## Focused fix

- Runtime popup objects/root now use DontSaveInBuild, retaining valid scene ownership.
- Show refuses Edit Mode emissions.
- ClearAll immediately hides and detaches numbers, empties tracking collections,
  destroys transient popups and only owned pool roots. It includes inactive/orphaned
  objects through Resources; it does not match arbitrary scene objects by name alone.
- Both full reset entry points clear hit numbers after successful save reset.
- Editor lifecycle cleanup runs on Stop, Enter Edit, Enter Play preparation,
  assembly reload, quitting and delayed startup orphan cleanup.
- Safe linked-list detachment and immediate Edit Mode material cleanup handle
  nonserialized state lost during reload.
- Existing orphaned popup objects were explicitly removed from the current Editor;
  no authored scene, map, materials or real player progress was reset/saved.

## Verification

Live Editor compile passed. Isolated GameSaveStore fixture used for full reset.
20 emitted popup instances belonged to a valid scene. Full reset at timeScale=0
hid all immediately, ClearAll was idempotent, and new emissions still worked.
Two Play/Stop cycles left zero popup instances and zero roots; the second left a
10000-second popup active before Stop, ruling out normal expiry as the cleanup.
A subsequent Play began with zero old popup instances. Edit Mode Show was rejected.
Final verification interval had no new Console errors. Test store detached.
