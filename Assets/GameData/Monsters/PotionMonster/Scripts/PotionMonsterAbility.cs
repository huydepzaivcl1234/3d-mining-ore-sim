using UnityEngine;
using GameData.Player.Script;
namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent,RequireComponent(typeof(MushroomMonster))]
    public sealed class PotionMonsterAbility : MonoBehaviour,IMonsterRangedAttack
    {
        public Transform releasePoint,heldPotion;
        private MushroomMonster monster;
        private Animator animator;
        private bool thrown;
        private int preparedKind=1;
        private bool prepared;
        private CharacterHealth preparedTarget;
        private readonly GameObject[] heldModels=new GameObject[3];
        private readonly RaycastHit[] sightHits=new RaycastHit[32];
        public PotionMonsterData Data=>(monster?monster:monster=GetComponent<MushroomMonster>()).SpeciesData as PotionMonsterData;
        public bool IsReady=>Data&&Data.greenPotion&&Data.redPotion&&Data.yellowPotion;
        public string FireState=>Data?Data.combat.fireState:"Throw";
        public float ReleaseMoment=>Data?Data.combat.releaseMoment:.45f;
        public float Range=>Data?Data.combat.rangedRange*transform.lossyScale.x:0;
        public bool CanReach(Transform target){
            if(!IsReady||!target)return false;var c=target.GetComponent<Collider>();var start=releasePoint?releasePoint.position:transform.position+Vector3.up;var ray=(c?c.bounds.center:target.position)-start;
            int count=Physics.RaycastNonAlloc(start,ray.normalized,sightHits,ray.magnitude,~0,QueryTriggerInteraction.Ignore);
            if(count==sightHits.Length)return false;for(int i=0;i<count;i++)if(!sightHits[i].transform.IsChildOf(transform)&&!sightHits[i].transform.IsChildOf(target)&&!sightHits[i].collider.GetComponentInParent<CharacterHealth>())return false;return true;
        }
        public void PrepareThrow(Transform target){
            preparedTarget=target?target.GetComponent<CharacterHealth>():null;
            preparedKind=Random.Range(0,3);
            if(preparedKind==0){
                CharacterHealth ally=null;float distance=Data.healingRange*Data.healingRange;bool wounded=false;
                foreach(var h in CharacterHealth.ActiveMonsters){
                    if(!h||!h.IsTargetable)continue;
                    float d=(h.transform.position-transform.position).sqrMagnitude;if(d>Data.healingRange*Data.healingRange)continue;
                    bool needsHealing=h.Health<h.MaxHealth;
                    if((needsHealing&&!wounded)||(needsHealing==wounded&&d<=distance)){distance=d;ally=h;wounded=needsHealing;}
                }
                preparedTarget=ally?ally:monster.Health;
            }
            thrown=false;
            prepared=preparedTarget;RestoreHeldVisuals();
        }
        public void Fire(MushroomMonster owner,Transform target){
            if(!IsReady)return;if(!prepared)PrepareThrow(target);if(!preparedTarget)return;
            var model=preparedKind==0?Data.greenPotion:preparedKind==1?Data.redPotion:Data.yellowPotion;
            var shot=SceneProjectilePool.Rent<PotionMonsterProjectile>(model,releasePoint?releasePoint.position:transform.position+Vector3.up,Quaternion.identity,gameObject.scene,32);
            shot.Launch(monster,preparedTarget,preparedKind);shot.gameObject.SetActive(true);
            prepared=false;thrown=true;RestoreHeldVisuals();
        }
        private void Awake(){monster=GetComponent<MushroomMonster>();animator=GetComponentInChildren<Animator>();if(heldPotion)heldModels[1]=heldPotion.gameObject;}
        private void OnEnable(){thrown=false;prepared=false;RestoreHeldVisuals();}
        public static void FitPotion(GameObject go,float size){
            foreach(var c in go.GetComponentsInChildren<Collider>())c.enabled=false;
            foreach(var b in go.GetComponentsInChildren<Rigidbody>()){b.isKinematic=true;b.useGravity=false;}
            var renderers=go.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)return;Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            float largest=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));if(largest>.00001f)go.transform.localScale*=Mathf.Max(.01f,size)/largest;
        }
        public void RestoreHeldVisuals(){
            if(!heldPotion||!Data)return;
            int kind=Mathf.Clamp(preparedKind,0,2);
            if(!heldModels[kind]){var prefab=kind==0?Data.greenPotion:kind==1?Data.redPotion:Data.yellowPotion;if(!prefab)return;heldModels[kind]=Instantiate(prefab,heldPotion.parent,false);heldModels[kind].name="Held "+(kind==0?"green":kind==1?"red":"yellow")+" potion";heldModels[kind].transform.SetLocalPositionAndRotation(heldPotion.localPosition,heldPotion.localRotation);FitPotion(heldModels[kind],Data.potionSize);}
            for(int i=0;i<heldModels.Length;i++)if(heldModels[i])heldModels[i].SetActive(i==kind&&!thrown);
        }
        private void Update(){
            if(!monster||!monster.Health||monster.Health.Health<=0||monster.IsDespawning||WorldActorVisibility.ShouldPause(monster)||RuneStation.PlayerUsesRuneTime)return;
            if(monster.TryGetComponent<SurvivorEnemyStatus>(out var status)&&status.Airborne)return;
            if(thrown&&animator&&!animator.IsInTransition(0)&&!animator.GetCurrentAnimatorStateInfo(0).IsName(FireState)){
                thrown=false;RestoreHeldVisuals();
            }
        }
        private void OnDrawGizmosSelected(){if(!Data)return;Gizmos.color=Color.red;Gizmos.DrawWireSphere(transform.position,Range);Gizmos.color=Color.green;Gizmos.DrawWireSphere(transform.position,Data.healingRange);}
    }
}
