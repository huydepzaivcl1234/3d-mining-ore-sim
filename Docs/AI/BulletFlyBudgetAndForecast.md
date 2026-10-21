# Bullet Fly cleanup and right-side monster forecast (2026-10-16)

Bullet Fly remains an available upgrade; save/unlock IDs are unchanged.

## Evidence and bounded fix

The previous runtime capped only visible lines; logical volleys could grow without
a limit and pooled renderers remained allocated indefinitely. A controlled 500-proc
test reproduced 500 concurrent logical volleys. This is evidence of unbounded work,
not a measured explanation of every FPS drop in the full game.

BulletFly.asset now exposes maximumConcurrentVolleys (24), retainedVisuals (16),
idleCleanupSeconds (2) and maximumFlightSeconds (6). Existing designer-authored
damage array, colour and other balance values are retained.

- If a new proc exceeds the concurrent limit, remaining damage from the oldest
  volley resolves immediately and that volley is released. No damage is dropped;
  under overload the timing of the oldest damage is intentionally accelerated.
- A timed-out flight also resolves its remaining damage and releases its visuals.
- Dead/despawned targets cancel their volleys. A volley snapshots its damage array,
  so editing the array during Play does not invalidate active indices.
- Excess idle lines are destroyed, not retained. After an idle interval all spare
  lines/volley buffers are released. Clear/disable/destruction also empty the pool.
- Normal unsaturated sequential homing behaviour is unchanged.

Damage popups are grouped under one runtime root, retain at most 16 idle instances
(editable maximumRetained), and preserve the existing maximum-visible budget.
Transient runtime cleanup is not deletion of authored assets or upgrades.

## HUD

MonsterDailyForecastHud anchors to top-right, with editable panelOffset default
(-24,-124), below Settings. Existing list, animations and localization remain.
No scene was saved or rewritten.

## Validation

- Connected Unity Editor compiled without errors.
- Same 500-proc synthetic input: previous 500 active vs candidate 24, max 150 visual
  lines; candidate pool and active count reached zero after completion/cleanup.
- All 500 x 27.5 = 13750 true damage reached the test enemy.
- Unsaturated volley remains sequential; Clear removes active and retained lines.
- 100 popup emissions respected a test budget of 8 and shared one hierarchy parent.
- Game View screenshot inspected: forecast at right, below Settings; centre clear.
- Isolated GameSaveStore used; test store detached after returning to Edit Mode.
- No before/after frame-time benchmark was collected; no specific FPS gain claimed.
