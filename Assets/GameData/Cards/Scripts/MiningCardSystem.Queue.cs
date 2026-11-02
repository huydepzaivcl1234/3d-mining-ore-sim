using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameData.Player.Script;
namespace MiningSimulator.Ores
{
    public sealed partial class MiningCardSystem
    {
        private sealed class QueuedUpgrade
        {
            public MiningCardData.Tier tier;
            public float rarityMultiplier;
            public List<SurvivorUpgradeData.Offer> rolled;
        }
        private readonly List<QueuedUpgrade> upgradeQueue=new();
        private QueuedUpgrade currentUpgrade;
        private bool choiceOpen;
        private RectTransform queueHud;
        private Button statQueueButton,abilityQueueButton;
        private int shownStats=-1,shownAbilities=-1;
        public int PendingStatUpgrades=>PendingCount(false);
        public int PendingAbilityUpgrades=>PendingCount(true);
        private int PendingCount(bool ability){int count=0;foreach(var q in upgradeQueue)if((q.tier==data.abilityCard)==ability)count++;return count;}
        public bool Collect(MiningCardData.Tier tier,float rarityMultiplier=1f)
        {
            if((!CanCollect&&!choiceOpen)||!data||tier==null||data.tiers==null||data.tiers.Length==0||tier!=data.tiers[0]&&tier!=data.abilityCard)return false;
            if(data.survivorUpgrades&&!data.survivorUpgrades.HasEligible(player,tier==data.abilityCard))return false;
            upgradeQueue.Add(new QueuedUpgrade{tier=tier,rarityMultiplier=rarityMultiplier});RefreshQueueHud();return true;
        }
        public bool OpenQueued(bool ability)
        {
            if(!choiceOpen&&!CanCollect)return false;
            foreach(var q in upgradeQueue)if((q.tier==data.abilityCard)==ability)return OpenQueuedRequest(q);
            return false;
        }
        private bool OpenQueuedRequest(QueuedUpgrade request)
        {
            currentUpgrade=request;
            if(request.rolled==null){
                if(!BeginChoice(request.tier,request.rarityMultiplier)){currentUpgrade=null;return false;}
                request.rolled=offers;
            }else{
                offers=request.rolled;pending=request.tier;abilityChoice=request.tier==data.abilityCard;blueChoice=false;
                title.text=abilityChoice?"CHOOSE AN ABILITY":"CHOOSE AN UPGRADE";
                for(int i=0;i<choices.Length;i++){bool shown=i<offers.Count&&i<3;choices[i].gameObject.SetActive(shown);choices[i].interactable=shown;}
                RefreshPresentation();
                if(!pauseOwned){previousTimeScale=Time.timeScale;previousCursor=Cursor.lockState;previousCursorVisible=Cursor.visible;previousAudioPause=AudioListener.pause;pauseOwned=true;if(controller){controllerWasEnabled=controller.enabled;controller.enabled=false;}}
                choiceOpen=true;if(!coordinator)Time.timeScale=0;AudioListener.pause=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
                if(coordinator)coordinator.OpenPanel(choicePanel);else choicePanel.gameObject.SetActive(true);
            }
            return true;
        }
        private void CompleteQueuedChoice()
        {
            if(currentUpgrade!=null)upgradeQueue.Remove(currentUpgrade);
            currentUpgrade=null;pending=null;offers=new();blueChoice=false;
            RefreshQueueHud();
            while(upgradeQueue.Count>0){var next=upgradeQueue[0];if(OpenQueuedRequest(next))return;upgradeQueue.Remove(next);}
            ClosePending();
        }
        public void ClosePending()
        {
            choiceOpen=false;
            if(coordinator&&choicePanel)coordinator.NotifyExternalPanelClosed(choicePanel);
            if(choicePanel)choicePanel.gameObject.SetActive(false);
            RestorePause();RefreshQueueHud();
        }
        private void EnsureCloseButton()
        {
            var rect=Box(choicePanel,"ClosePending",Vector2.zero,Vector2.one,Hex("30383D"));Pixel(rect,1384,24,44,44);
            var image=rect.GetComponent<Image>();image.raycastTarget=true;var button=rect.GetComponent<Button>();if(!button)button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            button.onClick.RemoveListener(ClosePending);button.onClick.AddListener(ClosePending);
            Label(rect,"X","X",0,0,44,44,23,Color.white).alignment=TMPro.TextAlignmentOptions.Center;
        }
        private void RefreshQueueHud()
        {
            if(!Application.isPlaying||!data||!title)return;
            if(!queueHud){
                var statsPanel=FindAnyObjectByType<PlayerStatsPanel>();var anchor=statsPanel?statsPanel.OpenButtonRect:null;if(!anchor)return;
                queueHud=new GameObject("Queued upgrade cards",typeof(RectTransform)).GetComponent<RectTransform>();queueHud.SetParent(anchor.parent,false);
                queueHud.anchorMin=anchor.anchorMin;queueHud.anchorMax=anchor.anchorMax;queueHud.pivot=anchor.pivot;queueHud.sizeDelta=new Vector2(150,84);queueHud.anchoredPosition=anchor.anchoredPosition+new Vector2(0,anchor.rect.height+18);
                statQueueButton=QueueButton("Stat card",0,Hex("B94257"),false);abilityQueueButton=QueueButton("Ability card",78,Hex("277FCA"),true);
            }
            int red=PendingStatUpgrades,blue=PendingAbilityUpgrades;if(red==shownStats&&blue==shownAbilities)return;
            shownStats=red;shownAbilities=blue;
            statQueueButton.gameObject.SetActive(red>0);abilityQueueButton.gameObject.SetActive(blue>0);
            Label((RectTransform)statQueueButton.transform.Find("CountBadge"),"Count",red.ToString(),0,0,26,26,17,Color.white).alignment=TMPro.TextAlignmentOptions.Center;
            Label((RectTransform)abilityQueueButton.transform.Find("CountBadge"),"Count",blue.ToString(),0,0,26,26,17,Color.white).alignment=TMPro.TextAlignmentOptions.Center;
        }
        private Button QueueButton(string name,float x,Color color,bool ability)
        {
            var rect=Box(queueHud,name,Vector2.zero,Vector2.one,Color.white);Pixel(rect,x,0,66,82);
            var image=rect.GetComponent<Image>();image.sprite=ability?data.abilityQueueIcon:data.statQueueIcon;image.type=Image.Type.Simple;image.preserveAspect=true;image.raycastTarget=true;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(()=>OpenQueued(ability));
            var badge=Box(rect,"CountBadge",Vector2.zero,Vector2.one,Hex("171B21"));Pixel(badge,40,0,26,26);
            return button;
        }
    }
}



