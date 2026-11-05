using System.Collections.Generic;
using UnityEngine;
using GameData.Player.Script;

namespace MiningSimulator.Ores
{
    public sealed partial class MiningCardSystem
    {
        [Header("Editable upgrade UI")]
        [SerializeField,Tooltip("Assign the view on your replacement panel. Its RectTransform, fonts and layout stay authored in the scene/prefab.")]
        UpgradePanelView panelView;

        void RefreshEditablePanel()
        {
            if(panelView)choicePanel=(RectTransform)panelView.transform;
            if(!choicePanel)return;
            if(!panelView||panelView.transform!=choicePanel)panelView=choicePanel.GetComponent<UpgradePanelView>();
            if(!panelView)return;
            panelView.ResolveSlots();
            if(panelView.slots==null||panelView.slots.Length!=3)return;
            var view=panelView;
            title=view.title;
            choices=new UnityEngine.UI.Button[view.slots.Length];
            for(int i=0;i<choices.Length;i++)choices[i]=view.slots[i]?.button;
            if(!backdropPreferenceLoaded){transparentBackdrop=PlayerPrefs.GetInt(TransparentBackdropKey,0)!=0;backdropPreferenceLoaded=true;}
            UpgradePanelView.Text(view.title,abilityChoice?"CHOOSE AN ABILITY":"CHOOSE AN UPGRADE");
            UpgradePanelView.Text(view.queueCount,$"{(abilityChoice?"ABILITY":"STAT")} CARDS  ·  {(abilityChoice?PendingAbilityUpgrades:PendingStatUpgrades)} WAITING");
            UpgradePanelView.Text(view.playerLevel,player?player.Level.ToString():"1");
            Wire(view.close,ClosePending);Wire(view.later,ClosePending);Wire(view.transparent,ToggleTransparentBackdrop);
            UpgradePanelView.Text(view.transparentLabel,transparentBackdrop?"TRANSPARENT: ON":"TRANSPARENT: OFF");
            if(view.sceneryCover!=null)foreach(var cover in view.sceneryCover)if(cover)cover.SetActive(!transparentBackdrop);
            if(coordinator)coordinator.SetModalBackdropTransparent(choicePanel,transparentBackdrop);
            var combat=player?player.GetComponent<SurvivorAutoCombat>():null;
            if(combat){UpgradePanelView.Text(view.damage,$"{combat.EffectiveDamage:0.##}");UpgradePanelView.Text(view.attackSpeed,$"{combat.ShotsPerSecond:0.##} /s");UpgradePanelView.Text(view.attackRange,$"{combat.AttackRange:0.##} m");UpgradePanelView.Text(view.projectileSpeed,$"{combat.ProjectileSpeed:0.##} m/s");}
            var names=new List<string>();var levels=new List<int>();var icons=new List<Sprite>();
            if(player&&data&&data.survivorUpgrades)foreach(var def in data.survivorUpgrades.definitions){if(def==null||def.Retired)continue;int lv=player.UpgradeLevel(def.id);if(lv==0)continue;names.Add(data.survivorUpgrades.Title(def.id));levels.Add(lv);icons.Add(PanelIcon(def.id,def.icon));}
            view.SetOwned(names.ToArray(),levels.ToArray(),icons.ToArray());
            for(int i=0;i<view.slots.Length;i++){
                var slot=view.slots[i];if(slot==null||!slot.button)continue;
                bool has=i<offers.Count&&i<3;slot.button.gameObject.SetActive(has);slot.button.interactable=has;if(!has)continue;
                var offer=offers[i];var def=data.survivorUpgrades.Find(offer.id);int lv=player?player.UpgradeLevel(offer.id):0;
                UpgradePanelView.Text(slot.name,data.survivorUpgrades.Title(offer.id));
                UpgradePanelView.Text(slot.description,def==null?"":def.Ability?data.survivorUpgrades.Description(offer.id):$"{def.description}: +{offer.amount:0.##}{def.unit}");
                UpgradePanelView.Text(slot.rarity,(offer.shiny?"SPARKLING · ":"")+SurvivorUpgradeData.RarityName(offer.rarity).ToUpperInvariant());
                UpgradePanelView.Text(slot.category,def!=null&&def.Ability?"ABILITY  /  PASSIVE":"CORE STAT  /  PERMANENT");
                UpgradePanelView.Text(slot.number,(i+1).ToString());UpgradePanelView.Text(slot.level,lv==0?"NEW · LEVEL 1":$"LEVEL {lv} → {Mathf.Min(lv+1,99)}");
                var value=UpgradePreview(offer);UpgradePanelView.Text(slot.previewCaption,string.IsNullOrEmpty(value)?"PASSIVE EFFECT":"AFTER THIS UPGRADE");UpgradePanelView.Text(slot.previewValue,string.IsNullOrEmpty(value)?"+1 ability level":value);
                UpgradePanelView.Text(slot.actionLabel,"CHOOSE UPGRADE");if(slot.progress)slot.progress.fillAmount=(lv+1)/99f;
                view.SetIcon(slot,PanelIcon(offer.id,def?.icon));
                var fx=slot.button.GetComponent<UpgradeChoiceFx>();
                if(fx&&fx.enabled)fx.Configure(slot.frame,slot.action,slot.actionLabel,RarityAccent(offer),data?(abilityChoice?data.abilityQueueIcon:data.statQueueIcon):null);
                slot.button.onClick.RemoveListener(ChooseDamage);slot.button.onClick.RemoveListener(ChooseMagazine);slot.button.onClick.RemoveListener(ChooseBulletSpeed);
            }
            BindOfferClicks();
            var sparks=GetComponent<UpgradeSparkVfx>();if(sparks&&sparks.enabled)sparks.Refresh(choices,offers,data);
        }
        Sprite PanelIcon(SurvivorUpgradeId id,Sprite dataIcon)
        {
            var art=panelView&&panelView.iconSource==UpgradePanelView.IconSource.SanctuaryArtwork?Artwork(id):null;
            return art!=null&&art.icon?art.icon:dataIcon?dataIcon:starIcon;
        }
        static void Wire(UnityEngine.UI.Button button,UnityEngine.Events.UnityAction callback){if(!button)return;button.onClick.RemoveListener(callback);button.onClick.AddListener(callback);}
        void BindOfferClicks()
        {
            if(choices==null)return;
            if(choices.Length>0)Wire(choices[0],ChooseDamage);
            if(choices.Length>1)Wire(choices[1],ChooseMagazine);
            if(choices.Length>2)Wire(choices[2],ChooseBulletSpeed);
        }
        Color RarityAccent(SurvivorUpgradeData.Offer offer)=>Hex(offer.id==SurvivorUpgradeId.Burn?"F1B66B":offer.id==SurvivorUpgradeId.ChargedCube?"CCA2F6":offer.id==SurvivorUpgradeId.MaxHealth?"CAA5F1":offer.rarity==0?"B7C5CF":offer.rarity==1?"8BD4C5":offer.rarity==2?"91BFF1":offer.rarity==3?"CCA2F6":"F1C56B");
    }
}
