using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Tracks persistent achievement progress and applies cumulative permanent rewards.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningAchievementSystem : MonoBehaviour
    {
        [Serializable]
        private sealed class SavedAchievementProgress
        {
            public string achievementId;
            public long progress;
            public bool completed;
        }

        [Serializable]
        private sealed class SavedAchievementData
        {
            public int version = 1;
            public List<SavedAchievementProgress> entries = new();
        }

        private sealed class RuntimeProgress
        {
            public long Progress;
            public bool Completed;
        }

        [Header("References")]
        [SerializeField] private MiningAchievementData achievementData;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private OreSpawner oreSpawner;
        [SerializeField] private NpcShop npcShop;
        [SerializeField] private NpcProgressionSystem progressionSystem;
        [SerializeField] private MiningRebirthSystem rebirthSystem;
        [SerializeField] private MiningUpgradeSystem upgradeSystem;
        [SerializeField] private MiningUnlockNotifier unlockNotifier;

        private readonly Dictionary<string, RuntimeProgress> progressById = new();
        private bool initialized;

        public MiningAchievementData AchievementData => achievementData;
        public event Action ProgressChanged;
        public event Action<MiningAchievementDefinition> AchievementUnlocked;

        private void Awake()
        {
            ResolveReferences();
            InitializeState();
        }

        private void OnEnable()
        {
            ResolveReferences();
            InitializeState();

            if (oreSpawner != null)
            {
                oreSpawner.OreRewardGranted -= HandleOreRewardGranted;
                oreSpawner.OreRewardGranted += HandleOreRewardGranted;
            }
            if (wallet != null)
            {
                wallet.MoneySpent -= HandleMoneySpent;
                wallet.MoneySpent += HandleMoneySpent;
            }
            if (npcShop != null)
            {
                npcShop.NpcCountChanged -= HandleNpcCountChanged;
                npcShop.NpcCountChanged += HandleNpcCountChanged;
            }
            if (progressionSystem != null)
            {
                progressionSystem.LevelChanged -= HandleLevelChanged;
                progressionSystem.LevelChanged += HandleLevelChanged;
            }
            if (rebirthSystem != null)
            {
                rebirthSystem.RebirthCompleted -= HandleRebirthCompleted;
                rebirthSystem.RebirthCompleted += HandleRebirthCompleted;
            }

            RefreshCurrentStateGoals(true);
        }

        private void Start()
        {
            // Re-check after every Scene component has completed Awake. This preserves a
            // previously earned first-Rebirth achievement regardless of script execution order.
            RefreshCurrentStateGoals(true);
        }

        private void OnDisable()
        {
            if (oreSpawner != null)
            {
                oreSpawner.OreRewardGranted -= HandleOreRewardGranted;
            }
            if (wallet != null)
            {
                wallet.MoneySpent -= HandleMoneySpent;
            }
            if (npcShop != null)
            {
                npcShop.NpcCountChanged -= HandleNpcCountChanged;
            }
            if (progressionSystem != null)
            {
                progressionSystem.LevelChanged -= HandleLevelChanged;
            }
            if (rebirthSystem != null)
            {
                rebirthSystem.RebirthCompleted -= HandleRebirthCompleted;
            }
        }

        public long GetProgress(string achievementId)
        {
            return progressById.TryGetValue(achievementId, out RuntimeProgress state)
                ? state.Progress
                : 0;
        }

        public bool IsCompleted(string achievementId)
        {
            return progressById.TryGetValue(achievementId, out RuntimeProgress state) &&
                   state.Completed;
        }

        /// <summary>Clears achievements only during the Settings full-data reset.</summary>
        public void ResetAllData()
        {
            if (achievementData != null && !string.IsNullOrWhiteSpace(achievementData.SaveKey))
            {
                PlayerPrefs.DeleteKey(achievementData.SaveKey);
            }

            progressById.Clear();
            initialized = false;
            InitializeState();
            ApplyPermanentRewards();
            PlayerPrefs.Save();
            ProgressChanged?.Invoke();
        }

        private void HandleOreRewardGranted(Ore ore, float reward)
        {
            if (ore == null || ore.Data == null || achievementData == null)
            {
                return;
            }

            bool changed = false;
            foreach (MiningAchievementDefinition achievement in achievementData.Achievements)
            {
                if (achievement == null || achievement.Trigger != MiningAchievementTrigger.MinedOreKind ||
                    achievement.OreKind != ore.Data.Kind || IsCompleted(achievement.AchievementId))
                {
                    continue;
                }

                changed |= AddProgress(achievement, 1, true);
            }

            if (changed)
            {
                SaveProgress();
                ProgressChanged?.Invoke();
            }
        }

        private void HandleNpcCountChanged(int count)
        {
            bool changed = SetGoalProgress(MiningAchievementTrigger.ConcurrentNpcCount,
                Math.Max(0, count), true);
            if (changed)
            {
                SaveProgress();
                ProgressChanged?.Invoke();
            }
        }

        private void HandleMoneySpent(float amount)
        {
            if (achievementData == null || amount < 1f)
            {
                return;
            }

            long spent = amount >= long.MaxValue
                ? long.MaxValue
                : (long)Math.Floor(amount);
            bool changed = false;
            foreach (MiningAchievementDefinition achievement in achievementData.Achievements)
            {
                if (achievement == null ||
                    achievement.Trigger != MiningAchievementTrigger.MoneySpent ||
                    IsCompleted(achievement.AchievementId))
                {
                    continue;
                }

                changed |= AddProgress(achievement, spent, true);
            }

            if (changed)
            {
                SaveProgress();
                ProgressChanged?.Invoke();
            }
        }

        private void HandleLevelChanged(int level)
        {
            RefreshOreUnlockGoals(true);
        }

        private void HandleRebirthCompleted(int count)
        {
            bool changed = SetGoalProgress(MiningAchievementTrigger.RebirthCount,
                Math.Max(0, count), true);
            if (changed)
            {
                SaveProgress();
                ProgressChanged?.Invoke();
            }
        }

        private void RefreshCurrentStateGoals(bool announceUnlocks)
        {
            if (achievementData == null)
            {
                return;
            }

            bool changed = false;
            changed |= SetGoalProgress(MiningAchievementTrigger.ConcurrentNpcCount,
                npcShop != null ? npcShop.PurchasedCount : 0, announceUnlocks);
            changed |= SetGoalProgress(MiningAchievementTrigger.RebirthCount,
                rebirthSystem != null ? rebirthSystem.CompletedRebirths : 0, announceUnlocks);
            changed |= RefreshOreUnlockGoals(announceUnlocks, false);
            if (changed)
            {
                SaveProgress();
                ProgressChanged?.Invoke();
            }
        }

        private void RefreshOreUnlockGoals(bool announceUnlocks)
        {
            if (RefreshOreUnlockGoals(announceUnlocks, true))
            {
                SaveProgress();
                ProgressChanged?.Invoke();
            }
        }

        private bool RefreshOreUnlockGoals(bool announceUnlocks, bool applyImmediately)
        {
            if (achievementData == null)
            {
                return false;
            }

            bool changed = false;
            foreach (MiningAchievementDefinition achievement in achievementData.Achievements)
            {
                if (achievement == null ||
                    achievement.Trigger != MiningAchievementTrigger.AllConfiguredOresUnlocked ||
                    IsCompleted(achievement.AchievementId))
                {
                    continue;
                }

                long unlockedCount = CountUnlockedRequiredOres(achievement);
                changed |= SetProgress(achievement, unlockedCount, announceUnlocks);
            }

            if (changed && applyImmediately)
            {
                ApplyPermanentRewards();
            }
            return changed;
        }

        private long CountUnlockedRequiredOres(MiningAchievementDefinition achievement)
        {
            if (progressionSystem == null)
            {
                return 0;
            }

            int power = progressionSystem.CurrentMiningPower;
            var unlockedKinds = new HashSet<OreKind>();
            foreach (OreData ore in achievement.RequiredOres)
            {
                if (ore != null && ore.MiningPowerRequired <= power)
                {
                    unlockedKinds.Add(ore.Kind);
                }
            }
            return unlockedKinds.Count;
        }

        private bool SetGoalProgress(MiningAchievementTrigger trigger, long value,
            bool announceUnlocks)
        {
            if (achievementData == null)
            {
                return false;
            }

            bool changed = false;
            foreach (MiningAchievementDefinition achievement in achievementData.Achievements)
            {
                if (achievement != null && achievement.Trigger == trigger &&
                    !IsCompleted(achievement.AchievementId))
                {
                    changed |= SetProgress(achievement, value, announceUnlocks);
                }
            }

            if (changed)
            {
                ApplyPermanentRewards();
            }
            return changed;
        }

        private bool AddProgress(MiningAchievementDefinition achievement, long amount,
            bool announceUnlock)
        {
            RuntimeProgress state = GetOrCreateState(achievement.AchievementId);
            long next = amount > 0 && state.Progress > long.MaxValue - amount
                ? long.MaxValue
                : Math.Max(0, state.Progress + amount);
            return SetProgress(achievement, next, announceUnlock);
        }

        private bool SetProgress(MiningAchievementDefinition achievement, long value,
            bool announceUnlock)
        {
            if (achievement == null || string.IsNullOrWhiteSpace(achievement.AchievementId))
            {
                return false;
            }

            RuntimeProgress state = GetOrCreateState(achievement.AchievementId);
            if (state.Completed)
            {
                return false;
            }

            long clamped = Math.Max(0, Math.Min(value, achievement.TargetAmount));
            bool progressChanged = state.Progress != clamped;
            state.Progress = clamped;
            if (state.Progress < achievement.TargetAmount)
            {
                return progressChanged;
            }

            state.Completed = true;
            ApplyPermanentRewards();
            if (announceUnlock)
            {
                AnnounceAchievement(achievement);
            }
            AchievementUnlocked?.Invoke(achievement);
            return true;
        }

        private void AnnounceAchievement(MiningAchievementDefinition achievement)
        {
            if (unlockNotifier == null)
            {
                return;
            }

            string reward = BuildRewardText(achievement);
            string message = string.Format(MiningLocalization.Text(
                    "ACHIEVEMENT: {0} • Permanent {1}",
                    "THÀNH TỰU: {0} • Vĩnh viễn {1}"),
                achievement.Title, reward);
            unlockNotifier.ShowToast(message);
        }

        private static string BuildRewardText(MiningAchievementDefinition achievement)
        {
            string money = achievement.MoneyMultiplierPercent > 0f
                ? $"+{achievement.MoneyMultiplierPercent:0.##}% {MiningLocalization.Text("money", "tiền")}"
                : string.Empty;
            string experience = achievement.ExperienceMultiplierPercent > 0f
                ? $"+{achievement.ExperienceMultiplierPercent:0.##}% XP"
                : string.Empty;
            if (!string.IsNullOrEmpty(money) && !string.IsNullOrEmpty(experience))
            {
                return $"{money}, {experience}";
            }
            return !string.IsNullOrEmpty(money) ? money : experience;
        }

        private void InitializeState()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            progressById.Clear();
            if (achievementData == null)
            {
                ApplyPermanentRewards();
                return;
            }

            foreach (MiningAchievementDefinition achievement in achievementData.Achievements)
            {
                if (achievement != null && !string.IsNullOrWhiteSpace(achievement.AchievementId))
                {
                    progressById[achievement.AchievementId] = new RuntimeProgress();
                }
            }

            if (!string.IsNullOrWhiteSpace(achievementData.SaveKey) &&
                PlayerPrefs.HasKey(achievementData.SaveKey))
            {
                try
                {
                    SavedAchievementData save = JsonUtility.FromJson<SavedAchievementData>(
                        PlayerPrefs.GetString(achievementData.SaveKey));
                    if (save?.entries != null)
                    {
                        foreach (SavedAchievementProgress entry in save.entries)
                        {
                            if (entry != null && progressById.TryGetValue(entry.achievementId,
                                out RuntimeProgress state))
                            {
                                MiningAchievementDefinition definition =
                                    achievementData.FindById(entry.achievementId);
                                state.Progress = definition != null
                                    ? Math.Max(0, Math.Min(entry.progress, definition.TargetAmount))
                                    : 0;
                                state.Completed = entry.completed;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Achievement save could not be loaded: {exception.Message}", this);
                }
            }

            ApplyPermanentRewards();
        }

        private RuntimeProgress GetOrCreateState(string achievementId)
        {
            if (!progressById.TryGetValue(achievementId, out RuntimeProgress state))
            {
                state = new RuntimeProgress();
                progressById[achievementId] = state;
            }
            return state;
        }

        private void SaveProgress()
        {
            if (achievementData == null || string.IsNullOrWhiteSpace(achievementData.SaveKey))
            {
                return;
            }

            var save = new SavedAchievementData();
            foreach (MiningAchievementDefinition achievement in achievementData.Achievements)
            {
                if (achievement == null || string.IsNullOrWhiteSpace(achievement.AchievementId))
                {
                    continue;
                }

                RuntimeProgress state = GetOrCreateState(achievement.AchievementId);
                save.entries.Add(new SavedAchievementProgress
                {
                    achievementId = achievement.AchievementId,
                    progress = state.Progress,
                    completed = state.Completed
                });
            }

            PlayerPrefs.SetString(achievementData.SaveKey, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
        }

        private void ApplyPermanentRewards()
        {
            float moneyPercent = 0f;
            float experiencePercent = 0f;
            if (achievementData != null)
            {
                foreach (MiningAchievementDefinition achievement in achievementData.Achievements)
                {
                    if (achievement != null && IsCompleted(achievement.AchievementId))
                    {
                        moneyPercent += achievement.MoneyMultiplierPercent;
                        experiencePercent += achievement.ExperienceMultiplierPercent;
                    }
                }
            }

            upgradeSystem?.SetAchievementRewardMultipliers(
                1f + Mathf.Max(0f, moneyPercent) * 0.01f,
                1f + Mathf.Max(0f, experiencePercent) * 0.01f);
        }

        private void ResolveReferences()
        {
            if (wallet == null)
            {
                wallet = FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            }
            if (oreSpawner == null)
            {
                oreSpawner = FindFirstObjectByType<OreSpawner>(FindObjectsInactive.Include);
            }
            if (npcShop == null)
            {
                npcShop = FindFirstObjectByType<NpcShop>(FindObjectsInactive.Include);
            }
            if (progressionSystem == null)
            {
                progressionSystem = FindFirstObjectByType<NpcProgressionSystem>(
                    FindObjectsInactive.Include);
            }
            if (rebirthSystem == null)
            {
                rebirthSystem = FindFirstObjectByType<MiningRebirthSystem>(
                    FindObjectsInactive.Include);
            }
            if (upgradeSystem == null)
            {
                upgradeSystem = FindFirstObjectByType<MiningUpgradeSystem>(
                    FindObjectsInactive.Include);
            }
            if (unlockNotifier == null)
            {
                unlockNotifier = GetComponent<MiningUnlockNotifier>() ??
                                 FindFirstObjectByType<MiningUnlockNotifier>(
                                     FindObjectsInactive.Include);
            }
        }
    }
}
