using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Owns saved daily/weekly quest progress and reward claiming.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningQuestSystem : MonoBehaviour
    {
        [Serializable]
        private sealed class SavedQuestEntry
        {
            public string questId;
            public int progress;
            public bool claimed;
        }

        [Serializable]
        private sealed class SavedQuestState
        {
            public int version = 1;
            public string dailyStamp;
            public string weeklyStamp;
            public List<SavedQuestEntry> entries = new();
        }

        private sealed class RuntimeQuestState
        {
            public int Progress;
            public bool Claimed;
        }

        [Header("References")]
        [SerializeField] private MiningQuestData data;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private OreSpawner oreSpawner;
        [SerializeField] private NpcShop npcShop;
        [SerializeField] private MiningRebirthSystem rebirthSystem;

        private readonly Dictionary<string, RuntimeQuestState> states = new();
        private string dailyStamp;
        private string weeklyStamp;
        private bool initialized;
        private bool dirty;

        public MiningQuestData Data => data;
        public event Action QuestsChanged;
        public event Action<MiningQuestDefinition, float> RewardClaimed;

        private void Awake()
        {
            ResolveReferences();
            InitializeState();
        }

        private void OnEnable()
        {
            ResolveReferences();
            InitializeState();
            Subscribe();
            RefreshPeriods();
        }

        private void OnDisable()
        {
            Unsubscribe();
            SaveState(true);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveState(true);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) RefreshPeriods();
        }

        private void OnApplicationQuit()
        {
            SaveState(true);
        }

        public int GetProgress(string questId)
        {
            RefreshPeriods();
            return states.TryGetValue(questId, out RuntimeQuestState state)
                ? state.Progress
                : 0;
        }

        public bool IsClaimed(string questId)
        {
            RefreshPeriods();
            return states.TryGetValue(questId, out RuntimeQuestState state) && state.Claimed;
        }

        public bool IsComplete(string questId)
        {
            MiningQuestDefinition definition = data != null ? data.GetDefinition(questId) : null;
            return definition != null && GetProgress(questId) >= definition.TargetAmount;
        }

        public bool TryClaimReward(string questId, out float rewardAmount)
        {
            rewardAmount = 0f;
            RefreshPeriods();
            MiningQuestDefinition definition = data != null ? data.GetDefinition(questId) : null;
            if (definition == null || wallet == null ||
                !states.TryGetValue(questId, out RuntimeQuestState state) ||
                state.Claimed || state.Progress < definition.TargetAmount)
            {
                return false;
            }

            rewardAmount = definition.RollReward();
            if (rewardAmount <= 0f)
            {
                return false;
            }

            state.Claimed = true;
            wallet.AddMoney(rewardAmount);
            dirty = true;
            SaveState(true);
            RewardClaimed?.Invoke(definition, rewardAmount);
            QuestsChanged?.Invoke();
            return true;
        }

        public void RefreshPeriods()
        {
            InitializeState();
            string currentDaily = GetDailyStamp(DateTime.Now);
            string currentWeekly = GetWeeklyStamp(DateTime.Now);
            bool dailyChanged = !string.Equals(dailyStamp, currentDaily, StringComparison.Ordinal);
            bool weeklyChanged = !string.Equals(weeklyStamp, currentWeekly,
                StringComparison.Ordinal);
            if (!dailyChanged && !weeklyChanged)
            {
                return;
            }

            dailyStamp = currentDaily;
            weeklyStamp = currentWeekly;
            if (data != null)
            {
                foreach (MiningQuestDefinition quest in data.Quests)
                {
                    if (quest == null || !states.TryGetValue(quest.QuestId,
                            out RuntimeQuestState state))
                    {
                        continue;
                    }
                    if ((dailyChanged && quest.Period == MiningQuestPeriod.Daily) ||
                        (weeklyChanged && quest.Period == MiningQuestPeriod.Weekly))
                    {
                        state.Progress = 0;
                        state.Claimed = false;
                    }
                }
            }

            dirty = true;
            SaveState(true);
            QuestsChanged?.Invoke();
        }

        public TimeSpan GetTimeUntilReset(MiningQuestPeriod period)
        {
            DateTime now = DateTime.Now;
            DateTime reset = period == MiningQuestPeriod.Daily
                ? now.Date.AddDays(1)
                : StartOfWeek(now).AddDays(7);
            return reset > now ? reset - now : TimeSpan.Zero;
        }

        /// <summary>Only called by the Settings full-data reset.</summary>
        public void ResetAllData()
        {
            if (data != null) PlayerPrefs.DeleteKey(data.SaveKey);
            states.Clear();
            initialized = false;
            dirty = false;
            InitializeState();
            SaveState(true);
            QuestsChanged?.Invoke();
        }

        private void HandleOreRewardGranted(Ore ore, float reward)
        {
            if (ore != null) AddProgress(MiningQuestObjective.MineOre, 1);
        }

        private void HandleNpcPurchased(MiningNpc npc)
        {
            if (npc != null) AddProgress(MiningQuestObjective.PurchaseNpc, 1);
        }

        private void HandleRebirthCompleted(int completedCount)
        {
            AddProgress(MiningQuestObjective.Rebirth, 1);
        }

        private void AddProgress(MiningQuestObjective objective, int amount)
        {
            if (amount <= 0 || data == null) return;
            RefreshPeriods();
            bool changed = false;
            foreach (MiningQuestDefinition quest in data.Quests)
            {
                if (quest == null || !quest.IsValid || quest.Objective != objective ||
                    !states.TryGetValue(quest.QuestId, out RuntimeQuestState state) ||
                    state.Claimed || state.Progress >= quest.TargetAmount)
                {
                    continue;
                }

                state.Progress = Mathf.Min(quest.TargetAmount, state.Progress + amount);
                changed = true;
            }
            if (!changed) return;
            dirty = true;
            SaveState(false);
            QuestsChanged?.Invoke();
        }

        private void InitializeState()
        {
            if (initialized) return;
            initialized = true;
            dailyStamp = GetDailyStamp(DateTime.Now);
            weeklyStamp = GetWeeklyStamp(DateTime.Now);
            LoadState();
            EnsureDefinitionStates();
        }

        private void LoadState()
        {
            if (data == null || !PlayerPrefs.HasKey(data.SaveKey)) return;
            string json = PlayerPrefs.GetString(data.SaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json)) return;

            SavedQuestState saved;
            try
            {
                saved = JsonUtility.FromJson<SavedQuestState>(json);
            }
            catch (Exception)
            {
                saved = null;
            }
            if (saved == null) return;

            dailyStamp = string.IsNullOrWhiteSpace(saved.dailyStamp)
                ? dailyStamp
                : saved.dailyStamp;
            weeklyStamp = string.IsNullOrWhiteSpace(saved.weeklyStamp)
                ? weeklyStamp
                : saved.weeklyStamp;
            if (saved.entries == null) return;
            foreach (SavedQuestEntry entry in saved.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.questId)) continue;
                states[entry.questId] = new RuntimeQuestState
                {
                    Progress = Mathf.Max(0, entry.progress),
                    Claimed = entry.claimed
                };
            }
        }

        private void EnsureDefinitionStates()
        {
            if (data == null) return;
            foreach (MiningQuestDefinition quest in data.Quests)
            {
                if (quest == null || !quest.IsValid) continue;
                if (!states.ContainsKey(quest.QuestId))
                {
                    states.Add(quest.QuestId, new RuntimeQuestState());
                }
            }
        }

        private void SaveState(bool flush)
        {
            if (data == null || (!dirty && !flush)) return;
            var saved = new SavedQuestState
            {
                dailyStamp = dailyStamp,
                weeklyStamp = weeklyStamp
            };
            foreach (MiningQuestDefinition quest in data.Quests)
            {
                if (quest == null || !states.TryGetValue(quest.QuestId,
                        out RuntimeQuestState state))
                {
                    continue;
                }
                saved.entries.Add(new SavedQuestEntry
                {
                    questId = quest.QuestId,
                    progress = state.Progress,
                    claimed = state.Claimed
                });
            }
            PlayerPrefs.SetString(data.SaveKey, JsonUtility.ToJson(saved));
            if (flush) PlayerPrefs.Save();
            dirty = false;
        }

        private void Subscribe()
        {
            Unsubscribe();
            if (oreSpawner != null) oreSpawner.OreRewardGranted += HandleOreRewardGranted;
            if (npcShop != null) npcShop.NpcPurchased += HandleNpcPurchased;
            if (rebirthSystem != null) rebirthSystem.RebirthCompleted += HandleRebirthCompleted;
        }

        private void Unsubscribe()
        {
            if (oreSpawner != null) oreSpawner.OreRewardGranted -= HandleOreRewardGranted;
            if (npcShop != null) npcShop.NpcPurchased -= HandleNpcPurchased;
            if (rebirthSystem != null) rebirthSystem.RebirthCompleted -= HandleRebirthCompleted;
        }

        private void ResolveReferences()
        {
            wallet ??= FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            oreSpawner ??= FindFirstObjectByType<OreSpawner>(FindObjectsInactive.Include);
            npcShop ??= FindFirstObjectByType<NpcShop>(FindObjectsInactive.Include);
            rebirthSystem ??= FindFirstObjectByType<MiningRebirthSystem>(
                FindObjectsInactive.Include);
        }

        private static string GetDailyStamp(DateTime time)
        {
            return time.Date.ToString("yyyyMMdd");
        }

        private static string GetWeeklyStamp(DateTime time)
        {
            return StartOfWeek(time).ToString("yyyyMMdd");
        }

        private static DateTime StartOfWeek(DateTime time)
        {
            int daysSinceMonday = ((int)time.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return time.Date.AddDays(-daysSinceMonday);
        }
    }
}
