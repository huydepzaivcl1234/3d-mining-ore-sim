using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Construction stock owns receipts separately from inventory slots.</summary>
    public sealed class TowerShopPanel : MonoBehaviour
    {
        PlayerWallet wallet;
        MiningItemSystem inventory;
        Action close;
        TowerData selected;
        TowerData preview;
        public TowerData Previewed => preview;
        TMP_Text heading, balance, detailText, primaryText, hint;
        RectTransform detail;
        Button primary, buy;
        TMP_Text buyText;
        readonly List<TowerData> definitions = new();
        readonly List<TMP_Text> captions = new();
        readonly List<Image> frames = new();
        bool built;
        string feedback;
        public TowerData Selected => selected;
        public int Owned => selected != null && inventory != null ? inventory.GetTowerStockCount(selected.inventoryItem) : 0;
        internal static string L(string en, string vi) => MiningLocalization.Text(en, vi);

        public void Initialize(PlayerWallet value, Action onClose)
        {
            Unsubscribe();
            wallet = value != null ? value : FindFirstObjectByType<PlayerWallet>();
            inventory = FindFirstObjectByType<MiningItemSystem>(); close = onClose;
            inventory?.TransferLegacyTowersToConstruction();
            if (!built) Build();
            if (isActiveAndEnabled) Subscribe();
            Refresh();
        }
        void Subscribe()
        {
            MiningLocalization.LanguageChanged += Refresh;
            if (wallet != null) wallet.MoneyChanged += MoneyChanged;
            if (inventory != null) inventory.InventoryChanged += Refresh;
        }
        void Unsubscribe()
        {
            MiningLocalization.LanguageChanged -= Refresh;
            if (wallet != null) wallet.MoneyChanged -= MoneyChanged;
            if (inventory != null) inventory.InventoryChanged -= Refresh;
        }
        void OnEnable() { Subscribe(); Refresh(); }
        void OnDisable() => Unsubscribe();
        void MoneyChanged(float _) => Refresh();
        public void ShowCatalog() { feedback = null; preview = null; Refresh(); }
        public void Select(TowerData tower) { selected = tower; feedback = null; Refresh(); }
        public void Preview(TowerData tower) { preview=tower; Refresh(); }
        public void EndPreview(TowerData tower) { if(preview==tower) { preview=null; Refresh(); } }

        void Build(TowerCatalog catalog = null)
        {
            built = true;
            if(catalog==null) catalog = Resources.Load<TowerCatalog>("TowerCatalog");
            if (catalog != null && catalog.towers != null)
                foreach (var tower in catalog.towers) if (tower != null) definitions.Add(tower);
            var root = (RectTransform)transform;
            root.sizeDelta = new Vector2(520, 212 + Mathf.Min(3, definitions.Count) * 114);
            heading = Text("Heading",24,24,400,42,28,true);
            var x = MakeButton("Close",452,16,44,42,out var xText);
            xText.text = "×"; x.onClick.AddListener(() => close?.Invoke());
            balance = Text("Balance",24,78,472,32,21);
            var scroll = TowerFantasyUi.Rect("Tower List",transform,new Vector2(472,Mathf.Min(3,definitions.Count)*114),Vector2.zero);
            Top(scroll,24,118);
            var scroller = scroll.gameObject.AddComponent<ScrollRect>();
            scroller.horizontal = false; scroller.movementType = ScrollRect.MovementType.Clamped;
            scroller.scrollSensitivity = 35f;
            var viewport = TowerFantasyUi.Rect("Viewport",scroll,Vector2.zero,Vector2.zero);
            viewport.anchorMin=Vector2.zero; viewport.anchorMax=Vector2.one; viewport.offsetMin=viewport.offsetMax=Vector2.zero;
            viewport.gameObject.AddComponent<Image>().color = new Color(1,1,1,.01f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var content = TowerFantasyUi.Rect("Content",viewport,Vector2.zero,Vector2.zero);
            content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);
            var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=10;
            layout.childControlWidth=true;layout.childControlHeight=false;layout.childForceExpandHeight=false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroller.content=content;scroller.viewport=viewport;
            foreach(var tower in definitions)
            {
                var row=TowerFantasyUi.Frame(tower.towerId,content,new Vector2(472,104),Vector2.zero,TowerFantasyUi.Cell);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight=104;
                var click=row.gameObject.AddComponent<Button>();click.targetGraphic=row.GetComponent<Image>();click.targetGraphic.raycastTarget=true;
                click.onClick.AddListener(()=>Select(tower));frames.Add(row.GetComponent<Image>());
                row.gameObject.AddComponent<TowerCardHover>().Initialize(this,tower);
                var icon=TowerFantasyUi.Rect("Icon",row,new Vector2(68,68),new Vector2(-190,0)).gameObject.AddComponent<Image>();
                icon.sprite=tower.icon;icon.preserveAspect=true;icon.raycastTarget=false;
                captions.Add(TowerFantasyUi.Label(row,"Summary",new Vector2(40,0),new Vector2(345,88),18,TowerFantasyUi.Cream));
            }
            float detailTop=140+Mathf.Min(3,definitions.Count)*114;
            detail=TowerFantasyUi.Frame("Selected Tower",transform,new Vector2(472,235),Vector2.zero,TowerFantasyUi.Cell);Top(detail,24,detailTop);
            detailText=TowerFantasyUi.Label(detail,"Stats",Vector2.zero,new Vector2(436,209),21,TowerFantasyUi.Cream);
            buy=MakeButton("Buy tower",24,detailTop+253,230,54,out buyText);
            buy.onClick.AddListener(()=>BuySelected());
            primary=MakeButton("Place tower",266,detailTop+253,230,54,out primaryText);
            primary.onClick.AddListener(()=>BuyOrPlace());
            hint=Text("Feedback",24,detailTop+323,472,54,16);hint.color=TowerFantasyUi.Gold;
        }
        static void Top(RectTransform r,float x,float y)
        {r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);}
        TMP_Text Text(string name,float x,float y,float w,float h,float size,bool title=false)
        {var t=TowerFantasyUi.Label(transform,name,Vector2.zero,new Vector2(w,h),size,TowerFantasyUi.Cream,title);Top(t.rectTransform,x,y);return t;}
        Button MakeButton(string name,float x,float y,float w,float h,out TMP_Text text)
        {
            var rect=TowerFantasyUi.Rect(name,transform,new Vector2(w,h),Vector2.zero);Top(rect,x,y);
            var image=TowerFantasyUi.Box(rect,TowerFantasyUi.Gold);image.raycastTarget=true;
            var b=rect.gameObject.AddComponent<Button>();b.targetGraphic=image;
            text=TowerFantasyUi.Label(rect,"Label",Vector2.zero,new Vector2(w-16,h-4),22,TowerFantasyUi.Background,true,TextAlignmentOptions.Center);return b;
        }
        public bool BuyOrPlace()
        {
            if(TowerPlacement.IsPlacing||selected==null||inventory==null)return false;
            if(!TowerPlacement.CanBeginPlacement(inventory,selected.inventoryItem))
            {feedback=L("Placement unavailable right now.","Chưa thể đặt trụ lúc này.");Refresh();return false;}
            if(Owned<=0){feedback=L("Buy a tower first.","Mua trụ trước khi đặt.");Refresh();return false;}
            // Release the UI modal before the placement owner takes over the camera.
            close?.Invoke();
            return TowerPlacement.BeginPlacement(inventory,selected.inventoryItem,-1);
        }
        public bool BuySelected()
        {
            if(TowerPlacement.IsPlacing||selected==null||inventory==null)return false;
            bool bought=inventory.TryBuyConstructionTower(selected,wallet);
            feedback=bought?L("Added to construction stock.","Đã thêm vào kho Xây dựng."):L("Not enough gold.","Không đủ vàng.");
            Refresh();return bought;
        }
        public static bool PrepareInventory(TowerData tower,MiningItemSystem items,PlayerWallet money)
        {
            if(tower==null||tower.prefab==null||tower.inventoryItem==null||items==null)return false;
            return items.GetItemCount(tower.inventoryItem)>0||items.TryBuyTower(tower,money);
        }
        void Refresh()
        {
            if(!built)return;
            heading.text=L("CONSTRUCTION","XÂY DỰNG");
            balance.text=L("Gold  ","Vàng  ")+MiningMoneyFormatter.Format(wallet!=null?wallet.CurrentMoney:0);
            for(int i=0;i<definitions.Count;i++)
            {
                var d=definitions[i];int owned=inventory!=null?inventory.GetTowerStockCount(d.inventoryItem):0;
                captions[i].text=d.displayName+"\n"+L("Ranged defense","Phòng thủ tầm xa")+"\n"+
                    MiningMoneyFormatter.Format(d.price)+L(" gold · Owned "," vàng · Có ")+owned;
                frames[i].color=d==selected?TowerFantasyUi.Teal:TowerFantasyUi.Gold;
            }
            detail.gameObject.SetActive(preview!=null);primary.gameObject.SetActive(selected!=null);buy.gameObject.SetActive(selected!=null);
            float listBottom=140+Mathf.Min(3,definitions.Count)*114;
            // Stable bounds: revealing information must not move a card out from under the pointer.
            ((RectTransform)transform).sizeDelta=new Vector2(520,listBottom+397);
            Top(hint.rectTransform,24,listBottom+323);
            if(preview!=null)
                detailText.text=$"{preview.displayName} · LV {preview.level}\n"+
                    L("Damage","Sát thương")+$"  {preview.damage:0.##}\n"+L("Attack speed","Tốc đánh")+$"  {preview.attackSpeed:0.##}/s\n"+
                    L("Range","Tầm đánh")+$"  {preview.range:0.#} m\nHP  {preview.health:0.#} · Armor {preview.armor:0.#} · MR {preview.magicResistance:0.#}";
            if(selected==null){hint.text=L("Hover to view stats; select to buy or place.","Rê chuột xem chỉ số; chọn để mua hoặc đặt.");return;}
            primaryText.text=L("Place · ×","Đặt · ×")+Owned;
            buyText.text=L("Buy · ","Mua · ")+MiningMoneyFormatter.Format(selected.price);
            bool affordable=wallet!=null&&selected.price>=0&&!float.IsNaN(selected.price)&&!float.IsInfinity(selected.price)&&wallet.CurrentMoney>=selected.price;
            bool ready=selected.prefab!=null&&selected.inventoryItem!=null&&inventory!=null;
            primary.interactable=ready&&Owned>0;buy.interactable=ready&&affordable;
            hint.text=feedback??L("Buy repeatedly; your stock stays here, not in the bag.","Mua nhiều lần; số trụ được giữ ở đây, không vào túi đồ.");
        }
    }
}
