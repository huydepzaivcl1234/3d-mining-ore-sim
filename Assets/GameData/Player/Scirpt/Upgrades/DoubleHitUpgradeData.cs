using UnityEngine;
namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName="Player/Blue Upgrades/Bắn 1 được 2 / Double Hit")]
    public sealed class DoubleHitUpgradeData : ScriptableObject
    {
        [Range(0,1)] public float splashDamageFraction=.3f;
        [Min(.1f)] public float splashRadius=8f;
    }
}

