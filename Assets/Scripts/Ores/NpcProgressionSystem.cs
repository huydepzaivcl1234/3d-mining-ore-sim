using System;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns the shared level, experience, and mining power of all purchased NPCs.</summary>
    [DisallowMultipleComponent]
    public sealed class NpcProgressionSystem : MonoBehaviour
    {
        [SerializeField] private OreSpawner oreSpawner;
        [SerializeField] private MiningUpgradeSystem upgradeSystem;
        [SerializeField] private NpcData npcData;
        [SerializeField] private MiningItemSystem itemSystem;

        [Header("Runtime Progression")]
        [Tooltip("Shared level for every current and future mining NPC.")]
        [Min(1), SerializeField] private int currentLevel = 1;
        [Tooltip("Experience currently earned toward the next shared NPC level.")]
        [Min(0f), SerializeField] private float currentExperience;

        public int CurrentLevel => currentLevel;
        public float CurrentExperience => currentExperience;
        public int ExperienceRequired => npcData != null
            ? npcData.GetExperienceRequirement(currentLevel)
            : 1;
        public float Progress01 => currentLevel >= MaximumLevel
            ? 1f
            : Mathf.Clamp01(currentExperience / Mathf.Max(1f, ExperienceRequired));
        public int MaximumLevel => npcData != null ? npcData.MaximumLevel : 1;
        public int CurrentMiningPower
        {
            get
            {
                int basePower = npcData != null ? npcData.MiningPower : 1;
                int powerPerLevel = npcData != null ? npcData.MiningPowerPerLevel : 1;
                long result = basePower + (long)Mathf.Max(0, currentLevel - 1) * powerPerLevel;
                return (int)Math.Min(int.MaxValue, result);
            }
        }
        public float CurrentDamagePerHit
        {
            get
            {
                int baseDamage = npcData != null ? npcData.DamagePerHit : 0;
                float multiplier = upgradeSystem != null
                    ? upgradeSystem.GetMultiplier(MiningUpgradeType.OreDamage)
                    : 1f;
                float itemMultiplier = itemSystem != null ? itemSystem.NpcDamageMultiplier : 1f;
                return baseDamage * multiplier * itemMultiplier;
            }
        }
        public float CurrentMoveSpeedMultiplier
        {
            get
            {
                float upgradeMultiplier = upgradeSystem != null
                    ? upgradeSystem.GetMultiplier(MiningUpgradeType.NpcMoveSpeed)
                    : 1f;
                float itemMultiplier = itemSystem != null ? itemSystem.NpcMoveSpeedMultiplier : 1f;
                return upgradeMultiplier * itemMultiplier;
            }
        }
        public event Action ProgressionChanged;
        public event Action<int> LevelChanged;

        private void Awake()
        {
            if (itemSystem == null)
            {
                itemSystem = FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
            }
            ValidateProgression();
        }

        private void OnEnable()
        {
            if (oreSpawner != null)
            {
                oreSpawner.OreRewardGranted -= HandleOreDepleted;
                oreSpawner.OreRewardGranted += HandleOreDepleted;
            }
            if (upgradeSystem != null)
            {
                upgradeSystem.UpgradesChanged -= HandleUpgradesChanged;
                upgradeSystem.UpgradesChanged += HandleUpgradesChanged;
            }
            if (itemSystem != null)
            {
                itemSystem.EffectsChanged -= HandleItemEffectsChanged;
                itemSystem.EffectsChanged += HandleItemEffectsChanged;
            }

            ProgressionChanged?.Invoke();
        }

        private void OnDisable()
        {
            if (oreSpawner != null)
            {
                oreSpawner.OreRewardGranted -= HandleOreDepleted;
            }
            if (upgradeSystem != null)
            {
                upgradeSystem.UpgradesChanged -= HandleUpgradesChanged;
            }
            if (itemSystem != null)
            {
                itemSystem.EffectsChanged -= HandleItemEffectsChanged;
            }
        }

        public void AddExperience(float baseExperience)
        {
            if (baseExperience <= 0f || currentLevel >= MaximumLevel)
            {
                return;
            }

            float multiplier = upgradeSystem != null
                ? upgradeSystem.GetMultiplier(MiningUpgradeType.NpcExperience) *
                  upgradeSystem.PermanentExperienceMultiplier
                : 1f;
            currentExperience += baseExperience * Mathf.Max(1f, multiplier);

            while (currentLevel < MaximumLevel && currentExperience >= ExperienceRequired)
            {
                currentExperience -= ExperienceRequired;
                currentLevel++;
                LevelChanged?.Invoke(currentLevel);
            }

            if (currentLevel >= MaximumLevel)
            {
                currentExperience = ExperienceRequired;
            }

            ProgressionChanged?.Invoke();
        }

        public void ResetProgression()
        {
            currentLevel = 1;
            currentExperience = 0f;
            LevelChanged?.Invoke(currentLevel);
            ProgressionChanged?.Invoke();
        }

        private void HandleOreDepleted(Ore ore, float moneyReward)
        {
            if (ore != null && ore.Data != null)
            {
                AddExperience(ore.Data.ExperienceReward);
            }
        }

        private void HandleUpgradesChanged()
        {
            ProgressionChanged?.Invoke();
        }

        private void HandleItemEffectsChanged()
        {
            ProgressionChanged?.Invoke();
        }

        private void OnValidate()
        {
            ValidateProgression();
            if (Application.isPlaying)
            {
                ProgressionChanged?.Invoke();
            }
        }

        private void ValidateProgression()
        {
            currentLevel = Mathf.Clamp(currentLevel, 1, MaximumLevel);
            currentExperience = Mathf.Max(0f, currentExperience);
            if (currentLevel >= MaximumLevel)
            {
                currentExperience = ExperienceRequired;
            }
            else
            {
                currentExperience = Mathf.Min(currentExperience,
                    Mathf.Max(0f, ExperienceRequired - Mathf.Epsilon));
            }
        }
    }
}