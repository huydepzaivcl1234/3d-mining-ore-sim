using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Editable TMP panel for drill purchase, production stats, and upgrades.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningDrillPanel : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private MiningUiPanelCoordinator panelCoordinator;

        [Header("Editable UI References")]
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private Button purchaseOrUpgradeButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI levelLabel;
        [SerializeField] private TextMeshProUGUI productionLabel;
        [SerializeField] private TextMeshProUGUI nextLevelLabel;
        [SerializeField] private TextMeshProUGUI actionLabel;

        [Header("Editable Text")]
        [SerializeField] private string titleText = "MÁY KHOAN TỰ ĐỘNG";
        [SerializeField] private string lockedLevelText = "CHƯA MUA";
        [SerializeField] private string levelFormat = "CẤP {0}/{1}";
        [SerializeField] private string productionFormat = "+{0} tiền mỗi {1:0.##} giây";
        [SerializeField] private string nextLevelFormat = "Cấp tiếp: +{0} tiền mỗi {1:0.##} giây";
        [SerializeField] private string buyFormat = "MUA - {0} TIỀN";
        [SerializeField] private string upgradeFormat = "NÂNG CẤP - {0} TIỀN";
        [SerializeField] private string maximumText = "ĐÃ ĐẠT CẤP TỐI ĐA";

        private MiningDrillStation station;

        public event Action PanelOpened;
        public event Action PanelClosed;

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = transform as RectTransform;
            }

            panelRoot?.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            AddListeners();
            SubscribeToStation();
            Refresh();
        }

        private void OnDisable()
        {
            RemoveListeners();
            UnsubscribeFromStation();
        }

        public void Bind(MiningDrillStation targetStation)
        {
            if (station == targetStation)
            {
                return;
            }

            UnsubscribeFromStation();
            station = targetStation;
            SubscribeToStation();
            Refresh();
        }

        public void SetPanelCoordinator(MiningUiPanelCoordinator coordinator)
        {
            panelCoordinator = coordinator;
        }

        public void OpenFor(MiningDrillStation targetStation)
        {
            Bind(targetStation);
            if (panelRoot == null)
            {
                return;
            }

            if (panelCoordinator != null)
            {
                panelCoordinator.OpenPanel(panelRoot);
            }
            else
            {
                panelRoot.gameObject.SetActive(true);
            }

            Refresh();
            PanelOpened?.Invoke();
        }

        public void Close()
        {
            if (panelRoot == null)
            {
                return;
            }

            if (panelCoordinator != null)
            {
                panelCoordinator.ClosePanel(panelRoot);
            }
            else
            {
                panelRoot.gameObject.SetActive(false);
            }

            PanelClosed?.Invoke();
        }

        private void AddListeners()
        {
            purchaseOrUpgradeButton?.onClick.RemoveListener(PurchaseOrUpgrade);
            purchaseOrUpgradeButton?.onClick.AddListener(PurchaseOrUpgrade);
            closeButton?.onClick.RemoveListener(Close);
            closeButton?.onClick.AddListener(Close);
        }

        private void RemoveListeners()
        {
            purchaseOrUpgradeButton?.onClick.RemoveListener(PurchaseOrUpgrade);
            closeButton?.onClick.RemoveListener(Close);
        }

        private void SubscribeToStation()
        {
            if (station == null)
            {
                return;
            }

            station.StateChanged -= Refresh;
            station.StateChanged += Refresh;
        }

        private void UnsubscribeFromStation()
        {
            if (station != null)
            {
                station.StateChanged -= Refresh;
            }
        }

        private void PurchaseOrUpgrade()
        {
            station?.TryPurchaseOrUpgrade();
            Refresh();
        }

        private void Refresh()
        {
            if (titleLabel != null)
            {
                titleLabel.text = titleText;
            }

            if (station == null || station.DrillData == null)
            {
                if (purchaseOrUpgradeButton != null)
                {
                    purchaseOrUpgradeButton.interactable = false;
                }
                return;
            }

            if (levelLabel != null)
            {
                levelLabel.text = station.IsPurchased
                    ? string.Format(levelFormat, station.CurrentLevel,
                        station.DrillData.MaximumLevel)
                    : lockedLevelText;
            }

            MiningDrillLevel current = station.CurrentDefinition;
            if (productionLabel != null)
            {
                productionLabel.text = current == null
                    ? "-"
                    : string.Format(productionFormat,
                        MiningMoneyFormatter.Format(current.MoneyPerCycle),
                        current.SecondsPerCycle);
            }

            MiningDrillLevel next = station.NextDefinition;
            if (nextLevelLabel != null)
            {
                nextLevelLabel.text = next == null
                    ? maximumText
                    : string.Format(nextLevelFormat,
                        MiningMoneyFormatter.Format(next.MoneyPerCycle),
                        next.SecondsPerCycle);
            }

            if (actionLabel != null)
            {
                actionLabel.text = station.IsMaximumLevel
                    ? maximumText
                    : string.Format(station.IsPurchased ? upgradeFormat : buyFormat,
                        MiningMoneyFormatter.Format(station.NextCost));
            }

            if (purchaseOrUpgradeButton != null)
            {
                purchaseOrUpgradeButton.interactable = station.CanPurchaseOrUpgrade;
            }
        }
    }
}
