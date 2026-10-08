using UnityEngine;

namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName = "Mining Simulator/Monsters/Mushnight")]
    public sealed class MushnightData : MonsterRewardData
    {
        [Header("Mushnight: theft, escape and stamina")]
        public MushnightSettings mushnight = new();
    }
}
