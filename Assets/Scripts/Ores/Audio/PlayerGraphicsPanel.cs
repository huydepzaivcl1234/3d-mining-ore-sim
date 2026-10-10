using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Graphics subpage of the existing settings menu, not a second input/pause owner.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerGraphicsPanel : MonoBehaviour
    {
        private PlayerGraphicsOptions options;
        private RectTransform page, content;
        private TMP_FontAsset font;
        private Sprite buttonSprite;
        private UnityEngine.UI.Slider sliderTemplate;
        private readonly List<Action> refreshers = new();
        private readonly List<Action> localizers = new();
        private TextMeshProUGUI hint;
        private int resolutionIndex;
        private Coroutine opening;
        private bool previewWasPending;
        private int lastCountdown = -1;

        public void Build(GameObject settings, PlayerGraphicsOptions graphics)
        {
            options = graphics;
            var template = settings.GetComponentInChildren<TextMeshProUGUI>(true);
            font = template != null ? template.font : TMP_Settings.defaultFontAsset;
            sliderTemplate = settings.transform.Find("Master Slider")?.GetComponent<UnityEngine.UI.Slider>();
            buttonSprite = settings.GetComponent<UnityEngine.UI.Image>()?.sprite;
            page = Rect("Graphics Page", settings.transform, new Vector2(752, 536));
            var background = page.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color32(255, 245, 219, 255);
            background.sprite = buttonSprite; background.type = UnityEngine.UI.Image.Type.Sliced;
            Text(page, "GRAPHICS", "ĐỒ HỌA", new Vector2(-190, 232), new Vector2(340, 40), 27);
            Button(page, "X", "X", new Vector2(335, 232), new Vector2(42, 40), Close);
            var viewport = Rect("Viewport", page, new Vector2(712, 398));
            viewport.anchoredPosition = new Vector2(0, 9);
            viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(1,1,1,.02f);
            viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            content = Rect("Content", viewport, new Vector2(712, 0));
            content.anchorMin = content.anchorMax = new Vector2(.5f, 1);
            content.pivot = new Vector2(.5f, 1);
            var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 7; layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
            hint = Text(page, "", "", new Vector2(0,-204), new Vector2(700,27), 14);
            Button(page, "RESET GRAPHICS", "MẶC ĐỊNH", new Vector2(-238,-242),new Vector2(210,36),()=> {options.ResetGraphics();Refresh();});
            Button(page, "KEEP DISPLAY", "GIỮ HIỂN THỊ", new Vector2(0,-242),new Vector2(210,36),()=> {options.ConfirmDisplay();Refresh();});
            Button(page, "BACK & SAVE", "LƯU & QUAY LẠI", new Vector2(238,-242),new Vector2(210,36),Close);
            page.gameObject.SetActive(false);

            // Keep the existing menu/controls actions, with three equal columns on their old row.
            ResizeEntry(settings.transform.Find("Return To Main Menu"), -253);
            ResizeEntry(settings.transform.Find("Controls"), 0);
            Button(settings.transform, "GRAPHICS", "ĐỒ HỌA", new Vector2(253,-148),new Vector2(238,48),Open);
            MiningLocalization.LanguageChanged += RefreshLanguage;
        }

        private static void ResizeEntry(Transform entry, float x)
        {
            if (entry is not RectTransform rect) return;
            rect.sizeDelta = new Vector2(238,48); rect.anchoredPosition = new Vector2(x,-148);
            foreach (var image in entry.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if (image.transform != entry && image.rectTransform.anchorMin == image.rectTransform.anchorMax)
                    image.rectTransform.sizeDelta = new Vector2(238,48);
            foreach (var text in entry.GetComponentsInChildren<TextMeshProUGUI>(true))
            { text.rectTransform.sizeDelta = new Vector2(225,42); text.fontSize = Mathf.Min(text.fontSize,19); }
        }

        public void Open()
        {
            options.Initialize();
            if (refreshers.Count == 0) Populate();
            RefreshLanguage(); Refresh(); page.SetAsLastSibling(); page.gameObject.SetActive(true);
            if (opening != null) StopCoroutine(opening);
            opening = StartCoroutine(AnimateOpen());
        }
        public void Close()
        {
            options.RevertDisplay(); options.Save();
            if (opening != null) StopCoroutine(opening);
            opening = null; page.localScale = Vector3.one; page.gameObject.SetActive(false);
        }

        private void Populate()
        {
            Label("DISPLAY", "HIỂN THỊ");
            Choice("Resolution / Hz", "Độ phân giải / Hz",()=>ResolutionText(),()=>
            {
                if (options.Resolutions.Length == 0) return;
                resolutionIndex=(resolutionIndex+1)%options.Resolutions.Length;
                options.BeginDisplayPreview(resolutionIndex,options.Values.fullscreen);
            });
            Choice("Fullscreen", "Toàn màn hình",()=>OnOff(options.Values.fullscreen),()=>
            {
                if(resolutionIndex>=0) options.BeginDisplayPreview(resolutionIndex,!options.Values.fullscreen);
            });
            Choice("VSync", "Đồng bộ VSync",()=>OnOff(options.Values.vsync),()=>options.Values.vsync=!options.Values.vsync);
            Label("QUALITY", "CHẤT LƯỢNG");
            Choice("Post-processing", "Hậu kỳ",()=>OnOff(options.Values.postProcessing),()=>options.Values.postProcessing=!options.Values.postProcessing);
            Choice("Camera AA", "Khử răng cưa",()=>new[]{"None","FXAA","SMAA"}[options.Values.aaMode],()=>options.Values.aaMode=(options.Values.aaMode+1)%3);
            Choice("AA quality", "Chất lượng AA",()=>new[]{"Low","Medium","High"}[options.Values.aaQuality],()=>options.Values.aaQuality=(options.Values.aaQuality+1)%3);
            Choice("MSAA", "MSAA",()=>options.SupportsMsaa?options.Values.msaa+"x":"Deferred: unavailable",()=>options.Values.msaa=options.Values.msaa==8?1:options.Values.msaa*2,()=>options.SupportsMsaa);
            Choice("Shadow cascades", "Phân tầng bóng",()=>options.Values.cascades.ToString(),()=>options.Values.cascades=options.Values.cascades%4+1);
            Slider("Shadow distance", "Khoảng cách bóng",0,150,()=>options.Values.shadowDistance,v=>options.Values.shadowDistance=v,"0 m");
            Slider("Render scale", "Tỷ lệ render",.5f,1.5f,()=>options.Values.renderScale,v=>options.Values.renderScale=v,"0.00x");
            Label("POST-PROCESSING", "HIỆU ỨNG HẬU KỲ");
            Choice("Bloom", "Ánh sáng Bloom",()=>OnOff(options.Values.bloom),()=>options.Values.bloom=!options.Values.bloom);
            Slider("Bloom strength", "Cường độ Bloom",0,3,()=>options.Values.bloomMultiplier,v=>options.Values.bloomMultiplier=v,"0.00x");
            Choice("Vignette", "Tối viền",()=>OnOff(options.Values.vignette),()=>options.Values.vignette=!options.Values.vignette);
            Choice("Motion blur", "Nhòe chuyển động",()=>OnOff(options.Values.motionBlur),()=>options.Values.motionBlur=!options.Values.motionBlur,()=>options.SupportsMotionBlur);
            Choice("Depth of field", "Độ sâu trường ảnh",()=>OnOff(options.Values.depthOfField),()=>options.Values.depthOfField=!options.Values.depthOfField,()=>options.SupportsDepthOfField);
            Choice("Ambient occlusion", "Bóng tiếp xúc SSAO",()=>OnOff(options.Values.ambientOcclusion),()=>options.Values.ambientOcclusion=!options.Values.ambientOcclusion,()=>options.SupportsAo);
            Slider("Brightness (offset)", "Độ sáng (bù)",-2,2,()=>options.Values.exposureOffset,v=>options.Values.exposureOffset=v,"+0.00;-0.00;0.00");
            Slider("Gamma (offset)", "Gamma (bù)",-.5f,.5f,()=>options.Values.gammaOffset,v=>options.Values.gammaOffset=v,"+0.00;-0.00;0.00");
        }

        private string OnOff(bool value) => value ? MiningLocalization.Text("ON","BẬT") : MiningLocalization.Text("OFF","TẮT");
        private string ResolutionText()
        {
            if(resolutionIndex<0) return options.Values.width+" x "+options.Values.height;
            var mode=options.Resolutions[resolutionIndex];
            return $"{mode.width} x {mode.height} ({mode.refreshRateRatio.value:0.##} Hz)";
        }
        private void Refresh()
        {
            resolutionIndex=PlayerGraphicsPreferences.MatchResolution(options.Resolutions,options.Values.width,options.Values.height,
                options.Values.refreshNumerator,options.Values.refreshDenominator);
            foreach(var refresher in refreshers) refresher();
        }
        private void RefreshLanguage() { lastCountdown=-1; foreach(var localizer in localizers) localizer(); if(options.Values!=null) Refresh(); }
        private void Update()
        {
            if(page==null || !page.gameObject.activeInHierarchy || options.Values==null) return;
            int countdown=Mathf.CeilToInt(options.DisplaySecondsRemaining);
            if(previewWasPending && !options.DisplayPending) Refresh();
            if(lastCountdown==countdown && previewWasPending==options.DisplayPending) return;
            previewWasPending=options.DisplayPending; lastCountdown=countdown;
            hint.text=options.DisplayPending
                ? MiningLocalization.Text($"Keep display? Reverting in {options.DisplaySecondsRemaining:0}s",$"Giữ hiển thị? Hoàn tác sau {options.DisplaySecondsRemaining:0}s")
                : MiningLocalization.Text(Application.isEditor?"Resolution/fullscreen must be tested in a build. Scroll for more.":"Click values to cycle. Scroll for more options.",
                    Application.isEditor?"Độ phân giải/toàn màn hình chỉ đổi trong bản build. Cuộn để xem thêm.":"Bấm giá trị để đổi. Cuộn để xem thêm.");
        }
        private void OnDisable()
        {
            if(page==null) return;
            options.RevertDisplay();
            if(opening!=null) StopCoroutine(opening);
            opening=null; page.localScale=Vector3.one; page.gameObject.SetActive(false);
        }
        private void OnDestroy() { MiningLocalization.LanguageChanged-=RefreshLanguage; }
        private IEnumerator AnimateOpen()
        {
            for(float t=0;t<.18f;t+=Time.unscaledDeltaTime)
            {page.localScale=Vector3.one*Mathf.Lerp(.92f,1,1-Mathf.Pow(1-Mathf.Clamp01(t/.18f),3));yield return null;}
            page.localScale=Vector3.one; opening=null;
        }

        private RectTransform Row(string name)
        {
            var row=Rect(name,content,new Vector2(712,42));
            var element=row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();element.preferredHeight=42;
            return row;
        }
        private void Label(string en,string vi) => Text(Row(en),en,vi,new Vector2(-165,0),new Vector2(370,38),21);
        private void Choice(string en,string vi,Func<string> read,Action change,Func<bool> available=null)
        {
            var row=Row(en);Text(row,en,vi,new Vector2(-165,0),new Vector2(370,38),18);
            var button=Button(row,"","",new Vector2(195,0),new Vector2(298,36),()=>{change();options.Apply();Refresh();});
            var value=button.GetComponentInChildren<TextMeshProUGUI>();
            refreshers.Add(()=>{button.interactable=available==null || available();value.text=button.interactable?read():MiningLocalization.Text("Unavailable","Không hỗ trợ");});
        }
        private void Slider(string en,string vi,float min,float max,Func<float> read,Action<float> write,string format)
        {
            if(sliderTemplate==null) return;
            var row=Row(en);Text(row,en,vi,new Vector2(-165,0),new Vector2(370,38),18);
            var slider=Instantiate(sliderTemplate,row);slider.name=en+" Slider";
            var rect=slider.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=new Vector2(145,0);rect.sizeDelta=new Vector2(192,16);
            slider.onValueChanged=new UnityEngine.UI.Slider.SliderEvent();slider.minValue=min;slider.maxValue=max;
            var value=Text(row,"","",new Vector2(292,0),new Vector2(105,36),16);
            slider.onValueChanged.AddListener(v=>{write(v);options.Apply();value.text=read().ToString(format);});
            refreshers.Add(()=>{slider.SetValueWithoutNotify(read());value.text=read().ToString(format);});
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 size)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;return rect;
        }
        private TextMeshProUGUI Text(Transform parent,string en,string vi,Vector2 position,Vector2 size,float fontSize)
        {
            var rect=Rect(en.Length>0?en:"Value",parent,size);rect.anchoredPosition=position;
            var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.fontSize=fontSize;
            label.color=new Color32(59,44,36,255);label.alignment=TextAlignmentOptions.MidlineLeft;
            label.raycastTarget=false;
            Action localize=()=>label.text=MiningLocalization.Text(en,vi);localizers.Add(localize);localize();return label;
        }
        private UnityEngine.UI.Button Button(Transform parent,string en,string vi,Vector2 position,Vector2 size,Action click)
        {
            var rect=Rect(en.Length>0?en:"Value Button",parent,size);rect.anchoredPosition=position;
            var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color32(255,210,83,255);
            image.sprite=buttonSprite;image.type=UnityEngine.UI.Image.Type.Sliced;
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            button.onClick.AddListener(()=>click());
            var text=Text(rect,en,vi,Vector2.zero,size-new Vector2(12,2),17);text.alignment=TextAlignmentOptions.Center;
            return button;
        }
    }
}
