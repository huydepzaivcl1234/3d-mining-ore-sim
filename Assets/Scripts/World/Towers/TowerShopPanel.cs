using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace MiningSimulator.Ores
{
    public sealed class TowerShopPanel : MonoBehaviour
    {
        private PlayerWallet wallet;
        private MiningItemSystem inventory;
        private TMP_Text message;
        public void Initialize(PlayerWallet balance, System.Action close)
        {
            wallet=balance!=null?balance:FindFirstObjectByType<PlayerWallet>();
            inventory=FindFirstObjectByType<MiningItemSystem>();
            foreach(Transform child in transform)child.gameObject.SetActive(false);
            var catalog=Resources.Load<TowerCatalog>("TowerCatalog");
            var bg=Rect("Tower Shop",transform,Vector2.zero,new Vector2(1540,860));
            bg.gameObject.AddComponent<Image>().color=new Color32(255,246,223,255);
            Label(bg,"TOWER SHOP",new Vector2(-520,355),new Vector2(480,90),58);
            var closeButton=Button(bg,"CLOSE",new Vector2(605,355),new Vector2(210,80));
            closeButton.onClick.AddListener(()=>close?.Invoke());
            message=Label(bg,"Buy → Inventory → Place | Sell returns 35%",new Vector2(0,-360),new Vector2(1350,70),32);
            if(catalog==null)return;
            for(int i=0;i<catalog.towers.Length;i++)
            {
                var tower=catalog.towers[i];if(tower==null)continue;
                var row=Rect(tower.displayName,bg,new Vector2(0,195-i*210),new Vector2(1400,190));
                row.gameObject.AddComponent<Image>().color=Color.white;
                var icon=Rect("Icon",row,new Vector2(-595,0),new Vector2(160,160)).gameObject.AddComponent<Image>();
                icon.sprite=tower.icon;icon.preserveAspect=true;icon.color=Color.white;
                Label(row,tower.displayName+"\n"+tower.Summary,new Vector2(-90,0),new Vector2(800,155),32);
                var buy=Button(row,$"BUY\n{MiningMoneyFormatter.Format(tower.price)}",new Vector2(545,0),new Vector2(230,125));
                buy.onClick.AddListener(()=>message.text=inventory!=null&&inventory.TryBuyTower(tower,wallet)?"Purchased! Open inventory to place.":"Not enough money or inventory is full.");
            }
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var rect=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;
            rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
        }
        private static TMP_Text Label(Transform parent,string value,Vector2 position,Vector2 size,float fontSize)
        {
            var label=Rect("Label",parent,position,size).gameObject.AddComponent<TextMeshProUGUI>();label.text=value;label.fontSize=fontSize;
            label.color=new Color32(45,37,30,255);label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
        }
        private static Button Button(Transform parent,string text,Vector2 position,Vector2 size)
        {
            var rect=Rect(text,parent,position,size);var image=rect.gameObject.AddComponent<Image>();image.color=new Color32(255,207,75,255);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;Label(rect,text,Vector2.zero,size,34);return button;
        }
    }
}
