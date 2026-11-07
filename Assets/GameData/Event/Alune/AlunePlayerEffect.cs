using UnityEngine;

namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent,RequireComponent(typeof(CharacterHealth))]
    public sealed class AlunePlayerEffect : MonoBehaviour
    {
        public float slowRemaining,slowPercent,stunRemaining,pendingStun,tickSeconds,tickElapsed,tickPercent;
        private CharacterHealth health;
        public bool IsStunned=>stunRemaining>0;
        public float MovementMultiplier=>IsStunned?0:slowRemaining>0?1-Mathf.Clamp(slowPercent,0,99)*.01f:1;
        private void Awake()=>health=GetComponent<CharacterHealth>();
        public bool Apply(AluneEventData data,float sample)
        {
            if(!data||!health||health.Health<=0||slowRemaining>0||IsStunned)return false;
            slowPercent=Mathf.Lerp(Mathf.Min(data.minimumSlowPercent,data.maximumSlowPercent),Mathf.Max(data.minimumSlowPercent,data.maximumSlowPercent),Mathf.Clamp01(sample));
            slowRemaining=Mathf.Max(.01f,data.slowSeconds);pendingStun=Mathf.Max(.01f,data.stunSeconds);
            tickSeconds=Mathf.Max(.1f,data.tickSeconds);tickPercent=Mathf.Max(0,data.playerTickMaxHealthPercent);tickElapsed=0;
            return true;
        }
        private void Update()=>Step(Time.deltaTime);
        public void Step(float seconds)
        {
            if(!health||health.Health<=0){Clear();return;}
            float dt=Mathf.Max(0,seconds);
            if(slowRemaining>0){float used=Mathf.Min(dt,slowRemaining);slowRemaining-=used;dt-=used;if(slowRemaining<=0){stunRemaining=pendingStun;pendingStun=0;}}
            if(stunRemaining<=0)return;
            float active=Mathf.Min(dt,stunRemaining);stunRemaining-=active;tickElapsed+=active;
            while(tickSeconds>0&&tickElapsed+0.00001f>=tickSeconds){tickElapsed=Mathf.Max(0,tickElapsed-tickSeconds);health.DealPeriodicDamage(health.MaxHealth*tickPercent*.01f,CombatDamageType.Magic,null);if(health.Health<=0){Clear();break;}}
        }
        public void Clear()=>slowRemaining=slowPercent=stunRemaining=pendingStun=tickElapsed=0;
        private void OnDisable()=>Clear();
    }
}
