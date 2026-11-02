using TMPro;
using UnityEngine;
namespace MiningSimulator.Ores
{
    public sealed class TreasureChestHud : MonoBehaviour
    {
        private TreasureChest chest;
        private RectTransform panel;
        private RectTransform repairPanel;
        private UnityEngine.UI.Button repairButton;
        private TextMeshProUGUI repairLabel;
        private TextMeshProUGUI repairCostLabel;
        private UnityEngine.UI.Image repairCoin;
        private PlayerWallet repairWallet;
        
        private UnityEngine.UI.Image hpHighlight, xpHighlight;
private UnityEngine.UI.Image hp, xp;
        private TextMeshProUGUI title, hpText, xpText;
        private TextMeshProUGUI moneyText;
        private RectTransform moneyPopup;
        private UnityEngine.UI.Image moneyIcon;
        private TextMeshProUGUI levelText, incomeText, armorText, resistanceText;
        private RectTransform footer;
        private UnityEngine.UI.Image statusFrame;
        private TextMeshProUGUI progressCaption, incomeCaption, armorCaption, resistanceCaption;
        private static Color Ink => new Color32(53, 41, 35, 255);
        private void Top(RectTransform r, float x, float y)
        {
            r.anchorMin = r.anchorMax = new Vector2(.5f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
        }
        
private Transform player;
        private float moneyAge=10f;
        
private Camera viewer;
        private CanvasGroup canvasGroup;
        private bool nearby;
        
private float visibility;
public void Bind(TreasureChest owner)
        {
            if(chest!=null){chest.Changed-=Refresh;chest.Paid-=ShowMoney;}
            MiningLocalization.LanguageChanged-=Refresh;
            chest=owner;
            if(panel==null)Build();
            if(repairPanel==null)BuildRepairButton();
            BindRepairWallet();
            chest.Changed+=Refresh;chest.Paid+=ShowMoney;
            MiningLocalization.LanguageChanged+=Refresh;
            var stats=FindAnyObjectByType<MiningPlayerStats>();player=stats!=null?stats.transform:null;
            Refresh();
        }
        private RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent,false);r.sizeDelta=size;r.anchoredPosition=position;return r;
        }
        private UnityEngine.UI.Image Image(string name,Transform parent,Vector2 size,Vector2 position,Color color)
        {
            var r=Rect(name,parent,size,position);var i=r.gameObject.AddComponent<UnityEngine.UI.Image>();
            i.color=color;i.raycastTarget=false;return i;
        }
        private TextMeshProUGUI Label(string name,Vector2 size,Vector2 position,float fontSize)
        {
            var r=Rect(name,panel,size,position);var t=r.gameObject.AddComponent<TextMeshProUGUI>();
            if(chest.Data.font!=null)t.font=chest.Data.font;
            t.fontSize=fontSize;t.alignment=TextAlignmentOptions.Center;t.color=Color.white;t.raycastTarget=false;
            t.fontStyle = FontStyles.Normal;
            return t;
        }
private void Build()
        {
            panel = Rect("Treasure Chest Status", transform, new Vector2(530, 210), Vector2.zero);
            var canvas = panel.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
            canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0f;
            var frame = Image("Panel frame", panel, panel.sizeDelta, Vector2.zero, Color.white);
            statusFrame = frame;
            frame.sprite = chest.Data.compactPanelFrame != null ? chest.Data.compactPanelFrame : chest.Data.panelFrame;
            frame.type = UnityEngine.UI.Image.Type.Sliced;
            frame.rectTransform.anchorMin=Vector2.zero;frame.rectTransform.anchorMax=Vector2.one;frame.rectTransform.sizeDelta=Vector2.zero;
            var chestIcon = Image("Chest icon", panel, new Vector2(24, 24), new Vector2(-232, 49), Color.white);
            chestIcon.sprite = chest.Data.chestStatusIcon;
            chestIcon.preserveAspect = true;
            chestIcon.enabled = chestIcon.sprite != null;
            Top(chestIcon.rectTransform, -232, 33);
            title = Label("Chest name", new Vector2(330, 32), new Vector2(-45, 49), 24);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.color = Ink;Top(title.rectTransform,-45,33);
            var badge = Image("Level badge", panel, new Vector2(62, 26), new Vector2(213, 49), Color.white);
            badge.sprite = chest.Data.levelBadge;
            badge.color=new Color32(255,206,87,255);
            badge.type = UnityEngine.UI.Image.Type.Sliced;Top(badge.rectTransform,221,32);
            levelText = Label("Level", new Vector2(60, 26), new Vector2(213, 49), 18);
            levelText.color = Ink;Top(levelText.rectTransform,221,32);
            moneyPopup = Rect("Gold payout", panel, new Vector2(320, 50), new Vector2(0, 108));
            moneyIcon = Image("Coin icon", moneyPopup, Vector2.one * chest.Data.moneyPopupIconSize, Vector2.zero, Color.clear);
            moneyIcon.sprite = chest.Data.moneyIcon;
            moneyIcon.preserveAspect = true;
            moneyText = Label("Gold amount", new Vector2(260, 50), Vector2.zero, chest.Data.moneyPopupFontSize);
            moneyText.rectTransform.SetParent(moneyPopup, false);
            moneyText.rectTransform.pivot = new Vector2(0, .5f);
            moneyText.alignment = TextAlignmentOptions.MidlineLeft;
            moneyText.color = new Color(1, .85f, .15f, 0);
            hp = Bar("HP", 73, new Color(.9f, .035f, .05f));
            xp = Bar("XP", 133, new Color(.08f, .8f, .58f));
            ((RectTransform)hp.transform.parent).sizeDelta = new Vector2(498, 32);
            hp.rectTransform.sizeDelta = new Vector2(492, 26);
            ((RectTransform)xp.transform.parent).sizeDelta = new Vector2(498, 12);
            xp.rectTransform.sizeDelta = new Vector2(492, 6);
            hpHighlight = Highlight("HP highlight", hp.transform.parent);
            xpHighlight = Highlight("XP highlight", xp.transform.parent);
            hpText = Label("HP value", new Vector2(480, 22), new Vector2(0, 15), 17);
            hpText.color=Ink;hpText.font=chest.Data.statusValueFont!=null?chest.Data.statusValueFont:chest.Data.font;Top(hpText.rectTransform,0,73);
            progressCaption=Label("Level progress",new Vector2(300,22),Vector2.zero,12);progressCaption.alignment=TextAlignmentOptions.MidlineLeft;progressCaption.color=new Color32(127,103,84,255);progressCaption.font=chest.Data.statusBodyFont;Top(progressCaption.rectTransform,-97,108);
            footer = Rect("Chest stats", panel, new Vector2(498, 44), Vector2.zero);Top(footer,0,171);
            foreach(float x in new[]{-168f,0f,168f}){var tile=Image("Stat card",footer,new Vector2(160,44),new Vector2(x,0),Color.white);tile.sprite=chest.Data.levelBadge;tile.type=UnityEngine.UI.Image.Type.Sliced;}
            var coin = Image("Income coin", footer, new Vector2(22, 22), new Vector2(-226, 0), Color.white);
            coin.sprite = chest.Data.statusIncomeIcon!=null?chest.Data.statusIncomeIcon:chest.Data.moneyIcon;
            coin.preserveAspect = true;
            coin.enabled = coin.sprite != null;
            var armorIcon=Image("Armor icon",footer,new Vector2(22,22),new Vector2(-58,0),Color.white);armorIcon.sprite=chest.Data.statusArmorIcon;armorIcon.preserveAspect=true;
            var mrIcon=Image("Resistance icon",footer,new Vector2(22,22),new Vector2(110,0),Color.white);mrIcon.sprite=chest.Data.statusResistanceIcon;mrIcon.preserveAspect=true;
            incomeText = FooterLabel("Income", 113, -146, Ink);
            armorText = FooterLabel("Armor", 113, 22, Ink);
            resistanceText = FooterLabel("Magic resistance", 113, 190, Ink);
            incomeCaption=Caption("Income caption",-146);armorCaption=Caption("Armor caption",22);resistanceCaption=Caption("MR caption",190);
            xpText = Label("XP value",new Vector2(115,22),Vector2.zero,14);xpText.font=chest.Data.statusValueFont;xpText.color=new Color32(0,181,165,255);Top(xpText.rectTransform,191,108);
            xpText.alignment = TextAlignmentOptions.MidlineRight;
        }

        private TextMeshProUGUI Caption(string name,float x)
        {
            var label=FooterLabel(name,113,x,new Color32(127,103,84,255));label.font=chest.Data.statusBodyFont;label.enableAutoSizing=false;label.fontSize=10;label.rectTransform.anchoredPosition=new Vector2(x,11);return label;
        }

        private TextMeshProUGUI FooterLabel(string name, float width, float x, Color color)
        {
            var label = Label(name, new Vector2(width, 26), new Vector2(x, 0), 16);
            label.rectTransform.SetParent(footer, false);
            label.rectTransform.anchoredPosition = new Vector2(x,-7);
            label.font=chest.Data.statusValueFont!=null?chest.Data.statusValueFont:chest.Data.font;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = color;
            label.enableAutoSizing = true;
            label.fontSizeMin = 11;
            label.fontSizeMax = 16;
            return label;
        }
private UnityEngine.UI.Image Bar(string name, float y, Color color)
        {
            var bg = Image(name + " Background", panel, new Vector2(494, 30), new Vector2(0, y), Color.white);
            bg.sprite = name=="XP" && chest.Data.statusXpBackground!=null ? chest.Data.statusXpBackground : chest.Data.barBackground;
            bg.type=UnityEngine.UI.Image.Type.Sliced;Top(bg.rectTransform,0,y);
            var fill = Image(name + " Fill", bg.transform, new Vector2(486, 23), Vector2.zero, color);
            fill.sprite = name == "HP" ? chest.Data.healthFill : chest.Data.experienceFill;
            if (fill.sprite != null) fill.color = Color.white;
            // Resize the sliced pill, preserving both rounded ends and the complete gradient.
            fill.type = UnityEngine.UI.Image.Type.Sliced;
            fill.pixelsPerUnitMultiplier = 1f;
            fill.rectTransform.anchorMin = fill.rectTransform.anchorMax = new Vector2(0, .5f);
            fill.rectTransform.pivot = new Vector2(0, .5f);
            fill.rectTransform.anchoredPosition = new Vector2(3, 0);
            return fill;
        }

private UnityEngine.UI.Image Highlight(string name, Transform parent)
        {
            var image = Image(name, parent, new Vector2(10, 12), Vector2.zero, Color.white);
            image.sprite = chest.Data.barHighlight;
            image.enabled = image.sprite != null;
            return image;
        }

private void Refresh()
        {
            if (chest == null || panel == null) return;
            repairLabel.text = MiningLocalization.TextKey("BASE_CHEST_REPAIR", "Repair");
            RefreshRepairCost();
            bool broken = chest.IsBroken;
            if (repairPanel.gameObject.activeSelf != broken)
            {
                repairPanel.gameObject.SetActive(broken);
            }
            panel.sizeDelta = new Vector2(530,broken?256:210);
            title.text = MiningLocalization.TextKey("BASE_CHEST_NAME", "CHEST") + (broken?MiningLocalization.Text(" • BROKEN", " • HỎNG"):string.Empty);
            title.fontSize=broken?22:24;
            progressCaption.text=MiningLocalization.Text("LEVEL PROGRESS", "TIẾN ĐỘ CẤP");
            incomeCaption.text=MiningLocalization.Text("INCOME", "THU NHẬP");armorCaption.text=MiningLocalization.Text("ARMOR", "GIÁP");resistanceCaption.text=MiningLocalization.Text("MAGIC RESIST", "KHÁNG PHÉP");
            levelText.text = string.Format(MiningLocalization.TextKey("BASE_CHEST_LEVEL_SHORT", "LV. {0}"), chest.Level);
            float health = Mathf.Clamp01(chest.Health.Health / Mathf.Max(1f, chest.Health.MaxHealth));
            float experience = Mathf.Clamp01(chest.Experience / Mathf.Max(1f, chest.ExperienceRequired));
            UpdateBar(hp, health);
            UpdateBar(xp, experience);
            UpdateHighlight(hpHighlight, health);
            UpdateHighlight(xpHighlight, experience);
            hpText.text = $"{Mathf.CeilToInt(chest.Health.Health)} / {Mathf.CeilToInt(chest.Health.MaxHealth)} HP";
            xpText.text = $"{experience * 100f:0}% XP";
            incomeText.text = string.Format(MiningLocalization.TextKey("BASE_CHEST_INCOME", "+{0:0.##}/{1:0.##}s"),
                chest.Data.Gold(chest.Level), 1f / Mathf.Max(.01f, chest.Data.ticksPerSecond));
            armorText.text = chest.Health.Armor.ToString("0.##");
            resistanceText.text = chest.Health.MagicResistance.ToString("0.##");
        }

private void UpdateHighlight(UnityEngine.UI.Image image, float progress)
        {
            image.enabled = false; // Compact bars use clean rounded ends instead of oversized caps.
            image.rectTransform.anchoredPosition = new Vector2(-243f + Mathf.Max(5f, progress * 486f - 5f), 0f);
        }


private void ShowMoney(float amount)
        {
            moneyText.text=$"+{amount:0.##}";
            moneyText.fontSize = Mathf.Max(1f, chest.Data.moneyPopupFontSize);
            moneyIcon.rectTransform.sizeDelta = Vector2.one * Mathf.Max(1f, chest.Data.moneyPopupIconSize);
            float width=moneyText.GetPreferredValues(moneyText.text).x;
            float iconWidth=moneyIcon.sprite!=null?chest.Data.moneyPopupIconSize:0;
            float gap=iconWidth>0?10:0;
            float total=iconWidth+gap+width;
            moneyIcon.rectTransform.anchoredPosition=new Vector2(-total*.5f+iconWidth*.5f,0);
            moneyText.rectTransform.sizeDelta=new Vector2(width+2,Mathf.Max(50f, moneyText.fontSize * 1.6f));
            moneyText.rectTransform.anchoredPosition=new Vector2(-total*.5f+iconWidth+gap,0);
            moneyAge=0;
        }

private void LateUpdate()
        {
            if (panel == null || chest == null) return;
            if (viewer == null) viewer = Camera.main;
            if (viewer == null) return;
            var d = chest.Data;
            moneyAge += Time.deltaTime;
            float progress = Mathf.Clamp01(moneyAge / Mathf.Max(.1f, d.moneyPopupLifetime));
            float alpha = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.55f, 1f, progress));
            float pop = Mathf.Clamp01(moneyAge / Mathf.Max(.01f, d.moneyPopupPopSeconds));
            float overshoot = 1f + 2.7f * Mathf.Pow(pop - 1f, 3f) + 1.7f * Mathf.Pow(pop - 1f, 2f);
            moneyPopup.localScale = Vector3.one * Mathf.Max(.01f, d.moneyPopupScale) * Mathf.LerpUnclamped(.45f, 1f, overshoot);
            moneyText.color = new Color(1, .85f, .15f, alpha);
            moneyIcon.color = new Color(1, 1, 1, moneyIcon.sprite != null ? alpha : 0);
            moneyPopup.anchoredPosition = new Vector2(0, 108 + d.moneyPopupRise * (1f - Mathf.Pow(1f - progress, 2f)));
            float distance = Vector3.Distance(transform.position, player != null ? player.position : viewer.transform.position);
            UpdateHealthPanel(distance, Time.unscaledDeltaTime);
        }

        private void UpdateHealthPanel(float distance, float deltaTime)
        {
            var d = chest.Data;
            // Upgrade Panel's eased fade, 12% minimum scale and downward slide.
            // Separate open/close thresholds prevent flickering at the boundary.
            if (distance <= d.panelNearDistance) nearby = true;
            else if (distance >= Mathf.Max(d.panelNearDistance + .01f, d.panelHideDistance)) nearby = false;
            visibility = Mathf.MoveTowards(visibility, nearby ? 1f : 0f,
                deltaTime / Mathf.Max(.01f, d.panelTransitionSeconds));
            float eased = Mathf.SmoothStep(0, 1, visibility);
            panel.gameObject.SetActive(eased > 0f);
            canvasGroup.alpha = eased;
            canvasGroup.interactable = canvasGroup.blocksRaycasts = chest.IsBroken && eased > .9f;
            panel.position = transform.position + d.panelOffset + Vector3.down * ((1f - eased) * .9f);
            panel.rotation = viewer.transform.rotation;
            panel.GetComponent<Canvas>().worldCamera = viewer;
            float scale = d.panelWorldScale * Mathf.Lerp(.12f, 1f, eased);
            Vector3 parent = transform.lossyScale;
            panel.localScale = new Vector3(scale / Mathf.Max(.001f, Mathf.Abs(parent.x)),
                scale / Mathf.Max(.001f, Mathf.Abs(parent.y)), scale / Mathf.Max(.001f, Mathf.Abs(parent.z)));
        }
private void OnDestroy(){if(chest!=null){chest.Changed-=Refresh;chest.Paid-=ShowMoney;}MiningLocalization.LanguageChanged-=Refresh;UnbindRepairWallet();}

        private void OnDisable()
        {
            UnbindRepairWallet();
            if (repairPanel != null) repairPanel.gameObject.SetActive(false);
            if (canvasGroup != null) canvasGroup.interactable = canvasGroup.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            if (chest != null && repairPanel != null) { BindRepairWallet(); Refresh(); }
        }

        private void BuildRepairButton()
        {
            // Share the chest billboard and distance fade. This is not a gameplay-blocking modal.
            panel.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var background = Image("Repair", panel, new Vector2(498, 36), Vector2.zero, new Color32(255,206,87,255));
            background.sprite = chest.Data.levelBadge;background.type=UnityEngine.UI.Image.Type.Sliced;Top(background.rectTransform,0,220);
            background.raycastTarget = true;
            repairPanel = background.rectTransform;
            repairPanel.gameObject.AddComponent<CanvasGroup>();
            repairButton = repairPanel.gameObject.AddComponent<UnityEngine.UI.Button>();
            repairButton.targetGraphic = background;
            repairButton.onClick.AddListener(() => chest.Repair());
            var label = Rect("Repair label", repairPanel, new Vector2(110, 34), new Vector2(-30, 0));
            repairLabel = label.gameObject.AddComponent<TextMeshProUGUI>();
            if (chest.Data.font != null) repairLabel.font = chest.Data.font;
            repairLabel.fontSize = 18;repairLabel.color=Ink;
            repairLabel.alignment = TextAlignmentOptions.Center;
            repairLabel.raycastTarget = false;
            repairCoin = Image("Repair coin", repairPanel, new Vector2(16, 16), Vector2.zero, Color.white);
            repairCoin.sprite = chest.Data.statusIncomeIcon!=null?chest.Data.statusIncomeIcon:chest.Data.moneyIcon;
            repairCoin.preserveAspect = true;
            repairCostLabel = Label("Repair cost", new Vector2(100, 24), new Vector2(76, 0), 18);repairCostLabel.font=chest.Data.statusValueFont;
            repairCostLabel.enableAutoSizing = true;
            repairCostLabel.fontSizeMin = 7;
            repairCostLabel.fontSizeMax = 16;
            repairCostLabel.rectTransform.SetParent(repairPanel, false);
            repairPanel.gameObject.SetActive(false);
        }

        private void BindRepairWallet()
        {
            UnbindRepairWallet();
            repairWallet = chest.Wallet;
            if (repairWallet != null) repairWallet.MoneyChanged += OnRepairMoneyChanged;
        }


        private void UnbindRepairWallet()
        {
            if (repairWallet != null) repairWallet.MoneyChanged -= OnRepairMoneyChanged;
            repairWallet = null;
        }

        private void OnRepairMoneyChanged(float _) => RefreshRepairCost();

        private void RefreshRepairCost()
        {
            repairCostLabel.text = chest.RepairCost.ToString("0.##");
            float width = Mathf.Min(100f, repairCostLabel.GetPreferredValues(repairCostLabel.text).x);
            float iconWidth = repairCoin.sprite != null ? 16f : 0f;
            float gap = iconWidth > 0 ? 4f : 0f;
            float total = iconWidth + gap + width;
            repairCoin.rectTransform.anchoredPosition = new Vector2(67f - total * .5f + iconWidth * .5f, 0);
            repairCostLabel.rectTransform.pivot = new Vector2(0, .5f);
            repairCostLabel.rectTransform.sizeDelta = new Vector2(width + 2, 24);
            repairCostLabel.rectTransform.anchoredPosition = new Vector2(67f - total * .5f + iconWidth + gap, 0);
            repairCostLabel.color = chest.CanRepair ? new Color(.1f, .08f, .03f) : new Color(.65f, .03f, .03f);
            repairButton.interactable = chest.CanRepair;
        }
    

private void UpdateBar(UnityEngine.UI.Image fill, float progress)
        {
            fill.fillAmount = progress;
            fill.enabled = progress > 0f;
            fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 492f * progress);
        }
}
}
