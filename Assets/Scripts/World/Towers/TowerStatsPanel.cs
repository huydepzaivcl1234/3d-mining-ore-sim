using TMPro;
using UnityEngine;
namespace MiningSimulator.Ores
{
    /// <summary>Non-modal world information. Movement remains available to walk away.</summary>
    public sealed class TowerStatsPanel : MonoBehaviour
    {
        private static TowerStatsPanel openedPanel;
        private TowerRuntime owner;
        private TowerData definition;
        private Transform player;
        private Camera viewer;
        private RectTransform panel;
        private CanvasGroup group;
        private TMP_Text title, levelLabel, hpLabel;
        private readonly TMP_Text[] statValues = new TMP_Text[6];
        private UnityEngine.UI.Image hpFill;
        private bool opened, previewMode, restoreShift;
        private float visibility, nextRefresh;
        private MiningOrbitCamera orbit;
        public bool IsOpen => opened;
        public RectTransform Panel => panel;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPanel() => openedPanel = null;
        public void Bind(TowerRuntime tower) { owner = tower; definition = tower.Data; Build(); }
        public void BindPreview(TowerData data, Transform followPlayer, Camera camera)
        { definition = data; player = followPlayer; viewer = camera; previewMode = true; Build(); opened = true; }
        public void Show(Transform followPlayer)
        {
            if (opened) return;
            if (owner == null || !owner.IsAlive || Vector3.Distance(followPlayer.position, owner.transform.position) > 4f) return;
            if (openedPanel != null && openedPanel != this) openedPanel.Close();
            openedPanel = this; player = followPlayer; viewer = Camera.main; opened = true;
            orbit = FindFirstObjectByType<MiningOrbitCamera>();
            restoreShift = orbit != null && orbit.IsShiftLocked;
            if (restoreShift) orbit.SetShiftLocked(false);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        public void Close()
        {
            opened = false;
            if (openedPanel == this) openedPanel = null;
            if (restoreShift && orbit != null && player != null && player.GetComponent<MiningCharacterHealth>()?.Health > 0)
                orbit.SetShiftLocked(true);
            restoreShift = false;
        }
        private void OnDisable() { Close(); if (panel != null) panel.gameObject.SetActive(false); visibility = 0; }
        private void OnEnable() { if (previewMode) opened = true; }
        private void Build()
        {
            if (panel != null || definition == null) return;
            panel=TowerFantasyUi.Frame("TowerStats",transform,new Vector2(520,740),Vector2.zero,TowerFantasyUi.Background,16);
            panel.localScale=Vector3.one*.004f;
            var canvas=panel.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=viewer!=null?viewer:Camera.main;
            panel.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            group=panel.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;
            title=TowerFantasyUi.Label(panel,"Title",new Vector2(-58,320),new Vector2(354,50),34,TowerFantasyUi.Cream,true);
            TowerFantasyUi.Label(panel,"Type",new Vector2(-96,286),new Vector2(278,24),14,TowerFantasyUi.Gold).text="DEFENSE TOWER";
            var badge=TowerFantasyUi.Frame("Level",panel,new Vector2(78,43),new Vector2(198,312),TowerFantasyUi.Cell,8);
            levelLabel=TowerFantasyUi.Label(badge,"LevelText",Vector2.zero,new Vector2(64,36),16,TowerFantasyUi.Gold,false,TextAlignmentOptions.Center);
            var iconFrame=TowerFantasyUi.Frame("IconFrame",panel,new Vector2(472,224),new Vector2(0,150),TowerFantasyUi.Cell,12);
            var image=TowerFantasyUi.Rect("CannonIcon",iconFrame,new Vector2(300,216),Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite=definition.icon;image.preserveAspect=true;image.raycastTarget=false;
            TowerFantasyUi.Label(panel,"Durability",new Vector2(-124,10),new Vector2(224,26),14,TowerFantasyUi.Gold).text="DURABILITY";
            hpLabel=TowerFantasyUi.Label(panel,"HP",new Vector2(124,10),new Vector2(224,26),16,TowerFantasyUi.Cream,false,TextAlignmentOptions.Right);
            hpFill=TowerFantasyUi.HealthFill(panel,new Vector2(472,22),new Vector2(0,-26));
            string[] captions={"DAMAGE","ATTACK SPEED","RANGE","PROJECTILE SPEED","ARMOR","MAGIC RESIST"};
            for(int i=0;i<6;i++)
            {
                var cell=TowerFantasyUi.Frame("Stat"+i,panel,new Vector2(230,70),new Vector2(i%2==0?-121:121,-89-(i/2)*80),TowerFantasyUi.Cell,9);
                TowerFantasyUi.Label(cell,"Caption",new Vector2(0,17),new Vector2(200,23),14,TowerFantasyUi.Gold).text=captions[i];
                statValues[i]=TowerFantasyUi.Label(cell,"Value",new Vector2(0,-11),new Vector2(200,33),24,TowerFantasyUi.Cream,false);
            }
            if(!previewMode)
            {
                var close=TowerFantasyUi.Rect("Close",panel,new Vector2(130,48),new Vector2(-169,-322));
                var closeGraphic=TowerFantasyUi.Box(close,TowerFantasyUi.Background,8);closeGraphic.raycastTarget=true;
                var closeButton=close.gameObject.AddComponent<UnityEngine.UI.Button>();closeButton.targetGraphic=closeGraphic;closeButton.onClick.AddListener(Close);
                TowerFantasyUi.Label(close,"Text",Vector2.zero,new Vector2(110,42),16,TowerFantasyUi.Gold).text="X  Close";
                var sell=TowerFantasyUi.Rect("Sell",panel,new Vector2(252,48),new Vector2(110,-322));
                var sellGraphic=TowerFantasyUi.Box(sell,TowerFantasyUi.Gold,10);sellGraphic.raycastTarget=true;
                var sellButton=sell.gameObject.AddComponent<UnityEngine.UI.Button>();sellButton.targetGraphic=sellGraphic;sellButton.onClick.AddListener(()=>owner?.Sell());
                TowerFantasyUi.Label(sell,"Text",Vector2.zero,new Vector2(230,44),16,TowerFantasyUi.Background,false,TextAlignmentOptions.Center).text=$"SELL  +{MiningMoneyFormatter.Format(owner.PaidPrice*.35f)}";
            }
            Refresh();panel.gameObject.SetActive(previewMode);
        }
        private void Refresh()
        {
            if(definition==null||title==null)return;
            float hp=owner!=null?owner.Health.Health:definition.health;
            float max=owner!=null?owner.Health.MaxHealth:definition.health;
            title.text=definition.displayName;levelLabel.text=$"LV. {definition.level}";
            hpLabel.text=$"{hp:0.#} / {max:0.#} HP";
            TowerFantasyUi.SetHealth(hpFill,hp/Mathf.Max(1,max));
            hpLabel.color=hp/max<=.2f?TowerFantasyUi.Critical:TowerFantasyUi.Cream;
            statValues[0].text=$"{definition.damage:0.##}";
            statValues[1].text=$"{definition.attackSpeed:0.##} / s";
            statValues[2].text=$"{definition.range:0.#} m";
            statValues[3].text=$"{definition.projectileSpeed:0.#} m / s";
            statValues[4].text=$"{definition.armor:0.#}";
            statValues[5].text=$"{definition.magicResistance:0.#}";
        }
        private void LateUpdate()
        {
            if (panel == null) return;
            if (!previewMode && opened && (owner == null || !owner.IsAlive || player == null ||
                Vector3.Distance(player.position,owner.transform.position)>5f || player.GetComponent<MiningCharacterHealth>()?.Health<=0)) Close();
            visibility = Mathf.MoveTowards(visibility, opened ? 1 : 0, Time.unscaledDeltaTime * 6);
            panel.gameObject.SetActive(visibility > .001f);
            group.alpha=visibility;group.interactable=group.blocksRaycasts=!previewMode&&opened&&visibility>.9f;
            if (viewer == null) viewer=Camera.main;
            if (viewer != null)
            {
                panel.GetComponent<Canvas>().worldCamera=viewer;
                panel.rotation=viewer.transform.rotation;
            }
            float top = owner != null ? owner.GetComponent<BoxCollider>().bounds.max.y-transform.position.y : 1.5f;
            panel.position=transform.position+Vector3.up*(top+1.65f)+(previewMode?Vector3.right*3.5f:Vector3.zero);
            panel.localScale=Vector3.one*(.004f*Mathf.Lerp(.85f,1,visibility));
            if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.1f;Refresh();}
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 point)
        {
            var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=point;return r;
        }
        private static TMP_Text Label(Transform parent,Vector2 point,Vector2 size,float font)
        {
            var text=Rect("Label",parent,size,point).gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize=font;text.alignment=TextAlignmentOptions.Center;text.color=new Color32(50,39,33,255);text.raycastTarget=false;
            var chest=TreasureChest.Active;if(chest!=null&&chest.Data.font!=null)text.font=chest.Data.font;
            return text;
        }
    }
}
