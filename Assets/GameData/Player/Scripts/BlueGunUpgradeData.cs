using UnityEngine;
namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName="Player/Blue Gun Upgrade Box")]
    public sealed class BlueGunUpgradeData : ScriptableObject
    {
        [Header("Box / Hộp")]
        public GameObject boxPrefab;
        [Min(.1f)] public float pickupRadius=1.5f;
        [Min(0)] public float pickupDelay=1f;
        [Min(.01f)] public float visualScale=.7f;
        [Header("Upgrade data / Mỗi nâng cấp một asset")]
        [InspectorName("Bắn 1 được 2 / Double Hit")] public DoubleHitUpgradeData doubleHit;
        [InspectorName("Đạn sắc / Sharp Rounds")] public SharpRoundsUpgradeData sharpRounds;
        [InspectorName("Đóng đinh / Nail")] public NailUpgradeData nail;
        [InspectorName("Faster Matter")] public FasterMatterUpgradeData fasterMatter;
        [InspectorName("Bullet Fly")] public BulletFlyUpgradeData bulletFly;
        [InspectorName("Thợ săn / Hunter")] public HunterUpgradeData hunter;
        [InspectorName("Huyết lệ / Blood Tears")] public BloodTearsUpgradeData bloodTears;
        [InspectorName("Bom khổng lồ / Giant Bomb")] public GiantBombUpgradeData giantBomb;
        [Header("Damage popup")]
        public MiningSimulator.Ores.MiningDamagePopupData popupData;
        [HideInInspector,SerializeField,UnityEngine.Serialization.FormerlySerializedAs("damagePopup")]
        private MiningSimulator.Ores.MiningDamagePopupSettings legacyDamagePopup=new();
        public MiningSimulator.Ores.MiningDamagePopupSettings damagePopup=>popupData?popupData.settings:legacyDamagePopup;
        [Header("Orange Box / Hộp cam")]
        public OrangeGunUpgradeData orange;
    }
}
