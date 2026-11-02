# Smooth reload / shooting authoring handoff

Source: FullBody_ReloadSmooth_Shoot_Edit.blend. Reload1-145, shooting181-221. Reload assets updated in Unity; shooting clip remains author-editable in Blender and has not replaced gameplay shooting.

Custom Blender validation: gun position second-difference RMS0.0105868m ->0.0047729m (54.9% reduction); finger basis error<2.4e-7; insertion sideways error<1.2e-7; arm target reach error max0.51mm; shoot slide stroke32mm. This metric describes frame-to-frame motion, not a guarantee of subjective realism.

Latest Play Mode: ADS before reload=true; reload observed=true; slide pull and return=true; ammo refilled9/9; ADS during reload flags=0; FPS/world state mismatches=0; root displacement over test=0.006070435m, maximum3-frame displacement=0.001987913m (no spawn reset observed).

Console:0 errors/warnings at final check. No C# changes in this smoothing pass. FPS pose/reload asset unchanged; world reload145frames re-baked with same duration2.4s. Backup assets and user-scene snapshot in Before_Smooth_Update/.

Blender rendered/viewed closeups and previews: ../BodyAnimationReview-20261026/SmoothPreview/, Reload_Smooth_Preview.mp4, Shoot_Edit_Preview.mp4. Original Reload_Unity_FPS_World.mp4 documents the earlier version; new live frames are SmoothPlayFrames/.

Not tested: multiplayer, standalone build. Inherited negative Blender rig scale retained.
