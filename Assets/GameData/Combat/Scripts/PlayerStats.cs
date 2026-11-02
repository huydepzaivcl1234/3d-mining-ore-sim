using UnityEngine;
using GameData.Player.Script;

namespace MiningSimulator.Ores
{
    [DefaultExecutionOrder(-1000), DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "MiningSimulator.Ores", "Assembly-CSharp", "MiningPlayerStats")]
    public sealed class PlayerStats : MonoBehaviour
    {
        [SerializeField, HideInInspector] private PlayerStatsData data;
        [SerializeField] private GameData.Player.Script.SurvivorCombatData coreData;
        public PlayerStatsData Data => data;
        public GameData.Player.Script.SurvivorCombatData CoreData => coreData;
        public SurvivorAbilityData AbilityData=>coreData&&coreData.abilities?coreData.abilities:SurvivorAbilityData.Fallback;
        public float CardAttractionRadius=>UpgradeLevel(SurvivorUpgradeId.CardMagnet)>0?Mathf.Max(0,AbilityData.common.cardMagnet.radius.At(UpgradeLevel(SurvivorUpgradeId.CardMagnet))):0;
        private int[] upgradeLevels=new int[(int)SurvivorUpgradeId.Count];
        private float[] upgradeTotals=new float[(int)SurvivorUpgradeId.Count];
        private float legacyPhysicalPercent,legacyArmorFlat,legacyMagicFlat,legacyRegenFlat;
        private void EnsureUpgradeCapacity(){if(upgradeLevels.Length!=(int)SurvivorUpgradeId.Count)System.Array.Resize(ref upgradeLevels,(int)SurvivorUpgradeId.Count);if(upgradeTotals.Length!=(int)SurvivorUpgradeId.Count)System.Array.Resize(ref upgradeTotals,(int)SurvivorUpgradeId.Count);}
        public int UpgradeLevel(SurvivorUpgradeId id)=>(uint)id<(uint)upgradeLevels.Length?upgradeLevels[(int)id]:0;
        public float UpgradeBonus(SurvivorUpgradeId id)=>(uint)id<(uint)upgradeTotals.Length?upgradeTotals[(int)id]:0;
        public bool ApplySurvivorUpgrade(SurvivorUpgradeId id,float amount)
        {
            EnsureUpgradeCapacity();
            if(saveBlocked||(uint)id>=(uint)SurvivorUpgradeId.Count||UpgradeLevel(id)>=SurvivorUpgradeData.LevelCap||amount<=0||float.IsNaN(amount)||float.IsInfinity(amount))return false;
            float total=UpgradeBonus(id)+amount;if(float.IsInfinity(total))return false;
            upgradeLevels[(int)id]++;upgradeTotals[(int)id]=total;dirty=true;ApplyBodySize();SaveProgress();return true;
        }
        private Vector3 authoredBodyScale;
        private float appliedBodySize=1;
        private bool HasTitan=>UpgradeLevel(SurvivorUpgradeId.TitanBlessing)>0;
        public float TitanFlatHealth=>HasTitan?AbilityData.epic.titanBlessing.flatHealth.At(UpgradeLevel(SurvivorUpgradeId.TitanBlessing)):0;
        public float BodySizeMultiplier=>1+(HasTitan?AbilityData.epic.titanBlessing.sizePercent*.01f:0);
        private void ApplyBodySize(){float size=BodySizeMultiplier;if(Mathf.Approximately(size,appliedBodySize))return;transform.localScale=authoredBodyScale*size;appliedBodySize=size;}
        public float MovementMultiplier=>1f+UpgradeBonus(SurvivorUpgradeId.MoveSpeed)*.01f;
        public float WalkSpeed=>(coreData?coreData.walkSpeed:3f)*MovementMultiplier+(itemEffects?Mathf.Max(0,itemEffects.PlayerMoveSpeedBonus):0);
        public float RunSpeed=>WalkSpeed+3f;
        public int BossesDefeated {get;private set;}
        public float BossBonusHealth {get;private set;}
        public void RecordBossKill(){BossesDefeated++;int lv=UpgradeLevel(SurvivorUpgradeId.BossVitality);var rule=AbilityData.rare.bossVitality;if(lv>0&&BossesDefeated%Mathf.Max(1,rule.killsPerReward)==0)BossBonusHealth+=Mathf.Max(0,rule.maxHealthReward.At(lv));dirty=true;SaveProgress();}
        public float BonusRunSpeed=>Mathf.Max(0,WalkSpeed-(coreData?coreData.walkSpeed:3f));
        private float UpgradeDamage(float physical,float extraPhysicalDamage=0)
        {
            float basePhysical=coreData?coreData.damage:physical;
            physical=physical*(1+legacyPhysicalPercent*.01f)+UpgradeBonus(SurvivorUpgradeId.PhysicalDamage)+extraPhysicalDamage;
            int fleet=UpgradeLevel(SurvivorUpgradeId.FleetForce),power=UpgradeLevel(SurvivorUpgradeId.PowerSurge),flat=UpgradeLevel(SurvivorUpgradeId.FlatPower);
            float extra=(fleet>0?Mathf.Floor(BonusRunSpeed/Mathf.Max(.01f,AbilityData.common.momentum.bonusSpeedPerStack))*AbilityData.common.momentum.damagePerStack.At(fleet):0)+(flat>0?AbilityData.uncommon.flatPower.bonus.At(flat):0);
            if(power>0)extra+=AbilityData.common.powerSurge.flatDamage.At(power)+AbilityData.common.powerSurge.bonusAttackDamagePercent*.01f*Mathf.Max(0,physical-basePhysical+extra);
            return (physical+extra)*(1+(HasTitan?AbilityData.epic.titanBlessing.physicalDamagePercent*.01f:0));
        }
        private RuneUpgradeProgress runeUpgrades;
        public RuneUpgradeProgress RuneUpgrades => runeUpgrades ??= new RuneUpgradeProgress(
            Resources.Load<RuneUpgradeData>("RuneUpgradeData"),
            Application.isPlaying ? GameSave.GetString(RuneUpgradeProgress.SaveKey, "") : null);
        public float Armor => (DefenseAtLevel(coreData != null ? coreData.armor : data != null ? data.armor : 0f,
            coreData != null ? coreData.armorPerLevel : data != null ? data.armorPerLevel : 0f) + RuneUpgrades.Bonus(RuneStat.Armor)+legacyArmorFlat)*(1+UpgradeBonus(SurvivorUpgradeId.Armor)*.01f);
        public float MagicResistance => (DefenseAtLevel(coreData != null ? coreData.magicResistance : data != null ? data.magicResistance : 0f,
            coreData != null ? coreData.magicResistancePerLevel : data != null ? data.magicResistancePerLevel : 0f) + RuneUpgrades.Bonus(RuneStat.MagicResistance)+legacyMagicFlat)*(1+UpgradeBonus(SurvivorUpgradeId.MagicResistance)*.01f);
        private float DefenseAtLevel(float baseline, float growth) => (float)System.Math.Min(float.MaxValue,
            (double)CombatDamage.NonNegative(baseline) + (double)CombatDamage.NonNegative(growth) * (Level - 1));
        private MiningItemSystem itemEffects;
        private GameData.Player.Script.GunSpecialUpgrades gunSpecials;
        private float DamagePotionMultiplier => itemEffects != null ? itemEffects.PlayerDamageMultiplier : 1f;
        private float AttackSpeedPotionMultiplier => itemEffects != null ? itemEffects.PlayerAttackSpeedMultiplier : 1f;
        public float MaxHealth=>CalculateMaxHealth();
        public float CalculateMaxHealth(float addedPercent=0) => ((((coreData != null ? Mathf.Max(1f, coreData.maxHealth) :
            data != null ? Mathf.Max(1, data.maxHealth + Mathf.Max(0, data.healthPerLevel) * (Level - 1)) : 100) * (1f + CardHealthPercent * .01f) + CardHealth) * (1f + RuneUpgrades.Bonus(RuneStat.Health) * .01f) * (itemEffects != null ? itemEffects.PlayerHealthMultiplier : 1f) + (coreData==null&&gunSpecials?gunSpecials.HunterBonusHealth:0))*(1f+(addedPercent+UpgradeBonus(SurvivorUpgradeId.MaxHealth))*.01f)+BossBonusHealth)*(1+(HasTitan?AbilityData.epic.titanBlessing.maxHealthPercent*.01f:0))+TitanFlatHealth;
        public float ResistanceScale => Mathf.Max(.01f, coreData != null ? coreData.resistanceScale : data != null ? data.resistanceScale : 100f);
        public float RegenAmount => (Mathf.Max(0, coreData != null ? coreData.regenAmount : data != null ? data.regenAmount : 0f)+legacyRegenFlat)*(1+UpgradeBonus(SurvivorUpgradeId.Regeneration)*.01f);
        public float RegenInterval => Mathf.Max(.1f, coreData != null ? coreData.regenInterval : data != null ? data.regenInterval : 5f);
        public float ConsumableDamageReduction => itemEffects != null ? itemEffects.PlayerDamageReduction : 0f;
        public float ConsumableMonsterTrueDamageFraction => itemEffects != null ? itemEffects.MonsterTrueDamageFraction : 0f;
        public float Damage => coreData != null ? CalculateSurvivorDamage(coreData.damage) : ((data != null ? Mathf.Max(0, data.damage + Mathf.Max(0, data.damagePerLevel) * (Level - 1)) : 1) * (1f + CardDamagePercent * .01f) + CardDamage) * DamagePotionMultiplier * (1f + RuneUpgrades.Bonus(RuneStat.Damage) * .01f);
        public float AttackSpeed => CalculateAttackSpeed();
        public float CalculateAttackSpeed(float addedPercent=0) => Mathf.Max(.1f, ((coreData != null ? coreData.attackSpeedMultiplier : data != null ? data.attackSpeed : 1f) * (1f + CardAttackSpeedPercent * .01f) + CardAttackSpeed) * AttackSpeedPotionMultiplier * (1f + RuneUpgrades.Bonus(RuneStat.AttackSpeed) * .01f)*(1f+(addedPercent+UpgradeBonus(SurvivorUpgradeId.AttackSpeed)+(UpgradeLevel(SurvivorUpgradeId.Quickening)>0?AbilityData.uncommon.quickening.bonus.At(UpgradeLevel(SurvivorUpgradeId.Quickening)):0))*.01f));
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
        public float GunLevelDamageBonus => 0f;
        public float CalculateGunDamage(float baseDamage) => (Mathf.Max(0f, baseDamage) + GunLevelDamageBonus) * GunDamageMultiplier;
        public float CalculateSurvivorDamage(float baseDamage,float extraPhysicalDamage=0) =>
            UpgradeDamage(((Mathf.Max(0,baseDamage)+GunLevelDamageBonus)*(1f+CardDamagePercent*.01f)+CardDamage)
            *GunDamageMultiplier*DamagePotionMultiplier*(1f+RuneUpgrades.Bonus(RuneStat.Damage)*.01f),extraPhysicalDamage);
        public float GunBulletSpeedMultiplier => 1f + (GunCardBulletSpeedPercent+UpgradeBonus(SurvivorUpgradeId.ProjectileSpeed)) * .01f;
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
            public int version = 8;
            public float legacyPhysicalPercent,legacyArmorFlat,legacyMagicFlat,legacyRegenFlat;
            public int bossesDefeated; public float bossBonusHealth;
            public int[] upgradeLevels;
            public float[] upgradeTotals;
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
            authoredBodyScale=transform.localScale;
            gunSpecials=GetComponent<GameData.Player.Script.GunSpecialUpgrades>();
            itemEffects = FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
            if (coreData != null) SetProgress(coreData.startingLevel, coreData.startingExperience, coreData.experienceRequired);
            else if (data != null) SetProgress(data.startingLevel, data.startingExperience, data.experienceRequired);
            LoadProgress();ApplyBodySize();
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
            EnsureUpgradeCapacity();
            if (!Application.isPlaying || !GameSave.HasKey(ProgressSaveKey)) return;
            try
            {
                var saved = JsonUtility.FromJson<SavedProgress>(GameSave.GetString(ProgressSaveKey));
                if (saved == null || (saved.version < 1 || saved.version > 8) || saved.level < 1 ||
                    float.IsNaN(saved.experience) || float.IsInfinity(saved.experience) || saved.experience < 0 ||
                    float.IsNaN(saved.requiredExperience) || float.IsInfinity(saved.requiredExperience) || saved.requiredExperience < 1)
                {
                    saveBlocked = true;
                    Debug.LogWarning("Player progression save is invalid or from an unsupported version. It is preserved until Reset Data.", this);
                    return;
                }
                for(int i=0;i<upgradeLevels.Length;i++)
                {
                    upgradeLevels[i]=saved.upgradeLevels!=null&&i<saved.upgradeLevels.Length?Mathf.Clamp(saved.upgradeLevels[i],0,SurvivorUpgradeData.LevelCap):0;
                    upgradeTotals[i]=saved.upgradeTotals!=null&&i<saved.upgradeTotals.Length?SafeBonus(saved.upgradeTotals[i]):0;
                }
                legacyPhysicalPercent=SafeBonus(saved.legacyPhysicalPercent);legacyArmorFlat=SafeBonus(saved.legacyArmorFlat);legacyMagicFlat=SafeBonus(saved.legacyMagicFlat);legacyRegenFlat=SafeBonus(saved.legacyRegenFlat);
                // Preserve earned effects while new stat cards use the new plan's flat/percent units.
                if(saved.version<8){legacyPhysicalPercent=upgradeTotals[(int)SurvivorUpgradeId.PhysicalDamage];legacyArmorFlat=upgradeTotals[(int)SurvivorUpgradeId.Armor];legacyMagicFlat=upgradeTotals[(int)SurvivorUpgradeId.MagicResistance];legacyRegenFlat=upgradeTotals[(int)SurvivorUpgradeId.Regeneration];foreach(var id in new[]{SurvivorUpgradeId.PhysicalDamage,SurvivorUpgradeId.Armor,SurvivorUpgradeId.MagicResistance,SurvivorUpgradeId.Regeneration})upgradeTotals[(int)id]=0;}
                BossesDefeated=Mathf.Max(0,saved.bossesDefeated);BossBonusHealth=SafeBonus(saved.bossBonusHealth);
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
            { legacyPhysicalPercent=legacyPhysicalPercent,legacyArmorFlat=legacyArmorFlat,legacyMagicFlat=legacyMagicFlat,legacyRegenFlat=legacyRegenFlat,bossesDefeated=BossesDefeated,bossBonusHealth=BossBonusHealth,upgradeLevels=upgradeLevels,upgradeTotals=upgradeTotals,level = Level, experience = Experience, requiredExperience = ExperienceRequired,
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
            foreach (var player in FindObjectsByType<PlayerStats>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var defaults = player.Data;
                player.RuneUpgrades.Reset();
                System.Array.Clear(player.upgradeLevels,0,player.upgradeLevels.Length);System.Array.Clear(player.upgradeTotals,0,player.upgradeTotals.Length);
                player.legacyPhysicalPercent=player.legacyArmorFlat=player.legacyMagicFlat=player.legacyRegenFlat=0;
                player.CardDamage = player.CardHealth = player.CardAttackSpeed = 0f;
                player.CardDamagePercent = player.CardHealthPercent = player.CardAttackSpeedPercent = 0f;
                player.CardRegenReductionPercent = player.CardHealingPercent = 0f;
                player.GunCardDamagePercent = player.GunCardBulletSpeedPercent = 0f;
                player.GunCardMagazineBonus = 0;player.BossesDefeated=0;player.BossBonusHealth=0;
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
            ApplyBodySize();
            if (dirty && Time.unscaledTime >= nextSave)
            {
                nextSave = Time.unscaledTime + 1;
                SaveProgress();
            }
        }
        public static PlayerStatsData For(Component owner)
        {
            var stats = owner.GetComponent<PlayerStats>();
            return stats != null ? stats.Data : null;
        }
    }
}
