using System.Collections.Generic;
using MiningSimulator.Ores;
using UnityEngine;

namespace GameData.Player.Script
{
    /// <summary>Sequential homing hits. Reuses visuals; visual limits never discard damage.</summary>
    public sealed class BulletFlyVolley
    {
        private sealed class Volley
        {
            public MiningCharacterHealth victim;
            public MushroomMonster monster;
            public LineRenderer[] lines;
            public float[] damage;
            public int index;
            public float wait, headHeight, age;
            public bool flying;
            public bool startedDamage;
            public float damageMultiplier;
            public Vector3 position;
        }
        public static bool ApplyingDamage { get; private set; }
        private readonly BlueGunUpgradeData data;
        private readonly GameObject owner;
        private readonly MiningPlayerStats playerStats;
        private readonly List<Volley> active=new();
        private readonly Stack<Volley> spare=new();
        private readonly Stack<LineRenderer> freeLines=new();
        private sealed class TargetProgress { public int survivingHits;public float reduction; }
        private readonly Dictionary<MiningCharacterHealth,TargetProgress> targetProgress=new();
        private readonly List<MiningCharacterHealth> expiredTargets=new();
        private readonly Queue<MiningCharacterHealth> pending=new();
        private float nextPrune;
        private Transform root;
        private Material material;
        private int lineCount;
        private float idleTime;
        public int VisualCount=>lineCount;
        public int PooledVisuals=>freeLines.Count;
        public int VolleysStarted { get; private set; }
        public int HitsApplied { get; private set; }
        public int ActiveVolleys=>active.Count;

        public BulletFlyVolley(BlueGunUpgradeData settings,GameObject source) { data=settings;owner=source;playerStats=source?source.GetComponentInParent<MiningPlayerStats>():null; }
        public void Play(MiningCharacterHealth victim)
        {
            if(data.bulletFly.bulletFlyDamage==null||data.bulletFly.bulletFlyDamage.Length==0)return;
            if(!victim||!victim.isActiveAndEnabled||victim.Health<=0||!owner)return;
            // Effect procs can request another volley during DealDamage. Defer it until
            // the next Step so it cannot remove/reuse the volley currently being hit-tested.
            if(ApplyingDamage){pending.Enqueue(victim);return;}
            while(active.Count>=Mathf.Max(1,data.bulletFly.maximumConcurrentVolleys))
            { FinishDamage(active[0]);Release(0); }
            idleTime=0;
            var v=spare.Count>0?spare.Pop():new Volley();
            int count=data.bulletFly.bulletFlyDamage.Length;
            if(v.lines==null||v.lines.Length!=count)v.lines=new LineRenderer[count];
            if(v.damage==null||v.damage.Length!=count)v.damage=new float[count];
            int level=playerStats?playerStats.Level:1;
            for(int i=0;i<count;i++)v.damage[i]=data.bulletFly.DamageAtLevel(i,level);
            v.age=0;
            v.startedDamage=false;v.damageMultiplier=1;
            v.victim=victim;v.monster=victim.GetComponent<MushroomMonster>();v.index=0;v.flying=false;
            v.wait=data.bulletFly.bulletFlyTellSeconds;
            var collider=victim.GetComponent<Collider>();
            v.headHeight=collider?Mathf.Max(0,collider.bounds.max.y-victim.transform.position.y):1.5f;
            if(victim.HealthBar)v.headHeight=Mathf.Max(v.headHeight,victim.HealthBar.position.y-victim.transform.position.y);
            for(int i=0;i<count;i++)
            {
                v.lines[i]=RentLine();
                Draw(v.lines[i],Above(v,i),Vector3.down);
            }
            active.Add(v);VolleysStarted++;
        }
        private LineRenderer RentLine()
        {
            if(freeLines.Count>0) { var reused=freeLines.Pop();reused.enabled=true;return reused; }
            if(lineCount>=Mathf.Max(1,data.bulletFly.bulletFlyVisibleLimit))return null;
            if(!root)
            {
                root=new GameObject("Bullet Fly visuals").transform;
                root.gameObject.hideFlags=HideFlags.DontSaveInBuild;
                if(owner)root.SetParent(owner.transform,false);
                material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetColor("_BaseColor",data.bulletFly.bulletFlyColor);
            }
            var go=new GameObject("Bullet Fly round");go.transform.SetParent(root,false);
            var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;
            line.positionCount=2;line.useWorldSpace=true;line.numCapVertices=4;
            line.startWidth=data.bulletFly.bulletFlyWidth;line.endWidth=data.bulletFly.bulletFlyWidth*.35f;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
            lineCount++;return line;
        }
        private Vector3 Above(Volley v,int i)
        {
            Vector3 right=owner?owner.transform.right:Vector3.right;
            return v.victim.transform.position+Vector3.up*(v.headHeight+data.bulletFly.bulletFlyHeight)+
                right*((i-(v.lines.Length-1)*.5f)*data.bulletFly.bulletFlySpacing);
        }
        private void Draw(LineRenderer line,Vector3 head,Vector3 direction)
        {
            if(!line)return;
            line.SetPosition(0,head-direction*data.bulletFly.bulletFlyLength);line.SetPosition(1,head);
        }
        public void Step(float dt)
        {
            if(dt<=0)return;
            int queued=pending.Count;
            for(int i=0;i<queued;i++)Play(pending.Dequeue());
            if(Time.time>=nextPrune)
            {
                nextPrune=Time.time+1;expiredTargets.Clear();
                foreach(var item in targetProgress)if(!item.Key||!item.Key.isActiveAndEnabled||item.Key.Health<=0)expiredTargets.Add(item.Key);
                foreach(var victim in expiredTargets)targetProgress.Remove(victim);
            }
            if(active.Count==0)
            {
                idleTime+=dt;
                if(idleTime>=data.bulletFly.idleCleanupSeconds){TrimPool();spare.Clear();}
                return;
            }
            for(int n=active.Count-1;n>=0;n--)
            {
                var v=active[n];
                if(!v.victim||!v.victim.isActiveAndEnabled||v.victim.Health<=0||v.monster&&v.monster.IsDespawning||!owner)
                { Release(n);continue; }
                v.age+=dt;
                if(v.age>=Mathf.Max(.1f,data.bulletFly.maximumFlightSeconds))
                { FinishDamage(v);Release(n);continue; }
                for(int i=v.index+(v.flying?1:0);i<v.lines.Length;i++)Draw(v.lines[i],Above(v,i),Vector3.down);
                if(!v.flying)
                {
                    v.wait-=dt;if(v.wait>0)continue;
                    v.flying=true;v.position=Above(v,v.index);
                }
                Vector3 target=v.victim.transform.position+Vector3.up*(v.headHeight*.65f);
                Vector3 direction=(target-v.position).normalized;
                v.position=Vector3.MoveTowards(v.position,target,Mathf.Max(.1f,data.bulletFly.bulletFlySpeed)*dt);
                Draw(v.lines[v.index],v.position,direction);
                if((target-v.position).sqrMagnitude>.0001f)continue;
                ApplyingDamage=true;
                try
                {
                    ApplyHit(v,v.index);
                }
                finally { ApplyingDamage=false; }
                ReturnLine(v.lines[v.index]);v.lines[v.index]=null;v.index++;v.flying=false;v.wait=data.bulletFly.bulletFlyInterval;
                if(v.index>=v.lines.Length||v.victim.Health<=0)Release(n);
            }
        }
        private void FinishDamage(Volley v)
        {
            ApplyingDamage=true;
            try
            {
                for(int i=v.index;i<v.damage.Length&&v.victim&&v.victim.isActiveAndEnabled&&v.victim.Health>0;i++)
                    ApplyHit(v,i);
            }
            finally{ApplyingDamage=false;}
        }
        private void ApplyHit(Volley v,int index)
        {
            if(!targetProgress.TryGetValue(v.victim,out var progress))targetProgress[v.victim]=progress=new TargetProgress();
            if(!v.startedDamage)
            {
                v.startedDamage=true;
                if(progress.survivingHits>=Mathf.Max(1,data.bulletFly.survivingHitsBeforeReduction))
                    progress.reduction=Mathf.Min(Mathf.Clamp01(data.bulletFly.maximumReduction),progress.reduction+Mathf.Max(0,data.bulletFly.reductionPerVolley));
                v.damageMultiplier=1-progress.reduction;
            }
            float dealt=v.victim.DealDamage(Mathf.Max(0,v.damage[index])*v.damageMultiplier,CombatDamageType.True,owner);
            if(dealt>0)
            {
                HitsApplied++;
                if(v.victim.Health>0&&progress.survivingHits<int.MaxValue)progress.survivingHits++;
            }
            if(!v.victim||!v.victim.isActiveAndEnabled||v.victim.Health<=0)targetProgress.Remove(v.victim);
        }
        private void ReturnLine(LineRenderer line)
        {
            if(!line)return;line.enabled=false;
            if(freeLines.Count<Mathf.Max(0,data.bulletFly.retainedVisuals))freeLines.Push(line);
            else{Object.Destroy(line.gameObject);lineCount--;}
        }
        private void TrimPool(){while(freeLines.Count>0){var line=freeLines.Pop();if(line)Object.Destroy(line.gameObject);lineCount--;}}
        private void Release(int n)
        {
            var v=active[n];
            for(int i=v.index;i<v.lines.Length;i++){ReturnLine(v.lines[i]);v.lines[i]=null;}
            v.victim=null;v.monster=null;spare.Push(v);active.RemoveAt(n);
        }
        public void Clear() { while(active.Count>0)Release(active.Count-1);TrimPool();spare.Clear();pending.Clear();targetProgress.Clear();idleTime=0; }
        public void Dispose()
        {
            Clear();freeLines.Clear();spare.Clear();
            if(root)Object.Destroy(root.gameObject);if(material)Object.Destroy(material);
        }
    }
}
