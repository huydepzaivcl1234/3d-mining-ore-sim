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
        private TMP_Text title, details, hpLabel;
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
            panel = Rect("TowerStats", transform, new Vector2(720, 430), Vector2.zero);
            panel.localScale = Vector3.one * .004f;
            var canvas = panel.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = viewer != null ? viewer : Camera.main;
            panel.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            group = panel.gameObject.AddComponent<CanvasGroup>();group.alpha = 0;group.blocksRaycasts = false;
            var bg = panel.gameObject.AddComponent<UnityEngine.UI.Image>(); bg.color = new Color32(255,246,223,245);bg.raycastTarget = false;
            title = Label(panel, new Vector2(0,165), new Vector2(540,70), 40);
            hpFill = Rect("HealthFill", panel, new Vector2(640,24), new Vector2(0,107)).gameObject.AddComponent<UnityEngine.UI.Image>();
            hpFill.color = new Color32(235,68,75,255);hpFill.raycastTarget = false;
            hpLabel = Label(panel, new Vector2(0,68), new Vector2(640,44), 27);
            details = Label(panel, new Vector2(0,-55), new Vector2(640,175), 30);
            if (!previewMode)
            {
                var rect = Rect("Close",panel,new Vector2(62,62),new Vector2(310,167));
                var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color = new Color32(239,93,92,255);
                var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.onClick.AddListener(Close);
                Label(rect,Vector2.zero,new Vector2(62,62),34).text="X";
                var sell = Rect("Sell", panel, new Vector2(400,56),new Vector2(0,-166));
                var sellImage=sell.gameObject.AddComponent<UnityEngine.UI.Image>();sellImage.color=new Color32(255,210,77,255);
                var sellButton=sell.gameObject.AddComponent<UnityEngine.UI.Button>();sellButton.targetGraphic=sellImage;
                sellButton.onClick.AddListener(()=>owner?.Sell());
                Label(sell,Vector2.zero,new Vector2(400,56),27).text=$"SELL +{MiningMoneyFormatter.Format(owner.PaidPrice*.35f)}";
            }
            Refresh(); panel.gameObject.SetActive(previewMode);
        }
        private void Refresh()
        {
            if (definition == null || title == null) return;
            float hp = owner != null ? owner.Health.Health : definition.health;
            float max = owner != null ? owner.Health.MaxHealth : definition.health;
            title.text=$"{definition.displayName}  •  LV {definition.level}";
            hpLabel.text=$"{hp:0.#} / {max:0.#} HP";
            hpFill.rectTransform.sizeDelta=new Vector2(640*Mathf.Clamp01(hp/Mathf.Max(1,max)),24);
            details.text=$"DMG {definition.damage:0.##}    AS {definition.attackSpeed:0.##}/s\nRange {definition.range:0.#}m    Projectile {definition.projectileSpeed:0.#}m/s\nArmor {definition.armor:0.#}    MR {definition.magicResistance:0.#}";
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
            panel.position=transform.position+Vector3.up*(top+1.15f)+(previewMode?Vector3.right*3.5f:Vector3.zero);
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
