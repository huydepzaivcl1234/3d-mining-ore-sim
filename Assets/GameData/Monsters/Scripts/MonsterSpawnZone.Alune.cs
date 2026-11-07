using UnityEngine;
namespace MiningSimulator.Ores
{
    public sealed partial class MonsterSpawnZone
    {
        private AluneMeteorEvent alune;
        private AluneMeteorEvent Alune
        {
            get{
                if(!alune){alune=GetComponent<AluneMeteorEvent>();if(!alune)alune=gameObject.AddComponent<AluneMeteorEvent>();}
                alune.Configure(dailyEvents?dailyEvents.alune:null,player);return alune;
            }
        }
        private void TickAlune(float dt)
        {
            if(!alune)return;
            alune.Step(dt);
            if(CurrentDailyEvent==DailyEncounterEvent.Alune&&!alune.IsActive)CurrentDailyEvent=DailyEncounterEvent.Normal;
        }
    }
}
