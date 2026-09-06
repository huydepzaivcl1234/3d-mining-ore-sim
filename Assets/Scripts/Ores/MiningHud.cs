using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Connects the editable TextMeshPro HUD to the mining gameplay state.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningHud : MonoBehaviour
    {
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private NpcShop npcShop;
        [SerializeField] private MiningGameData gameData;

        [Header("Editable HUD References")]
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI npcCountText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TextMeshProUGUI buyButtonLabel;

        [Header("Editable Text")]
        [SerializeField] private string moneyFormat = "Tiền: {0}";
        [SerializeField] private string npcCountFormat = "NPC đào quặng: {0}";
        [SerializeField] private string buyButtonFormat = "Mua NPC đào ({0})";
        [SerializeField] private string purchasedMessage = "Đã mua NPC đào quặng!";
        [SerializeField] private string purchaseFailedMessage = "Không đủ tiền hoặc thiếu cấu hình NPC.";

        private int displayedMoney;
        private int targetMoney;
        private float countAccumulator;
        private float currentCountSpeed;

        private void OnEnable()
        {
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
                wallet.MoneyChanged += HandleMoneyChanged;
                displayedMoney = wallet.CurrentMoney;
                targetMoney = displayedMoney;
            }

            if (npcShop != null)
            {
                npcShop.NpcCountChanged -= HandleNpcCountChanged;
                npcShop.NpcCountChanged += HandleNpcCountChanged;
            }

            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(BuyNpc);
                buyButton.onClick.AddListener(BuyNpc);
            }

            RefreshMoneyText();
            RefreshOtherText();
        }

        private void OnDisable()
        {
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
            }

            if (npcShop != null)
            {
                npcShop.NpcCountChanged -= HandleNpcCountChanged;
            }

            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(BuyNpc);
            }
        }

        private void Update()
        {
            if (displayedMoney == targetMoney || gameData == null)
            {
                return;
            }

            countAccumulator += Time.unscaledDeltaTime * currentCountSpeed;
            int wholeUnits = Mathf.FloorToInt(countAccumulator);
            if (wholeUnits <= 0)
            {
                return;
            }

            countAccumulator -= wholeUnits;
            int difference = targetMoney - displayedMoney;
            displayedMoney += Mathf.Clamp(difference, -wholeUnits, wholeUnits);
            RefreshMoneyText();
        }

        private void BuyNpc()
        {
            bool purchased = npcShop != null && npcShop.TryBuyNpc();
            if (statusText != null)
            {
                statusText.text = purchased ? purchasedMessage : purchaseFailedMessage;
            }
            RefreshOtherText();
        }

        private void HandleMoneyChanged(int money)
        {
            targetMoney = money;
            countAccumulator = 0f;

            if (gameData == null)
            {
                displayedMoney = targetMoney;
            }
            else
            {
                int difference = Mathf.Abs(targetMoney - displayedMoney);
                float durationLimitedSpeed = difference / gameData.MoneyCountMaximumDuration;
                currentCountSpeed = Mathf.Max(gameData.MoneyCountUnitsPerSecond, durationLimitedSpeed);
            }

            RefreshMoneyText();
            RefreshOtherText();
        }

        private void HandleNpcCountChanged(int count)
        {
            RefreshOtherText();
        }

        private void RefreshMoneyText()
        {
            if (moneyText != null)
            {
                moneyText.text = string.Format(moneyFormat, displayedMoney);
            }
        }

        private void RefreshOtherText()
        {
            if (npcCountText != null)
            {
                int count = npcShop != null ? npcShop.PurchasedCount : 0;
                npcCountText.text = string.Format(npcCountFormat, count);
            }

            if (buyButtonLabel != null)
            {
                int cost = npcShop != null ? npcShop.NpcCost : 0;
                buyButtonLabel.text = string.Format(buyButtonFormat, cost);
            }

            if (buyButton != null)
            {
                buyButton.interactable = npcShop != null && npcShop.CanBuy;
            }
        }
    }
}
