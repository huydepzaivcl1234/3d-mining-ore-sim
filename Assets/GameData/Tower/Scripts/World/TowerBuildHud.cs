using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Owns drawer visibility, shortcut and the modal-to-placement handoff.</summary>
    public sealed class TowerBuildHud : MonoBehaviour
    {
        [SerializeField, Min(.01f)] float transitionSeconds=.22f;
        RectTransform drawer;
        CanvasGroup group;
        Button button;
        TMP_Text buttonText;
        TowerShopPanel shop;
        MiningUiPanelCoordinator panels;
        MiningPlayerStats player;
        PlayerInput input;
        InputAction buildAction;
        CursorLockMode cursor;
        bool cursorVisible, shiftLocked, opened;
        float visibility;
        float nextBindingRefresh;
        string bindingText;
        public bool IsOpen=>opened;
        public TowerShopPanel Shop=>shop;

        public static TowerBuildHud Ensure(PlayerWallet wallet,MiningUiPanelCoordinator coordinator)
        {
            var hud=FindFirstObjectByType<TowerBuildHud>();
            if(hud!=null)return hud;
            var go=new GameObject("Tower Construction HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=40;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            hud=go.AddComponent<TowerBuildHud>();hud.Initialize(wallet,coordinator);return hud;
        }
        void Initialize(PlayerWallet wallet,MiningUiPanelCoordinator coordinator)
        {
            panels=coordinator!=null?coordinator:FindFirstObjectByType<MiningUiPanelCoordinator>();
            player=FindFirstObjectByType<MiningPlayerStats>();
            input=player!=null?player.GetComponent<PlayerInput>():null;
            buildAction=input!=null?input.actions.FindAction("Player/Build",false):null;
            drawer=TowerFantasyUi.Frame("Construction Drawer",transform,new Vector2(520,624),Vector2.zero,TowerFantasyUi.Background,16);
            drawer.anchorMin=drawer.anchorMax=new Vector2(1,.5f);drawer.pivot=new Vector2(1,.5f);
            group=drawer.gameObject.AddComponent<CanvasGroup>();drawer.gameObject.SetActive(false);
            shop=drawer.gameObject.AddComponent<TowerShopPanel>();shop.Initialize(wallet,CloseImmediately);
            var r=TowerFantasyUi.Frame("Build Button",transform,new Vector2(220,68),Vector2.zero,TowerFantasyUi.Background,12);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(1,0);r.anchoredPosition=new Vector2(-24,164);
            button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();button.targetGraphic.raycastTarget=true;
            button.onClick.AddListener(Toggle);
            var icon=TowerFantasyUi.Rect("Cannon Icon",r,new Vector2(48,48),new Vector2(-78,0)).gameObject.AddComponent<Image>();
            var catalog=Resources.Load<TowerCatalog>("TowerCatalog");
            if(catalog!=null&&catalog.towers!=null)foreach(var d in catalog.towers)if(d!=null){icon.sprite=d.icon;break;}
            icon.preserveAspect=true;icon.raycastTarget=false;
            buttonText=TowerFantasyUi.Label(r,"Build label",new Vector2(24,0),new Vector2(155,56),21,TowerFantasyUi.Cream,true,TextAlignmentOptions.Center);
            RefreshLanguage();Resize();
        }
        void OnEnable()=>MiningLocalization.LanguageChanged+=RefreshLanguage;
        void OnDisable(){MiningLocalization.LanguageChanged-=RefreshLanguage;CloseImmediately();}
        void RefreshLanguage()
        {
            if(buttonText==null)return;
            bindingText=buildAction!=null?buildAction.GetBindingDisplayString(0):"";
            buttonText.text=TowerShopPanel.L("Build","Xây dựng")+(string.IsNullOrEmpty(bindingText)?"":"  ["+bindingText+"]");
        }
        public void Toggle(){if(opened)Close();else Open();}
        public void Open()
        {
            if(drawer==null||opened||TowerPlacement.IsPlacing||player==null||!player.gameObject.activeInHierarchy||
                player.GetComponent<MiningCharacterHealth>()?.Health<=0||panels!=null&&panels.BlocksGameplay)return;
            cursor=Cursor.lockState;cursorVisible=Cursor.visible;
            opened=true;drawer.gameObject.SetActive(true);shop.ShowCatalog();
            panels?.NotifyExternalPanelOpened(drawer);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;Resize();
        }
        public void Close()
        {
            if(!opened)return;
            opened=false;group.interactable=group.blocksRaycasts=false;
            panels?.NotifyExternalPanelClosed(drawer);
            // Do not restore cursor over another modal that replaced this drawer.
            if(panels==null||!panels.BlocksGameplay)
            {
                Cursor.lockState=cursor;Cursor.visible=cursorVisible;
            }
        }
        public void CloseImmediately()
        {
            Close();visibility=0;
            if(drawer!=null)drawer.gameObject.SetActive(false);
        }
        void Resize()
        {
            if(drawer==null)return;
            var canvasRect=(RectTransform)transform;
            float availableWidth=canvasRect.rect.width, availableHeight=canvasRect.rect.height;
            if(availableWidth<100||availableHeight<100)return;
            float scale=Mathf.Min(1,availableWidth*.29f/520,(availableHeight-48)/drawer.sizeDelta.y);
            drawer.localScale=Vector3.one*scale;
            drawer.anchoredPosition=new Vector2(Mathf.Lerp(drawer.sizeDelta.x*scale+24,-24,Mathf.SmoothStep(0,1,visibility)),0);
        }
        void Update()
        {
            if(drawer==null)return;
            if(opened&&(!drawer.gameObject.activeSelf||player==null||player.GetComponent<MiningCharacterHealth>()?.Health<=0))CloseImmediately();
            if(buildAction?.WasPressedThisFrame()==true)Toggle();
            if(opened&&Keyboard.current?.escapeKey.wasPressedThisFrame==true)Close();
            visibility=Mathf.MoveTowards(visibility,opened?1:0,Time.unscaledDeltaTime/transitionSeconds);
            group.alpha=Mathf.SmoothStep(0,1,visibility);group.interactable=group.blocksRaycasts=opened&&visibility>.95f;
            if(!opened&&visibility==0)drawer.gameObject.SetActive(false);
            Resize();
            button.gameObject.SetActive(!opened&&visibility==0&&!TowerPlacement.IsPlacing&&player!=null&&player.gameObject.activeInHierarchy&&
                !(player.GetComponent<MiningCharacterHealth>()?.Health<=0)&&(panels==null||!panels.BlocksGameplay));
            if(Time.unscaledTime>=nextBindingRefresh)
            {
                nextBindingRefresh=Time.unscaledTime+.2f;
                if(buildAction!=null&&bindingText!=buildAction.GetBindingDisplayString(0))RefreshLanguage();
            }
        }
    }
}
