using UnityEngine;
namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName="Survivor/Spawning/Monster Spawn Settings")]
    public sealed class MonsterSpawnSettings : ScriptableObject
    {
        [Header("Continuous spawning — gameplay time, not a daily quota")]
        [Min(.01f)] public float initialSpawnsPerSecond=.5f;
        [Min(0),Tooltip("Gameplay seconds before population growth starts. Day/night transitions do not reset this timer.")] public float growthDelaySeconds=60f;
        [Min(0),Tooltip("Added to the spawn rate every gameplay minute after Growth Delay Seconds.")] public float additionalSpawnsPerSecondPerMinute=.15f;
        [Min(0),Tooltip("Added to the spawn rate for each player level above 1. No maximum spawn rate.")] public float additionalSpawnsPerSecondPerPlayerLevel=.15f;
        [Min(1),Tooltip("Simultaneous living monsters across spawners. Spawning waits for space; there is no daily quota.")] public int maximumLivingMonsters=50;
        public float RateAtSeconds(float seconds,int playerLevel=1)=>Mathf.Max(.01f,initialSpawnsPerSecond)+Mathf.Max(0,additionalSpawnsPerSecondPerMinute)*Mathf.Max(0,seconds-Mathf.Max(0,growthDelaySeconds))/60f+Mathf.Max(0,additionalSpawnsPerSecondPerPlayerLevel)*Mathf.Max(0,playerLevel-1);
        [Header("Spawn groups")]
        [Min(1)] public int initialGroupSize=3;
        [Min(1)] public int playerLevelsPerExtraGroupMember=10;
        public int GroupSizeAtLevel(int level)=>Mathf.Max(1,initialGroupSize)+Mathf.Max(0,level-1)/Mathf.Max(1,playerLevelsPerExtraGroupMember);
        [Header("Regular monsters — horizontal distance in metres")]
        [Tooltip("Off: around the spawner. On: around the moving player. Chest defense uses its own chest settings.")]
        public bool followPlayer;
        [Min(.5f)] public float minimumDistance=12;
        [Min(.5f)] public float maximumDistance=25;
        [Tooltip("No regular monster may appear closer to the player than this distance.")]
        [Min(0)] public float minimumPlayerDistance=3;
    }
}

