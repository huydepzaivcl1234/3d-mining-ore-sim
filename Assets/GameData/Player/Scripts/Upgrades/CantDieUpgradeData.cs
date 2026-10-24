using UnityEngine;
namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName="Player/Orange Upgrades/Cant Die")]
    public sealed class CantDieUpgradeData : ScriptableObject
    {
        [Min(.1f)] public float recoverySeconds=3f;
        [Range(0,1)] public float recoveryHealthFraction=.2f;
        [Min(0)] public float cooldownSeconds=120f;
    }
}
