using UnityEngine;

namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName="Survivor/Monsters/Bread Monster")]
    public sealed class BreadMonsterData : MonsterRewardData
    {
        [Header("Thrown bread slice")]
        public BreadSliceProjectile sliceProjectile;
        [Min(.1f)] public float flightSeconds=1.25f;
        [Min(0)] public float arcHeight=3f;
        [Tooltip("Sideways curve before descending onto the tracked target.")]
        public float arcSideways=1f;
        public float spinDegreesPerSecond=540f;
        [Header("Heal nearby monsters (does not revive)")]
        [Min(0)] public float healingRadius=6f;
        [Min(.1f)] public float healingInterval=5f;
        [Min(0)] public float healingFlat=5f;
        [Min(0)] public float healingPerLevel=1f;
        [Range(0,100)] public float healingMaxHealthPercent=3f;
        public bool healSelf=true;
        [Min(.1f)] public float healingFlightSeconds=.65f;
        [Header("Hyperglycemia — magic damage per tick, player only")]
        [Range(0,100)] public float sugarMaxHealthPercent=1f;
        [Min(0)] public float sugarDamagePerMonsterLevel=1f;
        [Min(.1f)] public float sugarDuration=5f;
        [Min(.1f)] public float sugarTickSeconds=1f;
        public float SugarDamage(float maxHealth,int level)=>Mathf.Max(0,maxHealth)*sugarMaxHealthPercent*.01f+Mathf.Max(1,level)*sugarDamagePerMonsterLevel;
    }
}
