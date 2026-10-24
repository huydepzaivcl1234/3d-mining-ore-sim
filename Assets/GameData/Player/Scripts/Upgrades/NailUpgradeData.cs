using UnityEngine;
namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName="Player/Blue Upgrades/Đóng đinh / Nail")]
    public sealed class NailUpgradeData : ScriptableObject
    {
        [Tooltip("Percent of victim max HP added per stack, not a fraction.")]
        [Min(0)] public float nailMaxHealthPercent=.0001f;
    }
}

