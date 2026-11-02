using UnityEngine;
using MiningSimulator.Ores;
namespace GameData.Player.Script
{
    [DisallowMultipleComponent]
    public sealed class SurvivorLootBox : MonoBehaviour
    {
        public GameObject shinyEffects;
        private SurvivorDailyLoot owner;
        private MiningCardSystem cards;
        private float readyAt;
        private GameObject animeStars;
        public bool Shiny{get;private set;}
        public bool IsFalling{get;private set;}
        public Vector3 LandingPoint{get;private set;}
        private float fallSpeed;
        public void BeginDrop(Vector3 point){LandingPoint=point;IsFalling=true;fallSpeed=0;transform.position=point+Vector3.up*owner.data.dropHeight;}
        private void TickFall(float dt){fallSpeed=Mathf.Min(Mathf.Max(.1f,owner.data.maximumFallSpeed),fallSpeed+Mathf.Max(.1f,owner.data.dropGravity)*dt);transform.position=Vector3.MoveTowards(transform.position,LandingPoint,fallSpeed*dt);if((transform.position-LandingPoint).sqrMagnitude<.000001f){transform.position=LandingPoint;IsFalling=false;readyAt=Time.time+owner.data.pickupDelay;}}
        public void Configure(SurvivorDailyLoot system,MiningCardSystem cardSystem,bool shiny){owner=system;cards=cardSystem;Shiny=shiny;if(shiny&&system.data.shinyVfx&&!animeStars){if(shinyEffects)shinyEffects.SetActive(false);animeStars=Instantiate(system.data.shinyVfx,transform);animeStars.name="Anime star sparkles";animeStars.transform.localPosition=system.data.shinyVfxOffset;animeStars.transform.localScale=Vector3.one*system.data.shinyVfxScale;shinyEffects=animeStars;}readyAt=Time.time+owner.data.pickupDelay;if(shinyEffects)shinyEffects.SetActive(shiny);if(shiny){var properties=new MaterialPropertyBlock();foreach(var renderer in GetComponentsInChildren<MeshRenderer>()){if(!renderer.sharedMaterial||!renderer.sharedMaterial.HasProperty("_EmissionColor"))continue;properties.SetColor("_EmissionColor",renderer.sharedMaterial.GetColor("_EmissionColor")*3);renderer.SetPropertyBlock(properties);}}}
        private void Update(){if(IsFalling){if(owner&&!WorldActorVisibility.ShouldPause(this))TickFall(Time.deltaTime);return;}if(!owner||!cards||Time.time<readyAt||WorldActorVisibility.ShouldPause(this)||!cards.CanCollect)return;owner.TryOpen(this);}
    }
}
