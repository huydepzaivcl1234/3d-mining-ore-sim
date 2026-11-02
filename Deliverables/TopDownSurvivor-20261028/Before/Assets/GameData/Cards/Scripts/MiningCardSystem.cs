using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    [DisallowMultipleComponent]
    public sealed class MiningCardSystem : MonoBehaviour
    {
        [SerializeField] private MiningCardData data;
        [SerializeField] private MiningPlayerStats player;
        [SerializeField] private MiningUiPanelCoordinator coordinator;
        [SerializeField] private RectTransform choicePanel;
        [SerializeField] private TMP_Text title;
        [SerializeField] private UnityEngine.UI.Button[] choices;
        private MiningCardData.Tier pending;
        private bool blueChoice;
        private readonly GameData.Player.Script.GunSpecialChoice[] blueOptions=new GameData.Player.Script.GunSpecialChoice[3];
        private GameData.Player.Script.GunSpecialUpgrades gunSpecials;
        private MiningCharacterHealth playerHealth;
        private float previousTimeScale;
        private CursorLockMode previousCursor;
        private bool previousCursorVisible, pauseOwned, controllerWasEnabled, previousAudioPause;
        private GameData.Player.Script.PlayerController1 controller;
        public bool IsChoosing => pending != null;
        public MiningPlayerStats Player => player;
        public bool CanCollect => pending==null && player!=null && playerHealth!=null && playerHealth.Health>0 &&
            (coordinator==null || !coordinator.BlocksGameplay);
        public void Configure(MiningCardData settings, MiningPlayerStats target, MiningUiPanelCoordinator ui,
            RectTransform panel, TMP_Text heading, UnityEngine.UI.Button[] buttons)
        { data=settings;player=target;coordinator=ui;choicePanel=panel;title=heading;choices=buttons; Bind(); }
        private void Awake() { Bind(); if(choicePanel!=null) choicePanel.gameObject.SetActive(false); }
        private void Start() { Bind(); }
        private void Update()
        {
            // If another modal interrupted the choice, offer it again once that modal closes.
            if(pending!=null && choicePanel!=null && !choicePanel.gameObject.activeInHierarchy &&
                playerHealth!=null && playerHealth.Health>0 && (coordinator==null || !coordinator.BlocksGameplay))
            {
                if(coordinator!=null) coordinator.OpenPanel(choicePanel); else choicePanel.gameObject.SetActive(true);
            }
            if (pending != null) { Time.timeScale = 0f; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }
        private void Bind()
        {
            if (!player) player=FindAnyObjectByType<MiningPlayerStats>();
            if(player!=null) { playerHealth=player.GetComponent<MiningCharacterHealth>(); controller=player.GetComponent<GameData.Player.Script.PlayerController1>(); }
            if(choices==null || choices.Length<3) return;
            if (choicePanel && data) choicePanel.sizeDelta = data.panelSize;
            for (int i=0;i<choices.Length;i++)
            {
                if (!choices[i]) continue;
                choices[i].gameObject.SetActive(i<3);
                if (i>=3) continue;
                var rect=(RectTransform)choices[i].transform;
                rect.anchorMin=new Vector2(.04f+i*.32f,.12f);rect.anchorMax=new Vector2(.31f+i*.32f,.76f);
                rect.offsetMin=rect.offsetMax=Vector2.zero;
                var colors=choices[i].colors;colors.normalColor=new Color(.34f,.09f,.12f,1f);colors.highlightedColor=new Color(.58f,.17f,.19f,1f);choices[i].colors=colors;
            }
            choices[0].onClick.RemoveListener(ChooseDamage); choices[0].onClick.AddListener(ChooseDamage);
            choices[1].onClick.RemoveListener(ChooseMagazine); choices[1].onClick.AddListener(ChooseMagazine);
            choices[2].onClick.RemoveListener(ChooseBulletSpeed); choices[2].onClick.AddListener(ChooseBulletSpeed);
        }
        public bool TryDrop(Vector3 position, bool boss)
        {
            if(data==null || Random.value*100f >= (boss?data.bossDropPercent:data.monsterDropPercent)) return false;
            return data.tiers!=null && data.tiers.Length>0 && Spawn(data.tiers[0],position)!=null;
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
        public bool Collect(MiningCardData.Tier tier)
        {
            if(!CanCollect || data==null || tier==null || data.tiers==null || data.tiers.Length==0 || tier!=data.tiers[0] || choicePanel==null || choices==null || choices.Length<3) return false;
            pending=tier;
            blueChoice=false;
            if (title) title.text="RED CARD · COMMON — "+CardText("CARD_CHOOSE_GUN","Choose one upgrade");
            string[] labels={ CardText("CARD_GUN_DAMAGE","Gun damage")+"\n+"+tier.gunDamagePercent.ToString("0.##")+"%",
                CardText("CARD_GUN_MAGAZINE","Magazine capacity")+"\n+"+tier.magazineBonus+" "+CardText("CARD_ROUND","round"),
                CardText("CARD_GUN_BULLET_SPEED","Bullet speed")+"\n+"+tier.bulletSpeedPercent.ToString("0.##")+"%" };
            for(int i=0;i<3;i++)
            {
                choices[i].gameObject.SetActive(true);
                choices[i].interactable=true;
                var colors=choices[i].colors;colors.normalColor=new Color(.34f,.09f,.12f,1f);colors.highlightedColor=new Color(.58f,.17f,.19f,1f);choices[i].colors=colors;
                var label=choices[i].GetComponentInChildren<TMP_Text>();
                label.text=labels[i]+"\n\n<size=70%>"+CardText("CARD_ALL_WEAPONS","All weapons")+"</size>";
                label.color=Color.white;label.enableAutoSizing=true;label.fontSizeMin=18f;label.fontSizeMax=data.fontSize;label.alignment=TextAlignmentOptions.Center;
            }
            previousTimeScale=Time.timeScale;previousCursor=Cursor.lockState;previousCursorVisible=Cursor.visible;pauseOwned=true;
            previousAudioPause=AudioListener.pause;AudioListener.pause=true;
            if(controller){controllerWasEnabled=controller.enabled;controller.enabled=false;}
            Time.timeScale=0f;
            if(coordinator!=null) coordinator.OpenPanel(choicePanel); else choicePanel.gameObject.SetActive(true);
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            return true;
        }
        public bool CollectBlueBox()=>CollectGunBox(false);
        public bool CollectOrangeBox()=>CollectGunBox(true);
        private bool CollectGunBox(bool orange)
        {
            gunSpecials=player?player.GetComponent<GameData.Player.Script.GunSpecialUpgrades>():null;
            if(!gunSpecials||!gunSpecials.Data||(orange?(!gunSpecials.Data.orange||gunSpecials.OrangeAllUnlocked):gunSpecials.AllUnlocked)||!data||data.tiers==null||data.tiers.Length==0||!Collect(data.tiers[0]))return false;
            blueChoice=true;
            if(title)title.text=orange?CardText("ORANGE_BOX_TITLE","ORANGE BOX — CHOOSE AN UPGRADE"):CardText("BLUE_BOX_TITLE","BLUE BOX — CHOOSE A GUN UPGRADE");
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
            pending=null;blueChoice=false;
            foreach(var button in choices)button.interactable=false;
            if(coordinator)coordinator.ClosePanel(choicePanel);else choicePanel.gameObject.SetActive(false);
            RestorePause();
        }
        private void ChooseDamage(){if(blueChoice)ChooseBlue(0);else Choose(MiningCardChoice.GunDamage);}
        private static string CardText(string key, string fallback)
        {
            string value=MiningLocalization.TextKey(key,fallback);
            return string.IsNullOrEmpty(value) || value==key ? fallback : value;
        }
        private void ChooseMagazine(){if(blueChoice)ChooseBlue(1);else Choose(MiningCardChoice.Magazine);}
        private void ChooseBulletSpeed(){if(blueChoice)ChooseBlue(2);else Choose(MiningCardChoice.BulletSpeed);}
        public bool Choose(MiningCardChoice choice)
        {
            if(pending==null || player==null || (choice!=MiningCardChoice.GunDamage && choice!=MiningCardChoice.Magazine && choice!=MiningCardChoice.BulletSpeed)) return false;
            if(!player.AddCardBonus(choice,pending.Bonus(choice))) return false;
            pending=null;
            foreach(var button in choices) button.interactable=false;
            if(coordinator!=null) coordinator.ClosePanel(choicePanel); else choicePanel.gameObject.SetActive(false);
            RestorePause();
            return true;
        }
        private void RestorePause()
        {
            if (!pauseOwned) return;
            pauseOwned=false;Time.timeScale=previousTimeScale;
            AudioListener.pause=previousAudioPause;
            Cursor.lockState=previousCursor;Cursor.visible=previousCursorVisible;
            if(controller)controller.enabled=controllerWasEnabled;
        }
        private void OnDisable()
        {
            CancelChoice();
        }
        public void CancelChoice()
        {
            pending=null;blueChoice=false;RestorePause();
            if(coordinator && choicePanel)coordinator.NotifyExternalPanelClosed(choicePanel);
            if(choicePanel)choicePanel.gameObject.SetActive(false);
        }
    }
}
