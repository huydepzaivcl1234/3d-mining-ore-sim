using UnityEngine;
namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName="Player/Blue Upgrades/Bom khổng lồ / Giant Bomb")]
    public sealed class GiantBombUpgradeData : ScriptableObject
    {
        [Header("Damage / Sát thương")]
        [Min(1)] public int giantBombHits=10;
        [Min(0)] public float giantBombBaseDamage=1f;
        public MiningSimulator.Ores.CombatDamageType giantBombDamageType=MiningSimulator.Ores.CombatDamageType.Physical;
        [Header("Wave / Vòng sóng (metres)")]
        [Tooltip("Bán kính tối đa của vòng bom, tính bằng mét.")]
        [Min(.1f)] public float giantBombRadius=8f;
        [Tooltip("Tốc độ lan vòng bom, mét/giây.")]
        [Min(.1f)] public float giantBombWaveSpeed=16f;
        [Tooltip("Độ chênh cao tối đa để trúng sóng.")]
        [Min(0)] public float giantBombVerticalRange=3f;
        [Tooltip("Độ dày vòng sóng, mét.")]
        [Min(.01f)] public float giantBombWidth=.15f;
        public Color giantBombColor=new(1f,.45f,.12f,1f);
        [Tooltip("Độ cao hiển thị của vòng so với điểm phát, không đổi vùng damage.")]
        public float giantBombVisualHeight=.15f;
        [Range(8,128)] public int giantBombSegments=64;
    }
}

