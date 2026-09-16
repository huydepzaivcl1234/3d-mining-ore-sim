using System.Collections;
using Microlight.MicroBar;
using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Decorates the authoritative NpcProgressionHud with an accurate next-ore goal.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NpcProgressionHud))]
    public sealed class JuicyMinerProgress : MonoBehaviour
    {
        [SerializeField] private NpcProgressionSystem progressionSystem;
        [SerializeField] private OreSpawner oreSpawner;
        [SerializeField] private MicroBar experienceBar;
        [SerializeField] private RectTransform cardTransform;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI levelBadgeText;
        [SerializeField] private TextMeshProUGUI rankTitleText;
        [SerializeField] private TextMeshProUGUI powerLabelText;
        [SerializeField] private TextMeshProUGUI powerValueText;
        [SerializeField] private TextMeshProUGUI rewardLabelText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private TextMeshProUGUI xpLabelText;
        [SerializeField] private TextMeshProUGUI xpPercentText;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip gainXpSound;
        [SerializeField] private AudioClip levelUpSound;

        private Coroutine punchRoutine;
        private Vector3 restingScale;
        private int lastLevel;
        private float lastExperience;
        private int displayedPercent = -1;

        private void Awake()
        {
            if (cardTransform != null) restingScale = cardTransform.localScale;
        }

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged += Refresh;
            if (progressionSystem != null)
            {
                progressionSystem.ProgressionChanged += OnProgressionChanged;
                progressionSystem.LevelChanged += OnLevelChanged;
                lastLevel = progressionSystem.CurrentLevel;
                lastExperience = progressionSystem.CurrentExperience;
            }
            Refresh();
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= Refresh;
            if (progressionSystem != null)
            {
                progressionSystem.ProgressionChanged -= OnProgressionChanged;
                progressionSystem.LevelChanged -= OnLevelChanged;
            }
            if (punchRoutine != null) StopCoroutine(punchRoutine);
            punchRoutine = null;
            if (cardTransform != null) cardTransform.localScale = restingScale;
        }

        private void Update()
        {
            // Follow the existing MicroBar animation instead of animating a second fake XP fill.
            if (xpPercentText == null || experienceBar == null) return;
            int percent = Mathf.RoundToInt(experienceBar.HPPercent * 100f);
            if (percent == displayedPercent) return;
            displayedPercent = percent;
            xpPercentText.text = $"{percent}%";
        }

        private void OnProgressionChanged()
        {
            if (progressionSystem != null)
            {
                float current = progressionSystem.CurrentExperience;
                if (progressionSystem.CurrentLevel == lastLevel && current > lastExperience + 0.01f)
                    Play(gainXpSound);
                lastExperience = current;
            }
            Refresh();
        }

        private void OnLevelChanged(int level)
        {
            bool gainedLevel = level > lastLevel;
            lastLevel = level;
            if (!gainedLevel) return; // Rebirth/reset is not a level-up reward.
            Play(levelUpSound);
            if (cardTransform != null)
            {
                if (punchRoutine != null) StopCoroutine(punchRoutine);
                punchRoutine = StartCoroutine(Punch());
            }
            Refresh();
        }

        private IEnumerator Punch()
        {
            Vector3 peak = restingScale * 1.05f;
            for (float elapsed = 0f; elapsed < 0.1f; elapsed += Time.unscaledDeltaTime)
            {
                cardTransform.localScale = Vector3.Lerp(restingScale, peak,
                    Mathf.Clamp01((elapsed + Time.unscaledDeltaTime) / 0.1f));
                yield return null;
            }
            for (float elapsed = 0f; elapsed < 0.15f; elapsed += Time.unscaledDeltaTime)
            {
                cardTransform.localScale = Vector3.Lerp(peak, restingScale,
                    Mathf.Clamp01((elapsed + Time.unscaledDeltaTime) / 0.15f));
                yield return null;
            }
            cardTransform.localScale = restingScale;
            punchRoutine = null;
        }

        public void Refresh()
        {
            int level = progressionSystem != null ? progressionSystem.CurrentLevel : 1;
            int power = progressionSystem != null ? progressionSystem.CurrentMiningPower : 0;
            if (titleText != null) titleText.text = MiningLocalization.Text(
                "MINER PROGRESS", "TIẾN TRÌNH THỢ MỎ");
            if (levelBadgeText != null) levelBadgeText.text = $"LV.{level}";
            if (rankTitleText != null) rankTitleText.text = string.Format(
                MiningLocalization.Text("Miner level {0}", "Thợ mỏ cấp {0}"), level);
            if (powerLabelText != null) powerLabelText.text = MiningLocalization.Text(
                "MINING POWER", "SỨC ĐÀO");
            if (powerValueText != null) powerValueText.text = power.ToString();
            if (rewardLabelText != null) rewardLabelText.text = MiningLocalization.Text(
                "NEXT ORE UNLOCK", "MỞ QUẶNG KẾ TIẾP");
            if (xpLabelText != null) xpLabelText.text = MiningLocalization.Text(
                "WORK XP", "KINH NGHIỆM");

            if (rewardText == null) return;
            if (progressionSystem == null || oreSpawner == null || oreSpawner.SpawnData == null)
            {
                rewardText.text = MiningLocalization.Text("Ore table unavailable",
                    "Chưa có bảng quặng");
                return;
            }

            OreData next = null;
            foreach (OreSpawnEntry entry in oreSpawner.SpawnData.OreSpawnTable)
            {
                OreData ore = entry?.Ore;
                // Match the actual unlock notifier: only configured, spawnable ores count.
                if (ore == null || ore.Prefab == null || ore.MiningPowerRequired <= power) continue;
                if (next == null || ore.MiningPowerRequired < next.MiningPowerRequired) next = ore;
            }
            rewardText.text = next == null
                ? MiningLocalization.Text("All configured ores unlocked", "Đã mở mọi quặng")
                : string.Format(MiningLocalization.Text("{0} • power {1}/{2}",
                        "{0} • sức đào {1}/{2}"),
                    MiningLocalization.Text(next.DisplayName), power, next.MiningPowerRequired);
        }

        private void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }

        // No AddXP() or gold reward: NpcProgressionSystem receives real mining XP;
        // MiningUnlockNotifier announces and guarantees the ore when power crosses its threshold.
    }
}
