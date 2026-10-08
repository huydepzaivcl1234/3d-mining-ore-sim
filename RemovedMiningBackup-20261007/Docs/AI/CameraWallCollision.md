# Camera wall collision correction

The follow camera previously swept from its last position, slid along the wall,
then separately corrected visibility from the player focus. The slide changed
camera height and fed that off-axis position into the next frame.

Follow mode now sphere-casts the desired focus-to-eye ray directly. Obstructions
shorten orbit distance without changing the ray. Inward correction resets outward
velocity; clearance recovery uses `collisionReturnSmoothTime` in the Inspector.
Overlap recovery remains for exceptional initial intersections. Free camera,
rotation, shift lock, zoom, combat framing and cinematic control are unchanged.

## Verification (Unity 6000.5.3f1)

- Baseline isolated wall test: camera deviation from intended orbit ray 1.076 m,
  with non-monotonic distance when backing toward the wall.
- Corrected wall test: deviation below 0.001 m, monotonic inward distance,
  no overlap, stable height at rest.
- Removing the wall returned distance from 0.341 m to 4 m gradually.
- Corner tests at 30, 60 and 144 FPS: finite positions, no sphere overlap,
  ray deviation below 0.001 m.
- Connected Editor compiled successfully; Console contained no errors.
- Tests used temporary off-map objects and removed them in finally blocks.
- SampleScene disk hash remained unchanged; no player saves were written.
- Full interactive Play validation remains to be done: back into walls and
  corners, orbit sideways, zoom, then walk away in normal and shift-lock modes.
