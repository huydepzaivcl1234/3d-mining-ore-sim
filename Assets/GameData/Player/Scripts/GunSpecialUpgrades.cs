using MiningSimulator.Ores;
using UnityEngine;
namespace GameData.Player.Script
{
    public enum GunSpecialChoice { DoubleHit, SharpRounds, Nail, FasterMatter, BulletFly, Hunter, BloodTears, GiantBomb, CantDie, Cannon, EtherealWeapon }
    [DisallowMultipleComponent]
    public sealed partial class GunSpecialUpgrades : MonoBehaviour
    {
        [SerializeField] private BlueGunUpgradeData data;
        [System.Serializable] private sealed class Progress { public int version=1; public bool doubleHit,sharpRounds,nail,fasterMatter,bulletFly,hunter,bloodTears,giantBomb,cantDie,cannon,etherealWeapon;public int hunterKills,hunterHealth; }
        private Progress progress=new(); private bool saveBlocked;
        private BulletFlyVolley bulletFly;
        private GiantBombWaves bombWaves;
        private MiningCharacterHealth playerHealth;
        private PistolShooting shooting;
        private bool progressDirty;
        private readonly System.Collections.Generic.Dictionary<MiningCharacterHealth,int> bombHits=new();
        private readonly System.Collections.Generic.List<MiningCharacterHealth> expiredBombTargets=new();
        private float nextBombPrune;
        private const string Key="Player.GunSpecialUpgrades.v1";
        public BlueGunUpgradeData Data=>data;
        public bool AllUnlocked=>progress.doubleHit&&progress.sharpRounds&&progress.nail&&progress.fasterMatter&&progress.bulletFly&&progress.hunter&&progress.bloodTears&&progress.giantBomb;
        public int HunterBonusHealth=>data&&progress.hunter?Mathf.Clamp(progress.hunterHealth,0,Mathf.Max(0,data.hunter.hunterMaxHealth)):0;
        public float FireRateMultiplier=>1f+(data&&progress.fasterMatter?Mathf.Max(0,data.fasterMatter.fireRateBonusPercent)*.01f:0f);
        public void ResetProgress()
        {
            progress = new Progress(); saveBlocked = progressDirty = false; GameSave.DeleteKey(Key); bulletFly?.Clear();bombWaves?.Clear();bombHits.Clear();
            ResetOrange();
        }
        public bool IsUnlocked(GunSpecialChoice choice)=>choice switch {
            GunSpecialChoice.DoubleHit=>progress.doubleHit, GunSpecialChoice.SharpRounds=>progress.sharpRounds,
            GunSpecialChoice.Nail=>progress.nail, GunSpecialChoice.FasterMatter=>progress.fasterMatter,
            GunSpecialChoice.BulletFly=>progress.bulletFly,GunSpecialChoice.Hunter=>progress.hunter,
            GunSpecialChoice.BloodTears=>progress.bloodTears,GunSpecialChoice.GiantBomb=>progress.giantBomb,
            GunSpecialChoice.CantDie=>progress.cantDie,GunSpecialChoice.Cannon=>progress.cannon,GunSpecialChoice.EtherealWeapon=>progress.etherealWeapon, _=>false };
        private void OnEnable(){MiningCharacterHealth.AnyDamaged+=OnAnyDamage;MiningCharacterHealth.AnyDied+=OnAnyDeath;}
        private void OnDisable(){MiningCharacterHealth.AnyDamaged-=OnAnyDamage;MiningCharacterHealth.AnyDied-=OnAnyDeath;bulletFly?.Clear();bombWaves?.Clear();bombHits.Clear();ClearOrangeEffects();FlushProgress();}
        private void OnDestroy(){bulletFly?.Dispose();bombWaves?.Dispose();}
        private void Update()
        {
            bulletFly?.Step(Time.deltaTime);bombWaves?.Step(Time.deltaTime);
            TickOrange(Time.deltaTime);
            if(bombHits.Count==0||Time.time<nextBombPrune)return;
            nextBombPrune=Time.time+1;expiredBombTargets.Clear();
            foreach(var pair in bombHits)if(!pair.Key||!pair.Key.isActiveAndEnabled)expiredBombTargets.Add(pair.Key);
            foreach(var victim in expiredBombTargets)bombHits.Remove(victim);
        }
        private void SaveProgress(){if(saveBlocked)return;GameSave.SetString(Key,JsonUtility.ToJson(progress));progressDirty=true;}
        private void FlushProgress(){if(progressDirty){GameSave.Save();progressDirty=false;}}
        private void OnApplicationPause(bool paused){if(paused)FlushProgress();}
        private void OnApplicationQuit()=>FlushProgress();
        private void OnAnyDeath(MiningCharacterHealth victim,GameObject source)
        {
            bombHits.Remove(victim);
            etherealCooldowns.Remove(victim);
            etherealWindows.Remove(victim);
            if(!data||saveBlocked||!progress.hunter||!victim.IsMonster||!MonsterKillCredit.IsPlayerOwned(source)||HunterBonusHealth>=data.hunter.hunterMaxHealth)return;
            progress.hunterKills++;
            if(progress.hunterKills>=Mathf.Max(1,data.hunter.hunterKillsPerReward))
            {
                progress.hunterKills=0;
                progress.hunterHealth=(int)System.Math.Min(data.hunter.hunterMaxHealth,(long)HunterBonusHealth+Mathf.Max(0,data.hunter.hunterHealthPerReward));
                SaveProgress();FlushProgress();
            }
            else SaveProgress();
        }
        private void OnAnyDamage(MiningCharacterHealth victim,float actualDamage,GameObject source,bool basicAttack,CombatDamageType type)
        {
            if(!data||!victim||!victim.IsMonster)return;
            // Reserve before nested damage listeners; one independent gate per victim.
            bool ethereal=progress.etherealWeapon&&!basicAttack&&victim.Health>0&&MonsterKillCredit.IsPlayerOwned(source)&&TryEthereal(victim);
            MiningDamagePopup.ShowForTarget(actualDamage,victim,data.damagePopup,type,victim.IsPeriodicDamage);
            if(progress.bloodTears&&playerHealth&&MonsterKillCredit.IsPlayerOwned(source))playerHealth.Heal(actualDamage*data.bloodTears.bloodTearsHealFraction);
            if(progress.giantBomb&&!GiantBombWaves.ApplyingDamage)
            {
                bombHits.TryGetValue(victim,out int count);
                if(++count>=Mathf.Max(1,data.giantBomb.giantBombHits))
                {
                    bombHits.Remove(victim);bombWaves??=new GiantBombWaves(data,gameObject);
                    bombWaves.Play(victim.transform.position,data.giantBomb.giantBombBaseDamage+(shooting?shooting.EffectiveDamage:0));
                }
                else bombHits[victim]=count;
            }
            if(ethereal)OnBulletHit(victim,shooting?shooting.EffectiveDamage:actualDamage,actualDamage,victim.transform.position,true);
        }
        private void Awake()
        {
            playerHealth=GetComponent<MiningCharacterHealth>();shooting=GetComponent<PistolShooting>();
            if(!GameSave.HasKey(Key))return;
            try{var loaded=JsonUtility.FromJson<Progress>(GameSave.GetString(Key));if(loaded==null||loaded.version!=1){saveBlocked=true;return;}progress=loaded;}
            catch(System.ArgumentException){saveBlocked=true;Debug.LogWarning("Gun upgrade save unreadable; preserved.",this);}
        }
        public bool Unlock(GunSpecialChoice choice)
        {
            if(saveBlocked||IsUnlocked(choice))return false;
            switch(choice){case GunSpecialChoice.DoubleHit:progress.doubleHit=true;break;case GunSpecialChoice.SharpRounds:progress.sharpRounds=true;break;case GunSpecialChoice.Nail:progress.nail=true;break;case GunSpecialChoice.FasterMatter:progress.fasterMatter=true;break;case GunSpecialChoice.BulletFly:progress.bulletFly=true;break;case GunSpecialChoice.Hunter:progress.hunter=true;break;case GunSpecialChoice.BloodTears:progress.bloodTears=true;break;case GunSpecialChoice.GiantBomb:progress.giantBomb=true;break;case GunSpecialChoice.CantDie:progress.cantDie=true;break;case GunSpecialChoice.Cannon:progress.cannon=true;break;case GunSpecialChoice.EtherealWeapon:progress.etherealWeapon=true;break;default:return false;}
            SaveProgress();FlushProgress();return true;
        }
        public void OnBulletHit(MiningCharacterHealth victim,float bulletDamage,float actualDamage,Vector3 point,bool fromEffect=false)
        {
            if(!data||!victim||actualDamage<=0)return;
            if(!fromEffect)OpenEthereal(victim);
            if(progress.cannon&&victim.IsMonster&&victim.Health>0)TryCannon(victim,bulletDamage);
            // Only the direct weapon hit dispatches this callback; damage effects never do.
            if(progress.bulletFly&&victim.IsMonster&&victim.Health>0)
            {
                bulletFly??=new BulletFlyVolley(data,gameObject);
                bulletFly.Play(victim);
            }
            if(progress.doubleHit)
            {
                MushroomMonster nearest=null;float distance=data.doubleHit.splashRadius*data.doubleHit.splashRadius;
                foreach(var monster in MushroomMonster.Monsters)
                {
                    if(!monster||monster.IsDespawning||!monster.Health||monster.Health==victim||monster.Health.Health<=0)continue;
                    float d=(monster.transform.position-victim.transform.position).sqrMagnitude;
                    if(d<distance){distance=d;nearest=monster;}
                }
                if(nearest)nearest.Health.DealDamage(bulletDamage*data.doubleHit.splashDamageFraction,CombatDamageType.Physical,gameObject);
            }
            if(victim.Health<=0||(!progress.sharpRounds&&!progress.nail))return;
            var effects=victim.GetComponent<GunVictimEffects>();if(!effects)effects=victim.gameObject.AddComponent<GunVictimEffects>();
            effects.Configure(victim,data,gameObject);
            if(progress.nail)effects.MarkNail();
            if(progress.sharpRounds)effects.RefreshBleed(bulletDamage*data.sharpRounds.bleedDamageFraction);
        }
    }
}
