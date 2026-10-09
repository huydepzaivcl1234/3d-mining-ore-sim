using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Read-only forecast. Modal visibility remains owned by the existing HUD coordinator.</summary>
    public sealed partial class MonsterDailyForecastHud : MonoBehaviour
    {
        private MonsterSpawnZone source;
        private RectTransform root;
        private TMP_Text heading, remaining;
        private RectTransform eventRow;
        private UnityEngine.UI.Image eventIcon;
        private TMP_Text eventLabel;
        private readonly List<TMP_Text> counts = new();
        private readonly List<TMP_Text> aliveCounts = new(), names = new();
        private TMP_Text remainingValue;
        private TMP_FontAsset displayFont;
        private Sprite creamSprite, whiteSprite;
        private readonly List<UnityEngine.UI.Image> icons = new();
        private readonly List<GameObject> rows = new();
        private readonly List<MonsterSpawnEntry> visibleEntries = new();

        public void Configure(MonsterSpawnZone spawner)
        {
            Unsubscribe();
            source = spawner;
            Subscribe();
            Build();
            Refresh();
        }
        private void OnEnable()
        {
            Subscribe();
            if (source != null) { SetVisible(source.isActiveAndEnabled); Refresh(); }
        }
        private void OnDisable() { Unsubscribe(); SetVisible(false); }
        private void OnDestroy() { Unsubscribe(); if (root != null) Destroy(root.gameObject); }
        private void Subscribe()
        {
            if (source != null) { source.ForecastChanged -= Refresh; source.ForecastChanged += Refresh; }
            MiningLocalization.LanguageChanged -= Refresh;
            MiningLocalization.LanguageChanged += Refresh;
        }
        private void Unsubscribe()
        {
            if (source != null) source.ForecastChanged -= Refresh;
            MiningLocalization.LanguageChanged -= Refresh;
        }
        public void SetVisible(bool visible)
        {
            hudRequested = visible;
            if (root != null) root.gameObject.SetActive(visible && (slideProgress > 0f || IsDaytime));
        }

        private void Build()
        {
            if (root != null) return;
            RectTransform settings = null;
            foreach (RectTransform rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (rect.name == "Audio Menu Button") { settings = rect; break; }
            if (settings == null || settings.parent == null) return;
            root = CreateRect("Daily Monster Forecast", settings.parent);
            root.anchorMin = root.anchorMax = root.pivot = Vector2.one;
            root.sizeDelta = new Vector2(344f, 120f);
            root.anchoredPosition = new Vector2(-14f, settings.anchoredPosition.y - settings.rect.height - 12f);
            shownPosition = root.anchoredPosition;
            slideProgress = IsDaytime ? 1f : 0f;
            var background = root.gameObject.AddComponent<UnityEngine.UI.Image>();
            creamSprite = Resources.Load<Sprite>("ShellUI/Cream"); whiteSprite = Resources.Load<Sprite>("ShellUI/White");
            var sample = settings.parent.Find("Audio Settings Panel/Design Settings Title")?.GetComponent<TMP_Text>();
            displayFont = sample != null ? sample.font : TMP_Settings.defaultFontAsset;
            background.sprite = creamSprite; background.type = UnityEngine.UI.Image.Type.Sliced;
            background.color = Color.white;
            background.raycastTarget = false;
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;
            var layout = root.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            heading = CreateText("Title", root, 29f, 18f);
            var columns = CreateRect("Column Headings", root); columns.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight=23;
            var colLayout=columns.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();colLayout.childControlWidth=colLayout.childControlHeight=true;colLayout.childForceExpandWidth=false;colLayout.spacing=8;
            var monsterCaption=CreateText("Monster Heading",columns,23,10);monsterCaption.text=MiningLocalization.Text("MONSTER","QUÁI");monsterCaption.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth=1;
            foreach(var caption in new[]{MiningLocalization.Text("TODAY","HÔM NAY"),MiningLocalization.Text("ALIVE","CÒN SỐNG")}){var t=CreateText("Column",columns,23,10);t.text=caption;t.alignment=TextAlignmentOptions.Center;t.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth=43;}
            eventRow = CreateRect("Daily Event", root);
            eventRow.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 68f;
            var eventLayout = eventRow.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            eventLayout.spacing = 8f;
            eventLayout.childControlWidth = eventLayout.childControlHeight = true;
            eventLayout.childForceExpandWidth = eventLayout.childForceExpandHeight = false;
            eventLayout.childAlignment = TextAnchor.MiddleLeft;
            var eventImage = CreateRect("Event Icon", eventRow);
            eventIcon = eventImage.gameObject.AddComponent<UnityEngine.UI.Image>();
            eventIcon.preserveAspect = true; eventIcon.raycastTarget = false;
            var eventSize = eventImage.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            eventSize.preferredWidth = eventSize.preferredHeight = eventSize.minWidth = 64f;
            eventLabel = CreateText("Event Name", eventRow, 64f, 17f);
            eventLabel.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1f;
            var footer=CreateRect("Remaining Row",root);footer.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight=36;
            var fl=footer.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();fl.childControlWidth=fl.childControlHeight=true;fl.childForceExpandWidth=false;fl.spacing=8;
            remaining = CreateText("Remaining",footer,36,13);remaining.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth=1;
            remainingValue = CreateNumber("Pending",footer,new Color32(217,233,222,255));
        }

        private void AddRow()
        {
            RectTransform row = CreateRect("Monster", root);
            var size = row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            size.preferredHeight = 50f;
            var layout = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            RectTransform imageRect = CreateRect("Icon", row);
            var image = imageRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            var imageSize = imageRect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            imageSize.preferredWidth = imageSize.preferredHeight = 42f;
            imageSize.minWidth = 42f;
            TMP_Text name = CreateText("Name", row, 50f, 14f);name.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1f;
            TMP_Text count = CreateNumber("Today",row,Color.white);
            TMP_Text alive = CreateNumber("Alive",row,new Color32(18,182,170,255));alive.color=Color.white;
            rows.Add(row.gameObject); icons.Add(image); counts.Add(count);
            aliveCounts.Add(alive);names.Add(name);
            row.gameObject.AddComponent<CanvasGroup>();
            numbers.Add(new ForecastNumbers());
        }

        private void Refresh()
        {
            if (source == null) return;
            Build();
            if (root == null) return;
            var forecast = source.DailyForecast;
            visibleEntries.Clear();
            foreach (var entry in source.MonsterEntries)
            {
                if (entry == null || entry.prefab == null) continue;
                bool planned = false;
                foreach (var daily in forecast) if (daily.Entry == entry && daily.Planned > 0) { planned = true; break; }
                if (planned || source.GetAliveCount(entry) > 0) visibleEntries.Add(entry);
            }
            while (rows.Count < visibleEntries.Count) AddRow();
            int pending = 0;
            foreach (var daily in forecast) pending += daily.Remaining;
            for (int i = 0; i < rows.Count; i++)
            {
                bool active = i < visibleEntries.Count;
                rows[i].SetActive(active);
                if (!active) continue;
                MonsterSpawnEntry entry = visibleEntries[i];
                int planned = 0;
                foreach (var daily in forecast) if (daily.Entry == entry) { planned = daily.Planned; break; }
                Sprite icon = entry.icon;
                if (icon == null && entry.prefab != null)
                    icon = Resources.Load<Sprite>("MiningMonsterIcons/" + entry.prefab.name);
                icons[i].sprite = icon;
                icons[i].enabled = icon != null;
                names[i].text = string.IsNullOrEmpty(entry.displayName) ? entry.prefab.name.Replace("Monster", "") : entry.displayName;
                SetRowNumbers(i, planned, source.GetAliveCount(entry));
            }
            heading.text = string.Format(MiningLocalization.TextKey("MONSTER_FORECAST_TITLE", "DAY {0} - MONSTERS"), source.ForecastDay);
            bool hasEvent = source.CurrentDailyEvent != DailyEncounterEvent.Normal || source.HasForecastBoss;
            eventRow.gameObject.SetActive(hasEvent);
            eventIcon.sprite = source.DailyEventIcon;
            eventIcon.enabled = eventIcon.sprite != null;
            eventLabel.text = hasEvent ? source.DailyEventLabel : "";
            if (source.CurrentDailyEvent == DailyEncounterEvent.Normal) eventLabel.text = "";
            if (source.HasForecastBoss) eventLabel.text += (eventLabel.text.Length > 0 ? "\n" : "") +
                MiningLocalization.TextKey("MONSTER_FORECAST_BOSS_PRESENT", "BOSS PRESENT");
            remaining.text = MiningLocalization.Text("Still arriving", "Đang đến");remainingValue.text=pending.ToString();
            remaining.transform.parent.SetAsLastSibling();
            root.sizeDelta = new Vector2(344f, 152f + visibleEntries.Count * 58f + (hasEvent ? 76f : 0f));
            if (revealedDay != source.ForecastDay && IsDaytime) BeginMorningReveal();
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }
        private TMP_Text CreateNumber(string name, Transform parent, Color color)
        {
            var rect=CreateRect(name,parent);var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=whiteSprite;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=color;image.raycastTarget=false;
            var le=rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();le.preferredWidth=le.minWidth=43;le.preferredHeight=36;
            var child=CreateRect("Value",rect);child.anchorMin=Vector2.zero;child.anchorMax=Vector2.one;child.offsetMin=child.offsetMax=Vector2.zero;
            var text=child.gameObject.AddComponent<TextMeshProUGUI>();text.font=displayFont;text.fontSize=14;text.color=new Color32(53,41,35,255);text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.overflowMode=TextOverflowModes.Overflow;return text;
        }
        private TMP_Text CreateText(string name, Transform parent, float height, float fontSize)
        {
            RectTransform rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = displayFont;
            text.fontSize = fontSize;
            text.color = new Color32(53,41,35,255);
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = fontSize;
            var layout = rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.preferredHeight = height;
            return text;
        }
    }
}
