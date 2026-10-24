using UnityEngine;
namespace MiningSimulator.Ores
{
    [CreateAssetMenu(menuName="Chest Defense/Towers/Cannon", fileName="CannonData")]
    public sealed class CannonTowerData : TowerData
    {
        [Min(0)] public float shotDelay = .033f;
        public string fireTrigger = "Fire";
    }
}
