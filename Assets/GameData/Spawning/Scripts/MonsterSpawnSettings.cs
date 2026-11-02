using UnityEngine;
namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName="Survivor/Spawning/Monster Spawn Settings")]
    public sealed class MonsterSpawnSettings : ScriptableObject
    {
        [Header("Continuous spawning — gameplay time, not a daily quota")]
        [Min(.01f)] public float initialSpawnsPerSecond=.5f;
        [Min(0)] public float additionalSpawnsPerSecondPerMinute=.25f;
        [Min(.01f)] public float maximumSpawnsPerSecond=5;
        [Range(1,16)] public int maximumSpawnsPerFrame=4;
        [Min(1),Tooltip("Simultaneous living monsters across spawners. Spawning waits for space; there is no daily quota.")] public int maximumLivingMonsters=50;
        public float RateAtSeconds(float seconds)=>Mathf.Min(Mathf.Max(.01f,maximumSpawnsPerSecond),Mathf.Max(.01f,initialSpawnsPerSecond)+Mathf.Max(0,additionalSpawnsPerSecondPerMinute)*Mathf.Max(0,seconds)/60f);
        [Header("Regular monsters — horizontal distance in metres")]
        [Tooltip("Off: around the spawner. On: around the moving player. Chest defense uses its own chest settings.")]
        public bool followPlayer;
        [Min(.5f)] public float minimumDistance=12;
        [Min(.5f)] public float maximumDistance=25;
        [Tooltip("No regular monster may appear closer to the player than this distance.")]
        [Min(0)] public float minimumPlayerDistance=3;
    }
}

