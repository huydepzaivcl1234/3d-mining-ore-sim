# Forest Golem damage-circle alignment

The same warning shader/radius were already used by both golems, but Forest's
first center was (0, 0.95) instead of (-0.02, 1.61) and the second was (0.84, 1.14)
instead of (0.30, 1.71). The correction copies regular Golem's circle shape, radii,
offsets, warning color and shader into ForestGolemRewards. No runtime scripts or
SampleScene changes are needed.

Forest-specific contact phases (0.50/0.54) remain unchanged. The current Inspector
wave-extra setting was 2 m and is preserved rather than resetting it to the old
1.2 m default. Charge and burn settings remain untouched.

Validation: compare ContainsHitPoint on both prefab copies across first/second
attacks, yaw 0/67 degrees, scales 1/1.5 and 29,768 sampled points in a disposable
preview scene; verify identical warning shader/color and unchanged Forest timing
and wave configuration. This is geometry/config validation, not a new gameplay
Play Mode run.
