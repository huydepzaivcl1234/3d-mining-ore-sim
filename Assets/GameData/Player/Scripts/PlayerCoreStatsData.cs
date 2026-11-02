using UnityEngine;

namespace GameData.Player.Script
{
    // Legacy asset type retained only for historical scene backups.
    public sealed class PlayerCoreStatsData : ScriptableObject
    {
        [Header("Automatic attack - base values before level/cards/buffs")]
        [Tooltip("Sát thương gốc mỗi viên đạn, chưa cộng level, thẻ và buff.")]
        [Min(0f)] public float baseDamage = 20f;
        [Tooltip("Tầm tự chọn mục tiêu và quãng đường đạn bay, tính bằng mét.")]
        [Min(.1f)] public float attackRange = 13f;
        [Tooltip("Số lần bắn mỗi giây. Ví dụ 2 = bắn mỗi 0.5 giây, chưa cộng buff.")]
        [Min(.1f)] public float fireRate = 1.5384615f;
        [Tooltip("Tốc độ đạn bay tính bằng mét/giây, chưa cộng thẻ tốc độ đạn.")]
        [Min(.1f)] public float projectileSpeed = 22f;

        [Header("Automatic magazine / reload")]
        [Min(1)] public int magazineCapacity=9;
        [Min(.1f)] public float reloadSeconds=1.5f;
        [Header("Health")]
        [Min(1f)] public float maxHealth = 50f;
        [Min(0f)] public float healthPerLevel;
        [Min(0f)] public float regenAmount;
        [Min(.1f)] public float regenInterval = 5f;
        [Header("Defenses - includes level growth; equipment is added at runtime")]
        [Min(0f)] public float armor;
        [Min(0f)] public float armorPerLevel;
        [Min(0f)] public float magicResistance;
        [Min(0f)] public float magicResistancePerLevel;
        [Min(.01f)] public float resistanceScale = 100f;
        [Header("Automatic attacks - multiplier on the equipped attack cadence")]
        [Min(.1f)] public float attackSpeedMultiplier = 1f;

        [Header("XP / Level - starting values; current progress is saved per player")]
        [Min(1)] public int startingLevel = 1;
        [Min(0f)] public float startingExperience;
        [Min(1f)] public float experienceRequired = 100f;
        [Min(1f)] public float experienceRequirementGrowth = 1.25f;
        [Tooltip("Flat attack damage added for each level above level 1.")]
        [Min(0f)] public float damagePerLevel = 1f;

        [Header("Stamina")]
        [Min(1f)] public float maxStamina = 100f;
        [Min(0f)] public float runningCostPerSecond = 10f;
        [Min(0f)] public float staminaRecoveryPerSecond = 8f;
        [Min(0f)] public float staminaRecoveryDelay = 1.5f;
        [Tooltip("After stamina reaches zero, recover this fraction of maximum stamina before sprinting again.")]
        [Range(0f, 1f)] public float sprintRecoveryThreshold = 0.25f;

        [Header("Movement Speed")]
        [Min(0f)] public float walkSpeed = 3f;
        [Min(0f)] public float runSpeed = 7f;

        [Header("Jump")]
        [Min(0f)] public float jumpStrength = 260f;

        private static float CombatValue(float value, float fallback, float minimum) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(minimum, value);
        private void OnValidate()
        {
            baseDamage = CombatValue(baseDamage, 20f, 0f);
            attackRange = CombatValue(attackRange, 13f, .1f);
            fireRate = CombatValue(fireRate, 1.5384615f, .1f);
            projectileSpeed = CombatValue(projectileSpeed, 22f, .1f);
            startingLevel = Mathf.Max(1, startingLevel);
            healthPerLevel = Mathf.Max(0, healthPerLevel);
            regenAmount = Mathf.Max(0, regenAmount);
            regenInterval = Mathf.Max(.1f, regenInterval);
            armor = Mathf.Max(0, armor); armorPerLevel = Mathf.Max(0, armorPerLevel);
            magicResistance = Mathf.Max(0, magicResistance); magicResistancePerLevel = Mathf.Max(0, magicResistancePerLevel);
            resistanceScale = Mathf.Max(.01f, resistanceScale);
            attackSpeedMultiplier = Mathf.Max(.1f, attackSpeedMultiplier);
            damagePerLevel = Mathf.Max(0f, damagePerLevel);
            startingExperience = Mathf.Max(0, startingExperience);
            experienceRequired = Mathf.Max(1, experienceRequired);
            experienceRequirementGrowth = Mathf.Max(1, experienceRequirementGrowth);
            maxHealth = Mathf.Max(1f, maxHealth);
            maxStamina = Mathf.Max(1f, maxStamina);
            runningCostPerSecond = Mathf.Max(0f, runningCostPerSecond);
            staminaRecoveryPerSecond = Mathf.Max(0f, staminaRecoveryPerSecond);
            staminaRecoveryDelay = Mathf.Max(0f, staminaRecoveryDelay);
            sprintRecoveryThreshold = Mathf.Clamp01(sprintRecoveryThreshold);
            walkSpeed = Mathf.Max(0f, walkSpeed);
            runSpeed = Mathf.Max(0f, runSpeed);
            jumpStrength = Mathf.Max(0f, jumpStrength);
        }
    }
}
