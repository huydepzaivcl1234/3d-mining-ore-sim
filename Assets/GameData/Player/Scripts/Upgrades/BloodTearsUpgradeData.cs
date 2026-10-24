using UnityEngine;
namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName="Player/Blue Upgrades/Huyết lệ / Blood Tears")]
    public sealed class BloodTearsUpgradeData : ScriptableObject
    {
        [Range(0,1)] public float bloodTearsHealFraction=.1f;
    }
}

