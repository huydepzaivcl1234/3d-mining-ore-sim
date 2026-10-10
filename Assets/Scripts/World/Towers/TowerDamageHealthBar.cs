using TMPro;
using UnityEngine;
namespace MiningSimulator.Ores
{
    /// <summary>Only damage reveals this compact billboard. Repeated hits restart its deadline.</summary>
    public sealed class TowerDamageHealthBar : MonoBehaviour
    {
        [SerializeField,Min(0)] float secondsAfterDamage=3;
        TowerRuntime owner;MiningCharacterHealth health;BoxCollider body;Camera viewer;
        RectTransform panel;TMP_Text nameLabel,hpLabel;UnityEngine.UI.Image fill;
        float expires=float.NegativeInfinity,displayedFraction=1;
        public bool IsVisible=>panel!=null&&panel.gameObject.activeSelf;
        public RectTransform Panel=>panel;
        public void Bind(TowerRuntime tower)
        {
            if(health!=null)health.Damaged-=OnDamage;
            owner=tower;health=owner.Health;body=owner.GetComponent<BoxCollider>();
            if(isActiveAndEnabled)health.Damaged+=OnDamage;
            if(panel!=null)return;
            panel=TowerFantasyUi.Frame("DamageHealth",transform,new Vector2(420,90),Vector2.zero,TowerFantasyUi.Background);
            panel.localScale=Vector3.one*.004f;panel.gameObject.AddComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            nameLabel=TowerFantasyUi.Label(panel,"Name",new Vector2(-86,22),new Vector2(220,28),16,TowerFantasyUi.Cream);
            hpLabel=TowerFantasyUi.Label(panel,"HP",new Vector2(126,22),new Vector2(136,28),16,TowerFantasyUi.Cream,false,TextAlignmentOptions.Right);
            fill=TowerFantasyUi.HealthFill(panel,new Vector2(388,18),new Vector2(0,-8));
            panel.gameObject.SetActive(false);
        }
        void OnEnable(){if(health!=null)health.Damaged+=OnDamage;}
        void OnDisable(){if(health!=null)health.Damaged-=OnDamage;expires=float.NegativeInfinity;if(panel!=null)panel.gameObject.SetActive(false);}
        void OnDamage(){if(owner==null||!owner.IsAlive)return;expires=Time.unscaledTime+secondsAfterDamage;panel.gameObject.SetActive(true);Refresh();}
        void Refresh()
        {
            float fraction=Mathf.Clamp01(health.Health/Mathf.Max(1,health.MaxHealth));
            displayedFraction=Mathf.MoveTowards(displayedFraction,fraction,Time.unscaledDeltaTime*5);
            TowerFantasyUi.SetHealth(fill,displayedFraction);
            // Critical color responds immediately, even while the fill animates.
            fill.color=fraction<=.2f?TowerFantasyUi.Critical:TowerFantasyUi.Teal;
            nameLabel.text=$"{owner.Data.displayName}  •  LV. {owner.Data.level}";
            hpLabel.text=$"{health.Health:0.#} / {health.MaxHealth:0.#} HP";hpLabel.color=fill.color==TowerFantasyUi.Critical?TowerFantasyUi.Critical:TowerFantasyUi.Cream;
        }
        void LateUpdate()
        {
            if(panel==null)return;
            bool show=owner!=null&&owner.IsAlive&&Time.unscaledTime<expires;panel.gameObject.SetActive(show);if(!show)return;
            if(viewer==null)viewer=Camera.main;
            if(viewer!=null){panel.rotation=viewer.transform.rotation;panel.GetComponent<Canvas>().worldCamera=viewer;}
            panel.position=new Vector3(transform.position.x,body.bounds.max.y+.3f,transform.position.z);Refresh();
        }
    }
}
