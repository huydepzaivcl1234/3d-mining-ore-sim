using UnityEngine;
namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName="Survivor/Monsters/Potion Monster")]
    public sealed class PotionMonsterData : MonsterRewardData
    {
        [Header("Potion models (existing project prefabs)")]
        public GameObject greenPotion, redPotion, yellowPotion;
        [Header("Throw")]
        [Min(.1f)] public float flightSeconds=1.2f;
        [Min(.01f)] public float potionSize=.24f;
        [Min(0)] public float arcHeight=2.5f;
        [Header("Green — heal one wounded ally")]
        [Min(0)] public float healingRange=10, healingFlat=10;
        [Range(0,100)] public float healingMaxHealthPercent=10;
        [Header("Red — TRUE damage per second")]
        [Min(0)] public float redFlatDamage=5;
        [Range(0,100)] public float redPlayerMaxHealthPercent=5;
        [Min(.1f)] public float burnDuration=5;
        [Header("Yellow — magic damage and attack-speed slow")]
        [Min(0)] public float abilityPower=20, abilityPowerPerLevel=2;
        [Min(0)] public float yellowFlatDamage=10;
        [Range(0,100)] public float yellowAbilityPowerPercent=15, attackSpeedSlowPercent=25;
        [Min(.1f)] public float slowDuration=5;
        public float AbilityPowerAt(int level)=>abilityPower+abilityPowerPerLevel*Mathf.Max(0,level-1);
        public float YellowDamage(int level)=>yellowFlatDamage+yellowAbilityPowerPercent*.01f*AbilityPowerAt(level);
        public float BurnDamage(float playerMaxHealth)=>redFlatDamage+redPlayerMaxHealthPercent*.01f*playerMaxHealth;
    }
}
