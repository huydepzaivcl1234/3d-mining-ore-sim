using UnityEngine;

namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent,RequireComponent(typeof(MushroomMonster))]
    public sealed class BreadMonsterAbility : MonoBehaviour, IMonsterRangedAttack
    {
        [SerializeField] private Transform heldSlice;
        [SerializeField] private Transform releasePoint;
        private readonly RaycastHit[] sightHits = new RaycastHit[32];
        private MushroomMonster monster;
        private Animator animator;
        [SerializeField] private Transform groundVisual, leftGroundFoot, rightGroundFoot;
        [SerializeField] private Vector3[] leftSoleVertices, rightSoleVertices;
        private float groundVisualY;
        private float healingClock;
        private bool thrown;
        public BreadMonsterData Data=> (monster ? monster : monster=GetComponent<MushroomMonster>()).SpeciesData as BreadMonsterData;
        public bool SliceInHand=>heldSlice&&heldSlice.gameObject.activeSelf;
        public bool IsReady => Data && Data.sliceProjectile && releasePoint;
        public string FireState => Data ? Data.combat.fireState : "Throw";
        public float ReleaseMoment => Data ? Data.combat.releaseMoment : .45f;
        public float Range => Data ? Data.combat.rangedRange * Mathf.Max(.01f, transform.lossyScale.x) : 0;
        public bool CanReach(Transform victim)
        {
            if (!IsReady || !victim) return false;
            var collider = victim.GetComponent<Collider>();
            Vector3 ray = (collider && collider.enabled ? collider.bounds.center : victim.position) - releasePoint.position;
            if (ray.sqrMagnitude < .001f) return true;
            int count = Physics.RaycastNonAlloc(releasePoint.position, ray.normalized, sightHits, ray.magnitude, ~0, QueryTriggerInteraction.Ignore);
            if (count == sightHits.Length) return false;
            for (int i=0; i<count; i++)
                if (!sightHits[i].transform.IsChildOf(transform) && !sightHits[i].transform.IsChildOf(victim)) return false;
            return true;
        }
        public void Fire(MushroomMonster owner, Transform victim)
        {
            if (!IsReady || !owner || !victim) return;
            var shot = SceneProjectilePool.Rent<BreadSliceProjectile>(Data.sliceProjectile.gameObject, releasePoint.position, Quaternion.identity, gameObject.scene, 32);
            shot.Launch(owner, victim); shot.gameObject.SetActive(true); ThrowSlice();
        }
        private void Awake(){monster=GetComponent<MushroomMonster>();animator=GetComponentInChildren<Animator>();if(groundVisual)groundVisualY=groundVisual.localPosition.y;}
        private void OnEnable(){healingClock=0;thrown=false;if(heldSlice)heldSlice.gameObject.SetActive(true);}
        public void ThrowSlice(){thrown=true;if(heldSlice)heldSlice.gameObject.SetActive(false);}
        private void Update()
        {
            if(!monster||!monster.Health||monster.Health.Health<=0||monster.IsDespawning||WorldActorVisibility.ShouldPause(monster)||RuneStation.PlayerUsesRuneTime)return;
            if(monster.TryGetComponent<GameData.Player.Script.SurvivorEnemyStatus>(out var status)&&status.Airborne)return;
            var data=Data;if(!data)return;
            if(thrown&&animator&&!animator.IsInTransition(0)&&!animator.GetCurrentAnimatorStateInfo(0).IsName(data.combat.fireState)){
                thrown=false;if(heldSlice)heldSlice.gameObject.SetActive(true);
            }
            TickHealing(Time.deltaTime);
        }
        // Keep the authored poses; only correct their vertical contact after Animator evaluation.
        private void LateUpdate()
        {
            if(!groundVisual||!monster||!monster.Health||monster.Health.Health<=0||monster.IsDespawning||WorldActorVisibility.ShouldPause(monster))return;
            var position=groundVisual.localPosition;position.y=groundVisualY;groundVisual.localPosition=position;
            if(monster.TryGetComponent<GameData.Player.Script.SurvivorEnemyStatus>(out var status)&&status.Airborne)return;
            float bottom=Mathf.Min(SoleHeight(leftGroundFoot,leftSoleVertices),SoleHeight(rightGroundFoot,rightSoleVertices));
            if(float.IsInfinity(bottom))return;
            groundVisual.position+=Vector3.up*(transform.position.y+.002f-bottom);
        }
        private static float SoleHeight(Transform foot,Vector3[] points)
        {
            float bottom=float.PositiveInfinity;if(!foot||points==null)return bottom;
            var matrix=foot.localToWorldMatrix;
            foreach(var point in points)bottom=Mathf.Min(bottom,matrix.MultiplyPoint3x4(point).y);
            return bottom;
        }
        public void TickHealing(float seconds)
        {
            var data=Data;if(!data||!monster.Health||monster.Health.Health<=0)return;
            healingClock+=Mathf.Max(0,seconds);
            if(healingClock<Mathf.Max(.1f,data.healingInterval))return;
            healingClock=0;
            foreach(var ally in CharacterHealth.ActiveMonsters){
                if(!ally||ally.Health<=0||(!data.healSelf&&ally==monster.Health)||(ally.transform.position-transform.position).sqrMagnitude>data.healingRadius*data.healingRadius)continue;
                if(ally.Health>=ally.MaxHealth)continue;
                float amount=data.healingFlat+data.healingPerLevel*Mathf.Max(0,monster.Level-1)+ally.MaxHealth*data.healingMaxHealthPercent*.01f;
                if(ally==monster.Health){ally.Heal(amount);continue;}
                if(!data.sliceProjectile)continue;
                var bread=SceneProjectilePool.Rent<BreadSliceProjectile>(data.sliceProjectile.gameObject,releasePoint?releasePoint.position:transform.position+Vector3.up,Quaternion.identity,gameObject.scene,32);
                bread.LaunchHealing(monster,ally,amount);bread.gameObject.SetActive(true);
            }
        }
        public void ApplySugar(CharacterHealth victim)
        {
            var data=Data;if(!data||!victim||victim.IsMonster||!victim.GetComponent<PlayerStats>())return;
            var effect=victim.GetComponent<BreadSugarEffect>();if(!effect)effect=victim.gameObject.AddComponent<BreadSugarEffect>();
            effect.Apply(data.SugarDamage(victim.MaxHealth,monster.Level),data.sugarTickSeconds,data.sugarDuration,gameObject);
        }
    }
}

