using StarterAssets;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [DefaultExecutionOrder(-1000), DisallowMultipleComponent]
    public sealed class MiningPlayerStats : MonoBehaviour
    {
        [SerializeField] private MiningPlayerStatsData data;
        public MiningPlayerStatsData Data => data;
        private RuneUpgradeProgress runeUpgrades;
        public RuneUpgradeProgress RuneUpgrades => runeUpgrades ??= new RuneUpgradeProgress(
            Resources.Load<RuneUpgradeData>("RuneUpgradeData"),
            Application.isPlaying ? PlayerPrefs.GetString(RuneUpgradeProgress.SaveKey, "") : null);
        public float Armor => DefenseAtLevel(data != null ? data.armor : 0f,
            data != null ? data.armorPerLevel : 0f) + RuneUpgrades.Bonus(RuneStat.Armor);
        public float MagicResistance => DefenseAtLevel(data != null ? data.magicResistance : 0f,
            data != null ? data.magicResistancePerLevel : 0f) + RuneUpgrades.Bonus(RuneStat.MagicResistance);
        private float DefenseAtLevel(float baseline, float growth) => (float)System.Math.Min(float.MaxValue,
            (double)CombatDamage.NonNegative(baseline) + (double)CombatDamage.NonNegative(growth) * (Level - 1));
        private MiningItemSystem itemEffects;
        private float DamagePotionMultiplier => itemEffects != null ? itemEffects.PlayerDamageMultiplier : 1f;
        private float AttackSpeedPotionMultiplier => itemEffects != null ? itemEffects.PlayerAttackSpeedMultiplier : 1f;
        public float MaxHealth => ((data != null ? Mathf.Max(1, data.maxHealth + Mathf.Max(0, data.healthPerLevel) * (Level - 1)) : 100) * (1f + CardHealthPercent * .01f) + CardHealth) * (1f + RuneUpgrades.Bonus(RuneStat.Health) * .01f);
        public float Damage => ((data != null ? Mathf.Max(0, data.damage + Mathf.Max(0, data.damagePerLevel) * (Level - 1)) : 1) * (1f + CardDamagePercent * .01f) + CardDamage) * DamagePotionMultiplier * (1f + RuneUpgrades.Bonus(RuneStat.Damage) * .01f);
        public float AttackSpeed => Mathf.Max(.1f, ((data != null ? data.attackSpeed : 1f) * (1f + CardAttackSpeedPercent * .01f) + CardAttackSpeed) * AttackSpeedPotionMultiplier * (1f + RuneUpgrades.Bonus(RuneStat.AttackSpeed) * .01f));
        // Retain old flat bonuses for v2 saves; new cards add percentage points.
        public float CardDamagePercent { get; private set; }
        public float CardHealthPercent { get; private set; }
        public float CardAttackSpeedPercent { get; private set; }
        public float CardRegenReductionPercent { get; private set; }
        public float CardHealingPercent { get; private set; }
        public float CardRegenIntervalMultiplier => Mathf.Max(0f, 1f - CardRegenReductionPercent * .01f);
        public float CardDamage { get; private set; }
        public float CardHealth { get; private set; }
        public float CardAttackSpeed { get; private set; }
        public bool AddCardBonus(MiningCardChoice choice, float amount)
        {
            if (amount < 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return false;
            float current = choice switch
            {
                MiningCardChoice.Damage => CardDamagePercent,
                MiningCardChoice.AttackSpeed => CardAttackSpeedPercent,
                MiningCardChoice.Health => CardHealthPercent,
                MiningCardChoice.RegenInterval => CardRegenReductionPercent,
                MiningCardChoice.HealingEffectiveness => CardHealingPercent,
                _ => 0f
            };
            if (float.IsInfinity(current + amount)) return false;
            switch (choice)
            {
                case MiningCardChoice.Damage: CardDamagePercent += amount; break;
                case MiningCardChoice.AttackSpeed: CardAttackSpeedPercent += amount; break;
                case MiningCardChoice.Health: CardHealthPercent += amount; break;
                case MiningCardChoice.RegenInterval:
                    CardRegenReductionPercent = Mathf.Min(100f, current + amount);
                    break;
                case MiningCardChoice.HealingEffectiveness: CardHealingPercent += amount; break;
                default: return false;
            }
            dirty = true;
            SaveProgress();
            return true;
        }
        private readonly ExperienceProgression progression = new ExperienceProgression();
        public int Level => progression.Level;
        public float Experience => progression.Experience;
        public float ExperienceRequired => progression.Required;
        public float ExperienceProgress => Mathf.Clamp01(Experience / Mathf.Max(1, ExperienceRequired));
        private ThirdPersonController movement;
        private MiningAudioManager feedbackAudio;
        private const string ProgressSaveKey = "MiningSimulator.PlayerProgress.v1";
        private bool dirty, saveBlocked;
        private float nextSave;
        [System.Serializable]
        private sealed class SavedProgress
        {
            public int version = 4;
            public float cardRegenReductionPercent, cardHealingPercent;
            public float cardDamage, cardHealth, cardAttackSpeed;
            public float cardDamagePercent, cardHealthPercent, cardAttackSpeedPercent;
            public int level;
            public float experience;
            public float requiredExperience;
        }
        private void Awake()
        {
            movement = GetComponent<ThirdPersonController>();
            itemEffects = FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
            if (movement != null && GetComponent<PlayerMonsterHeadDeflection>() == null)
                gameObject.AddComponent<PlayerMonsterHeadDeflection>();
            feedbackAudio = FindFirstObjectByType<MiningAudioManager>();
            if (data != null) SetProgress(data.startingLevel, data.startingExperience, data.experienceRequired);
            LoadProgress();
            dirty = false;
            ApplyMovement();
        }
        public void AddExperience(float amount)
        {
            if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            amount *= MiningGameplayTuning.Current != null ? MiningGameplayTuning.Current.PlayerXpMultiplier : 1f;
            // Central player XP boundary: the potion applies once to every XP source.
            amount *= itemEffects != null ? itemEffects.PlayerExperienceMultiplier : 1f;
            if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            float growth = data != null ? Mathf.Max(1, data.experienceRequirementGrowth) : 1.25f;
            progression.Add(amount, growth);
            dirty = true;
        }
        public void SetProgress(int level, float experience, float requiredExperience)
        {
            progression.Set(level, experience, requiredExperience);
            dirty = true;
        }
        private void LoadProgress()
        {
            if (!Application.isPlaying || !PlayerPrefs.HasKey(ProgressSaveKey)) return;
            try
            {
                var saved = JsonUtility.FromJson<SavedProgress>(PlayerPrefs.GetString(ProgressSaveKey));
                if (saved == null || (saved.version < 1 || saved.version > 4) || saved.level < 1 ||
                    float.IsNaN(saved.experience) || float.IsInfinity(saved.experience) || saved.experience < 0 ||
                    float.IsNaN(saved.requiredExperience) || float.IsInfinity(saved.requiredExperience) || saved.requiredExperience < 1)
                {
                    saveBlocked = true;
                    Debug.LogWarning("Player progression save is invalid or from an unsupported version. It is preserved until Reset Data.", this);
                    return;
                }
                SetProgress(saved.level, saved.experience, saved.requiredExperience);
                CardDamage = SafeBonus(saved.cardDamage);
                CardHealth = SafeBonus(saved.cardHealth);
                CardAttackSpeed = SafeBonus(saved.cardAttackSpeed);
                CardDamagePercent = SafeBonus(saved.cardDamagePercent);
                CardHealthPercent = SafeBonus(saved.cardHealthPercent);
                CardAttackSpeedPercent = SafeBonus(saved.cardAttackSpeedPercent);
                CardRegenReductionPercent = Mathf.Min(100f, SafeBonus(saved.cardRegenReductionPercent));
                CardHealingPercent = SafeBonus(saved.cardHealingPercent);
            }
            catch (System.ArgumentException)
            {
                saveBlocked = true;
                Debug.LogWarning("Cannot read player progression. Existing save is preserved until Reset Data.", this);
            }
        }
        private void SaveProgress()
        {
            if (!Application.isPlaying || !dirty || saveBlocked || data == null) return;
            PlayerPrefs.SetString(ProgressSaveKey, JsonUtility.ToJson(new SavedProgress
            { level = Level, experience = Experience, requiredExperience = ExperienceRequired,
                cardDamage = CardDamage, cardHealth = CardHealth, cardAttackSpeed = CardAttackSpeed,
                cardDamagePercent = CardDamagePercent, cardHealthPercent = CardHealthPercent,
                cardAttackSpeedPercent = CardAttackSpeedPercent,
                cardRegenReductionPercent = CardRegenReductionPercent, cardHealingPercent = CardHealingPercent }));
            PlayerPrefs.SetInt("MiningSimulator.SaveExists.v1", 1);
            PlayerPrefs.Save();
            dirty = false;
        }
        public static void ResetSavedProgress()
        {
            PlayerPrefs.DeleteKey(ProgressSaveKey);
            PlayerPrefs.DeleteKey(RuneUpgradeProgress.SaveKey);
            foreach (var player in FindObjectsByType<MiningPlayerStats>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var defaults = player.Data;
                player.RuneUpgrades.Reset();
                player.CardDamage = player.CardHealth = player.CardAttackSpeed = 0f;
                player.CardDamagePercent = player.CardHealthPercent = player.CardAttackSpeedPercent = 0f;
                player.CardRegenReductionPercent = player.CardHealingPercent = 0f;
                player.SetProgress(defaults != null ? defaults.startingLevel : 1,
                    defaults != null ? defaults.startingExperience : 0,
                    defaults != null ? defaults.experienceRequired : 100);
                player.dirty = false; // Do not recreate a deleted save on scene unload.
                player.saveBlocked = false;
            }
            PlayerPrefs.Save();
        }
        private void OnApplicationPause(bool paused) { if (paused) SaveProgress(); }
        private static float SafeBonus(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
        private void OnApplicationQuit() => SaveProgress();
        private void OnEnable()
        {
            if (movement != null) movement.Jumped += PlayJumpSound;
        }
        private void OnDisable()
        {
            if (movement != null) movement.Jumped -= PlayJumpSound;
            SaveProgress();
        }
        private void PlayJumpSound()
        {
            if (data != null && feedbackAudio != null)
                feedbackAudio.PlayWorldSfx(data.jumpSfx, transform.position, data.jumpSfxVolume);
        }
        // Movement's public fields are compatibility inputs, not a second authoring source.
        private void Update()
        {
            ApplyMovement();
            if (dirty && Time.unscaledTime >= nextSave)
            {
                nextSave = Time.unscaledTime + 1;
                SaveProgress();
            }
        }
        private void ApplyMovement()
        {
            if (data == null || movement == null) return;
            movement.MoveSpeed = Mathf.Max(0, data.MoveSpeed);
            movement.SprintSpeed = Mathf.Max(0, data.SprintSpeed);
            movement.RotationSmoothTime = Mathf.Max(0.001f, data.RotationSmoothTime);
            movement.SpeedChangeRate = Mathf.Max(0, data.SpeedChangeRate);
            movement.JumpHeight = Mathf.Max(0, data.JumpHeight);
            movement.Gravity = data.Gravity;
            movement.JumpTimeout = Mathf.Max(0, data.JumpTimeout);
            movement.FallTimeout = Mathf.Max(0, data.FallTimeout);
        }
        public static MiningPlayerStatsData For(Component owner)
        {
            var stats = owner.GetComponent<MiningPlayerStats>();
            return stats != null ? stats.Data : null;
        }
    }
}
