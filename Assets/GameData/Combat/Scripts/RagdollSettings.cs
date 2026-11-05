using UnityEngine;
namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName="Survivor/Physics/Ragdoll pool",fileName="SurvivorRagdollSettings")]
    public sealed class RagdollSettings:ScriptableObject
    {
        [Tooltip("Death ragdolls for Bread and Potion only. Other monsters keep their existing deaths.")]
        public bool monsterRagdolls=true;
        [Min(1)] public int maximumActiveCorpses=12;
        [Min(0)] public int maximumCachedCorpses=8;
        [Min(.2f)] public float corpseSeconds=8;
        [Range(1,24)] public int maximumBodiesPerCorpse=16;
        [Header("Active death muscles — fades into a relaxed collapse")]
        [Min(.01f)] public float muscleSeconds=.7f;
        [Min(0)] public float spring=18,damping=3,maximumAngularAcceleration=30;
        static RagdollSettings current;
        public static RagdollSettings Current {get{if(!current)current=Resources.Load<RagdollSettings>("SurvivorRagdollSettings");return current;}}
    }
}
