using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent]
    public sealed class AluneMeteorEvent : MonoBehaviour
    {
        [Serializable] public sealed class MeteorState {public Vector3 ground;public float age;public bool proximity,impacted;}
        [Serializable] public sealed class Saved {public float remaining,next;public List<MeteorState> meteors=new();}
        private sealed class Meteor {public MeteorState state;public GameObject root,rock,impact;public MonsterAttackWarning warning;}
        [SerializeField] private AluneEventData data;
        private CharacterHealth player;
        private float remaining,next;
        private readonly List<Meteor> meteors=new();
        private readonly List<CharacterHealth> victims=new();
        public bool IsActive=>remaining>0;
        public float Remaining=>remaining;
        public int FallingCount {get{int count=0;foreach(var m in meteors)if(m.root.activeSelf&&!m.state.impacted)count++;return count;}}
        public void Configure(AluneEventData settings,CharacterHealth target){data=settings;player=target;}
        public void Begin(){if(data&&!IsActive){remaining=Mathf.Max(1,data.duration);next=0;}}
        public void Step(float seconds)
        {
            if(!data||!player||player.Health<=0||RunCheckpoint.IsRestoring)return;
            float dt=Mathf.Max(0,seconds),active=Mathf.Min(dt,remaining);remaining=Mathf.Max(0,remaining-dt);
            if(active>0){next-=active;if(next<=0){next=Mathf.Max(.25f,data.meteorInterval);TrySpawn();}}
            foreach(var m in meteors){if(!m.root.activeSelf)continue;
                var s=m.state;float oldAge=s.age;s.age+=dt;
                float fall=Mathf.Max(1,data.fallSeconds);
                if(!s.impacted){
                    Vector3 start=s.ground+Vector3.up*Mathf.Max(3,data.fallHeight);
                    Vector3 previous=Vector3.Lerp(start,s.ground,Mathf.Clamp01(oldAge/fall));
                    Vector3 position=Vector3.Lerp(start,s.ground,Mathf.Clamp01(s.age/fall));
                    m.rock.transform.position=position;m.warning.SetProgress(s.age/fall);
                    // Sweep the falling segment so proximity is not skipped on a slow frame.
                    Vector3 point=player.transform.position+Vector3.up;
                    Vector3 segment=position-previous;float t=segment.sqrMagnitude>0?Mathf.Clamp01(Vector3.Dot(point-previous,segment)/segment.sqrMagnitude):0;
                    if(!s.proximity&&(point-(previous+segment*t)).sqrMagnitude<=data.proximityDistance*data.proximityDistance){
                        s.proximity=true;var effect=player.GetComponent<AlunePlayerEffect>();if(!effect)effect=player.gameObject.AddComponent<AlunePlayerEffect>();effect.Apply(data,UnityEngine.Random.value);
                    }
                    if(s.age>=fall){s.impacted=true;m.rock.SetActive(false);m.warning.Hide();Impact(s.ground);if(m.impact){m.impact.SetActive(true);foreach(var ps in m.impact.GetComponentsInChildren<ParticleSystem>())ps.Play(true);}}
                }
                if(s.impacted&&s.age>=fall+Mathf.Max(.1f,data.impactVisualSeconds))m.root.SetActive(false);
            }
        }
        private void TrySpawn()
        {
            Vector2 offset=UnityEngine.Random.insideUnitCircle*Mathf.Max(0,data.scatterRadius);
            Vector3 point=player.transform.position+new Vector3(offset.x,0,offset.y);
            var grid=WorldNavigationGrid.Instance;if(!grid||!grid.TryGetGroundPoint(point,out point))return;
            SpawnAt(point);
        }
        public void SpawnAt(Vector3 ground)
        {
            if(!data)return;
            var m=meteors.Find(x=>!x.root.activeSelf);
            if(m==null){
                m=new Meteor{root=new GameObject("Alune meteor (pooled)")};m.root.transform.SetParent(transform,false);
                m.rock=data.meteorPrefab?Instantiate(data.meteorPrefab,m.root.transform):GameObject.CreatePrimitive(PrimitiveType.Sphere);
                m.rock.transform.SetParent(m.root.transform,false);m.rock.name="Falling rock";
                foreach(var collider in m.rock.GetComponentsInChildren<Collider>(true))collider.enabled=false;
                foreach(var body in m.rock.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.detectCollisions=false;}
                m.warning=m.root.AddComponent<MonsterAttackWarning>();
                if(data.impactPrefab){m.impact=Instantiate(data.impactPrefab,m.root.transform);m.impact.SetActive(false);}
                meteors.Add(m);
            }
            m.state=new MeteorState{ground=ground};m.root.SetActive(true);m.rock.SetActive(true);
            m.rock.transform.localScale=Vector3.one*Mathf.Max(.1f,data.meteorSize);m.rock.transform.position=ground+Vector3.up*Mathf.Max(3,data.fallHeight);
            if(m.impact){m.impact.SetActive(false);m.impact.transform.position=ground;}
            m.warning.Show(ground,data.impactRadius,data.warningColor,data.warningShader,transform);
        }
        public void Impact(Vector3 ground)
        {
            float radius=Mathf.Max(.1f,data.impactRadius);victims.Clear();
            foreach(var h in CharacterHealth.ActiveMonsters)if(h&&h.isActiveAndEnabled&&h.Health>0&&InRadius(h,ground,radius))victims.Add(h);
            // Death removes entries from ActiveMonsters, so damage the snapshot, not its enumerator.
            foreach(var h in victims)if(h&&h.Health>0)h.DealDamage(h.MaxHealth*Mathf.Max(0,data.monsterMaxHealthPercent)*.01f,CombatDamageType.True,null);
            if(player&&player.Health>0&&InRadius(player,ground,radius))player.DealDamage(player.MaxHealth*Mathf.Max(0,data.playerImpactMaxHealthPercent)*.01f,CombatDamageType.Magic,null);
        }
        private static bool InRadius(CharacterHealth h,Vector3 ground,float radius){var d=h.transform.position-ground;return new Vector2(d.x,d.z).sqrMagnitude<=radius*radius&&Mathf.Abs(d.y)<=radius+1;}
        public Saved Capture(){var saved=new Saved{remaining=remaining,next=next};foreach(var m in meteors)if(m.root.activeSelf)saved.meteors.Add(new MeteorState{ground=m.state.ground,age=m.state.age,proximity=m.state.proximity,impacted=m.state.impacted});return saved;}
        public void Restore(Saved saved){Clear();if(saved==null||!data)return;remaining=Mathf.Max(0,saved.remaining);next=Mathf.Max(0,saved.next);foreach(var s in saved.meteors){var m=meteors.Find(x=>!x.root.activeSelf);SpawnAt(s.ground);if(m==null)m=meteors[meteors.Count-1];m.state=s;m.rock.transform.position=s.ground+Vector3.up*Mathf.Max(3,data.fallHeight)*(1-Mathf.Clamp01(s.age/Mathf.Max(1,data.fallSeconds)));m.rock.SetActive(!s.impacted);if(s.impacted){m.warning.Hide();if(m.impact){m.impact.SetActive(true);foreach(var ps in m.impact.GetComponentsInChildren<ParticleSystem>())ps.Simulate(Mathf.Max(0,s.age-data.fallSeconds),true,true);}}else m.warning.SetProgress(s.age/Mathf.Max(1,data.fallSeconds));}}
        public void Clear(){remaining=next=0;foreach(var m in meteors)if(m.root)m.root.SetActive(false);}
        private void OnDisable()=>Clear();
    }
}
