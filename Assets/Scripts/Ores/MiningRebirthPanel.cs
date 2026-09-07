using System;
using Microlight.MicroBar;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Event-driven HUD and confirmation panel for rebirth progression.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningRebirthPanel : MonoBehaviour
    {
        [SerializeField] private MiningRebirthSystem rebirthSystem;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI progressLabel;
        [SerializeField] private TextMeshProUGUI boostLabel;
        [SerializeField] private TextMeshProUGUI warningLabel;
        [SerializeField] private TextMeshProUGUI nextBoostLabel;
        [SerializeField] private MicroBar progressBar;

        [Header("Editable Text")]
        [SerializeField] private string progressFormat = "{0:N0} / {1:N0} TIỀN";
        [SerializeField] private string boostFormat = "REBIRTH {0}  •  x{1:0.00} TIỀN";
        [SerializeField] private string warningText =
            "CẢNH BÁO!\n\nBạn sắp Rebirth! Toàn bộ tiền và mọi nâng cấp hiện tại sẽ bị xóa.";
        [SerializeField] private string nextBoostFormat = "Boost vĩnh viễn sau Rebirth: x{0:0.00} tiền";

        private bool barInitialized;
        private int initializedRequirement;

        public event Action PanelOpened;
        public event Action PanelClosed;

        private void Awake()
        {
            confirmationPanel?.SetActive(false);
        }

        private void OnEnable()
        {
            AddListeners();
            if (rebirthSystem != null)
            {
                rebirthSystem.StateChanged -= Refresh;
                rebirthSystem.StateChanged += Refresh;
                rebirthSystem.RebirthCompleted -= HandleRebirthCompleted;
                rebirthSystem.RebirthCompleted += HandleRebirthCompleted;
            }
            Refresh();
        }

        private void OnDisable()
        {
            RemoveListeners();
            if (rebirthSystem != null)
            {
                rebirthSystem.StateChanged -= Refresh;
                rebirthSystem.RebirthCompleted -= HandleRebirthCompleted;
            }
        }

        private void AddListeners()
        {
            openButton?.onClick.RemoveListener(OpenPanel);
            openButton?.onClick.AddListener(OpenPanel);
            confirmButton?.onClick.RemoveListener(ConfirmRebirth);
            confirmButton?.onClick.AddListener(ConfirmRebirth);
            cancelButton?.onClick.RemoveListener(ClosePanel);
            cancelButton?.onClick.AddListener(ClosePanel);
        }

        private void RemoveListeners()
        {
            openButton?.onClick.RemoveListener(OpenPanel);
            confirmButton?.onClick.RemoveListener(ConfirmRebirth);
            cancelButton?.onClick.RemoveListener(ClosePanel);
        }

        private void OpenPanel()
        {
            confirmationPanel?.SetActive(true);
            Refresh();
            PanelOpened?.Invoke();
        }

        private void ClosePanel()
        {
            confirmationPanel?.SetActive(false);
            PanelClosed?.Invoke();
        }

        private void ConfirmRebirth()
        {
            if (rebirthSystem != null && rebirthSystem.TryRebirth())
            {
                ClosePanel();
            }
            else
            {
                Refresh();
            }
        }

        private void HandleRebirthCompleted(int count)
        {
            Refresh();
        }

        private void Refresh()
        {
            int money = wallet != null ? wallet.CurrentMoney : 0;
            int requirement = rebirthSystem != null ? rebirthSystem.CurrentRequirement : 1;

            if (progressLabel != null)
            {
                progressLabel.text = string.Format(progressFormat, money, requirement);
            }
            if (boostLabel != null)
            {
                int count = rebirthSystem != null ? rebirthSystem.CompletedRebirths : 0;
                float multiplier = rebirthSystem != null ? rebirthSystem.PermanentMoneyMultiplier : 1f;
                boostLabel.text = string.Format(boostFormat, count, multiplier);
            }
            if (warningLabel != null)
            {
                warningLabel.text = warningText;
            }
            if (nextBoostLabel != null)
            {
                float nextMultiplier = rebirthSystem != null ? rebirthSystem.NextMoneyMultiplier : 1f;
                nextBoostLabel.text = string.Format(nextBoostFormat, nextMultiplier);
            }
            if (confirmButton != null)
            {
                confirmButton.interactable = rebirthSystem != null && rebirthSystem.CanRebirth;
            }

            RefreshProgressBar(money, requirement);
        }

        private void RefreshProgressBar(int current, int maximum)
        {
            if (progressBar == null)
            {
                return;
            }

            int safeMaximum = Mathf.Max(1, maximum);
            if (!barInitialized)
            {
                progressBar.Initialize(safeMaximum);
                initializedRequirement = safeMaximum;
                barInitialized = true;
            }
            else if (initializedRequirement != safeMaximum)
            {
                initializedRequirement = safeMaximum;
                progressBar.SetNewMaxHP(safeMaximum, true);
            }

            progressBar.UpdateBar(Mathf.Clamp(current, 0, safeMaximum));
        }
    }
}
