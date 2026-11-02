using UnityEngine;
namespace GameData.Player.Script
{
    [CreateAssetMenu(menuName="Survivor/Daily Loot")]
    public sealed class SurvivorLootData : ScriptableObject
    {
        public GameObject prefab;
        [Header("Sparkling box — Anime Star VFX")]
        public GameObject shinyVfx;
        [Min(.01f)] public float shinyVfxScale=.65f;
        public Vector3 shinyVfxOffset=new(0,.55f,0);
        [Tooltip("Number of ability boxes spawned each morning. Changes apply on the next morning. Zero disables new boxes.")]
        [Range(0,50)] public int boxesPerMorning=1;
        [Min(1)] public float minimumDistance=7,maximumDistance=10;
        [Range(0,100)] public float shinyChancePercent=10;
        [Min(0)] public float shinyAbilityBonusPercent=20;
        [Min(.1f)] public float pickupRadius=1.6f;
        [Min(0)] public float pickupDelay=.75f;
        [Header("Sky drop")]
        [Min(.1f)]public float dropHeight=18,dropGravity=25,maximumFallSpeed=30;
        public LayerMask groundLayers=~0;
        private void OnValidate(){minimumDistance=Mathf.Max(1,minimumDistance);maximumDistance=Mathf.Max(minimumDistance,maximumDistance);}
    }
}
