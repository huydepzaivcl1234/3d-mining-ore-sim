using UnityEngine;
namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent,RequireComponent(typeof(CharacterHealth))]
    public sealed class PotionPlayerEffect : MonoBehaviour
    {
        public readonly DamageOverTime burn=new();
        public float slowRemaining, slowPercent;
        public GameObject source;
        private CharacterHealth health;
        private GameObject burnVisual;
        public float AttackSpeedMultiplier=>slowRemaining>0?1-Mathf.Clamp01(slowPercent*.01f):1;
        public void ApplyRed(PotionMonsterData data,GameObject attacker){source=attacker;burn.Apply(data.BurnDamage(GetComponent<CharacterHealth>().MaxHealth),data.burnTickSeconds,data.burnDuration);}
        public void ApplyYellow(PotionMonsterData data,int level,GameObject attacker,float damageOverride=-1){
            GetComponent<CharacterHealth>().DealDamage(damageOverride>=0?damageOverride:data.YellowDamage(level),CombatDamageType.Magic,attacker);
            slowPercent=data.attackSpeedSlowPercent;slowRemaining=data.slowDuration;
        }
        private void Awake()=>health=GetComponent<CharacterHealth>();
        private void Update(){if(!health||health.Health<=0){burn.Clear();slowRemaining=0;return;}slowRemaining=Mathf.Max(0,slowRemaining-Time.deltaTime);burn.Tick(Time.deltaTime,DealBurn);RestoreVisuals();}
        public void RestoreVisuals()=>GameData.Player.Script.SurvivorLoopVfx.Set(ref burnVisual,GameData.Player.Script.SurvivorAbilityData.Fallback.common.ignite.visual,transform,burn.IsActive);
        private void DealBurn(float damage)=>health.DealPeriodicDamage(damage,CombatDamageType.True,source);
        private void OnDisable(){burn.Clear();slowRemaining=0;source=null;if(burnVisual)burnVisual.SetActive(false);}
    }
}
