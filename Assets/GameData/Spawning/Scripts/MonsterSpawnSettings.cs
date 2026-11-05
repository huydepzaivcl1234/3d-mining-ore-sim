using UnityEngine;
namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName="Survivor/Spawning/Monster Spawn Settings")]
    public sealed class MonsterSpawnSettings : ScriptableObject
    {
        [Header("Slow start — first monster must be defeated")]
        [Min(.1f),Tooltip("Seconds between regular spawns after the first kill. Each spawn creates one monster.")] public float initialSpawnInterval=8f;
        [Min(1)] public int killsPerSpeedIncrease=10;
        [Min(0),Tooltip("Seconds removed from the interval after each kill milestone.")] public float intervalReductionPerMilestone=.5f;
        [Min(.1f)] public float minimumSpawnInterval=2f;
        [Min(1),Tooltip("Total living monsters across spawners. Boss/special encounter rules are separate.")] public int maximumLivingMonsters=1000;
        public float IntervalAfterKills(int kills)=>Mathf.Max(Mathf.Max(.1f,minimumSpawnInterval),Mathf.Max(.1f,initialSpawnInterval)-Mathf.Max(0,kills)/Mathf.Max(1,killsPerSpeedIncrease)*Mathf.Max(0,intervalReductionPerMilestone));
        [Header("Regular monsters â€” horizontal distance in metres")]
        [Tooltip("Off: around the spawner. On: around the moving player. Chest defense uses its own chest settings.")]
        public bool followPlayer;
        [Min(.5f)] public float minimumDistance=12;
        [Min(.5f)] public float maximumDistance=25;
        [Tooltip("No regular monster may appear closer to the player than this distance.")]
        [Min(0)] public float minimumPlayerDistance=3;
    }
}

