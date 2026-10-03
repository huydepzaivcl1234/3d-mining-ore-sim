using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Read-only forecast. Modal visibility remains owned by the existing HUD coordinator.</summary>
    public sealed class MonsterDailyForecastHud : MonoBehaviour
    {
        private MonsterSpawnZone source;
        private RectTransform root;
        private TMP_Text heading, remaining;
        private RectTransform eventRow;
        private UnityEngine.UI.Image eventIcon;
        private TMP_Text eventLabel;
        private readonly List<TMP_Text> counts = new();
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
        public void SetVisible(bool visible) { if (root != null) root.gameObject.SetActive(visible); }

        private void Build()
        {
            if (root != null) return;
            RectTransform settings = null;
            foreach (RectTransform rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (rect.name == "Audio Menu Button") { settings = rect; break; }
            if (settings == null || settings.parent == null) return;
            root = CreateRect("Daily Monster Forecast", settings.parent);
            root.anchorMin = root.anchorMax = root.pivot = Vector2.one;
            root.sizeDelta = new Vector2(320f, 120f);
            root.anchoredPosition = new Vector2(-14f, settings.anchoredPosition.y - settings.rect.height - 12f);
            var background = root.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.10f, 0.065f, 0.035f, 0.94f);
            background.raycastTarget = false;
            var outline = root.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.8f, 0.48f, 0.1f);
            outline.effectDistance = new Vector2(2f, -2f);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;
            var layout = root.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 4f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            heading = CreateText("Title", root, 29f, 18f);
            heading.color = new Color(1f, 0.75f, 0.3f);
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
            remaining = CreateText("Remaining", root, 23f, 15f);
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
            TMP_Text count = CreateText("Count", row, 50f, 17f);
            count.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1f;
            count.alignment = TextAlignmentOptions.MidlineLeft;
            rows.Add(row.gameObject); icons.Add(image); counts.Add(count);
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
                counts[i].text = string.Format(MiningLocalization.TextKey("MONSTER_FORECAST_COUNTS", "Today: {0}\nAlive: {1}"),
                    planned, source.GetAliveCount(entry));
            }
            heading.text = string.Format(MiningLocalization.TextKey("MONSTER_FORECAST_TITLE", "DAY {0} - MONSTERS"), source.ForecastDay);
            bool hasEvent = source.CurrentDailyEvent != DailyEncounterEvent.Normal;
            eventRow.gameObject.SetActive(hasEvent);
            eventIcon.sprite = source.DailyEventIcon;
            eventIcon.enabled = eventIcon.sprite != null;
            eventLabel.text = hasEvent ? source.DailyEventLabel : "";
            remaining.text = string.Format(MiningLocalization.TextKey("MONSTER_FORECAST_REMAINING", "Still arriving: {0}"), pending);
            remaining.transform.SetAsLastSibling();
            root.sizeDelta = new Vector2(320f, 76f + visibleEntries.Count * 54f + (hasEvent ? 72f : 0f));
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }
        private static TMP_Text CreateText(string name, Transform parent, float height, float fontSize)
        {
            RectTransform rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = Color.white;
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
