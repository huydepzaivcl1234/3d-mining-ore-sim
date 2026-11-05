using System;
using System.Collections.Generic;
using UnityEngine;
using GameData.Player.Script;
namespace MiningSimulator.Ores
{
    public static class RunCheckpoint
    {
        [Serializable] public sealed class Animation {public string path;public int layer,hash,next;public float time,nextTime,duration,transition,weight,speed;}
        [Serializable] public sealed class Particle {public string path;public float time;public uint seed;public bool playing;}
        [Serializable] public sealed class Actor {public string id;public float health;public Vector3 position,velocity,angularVelocity;public Quaternion rotation;public List<RunStateFields.Bag> fields;public List<SurvivorEnemyStatus.SavedCombo> combos;public bool camera;public Vector3 cameraPosition;public Quaternion cameraRotation;public float cameraFov;public bool ragdoll;public List<Body> bodies=new();public List<Animation> animations=new();public List<Particle> particles=new();public RunStateFields.Bag boss;public SurvivorAbilities.SavedAbilityVisuals abilities;public List<SurvivorAutoCombat.SavedBolt> bolts;}
        [Serializable] public sealed class Body {public string path;public Vector3 position,velocity,angular;public Quaternion rotation;}
        [Serializable] public sealed class Shot {public int kind;public string owner,target;public Vector3 position,scale;public Quaternion rotation;public RunStateFields.Bag fields;}
        [Serializable] public sealed class State {public int version=1;public List<MonsterItemPickup.SavedDrop> loot=new();public List<WorldItemPickup.SavedDrop> items=new();public string random;public List<MiningItemSystem.SavedEffect> effects;public List<Actor> actors=new();public List<Shot> shots=new();public List<MiningWorldCard.SavedCard> cards=new();}
        public static bool IsRestoring {get;set;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset()=>IsRestoring=false;
        static string Path(Transform child,Transform root){string path="";while(child&&child!=root){path=child.name+(path.Length>0?"/"+path:"");child=child.parent;}return path;}
        static Transform At(Transform root,string path)=>string.IsNullOrEmpty(path)?root:root.Find(path);
        public static State Capture(CharacterHealth player){
            var state=new State();var actors=RunStateFields.Actors();
            foreach(var pair in actors){var h=pair.Value;if(h.gameObject.scene!=player.gameObject.scene)continue;
                var a=new Actor{id=pair.Key,health=h.Health,position=h.transform.position,rotation=h.transform.rotation,fields=RunStateFields.CaptureComponents(h.gameObject)};
                if(h.TryGetComponent<MushroomMonster>(out var mushroom))a.boss=RunStateFields.Capture(RunStateFields.Get<BossSkillRuntime>(mushroom,"bossSkill"),"healUsed","healRemaining","strikes","hasteStacks","playerDamageProgress");
                if(h.TryGetComponent<Rigidbody>(out var rb)){a.velocity=rb.linearVelocity;a.angularVelocity=rb.angularVelocity;}
                if(h.TryGetComponent<TopDownSurvivor>(out var view)&&view.viewCamera){a.camera=true;a.cameraPosition=view.viewCamera.transform.position;a.cameraRotation=view.viewCamera.transform.rotation;a.cameraFov=view.viewCamera.fieldOfView;}
                if(h.TryGetComponent<SurvivorEnemyStatus>(out var status))a.combos=status.CaptureCombos();
                if(h.TryGetComponent<PlayerRagdoll>(out var ragdoll)&&ragdoll.IsRagdoll){a.ragdoll=true;foreach(var b in ragdoll.Bodies)if(b)a.bodies.Add(new(){path=Path(b.transform,h.transform),position=b.position,rotation=b.rotation,velocity=b.linearVelocity,angular=b.angularVelocity});}
                if(h.TryGetComponent<SurvivorAbilities>(out var ability))a.abilities=ability.CaptureRuntimeVisuals();
                if(h.TryGetComponent<SurvivorAutoCombat>(out var combat))a.bolts=combat.CaptureBolts();
                foreach(var animator in h.GetComponentsInChildren<Animator>()){if(!animator.runtimeAnimatorController)continue;for(int layer=0;layer<animator.layerCount;layer++){var current=animator.GetCurrentAnimatorStateInfo(layer);var next=animator.GetNextAnimatorStateInfo(layer);var transition=animator.GetAnimatorTransitionInfo(layer);a.animations.Add(new(){path=Path(animator.transform,h.transform),layer=layer,hash=current.fullPathHash,time=current.normalizedTime,next=animator.IsInTransition(layer)?next.fullPathHash:0,nextTime=next.normalizedTime,duration=transition.duration,transition=transition.normalizedTime,weight=animator.GetLayerWeight(layer),speed=animator.speed});}}
                foreach(var p in h.GetComponentsInChildren<ParticleSystem>())a.particles.Add(new(){path=Path(p.transform,h.transform),time=p.time,seed=p.randomSeed,playing=p.isPlaying});
                state.actors.Add(a);
            }
            void Shot(Component c,int kind,string[] names){var owner=RunStateFields.Get<MushroomMonster>(c,"owner");var victim=kind==1?RunStateFields.Get<CharacterHealth>(c,"victimHealth"):RunStateFields.Get<CharacterHealth>(c,"victim");if(!owner||!victim)return;state.shots.Add(new(){kind=kind,owner=RunStateFields.Id(owner.Health),target=RunStateFields.Id(victim),position=c.transform.position,rotation=c.transform.rotation,scale=c.transform.localScale,fields=RunStateFields.Capture(c,names)});}
            foreach(var p in UnityEngine.Object.FindObjectsByType<BreadSliceProjectile>(FindObjectsSortMode.None))Shot(p,0,new[]{"start","end","side","age","healing","healingFlight","resolved"});
            foreach(var p in UnityEngine.Object.FindObjectsByType<HomingMonsterProjectile>(FindObjectsSortMode.None))Shot(p,1,new[]{"age","launched","resolved"});
            foreach(var p in UnityEngine.Object.FindObjectsByType<PotionMonsterProjectile>(FindObjectsSortMode.None))Shot(p,2,new[]{"start","age","kind","resolved","yellowDamage"});
            var cardSystem=UnityEngine.Object.FindAnyObjectByType<MiningCardSystem>();
            foreach(var c in UnityEngine.Object.FindObjectsByType<MiningWorldCard>(FindObjectsSortMode.None))state.cards.Add(c.CaptureRuntime());
            foreach(var loot in UnityEngine.Object.FindObjectsByType<SurvivorDailyLoot>(FindObjectsSortMode.None))loot.SaveRuntime();
            foreach(var drop in UnityEngine.Object.FindObjectsByType<MonsterItemPickup>(FindObjectsSortMode.None))state.loot.Add(drop.CaptureRuntime());
            foreach(var drop in UnityEngine.Object.FindObjectsByType<WorldItemPickup>(FindObjectsSortMode.None))state.items.Add(drop.CaptureRuntime());
            state.random=JsonUtility.ToJson(UnityEngine.Random.state);var inventory=UnityEngine.Object.FindAnyObjectByType<MiningItemSystem>();if(inventory)state.effects=inventory.CaptureRuntimeEffects();return state;
        }
        public static void Restore(State state,CharacterHealth player){
            if(state==null)return;if(state.version!=1)throw new InvalidOperationException("Unsupported run checkpoint; preserve original save");
            var actors=RunStateFields.Actors();
            var inventory=UnityEngine.Object.FindAnyObjectByType<MiningItemSystem>();if(inventory)inventory.RestoreRuntimeEffects(state.effects);
            // First place all targets, then reconnect sources and projectiles. Loading never awards damage/kills.
            foreach(var a in state.actors){if(a.id==null||!actors.TryGetValue(a.id,out var h))continue;var motor=h.GetComponent<CharacterController>();bool was=motor&&motor.enabled;if(was)motor.enabled=false;h.transform.SetPositionAndRotation(a.position,a.rotation);if(was)motor.enabled=true;h.RestoreSavedHealth(a.health);if(h.TryGetComponent<Rigidbody>(out var rb)){rb.position=a.position;rb.rotation=a.rotation;if(!rb.isKinematic){rb.linearVelocity=a.velocity;rb.angularVelocity=a.angularVelocity;}}}
            foreach(var a in state.actors){if(a.id==null||!actors.TryGetValue(a.id,out var h))continue;RunStateFields.RestoreComponents(h.gameObject,a.fields,actors);
                if(h.TryGetComponent<SurvivorEnemyStatus>(out var status))status.RestoreCombos(a.combos,actors);
                if(h.TryGetComponent<SurvivorAbilities>(out var ability))ability.RestoreRuntimeVisuals(a.abilities,actors);
                if(h.TryGetComponent<SurvivorAutoCombat>(out var combat))combat.RestoreBolts(a.bolts,actors);
                if(a.boss!=null&&h.TryGetComponent<MushroomMonster>(out var mushroom))RunStateFields.Restore(RunStateFields.Get<BossSkillRuntime>(mushroom,"bossSkill"),a.boss,actors);
                if(h.TryGetComponent<GodBossController>(out var god)){float distance=RunStateFields.Get<float>(god,"waveDistance");if(distance>=0){typeof(GodBossController).GetMethod("StartWave",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(god,null);RunStateFields.Restore(god,a.fields.Find(b=>b.type==typeof(GodBossController).AssemblyQualifiedName),actors);}}
                if(h.TryGetComponent<PotionMonsterAbility>(out var potionAbility))potionAbility.RestoreHeldVisuals();h.RestoreEffectVisuals();if(h.TryGetComponent<PotionPlayerEffect>(out var potionStatus))potionStatus.RestoreVisuals();
                if(a.camera&&h.TryGetComponent<TopDownSurvivor>(out var view)&&view.viewCamera){view.viewCamera.transform.SetPositionAndRotation(a.cameraPosition,a.cameraRotation);view.viewCamera.fieldOfView=a.cameraFov;}
                foreach(var saved in a.animations){var t=At(h.transform,saved.path);var animator=t?t.GetComponent<Animator>():null;if(!animator||!animator.runtimeAnimatorController||saved.hash==0||!animator.HasState(saved.layer,saved.hash))continue;animator.speed=saved.speed;animator.SetLayerWeight(saved.layer,saved.weight);animator.Play(saved.hash,saved.layer,saved.time);if(saved.next!=0&&animator.HasState(saved.layer,saved.next))animator.CrossFade(saved.next,saved.duration,saved.layer,saved.nextTime,saved.transition);animator.Update(0);}
                if(a.ragdoll&&h.TryGetComponent<PlayerRagdoll>(out var ragdoll)){
                    var respawn=h.GetComponent<PlayerRespawn>();if(respawn)typeof(PlayerRespawn).GetMethod("HandleDeath",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(respawn,null);else ragdoll.Activate();
                    foreach(var b in a.bodies){var tr=At(h.transform,b.path);var body=tr?tr.GetComponent<Rigidbody>():null;if(body){body.position=b.position;body.rotation=b.rotation;body.linearVelocity=b.velocity;body.angularVelocity=b.angular;}}
                }
                foreach(var saved in a.particles){var t=At(h.transform,saved.path);var particle=t?t.GetComponent<ParticleSystem>():null;if(!particle)continue;particle.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);particle.useAutoRandomSeed=false;particle.randomSeed=saved.seed;particle.Simulate(saved.time,false,true,true);if(saved.playing)particle.Play(false);else particle.Pause(false);}
            }
            foreach(var p in UnityEngine.Object.FindObjectsByType<BreadSliceProjectile>(FindObjectsSortMode.None))SceneProjectilePool.Release(p.gameObject);
            foreach(var p in UnityEngine.Object.FindObjectsByType<HomingMonsterProjectile>(FindObjectsSortMode.None))SceneProjectilePool.Release(p.gameObject);
            foreach(var p in UnityEngine.Object.FindObjectsByType<PotionMonsterProjectile>(FindObjectsSortMode.None))SceneProjectilePool.Release(p.gameObject);
            foreach(var s in state.shots){if(s.owner==null||s.target==null||!actors.TryGetValue(s.owner,out var owner)||!actors.TryGetValue(s.target,out var target))continue;var m=owner.GetComponent<MushroomMonster>();if(!m)continue;Component c=null;
                if(s.kind==0&&m.SpeciesData is BreadMonsterData bread&&bread.sliceProjectile){var p=SceneProjectilePool.Rent<BreadSliceProjectile>(bread.sliceProjectile.gameObject,s.position,s.rotation,player.gameObject.scene,32);p.Launch(m,target.transform);c=p;}
                else if(s.kind==1&&m.CombatData.projectile){var p=SceneProjectilePool.Rent<HomingMonsterProjectile>(m.CombatData.projectile.gameObject,s.position,s.rotation,player.gameObject.scene,32);p.Launch(m,target.transform);c=p;}
                else if(s.kind==2&&m.SpeciesData is PotionMonsterData potion){int kind=s.fields.values.Find(v=>v.name=="kind").integer;var prefab=kind==0?potion.greenPotion:kind==1?potion.redPotion:potion.yellowPotion;if(!prefab)continue;var p=SceneProjectilePool.Rent<PotionMonsterProjectile>(prefab,s.position,s.rotation,player.gameObject.scene,32);p.Launch(m,target,kind);c=p;}
                if(c){RunStateFields.Restore(c,s.fields,actors);c.transform.localScale=s.scale;c.gameObject.SetActive(true);}
            }
            var cards=UnityEngine.Object.FindAnyObjectByType<MiningCardSystem>();if(cards){foreach(var c in UnityEngine.Object.FindObjectsByType<MiningWorldCard>(FindObjectsSortMode.None)){c.gameObject.SetActive(false);UnityEngine.Object.Destroy(c.gameObject);}foreach(var c in state.cards)MiningWorldCard.RestoreRuntime(cards,c);}
            foreach(var drop in UnityEngine.Object.FindObjectsByType<MonsterItemPickup>(FindObjectsSortMode.None)){drop.gameObject.SetActive(false);UnityEngine.Object.Destroy(drop.gameObject);}
            foreach(var drop in UnityEngine.Object.FindObjectsByType<WorldItemPickup>(FindObjectsSortMode.None)){drop.gameObject.SetActive(false);UnityEngine.Object.Destroy(drop.gameObject);}
            foreach(var drop in state.loot)MonsterItemPickup.RestoreRuntime(drop,player);
            foreach(var drop in state.items)WorldItemPickup.RestoreRuntime(drop);
            if(!string.IsNullOrEmpty(state.random))UnityEngine.Random.state=JsonUtility.FromJson<UnityEngine.Random.State>(state.random);
            Physics.SyncTransforms();
        }
    }
}
