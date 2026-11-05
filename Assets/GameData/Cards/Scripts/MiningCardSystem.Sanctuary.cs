using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.VectorGraphics;
using UnityEngine.U2D;
using GameData.Player.Script;

namespace MiningSimulator.Ores
{
    public sealed partial class MiningCardSystem
    {
        [SerializeField] UpgradeSanctuarySkin sanctuarySkin;
        const string TransparentBackdropKey="Survivor.UpgradeUI.TransparentBackdrop";
        bool transparentBackdrop, backdropPreferenceLoaded;
        Coroutine pickFeedback;
        MiningAudioManager panelAudio;
        UpgradeSanctuarySkin.Artwork Artwork(SurvivorUpgradeId id)
        {
            if(!sanctuarySkin)return null;
            return id switch {SurvivorUpgradeId.PhysicalDamage=>sanctuarySkin.sword,SurvivorUpgradeId.MaxHealth or SurvivorUpgradeId.LifeSpring or SurvivorUpgradeId.EmergencyHeal=>sanctuarySkin.heart,
                SurvivorUpgradeId.ProjectileSpeed or SurvivorUpgradeId.AttackSpeed or SurvivorUpgradeId.MoveSpeed or SurvivorUpgradeId.Quickening=>sanctuarySkin.speed,
                SurvivorUpgradeId.Burn=>sanctuarySkin.fire,SurvivorUpgradeId.Ricochet or SurvivorUpgradeId.SplitShot=>sanctuarySkin.ricochet,SurvivorUpgradeId.ChargedCube=>sanctuarySkin.cube,_=>null};
        }
        void PanelAudio(bool open)
        {if(!Application.isPlaying)return;if(!panelAudio)panelAudio=FindAnyObjectByType<MiningAudioManager>();if(!panelAudio)return;if(open)panelAudio.BeginUpgradePanelAudio();else panelAudio.EndUpgradePanelAudio();}
        void SanctuaryInput()
        {
            if(!choiceOpen||Keyboard.current==null)return;
            if(Keyboard.current.escapeKey.wasPressedThisFrame){ClosePending();return;}
            if(Keyboard.current.digit1Key.wasPressedThisFrame)PickWithFeedback(0);
            if(Keyboard.current.digit2Key.wasPressedThisFrame)PickWithFeedback(1);
            if(Keyboard.current.digit3Key.wasPressedThisFrame)PickWithFeedback(2);
        }
        void PickWithFeedback(int index)
        {
            if(!choiceOpen||pickFeedback!=null||index>=offers.Count||index<0)return;
            foreach(var choice in choices){var motion=choice?choice.GetComponent<UpgradeChoiceFx>():null;if(motion&&motion.enabled&&motion.IsRevealing)return;}
            pickFeedback=StartCoroutine(ConfirmPick(index));
        }
        IEnumerator ConfirmPick(int index)
        {
            foreach(var b in choices)if(b)b.interactable=false;
            choices[index].GetComponent<UpgradeChoiceFx>()?.Confirm();
            if(!panelAudio)panelAudio=FindAnyObjectByType<MiningAudioManager>();panelAudio?.PlayCardPickSfx(offers[index].rarity);
            yield return new WaitForSecondsRealtime(.38f);
            pickFeedback=null;if(choiceOpen)ChooseOffer(index);
        }
        void RevealOffers()
        {
            for(int i=0;i<Mathf.Min(3,choices.Length);i++)if(choices[i]&&choices[i].gameObject.activeInHierarchy){var fx=choices[i].GetComponent<UpgradeChoiceFx>();if(fx&&fx.enabled)fx.PlayReveal(.10f+i*.08f);}
        }
        void CancelPickFeedback(){if(pickFeedback!=null)StopCoroutine(pickFeedback);pickFeedback=null;}

        [ContextMenu("UI/Build default Sanctuary layout (resets authored layout)")]
        void RefreshSanctuary()
        {
            if(!choicePanel||!title||choices==null||choices.Length<3)return;
            if(sanctuarySkin){headingFont=sanctuarySkin.heading;bodyFont=sanctuarySkin.body;}
            if(!backdropPreferenceLoaded){transparentBackdrop=PlayerPrefs.GetInt(TransparentBackdropKey,0)!=0;backdropPreferenceLoaded=true;}
            // 1728/1920 safe frame. Build / gutter / offers = 448 / 64 / 1152.
            choicePanel.sizeDelta=new Vector2(1728,896);
            var old=choicePanel.GetComponent<Image>();if(old){old.color=Color.clear;old.raycastTarget=true;}
            var outline=choicePanel.GetComponent<Outline>();if(outline)outline.enabled=false;
            foreach(string n in new[]{"OfferHeader","ChoiceHint","BuildInventory","RefreshOffer","SkipOffer","PauseNote"})Hide(choicePanel,n);
            var backdrop=Node(choicePanel,"Sanctuary backdrop");backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.offsetMin=new Vector2(-2000,-1200);backdrop.offsetMax=new Vector2(2000,1200);backdrop.SetAsFirstSibling();
            Paint(backdrop,"Fill",Hex("162824"),Hex("07111D"),Color.clear,0).raycastTarget=true;
            if(sanctuarySkin){VectorArt(choicePanel,"Ambient left",sanctuarySkin.ambientLeft,-80,-130,800,800);VectorArt(choicePanel,"Ambient right",sanctuarySkin.ambientRight,1120,400,800,800);}
            var shadow=Node(choicePanel,"Panel shadow");Pixel(shadow,-20,20,1768,916);Paint(shadow,"Fill",new Color(0,0,0,.3f),new Color(0,0,0,.48f),Color.clear,34);shadow.SetSiblingIndex(3);
            Paint(choicePanel,"Sanctuary frame",Hex("1D2D3B"),Hex("0F1A26"),new Color(.85f,.714f,.455f,.48f),24).transform.SetSiblingIndex(4);
            Rule(choicePanel,"Top highlight",32,0,320,2,Hex("D9B674"));Rule(choicePanel,"Header divider",32,181,1664,1,new Color(.85f,.714f,.455f,.2f));
            L(choicePanel,"Eyebrow","SURVIVOR  /  SANCTUARY",32,28,800,22,13,Hex("B6A57C"),false,true);
            Pixel(title.rectTransform,32,65,1200,75);title.font=headingFont;title.fontStyle=FontStyles.Normal;title.fontSize=55;title.enableAutoSizing=false;title.alignment=TextAlignmentOptions.TopLeft;title.color=Hex("F5E0AB");title.text=abilityChoice?"CHOOSE AN ABILITY":"CHOOSE AN UPGRADE";title.transform.SetAsLastSibling();
            L(choicePanel,"Sanctuary subtitle","One choice. A stronger run.",34,143,1000,30,21,Hex("9FB4BF"));
            var queue=Node(choicePanel,"Queue pill");Pixel(queue,1286,107,364,30);Paint(queue,"Fill",Hex("10212B"),Hex("10212B"),new Color(.85f,.714f,.455f,.16f),15);
            L(queue,"Text",$"{(abilityChoice?"ABILITY":"STAT")} CARDS  ·  {(abilityChoice?PendingAbilityUpgrades:PendingStatUpgrades)} WAITING",0,0,364,30,13,Hex("D9B674"),false,true).alignment=TextAlignmentOptions.Center;
            EnsureCloseButton();var close=(RectTransform)choicePanel.Find("ClosePending");Pixel(close,1652,28,44,44);Paint(close,"Fill",Hex("293C49"),Hex("293C49"),Color.clear,12);close.Find("X").SetAsLastSibling();close.SetAsLastSibling();
            BuildSanctuaryInventory();for(int i=0;i<3;i++)BuildSanctuaryOffer(i);
            BuildTransparencyButton();
            var later=Node(choicePanel,"Sanctuary later");Pixel(later,32,832,200,42);var surface=Paint(later,"Fill",Hex("243440"),Hex("243440"),new Color(.7f,.77f,.81f,.18f),9);surface.raycastTarget=true;
            var lb=later.GetComponent<Button>();if(!lb)lb=later.gameObject.AddComponent<Button>();lb.targetGraphic=surface;lb.onClick.RemoveListener(ClosePending);lb.onClick.AddListener(ClosePending);
            L(later,"Label","LATER",20,0,125,42,15,Hex("E1D8C2"),false,true).alignment=TextAlignmentOptions.MidlineLeft;L(later,"Esc","Esc",148,0,40,42,12,Hex("9FB4BF")).alignment=TextAlignmentOptions.Midline;
            L(choicePanel,"Keyboard hint","1–3  Choose an offer",544,841,400,25,15,Hex("9FB4BF"));L(choicePanel,"Paused hint","The game is paused while you choose an upgrade.",1100,841,560,25,14,Hex("9FB4BF")).alignment=TextAlignmentOptions.TopRight;
            var sparks=GetComponent<UpgradeSparkVfx>();if(!sparks&&data&&data.sparkleVfx)sparks=gameObject.AddComponent<UpgradeSparkVfx>();if(sparks)sparks.Refresh(choices,offers,data);
        }
        void BuildSanctuaryOffer(int index)
        {
            var button=choices[index];if(!button)return;var rect=(RectTransform)button.transform;
            Pixel(rect,544+index*392,196,368,608);rect.localScale=Vector3.one;rect.SetAsLastSibling();
            var le=button.GetComponent<LayoutElement>();if(!le)le=button.gameObject.AddComponent<LayoutElement>();le.ignoreLayout=true;
            foreach(Transform child in rect)child.gameObject.SetActive(false);
            var image=button.GetComponent<Image>();if(image){image.color=Color.clear;image.raycastTarget=true;}
            var border=button.GetComponent<Outline>();if(border)border.enabled=false;button.transition=Selectable.Transition.None;
            bool has=index<offers.Count;var offer=has?offers[index]:default;var def=has&&data&&data.survivorUpgrades?data.survivorUpgrades.Find(offer.id):null;
            var art=Artwork(offer.id);Color accent=Hex(offer.id==SurvivorUpgradeId.Burn?"F1B66B":offer.id==SurvivorUpgradeId.ChargedCube?"CCA2F6":offer.id==SurvivorUpgradeId.MaxHealth?"CAA5F1":offer.rarity==0?"B7C5CF":offer.rarity==1?"8BD4C5":offer.rarity==2?"91BFF1":offer.rarity==3?"CCA2F6":"F1C56B");
            var frame=Paint(rect,"Frame",Hex("1B2A38"),Hex("111B27"),new Color(accent.r,accent.g,accent.b,.44f),18);
            var chamber=Node(rect,"Visual chamber");Pixel(chamber,1,1,366,231);Paint(chamber,"Fill",Color.Lerp(accent,Hex("101C29"),.82f),Hex("101C29"),Color.clear,17);
            if(art!=null){VectorArt(rect,"Art glow",art.glow,28,-25,312,312);VectorArt(rect,"Art ring",art.ring,96,37,176,176);VectorArt(rect,"Art medallion",art.medallion,91,42,186,186);VectorArt(rect,"Figma icon",art.icon,137,77,94,94);}
            else{
                if(sanctuarySkin){var neutral=sanctuarySkin.sword;VectorArt(rect,"Art glow",neutral.glow,28,-25,312,312);VectorArt(rect,"Art ring",neutral.ring,96,37,176,176);VectorArt(rect,"Art medallion",neutral.medallion,91,42,186,186);}
                RenderIcon(rect,"Authored icon",def?.icon?def.icon:starIcon?starIcon:sanctuarySkin?sanctuarySkin.spark:null,137,77,94,94);
            }
            var pill=Node(rect,"Rarity pill");Pixel(pill,20,18,offer.shiny?175:124,30);Paint(pill,"Fill",new Color(.025f,.06f,.09f,.8f),new Color(.025f,.06f,.09f,.8f),new Color(accent.r,accent.g,accent.b,.22f),15);
            L(pill,"Label",(offer.shiny?"SPARKLING · ":"")+SurvivorUpgradeData.RarityName(offer.rarity).ToUpperInvariant(),0,0,offer.shiny?175:124,30,12,accent,false,true).alignment=TextAlignmentOptions.Center;
            L(rect,"Number",(index+1).ToString(),318,18,30,30,14,Hex("DDE4E8"),false,true).alignment=TextAlignmentOptions.Center;
            Rule(rect,"Rim",24,1,320,1,new Color(accent.r,accent.g,accent.b,.38f));
            L(rect,"Category",def!=null&&def.Ability?"ABILITY  /  PASSIVE":"CORE STAT  /  PERMANENT",20,202,328,20,12,accent,false,true);
            var heading=L(rect,"OfferName",has?data.survivorUpgrades.Title(offer.id):"",24,256,320,44,34,Hex("F5E0AB"),true);heading.enableAutoSizing=true;heading.fontSizeMin=27;heading.fontSizeMax=34;
            string description=def==null?"":def.Ability?data.survivorUpgrades.Description(offer.id):$"{def.description}: +{offer.amount:0.##}{def.unit}";
            Hide(rect,"OfferDescription");
            var area=Node(rect,"Description area");var desc=L(area,"Text",description,0,0,320,1000,18,Hex("C1CED3"));float preferred=desc.GetPreferredValues(description,320,1000).y;float dh=94;Pixel(area,24,313,320,dh);Pixel(desc.rectTransform,0,0,320,Mathf.Max(dh,preferred));desc.overflowMode=TextOverflowModes.Overflow;
            if(!area.GetComponent<RectMask2D>())area.gameObject.AddComponent<RectMask2D>();var hit=area.GetComponent<Image>();if(!hit)hit=area.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            var descriptionScroll=area.GetComponent<ScrollRect>();if(!descriptionScroll)descriptionScroll=area.gameObject.AddComponent<ScrollRect>();descriptionScroll.viewport=area;descriptionScroll.content=desc.rectTransform;descriptionScroll.horizontal=false;descriptionScroll.vertical=preferred>dh;descriptionScroll.movementType=ScrollRect.MovementType.Clamped;descriptionScroll.scrollSensitivity=24;descriptionScroll.verticalNormalizedPosition=1;
            float y=417;
            var preview=Node(rect,"Preview well");Pixel(preview,24,y,320,70);Paint(preview,"Fill",Hex("0D1925"),Hex("0D1925"),new Color(accent.r,accent.g,accent.b,.18f),9);
            string value=has?UpgradePreview(offer):"";L(preview,"Caption",preferred>94?"SCROLL DESCRIPTION FOR DETAILS":string.IsNullOrEmpty(value)?"PASSIVE EFFECT":"AFTER THIS UPGRADE",14,10,292,17,11,Hex("8FA4B1"),false,true);
            var val=L(preview,"Value",string.IsNullOrEmpty(value)?"+1 ability level":value,14,29,292,30,20,accent,false,true);val.enableAutoSizing=true;val.fontSizeMin=13;val.fontSizeMax=20;val.textWrappingMode=TextWrappingModes.NoWrap;
            y=500;int lv=has&&player?player.UpgradeLevel(offer.id):0;
            L(rect,"OfferLevel",lv==0?"NEW · LEVEL 1":$"LEVEL {lv} → {Mathf.Min(lv+1,99)}",24,y,240,22,13,Hex("D9B674"),false,true);L(rect,"Cap","MAX 99",274,y,70,22,11,Hex("80949F")).alignment=TextAlignmentOptions.TopRight;
            y=535;Rule(rect,"Progress track",24,y,320,3,Hex("30414C"));Rule(rect,"Progress",24,y,Mathf.Max(10,320*(lv+1)/99f),3,accent);
            y=551;var action=Node(rect,"SelectAction");Pixel(action,24,y,320,45);var cta=Paint(action,"Fill",Hex("243846"),Hex("243846"),accent,9);
            var label=L(action,"Label","CHOOSE UPGRADE",0,0,320,45,16,Hex("E1D8C2"),false,true);label.alignment=TextAlignmentOptions.Center;
            if(offer.shiny&&sanctuarySkin){VectorArt(action,"Left star",sanctuarySkin.spark,10,13,18,18);VectorArt(action,"Right star",sanctuarySkin.spark,292,13,18,18);}
            else{Hide(action,"Left star");Hide(action,"Right star");}
            var fx=button.GetComponent<UpgradeChoiceFx>();if(!fx)fx=button.gameObject.AddComponent<UpgradeChoiceFx>();fx.Configure(frame,cta,label,accent,data?(abilityChoice?data.abilityQueueIcon:data.statQueueIcon):null);
        }
        void BuildSanctuaryInventory()
        {
            var root=Node(choicePanel,"Sanctuary build");Pixel(root,32,196,448,608);
            var crest=Node(root,"Crest");Pixel(crest,0,0,55,55);Paint(crest,"Fill",Hex("243440"),Hex("0D1923"),Hex("B6A57C"),27);
            L(crest,"Level",player?player.Level.ToString():"1",0,0,55,55,20,Hex("F5E0AB"),true).alignment=TextAlignmentOptions.Center;
            L(root,"Heading","YOUR BUILD",72,0,376,35,24,Hex("F5E0AB"),true);L(root,"Caption","Shape your next encounter.",72,34,376,22,13,Hex("9FB4BF"));
            var stats=Node(root,"Stats");Pixel(stats,0,79,448,208);Paint(stats,"Fill",Hex("0D1923"),Hex("0D1923"),Color.clear,16);L(stats,"Heading","CURRENT STATS",24,20,400,20,12,Hex("8FA4B1"),false,true);
            var combat=player?player.GetComponent<SurvivorAutoCombat>():null;string[] names={"Damage","Attack speed","Attack range","Projectile speed"};string[] values=combat?new[]{$"{combat.EffectiveDamage:0.##}",$"{combat.ShotsPerSecond:0.##} /s",$"{combat.AttackRange:0.##} m",$"{combat.ProjectileSpeed:0.##} m/s"}:new[]{"—","—","—","—"};
            for(int i=0;i<4;i++){L(stats,"Stat "+i,names[i],24,52+i*36,230,26,18,Hex("C1CED3"));L(stats,"Value "+i,values[i],240,52+i*36,184,26,20,Hex("F5E0AB"),false,true).alignment=TextAlignmentOptions.TopRight;}
            var owned=Node(root,"Owned");Pixel(owned,0,308,448,250);Paint(owned,"Fill",Hex("12202A"),Hex("12202A"),Color.clear,16);L(owned,"Heading","OWNED CARDS",20,18,300,26,18,Hex("F5E0AB"),true);
            var scrollRoot=Node(owned,"Scroll");Pixel(scrollRoot,20,60,408,172);var viewport=Node(scrollRoot,"Viewport");Pixel(viewport,0,0,390,172);if(!viewport.GetComponent<RectMask2D>())viewport.gameObject.AddComponent<RectMask2D>();
            var bg=viewport.GetComponent<Image>();if(!bg)bg=viewport.gameObject.AddComponent<Image>();bg.color=Color.clear;bg.raycastTarget=true;
            var content=Node(viewport,"Content");int count=0;foreach(Transform child in content)child.gameObject.SetActive(false);
            if(player&&data&&data.survivorUpgrades)foreach(var def in data.survivorUpgrades.definitions){if(def==null||def.Retired)continue;int level=player.UpgradeLevel(def.id);if(level==0)continue;
                var row=Node(content,def.id.ToString());Pixel(row,0,count*43,390,40);var art=Artwork(def.id);if(art!=null)VectorArt(row,"Icon",art.icon,5,6,25,25);else RenderIcon(row,"Icon",def.icon?def.icon:starIcon?starIcon:sanctuarySkin?sanctuarySkin.spark:null,5,6,25,25);
                L(row,"Name",data.survivorUpgrades.Title(def.id),45,7,235,30,17,Hex("C1CED3"));L(row,"Level",$"{level} / 99",285,8,100,30,14,Hex("9FB4BF")).alignment=TextAlignmentOptions.TopRight;count++;
            }
            Pixel(content,0,0,390,Mathf.Max(172,count*43));if(count==0)L(content,"Empty","Your journey begins here.",0,7,390,35,17,Hex("9FB4BF"));
            L(owned,"Count",count.ToString(),350,20,75,23,12,Hex("9FB4BF")).alignment=TextAlignmentOptions.TopRight;
            var track=Node(scrollRoot,"Scrollbar");Pixel(track,400,0,5,172);Paint(track,"Fill",Hex("283945"),Hex("283945"),Color.clear,2);
            var handle=Node(track,"Handle");Place(handle,Vector2.zero,Vector2.one);var handleImage=handle.GetComponent<Image>();if(!handleImage)handleImage=handle.gameObject.AddComponent<Image>();handleImage.color=Hex("B6A57C");
            var bar=track.GetComponent<Scrollbar>();if(!bar)bar=track.gameObject.AddComponent<Scrollbar>();bar.handleRect=handle;bar.targetGraphic=handleImage;bar.direction=Scrollbar.Direction.BottomToTop;bar.size=1;
            var scroll=scrollRoot.GetComponent<ScrollRect>();if(!scroll)scroll=scrollRoot.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=30;scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;scroll.verticalNormalizedPosition=1;
            L(root,"Cap note","Max-level cards will not appear again.",0,576,448,26,14,Hex("80949F"));
        }
        void BuildTransparencyButton()
        {
            var rect=Node(choicePanel,"Transparent option");Pixel(rect,250,832,250,42);
            var fill=Paint(rect,"Fill",Hex("243440"),Hex("243440"),new Color(.7f,.77f,.81f,.18f),9);fill.raycastTarget=true;
            var button=rect.GetComponent<Button>();if(!button)button=rect.gameObject.AddComponent<Button>();button.targetGraphic=fill;
            button.onClick.RemoveListener(ToggleTransparentBackdrop);button.onClick.AddListener(ToggleTransparentBackdrop);
            L(rect,"Label",transparentBackdrop?"TRANSPARENT: ON":"TRANSPARENT: OFF",0,0,250,42,14,Hex("E1D8C2"),false,true).alignment=TextAlignmentOptions.Center;
            ApplyTransparentBackdrop();
        }
        void ToggleTransparentBackdrop()
        {
            transparentBackdrop=!transparentBackdrop;
            PlayerPrefs.SetInt(TransparentBackdropKey,transparentBackdrop?1:0);
            RefreshEditablePanel();
        }
        void ApplyTransparentBackdrop()
        {
            var backdrop=choicePanel.Find("Sanctuary backdrop/Fill");
            if(backdrop){var fill=backdrop.GetComponent<UpgradeSurface>();fill.Paint(transparentBackdrop?Color.clear:Hex("162824"),transparentBackdrop?Color.clear:Hex("07111D"),Color.clear,0);}
            foreach(string name in new[]{"Ambient left","Ambient right"}){var glow=choicePanel.Find(name);if(glow)glow.gameObject.SetActive(!transparentBackdrop);}
            if(coordinator)coordinator.SetModalBackdropTransparent(choicePanel,transparentBackdrop);
        }
        void RenderIcon(RectTransform parent,string name,Sprite sprite,float x,float y,float width,float height)
        {
            var rect=Node(parent,name);Pixel(rect,x,y,width,height);
            var old=rect.GetComponent<Graphic>();if(old)old.enabled=false;
            bool vector=sprite&&sprite.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color);
            Hide(rect,vector?"Raster":"Vector");
            if(vector)VectorArt(rect,"Vector",sprite,0,0,width,height);
            else{var raster=Box(rect,"Raster",Vector2.zero,Vector2.one,Color.white);raster.gameObject.SetActive(true);var image=raster.GetComponent<Image>();image.sprite=sprite;image.type=Image.Type.Simple;image.preserveAspect=true;}
        }
        static RectTransform Node(Transform parent,string name)
        {var t=parent.Find(name) as RectTransform;if(!t){t=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();t.SetParent(parent,false);}t.gameObject.SetActive(true);t.SetAsLastSibling();return t;}
        UpgradeSurface Paint(RectTransform parent,string name,Color a,Color b,Color edge,float radius)
        {var node=Node(parent,name);Place(node,Vector2.zero,Vector2.one);if(!node.GetComponent<CanvasRenderer>())node.gameObject.AddComponent<CanvasRenderer>();var s=node.GetComponent<UpgradeSurface>();if(!s)s=node.gameObject.AddComponent<UpgradeSurface>();s.raycastTarget=false;s.Paint(a,b,edge,radius);return s;}
        void Rule(RectTransform p,string n,float x,float y,float w,float h,Color c){var node=Node(p,n);Pixel(node,x,y,w,h);Paint(node,"Fill",c,c,Color.clear,0);}
        void VectorArt(RectTransform p,string n,Sprite sprite,float x,float y,float w,float h)
        {if(!sprite)return;var rect=Node(p,n);Pixel(rect,x,y,w,h);var graphic=rect.GetComponent<SVGImage>();if(!graphic)graphic=rect.gameObject.AddComponent<SVGImage>();graphic.sprite=sprite;graphic.preserveAspect=true;graphic.raycastTarget=false;graphic.color=Color.white;if(sanctuarySkin)graphic.material=sanctuarySkin.vectorMaterial;var canvas=graphic.canvas;if(canvas)canvas.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord2;}
        TMP_Text L(RectTransform p,string n,string text,float x,float y,float w,float h,float size,Color c,bool heading=false,bool bold=false)
        {var label=Label(p,n,text,x,y,w,h,size,c);label.transform.SetAsLastSibling();label.font=heading?headingFont:bodyFont;label.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;return label;}
    }
}


