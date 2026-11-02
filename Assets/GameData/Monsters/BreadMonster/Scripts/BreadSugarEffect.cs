using UnityEngine;

namespace MiningSimulator.Ores
{
    // Separate from Ignite: no fire VFX, strongest tick refreshes without stacking copies.
    [DisallowMultipleComponent,RequireComponent(typeof(CharacterHealth))]
    public sealed class BreadSugarEffect : MonoBehaviour
    {
        private readonly DamageOverTime effect=new();
        private CharacterHealth health;
        private GameObject source;
        public bool IsActive=>effect.IsActive;
        public void Apply(float damage,float interval,float seconds,GameObject attacker){
            health??=GetComponent<CharacterHealth>();
            if(health.Health<=0)return;
            if(!effect.IsActive||damage/Mathf.Max(.1f,interval)>=effect.DamagePerSecond)source=attacker;
            effect.Apply(damage,interval,seconds);
        }
        private void Update(){if(RuneStation.PlayerUsesRuneTime)return;Tick(Time.deltaTime);}
        public void Tick(float seconds){
            if(!health||health.Health<=0){effect.Clear();return;}
            effect.Tick(seconds,Deal);
        }
        private void Deal(float damage){if(health&&health.Health>0)health.DealPeriodicDamage(damage,CombatDamageType.Magic,source);}
        private void OnDisable(){effect.Clear();source=null;}
    }
}
