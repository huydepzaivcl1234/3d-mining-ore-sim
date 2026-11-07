using UnityEngine;

namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName = "Mining Simulator/Monsters/Mushranon")]
    public sealed class MushranonData : MonsterRewardData
    {
        [Min(.1f),Tooltip("Flies straight in its firing direction, then returns to the pool after this many metres.")]
        public float projectileTravelDistance=10f;
    }
}
