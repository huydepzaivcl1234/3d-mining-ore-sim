using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
namespace Survivor.CrowdPrototype
{
 // Isolated rendering/simulation trial. Does not impersonate CharacterHealth or migrate cards.
 public sealed class Crowd10K : MonoBehaviour
 {
  public Crowd10KData data;
  public Transform target;
  public Camera view;
  public int Alive {get;private set;}
  public int Drawn {get;private set;}
  public int Triangles {get;private set;}
  public double SimulationMs {get;private set;}
  public bool benchmark;
  public string outputDirectory="";
  public int[] benchmarkCounts={1000,5000,10000};
  NativeArray<float3> positions,next;
  NativeArray<float> health;
  NativeParallelMultiHashMap<int,int> cells;
  readonly Matrix4x4[][] matrices=new Matrix4x4[4][];
  readonly Vector4[][] animation=new Vector4[4][];
  readonly int[] counts=new int[4];
  readonly int[] triangleCounts=new int[4];
  readonly Matrix4x4[] batch=new Matrix4x4[1023];
  readonly Vector4[] animBatch=new Vector4[1023];
  readonly Plane[] planes=new Plane[6];
  MaterialPropertyBlock properties;
  readonly Stopwatch clock=new Stopwatch();
  readonly List<float> timings=new List<float>(10000);
  readonly List<float> simTimings=new List<float>(10000);
  float yaw,pitch=48,distance=135,elapsed,hudTimer,fireTimer;
  string hud="";
  int stage;
  long sampleDrawn,sampleTriangles;
  void Start(){
   if(!view)view=Camera.main;
   properties=new MaterialPropertyBlock();
   var args=Environment.GetCommandLineArgs();
   int arg=Array.IndexOf(args,"--crowd-benchmark");if(arg>=0){benchmark=true;if(arg+1<args.Length)outputDirectory=args[arg+1];}
   QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
   ResetCrowd(benchmark?benchmarkCounts[0]:data.count);
  }
  public void ResetCrowd(int count){
   Dispose();count=Mathf.Clamp(count,1,50000);positions=new NativeArray<float3>(count,Allocator.Persistent);next=new NativeArray<float3>(count,Allocator.Persistent);health=new NativeArray<float>(count,Allocator.Persistent);cells=new NativeParallelMultiHashMap<int,int>(count,Allocator.Persistent);
   int side=Mathf.CeilToInt(Mathf.Sqrt(count));float spacing=math.min(1.2f,data.mapHalfSize*1.8f/side);
   for(int i=0;i<count;i++){positions[i]=new float3((i%side-(side-1)*.5f)*spacing,0,(i/side-(side-1)*.5f)*spacing);health[i]=30;}
   for(int l=0;l<4;l++){triangleCounts[l]=(int)data.meshes[l].GetIndexCount(0)/3;matrices[l]=new Matrix4x4[count];animation[l]=new Vector4[count];}
   Alive=count;elapsed=0;timings.Clear();simTimings.Clear();sampleDrawn=sampleTriangles=0;
  }
  void Update(){
   if(!positions.IsCreated)return;
   float dt=math.min(Time.deltaTime,.04f);
   if(!benchmark){
    var k=Keyboard.current;float2 move=float2.zero;if(k!=null){move.x=(k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0);move.y=(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0);}
    var motion=Quaternion.Euler(0,yaw,0)*new Vector3(move.x,0,move.y);target.position+=motion*dt*12;
    target.position=new Vector3(Mathf.Clamp(target.position.x,-60,60),1,Mathf.Clamp(target.position.z,-60,60));
    var mouse=Mouse.current;if(mouse!=null){if(mouse.rightButton.isPressed){var d=mouse.delta.ReadValue();yaw+=d.x*.15f;pitch=Mathf.Clamp(pitch-d.y*.15f,20,85);}distance=Mathf.Clamp(distance-mouse.scroll.ReadValue().y*.04f,12,150);}
    fireTimer-=dt;if(k!=null&&k.spaceKey.isPressed&&fireTimer<=0){fireTimer=.1f;int nearest=-1;float best=400;for(int i=0;i<positions.Length;i++){if(health[i]<=0)continue;float dd=math.distancesq(positions[i],(float3)target.position);if(dd<best){best=dd;nearest=i;}}if(nearest>=0){health[nearest]-=15;if(health[nearest]<=0)Alive--;}}
   }
   clock.Restart();cells.Clear();
   var hash=new HashJob{positions=positions,health=health,cells=cells.AsParallelWriter()}.Schedule(positions.Length,128);
   new MoveJob{positions=positions,next=next,health=health,cells=cells,target=target.position,speed=data.movementSpeed,radius=data.separationRadius,dt=dt,halfSize=data.mapHalfSize}.Schedule(positions.Length,64,hash).Complete();
   var temp=positions;positions=next;next=temp;clock.Stop();SimulationMs=clock.Elapsed.TotalMilliseconds;
   view.transform.position=target.position+Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-distance);view.transform.LookAt(target.position);
   GeometryUtility.CalculateFrustumPlanes(view,planes);Array.Clear(counts,0,4);Drawn=Triangles=0;
   var t=target.position;
   for(int i=0;i<positions.Length;i++){
    if(health[i]<=0)continue;Vector3 p=positions[i];if(!GeometryUtility.TestPlanesAABB(planes,new Bounds(p+Vector3.up,Vector3.one*3)))continue;
    float d=Vector3.Distance(p,t);float cameraDistance=Vector3.Distance(p,view.transform.position);int lod=cameraDistance<data.lodDistances[0]?0:cameraDistance<data.lodDistances[1]?1:cameraDistance<data.lodDistances[2]?2:3;
    int slot=counts[lod]++;Vector3 direction=t-p;direction.y=0;var rotation=direction.sqrMagnitude>.001f?Quaternion.LookRotation(direction):Quaternion.identity;
    matrices[lod][slot]=Matrix4x4.TRS(p,rotation,Vector3.one);
    int clip=d<1.8f?2:1;float phase=(i*.61803398875f)%1;
    animation[lod][slot]=new Vector4(clip*data.framesPerClip,data.framesPerClip,phase,1/data.clipLengths[clip]);Drawn++;Triangles+=triangleCounts[lod];
   }
   for(int l=0;l<4;l++)for(int offset=0;offset<counts[l];offset+=1023){int n=Math.Min(1023,counts[l]-offset);Array.Copy(matrices[l],offset,batch,0,n);Array.Copy(animation[l],offset,animBatch,0,n);properties.SetVectorArray("_AnimData",animBatch);var rp=new RenderParams(data.material){camera=view,matProps=properties,shadowCastingMode=ShadowCastingMode.Off,receiveShadows=false,worldBounds=new Bounds(Vector3.zero,Vector3.one*400)};Graphics.RenderMeshInstanced(rp,data.meshes[l],0,batch,n);}
   if(benchmark)Measure();
   hudTimer-=Time.unscaledDeltaTime;if(hudTimer<=0){hudTimer=.25f;hud=$"GPU CROWD PROTOTYPE | {Alive:N0} NPCs | {Drawn:N0} drawn | {Triangles:N0} triangles\nAll {positions.Length:N0} simulated every frame | Jobs {SimulationMs:F2} ms | {1/Time.unscaledDeltaTime:F0} FPS\nWASD target  |  RMB orbit  |  Wheel zoom  |  Space damage/death trial";}
  }
  void Measure(){
   elapsed+=Time.unscaledDeltaTime;if(elapsed<4)return;
   timings.Add(Time.unscaledDeltaTime*1000);simTimings.Add((float)SimulationMs);sampleDrawn+=Drawn;sampleTriangles+=Triangles;
   if(elapsed<16)return;timings.Sort();simTimings.Sort();
   var result=new Result{npcCount=positions.Length,samples=timings.Count,medianMs=timings[timings.Count/2],p95Ms=timings[(int)(timings.Count*.95f)],meanMs=Mean(timings),simulationMedianMs=simTimings[simTimings.Count/2],drawnMean=(float)sampleDrawn/timings.Count,trianglesMean=(float)sampleTriangles/timings.Count,width=Screen.width,height=Screen.height,device=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,unity=Application.unityVersion,editor=Application.isEditor,allSimulated=true};
   result.fps=1000/result.medianMs;
   if(!string.IsNullOrEmpty(outputDirectory)){Directory.CreateDirectory(outputDirectory);File.WriteAllText(Path.Combine(outputDirectory,"Crowd"+positions.Length+"-"+(Application.isEditor?"Editor":"Player")+".json"),JsonUtility.ToJson(result,true));ScreenCapture.CaptureScreenshot(Path.Combine(outputDirectory,"Crowd"+positions.Length+"-"+(Application.isEditor?"Editor":"Player")+".png"));}
   UnityEngine.Debug.Log(JsonUtility.ToJson(result));stage++;
   // Defer population changes until after screenshot's end-of-frame readback.
   if(stage<benchmarkCounts.Length)StartCoroutine(NextStage());else benchmark=false;
  }
  System.Collections.IEnumerator NextStage(){benchmark=false;yield return new WaitForEndOfFrame();yield return null;ResetCrowd(benchmarkCounts[stage]);benchmark=true;}
  static float Mean(List<float> v){double sum=0;foreach(var x in v)sum+=x;return(float)(sum/v.Count);}
  [Serializable] public class Result{public int npcCount,samples,width,height;public float medianMs,p95Ms,meanMs,fps,simulationMedianMs,drawnMean,trianglesMean;public string device,cpu,unity;public bool editor,allSimulated;}
  void OnGUI(){GUI.Box(new Rect(12,12,880,82),hud);}
  void OnDestroy(){Dispose();}
  void Dispose(){if(positions.IsCreated)positions.Dispose();if(next.IsCreated)next.Dispose();if(health.IsCreated)health.Dispose();if(cells.IsCreated)cells.Dispose();}
  [BurstCompile] struct HashJob:IJobParallelFor{
   [ReadOnly]public NativeArray<float3> positions;[ReadOnly]public NativeArray<float> health;public NativeParallelMultiHashMap<int,int>.ParallelWriter cells;
   public void Execute(int i){if(health[i]>0){int2 c=(int2)math.floor(positions[i].xz/1.2f);cells.Add(c.x*73856093^c.y*19349663,i);}}
  }
  [BurstCompile] struct MoveJob:IJobParallelFor{
   [ReadOnly]public NativeArray<float3> positions;[ReadOnly]public NativeArray<float> health;[ReadOnly]public NativeParallelMultiHashMap<int,int> cells;[WriteOnly]public NativeArray<float3> next;
   public float3 target;public float speed,radius,dt,halfSize;
   public void Execute(int i){float3 p=positions[i];if(health[i]<=0){next[i]=p;return;}float2 goal=target.xz-p.xz;float dist=math.length(goal);float2 vel=dist>1.8f?math.normalizesafe(goal)*speed:float2.zero;float2 push=float2.zero;int2 cell=(int2)math.floor(p.xz/1.2f);int found=0;
    for(int y=-1;y<=1&&found<64;y++)for(int x=-1;x<=1&&found<64;x++){int2 c=cell+new int2(x,y);int key=c.x*73856093^c.y*19349663;if(cells.TryGetFirstValue(key,out int j,out var it))do{if(i==j)continue;found++;float2 diff=p.xz-positions[j].xz;float ds=math.lengthsq(diff);float reach=radius*2;if(ds<reach*reach&&ds>.000001f){float d=math.sqrt(ds);push+=diff/d*(reach-d)*5;}if(ds<(reach+.12f)*(reach+.12f)&&ds>.000001f){float2 normal=diff*math.rsqrt(ds);float closing=math.dot(vel,normal);if(closing<0)vel-=normal*closing;}if(found>=64)break;}while(cells.TryGetNextValue(out j,ref it));}
    vel+=push;float len=math.length(vel);vel*=math.min(1,speed*1.5f/math.max(.001f,len));p.xz=math.clamp(p.xz+vel*dt,-halfSize,halfSize);next[i]=p;
   }
  }
 }
}
