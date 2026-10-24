using System.Diagnostics;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>One shared budget across zones; queued waves are deferred, never discarded.</summary>
    public static class MonsterSpawnBudget
    {
        private static int frame=-1,attempts;
        private static long started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){frame=-1;attempts=0;started=0;}
        public static bool Available
        {
            get
            {
                var data=GameplayPerformanceData.Current;
                if(!data||!data.spreadSpawnsAcrossFrames)return true;
                if(frame!=Time.frameCount)return true;
                return attempts<Mathf.Max(1,data.maximumSpawnAttemptsPerFrame)&&
                    (Stopwatch.GetTimestamp()-started)*1000d/Stopwatch.Frequency<Mathf.Max(.1f,data.spawnBudgetMilliseconds);
            }
        }
        public static bool TryBegin()
        {
            if(!Available)return false;
            if(frame!=Time.frameCount){frame=Time.frameCount;attempts=0;started=Stopwatch.GetTimestamp();}
            attempts++;return true;
        }
    }
}
