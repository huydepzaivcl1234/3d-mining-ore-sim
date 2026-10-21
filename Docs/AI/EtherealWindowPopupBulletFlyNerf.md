# Ethereal window, popup data and Bullet Fly balance (2026-10-16)

## Ethereal Weapon

Each living monster has its own window. A direct gun hit opens it for 3 scaled
seconds; later hits during that window do not refresh its end. Owned damage effects
can apply on-hit effects within the window, gated once per second independently
per monster. Effect damage cannot open or extend windows. After expiry there is a
1-second lockout; a fresh direct shot after that lockout can open a new window.
Regular direct-shot on-hit effects still work during lockout.

The duration, per-target proc cooldown and reactivation lockout are editable in
`Assets/GameData/Player/Weapons/OrangeUpgrades/EtherealWeapon.asset`. Dead/disabled
targets are pruned, and reset/disable clears all window state. Save IDs unchanged.

## Popup data

The authoritative data is now
`Assets/GameData/Player/Weapons/DamagePopupData.asset`. Existing font, material,
colours, spread, animation and pool settings were copied without overwriting them.
The former embedded settings remain hidden solely for serialized backward
compatibility; runtime resolves through the new data reference. The unused legacy
PlayerStatsData popup field is hidden to avoid editing the wrong source.

Settings:

- Scale With Damage: enable automatic sizing; off uses legacy Font Size.
- Damage For Minimum Size / Damage For Maximum Size: interpolation thresholds.
- Minimum Font Size / Maximum Font Size: clamped base font range, before existing
  pop/transform animation. Values between thresholds interpolate linearly.
- Existing world scale and punch remain editable, so apparent screen size also
  depends on those settings and distance.

Default migrated limits are 70% and 140% of the previous font size (14/28 for the
current asset's previous font size 20). No scene changes required.

## Bullet Fly

Track successful Bullet Fly hits survived by each target separately. After five
survived hits, every subsequent volley starts at 95%, 90%, 85%, then 80% original
damage, with a default maximum reduction of 20%. All hits of one volley share the
multiplier captured at its first impact; existing started volleys do not change
mid-flight. A fresh target starts at full damage. Dead/despawned targets are removed;
upgrade reset/disable also clears this transient state.

Threshold, reduction per volley and maximum reduction are editable in BulletFly.asset.
Normal flight and over-budget/timeout resolution share one damage calculation.
Re-entrant volley requests from effect damage are deferred until the next Step,
avoiding list mutation/reuse of a volley during its own DealDamage callback.
The visual root now retains scene ownership (DontSaveInBuild).

English/Vietnamese Lean descriptions reflect the new window and reduction rules.

## Validation

- Connected Unity Editor compiled without errors; no scene was saved.
- Isolated-save runtime test: effect cannot open a window, direct hit opens,
  repeated shots do not extend, once-per-second proc gate, independent victims,
  effects stop at 3 seconds, lockout, effects cannot reopen after expiry, new shot
  reopens after lockout. All passed against real scaled time.
- Five-hit volleys of 100 per hit: 500, 475, 450, 425, 400, 400, 400 damage.
- Single-hit volleys: 100 for five survived hits, then 95. Fresh target full damage.
- Over-budget resolution: first two five-hit volleys removed 975 HP (500 + 475).
- Rendered TMP components at damage .1 / 50.5 / 100000 used font sizes 12 / 21 / 30
  with test thresholds 1..100; reference resolves to the new asset settings.
- 100 direct on-hit burst with Ethereal/Bullet Fly: active work stayed within 24;
  after the chain ended active volleys, visual count and pool count were all zero.
- Actual player save not reset; fixture store detached after returning to Edit Mode.
