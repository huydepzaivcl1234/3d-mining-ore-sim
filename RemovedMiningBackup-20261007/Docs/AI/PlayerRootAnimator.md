# Player root Animator

Stop Play Mode. Select Player or its model in the Hierarchy, then run:
Mining Simulator > Setup > Move Player Animator To Root.
Save the scene (Ctrl+S), or save the prefab when using Prefab Mode.

The command moves the child model's Humanoid Avatar onto the Player Animator,
preserves the root's existing controller, updates references to the removed
child Animator, and removes attached PlayerAnimationAudioRelay components.
It leaves the model, bones, transforms, assigned SFX, and animation assets unchanged.
Ctrl+Z undoes the migration. Multiple child Animators are rejected to avoid
changing unrelated animated objects. No runtime forwarding script is required.

Animation events must still exist: OnFootstep at foot contacts on walk/run
clips, and OnLand at ground contact on the landing clip. Both are received
directly by ThirdPersonController on Player. Use AnimationEvent parameters.

When changing models: replace the visual child, use its valid Humanoid Avatar,
and run this command again if the replacement has its own Animator. Do not
keep an old visible skeleton/model alongside the replacement. Generic rigs
are not compatible with this Humanoid setup.

This patch supplies an opt-in migration, not a replacement scene. C# compilation
is checked separately; live animation and audible playback need Editor testing.
