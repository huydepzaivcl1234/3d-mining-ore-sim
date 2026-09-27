# E toggle and health status

E alternates standing/combat; the second press no longer fires DrawWeapon Trigger again. DrawWeapon Bool is set true/false. For Trigger-based controllers use separate DrawWeapon and SheathWeapon triggers. Existing CombatMode Bool remains supported. Disable/death clears combat mode and aim.

Player health text now has a Standing/Combat line above level and HP, using the existing health label. Monster text is unchanged. Status refresh does not snap/reset the authored MicroBar damage animation. Existing input bindings and scene are preserved.

The on-disk Player controller.controller bound in SampleScene still contains only a locomotion Blend Tree: no draw/sheath states or parameters. This patch supplies the state and commands, not a physical sword mount or missing animations. Save the authored Animator into the repo, or add DrawWeapon Bool with true => draw, false => sheath transitions (Has Exit Time disabled on combat-to-sheath). Alternatively add the separate triggers described above.

Static diff checks passed. Toggle/reset regression test added; Editor compile, tests and Play Mode unverified. No desktop control requested this turn.
