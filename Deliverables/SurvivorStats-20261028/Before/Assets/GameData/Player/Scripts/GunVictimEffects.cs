using MiningSimulator.Ores;
using UnityEngine;
namespace GameData.Player.Script
{
    [DisallowMultipleComponent]
    public sealed class GunVictimEffects : MonoBehaviour
    {
        private MiningCharacterHealth health;private BlueGunUpgradeData data;private GameObject source;
        private double nailStacks;private bool applyingNail;
        private float bleedDamage,bleedRemaining,nextTick;
        public double NailStacks=>nailStacks;
        public void Configure(MiningCharacterHealth victim,BlueGunUpgradeData settings,GameObject owner)
        { if(health!=victim){if(health)health.Damaged-=OnDamaged;health=victim;health.Damaged+=OnDamaged;}data=settings;source=owner; }
        public void MarkNail(){if(nailStacks==0&&health.Health>0)nailStacks=1;}
        public void RefreshBleed(float damage)
        {bleedDamage=Mathf.Max(bleedDamage,damage);bleedRemaining=data.sharpRounds.bleedDuration;if(nextTick<=0)nextTick=data.sharpRounds.bleedTickInterval;}
        private void OnDamaged()
        {
            if(applyingNail||nailStacks<=0||!health||health.Health<=0||!data)return;
            applyingNail=true;
            try {
                float amount=(float)System.Math.Min(float.MaxValue,nailStacks*health.MaxHealth*data.nail.nailMaxHealthPercent*.01);
                health.DealDamage(amount,CombatDamageType.Magic,source);
                if(health.Health>0)nailStacks+=1d;
            }finally{applyingNail=false;}
        }
        private void Update()
        {
            if(!health||health.Health<=0||!data){enabled=false;return;}
            if(bleedRemaining<=0||Time.deltaTime<=0)return;
            float dt=Mathf.Min(Time.deltaTime,bleedRemaining);bleedRemaining-=dt;nextTick-=dt;
            while(nextTick<=.00001f&&health.Health>0){nextTick+=Mathf.Max(.1f,data.sharpRounds.bleedTickInterval);health.DealPeriodicDamage(bleedDamage,CombatDamageType.Physical,source);}
            if(bleedRemaining<=0){bleedDamage=0;nextTick=0;}
        }
        private void OnDisable(){bleedRemaining=bleedDamage=nextTick=0;}
        private void OnDestroy(){if(health)health.Damaged-=OnDamaged;}
    }
}
