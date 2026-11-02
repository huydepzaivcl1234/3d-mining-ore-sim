using UnityEngine;

namespace MiningSimulator.Ores
{
    public enum DailyEncounterEvent { Normal = 0, NightOnly = 1, BossInvasion = 2 }
    public enum DailyEncounterOverride { Random = -1, Normal = 0, NightOnly = 1, BossInvasion = 2 }

    [CreateAssetMenu(menuName = "Mining Simulator/Daily Encounter Events")]
    public sealed class DailyEncounterEventData : ScriptableObject
    {
        [Header("Monster population")]
        [HideInInspector,Min(0)]public int dailyMonsterCount=1;
        [HideInInspector,Min(0)]public int extraMonstersPerDay=1;
        [HideInInspector,Min(0)]public int dailyMonsterLimit;
        [HideInInspector,Min(1)]public int maximumLivingMonsters=50;
        public int MonsterCount(int day){long count=System.Math.Max(0,dailyMonsterCount)+(System.Math.Max(1L,day)-1)*System.Math.Max(0,extraMonstersPerDay);if(dailyMonsterLimit>0)count=System.Math.Min(count,dailyMonsterLimit);return (int)System.Math.Min(int.MaxValue,count);}
        public bool enabled = true;
        [HideInInspector,Range(0, 100)] public float nightOnlyChancePercent = 15f;
        [Range(0, 100)] public float bossInvasionChancePercent = 5f;
        [Min(0)] public float bossExtraSizePercent = 50f;
        [Min(0)] public float bossExtraDamagePercent = 30f;
        [Min(0)] public float bossExtraHealthPercent = 50f;
        public Sprite nightOnlyIcon, bossInvasionIcon;
        [Tooltip("Force an event for testing; boss level/species gates remain controlled by existing debug settings.")]
        public DailyEncounterOverride debugOverride = DailyEncounterOverride.Random;

        public DailyEncounterEvent Roll(float sample, bool bossEligible)
        {
            if (!enabled) return DailyEncounterEvent.Normal;
            if (debugOverride != DailyEncounterOverride.Random)
                return debugOverride == DailyEncounterOverride.BossInvasion && !bossEligible
                    ? DailyEncounterEvent.Normal : (DailyEncounterEvent)debugOverride;
            float night = Mathf.Clamp(nightOnlyChancePercent, 0, 100);
            float boss = bossEligible ? Mathf.Clamp(bossInvasionChancePercent, 0, 100 - night) : 0;
            float roll = Mathf.Clamp01(sample) * 100;
            return roll < night ? DailyEncounterEvent.NightOnly :
                roll < night + boss ? DailyEncounterEvent.BossInvasion : DailyEncounterEvent.Normal;
        }
    }
}

