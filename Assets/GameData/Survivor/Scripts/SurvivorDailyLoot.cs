using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MiningSimulator.Ores;
namespace GameData.Player.Script
{
    [DisallowMultipleComponent]
    public sealed class SurvivorDailyLoot : MonoBehaviour
    {
        public SurvivorLootData data;
        private DayNightSystem clock;
        private MiningCardSystem cards;
        private float retryAt;
        private bool saveBlocked;
        private const string Key="Survivor.DailyLoot.v1";
        [Serializable] private sealed class BoxProgress {public bool shiny,collected;}
        [Serializable] private sealed class Progress {public int version=2,day=-1;public bool shiny,collected;public List<BoxProgress> boxes=new();}
        private Progress progress=new();
        private readonly RaycastHit[] groundHits=new RaycastHit[32];
        private readonly Collider[] obstacles=new Collider[32];
        private readonly List<SurvivorLootBox> boxes=new();
        public IReadOnlyList<SurvivorLootBox> CurrentBoxes=>boxes;
        public SurvivorLootBox CurrentBox {get {foreach(var box in boxes)if(box)return box;return null;}}
        public int SpawnedDay=>progress.day;
        public bool CollectedToday {get {if(progress.day<0)return false;foreach(var box in progress.boxes)if(!box.collected)return false;return true;}}
        public int BoxesSpawned{get;private set;}
        private void Awake(){clock=GetComponent<DayNightSystem>();if(!clock)clock=FindAnyObjectByType<DayNightSystem>();cards=FindAnyObjectByType<MiningCardSystem>();Load();}
        private void OnEnable(){if(!clock)clock=GetComponent<DayNightSystem>();if(clock)clock.PeriodChanged+=PeriodChanged;}
        private void OnDisable(){if(clock)clock.PeriodChanged-=PeriodChanged;}
        private void Start()=>RefreshMorning();
        private void PeriodChanged(MiningTimePeriod period){if(period==MiningTimePeriod.Day)RefreshMorning();}
        private void Update(){if(!CollectedToday&&Time.time>=retryAt)RefreshMorning();}
        private void Load()
        {
            string json=GameSave.GetString(Key,"");if(string.IsNullOrEmpty(json))return;
            try{var p=JsonUtility.FromJson<Progress>(json);if(p==null||(p.version!=1&&p.version!=2)||p.day<0){saveBlocked=true;return;}if(p.version==1){p.boxes=new List<BoxProgress>{new(){shiny=p.shiny,collected=p.collected}};p.version=2;}
                if(p.boxes==null||p.boxes.Count>50||p.boxes.Exists(b=>b==null)){saveBlocked=true;return;}progress=p;}
            catch(ArgumentException){saveBlocked=true;}
        }
        private void Save(){GameSave.SetString(Key,JsonUtility.ToJson(progress));GameSave.Save();}
        private void ClearBoxes(){foreach(var box in boxes)if(box){box.gameObject.SetActive(false);Destroy(box.gameObject);}boxes.Clear();}
        public void ResetProgress(){GameSave.DeleteKey(Key);progress=new();saveBlocked=false;ClearBoxes();retryAt=0;}
        public bool RefreshMorning()
        {
            retryAt=Time.time+2;
            if(saveBlocked||!data||!data.prefab||!clock||clock.CurrentPeriod!=MiningTimePeriod.Day||!cards||!cards.Player||!cards.CanOfferAbilities)return false;
            if(progress.day!=clock.DayNumber){
                ClearBoxes();progress=new Progress{day=clock.DayNumber};
                for(int i=0;i<Mathf.Clamp(data.boxesPerMorning,0,50);i++)progress.boxes.Add(new BoxProgress{shiny=UnityEngine.Random.value<data.shinyChancePercent*.01f});
                Save();
            }
            while(boxes.Count<progress.boxes.Count)boxes.Add(null);
            bool spawned=false;
            for(int i=0;i<progress.boxes.Count;i++){
                if(progress.boxes[i].collected||boxes[i]||!TryFindPosition(out var point))continue;
                var go=Instantiate(data.prefab,point+Vector3.up*data.dropHeight,Quaternion.Euler(0,UnityEngine.Random.Range(0,360),0),transform);
                var box=go.GetComponent<SurvivorLootBox>();if(!box)box=go.AddComponent<SurvivorLootBox>();boxes[i]=box;
                box.Configure(this,cards,progress.boxes[i].shiny);box.BeginDrop(point);WorldActorVisibility.Attach(go);BoxesSpawned++;spawned=true;
            }
            return spawned;
        }
        public bool TryFindPosition(out Vector3 position)
        {
            position=default;if(!cards||!cards.Player||!data)return false;Vector3 origin=cards.Player.transform.position;
            float min=Mathf.Max(1,data.minimumDistance),max=Mathf.Max(min,data.maximumDistance);
            for(int attempt=0;attempt<48;attempt++){
                float angle=UnityEngine.Random.value*Mathf.PI*2,radius=Mathf.Sqrt(UnityEngine.Random.Range(min*min,max*max));
                Vector3 candidate=origin+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                bool nav=NavMesh.SamplePosition(candidate,out var walkable,1.25f,NavMesh.AllAreas);if(nav)candidate=walkable.position;
                float distance=Vector3.ProjectOnPlane(candidate-origin,Vector3.up).magnitude;if(distance<min||distance>max)continue;
                int count=Physics.RaycastNonAlloc(candidate+Vector3.up*8,Vector3.down,groundHits,40,data.groundLayers,QueryTriggerInteraction.Ignore);
                bool found=false;float top=float.NegativeInfinity;
                for(int i=0;i<count;i++){var hit=groundHits[i];if(hit.normal.y<.7f||hit.collider.GetComponentInParent<CharacterHealth>()||hit.collider.GetComponentInParent<SurvivorLootBox>())continue;if(nav&&Mathf.Abs(hit.point.y-candidate.y)>1.5f)continue;if(hit.point.y>top){top=hit.point.y;position=hit.point;found=true;}}
                if(!found)continue;
                int occupied=Physics.OverlapBoxNonAlloc(position+Vector3.up*.48f,new Vector3(.58f,.40f,.58f),obstacles,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
                bool blocked=occupied>=obstacles.Length;
                for(int i=0;i<occupied&&!blocked;i++)if(obstacles[i]&&obstacles[i].bounds.max.y>position.y+.12f)blocked=true;
                foreach(var box in boxes)if(box&&((box.IsFalling?box.LandingPoint:box.transform.position)-position).sqrMagnitude<2.25f)blocked=true;
                if(!blocked){position+=Vector3.up*.04f;float actual=Vector3.Distance(position,origin);if(actual<min||actual>max)continue;return true;}
            }
            return false;
        }
        public bool TryOpen(SurvivorLootBox box)
        {
            int index=boxes.IndexOf(box);
            if(!box||box.IsFalling||index<0||index>=progress.boxes.Count||progress.boxes[index].collected||!cards||!cards.Player||!cards.CanCollect||!data)return false;
            if(Vector3.Distance(cards.Player.transform.position,box.transform.position)>data.pickupRadius)return false;
            float boost=box.Shiny?1+Mathf.Max(0,data.shinyAbilityBonusPercent)*.01f:1;
            if(!cards.CollectBlueBox(boost))return false;
            progress.boxes[index].collected=true;Save();boxes[index]=null;box.gameObject.SetActive(false);Destroy(box.gameObject);return true;
        }
    }
}
