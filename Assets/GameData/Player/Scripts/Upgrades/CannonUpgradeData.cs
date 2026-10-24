using UnityEngine;
namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName="Player/Orange Upgrades/Cannon")]
    public sealed class CannonUpgradeData : ScriptableObject
    {
        [Range(0,100)] public float chancePercent=35;
        [Min(0)] public float damageFraction=.3f;
        [Min(.1f)] public float projectileSpeed=35,flightLimit=4;
        [Min(.01f)] public float visualSize=.25f;
        [Min(1)] public int maximumProjectiles=24;
        public Color color=new(1,.4f,.05f,1);
    }
}
