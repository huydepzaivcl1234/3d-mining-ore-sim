using UnityEngine;
namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName="Player/Blue Upgrades/Đạn sắc / Sharp Rounds")]
    public sealed class SharpRoundsUpgradeData : ScriptableObject
    {
        [Range(0,1)] public float bleedDamageFraction=.35f;
        [Min(.1f)] public float bleedDuration=3f, bleedTickInterval=1f;
    }
}

