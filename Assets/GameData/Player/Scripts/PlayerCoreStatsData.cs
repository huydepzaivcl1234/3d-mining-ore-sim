using UnityEngine;

namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName = "Chest Defense/Player Core Stats", fileName = "PlayerCoreStatsData")]
    public sealed class PlayerCoreStatsData : ScriptableObject
    {
        [Header("Health")]
        [Min(1f)] public float maxHealth = 50f;

        [Header("XP / Level - starting values; current progress is saved per player")]
        [Min(1)] public int startingLevel = 1;
        [Min(0f)] public float startingExperience;
        [Min(1f)] public float experienceRequired = 100f;
        [Min(1f)] public float experienceRequirementGrowth = 1.25f;
        [Tooltip("Flat gun damage added for each level above level 1.")]
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

        private void OnValidate()
        {
            startingLevel = Mathf.Max(1, startingLevel);
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
