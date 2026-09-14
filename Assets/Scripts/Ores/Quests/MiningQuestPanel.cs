using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Authored UI shared by the startup menu and gameplay HUD.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningQuestPanel : MonoBehaviour
    {
        [Serializable]
        private sealed class QuestRowView
        {
            [SerializeField] private string questId;
            [SerializeField] private GameObject root;
            [SerializeField] private TextMeshProUGUI periodLabel;
            [SerializeField] private TextMeshProUGUI nameLabel;
            [SerializeField] private TextMeshProUGUI progressLabel;
            [SerializeField] private TextMeshProUGUI rewardLabel;
            [SerializeField] private Image progressFill;
            [SerializeField] private Button claimButton;
            [SerializeField] private TextMeshProUGUI claimLabel;

            private UnityAction claimAction;

            public string QuestId => questId;
            public Button ClaimButton => claimButton;

            public void Bind(Action<string> claim)
            {
                Unbind();
                if (claimButton == null) return;
                claimAction = () => claim(questId);
                claimButton.onClick.AddListener(claimAction);
            }

            public void Unbind()
            {
                if (claimButton != null && claimAction != null)
                {
                    claimButton.onClick.RemoveListener(claimAction);
                }
                claimAction = null;
            }

            public void Refresh(MiningQuestDefinition definition, MiningQuestSystem system)
            {
                bool valid = definition != null && system != null;
                root?.SetActive(valid);
                if (!valid) return;

                int progress = system.GetProgress(questId);
                bool complete = progress >= definition.TargetAmount;
                bool claimed = system.IsClaimed(questId);
                if (periodLabel != null)
                {
                    periodLabel.text = definition.Period == MiningQuestPeriod.Daily
                        ? MiningLocalization.Text("DAILY", "HÀNG NGÀY")
                        : MiningLocalization.Text("WEEKLY", "HÀNG TUẦN");
                }
                if (nameLabel != null) nameLabel.text = definition.GetLocalizedName();
                if (progressLabel != null)
                {
                    progressLabel.text = $"{Mathf.Min(progress, definition.TargetAmount)} / " +
                                         definition.TargetAmount;
                }
                if (rewardLabel != null)
                {
                    rewardLabel.text = MiningLocalization.Text("REWARD: ", "THƯỞNG: ") +
                                       definition.GetRewardPreview();
                }
                if (progressFill != null)
                {
                    progressFill.fillAmount = Mathf.Clamp01((float)progress /
                                                            definition.TargetAmount);
                }
                if (claimButton != null) claimButton.interactable = complete && !claimed;
                if (claimLabel != null)
                {
                    claimLabel.text = claimed
                        ? MiningLocalization.Text("CLAIMED", "ĐÃ NHẬN")
                        : complete
                            ? MiningLocalization.Text("CLAIM", "NHẬN")
                            : MiningLocalization.Text("IN PROGRESS", "ĐANG LÀM");
                }
            }
        }

        [Header("Systems")]
        [SerializeField] private MiningQuestData data;
        [SerializeField] private MiningQuestSystem questSystem;
        [SerializeField] private MiningUiPanelCoordinator panelCoordinator;

        [Header("Authored UI")]
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private Button gameplayOpenButton;
        [SerializeField] private Button mainMenuOpenButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI resetTimerLabel;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private List<QuestRowView> rows = new();

        private float nextTimerRefresh;
        private GameObject lastOpener;

        private void Awake()
        {
            panelCoordinator?.RegisterQuestUi(gameplayOpenButton != null
                ? gameplayOpenButton.transform as RectTransform
                : null);
            panelRoot?.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            AddListeners();
            if (questSystem != null)
            {
                questSystem.QuestsChanged -= Refresh;
                questSystem.QuestsChanged += Refresh;
            }
            MiningLocalization.LanguageChanged -= Refresh;
            MiningLocalization.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            RemoveListeners();
            if (questSystem != null) questSystem.QuestsChanged -= Refresh;
            MiningLocalization.LanguageChanged -= Refresh;
        }

        private void Update()
        {
            if (panelRoot == null || !panelRoot.gameObject.activeInHierarchy ||
                Time.unscaledTime < nextTimerRefresh)
            {
                return;
            }
            nextTimerRefresh = Time.unscaledTime + 1f;
            questSystem?.RefreshPeriods();
            RefreshTimer();
        }

        public void Open()
        {
            if (panelRoot == null) return;
            if (statusLabel != null) statusLabel.text = string.Empty;
            panelRoot.SetAsLastSibling();
            if (panelCoordinator != null) panelCoordinator.OpenPanel(panelRoot);
            else panelRoot.gameObject.SetActive(true);
            Refresh();
            EventSystem.current?.SetSelectedGameObject(GetFirstClaimableButton() ??
                                                       closeButton?.gameObject);
        }

        public void Close()
        {
            if (panelCoordinator != null) panelCoordinator.ClosePanel(panelRoot);
            else panelRoot?.gameObject.SetActive(false);
            EventSystem.current?.SetSelectedGameObject(lastOpener);
        }

        private void OpenFromGameplay()
        {
            lastOpener = gameplayOpenButton != null ? gameplayOpenButton.gameObject : null;
            Open();
        }

        private void OpenFromMainMenu()
        {
            lastOpener = mainMenuOpenButton != null ? mainMenuOpenButton.gameObject : null;
            Open();
        }

        private void Claim(string questId)
        {
            if (questSystem != null && questSystem.TryClaimReward(questId, out float amount))
            {
                if (statusLabel != null)
                {
                    statusLabel.text = string.Format(MiningLocalization.Text(
                        "+{0} MONEY RECEIVED!", "+{0} TIỀN ĐÃ NHẬN!"),
                        MiningMoneyFormatter.Format(amount));
                }
            }
            RefreshRows();
        }

        private void Refresh()
        {
            if (titleLabel != null)
            {
                titleLabel.text = MiningLocalization.Text("DAILY & WEEKLY QUESTS",
                    "NHIỆM VỤ NGÀY & TUẦN");
            }
            if (gameplayOpenButton != null)
            {
                TextMeshProUGUI label = gameplayOpenButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = MiningLocalization.Text("QUESTS", "NHIỆM VỤ");
            }
            if (mainMenuOpenButton != null)
            {
                TextMeshProUGUI label = mainMenuOpenButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = MiningLocalization.Text("QUESTS", "NHIỆM VỤ");
            }
            RefreshTimer();
            RefreshRows();
        }

        private void RefreshRows()
        {
            foreach (QuestRowView row in rows)
            {
                MiningQuestDefinition definition = data != null
                    ? data.GetDefinition(row.QuestId)
                    : null;
                row.Refresh(definition, questSystem);
            }
        }

        private void RefreshTimer()
        {
            if (resetTimerLabel == null || questSystem == null) return;
            TimeSpan daily = questSystem.GetTimeUntilReset(MiningQuestPeriod.Daily);
            TimeSpan weekly = questSystem.GetTimeUntilReset(MiningQuestPeriod.Weekly);
            resetTimerLabel.text = string.Format(MiningLocalization.Text(
                    "DAILY RESET {0}  •  WEEKLY RESET {1}",
                    "RESET NGÀY {0}  •  RESET TUẦN {1}"),
                FormatDuration(daily), FormatDuration(weekly));
        }

        private void AddListeners()
        {
            RemoveListeners();
            gameplayOpenButton?.onClick.AddListener(OpenFromGameplay);
            mainMenuOpenButton?.onClick.AddListener(OpenFromMainMenu);
            closeButton?.onClick.AddListener(Close);
            foreach (QuestRowView row in rows) row.Bind(Claim);
        }

        private void RemoveListeners()
        {
            gameplayOpenButton?.onClick.RemoveListener(OpenFromGameplay);
            mainMenuOpenButton?.onClick.RemoveListener(OpenFromMainMenu);
            closeButton?.onClick.RemoveListener(Close);
            foreach (QuestRowView row in rows) row.Unbind();
        }

        private GameObject GetFirstClaimableButton()
        {
            foreach (QuestRowView row in rows)
            {
                if (row.ClaimButton != null && row.ClaimButton.interactable)
                {
                    return row.ClaimButton.gameObject;
                }
            }
            return null;
        }

        private static string FormatDuration(TimeSpan duration)
        {
            int totalHours = Mathf.Max(0, (int)duration.TotalHours);
            return $"{totalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
        }
    }
}
