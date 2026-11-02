using UnityEngine;

namespace MiningSimulator.Ores
{
    [DefaultExecutionOrder(-1000), DisallowMultipleComponent]
    public sealed class MiningPlayerStats : MonoBehaviour
    {
        [SerializeField, HideInInspector] private MiningPlayerStatsData data;
        [SerializeField] private GameData.Player.Script.PlayerCoreStatsData coreData;
        public MiningPlayerStatsData Data => data;
        public GameData.Player.Script.PlayerCoreStatsData CoreData => coreData;
        private RuneUpgradeProgress runeUpgrades;
        public RuneUpgradeProgress RuneUpgrades => runeUpgrades ??= new RuneUpgradeProgress(
            Resources.Load<RuneUpgradeData>("RuneUpgradeData"),
            Application.isPlaying ? GameSave.GetString(RuneUpgradeProgress.SaveKey, "") : null);
        public float Armor => DefenseAtLevel(data != null ? data.armor : 0f,
            data != null ? data.armorPerLevel : 0f) + RuneUpgrades.Bonus(RuneStat.Armor);
        public float MagicResistance => DefenseAtLevel(data != null ? data.magicResistance : 0f,
            data != null ? data.magicResistancePerLevel : 0f) + RuneUpgrades.Bonus(RuneStat.MagicResistance);
        private float DefenseAtLevel(float baseline, float growth) => (float)System.Math.Min(float.MaxValue,
            (double)CombatDamage.NonNegative(baseline) + (double)CombatDamage.NonNegative(growth) * (Level - 1));
        private MiningItemSystem itemEffects;
        private GameData.Player.Script.GunSpecialUpgrades gunSpecials;
        private float DamagePotionMultiplier => itemEffects != null ? itemEffects.PlayerDamageMultiplier : 1f;
        private float AttackSpeedPotionMultiplier => itemEffects != null ? itemEffects.PlayerAttackSpeedMultiplier : 1f;
        public float MaxHealth => ((coreData != null ? Mathf.Max(1f, coreData.maxHealth) :
            data != null ? Mathf.Max(1, data.maxHealth + Mathf.Max(0, data.healthPerLevel) * (Level - 1)) : 100) * (1f + CardHealthPercent * .01f) + CardHealth) * (1f + RuneUpgrades.Bonus(RuneStat.Health) * .01f) * (itemEffects != null ? itemEffects.PlayerHealthMultiplier : 1f) + (gunSpecials?gunSpecials.HunterBonusHealth:0);
        public float ConsumableDamageReduction => itemEffects != null ? itemEffects.PlayerDamageReduction : 0f;
        public float ConsumableMonsterTrueDamageFraction => itemEffects != null ? itemEffects.MonsterTrueDamageFraction : 0f;
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
        public float GunCardDamagePercent { get; private set; }
        public int GunCardMagazineBonus { get; private set; }
        public float GunCardBulletSpeedPercent { get; private set; }
        public float GunDamageMultiplier => 1f + GunCardDamagePercent * .01f;
        public float GunLevelDamageBonus => Mathf.Max(0, Level - 1) * (CoreData != null ? Mathf.Max(0f, CoreData.damagePerLevel) : 0f);
        public float CalculateGunDamage(float baseDamage) => (Mathf.Max(0f, baseDamage) + GunLevelDamageBonus) * GunDamageMultiplier;
        public float GunBulletSpeedMultiplier => 1f + GunCardBulletSpeedPercent * .01f;
        public bool AddCardBonus(MiningCardChoice choice, float amount)
        {
            if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return false;
            float current = choice switch
            {
                MiningCardChoice.GunDamage => GunCardDamagePercent,
                MiningCardChoice.BulletSpeed => GunCardBulletSpeedPercent,
                _ => 0f
            };
            if (float.IsInfinity(current + amount)) return false;
            switch (choice)
            {
                case MiningCardChoice.GunDamage: GunCardDamagePercent += amount; break;
                case MiningCardChoice.BulletSpeed: GunCardBulletSpeedPercent += amount; break;
                case MiningCardChoice.Magazine:
                    if (amount != Mathf.Floor(amount) || amount > int.MaxValue - (double)GunCardMagazineBonus) return false;
                    GunCardMagazineBonus += (int)amount; break;
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
        private const string ProgressSaveKey = "MiningSimulator.PlayerProgress.v1";
        private bool dirty, saveBlocked;
        private float nextSave;
        [System.Serializable]
        private sealed class SavedProgress
        {
            public int version = 5;
            public float gunCardDamagePercent, gunCardBulletSpeedPercent;
            public int gunCardMagazineBonus;
            public float cardRegenReductionPercent, cardHealingPercent;
            public float cardDamage, cardHealth, cardAttackSpeed;
            public float cardDamagePercent, cardHealthPercent, cardAttackSpeedPercent;
            public int level;
            public float experience;
            public float requiredExperience;
        }
        private void Awake()
        {
            gunSpecials=GetComponent<GameData.Player.Script.GunSpecialUpgrades>();
            itemEffects = FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
            if (coreData != null) SetProgress(coreData.startingLevel, coreData.startingExperience, coreData.experienceRequired);
            else if (data != null) SetProgress(data.startingLevel, data.startingExperience, data.experienceRequired);
            LoadProgress();
            dirty = false;
        }
        public void AddExperience(float amount)
        {
            if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            amount *= MiningGameplayTuning.Current != null ? MiningGameplayTuning.Current.PlayerXpMultiplier : 1f;
            // Central player XP boundary: the potion applies once to every XP source.
            amount *= itemEffects != null ? itemEffects.PlayerExperienceMultiplier : 1f;
            if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            float growth = coreData != null ? Mathf.Max(1, coreData.experienceRequirementGrowth) : data != null ? Mathf.Max(1, data.experienceRequirementGrowth) : 1.25f;
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
            if (!Application.isPlaying || !GameSave.HasKey(ProgressSaveKey)) return;
            try
            {
                var saved = JsonUtility.FromJson<SavedProgress>(GameSave.GetString(ProgressSaveKey));
                if (saved == null || (saved.version < 1 || saved.version > 5) || saved.level < 1 ||
                    float.IsNaN(saved.experience) || float.IsInfinity(saved.experience) || saved.experience < 0 ||
                    float.IsNaN(saved.requiredExperience) || float.IsInfinity(saved.requiredExperience) || saved.requiredExperience < 1)
                {
                    saveBlocked = true;
                    Debug.LogWarning("Player progression save is invalid or from an unsupported version. It is preserved until Reset Data.", this);
                    return;
                }
                SetProgress(saved.level, saved.experience, saved.requiredExperience);
                // Retired card effects no longer affect player health, melee, regen or attack speed.
                CardDamage = CardHealth = CardAttackSpeed = 0f;
                CardDamagePercent = CardHealthPercent = CardAttackSpeedPercent = 0f;
                CardRegenReductionPercent = CardHealingPercent = 0f;
                GunCardDamagePercent = SafeBonus(saved.gunCardDamagePercent);
                GunCardBulletSpeedPercent = SafeBonus(saved.gunCardBulletSpeedPercent);
                GunCardMagazineBonus = Mathf.Max(0, saved.gunCardMagazineBonus);
            }
            catch (System.ArgumentException)
            {
                saveBlocked = true;
                Debug.LogWarning("Cannot read player progression. Existing save is preserved until Reset Data.", this);
            }
        }
        private void SaveProgress()
        {
            if (!Application.isPlaying || !dirty || saveBlocked || (data == null && coreData == null)) return;
            GameSave.SetString(ProgressSaveKey, JsonUtility.ToJson(new SavedProgress
            { level = Level, experience = Experience, requiredExperience = ExperienceRequired,
                cardDamage = CardDamage, cardHealth = CardHealth, cardAttackSpeed = CardAttackSpeed,
                cardDamagePercent = CardDamagePercent, cardHealthPercent = CardHealthPercent,
                cardAttackSpeedPercent = CardAttackSpeedPercent,
                cardRegenReductionPercent = CardRegenReductionPercent, cardHealingPercent = CardHealingPercent,
                gunCardDamagePercent = GunCardDamagePercent, gunCardMagazineBonus = GunCardMagazineBonus,
                gunCardBulletSpeedPercent = GunCardBulletSpeedPercent }));
            GameSave.SetInt("MiningSimulator.SaveExists.v1", 1);
            GameSave.Save();
            dirty = false;
        }
        public static void ResetSavedProgress()
        {
            GameSave.DeleteKey(ProgressSaveKey);
            GameSave.DeleteKey(RuneUpgradeProgress.SaveKey);
            foreach (var player in FindObjectsByType<MiningPlayerStats>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var defaults = player.Data;
                player.RuneUpgrades.Reset();
                player.CardDamage = player.CardHealth = player.CardAttackSpeed = 0f;
                player.CardDamagePercent = player.CardHealthPercent = player.CardAttackSpeedPercent = 0f;
                player.CardRegenReductionPercent = player.CardHealingPercent = 0f;
                player.GunCardDamagePercent = player.GunCardBulletSpeedPercent = 0f;
                player.GunCardMagazineBonus = 0;
                player.SetProgress(player.coreData != null ? player.coreData.startingLevel : defaults != null ? defaults.startingLevel : 1,
                    player.coreData != null ? player.coreData.startingExperience : defaults != null ? defaults.startingExperience : 0,
                    player.coreData != null ? player.coreData.experienceRequired : defaults != null ? defaults.experienceRequired : 100);
                player.dirty = false; // Do not recreate a deleted save on scene unload.
                player.saveBlocked = false;
            }
            GameSave.Save();
        }
        private void OnApplicationPause(bool paused) { if (paused) SaveProgress(); }
        private static float SafeBonus(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
        private void OnApplicationQuit() => SaveProgress();
        private void OnDisable()
        {
            SaveProgress();
        }
        private void Update()
        {
            if (dirty && Time.unscaledTime >= nextSave)
            {
                nextSave = Time.unscaledTime + 1;
                SaveProgress();
            }
        }
        public static MiningPlayerStatsData For(Component owner)
        {
            var stats = owner.GetComponent<MiningPlayerStats>();
            return stats != null ? stats.Data : null;
        }
    }
}
