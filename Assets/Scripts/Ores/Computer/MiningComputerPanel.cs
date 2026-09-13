using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Scene-authored information and upgrade panel for the purchased coin computer.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningComputerPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private MiningUiPanelCoordinator panelCoordinator;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI levelLabel;
        [SerializeField] private TextMeshProUGUI incomeLabel;
        [SerializeField] private TextMeshProUGUI nextIncomeLabel;
        [SerializeField] private TextMeshProUGUI costLabel;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private TextMeshProUGUI upgradeButtonLabel;

        private MiningComputerStation station;
        private PlayerWallet wallet;

        public void Configure(RectTransform root, MiningUiPanelCoordinator coordinator,
            Button close, Button upgrade, TextMeshProUGUI title, TextMeshProUGUI level,
            TextMeshProUGUI income, TextMeshProUGUI nextIncome, TextMeshProUGUI cost,
            TextMeshProUGUI status, TextMeshProUGUI upgradeText)
        {
            panelRoot = root;
            panelCoordinator = coordinator;
            closeButton = close;
            upgradeButton = upgrade;
            titleLabel = title;
            levelLabel = level;
            incomeLabel = income;
            nextIncomeLabel = nextIncome;
            costLabel = cost;
            statusLabel = status;
            upgradeButtonLabel = upgradeText;
        }

        private void Awake()
        {
            panelRoot ??= transform as RectTransform;
            panelRoot?.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            closeButton?.onClick.RemoveListener(Close);
            closeButton?.onClick.AddListener(Close);
            upgradeButton?.onClick.RemoveListener(Upgrade);
            upgradeButton?.onClick.AddListener(Upgrade);
            MiningLocalization.LanguageChanged -= Refresh;
            MiningLocalization.LanguageChanged += Refresh;
            SubscribeStation();
            Refresh();
        }

        private void OnDisable()
        {
            closeButton?.onClick.RemoveListener(Close);
            upgradeButton?.onClick.RemoveListener(Upgrade);
            MiningLocalization.LanguageChanged -= Refresh;
            UnsubscribeStation();
        }

        public void Show(MiningComputerStation targetStation)
        {
            if (targetStation == null || !targetStation.IsPurchased)
            {
                return;
            }

            UnsubscribeStation();
            station = targetStation;
            wallet = FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            if (panelCoordinator == null)
            {
                panelCoordinator = FindFirstObjectByType<MiningUiPanelCoordinator>(
                    FindObjectsInactive.Include);
            }

            panelRoot ??= transform as RectTransform;
            if (panelCoordinator != null)
            {
                panelCoordinator.OpenPanel(panelRoot);
            }
            else
            {
                panelRoot?.gameObject.SetActive(true);
            }
            SubscribeStation();
            Refresh();
        }

        public void Close()
        {
            if (panelCoordinator != null)
            {
                panelCoordinator.ClosePanel(panelRoot);
            }
            else
            {
                panelRoot?.gameObject.SetActive(false);
            }
        }

        private void Upgrade()
        {
            station?.TryUpgrade();
            Refresh();
        }

        private void SubscribeStation()
        {
            if (station != null)
            {
                station.StateChanged -= Refresh;
                station.StateChanged += Refresh;
            }
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
                wallet.MoneyChanged += HandleMoneyChanged;
            }
        }

        private void UnsubscribeStation()
        {
            if (station != null)
            {
                station.StateChanged -= Refresh;
            }
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
            }
        }

        private void HandleMoneyChanged(float money)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (station == null)
            {
                return;
            }

            if (titleLabel != null)
                titleLabel.text = MiningLocalization.Text("COIN COMPUTER", "MÁY TẠO TIỀN");
            if (levelLabel != null)
                levelLabel.text = string.Format(MiningLocalization.Text(
                    "Level: {0}/{1}", "Cấp: {0}/{1}"), station.CurrentLevel,
                    station.MaximumLevel);
            if (incomeLabel != null)
                incomeLabel.text = string.Format(MiningLocalization.Text(
                    "Income: {0} every {1:0.##}s", "Thu nhập: {0} mỗi {1:0.##} giây"),
                    MiningMoneyFormatter.Format(station.CurrentRewardPerTick),
                    station.SecondsPerTick);

            bool maximum = station.IsMaximumLevel;
            if (nextIncomeLabel != null)
                nextIncomeLabel.text = maximum
                    ? MiningLocalization.Text("Maximum income reached", "Đã đạt thu nhập tối đa")
                    : string.Format(MiningLocalization.Text(
                        "Next level: {0} every {1:0.##}s",
                        "Cấp tiếp theo: {0} mỗi {1:0.##} giây"),
                        MiningMoneyFormatter.Format(station.NextRewardPerTick),
                        station.SecondsPerTick);
            if (costLabel != null)
                costLabel.text = maximum
                    ? MiningLocalization.Text("Upgrade cost: MAX", "Giá nâng cấp: TỐI ĐA")
                    : string.Format(MiningLocalization.Text(
                        "Upgrade cost: {0}", "Giá nâng cấp: {0}"),
                        MiningMoneyFormatter.Format(station.UpgradeCost));

            if (upgradeButton != null)
                upgradeButton.interactable = station.CanUpgrade;
            if (upgradeButtonLabel != null)
                upgradeButtonLabel.text = maximum
                    ? MiningLocalization.Text("MAX LEVEL", "CẤP TỐI ĐA")
                    : string.Format(MiningLocalization.Text(
                        "UPGRADE ({0})", "NÂNG CẤP ({0})"),
                        MiningMoneyFormatter.Format(station.UpgradeCost));
            if (statusLabel != null)
            {
                if (maximum)
                {
                    statusLabel.text = MiningLocalization.Text(
                        "Machine fully upgraded", "Máy đã nâng cấp hoàn toàn");
                }
                else if (station.CanUpgrade)
                {
                    statusLabel.text = MiningLocalization.Text(
                        "Ready to upgrade", "Có thể nâng cấp");
                }
                else
                {
                    float missing = Mathf.Max(0f, station.UpgradeCost -
                        (wallet != null ? wallet.CurrentMoney : 0f));
                    statusLabel.text = string.Format(MiningLocalization.Text(
                        "Need {0} more", "Cần thêm {0}"),
                        MiningMoneyFormatter.Format(missing));
                }
            }
        }
    }
}
