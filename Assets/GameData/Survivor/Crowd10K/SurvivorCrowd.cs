using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using GameData.Player.Script;
using Survivor.CrowdPrototype;

namespace MiningSimulator.Ores
{
    // CharacterHealth remains authoritative for damage, cards and kill events; no per-actor skeleton or motor.
    [DefaultExecutionOrder(12000), DisallowMultipleComponent]
    public sealed partial class SurvivorCrowd : MonoBehaviour
    {
        public static SurvivorCrowd Current { get; private set; }
        [SerializeField] private Crowd10KData animationData;
        [SerializeField] private MushroomMonster mushroomPrefab;
        [SerializeField] private CharacterHealth player;
        [SerializeField, Min(64)] private int capacity = 10000;
        [SerializeField] private bool castNearbyShadows = true;
        public int AliveCount { get; private set; }
        public int DrawnCount { get; private set; }
        public int ProxyCount => used;
        public double SimulationMilliseconds { get; private set; }
        public CharacterHealth Player => player;
        public MushroomMonster SourcePrefab => mushroomPrefab;
        public int LevelOf(int index)=>index>=0&&index<used?actors[index].level:1;
        private sealed class Actor
        {
            public CharacterHealth health;
            public Transform transform;
            public MonsterSpawnZone zone;
            public MonsterSpawnEntry entry;
            public MonsterRewardData reward;
            public MonsterCombatSettings combat;
            public int level, clip=1;
            public float damage, scale=1, clock, phase, cooldown, deadUntil, flash, lifetime;
            public bool living, paused, hit;
        }
        private Actor[] actors;
        private int used;
        private readonly Stack<int> free = new();
        private NativeArray<float3> positions, next, ground;
        private NativeArray<float4> motion;
        private NativeArray<float> scales;
        private NativeArray<Matrix4x4> renderMatrices;
        private NativeArray<byte> state, walkable;
        private NativeArray<int> distances, queue;
        private NativeArray<float2> flow;
        private NativeParallelMultiHashMap<int,int> cells;
        private Vector3[] groundCopy;
        private byte[] openCopy;
        private WorldNavigationGrid navigation;
        private int gridRevision=-1, columns, rows, targetCell=-1;
        private float cellSize;
        private float3 origin;
        private Camera view;
        private MiningCardSystem cards;
        private readonly Plane[] planes = new Plane[6];
        private readonly Matrix4x4[] matrices = new Matrix4x4[1023];
        private readonly Vector4[] animations = new Vector4[1023], tints = new Vector4[1023];
        private readonly List<int>[] visible = {new(),new(),new(),new()};
        private MaterialPropertyBlock properties;
        private readonly System.Diagnostics.Stopwatch watch = new();
        private static readonly Unity.Profiling.ProfilerMarker UpdateMarker=new("Survivor.CrowdUpdate"),RenderMarker=new("Survivor.CrowdRender");
        private bool shuttingDown;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current=null;
        private void OnEnable()
        {
            if(Current && Current!=this){enabled=false;return;} Current=this;
            capacity=Mathf.Max(64,capacity);actors=new Actor[capacity];
            positions=new(capacity,Allocator.Persistent);next=new(capacity,Allocator.Persistent);motion=new(capacity,Allocator.Persistent);state=new(capacity,Allocator.Persistent);
            scales=new(capacity,Allocator.Persistent);renderMatrices=new(capacity,Allocator.Persistent);
            cells=new(capacity,Allocator.Persistent);properties=new MaterialPropertyBlock();shuttingDown=false;
        }
        private void Start(){view=Camera.main;navigation=WorldNavigationGrid.Instance;cards=FindAnyObjectByType<MiningCardSystem>();}
        public bool CanHandle(MushroomMonster prefab) => enabled && animationData && prefab==mushroomPrefab && mushroomPrefab && mushroomPrefab.CombatData!=null;
        public bool IsPaused(int index) => index>=0 && index<used && actors[index]!=null && actors[index].paused;
        public int CountFor(MonsterSpawnZone zone,MonsterSpawnEntry entry=null)
        {
            int count=0;for(int i=0;i<used;i++){var a=actors[i];if(a.living&&a.zone==zone&&(entry==null||a.entry==entry))count++;}return count;
        }
        private int lastSpawnedSlot;
        public bool TrySpawn(MonsterSpawnZone zone,MonsterSpawnEntry entry,Vector3 point,bool restoring=false)
        {
            if(!CanHandle(entry.prefab)||(!player&&!TryFindPlayer())||used>=capacity&&free.Count==0)return false;
            navigation??=WorldNavigationGrid.Instance;
            if(!restoring&&(!navigation||!navigation.HasBaked||!navigation.IsCapsulePlacementClear(point,.4f,1.4f)))return false;
            for(int n=0;!restoring&&n<used;n++)if(actors[n].living&&math.distancesq(positions[n],(float3)point)<.65f*.65f)return false;
            int i=free.Count>0?free.Pop():used++;
            var a=actors[i];
            if(a==null){
                var go=new GameObject("Mushroom — GPU combat proxy");go.SetActive(false);go.layer=LayerMask.NameToLayer("Enemy");go.transform.SetParent(transform,false);
                a=actors[i]=new Actor{transform=go.transform};a.health=go.AddComponent<CharacterHealth>();a.health.CrowdOwner=this;a.health.CrowdSlot=i;
                int slot=i;a.health.Died+=()=>Killed(slot);a.health.Damaged+=()=>Hit(slot);
            }
            a.zone=zone;a.entry=entry;a.reward=entry.prefab.SpeciesData;a.combat=entry.prefab.CombatData;
            var stats=player.GetComponent<PlayerStats>();a.level=a.reward.RollSpawnLevel(UnityEngine.Random.value,stats?stats.Level:1,zone?zone.SpawnDayNumber:1);
            float growth=a.reward.StatMultiplier(a.level);float low=Mathf.Max(.01f,Mathf.Min(a.reward.randomStatMultiplier.x,a.reward.randomStatMultiplier.y)),high=Mathf.Max(low,Mathf.Max(a.reward.randomStatMultiplier.x,a.reward.randomStatMultiplier.y));
            a.damage=a.combat.damage*growth*UnityEngine.Random.Range(low,high);a.scale=Mathf.Max(.01f,entry.prefab.transform.lossyScale.x)*(zone?Mathf.Abs(zone.transform.lossyScale.x):1);
            a.lifetime=a.reward.combatSeconds;
            a.clip=1;a.clock=a.cooldown=a.flash=0;a.phase=(i*.618034f)%1;a.living=true;a.paused=false;a.hit=false;
            scales[i]=a.scale;positions[i]=next[i]=point;state[i]=1;a.transform.SetPositionAndRotation(point,Quaternion.identity);a.transform.localScale=Vector3.one*a.scale;
            a.health.ConfigureSpawnHealth(a.combat.maxHealth*growth*UnityEngine.Random.Range(low,high));a.health.ConfigureRegeneration(a.combat.regenAmount,a.combat.regenInterval);a.health.ConfigureHealingBonus(a.reward.healingBonusPercent);a.health.ConfigureDefenses(a.reward.ArmorAtLevel(a.level),a.reward.MagicResistanceAtLevel(a.level),a.reward.resistanceScale);
            // A pooled life must not inherit a previous attacker or unfinished burn.
            a.health.Respawn();a.health.enabled=true;a.health.gameObject.SetActive(true);AliveCount++;lastSpawnedSlot=i;
            return true;
        }
        private bool TryFindPlayer(){var p=FindAnyObjectByType<SurvivorAutoCombat>();if(p)player=p.GetComponent<CharacterHealth>();return player;}
        private void Hit(int i)
        {
            var a=actors[i];a.flash=.15f;if(!a.living||a.health.HealthBar)return;
            var template=mushroomPrefab.GetComponent<CharacterHealth>();if(!template||!template.HealthBar)return;
            var bar=Instantiate(template.HealthBar,a.transform,false);
            bar.localPosition=mushroomPrefab.transform.InverseTransformPoint(template.HealthBar.position);
            a.health.AttachCrowdBar(template,bar);
        }
        private void Killed(int i)
        {
            var a=actors[i];if(shuttingDown||!a.living)return;
            a.living=false;AliveCount--;state[i]=2;a.clip=3;a.clock=0;a.deadUntil=Time.time+Mathf.Max(.2f,a.combat.deathDelay);
            if(a.health.HealthBar)a.health.HealthBar.gameObject.SetActive(false);
            if(navigation&&navigation.TryGetGroundPoint(a.transform.position,out var foot))positions[i]=next[i]=foot;
            a.zone?.NotifyDefeated(a.transform.position);
            MonsterKillCredit.Notify(a.health.LastDamageSource,a.reward,false,a.reward.experience);
            if(cards)cards.TryDrop(a.transform.position,false);
            a.zone?.GrantRewards(a.reward,a.transform.position+Vector3.up*.6f,a.reward.GoldMultiplier(a.level));
        }
        public void RemoveZone(MonsterSpawnZone zone)
        {
            for(int i=0;i<used;i++)if(state[i]!=0&&actors[i].zone==zone)Release(i);
        }
        private void Release(int i)
        {
            var a=actors[i];if(state[i]==0)return;if(a.living)AliveCount--;a.living=false;state[i]=0;
            a.health.gameObject.SetActive(false);a.zone=null;a.entry=null;free.Push(i);
        }
        private bool PrepareGrid()
        {
            if(!navigation)navigation=WorldNavigationGrid.Instance;if(!navigation||!navigation.HasBaked)return false;
            if(gridRevision==navigation.Revision&&ground.IsCreated)return true;
            DisposeGrid();columns=navigation.CrowdColumns;rows=navigation.CrowdRows;cellSize=navigation.CrowdCellSize;origin=navigation.CrowdOrigin;
            int count=columns*rows;groundCopy=new Vector3[count];openCopy=new byte[count];navigation.CopyCrowdCells(groundCopy,openCopy);
            ground=new(count,Allocator.Persistent);walkable=new(count,Allocator.Persistent);flow=new(count,Allocator.Persistent);distances=new(count,Allocator.Persistent);queue=new(count,Allocator.Persistent);
            for(int i=0;i<count;i++){ground[i]=groundCopy[i];walkable[i]=openCopy[i];}
            gridRevision=navigation.Revision;targetCell=-1;return true;
        }
        private void Update()
        {
            if(used==0||!player||Time.deltaTime<=0||RuneStation.PlayerUsesRuneTime||!PrepareGrid())return;
            using var sample=UpdateMarker.Auto();
            if(!view)view=Camera.main;if(view)GeometryUtility.CalculateFrustumPlanes(view,planes);
            Vector3 playerPosition=player.transform.position;bool targetAlive=player.IsTargetable;
            float dt=Mathf.Min(Time.deltaTime,.05f);var perf=GameplayPerformanceData.Current;bool freeze=perf&&perf.freezeOffscreenSimulation&&perf.cullOffscreenActors;
            for(int i=0;i<used;i++){
                if(state[i]==0)continue;var a=actors[i];
                a.paused=freeze&&view&&!GeometryUtility.TestPlanesAABB(planes,new Bounds((Vector3)positions[i]+Vector3.up*a.scale,Vector3.one*(3*a.scale+.5f)));
                if(a.paused){motion[i]=new float4(0,.4f*a.scale,0,0);continue;}
                if(!a.living){if(Time.time>=a.deadUntil){Release(i);continue;}a.clock+=dt;motion[i]=float4.zero;continue;}
                if(a.lifetime>0){a.lifetime-=dt;if(a.lifetime<=0){Release(i);continue;}}
                var status=a.health.CrowdStatus;
                if(status)positions[i]=a.transform.position;
                if(a.health.NeedsCrowdHealthTick)a.health.TickCrowdHealth(dt);if(!a.living)continue;
                a.flash=Mathf.Max(0,a.flash-dt);a.cooldown=Mathf.Max(0,a.cooldown-dt);
                float range=a.combat.attackRange*a.scale;float distance=math.distance(positions[i],(float3)playerPosition);
                float attackLength=animationData.clipLengths[2]/a.combat.SafeAttackSpeed;
                if(a.clip==2){
                    a.clock+=dt;
                    if(!a.hit&&a.clock>=attackLength*a.combat.hitMoment){a.hit=true;
                        if(targetAlive&&distance<=range+.3f&&!(status&&status.Airborne)&&HasStrikeLine(a,playerPosition)){
                            float dealt=player.DealDamage(a.damage,a.reward.attackDamageType,a.health.gameObject);
                            if(dealt>0){a.health.Heal(dealt*a.reward.lifeStealPercent*.01f);if(a.reward.burnDamagePerTick>0)player.ApplyBurn(a.reward.burnDamagePerTick*a.reward.StatMultiplier(a.level),a.reward.burnTickSeconds,a.reward.burnDurationSeconds,a.health.gameObject);}
                        }
                    }
                    if(a.clock>=attackLength){a.clip=1;a.clock=0;}
                }else if(targetAlive&&distance<=range&&a.cooldown<=0&&!(status&&status.Airborne)){
                    a.clip=2;a.clock=0;a.hit=false;a.cooldown=a.combat.attackCooldown/a.combat.SafeAttackSpeed;
                }else a.clock+=dt;
                bool move=targetAlive&&a.clip!=2&&!(status&&status.Airborne);
                motion[i]=new float4(a.combat.moveSpeed*(status?status.MovementMultiplier:1),.4f*a.scale,range*.85f,move?1:0);
            }
            watch.Restart();
            int2 targetXY=(int2)math.floor(((float3)playerPosition-origin).xz/cellSize);
            int cell=math.clamp(targetXY.y,0,rows-1)*columns+math.clamp(targetXY.x,0,columns-1);
            JobHandle dependency=default;
            if(cell!=targetCell){
                targetCell=cell;
                dependency=new FlowJob{open=walkable,ground=ground,distances=distances,queue=queue,flow=flow,width=columns,depth=rows,target=cell,maxStep=navigation.MaximumStep}.Schedule();
                dependency=new SmoothFlowJob{open=walkable,ground=ground,distances=distances,flow=flow,width=columns,depth=rows,target=cell,maxStep=navigation.MaximumStep}.Schedule(walkable.Length,128,dependency);
            }
            cells.Clear();var hash=new HashJob{positions=positions,state=state,cells=cells.AsParallelWriter()}.Schedule(used,128,dependency);
            new MoveJob{positions=positions,next=next,motion=motion,state=state,cells=cells,ground=ground,open=walkable,flow=flow,origin=origin,cellSize=cellSize,width=columns,depth=rows,target=playerPosition,dt=dt,maxStep=navigation.MaximumStep}.Schedule(used,64,hash).Complete();
            var swap=positions;positions=next;next=swap;watch.Stop();SimulationMilliseconds=watch.Elapsed.TotalMilliseconds;
            for(int i=0;i<used;i++)if(state[i]!=0&&!actors[i].paused){var a=actors[i];if(a.health.CrowdStatus&&a.health.CrowdStatus.Airborne)continue;if(math.distancesq(positions[i],next[i])>.0000001f)a.transform.position=positions[i];}
        }
        private bool HasStrikeLine(Actor a,Vector3 playerPosition)
        {
            Vector3 start=a.transform.position+Vector3.up*.6f,end=playerPosition+Vector3.up*.6f;
            return !Physics.Linecast(start,end,out var hit,~0,QueryTriggerInteraction.Ignore)||hit.collider.GetComponentInParent<CharacterHealth>()==player;
        }
        private void LateUpdate()
        {
            using var sample=RenderMarker.Auto();
            DrawnCount=0;if(!animationData||!animationData.material||used==0)return;if(!view)view=Camera.main;if(!view)return;
            GeometryUtility.CalculateFrustumPlanes(view,planes);foreach(var list in visible)list.Clear();
            Vector3 cameraPosition=view.transform.position,cameraUp=view.transform.up;
            new MatrixJob{positions=positions,scales=scales,state=state,matrices=renderMatrices,target=player?player.transform.position:Vector3.forward}.Schedule(used,128).Complete();
            for(int i=0;i<used;i++){
                if(state[i]==0)continue;var a=actors[i];Vector3 p=positions[i];
                bool inView=GeometryUtility.TestPlanesAABB(planes,new Bounds(p+Vector3.up*a.scale,Vector3.one*(3*a.scale+.5f)));
                if(a.health.HealthBar){var bar=a.health.HealthBar;bool show=inView&&a.living&&a.health.Health<a.health.MaxHealth;if(bar.gameObject.activeSelf!=show)bar.gameObject.SetActive(show);if(show)bar.rotation=Quaternion.LookRotation(bar.position-cameraPosition,cameraUp);}
                if(!inView)continue;
                float d=Vector3.Distance(p,cameraPosition);var thresholds=animationData.lodDistances;int lod=d<thresholds[0]?0:d<thresholds[1]?1:d<thresholds[2]?2:3;
                visible[lod].Add(i);DrawnCount++;
            }
            var bounds=navigation&&navigation.HasBaked?navigation.AreaBounds:new Bounds(transform.position,Vector3.one*1000);bounds.Expand(Vector3.up*100);
            for(int lod=0;lod<4;lod++)for(int offset=0;offset<visible[lod].Count;offset+=1023){
                int count=Mathf.Min(1023,visible[lod].Count-offset);
                for(int n=0;n<count;n++){
                    int i=visible[lod][offset+n];var a=actors[i];matrices[n]=renderMatrices[i];
                    float length=animationData.clipLengths[a.clip]/(a.clip==2?a.combat.SafeAttackSpeed:1);
                    animations[n]=new Vector4(a.clip*animationData.framesPerClip,(a.clip==2||a.clip==3?-1:1)*animationData.framesPerClip,a.clock/length+(a.clip==1?a.phase:0),0);
                    Color tint=a.health.CrowdStatus&&a.health.CrowdStatus.Frosted?a.health.CrowdStatus.FrostTint:Color.white;
                    if(a.flash>0)tint=Color.Lerp(tint,new Color(2.5f,2.5f,2.5f),a.flash/.15f*.8f);tints[n]=new Vector4(tint.r,tint.g,tint.b,1);
                }
                properties.SetVectorArray("_AnimData",animations);properties.SetVectorArray("_Tint",tints);
                var parameters=new RenderParams(animationData.material){matProps=properties,layer=LayerMask.NameToLayer("Enemy"),worldBounds=bounds,shadowCastingMode=castNearbyShadows&&lod<2?ShadowCastingMode.On:ShadowCastingMode.Off};
                Graphics.RenderMeshInstanced(parameters,animationData.meshes[lod],0,matrices,count);
            }
        }
        private void OnDisable()
        {
            if(Current!=this)return;shuttingDown=true;Current=null;
            if(actors!=null)foreach(var a in actors)if(a?.health){a.health.gameObject.SetActive(false);Destroy(a.health.gameObject);}
            if(scales.IsCreated)scales.Dispose();if(renderMatrices.IsCreated)renderMatrices.Dispose();if(positions.IsCreated)positions.Dispose();if(next.IsCreated)next.Dispose();if(motion.IsCreated)motion.Dispose();if(state.IsCreated)state.Dispose();if(cells.IsCreated)cells.Dispose();DisposeGrid();used=AliveCount=0;free.Clear();
        }
        private void DisposeGrid(){if(ground.IsCreated)ground.Dispose();if(walkable.IsCreated)walkable.Dispose();if(flow.IsCreated)flow.Dispose();if(distances.IsCreated)distances.Dispose();if(queue.IsCreated)queue.Dispose();}
        [BurstCompile] private struct FlowJob:IJob
        {
            [ReadOnly]public NativeArray<byte> open;[ReadOnly]public NativeArray<float3> ground;
            public NativeArray<int> distances,queue;public NativeArray<float2> flow;public int width,depth,target;public float maxStep;
            public void Execute(){
                for(int i=0;i<open.Length;i++){distances[i]=int.MaxValue;flow[i]=float2.zero;}
                int start=target;if(open[start]==0){int best=int.MaxValue;for(int i=0;i<open.Length;i++)if(open[i]!=0){int d=math.abs(i%width-target%width)+math.abs(i/width-target/width);if(d<best){best=d;start=i;}}}
                if(open[start]==0)return;int head=0,tail=0;queue[tail++]=start;distances[start]=0;
                while(head<tail){int i=queue[head++];int x=i%width,y=i/width;for(int n=0;n<4;n++){int xx=x+(n==0?1:n==1?-1:0),yy=y+(n==2?1:n==3?-1:0);if(xx<0||xx>=width||yy<0||yy>=depth)continue;int j=yy*width+xx;if(open[j]==0||distances[j]!=int.MaxValue||math.abs(ground[j].y-ground[i].y)>maxStep)continue;distances[j]=distances[i]+1;flow[j]=math.normalizesafe(ground[i].xz-ground[j].xz);queue[tail++]=j;}}
            }
        }
        [BurstCompile] private struct SmoothFlowJob:IJobParallelFor
        {
            [ReadOnly]public NativeArray<byte> open;
            [ReadOnly]public NativeArray<float3> ground;
            [ReadOnly]public NativeArray<int> distances;
            public NativeArray<float2> flow;public int width,depth,target;public float maxStep;
            public void Execute(int i){
                    if(open[i]==0||distances[i]==int.MaxValue||distances[i]==0)return;
                    // Follow a direct ray across open cells; retain the field around obstacles.
                    if(ClearLine(i,target)){flow[i]=math.normalizesafe(ground[target].xz-ground[i].xz);return;}
                    int x=i%width,y=i/width;float2 direction=float2.zero;
                    for(int yy=-1;yy<=1;yy++)for(int xx=-1;xx<=1;xx++){
                        if(xx==0&&yy==0)continue;int nx=x+xx,ny=y+yy;if(nx<0||nx>=width||ny<0||ny>=depth)continue;int j=ny*width+nx;
                        if(open[j]==0||distances[j]>=distances[i]||math.abs(ground[j].y-ground[i].y)>maxStep)continue;
                        if(xx!=0&&yy!=0&&(open[y*width+nx]==0||open[ny*width+x]==0))continue;
                        direction+=math.normalizesafe(ground[j].xz-ground[i].xz);
                    }
                    if(math.lengthsq(direction)>.000001f)flow[i]=math.normalize(direction);
            }
            private bool HasMargin(int cell){
                int x=cell%width,y=cell/width;
                if(x==0||y==0||x==width-1||y==depth-1)return false;
                for(int yy=-1;yy<=1;yy++)for(int xx=-1;xx<=1;xx++)if(open[(y+yy)*width+x+xx]==0)return false;
                return true;
            }
            private bool ClearLine(int from,int to){
                if(!HasMargin(from))return false;
                int x=from%width,y=from/width,tx=to%width,ty=to/width;
                int dx=math.abs(tx-x),dy=math.abs(ty-y),sx=x<tx?1:-1,sy=y<ty?1:-1,error=dx-dy;
                while(x!=tx||y!=ty){
                    int xx=x,yy=y,e=error*2;
                    if(e>-dy){error-=dy;xx+=sx;}if(e<dx){error+=dx;yy+=sy;}
                    int previous=y*width+x,next=yy*width+xx;
                    if(!HasMargin(next)||math.abs(ground[next].y-ground[previous].y)>maxStep)return false;
                    if(xx!=x&&yy!=y&&(open[y*width+xx]==0||open[yy*width+x]==0))return false;
                    x=xx;y=yy;
                }
                return true;
            }

        }
        [BurstCompile] private struct MatrixJob:IJobParallelFor
        {
            [ReadOnly]public NativeArray<float3> positions;
            [ReadOnly]public NativeArray<float> scales;
            [ReadOnly]public NativeArray<byte> state;
            [WriteOnly]public NativeArray<Matrix4x4> matrices;
            public float3 target;
            public void Execute(int i){
                if(state[i]==0)return;float3 p=positions[i],direction=target-p;direction.y=0;
                var rotation=quaternion.LookRotationSafe(direction,math.up());var m=float4x4.TRS(p,rotation,new float3(scales[i]));
                matrices[i]=new Matrix4x4(new Vector4(m.c0.x,m.c0.y,m.c0.z,m.c0.w),new Vector4(m.c1.x,m.c1.y,m.c1.z,m.c1.w),new Vector4(m.c2.x,m.c2.y,m.c2.z,m.c2.w),new Vector4(m.c3.x,m.c3.y,m.c3.z,m.c3.w));
            }
        }
        [BurstCompile] private struct HashJob:IJobParallelFor
        {
            [ReadOnly]public NativeArray<float3> positions;[ReadOnly]public NativeArray<byte> state;public NativeParallelMultiHashMap<int,int>.ParallelWriter cells;
            public void Execute(int i){if(state[i]!=1)return;int2 c=(int2)math.floor(positions[i].xz/1.2f);cells.Add(c.x*73856093^c.y*19349663,i);}
        }
        [BurstCompile] private struct MoveJob:IJobParallelFor
        {
            [ReadOnly]public NativeArray<float3> positions,ground;[ReadOnly]public NativeArray<float4> motion;[ReadOnly]public NativeArray<byte> state,open;[ReadOnly]public NativeArray<float2> flow;[ReadOnly]public NativeParallelMultiHashMap<int,int> cells;
            [WriteOnly]public NativeArray<float3> next;public float3 origin,target;public float cellSize,dt,maxStep;public int width,depth;
            public void Execute(int i){
                float3 p=positions[i];next[i]=p;if(state[i]!=1||motion[i].w==0)return;
                int2 xy=(int2)math.floor((p-origin).xz/cellSize);if(xy.x<0||xy.y<0||xy.x>=width||xy.y>=depth)return;int index=xy.y*width+xy.x;
                float4 m=motion[i];float2 toward=target.xz-p.xz;float2 direction=flow[index];float2 velocity=math.lengthsq(toward)>m.z*m.z?direction*m.x:float2.zero;
                int2 cell=(int2)math.floor(p.xz/1.2f);int seen=0;float2 push=float2.zero;
                for(int y=-1;y<=1&&seen<64;y++)for(int x=-1;x<=1&&seen<64;x++){int2 c=cell+new int2(x,y);if(cells.TryGetFirstValue(c.x*73856093^c.y*19349663,out int j,out var it))do{if(i==j)continue;seen++;float2 delta=p.xz-positions[j].xz;float ds=math.lengthsq(delta);float reach=m.y+motion[j].y;if(ds>.000001f&&ds<(reach+.12f)*(reach+.12f)){float d=math.sqrt(ds);float2 normal=delta/d;float closing=math.dot(velocity,normal);if(closing<0)velocity-=normal*closing;if(d<reach)push+=normal*(reach-d)*4;}if(seen>=64)break;}while(cells.TryGetNextValue(out j,ref it));}
                velocity+=push;float length=math.length(velocity);velocity*=math.min(1,math.max(.1f,m.x)*1.5f/math.max(.001f,length));float3 candidate=p;candidate.xz+=velocity*dt;
                int2 cellNext=(int2)math.floor((candidate-origin).xz/cellSize);if(cellNext.x<0||cellNext.y<0||cellNext.x>=width||cellNext.y>=depth)return;int nextIndex=cellNext.y*width+cellNext.x;if(open[nextIndex]==0||math.abs(ground[nextIndex].y-ground[index].y)>maxStep)return;
                // Never cut a diagonal through a blocked corner of the authored navigation grid.
                if(cellNext.x!=xy.x&&cellNext.y!=xy.y&&(open[xy.y*width+cellNext.x]==0||open[cellNext.y*width+xy.x]==0))return;
                candidate.y=ground[nextIndex].y;next[i]=candidate;
            }
        }
    }
}

