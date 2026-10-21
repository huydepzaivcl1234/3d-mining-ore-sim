using System.Collections.Generic;
using MiningSimulator.Ores;
using UnityEngine;

namespace GameData.Player.Script
{
    public sealed class GiantBombWaves
    {
        private sealed class Wave
        {
            public LineRenderer line;
            public Vector3 center;
            public float radius,damage;
            public Vector3[] points;
            public readonly List<MiningCharacterHealth> targets=new();
            public readonly HashSet<MiningCharacterHealth> hit=new();
        }
        public static bool ApplyingDamage { get; private set; }
        private readonly BlueGunUpgradeData data;
        private readonly GameObject owner;
        private readonly List<Wave> active=new();
        private readonly Stack<Wave> pool=new();
        private Transform root;
        private Material material;
        public int WavesStarted { get; private set; }
        public GiantBombWaves(BlueGunUpgradeData settings,GameObject source){data=settings;owner=source;}
        public void Play(Vector3 center,float damage)
        {
            if(!root)
            {
                root=new GameObject("Giant Bomb waves").transform;root.gameObject.hideFlags=HideFlags.DontSave;
                if(owner)root.SetParent(owner.transform,false);
                material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",data.giantBomb.giantBombColor);
            }
            Wave wave;
            if(pool.Count>0)wave=pool.Pop();
            else
            {
                wave=new Wave();var go=new GameObject("Giant Bomb ring");go.transform.SetParent(root,false);
                wave.line=go.AddComponent<LineRenderer>();wave.line.sharedMaterial=material;
                wave.line.useWorldSpace=true;
                wave.line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;wave.line.receiveShadows=false;
            }
            int count=Mathf.Clamp(data.giantBomb.giantBombSegments,8,128)+1;
            if(wave.points==null||wave.points.Length!=count)wave.points=new Vector3[count];
            wave.line.positionCount=count;
            wave.center=center;wave.radius=0;wave.damage=Mathf.Max(0,damage);wave.hit.Clear();wave.targets.Clear();
            wave.targets.AddRange(MiningCharacterHealth.ActiveMonsters);
            // The carrier can die/disable its health during this damage event; its center still emits the wave.
            wave.line.enabled=false;active.Add(wave);WavesStarted++;
        }
        public void Step(float dt)
        {
            if(dt<=0)return;
            if(material)material.SetColor("_BaseColor",data.giantBomb.giantBombColor);
            for(int n=active.Count-1;n>=0;n--)
            {
                var wave=active[n];float previous=wave.radius;
                wave.radius=Mathf.Min(data.giantBomb.giantBombRadius,wave.radius+Mathf.Max(.1f,data.giantBomb.giantBombWaveSpeed)*dt);
                for(int i=0;i<wave.points.Length;i++)
                {
                    float a=i*Mathf.PI*2/(wave.points.Length-1);
                    wave.points[i]=wave.center+Vector3.up*data.giantBomb.giantBombVisualHeight+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*wave.radius;
                }
                wave.line.SetPositions(wave.points);wave.line.startWidth=wave.line.endWidth=data.giantBomb.giantBombWidth;wave.line.enabled=true;
                foreach(var victim in wave.targets)
                {
                    if(!victim||!victim.isActiveAndEnabled||victim.Health<=0||wave.hit.Contains(victim))continue;
                    Vector3 delta=victim.transform.position-wave.center;
                    if(Mathf.Abs(delta.y)>data.giantBomb.giantBombVerticalRange)continue;
                    delta.y=0;float distance=delta.magnitude;
                    if(distance>data.giantBomb.giantBombRadius||distance> wave.radius+data.giantBomb.giantBombWidth||distance<previous-data.giantBomb.giantBombWidth)continue;
                    wave.hit.Add(victim);ApplyingDamage=true;
                    try{victim.DealDamage(wave.damage,data.giantBomb.giantBombDamageType,owner);}
                    finally{ApplyingDamage=false;}
                }
                if(wave.radius>=data.giantBomb.giantBombRadius)Release(n);
            }
        }
        private void Release(int n){var wave=active[n];wave.line.enabled=false;wave.targets.Clear();wave.hit.Clear();pool.Push(wave);active.RemoveAt(n);}
        public void Clear(){while(active.Count>0)Release(active.Count-1);}
        public void Dispose(){Clear();pool.Clear();if(root)Object.Destroy(root.gameObject);if(material)Object.Destroy(material);}
    }
}
