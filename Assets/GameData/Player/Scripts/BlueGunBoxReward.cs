using MiningSimulator.Ores;
using UnityEngine;
namespace GameData.Player.Script
{
    [DisallowMultipleComponent,RequireComponent(typeof(MonsterSpawnZone))]
    public sealed class BlueGunBoxReward : MonoBehaviour
    {
        [SerializeField] private BlueGunUpgradeData data;
        private MonsterSpawnZone zone;private MiningCardSystem cards;private GunSpecialUpgrades upgrades;
        public int BoxesDropped{get;private set;}
        private void Awake(){zone=GetComponent<MonsterSpawnZone>();cards=FindAnyObjectByType<MiningCardSystem>();upgrades=FindAnyObjectByType<GunSpecialUpgrades>();}
        private void OnEnable(){if(!zone)zone=GetComponent<MonsterSpawnZone>();zone.PeriodCleared+=Drop;zone.PeriodCleared+=DropOrange;}
        private void OnDisable(){if(zone){zone.PeriodCleared-=Drop;zone.PeriodCleared-=DropOrange;}}
        private void Drop(Vector3 point)
        {
            if(!data||!data.boxPrefab||!cards||!upgrades||upgrades.AllUnlocked)return;
            Spawn(point,data.boxPrefab,data.visualScale,false);
        }
        private void DropOrange(Vector3 point)
        {
            var s=data?data.orange:null;
            if(!s||!s.boxPrefab||!cards||!upgrades||upgrades.OrangeAllUnlocked||s.dropChancePercent<=0||s.dropChancePercent<100&&Random.value*100>=s.dropChancePercent)return;
            Spawn(point+s.dropOffset,s.boxPrefab,s.visualScale,true);
        }
        private void Spawn(Vector3 point,GameObject prefab,float scale,bool orange)
        {
            if(Physics.Raycast(point+Vector3.up*10,Vector3.down,out var ground,50,~0,QueryTriggerInteraction.Ignore))point=ground.point;
            var box=Instantiate(prefab,point+Vector3.up,Quaternion.identity,transform);box.transform.localScale*=scale;
            foreach(var c in box.GetComponentsInChildren<Collider>(true))c.enabled=false;
            var bounds=new Bounds(box.transform.position,Vector3.zero);bool first=true;
            foreach(var r in box.GetComponentsInChildren<Renderer>()){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
            box.transform.position+=Vector3.up*(point.y-bounds.min.y+.03f);
            var landing=box.transform.position;box.transform.position+=Vector3.up*1.2f;
            var pickup=box.AddComponent<BlueGunUpgradeBox>();
            if(orange)pickup.Configure(cards,data.orange,landing);else pickup.Configure(cards,data,landing);
            BoxesDropped++;
        }
    }
}
