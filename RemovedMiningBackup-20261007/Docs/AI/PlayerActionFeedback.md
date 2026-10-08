# Jump and attack feedback

Run Mining Simulator > Setup > Configure Player Jump Attack Feedback once.
This assigns the project's existing whoosh.mp3 only to empty sound fields.
Replace Jump Sfx and Attack Sfx independently on Assets/GameData/Player/PlayerStatsData.asset.
The shared MiningAudioManager handles volume, SFX mute, and mixer routing.
No synthesized or fallback audio is added. Existing footsteps and landing remain unchanged.

Jump sound fires on an accepted jump, not on every Space press.
Attack sound fires once when the Animator enters Attack, not on rejected clicks.
The visual arc fires at Hit Time, uses the actual attack range and angle, and fades
in a duration adjusted by Attack Speed. It is visual only and does not deal damage.
Edit Show Attack Arc, Attack Arc Color, Width, Seconds in PlayerStatsData.
Assign Attack Vfx Prefab for a custom prefab instead of the arc; Lifetime controls cleanup.
Custom effect prefabs should use Particle Systems with Play On Awake.

No controller, animation clip, scene, or player prefab is replaced.
For player builds, retain Sprites/Default in shader inclusion or use an authored
VFX prefab with a referenced material. Test jumping, rejected attacks, repeated
attacks, moving attacks, pause/mute, and disable/respawn in the Editor.
