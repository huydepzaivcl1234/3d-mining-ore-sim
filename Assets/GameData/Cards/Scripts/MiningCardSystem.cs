using TMPro;
using UnityEngine;
using GameData.Player.Script;
using System.Collections.Generic;

namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent]
    public sealed partial class MiningCardSystem : MonoBehaviour
    {
        [SerializeField] private MiningCardData data;
        [SerializeField] private PlayerStats player;
        [SerializeField] private UiPanelCoordinator coordinator;
        [SerializeField] private RectTransform choicePanel;
        [SerializeField] private TMP_Text title;
        [SerializeField] private UnityEngine.UI.Button[] choices;
        [SerializeField] private Sprite[] upgradeIcons = new Sprite[3];
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private TMP_FontAsset headingFont, bodyFont;
        [SerializeField] private Sprite arrowIcon, starIcon, swordsIcon;
        private List<SurvivorUpgradeData.Offer> offers=new();
        public IReadOnlyList<SurvivorUpgradeData.Offer> CurrentOffers=>offers;
        public bool ChooseOffer(int index)
        {
            if(blueChoice){ChooseBlue(index);return !IsChoosing;}
            if(!IsChoosing||index<0||index>=offers.Count||!data.survivorUpgrades||!player.ApplySurvivorUpgrade(offers[index].id,offers[index].amount))return false;
            if(offers[index].id==SurvivorUpgradeId.DoubleBasics)GrantBasicChoices(player.AbilityData.rare.doubleChoice.choices);
            if(offers[index].id==SurvivorUpgradeId.TripleBasics)GrantBasicChoices(player.AbilityData.epic.tripleChoice.choices);
            CompleteQueuedChoice();return true;
        }
        private MiningCardData.Tier pending;
        private bool blueChoice, abilityChoice;
        public int QueuedBasicChoices=>PendingStatUpgrades;
        public void GrantBasicChoices(int count){for(int i=0;i<Mathf.Clamp(count,0,10);i++)Collect(data.tiers[0]);}
        public bool CanOfferAbilities=>player&&data&&data.survivorUpgrades&&data.survivorUpgrades.HasEligible(player,true);
        private readonly GameData.Player.Script.GunSpecialChoice[] blueOptions=new GameData.Player.Script.GunSpecialChoice[3];
        private GameData.Player.Script.GunSpecialUpgrades gunSpecials;
        private CharacterHealth playerHealth;
        private float previousTimeScale;
        private CursorLockMode previousCursor;
        private bool previousCursorVisible, pauseOwned, controllerWasEnabled, previousAudioPause;
        private GameData.Player.Script.PlayerController1 controller;
        public bool IsChoosing => choiceOpen;
        public PlayerStats Player => player;
        public bool CanCollect => player!=null && playerHealth!=null && playerHealth.Health>0 &&
            (coordinator==null || !coordinator.BlocksGameplay);
        public void Configure(MiningCardData settings, PlayerStats target, UiPanelCoordinator ui,
            RectTransform panel, TMP_Text heading, UnityEngine.UI.Button[] buttons)
        { data=settings;player=target;coordinator=ui;choicePanel=panel;title=heading;choices=buttons; Bind(); }
        private void Awake() { Bind(); if(choicePanel!=null) choicePanel.gameObject.SetActive(false); }
        private void Start() { Bind(); }
        private void Update()
        {
            RefreshQueueHud();
            if(choiceOpen && choicePanel && !choicePanel.gameObject.activeInHierarchy)ClosePending();
            if(choiceOpen){if(!coordinator)Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}

        }
        private void Bind()
        {
            if (!player) player=FindAnyObjectByType<PlayerStats>();
            if(player!=null) { playerHealth=player.GetComponent<CharacterHealth>(); controller=player.GetComponent<GameData.Player.Script.PlayerController1>(); }
            if(choices==null || choices.Length<3) return;
            RefreshPresentation();
            for (int i=0;i<choices.Length;i++)
            {
                if (!choices[i]) continue;
                choices[i].gameObject.SetActive(i<3);
                if (i>=3) continue;

            }
            choices[0].onClick.RemoveListener(ChooseDamage); choices[0].onClick.AddListener(ChooseDamage);
            choices[1].onClick.RemoveListener(ChooseMagazine); choices[1].onClick.AddListener(ChooseMagazine);
            choices[2].onClick.RemoveListener(ChooseBulletSpeed); choices[2].onClick.AddListener(ChooseBulletSpeed);
        }
        // Figma 2:3, 1460 x 870. Native controls retain their existing callbacks.
        public void RefreshPresentation()
        {
            if(!choicePanel||!title||choices==null||choices.Length<3)return;
            choicePanel.sizeDelta=new Vector2(1460,870);
            var bg=choicePanel.GetComponent<UnityEngine.UI.Image>();if(bg){bg.color=Hex("171B21");bg.sprite=panelSprite;bg.type=UnityEngine.UI.Image.Type.Sliced;}
            Border(choicePanel,Hex("9B7A42"));
            Hide(choicePanel,"OfferHeader");
            EnsureCloseButton();
            Pixel(title.rectTransform,32,28,1396,60);title.font=headingFont?headingFont:title.font;title.fontStyle=FontStyles.Normal;title.enableAutoSizing=false;title.fontSize=40;title.alignment=TextAlignmentOptions.TopLeft;title.color=Hex("F0D99F");title.text=abilityChoice?"CHOOSE AN ABILITY":"CHOOSE AN UPGRADE";
            Label(choicePanel,"ChoiceHint","Each upgrade adds the listed bonus. Max-level cards (99) are removed from offers.",32,96,1396,30,21,Hex("9FA8AA"));
            for(int i=0;i<3;i++)
            {
                var button=choices[i];if(!button)continue;var rect=(RectTransform)button.transform;
                Pixel(rect,424,150+i*196,1004,178);Hide(rect,"RarityStripe");
                foreach(var old in button.GetComponentsInChildren<TMP_Text>(true))if(old.transform.parent==rect&&!old.name.StartsWith("Offer"))old.gameObject.SetActive(false);
                var layout=button.GetComponent<UnityEngine.UI.LayoutElement>();if(!layout)layout=button.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();layout.ignoreLayout=true;
                int rarity=i<offers.Count?offers[i].rarity:0;
                Color accent=data&&data.survivorUpgrades?data.survivorUpgrades.RarityColor(rarity):Hex("A6B3A8");
                Color surface=rarity==4?Hex("302719"):rarity==3?Hex("242038"):rarity==2?Hex("192A38"):rarity==1?Hex("172D29"):Hex("1E2828");
                var image=button.GetComponent<UnityEngine.UI.Image>();image.sprite=panelSprite;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=surface;
                Border(rect,accent);var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.1f,1.1f,1.1f,1);colors.pressedColor=new Color(.9f,.9f,.9f,1);colors.selectedColor=Color.white;button.colors=colors;
                var plate=Box(rect,"IconPlate",Vector2.zero,Vector2.one,Hex("10191F"));Pixel(plate,22,22,112,134);
                var icon=Box(rect,"UpgradeIcon",Vector2.zero,Vector2.one,Color.white);Pixel(icon,49,60,58,58);icon.SetAsLastSibling();
                var iconImage=icon.GetComponent<UnityEngine.UI.Image>();Sprite sprite=null;
                string name="",description="",level="",preview="";
                if(data&&data.survivorUpgrades&&i<offers.Count)
                {
                    var offer=offers[i];var def=data.survivorUpgrades.Find(offer.id);name=(offer.shiny?"SPARKLING ":"")+data.survivorUpgrades.Title(offer.id);
                    description=def.Ability?data.survivorUpgrades.Description(offer.id):$"{def.description}: +{offer.amount:0.##}{def.unit}";
                    int current=player?player.UpgradeLevel(offer.id):0;level=current==0?"NEW · LEVEL 1":$"LEVEL {current} → {current+1}";
                    sprite=offer.id==SurvivorUpgradeId.ProjectileSpeed?arrowIcon:offer.id==SurvivorUpgradeId.MaxHealth?starIcon:offer.id==SurvivorUpgradeId.PhysicalDamage?swordsIcon:def.icon;
                    preview=UpgradePreview(offer);
                }
                if(!sprite&&upgradeIcons!=null&&i<upgradeIcons.Length)sprite=upgradeIcons[i];
                if(!sprite)sprite=starIcon;
                iconImage.sprite=sprite;iconImage.type=UnityEngine.UI.Image.Type.Simple;iconImage.preserveAspect=true;iconImage.color=Color.white;
                Label(rect,"OfferRarity",(i<offers.Count&&offers[i].shiny?"SPARKLING · ":"")+SurvivorUpgradeData.RarityName(rarity),158,22,560,20,14,accent);
                var nameText=Label(rect,"OfferName",name,158,47,560,43,29,Hex("F0E3C7"));nameText.font=headingFont?headingFont:title.font;nameText.enableAutoSizing=true;nameText.fontSizeMin=24;nameText.fontSizeMax=29;
                bool hasPreview=!string.IsNullOrWhiteSpace(preview);
                var descriptionText=Label(rect,"OfferDescription",description,158,94,560,hasPreview?44:(i<offers.Count&&data&&data.survivorUpgrades&&data.survivorUpgrades.Find(offers[i].id).Ability)?72:50,description.Length>65?18:20,Hex("CDD2CA"));descriptionText.enableAutoSizing=true;descriptionText.fontSizeMin=hasPreview?14:13;descriptionText.fontSizeMax=hasPreview?18:20;descriptionText.overflowMode=TextOverflowModes.Ellipsis;
                var previewText=Label(rect,"OfferPreview",preview,158,148,560,20,17,accent);previewText.textWrappingMode=TextWrappingModes.NoWrap;previewText.enableAutoSizing=true;previewText.fontSizeMin=13;previewText.fontSizeMax=17;previewText.overflowMode=TextOverflowModes.Ellipsis;
                var levelText=Label(rect,"OfferLevel",level,742,22,240,134,18,Hex("D6BC87"));levelText.alignment=TextAlignmentOptions.MidlineLeft;
            }
            var sparks=GetComponent<UpgradeSparkVfx>();if(!sparks&&data&&data.sparkleVfx)sparks=gameObject.AddComponent<UpgradeSparkVfx>();if(sparks)sparks.Refresh(choices,offers,data);
            var inventory=Box(choicePanel,"BuildInventory",Vector2.zero,Vector2.one,Hex("11171C"));Pixel(inventory,32,150,362,570);
            var outline=inventory.GetComponent<UnityEngine.UI.Outline>();if(outline)outline.enabled=false;Hide(inventory,"Header");
            var heading=Label(inventory,"Heading","YOUR CHARACTER",24,24,314,36,24,Hex("D6BC87"));heading.font=headingFont?headingFont:title.font;
            Label(inventory,"Caption","AUTOMATIC ATTACKS",24,82,314,20,14,Hex("909F9E"));
            var combat=player?player.GetComponent<SurvivorAutoCombat>():null;
            string totals=combat?$"Damage<pos=185>{combat.EffectiveDamage:0.##}\nAttack speed<pos=185>{combat.ShotsPerSecond:0.##} /s\nAttack range<pos=185>{combat.AttackRange:0.##} m\nProjectile speed<pos=185>{combat.ProjectileSpeed:0.##} m/s":"";
            Label(inventory,"Totals",totals,24,124,314,120,23,Hex("EDE1C6"));
            var divider=Box(inventory,"Divider",Vector2.zero,Vector2.one,Hex("3E4242"));Pixel(divider,24,265,314,1);
            var ownedHeading=Label(inventory,"OwnedHeading","OWNED CARDS",24,288,314,28,18,Hex("D6BC87"));ownedHeading.font=headingFont?headingFont:title.font;
            string owned="";int count=0;
            if(player&&data&&data.survivorUpgrades)foreach(var def in data.survivorUpgrades.definitions){if(def==null||def.Retired)continue;int lv=player.UpgradeLevel(def.id);if(lv<=0)continue;owned+=$"{data.survivorUpgrades.Title(def.id)}<pos=176>Level {lv} / 99\n";count++;}
            if(count==0)owned="No upgrades yet";
            RefreshOwnedScroll(inventory,owned);
            Label(inventory,"CapNote","Max-level cards will not appear again.",24,510,314,40,16,Hex("7F8D8D"));
            var skip=Box(choicePanel,"SkipOffer",Vector2.zero,Vector2.one,Hex("252C2E"));Pixel(skip,420,744,210,58);var border=skip.GetComponent<UnityEngine.UI.Outline>();if(border)border.enabled=false;
            var skipButton=skip.GetComponent<UnityEngine.UI.Button>();if(!skipButton)skipButton=skip.gameObject.AddComponent<UnityEngine.UI.Button>();skip.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;skipButton.targetGraphic=skip.GetComponent<UnityEngine.UI.Image>();skipButton.onClick.RemoveListener(CancelChoice);skipButton.onClick.RemoveListener(ClosePending);skipButton.onClick.AddListener(ClosePending);
            var skipText=Label(skip,"Label","LATER",0,0,210,58,20,Hex("D0C7B2"));skipText.font=headingFont?headingFont:title.font;skipText.alignment=TextAlignmentOptions.Center;
            Hide(choicePanel,"RefreshOffer");Label(choicePanel,"PauseNote","The game is paused while you choose an upgrade.",658,744,738,58,18,Hex("899594")).alignment=TextAlignmentOptions.MidlineLeft;
        }
        private void RefreshOwnedScroll(RectTransform inventory,string owned)
        {
            Hide(inventory,"Bonuses");
            var root=Box(inventory,"OwnedScroll",Vector2.zero,Vector2.one,Color.clear);Pixel(root,24,338,314,165);
            root.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var viewport=Box(root,"Viewport",Vector2.zero,Vector2.one,Color.clear);viewport.offsetMax=new Vector2(-16,0);
            if(!viewport.GetComponent<UnityEngine.UI.RectMask2D>())viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var text=Text(viewport,"OwnedEntries",owned,Vector2.zero,Vector2.one,20);text.color=Hex("ABB5B2");text.alignment=TextAlignmentOptions.TopLeft;text.textWrappingMode=TextWrappingModes.NoWrap;
            var content=text.rectTransform;content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(0,1);content.anchoredPosition=Vector2.zero;content.sizeDelta=new Vector2(0,text.preferredHeight);
            if(!content.GetComponent<UnityEngine.UI.ContentSizeFitter>()){var fit=content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();fit.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;}
            var track=Box(root,"Scrollbar",new Vector2(1,0),Vector2.one,Hex("252C2E"));track.pivot=new Vector2(1,.5f);track.offsetMin=new Vector2(-9,0);track.offsetMax=Vector2.zero;
            var handle=Box(track,"Handle",Vector2.zero,Vector2.one,Hex("9B7A42"));
            var bar=track.GetComponent<UnityEngine.UI.Scrollbar>();if(!bar)bar=track.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();bar.handleRect=handle;bar.targetGraphic=handle.GetComponent<UnityEngine.UI.Image>();bar.direction=UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            track.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;handle.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var scroll=root.GetComponent<UnityEngine.UI.ScrollRect>();if(!scroll)scroll=root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=30;scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);scroll.verticalNormalizedPosition=1;
        }
        private string UpgradePreview(SurvivorUpgradeData.Offer offer)
        {
            if(!player)return "";var c=player.GetComponent<SurvivorAutoCombat>();if(!c)return "";
            float before=0,after=0;string unit="";float bonus=player.UpgradeBonus(offer.id),ratio=(100+bonus+offer.amount)/(100+bonus);
            switch(offer.id){
                case SurvivorUpgradeId.CardMagnet:before=player.CardAttractionRadius;after=player.AbilityData.common.cardMagnet.radius.At(player.UpgradeLevel(offer.id)+1);unit=" m pickup range";break;
                case SurvivorUpgradeId.PhysicalDamage:before=c.EffectiveDamage;after=player.CalculateSurvivorDamage(c.BaseDamage,offer.amount);unit=" damage";break;
                case SurvivorUpgradeId.AttackSpeed:before=c.ShotsPerSecond;after=Mathf.Min(12.5f,c.BaseFireRate*Mathf.Max(.1f,player.CalculateAttackSpeed(offer.amount)));unit=" /s";break;
                case SurvivorUpgradeId.ProjectileSize:before=c.ProjectileRadius*2;after=before*ratio;unit=" m";break;
                case SurvivorUpgradeId.AttackRange:before=c.AttackRange;after=before*ratio;unit=" m";break;
                case SurvivorUpgradeId.ProjectileSpeed:before=c.ProjectileSpeed;after=before*(100+player.GunCardBulletSpeedPercent+bonus+offer.amount)/(100+player.GunCardBulletSpeedPercent+bonus);unit=" m/s";break;
                case SurvivorUpgradeId.MaxHealth:before=player.MaxHealth;after=player.CalculateMaxHealth(offer.amount);unit=" HP";break;
                case SurvivorUpgradeId.Armor:before=player.Armor;after=before*ratio;break;
                case SurvivorUpgradeId.MagicResistance:before=player.MagicResistance;after=before*ratio;break;
                case SurvivorUpgradeId.Regeneration:before=player.RegenAmount;after=before*ratio;unit=" HP/tick";break;
                case SurvivorUpgradeId.MoveSpeed:before=player.WalkSpeed;after=before+(player.CoreData?player.CoreData.walkSpeed:3)*offer.amount*.01f;return $"Walk {before:0.###} → {after:0.###} m/s  ·  Run {before+3:0.###} → {after+3:0.###} m/s";
                case SurvivorUpgradeId.LifeSteal:before=bonus;after=before+offer.amount;unit="% life steal";break;
                default:return "";
            }
            if(Mathf.Approximately(before,after))return offer.id==SurvivorUpgradeId.AttackSpeed?$"{before:0.###}{unit} (speed cap reached)":$"{before:0.###} → {after:0.###}{unit} (needs base stat)";
            return $"{before:0.###} → {after:0.###}{unit}  (+{after-before:0.###})";
        }
        private static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var color);return color;}
        private static void Hide(Transform parent,string name){var t=parent.Find(name);if(t)t.gameObject.SetActive(false);}
        private static void Pixel(RectTransform rect,float x,float y,float w,float h){rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);}
        private TMP_Text Label(RectTransform parent,string name,string value,float x,float y,float w,float h,float size,Color color)
        {var text=Text(parent,name,value,Vector2.zero,Vector2.one,size);text.gameObject.SetActive(true);Pixel(text.rectTransform,x,y,w,h);text.color=color;text.alignment=TextAlignmentOptions.TopLeft;return text;}

        private static void Place(RectTransform rect,Vector2 min,Vector2 max)
        {rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        private static void Border(RectTransform rect,Color color)
        {
            var outline=rect.GetComponent<UnityEngine.UI.Outline>();if(!outline)outline=rect.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor=color;outline.effectDistance=new Vector2(1,-1);
        }
        private RectTransform Box(RectTransform parent,string name,Vector2 min,Vector2 max,Color color)
        {
            var rect=parent.Find(name) as RectTransform;
            if(!rect){rect=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();rect.SetParent(parent,false);}
            Place(rect,min,max);var image=rect.GetComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;if(panelSprite){image.sprite=panelSprite;image.type=UnityEngine.UI.Image.Type.Sliced;}return rect;
        }
        private TMP_Text Text(RectTransform parent,string name,string value,Vector2 min,Vector2 max,float size)
        {
            var rect=parent.Find(name) as RectTransform;
            if(!rect){rect=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<RectTransform>();rect.SetParent(parent,false);}
            Place(rect,min,max);var text=rect.GetComponent<TMP_Text>();text.font=bodyFont?bodyFont:title.font;text.fontStyle=FontStyles.Normal;text.enableAutoSizing=false;text.textWrappingMode=TextWrappingModes.Normal;text.text=value;text.fontSize=size;text.color=new Color(.92f,.93f,.87f);text.raycastTarget=false;
            text.alignment=TextAlignmentOptions.MidlineLeft;return text;
        }
        public Vector2 DropChances(bool boss)
        {
            if(!data)return Vector2.zero;
            float baseline=boss?data.bossDropPercent:data.monsterDropPercent;
            float blue=Mathf.Clamp01(data.abilityCardPercent*.01f);
            int fortune=player?player.UpgradeLevel(SurvivorUpgradeId.CardFortune):0;
            float bonus=fortune>0?player.AbilityData.epic.cardFortune.dropBonus.At(fortune):0;
            float basicBonus=player&&player.UpgradeLevel(SurvivorUpgradeId.TripleBasics)>0?player.AbilityData.epic.tripleChoice.basicDropBonus:0;
            var chance=new Vector2(Mathf.Max(0,baseline*(1-blue)+bonus+basicBonus),Mathf.Max(0,baseline*blue+bonus));
            float total=chance.x+chance.y;return total>100?chance*(100/total):chance;
        }
        public bool TryDrop(Vector3 position, bool boss)
        {
            if(!data||data.tiers==null||data.tiers.Length==0)return false;
            bool basic=!data.survivorUpgrades||data.survivorUpgrades.HasEligible(player,false);
            bool ability=CanOfferAbilities&&data.abilityCard!=null&&data.abilityCard.prefab;
            var chance=DropChances(boss);if(!ability){chance.x+=chance.y;chance.y=0;}if(!basic){chance.y+=chance.x;chance.x=0;}float sample=Random.value*100;
            if(ability&&sample<chance.y)return Spawn(data.abilityCard,position)!=null;
            if(basic&&sample>=chance.y&&sample<chance.y+chance.x)return Spawn(data.tiers[0],position)!=null;
            return false;
        }
        public GameObject Spawn(MiningCardData.Tier tier, Vector3 position)
        {
            if(data==null || tier==null || tier.prefab==null) return null;
            var root=new GameObject("World Card - "+tier.name);
            root.transform.position=position+Vector3.up*data.spawnHeight;
            var visual=Instantiate(tier.prefab,root.transform);
            visual.transform.localScale*=data.cardScale;
            // Keep authored visual intact, but do not let nested prefab colliders collect/block the player.
            foreach(var c in visual.GetComponentsInChildren<Collider>(true)) c.enabled=false;
            foreach(var rb in visual.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic=true;
            var renderers=visual.GetComponentsInChildren<Renderer>();
            Bounds bounds=renderers.Length>0?renderers[0].bounds:new Bounds(root.transform.position,Vector3.one*.05f);
            foreach(var r in renderers) bounds.Encapsulate(r.bounds);
            // FBX card variants have different authored X offsets. Centre each spawned visual only.
            visual.transform.position-=bounds.center-root.transform.position;
            var box=root.AddComponent<BoxCollider>(); box.center=Vector3.zero;
            box.size=Vector3.Max(bounds.size,Vector3.one*.05f);
            var body=root.AddComponent<Rigidbody>();
            body.AddForce(Vector3.up*data.upwardImpulse+Vector3.ProjectOnPlane(Random.onUnitSphere,Vector3.up)*data.sidewaysImpulse,ForceMode.Impulse);
            root.AddComponent<MiningWorldCard>().Configure(this,data,tier);
            return root;
        }
        private bool BeginChoice(MiningCardData.Tier tier,float rarityMultiplier=1f)
        {
            if(playerHealth==null||playerHealth.Health<=0 || data==null || tier==null || data.tiers==null || data.tiers.Length==0 || (tier!=data.tiers[0]&&tier!=data.abilityCard) || choicePanel==null || choices==null || choices.Length<3) return false;
            bool abilities=tier==data.abilityCard;
            if(data.survivorUpgrades){offers=data.survivorUpgrades.Roll(player,3,abilities,rarityMultiplier);if(offers.Count==0)return false;}
            abilityChoice=abilities;
            pending=tier;
            blueChoice=false;
            if (title) title.text=abilityChoice?"CHOOSE AN ABILITY":"CHOOSE AN UPGRADE";
            Bind();

            for(int i=0;i<choices.Length;i++){bool shown=i<offers.Count;choices[i].gameObject.SetActive(shown);choices[i].interactable=shown;}
            RefreshPresentation();
            if(!pauseOwned){previousTimeScale=Time.timeScale;previousCursor=Cursor.lockState;previousCursorVisible=Cursor.visible;pauseOwned=true;
            previousAudioPause=AudioListener.pause;
            if(controller){controllerWasEnabled=controller.enabled;controller.enabled=false;}}
            choiceOpen=true;
            AudioListener.pause=true;
            if(!coordinator)Time.timeScale=0f;
            if(coordinator!=null) coordinator.OpenPanel(choicePanel); else choicePanel.gameObject.SetActive(true);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            return true;
        }
        public bool CollectBlueBox(float rarityMultiplier=1f)=>data&&data.survivorUpgrades?Collect(data.abilityCard,rarityMultiplier):CollectGunBox(false);
        public bool CollectOrangeBox()=>CollectGunBox(true);
        private bool CollectGunBox(bool orange)
        {
            gunSpecials=player?player.GetComponent<GameData.Player.Script.GunSpecialUpgrades>():null;
            if(!gunSpecials||!gunSpecials.Data||(orange?(!gunSpecials.Data.orange||gunSpecials.OrangeAllUnlocked):gunSpecials.AllUnlocked)||!data||data.tiers==null||data.tiers.Length==0||!BeginChoice(data.tiers[0]))return false;
            blueChoice=true;
            if(title)title.text=orange?CardText("ORANGE_BOX_TITLE","ORANGE BOX — CHOOSE AN UPGRADE"):CardText(player&&player.GetComponent<GameData.Player.Script.SurvivorAutoCombat>()?"SURVIVOR_BLUE_TITLE":"BLUE_BOX_TITLE","BLUE BOX — CHOOSE AN ATTACK UPGRADE");
            var settings=gunSpecials.Data;
            var available=new System.Collections.Generic.List<GameData.Player.Script.GunSpecialChoice>();
            foreach(GameData.Player.Script.GunSpecialChoice option in System.Enum.GetValues(typeof(GameData.Player.Script.GunSpecialChoice)))
                if((option>=GameData.Player.Script.GunSpecialChoice.CantDie)==orange&&!gunSpecials.IsUnlocked(option))available.Add(option);
            for(int i=0;i<3;i++)
            {
                bool shown=available.Count>0;choices[i].gameObject.SetActive(shown);choices[i].interactable=shown;
                if(!shown)continue;
                int pick=Random.Range(0,available.Count);blueOptions[i]=available[pick];available.RemoveAt(pick);
                choices[i].GetComponentInChildren<TMP_Text>().text=BlueLabel(blueOptions[i],settings);
                var colors=choices[i].colors;colors.normalColor=orange?new Color(.65f,.23f,.03f):new Color(.04f,.2f,.5f);colors.highlightedColor=orange?new Color(.95f,.45f,.08f):new Color(.08f,.42f,.8f);choices[i].colors=colors;
            }
            return true;
        }
        private static string BlueLabel(GameData.Player.Script.GunSpecialChoice choice,GameData.Player.Script.BlueGunUpgradeData s)
        {
            switch(choice)
            {
                case GameData.Player.Script.GunSpecialChoice.DoubleHit:
                    return string.Format(CardText("BLUE_DOUBLE_HIT","One shot, two hits\nDeal {0}% damage to the nearest enemy"),(s.doubleHit.splashDamageFraction*100).ToString("0.##"));
                case GameData.Player.Script.GunSpecialChoice.SharpRounds:
                    return string.Format(CardText("BLUE_SHARP_ROUNDS","Sharp rounds\n{0}% damage every {1}s for {2}s"),(s.sharpRounds.bleedDamageFraction*100).ToString("0.##"),s.sharpRounds.bleedTickInterval.ToString("0.##"),s.sharpRounds.bleedDuration.ToString("0.##"));
                case GameData.Player.Script.GunSpecialChoice.Nail:
                    return string.Format(CardText("BLUE_NAIL","Nail\n+{0}% enemy max HP as magic damage\nUnlimited stacks"),s.nail.nailMaxHealthPercent.ToString("0.#######"));
                case GameData.Player.Script.GunSpecialChoice.FasterMatter:
                    return string.Format(CardText("BLUE_FASTER_MATTER","Faster Matter\n+{0}% fire rate for all guns"),s.fasterMatter.fireRateBonusPercent.ToString("0.##"));
                case GameData.Player.Script.GunSpecialChoice.BulletFly:
                    string damage=s.bulletFly.bulletFlyDamage==null?"":string.Join(", ",System.Array.ConvertAll(s.bulletFly.bulletFlyDamage,d=>d.ToString("0.##")));
                    return string.Format(CardText("BLUE_BULLET_FLY","Bullet Fly\nBase true damage: {0}; + player level × {4} per bullet\nAfter surviving {1} hits: -{2}% per later volley, up to -{3}%"),damage,s.bulletFly.survivingHitsBeforeReduction,(s.bulletFly.reductionPerVolley*100).ToString("0.##"),(s.bulletFly.maximumReduction*100).ToString("0.##"),s.bulletFly.damagePerPlayerLevel.ToString("0.##"));
                case GameData.Player.Script.GunSpecialChoice.Hunter:
                    return string.Format(CardText("BLUE_HUNTER","Hunter\nEvery {0} kills grants +{1} permanent max HP\nUp to +{2} HP"),s.hunter.hunterKillsPerReward,s.hunter.hunterHealthPerReward,s.hunter.hunterMaxHealth);
                case GameData.Player.Script.GunSpecialChoice.BloodTears:
                    return string.Format(CardText("BLUE_BLOOD_TEARS","Blood Tears\nHeal for {0}% of damage you deal to enemies"),(s.bloodTears.bloodTearsHealFraction*100).ToString("0.##"));
                case GameData.Player.Script.GunSpecialChoice.GiantBomb:
                    return string.Format(CardText("BLUE_GIANT_BOMB","Giant Bomb\nEvery {0} hits on an enemy releases a shockwave\nDamage: {1} + current gun damage"),s.giantBomb.giantBombHits,s.giantBomb.giantBombBaseDamage.ToString("0.##"));
                case GameData.Player.Script.GunSpecialChoice.CantDie:
                    return string.Format(CardText("ORANGE_CANT_DIE","Cant Die\nRecover {0}% max HP over {1}s when fatally hit; enemies ignore you\nCooldown: {2}s"),(s.orange.cantDie.recoveryHealthFraction*100).ToString("0.##"),s.orange.cantDie.recoverySeconds,s.orange.cantDie.cooldownSeconds);
                case GameData.Player.Script.GunSpecialChoice.Cannon:
                    return string.Format(CardText("ORANGE_CANNON","Divine Cannon\n{0}% on-hit chance to fire a large round for {1}% extra damage"),s.orange.cannon.chancePercent,(s.orange.cannon.damageFraction*100).ToString("0.##"));
                case GameData.Player.Script.GunSpecialChoice.EtherealWeapon:
                    return string.Format(CardText("ORANGE_ETHEREAL","Ethereal Weapon\nDirect hits open a {1}s effect-to-on-hit window\n{0}s cooldown per enemy; wait {2}s after expiry, then shoot again"),s.orange.etherealWeapon.perTargetCooldown,s.orange.etherealWeapon.activeSeconds,s.orange.etherealWeapon.reactivationCooldown);
                default:return "";
            }
        }
        private void ChooseBlue(int index)
        {
            if(!blueChoice||index<0||index>=blueOptions.Length||!choices[index].gameObject.activeSelf||!gunSpecials||!gunSpecials.Unlock(blueOptions[index]))return;
            CompleteQueuedChoice();
        }
        private void ChooseDamage(){if(data&&data.survivorUpgrades&&!blueChoice)ChooseOffer(0);else if(blueChoice)ChooseBlue(0);else Choose(MiningCardChoice.GunDamage);}
        private static string CardText(string key, string fallback)
        {
            string value=MiningLocalization.TextKey(key,fallback);
            return string.IsNullOrEmpty(value) || value==key ? fallback : value;
        }
        private void ChooseMagazine(){if(data&&data.survivorUpgrades&&!blueChoice)ChooseOffer(1);else if(blueChoice)ChooseBlue(1);else Choose(MiningCardChoice.Magazine);}
        private void ChooseBulletSpeed(){if(data&&data.survivorUpgrades&&!blueChoice)ChooseOffer(2);else if(blueChoice)ChooseBlue(2);else Choose(MiningCardChoice.BulletSpeed);}
        public bool Choose(MiningCardChoice choice)
        {
            if(pending==null || player==null || (choice!=MiningCardChoice.GunDamage && choice!=MiningCardChoice.Magazine && choice!=MiningCardChoice.BulletSpeed)) return false;
            if(!player.AddCardBonus(choice,pending.Bonus(choice))) return false;
            CompleteQueuedChoice();
            return true;
        }
        private void RestorePause()
        {
            if (!pauseOwned) return;
            pauseOwned=false;if(!coordinator)Time.timeScale=previousTimeScale;
            AudioListener.pause=previousAudioPause;
            Cursor.lockState=previousCursor;Cursor.visible=previousCursorVisible;
            if(controller)controller.enabled=controllerWasEnabled;
        }
        private void OnDisable()
        {
            CancelChoice();
        }
        public void CancelChoice(){ClosePending();}
    }
}

