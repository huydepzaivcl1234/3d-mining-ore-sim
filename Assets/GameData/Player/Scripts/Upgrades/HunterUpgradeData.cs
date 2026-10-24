using UnityEngine;
namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName="Player/Blue Upgrades/Thợ săn / Hunter")]
    public sealed class HunterUpgradeData : ScriptableObject
    {
        [Min(1)] public int hunterKillsPerReward=5, hunterHealthPerReward=1;
        [Tooltip("Maximum bonus HP, added on top of base max health.")]
        [Min(0)] public int hunterMaxHealth=500;
    }
}

