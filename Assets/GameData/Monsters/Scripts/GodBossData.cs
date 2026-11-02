using UnityEngine;

namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName = "Mining Simulator/Game Data/God Boss")]
    public sealed class GodBossData : ScriptableObject
    {
        public GodBossController prefab;
        [HideInInspector] public int everyDays = 50; // Legacy serialized field; encounters now use daily rolls.
        [Header("Daily encounter after reaching the required player level")]
        [Min(1)] public int minimumPlayerLevel=15;
        [Range(0,100)] public float initialChancePercent=5,dailyChanceIncreasePercent=5;
        [Header("Added strength per later appearance (not per day)")]
        [Min(0)] public float healthIncreasePercent=25,damageIncreasePercent=25;
        [Header("Crowd-control resistance")]
        [Range(0,100),Tooltip("Reduces control duration. At 100%, immune to slow and airborne; does not reduce burn damage.")] public float crowdControlResistancePercent=50;
        public float SpawnChancePercent(int day,int firstEligibleDay,int lastAppearanceDay=0)=>firstEligibleDay<1||day<firstEligibleDay?0:lastAppearanceDay>0?Mathf.Clamp(dailyChanceIncreasePercent*Mathf.Max(0,day-lastAppearanceDay),0,100):Mathf.Clamp(initialChancePercent+dailyChanceIncreasePercent*(day-firstEligibleDay),0,100);
        public float HealthMultiplier(int appearance)=>1+Mathf.Max(0,appearance-1)*healthIncreasePercent*.01f;
        public float DamageMultiplier(int appearance)=>1+Mathf.Max(0,appearance-1)*damageIncreasePercent*.01f;
        [Min(1)] public float health = 5000;
        [Min(0)] public float armor = 10, magicResistance = 10;
        [Min(1)] public float height = 8;
        [Min(1)] public float spawnDistance = 35;
        [Min(0)] public float walkSpeed = 3, runSpeed = 9;
        [Min(0)] public float startRunningDistance = 30, stopRunningDistance = 20;
        [Min(0)] public float turnSpeed = 180;
        [Header("Close attacks - clip speed, impact point and recovery")]
        public Strike punch = new Strike { speed = 1.5f, damage = 12, hitTime = .43f, knockback = 3 };
        public Strike swipe = new Strike { speed = 1, damage = 16, hitTime = .5f };
        [Min(0)] public float meleeRange = 5, lungeDistance = 1.5f;
        [Range(0,180)] public float attackHalfAngle = 65;
        [Min(0)] public float recovery = .35f;
        [Header("Committed jump")]
        [Min(0)] public float jumpRange = 10, jumpCooldown = 7, jumpWindup = .5f;
        [Min(.01f)] public float jumpSpeed = 1, jumpTravelSeconds = .65f;
        [Min(0)] public float jumpHeight = 5, jumpDamage = 20, jumpKnockback = 12;
        [Min(.1f)] public float waveRadius = 15, waveSpeed = 18, waveThickness = 1;
        [Header("Roar - random check only while injured")]
        [Min(.1f)] public float roarCheckSeconds = 3, roarCooldown = 14, roarSpeed = 1;
        [Range(0,1)] public float roarChance = .2f;
        [Range(0,1)] public float healMaxHealthFraction = .05f;
        [Min(1)] public float summonHealthAtZeroBossHP = 3, summonDamageAtZeroBossHP = 2;
        public bool summonBossVariants = true;
        [Min(1)] public int maxLivingSummons = 24;
        [Header("Minion placement around God Boss (metres)")]
        [Min(0)] public float summonMinimumDistance = 8, summonMaximumDistance = 16;
        [Min(0)] public float summonMinimumPlayerDistance = 3;
        [Min(0)] public float bleedMaxHealthPercentPerSecond = 1, bleedDuration = 2;
        [Header("Animation")]
        public AnimationClip idle, walk, run, punchClip, swipeClip, jump, roar, dying;
        [Min(.01f)] public float crossfade = .12f, deathSpeed = 1;
        public bool IsEncounterDay(int day) => day > 0;
        [System.Serializable] public sealed class Strike
        {
            [Min(.01f)] public float speed = 1;
            [Min(0)] public float damage = 10;
            [Range(0,1)] public float hitTime = .45f;
            [Min(0)] public float knockback;
        }
    }
}
