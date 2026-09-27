using UnityEngine;

namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName = "Mining Simulator/Player Stats", fileName = "PlayerStatsData")]
    public sealed class MiningPlayerStatsData : ScriptableObject
    {
        [Header("Player progression - separate from mining NPC level")]
        [Min(1)] public int startingLevel = 1;
        [Min(0)] public float startingExperience;
        [Min(1)] public float experienceRequired = 100;
        [Min(1)] public float experienceRequirementGrowth = 1.25f;
        [Min(0)] public float healthPerLevel = 10;
        [Min(0)] public float damagePerLevel = 1;
        [Header("Stamina")]
        [Min(1)] public float maxStamina = 100;
        [Min(0)] public float sprintStaminaPerSecond = 15;
        [Min(0)] public float staminaRegenAmount = 10;
        [Min(0.1f)] public float staminaRegenInterval = 1;
        [Tooltip("Stamina required to resume sprinting after exhaustion.")]
        [Min(0)] public float staminaResumeThreshold = 20;
        public AudioClip exhaustedBreathing;
        [Range(0, 1)] public float breathingVolume = 0.5f;
        [Min(1)] public float breathingInterval = 3;
        [Range(0, 1)] public float lowStaminaFraction = 0.2f;
        public Color lowStaminaTextColor = Color.red;
        [Min(0.1f)] public float lowStaminaBlinkSeconds = 0.7f;
        [Header("Player death sound")]
        public AudioClip deathSfx;
        [Range(0, 1)] public float deathSfxVolume = 0.7f;
        [Header("Health")]
        [Min(1)] public float maxHealth = 100;
        [Min(0)] public float regenAmount = 1;
        [Min(0.1f)] public float regenInterval = 10;
        [Min(1)] public float respawnSeconds = 10;
        [Header("Combat")]
        [Min(0)] public float damage = 1;
        [Min(0.1f)] public float attackRange = 1.75f;
        [Range(1, 180)] public float attackAngle = 100;
        [Tooltip("Animation playback multiplier, not attacks per second.")]
        [Min(0.1f)] public float attackSpeed = 1;
        [Min(0.01f)] public float combatBlendSeconds = 0.15f;
        [Range(0, 1)] public float hitTime = 0.45f;
        public Vector3 hitOriginOffset = new Vector3(0, 1, 0);
        [Header("Movement")]
        [Min(0)] public float MoveSpeed = 2;
        [Min(0)] public float SprintSpeed = 5.335f;
        [Min(0.001f)] public float RotationSmoothTime = 0.12f;
        [Min(0)] public float SpeedChangeRate = 10;
        [Min(0)] public float JumpHeight = 1.2f;
        public float Gravity = -15;
        [Min(0)] public float JumpTimeout = 0.5f;
        [Min(0)] public float FallTimeout = 0.15f;
        private void OnValidate()
        {
            startingLevel = Mathf.Max(1, startingLevel);
            experienceRequired = Mathf.Max(1, experienceRequired);
            experienceRequirementGrowth = Mathf.Max(1, experienceRequirementGrowth);
            startingExperience = Mathf.Clamp(startingExperience, 0, experienceRequired);
            maxHealth = Mathf.Max(1, maxHealth);
            regenAmount = Mathf.Max(0, regenAmount);
            regenInterval = Mathf.Max(0.1f, regenInterval);
            respawnSeconds = Mathf.Max(1, respawnSeconds);
            damage = Mathf.Max(0, damage);
            attackRange = Mathf.Max(0.1f, attackRange);
            attackAngle = Mathf.Clamp(attackAngle, 1, 180);
            attackSpeed = Mathf.Max(0.1f, attackSpeed);
            combatBlendSeconds = Mathf.Max(0.01f, combatBlendSeconds);
            hitTime = Mathf.Clamp01(hitTime);
            MoveSpeed = Mathf.Max(0, MoveSpeed);
            SprintSpeed = Mathf.Max(0, SprintSpeed);
            RotationSmoothTime = Mathf.Max(0.001f, RotationSmoothTime);
            SpeedChangeRate = Mathf.Max(0, SpeedChangeRate);
            JumpHeight = Mathf.Max(0, JumpHeight);
            JumpTimeout = Mathf.Max(0, JumpTimeout);
            FallTimeout = Mathf.Max(0, FallTimeout);
        }
    }
}
