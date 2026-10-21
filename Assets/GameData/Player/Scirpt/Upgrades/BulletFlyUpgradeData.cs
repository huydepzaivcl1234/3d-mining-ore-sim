using UnityEngine;
namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName="Player/Blue Upgrades/Bullet Fly")]
    public sealed class BulletFlyUpgradeData : ScriptableObject
    {
        public float[] bulletFlyDamage={1f,2f,3f,4f,5f};
        [Tooltip("Mỗi viên: base trong danh sách + cấp nhân vật × giá trị này. Chốt cấp khi bắt đầu đợt đạn; vẫn là sát thương chuẩn.")]
        [Min(0)] public float damagePerPlayerLevel=1f;
        public float DamageAtLevel(int index,int playerLevel)
        {
            if(bulletFlyDamage==null||index<0||index>=bulletFlyDamage.Length)return 0;
            return (float)System.Math.Min(float.MaxValue,
                (double)MiningSimulator.Ores.CombatDamage.NonNegative(bulletFlyDamage[index])+
                (double)MiningSimulator.Ores.CombatDamage.NonNegative(damagePerPlayerLevel)*Mathf.Max(1,playerLevel));
        }
        [Min(.1f)] public float bulletFlySpeed=10f;
        [Min(0)] public float bulletFlyTellSeconds=.2f, bulletFlyInterval=.12f;
        [Min(.1f)] public float bulletFlyHeight=.8f, bulletFlySpacing=.3f;
        [Min(.01f)] public float bulletFlyWidth=.08f, bulletFlyLength=.25f;
        [Min(1)] public int bulletFlyVisibleLimit=150;
        [Header("VFX budget / Giới hạn hiệu ứng")]
        [Tooltip("Số đợt bay tối đa. Quá tải: giải quyết damage còn lại của đợt cũ, không bỏ damage.")]
        [Min(1)] public int maximumConcurrentVolleys=24;
        [Min(0)] public int retainedVisuals=16;
        [Tooltip("Xóa đạn trong pool khi không còn đợt bay, tính bằng giây.")]
        [Min(0)] public float idleCleanupSeconds=2f;
        [Tooltip("Sau thời gian này giải quyết damage còn lại và dọn đợt đạn.")]
        [Min(.1f)] public float maximumFlightSeconds=6f;
        public Color bulletFlyColor=new(.25f,.8f,1f,1f);
        [Header("Surviving target / Giảm damage theo quái")]
        [Min(1)] public int survivingHitsBeforeReduction=5;
        [Range(0,1)] public float reductionPerVolley=.05f,maximumReduction=.2f;
    }
}

