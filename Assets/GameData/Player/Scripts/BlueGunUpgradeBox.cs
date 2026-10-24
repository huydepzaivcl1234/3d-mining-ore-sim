using MiningSimulator.Ores;
using UnityEngine;
namespace GameData.Player.Script
{
    public sealed class BlueGunUpgradeBox : MonoBehaviour
    {
        private MiningCardSystem cards;private BlueGunUpgradeData data;private float ready,fallVelocity;private bool collected,landed;private Vector3 landing;
        private OrangeGunUpgradeData orange;
        private void Start() => WorldActorVisibility.Attach(gameObject);
        public void Configure(MiningCardSystem owner,OrangeGunUpgradeData settings,Vector3 ground){cards=owner;orange=settings;landing=ground;ready=Time.time+settings.pickupDelay;}
        public void Configure(MiningCardSystem owner,BlueGunUpgradeData settings,Vector3 ground){cards=owner;data=settings;landing=ground;ready=Time.time+data.pickupDelay;}
        private void Update()
        {
            if(!landed){fallVelocity+=Physics.gravity.y*Time.deltaTime;var position=transform.position;position.y=Mathf.Max(landing.y,position.y+fallVelocity*Time.deltaTime);transform.position=position;landed=position.y<=landing.y;}
            if(!landed||collected||!cards||(!data&&!orange)||Time.time<ready||!cards.CanCollect)return;
            float radius=orange?orange.pickupRadius:data.pickupRadius;
            if((cards.Player.transform.position-transform.position).sqrMagnitude>radius*radius)return;
            if(orange?cards.CollectOrangeBox():cards.CollectBlueBox()){collected=true;Destroy(gameObject);}
        }
    }
}
