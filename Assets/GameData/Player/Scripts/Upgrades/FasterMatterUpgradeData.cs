using UnityEngine;
namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName="Player/Blue Upgrades/Faster Matter")]
    public sealed class FasterMatterUpgradeData : ScriptableObject
    {
        [Min(0)] public float fireRateBonusPercent=20f;
    }
}

