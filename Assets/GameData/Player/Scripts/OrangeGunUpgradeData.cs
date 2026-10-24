using UnityEngine;
namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName="Player/Orange Gun Upgrade Box")]
    public sealed class OrangeGunUpgradeData : ScriptableObject
    {
        public GameObject boxPrefab;
        [Range(0,100)] public float dropChancePercent=100;
        [Min(.1f)] public float pickupRadius=1.5f;
        [Min(0)] public float pickupDelay=1;
        [Min(.01f)] public float visualScale=.7f;
        public Vector3 dropOffset=new(2,0,0);
        public CantDieUpgradeData cantDie;
        public CannonUpgradeData cannon;
        public EtherealWeaponUpgradeData etherealWeapon;
    }
}
