using UnityEngine;
using UnityEngine.EventSystems;

namespace MiningSimulator.Ores
{
    /// <summary>Rune presentation only: snapped, unscaled scrolling like the upgrade carousel.</summary>
    public sealed class RuneUpgradeScrollRect : UnityEngine.UI.ScrollRect
    {
        private RuneUpgradeData settings;
        private RectTransform[] cards;
        private CanvasGroup[] groups;
        private MiningUpgradeCardHover[] hover;
        private UnityEngine.UI.Scrollbar bar;
        private int first;
        public int FirstVisibleIndex => first;
        public void Initialize(RuneUpgradeRow[] rows, RuneUpgradeData data)
        {
            if (cards != null) return;
            settings = data;
            cards = new RectTransform[rows.Length];
            groups = new CanvasGroup[rows.Length];
            hover = new MiningUpgradeCardHover[rows.Length];
            float height = data.barSize.y * data.visibleRows + data.panelSpacing * (data.visibleRows - 1);
            viewport = Rect("Viewport", transform, new Vector2(data.barSize.x + 16, height), new Vector2(-12,0));
            var background = viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(.015f,.025f,.045f,.94f);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            content = Rect("Content", viewport, new Vector2(data.barSize.x + 16, rows.Length * (data.barSize.y + data.panelSpacing)), Vector2.zero);
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f,1);
            content.anchoredPosition = Vector2.zero;
            for (int i=0; i<rows.Length; i++)
            {
                cards[i] = (RectTransform)rows[i].transform;
                cards[i].SetParent(content,false);
                cards[i].anchorMin = cards[i].anchorMax = cards[i].pivot = new Vector2(.5f,1);
                cards[i].sizeDelta = data.barSize;
                cards[i].anchoredPosition = new Vector2(0,-i * (data.barSize.y + data.panelSpacing));
                groups[i] = rows[i].GetComponent<CanvasGroup>();
                if (groups[i] == null) groups[i] = rows[i].gameObject.AddComponent<CanvasGroup>();
                hover[i] = rows[i].GetComponent<MiningUpgradeCardHover>();
                if (hover[i] == null) hover[i] = rows[i].gameObject.AddComponent<MiningUpgradeCardHover>();
            }
            var track = Rect("Scrollbar",transform,new Vector2(18,height),new Vector2(data.barSize.x*.5f+18,0));
            var trackImage = track.gameObject.AddComponent<UnityEngine.UI.Image>();
            trackImage.color = new Color(.025f,.25f,.3f,1);
            var handle = Rect("Handle",track,new Vector2(18,50),Vector2.zero);
            var handleImage = handle.gameObject.AddComponent<UnityEngine.UI.Image>();
            handleImage.color = new Color(.15f,.85f,1,1);
            bar = track.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();
            bar.handleRect = handle; bar.targetGraphic = handleImage;
            bar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            bar.size = Mathf.Min(1,(float)data.visibleRows/Mathf.Max(1,rows.Length));
            bar.numberOfSteps = Mathf.Max(2,rows.Length);
            bar.onValueChanged.AddListener(SelectFromBar);
            bar.SetValueWithoutNotify(1);
            horizontal = false; vertical = true; inertia = false;
            movementType = MovementType.Clamped;
            ApplyPresentation(1);
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = (RectTransform)new GameObject(name,typeof(RectTransform)).transform;
            rect.SetParent(parent,false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
            rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        private void SelectFromBar(float value) => ScrollTo(Mathf.RoundToInt((1-value)*Mathf.Max(0,cards.Length-1)));
        public void ScrollTo(int index)
        {
            first = Mathf.Clamp(index,0,Mathf.Max(0,cards.Length-1));
            bar.SetValueWithoutNotify(cards.Length<=1 ? 1 : 1-(float)first/(cards.Length-1));
        }
        public override void OnScroll(PointerEventData eventData)
        {
            if (cards != null && Mathf.Abs(eventData.scrollDelta.y)>.001f)
                ScrollTo(first+(eventData.scrollDelta.y<0 ? 1 : -1));
        }
        public override void OnBeginDrag(PointerEventData eventData) { }
        public override void OnDrag(PointerEventData eventData) { }
        public override void OnEndDrag(PointerEventData eventData) { }
        protected override void LateUpdate()
        {
            if (cards == null) return;
            ApplyPresentation(1-Mathf.Exp(-Time.unscaledDeltaTime*5/Mathf.Max(.01f,settings.scrollSeconds)));
        }
        private void ApplyPresentation(float blend)
        {
            content.anchoredPosition = Vector2.Lerp(content.anchoredPosition,new Vector2(0,first*(settings.barSize.y+settings.panelSpacing)),blend);
            for (int i=0;i<cards.Length;i++)
            {
                int slot=i-first;
                bool visible=slot>=0 && slot<settings.visibleRows;
                groups[i].blocksRaycasts=groups[i].interactable=visible;
                float alpha=visible ? Mathf.Pow(settings.lowerRowOpacity,slot) : 0;
                groups[i].alpha=visible ? Mathf.Lerp(groups[i].alpha,alpha,blend) : 0;
                float scale=visible ? 1-slot*.055f : .85f;
                if(slot==0 && hover[i].IsHovered) scale+=.025f;
                cards[i].localScale=Vector3.Lerp(cards[i].localScale,Vector3.one*scale,blend);
            }
        }
        protected override void OnDestroy()
        {
            if(bar!=null) bar.onValueChanged.RemoveListener(SelectFromBar);
            base.OnDestroy();
        }
    }
}
