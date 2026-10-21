# Bullet Fly: direct shots only (2026-10-16)

Bullet Fly now starts exclusively in GunSpecialUpgrades.OnBulletHit, called by
PistolShooting after a direct shot deals positive damage. Pistol and rifle share
that shooting path. A living monster is required for a homing target. Lethal shots
do not create a pointless volley on a corpse.

Removed the trigger from the global AnyDamaged handler. Bleed, Nail, Giant Bomb,
Double Hit splash, skills, towers and Bullet Fly's own damage cannot trigger it.
Other upgrade hooks, damage popups, budgets, damage arrays and save IDs are unchanged.
Lean English/Vietnamese descriptions now specify direct gun hits only.

Validation: connected Editor compiled; isolated-save Play Mode tests passed for
generic physical/magic/true damage (no proc), real bomb-wave damage (no proc), direct
hit (one proc), Nail/secondary damage (no additional proc), full sequential volley
(no self proc), lethal hit (no volley). Fixture detached; Editor returned to Edit Mode.
