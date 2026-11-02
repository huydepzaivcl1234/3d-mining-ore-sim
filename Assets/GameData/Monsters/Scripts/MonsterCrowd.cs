using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    // One spatial snapshot per frame; no per-enemy overlap query or all-pairs scan.
    internal static class MonsterCrowd
    {
        private struct Agent { public MushroomMonster owner; public Vector3 position; public float radius; public int next; }
        private const float CellSize=2.5f;
        private static readonly Dictionary<Vector2Int,int> cells=new();
        private static readonly List<Agent> agents=new();
        private static int frame=-1;
        private static float largestRadius;
        private static readonly Unity.Profiling.ProfilerMarker Marker=new("Combat.CrowdSeparation");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){cells.Clear();agents.Clear();frame=-1;}
        private static Vector2Int Cell(Vector3 p)=>new(Mathf.FloorToInt(p.x/CellSize),Mathf.FloorToInt(p.z/CellSize));
        private static void Snapshot()
        {
            if(frame==Time.frameCount)return;
            frame=Time.frameCount;cells.Clear();agents.Clear();largestRadius=0;
            foreach(var actor in MushroomMonster.Monsters){
                if(!actor||!actor.CrowdActive)continue;
                var motor=actor.CrowdMotor;if(!motor||!motor.enabled)continue;
                var pos=actor.transform.position;var scale=actor.transform.lossyScale;
                float radius=motor.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
                var key=Cell(pos);int previous=cells.TryGetValue(key,out var head)?head:-1;
                cells[key]=agents.Count;agents.Add(new Agent{owner=actor,position=pos,radius=radius,next=previous});
                largestRadius=Mathf.Max(largestRadius,radius);
            }
        }
        internal static Vector3 Repulsion(Vector3 selfPosition,float selfRadius,Vector3 otherPosition,float otherRadius,int selfId,int otherId,float padding)
        {
            Vector3 delta=selfPosition-otherPosition;delta.y=0;
            float reach=selfRadius+otherRadius+padding, square=delta.sqrMagnitude;
            if(square>=reach*reach||Mathf.Abs(selfPosition.y-otherPosition.y)>reach)return Vector3.zero;
            if(square<.000001f){
                int low=Mathf.Min(selfId,otherId),high=Mathf.Max(selfId,otherId);
                float angle=(unchecked(low*73856093^high*19349663)&1023)*(Mathf.PI*2/1024);
                return new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*reach*(selfId<otherId?1:-1);
            }
            float distance=Mathf.Sqrt(square);
            return delta*((reach-distance)/distance);
        }
        public static Vector3 Velocity(MushroomMonster actor,Vector3 desired,float radius,GameplayPerformanceData data)
        {
            using var sample=Marker.Auto();Snapshot();
            Vector3 pos=actor.transform.position,force=Vector3.zero;var key=Cell(pos);
            int range=Mathf.CeilToInt((radius+largestRadius+data.crowdPadding)/CellSize),checkedNeighbours=0;
            int id=actor.GetHashCode();
            for(int z=-range;z<=range;z++)for(int x=-range;x<=range;x++){
                if(!cells.TryGetValue(key+new Vector2Int(x,z),out int index))continue;
                for(;index>=0;index=agents[index].next){
                    var other=agents[index];if(other.owner==actor)continue;
                    var push=Repulsion(pos,radius,other.position,other.radius,id,other.owner.GetHashCode(),data.crowdPadding);
                    if(push.sqrMagnitude<.000001f)continue;
                    force+=push;
                    if(++checkedNeighbours>=24)return Vector3.ClampMagnitude(desired+force*data.crowdSeparationStrength,desired.magnitude);
                }
            }
            return Vector3.ClampMagnitude(desired+force*data.crowdSeparationStrength,desired.magnitude);
        }
    }
}
