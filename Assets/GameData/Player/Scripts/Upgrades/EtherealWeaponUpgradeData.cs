using UnityEngine;
namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName="Player/Orange Upgrades/Ethereal Weapon")]
    public sealed class EtherealWeaponUpgradeData : ScriptableObject
    {
        [Tooltip("Cooldown riêng trên mỗi quái; đòn bắn trực tiếp không bị khóa.")]
        [Min(.01f)] public float perTargetCooldown=1f;
        [Tooltip("Bắn thường mở cửa sổ, các hiệu ứng không tự mở hoặc gia hạn.")]
        [Min(.01f)] public float activeSeconds=3f;
        [Min(0)] public float reactivationCooldown=1f;
    }
}
