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
            return t;
        }
private void Build()
        {
            panel = Rect("Treasure Chest Status", transform, new Vector2(530, 144), Vector2.zero);
            var canvas = panel.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
            canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0f;
            var frame = Image("Panel frame", panel, panel.sizeDelta, Vector2.zero, Color.white);
            frame.sprite = chest.Data.compactPanelFrame != null ? chest.Data.compactPanelFrame : chest.Data.panelFrame;
            var chestIcon = Image("Chest icon", panel, new Vector2(24, 24), new Vector2(-232, 49), Color.white);
            chestIcon.sprite = chest.Data.chestStatusIcon;
            chestIcon.preserveAspect = true;
            chestIcon.enabled = chestIcon.sprite != null;
            title = Label("Chest name", new Vector2(330, 32), new Vector2(-45, 49), 24);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            var badge = Image("Level badge", panel, new Vector2(62, 26), new Vector2(213, 49), Color.white);
            badge.sprite = chest.Data.levelBadge;
            levelText = Label("Level", new Vector2(60, 26), new Vector2(213, 49), 18);
            levelText.color = new Color(.45f, .83f, 1f);
            moneyPopup = Rect("Gold payout", panel, new Vector2(320, 50), new Vector2(0, 108));
            moneyIcon = Image("Coin icon", moneyPopup, Vector2.one * chest.Data.moneyPopupIconSize, Vector2.zero, Color.clear);
            moneyIcon.sprite = chest.Data.moneyIcon;
            moneyIcon.preserveAspect = true;
            moneyText = Label("Gold amount", new Vector2(260, 50), Vector2.zero, chest.Data.moneyPopupFontSize);
            moneyText.rectTransform.SetParent(moneyPopup, false);
            moneyText.rectTransform.pivot = new Vector2(0, .5f);
            moneyText.alignment = TextAlignmentOptions.MidlineLeft;
            moneyText.color = new Color(1, .85f, .15f, 0);
            hp = Bar("HP", 15, new Color(.9f, .035f, .05f));
            xp = Bar("XP", -9, new Color(.08f, .8f, .58f));
            ((RectTransform)hp.transform.parent).sizeDelta = new Vector2(494, 22);
            hp.rectTransform.sizeDelta = new Vector2(486, 18);
            ((RectTransform)xp.transform.parent).sizeDelta = new Vector2(494, 10);
            xp.rectTransform.sizeDelta = new Vector2(486, 6);
            hpHighlight = Highlight("HP highlight", hp.transform.parent);
            xpHighlight = Highlight("XP highlight", xp.transform.parent);
            hpText = Label("HP value", new Vector2(480, 22), new Vector2(0, 15), 17);
            Image("Footer divider", panel, new Vector2(494, 1), new Vector2(0, -25), new Color(.6f, .44f, .15f, .3f));
            footer = Rect("Chest stats", panel, new Vector2(494, 28), new Vector2(0, -46));
            var coin = Image("Income coin", footer, new Vector2(18, 18), new Vector2(-235, 0), Color.white);
            coin.sprite = chest.Data.moneyIcon;
            coin.preserveAspect = true;
            coin.enabled = coin.sprite != null;
            incomeText = FooterLabel("Income", 94, -174, new Color(1f, .77f, .16f));
            armorText = FooterLabel("Armor", 120, -49, new Color(.69f, .83f, 1f));
            resistanceText = FooterLabel("Magic resistance", 114, 81, new Color(.8f, .66f, 1f));
            xpText = FooterLabel("XP value", 95, 199, new Color(.12f, .86f, .63f));
            xpText.alignment = TextAlignmentOptions.MidlineRight;
        }

        private TextMeshProUGUI FooterLabel(string name, float width, float x, Color color)
        {
            var label = Label(name, new Vector2(width, 26), new Vector2(x, 0), 16);
            label.rectTransform.SetParent(footer, false);
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
            bg.sprite = chest.Data.barBackground;
            var fill = Image(name + " Fill", bg.transform, new Vector2(486, 23), Vector2.zero, color);
            fill.sprite = name == "HP" ? chest.Data.healthFill : chest.Data.experienceFill;
            if (fill.sprite != null) fill.color = Color.white;
            // Resize the sliced pill, preserving both rounded ends and the complete gradient.
            fill.type = UnityEngine.UI.Image.Type.Sliced;
            if (fill.sprite != null) fill.pixelsPerUnitMultiplier = fill.sprite.rect.width / 98f;
            fill.rectTransform.anchorMin = fill.rectTransform.anchorMax = new Vector2(0, .5f);
            fill.rectTransform.pivot = new Vector2(0, .5f);
            fill.rectTransform.anchoredPosition = new Vector2(4, 0);
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
            title.text = MiningLocalization.TextKey("BASE_CHEST_NAME", "CHEST");
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
            armorText.text = string.Format(MiningLocalization.TextKey("BASE_CHEST_ARMOR", "Armor: {0:0.##}"), chest.Health.Armor);
            resistanceText.text = string.Format(MiningLocalization.TextKey("BASE_CHEST_MR", "MR: {0:0.##}"), chest.Health.MagicResistance);
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
            var background = Image("Repair", panel, new Vector2(340, 89), chest.Data.repairButtonOffset, Color.white);
            background.sprite = chest.Data.repairButtonSprite != null ? chest.Data.repairButtonSprite : chest.Data.panelFrame;
            background.raycastTarget = true;
            repairPanel = background.rectTransform;
            repairPanel.gameObject.AddComponent<CanvasGroup>();
            repairButton = repairPanel.gameObject.AddComponent<UnityEngine.UI.Button>();
            repairButton.targetGraphic = background;
            repairButton.onClick.AddListener(() => chest.Repair());
            var label = Rect("Repair label", repairPanel, new Vector2(230, 60), new Vector2(-48, 0));
            repairLabel = label.gameObject.AddComponent<TextMeshProUGUI>();
            if (chest.Data.font != null) repairLabel.font = chest.Data.font;
            repairLabel.fontSize = 30;
            repairLabel.alignment = TextAlignmentOptions.Center;
            repairLabel.raycastTarget = false;
            repairCoin = Image("Repair coin", repairPanel, new Vector2(16, 16), Vector2.zero, Color.white);
            repairCoin.sprite = chest.Data.moneyIcon;
            repairCoin.preserveAspect = true;
            repairCostLabel = Label("Repair cost", new Vector2(44, 24), new Vector2(130, 0), 16);
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
            float width = Mathf.Min(44f, repairCostLabel.GetPreferredValues(repairCostLabel.text).x);
            float iconWidth = repairCoin.sprite != null ? 16f : 0f;
            float gap = iconWidth > 0 ? 4f : 0f;
            float total = iconWidth + gap + width;
            repairCoin.rectTransform.anchoredPosition = new Vector2(123f - total * .5f + iconWidth * .5f, 0);
            repairCostLabel.rectTransform.pivot = new Vector2(0, .5f);
            repairCostLabel.rectTransform.sizeDelta = new Vector2(width + 2, 24);
            repairCostLabel.rectTransform.anchoredPosition = new Vector2(123f - total * .5f + iconWidth + gap, 0);
            repairCostLabel.color = chest.CanRepair ? new Color(.1f, .08f, .03f) : new Color(.65f, .03f, .03f);
            repairButton.interactable = chest.CanRepair;
        }
    

private void UpdateBar(UnityEngine.UI.Image fill, float progress)
        {
            fill.fillAmount = progress;
            fill.enabled = progress > 0f;
            fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 486f * progress);
        }
}
}
