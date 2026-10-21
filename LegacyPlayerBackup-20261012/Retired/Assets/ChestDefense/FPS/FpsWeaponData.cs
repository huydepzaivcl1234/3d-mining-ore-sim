using UnityEngine;
using MiningSimulator.Ores;

namespace ChestDefense.Fps
{
    public enum FpsFireMode { SemiAutomatic, Automatic }

    [CreateAssetMenu(menuName = "Chest Defense/FPS/Weapon")]
    public sealed class FpsWeaponData : ScriptableObject
    {
        [Tooltip("Stable save identity. Unique within the loadout; do not rename after shipping.")]
        public string weaponId;
        public string nameKey, displayName;
        public FpsGunView viewPrefab;
        public AnimationClip idle, shoot, reload;
        [Header("View — camera local coordinates")]
        public Vector3 hipPosition, hipEuler, aimPosition, aimEuler;
        [Tooltip("Align the animated rear/front iron sights while idle in ADS; retain shot/reload motion.")]
        public bool alignIronSights;
        [Min(.03f)] public float aimEyeRelief = .14f;
        [Tooltip("Camera-local metres: fine adjustment of the sight line, not muzzle placement.")]
        public Vector2 aimSightOffset;
        public float poseBlendSeconds = .15f;
        public float aimFieldOfView = 48f;
        public float animationBlendSeconds = .06f;
        [Header("Fire")]
        public FpsFireMode fireMode;
        public float roundsPerSecond = 4f;
        [Min(0f), Tooltip("Minimum seconds between accepted shots, even with player attack-speed bonuses. Zero uses only rounds per second.")]
        public float minimumShotInterval;
        [Tooltip("Multiply weapon fire rate by current player AttackSpeed / baseline AttackSpeed, preserving existing upgrades.")]
        public bool usePlayerAttackSpeed = true;
        public float range = 100f;
        public float hipSpreadDegrees = 1f, aimSpreadDegrees = .15f;
        public LayerMask hitLayers = ~0;
        public bool usePlayerDamage = true;
        public float damageMultiplier = 1f, flatDamage = 10f;
        public CombatDamageType damageType = CombatDamageType.Physical;
        [Header("Ammo")]
        public int magazineCapacity = 12, startingReserve = 60, maximumReserve = 240;
        public float reloadSeconds = 2f;
        public bool reloadWhenEmpty;
        [Header("Feedback")]
        public float recoilPitch = 1f, recoilYaw = .25f, recoilRecovery = 8f;
        public AudioClip shotSound, reloadSound, emptySound;
        [Range(0f, 1f)] public float soundGain = 1f;
        public GameObject impactPrefab;
        public float impactLifetime = 1f;

        public string LocalizedName => MiningLocalization.TextKey(nameKey, displayName);
        public double ShotInterval(float attackSpeedRatio)
        {
            if (!Finite(attackSpeedRatio) || attackSpeedRatio <= 0) attackSpeedRatio = 1;
            return System.Math.Max(minimumShotInterval, 1d / ((double)roundsPerSecond * attackSpeedRatio));
        }
        public bool TryValidate(out string reason)
        {
            reason = null;
            if (string.IsNullOrWhiteSpace(weaponId)) reason = "Weapon ID is empty.";
            else if (viewPrefab == null || idle == null || shoot == null || reload == null)
                reason = "A view prefab and matching Idle/Shoot/Reload clips are required.";
            else if (!Finite(roundsPerSecond) || roundsPerSecond <= 0 || !Finite(minimumShotInterval) || minimumShotInterval < 0 || !Finite(range) || range <= 0 ||
                     !Finite(reloadSeconds) || reloadSeconds <= 0 || magazineCapacity <= 0 ||
                     startingReserve < 0 || maximumReserve < startingReserve)
                reason = "Invalid fire rate, range, reload time or ammo capacities.";
            else if (!Finite(damageMultiplier) || damageMultiplier < 0 || !Finite(flatDamage) || flatDamage < 0 ||
                     !Finite(hipSpreadDegrees) || hipSpreadDegrees < 0 || hipSpreadDegrees >= 90 ||
                     !Finite(aimSpreadDegrees) || aimSpreadDegrees < 0 || aimSpreadDegrees >= 90 ||
                     !Finite(aimFieldOfView) || aimFieldOfView <= 0 || aimFieldOfView >= 179 ||
                     !Finite(poseBlendSeconds) || poseBlendSeconds < 0 ||
                     !Finite(animationBlendSeconds) || animationBlendSeconds < 0 ||
                     !Finite(recoilPitch) || !Finite(recoilYaw) || !Finite(recoilRecovery) || recoilRecovery < 0 ||
                     !Finite(impactLifetime) || impactLifetime <= 0 || !Finite(soundGain) || soundGain < 0 || soundGain > 1 ||
                     !Finite(hipPosition) || !Finite(hipEuler) || !Finite(aimPosition) || !Finite(aimEuler) ||
                     !Finite(aimEyeRelief) || aimEyeRelief < .03f || !Finite(aimSightOffset.x) || !Finite(aimSightOffset.y))
                reason = "Invalid damage, pose, spread, recoil, FOV or feedback settings.";
            return reason == null;
        }
        static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
    }
}
