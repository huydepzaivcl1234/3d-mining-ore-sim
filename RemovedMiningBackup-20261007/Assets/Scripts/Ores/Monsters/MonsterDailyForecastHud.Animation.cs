using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    public sealed partial class MonsterDailyForecastHud
    {
        [Header("Forecast presentation")]
        [Min(.01f), SerializeField] private float slideSeconds = .6f;
        [Min(.01f), SerializeField] private float numberRollSeconds = .65f;
        [Min(0f), SerializeField] private float rowRevealDelay = .18f;
        [Min(1f), SerializeField] private float charactersPerSecond = 32f;
        [SerializeField] private float rollAngle = 12f;
        [SerializeField] private float offscreenPadding = 30f;
        private Vector2 shownPosition;
        private bool hudRequested = true, wasDaytime;
        private float slideProgress, revealElapsed;
        private int revealedDay = -1;
        private bool revealing;
        private readonly List<ForecastNumbers> numbers = new();
        private sealed class ForecastNumbers
        {
            public int planned, alive, fromPlanned, fromAlive, toPlanned, toAlive;
            public float elapsed;
        }
        private bool IsDaytime => source == null || source.DayNight == null ||
            source.DayNight.CurrentPeriod != MiningTimePeriod.Night;

        private void BeginMorningReveal()
        {
            revealedDay = source.ForecastDay;
            revealElapsed = 0f; revealing = true;
            heading.maxVisibleCharacters = eventLabel.maxVisibleCharacters = 0;
            foreach (var number in numbers)
            {
                number.planned = number.alive = number.fromPlanned = number.fromAlive = 0;
                number.elapsed = 0f;
            }
            foreach (var row in rows) row.GetComponent<CanvasGroup>().alpha = 0f;
        }

        private void SetRowNumbers(int index, int planned, int alive)
        {
            var n = numbers[index];
            if (n.toPlanned != planned || n.toAlive != alive)
            {
                n.fromPlanned = n.planned; n.fromAlive = n.alive;
                n.toPlanned = planned; n.toAlive = alive; n.elapsed = 0f;
            }
            DrawNumbers(index);
        }
        private void DrawNumbers(int index)
        {
            var n = numbers[index];
            counts[index].text = string.Format(MiningLocalization.TextKey("MONSTER_FORECAST_COUNTS", "Today: {0}\nAlive: {1}"), n.planned, n.alive);
        }

        private void Update()
        {
            AdvancePresentation(Time.unscaledDeltaTime);
        }

        private void AdvancePresentation(float dt)
        {
            if (source == null || root == null) return;
            bool day = IsDaytime;
            if (day && !wasDaytime) { Refresh(); BeginMorningReveal(); }
            wasDaytime = day;
            slideProgress = Mathf.MoveTowards(slideProgress, day ? 1f : 0f, dt / Mathf.Max(.01f, slideSeconds));
            float eased = slideProgress * slideProgress * (3f - 2f * slideProgress);
            root.anchoredPosition = shownPosition + Vector2.right * (root.rect.width + offscreenPadding) * (1f - eased);
            root.localRotation = Quaternion.Euler(0, 0, rollAngle * Mathf.Sin((1f - eased) * Mathf.PI));
            root.GetComponent<CanvasGroup>().alpha = eased;
            root.gameObject.SetActive(hudRequested && (slideProgress > 0f || day));
            if (!hudRequested || !day) return;
            if (revealing && slideProgress >= 1f) revealElapsed += dt;
            int characters = revealing ? Mathf.FloorToInt(revealElapsed * charactersPerSecond) : int.MaxValue;
            heading.maxVisibleCharacters = eventLabel.maxVisibleCharacters = characters;
            for (int i = 0; i < visibleEntries.Count; i++)
            {
                float rowTime = revealing ? Mathf.Max(0, revealElapsed - i * rowRevealDelay) : numberRollSeconds;
                var group = rows[i].GetComponent<CanvasGroup>();
                group.alpha = Mathf.Clamp01(rowTime / Mathf.Max(.01f, rowRevealDelay));
                if (group.alpha <= 0f) continue;
                var n = numbers[i];
                n.elapsed += dt;
                float t = Mathf.Clamp01(n.elapsed / Mathf.Max(.01f, numberRollSeconds));
                int planned = Mathf.RoundToInt(Mathf.Lerp(n.fromPlanned, n.toPlanned, t));
                int alive = Mathf.RoundToInt(Mathf.Lerp(n.fromAlive, n.toAlive, t));
                if (planned != n.planned || alive != n.alive)
                {
                    n.planned = planned; n.alive = alive; DrawNumbers(i);
                    counts[i].rectTransform.localRotation = Quaternion.Euler(0, 0, (1f - t) * 3f);
                }
                var rect = counts[i].rectTransform;
                // A small vertical reel motion accompanies the stepping digits.
                rect.localScale = new Vector3(1f, t < 1f ? 1f + Mathf.Sin(t * Mathf.PI * 8f) * .08f : 1f, 1f);
                if (t >= 1f) rect.localRotation = Quaternion.identity;
            }
            if (revealing && revealElapsed > Mathf.Max(heading.text.Length, eventLabel.text.Length) / charactersPerSecond +
                visibleEntries.Count * rowRevealDelay + numberRollSeconds) revealing = false;
        }
    }
}
