Mining Simulator - localization and Candy cleanup overlay

1. Close Unity. Extract this ZIP over the root of your existing Unity project.
   The ZIP does not contain SampleScene.unity and will not overwrite your scene.
2. The ZIP overwrites the obsolete MiningCandyUiSetupMenu.cs with a harmless
   compatibility placeholder and keeps its existing GUID. Do not delete any
   script or .meta file to fix the CandyGlossAlpha compilation error.
3. Open your OWN gameplay scene in Unity. If you want to remove its existing
   Candy effects, choose Mining Simulator > Fixes > Remove Candy UI From Open Scene.
   Inspect the UI, then save your scene yourself.

MiningCandyGradient.cs must remain for now: your own SampleScene currently
references this component 239 times. Deleting it before step 3 would create
Missing Script components. This ZIP deliberately does not delete it.

The extracted files include localization updates from the prior fixes.
No SampleScene.unity is included in the ZIP.
