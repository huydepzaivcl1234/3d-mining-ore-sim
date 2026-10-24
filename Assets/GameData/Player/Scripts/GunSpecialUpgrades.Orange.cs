using System.Collections.Generic;
using MiningSimulator.Ores;
using UnityEngine;
namespace GameData.Player.Script
{
    public sealed partial class GunSpecialUpgrades
    {
        private readonly Dictionary<MiningCharacterHealth,float> etherealCooldowns=new();
        private readonly Dictionary<MiningCharacterHealth,float> etherealWindows=new();
        private readonly List<MiningCharacterHealth> expiredEthereal=new();
        private readonly List<CannonRound> cannonRounds=new();
        private float reprieveRemaining,reprieveReady,reprieveHealingRate,nextEtherealPrune;
        private Material cannonMaterial;
        public bool OrangeAllUnlocked=>progress.cantDie&&progress.cannon&&progress.etherealWeapon;
        public bool ReprieveActive=>reprieveRemaining>0;
        public float ReprieveCooldown=>Mathf.Max(0,reprieveReady-Time.time);
        public int CannonShots{get;private set;}
        public bool TryPreventDeath()
        {
            if(!isActiveAndEnabled||!progress.cantDie||!data||!data.orange||!data.orange.cantDie||Time.time<reprieveReady)return false;
            var s=data.orange.cantDie;
            reprieveRemaining=Mathf.Max(.1f,s.recoverySeconds);reprieveReady=Time.time+Mathf.Max(0,s.cooldownSeconds);
            float max=playerHealth?playerHealth.MaxHealth:1;
            reprieveHealingRate=Mathf.Max(0,max*s.recoveryHealthFraction-Mathf.Min(1,max))/reprieveRemaining;
            return true;
        }
        private bool TryEthereal(MiningCharacterHealth victim)
        {
            if(!data.orange||!data.orange.etherealWeapon)return false;
            if(!etherealWindows.TryGetValue(victim,out float end)||Time.time>=end)return false;
            if(etherealCooldowns.TryGetValue(victim,out float ready)&&Time.time<ready)return false;
            etherealCooldowns[victim]=Time.time+Mathf.Max(.01f,data.orange.etherealWeapon.perTargetCooldown);
            return true;
        }
        private void OpenEthereal(MiningCharacterHealth victim)
        {
            var s=data.orange?data.orange.etherealWeapon:null;
            if(!progress.etherealWeapon||!s||!victim.IsMonster||victim.Health<=0)return;
            if(etherealWindows.TryGetValue(victim,out float end)&&Time.time<end+Mathf.Max(0,s.reactivationCooldown))return;
            etherealWindows[victim]=Time.time+Mathf.Max(.01f,s.activeSeconds);
        }
        private void TryCannon(MiningCharacterHealth victim,float damage)
        {
            var s=data.orange?data.orange.cannon:null;
            if(!s||s.chancePercent<=0||s.chancePercent<100&&Random.value*100>=s.chancePercent)return;
            CannonShots++;
            while(cannonRounds.Count>=Mathf.Max(1,s.maximumProjectiles))
            {var oldest=cannonRounds[0];cannonRounds.RemoveAt(0);if(oldest)oldest.Finish();}
            if(!cannonMaterial){cannonMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));cannonMaterial.SetColor("_BaseColor",s.color);}
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Cannon round";go.hideFlags=HideFlags.DontSaveInBuild;
            var collider=go.GetComponent<Collider>();collider.enabled=false;Destroy(collider);go.GetComponent<Renderer>().sharedMaterial=cannonMaterial;
            go.transform.localScale=Vector3.one*s.visualSize;
            var muzzle=GetComponent<PistolShooting>();
            go.transform.position=muzzle&&muzzle.Muzzle?muzzle.Muzzle.position:transform.position+Vector3.up*1.3f;
            var round=go.AddComponent<CannonRound>();round.Configure(victim,gameObject,s,damage*s.damageFraction);
            cannonRounds.Add(round);
        }
        private void TickOrange(float dt)
        {
            if(ReprieveActive&&dt>0)
            {
                float elapsed=Mathf.Min(dt,reprieveRemaining);
                if(playerHealth)playerHealth.Heal(reprieveHealingRate*elapsed/Mathf.Max(.001f,playerHealth.HealingMultiplier));
                reprieveRemaining=Mathf.Max(0,reprieveRemaining-elapsed);
            }
            for(int i=cannonRounds.Count-1;i>=0;i--)if(!cannonRounds[i])cannonRounds.RemoveAt(i);
            if(Time.time<nextEtherealPrune)return;
            nextEtherealPrune=Time.time+1;expiredEthereal.Clear();
            float lockout=data&&data.orange&&data.orange.etherealWeapon?Mathf.Max(0,data.orange.etherealWeapon.reactivationCooldown):0;
            foreach(var item in etherealWindows)if(!item.Key||!item.Key.isActiveAndEnabled||item.Key.Health<=0||Time.time>=item.Value+lockout)expiredEthereal.Add(item.Key);
            foreach(var victim in expiredEthereal){etherealCooldowns.Remove(victim);etherealWindows.Remove(victim);}
        }
        private void ClearOrangeEffects()
        {
            reprieveRemaining=0;etherealCooldowns.Clear();etherealWindows.Clear();
            foreach(var round in cannonRounds)if(round)Destroy(round.gameObject);
            cannonRounds.Clear();if(cannonMaterial)Destroy(cannonMaterial);cannonMaterial=null;
        }
        private void ResetOrange(){ClearOrangeEffects();reprieveReady=0;}
    }
}


