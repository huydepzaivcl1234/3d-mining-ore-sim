using UnityEngine;

namespace GameData.Player.Script
{
    public enum ProjectileAppearance { Automatic, Normal, Ice, Fire, Toxic }
    [CreateAssetMenu(menuName="Survivor/Automatic Combat")]
    public sealed class SurvivorCombatData : ScriptableObject
    {
        [Header("Survivor — base attack") ]
        [Min(0)] public float damage=20f;
        [HideInInspector,Min(.08f)] public float attackInterval=.65f; // Old seconds/attack; migrated once.
        [Min(.01f),Tooltip("Base attacks per second, before bonuses.")] public float attackSpeed=1f/.65f;
        [Min(0),Tooltip("Attacks/second added by +100% bonus attack speed.")] public float attackSpeedRatio=1f/.65f;
        public float baseBonusAttackSpeedPercent;
        [Min(.01f)] public float maximumAttackSpeed=12.5f;
        [SerializeField,HideInInspector] private int attackSpeedVersion;
        public void MigrateAttackSpeed(){
            if(attackSpeedVersion>0)return;
            attackSpeed=1f/CombatValue(attackInterval,.65f,.08f);attackSpeedRatio=attackSpeed;
            baseBonusAttackSpeedPercent=(CombatValue(attackSpeedMultiplier,1,.1f)-1)*100f;attackSpeedVersion=1;
        }
        private void OnEnable(){MigrateAttackSpeed();}
        public float AttacksPerSecond(float bonusPercent=0){
            MigrateAttackSpeed();
            float total=CombatValue(attackSpeed,1f/.65f,.01f)+CombatValue(attackSpeedRatio,0,0)*(baseBonusAttackSpeedPercent+bonusPercent)*.01f;
            return Mathf.Clamp(CombatValue(total,.01f,.01f),.01f,CombatValue(maximumAttackSpeed,12.5f,.01f));
        }
        [Min(1)] public float range=13f;
        [Min(1)] public float projectileSpeed=22f;
        [Min(.02f)] public float projectileRadius=.10f;
        [Range(1,16)] public int maximumProjectilesPerVolley=8;
        [Min(8)] public int poolCapacity=64;
        public LayerMask hitLayers=~0;
        [Header("Imported projectile models")]
        public ProjectileAppearance projectileAppearance=ProjectileAppearance.Automatic;
        public GameObject normalProjectilePrefab,iceProjectilePrefab,fireProjectilePrefab,toxicProjectilePrefab,chargedCubePrefab;
        [Header("Projectile feedback — visuals only")]
        [Min(0),Tooltip("Trail persistence in seconds. Does not change projectile speed or damage.")] public float projectileTrailSeconds=.08f;
        [Range(.1f,2f)] public float projectileTrailWidth=.65f;
        [Min(0),Tooltip("Set 0 to disable the small impact flash.")] public float impactFlashSeconds=.09f;
        [Min(.1f)] public float impactFlashSize=1.8f;
        [HideInInspector]
        [Min(1)] public int magazineCapacity=9;
        [HideInInspector,Min(.1f)] public float reloadSeconds=1.5f;
        public SurvivorAbilityData abilities;
        [HideInInspector]
        public AudioClip[] igniteSfx=System.Array.Empty<AudioClip>();
        [HideInInspector,Range(0,1)] public float igniteVolume=.65f;
        [Header("Health")]
        [Min(1f)] public float maxHealth = 50f;
        [HideInInspector,Min(0f)] public float healthPerLevel;
        [Min(0f)] public float regenAmount;
        [Min(.1f)] public float regenInterval = 5f;
        [Header("Defenses - includes level growth; equipment is added at runtime")]
        [Min(0f)] public float armor;
        [Min(0f)] public float armorPerLevel;
        [Min(0f)] public float magicResistance;
        [Min(0f)] public float magicResistancePerLevel;
        [Min(.01f)] public float resistanceScale = 100f;
        [Header("Automatic attacks - multiplier on the equipped attack cadence")]
        [HideInInspector,Min(.1f)] public float attackSpeedMultiplier = 1f;

        [Header("XP / Level - starting values; current progress is saved per player")]
        [Min(1)] public int startingLevel = 1;
        [Min(0f)] public float startingExperience;
        [Min(1f)] public float experienceRequired = 100f;
        [Min(1f)] public float experienceRequirementGrowth = 1.25f;
        [Tooltip("Flat attack damage added for each level above level 1.")]
        [HideInInspector,Min(0f)] public float damagePerLevel;

        [Header("Stamina")]
        [Min(1f)] public float maxStamina = 100f;
        [Min(0f)] public float runningCostPerSecond = 10f;
        [Min(0f)] public float staminaRecoveryPerSecond = 8f;
        [Min(0f)] public float staminaRecoveryDelay = 1.5f;
        [Tooltip("After stamina reaches zero, recover this fraction of maximum stamina before sprinting again.")]
        [Range(0f, 1f)] public float sprintRecoveryThreshold = 0.25f;

        [Header("Movement Speed")]
        [Min(0f)] public float walkSpeed = 3f;
        [HideInInspector] public float runSpeed = 6f; // Legacy serialized field; sprint is walking speed + 3.

        [Header("Jump")]
        [Min(0f)] public float jumpStrength = 260f;


        private static float CombatValue(float value, float fallback, float minimum) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(minimum, value);
        private void OnValidate()
        {
            MigrateAttackSpeed();
            attackSpeed=CombatValue(attackSpeed,1f/.65f,.01f);attackSpeedRatio=CombatValue(attackSpeedRatio,0,0);
            maximumAttackSpeed=CombatValue(maximumAttackSpeed,12.5f,.01f);
            baseBonusAttackSpeedPercent=float.IsNaN(baseBonusAttackSpeedPercent)||float.IsInfinity(baseBonusAttackSpeedPercent)?0:baseBonusAttackSpeedPercent;
            damage = CombatValue(damage, 20f, 0f);
            range = CombatValue(range, 13f, .1f);
            attackInterval = CombatValue(attackInterval, .65f, .08f);
            projectileSpeed = CombatValue(projectileSpeed, 22f, .1f);
            projectileTrailSeconds=CombatValue(projectileTrailSeconds,.08f,0f);
            projectileTrailWidth=Mathf.Clamp(CombatValue(projectileTrailWidth,.65f,.1f),.1f,2f);
            impactFlashSeconds=CombatValue(impactFlashSeconds,.09f,0f);
            impactFlashSize=CombatValue(impactFlashSize,1.8f,.1f);
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
            runSpeed = walkSpeed + 3f;
            jumpStrength = Mathf.Max(0f, jumpStrength);
        }

        public Color projectileColor=new Color(1f,.65f,.12f,1);
    }
}
