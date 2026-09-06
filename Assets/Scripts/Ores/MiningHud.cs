using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Connects the editable HUD objects to the mining gameplay state.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningHud : MonoBehaviour
    {
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private NpcShop npcShop;

        [Header("Editable HUD References")]
        [SerializeField] private Text moneyText;
        [SerializeField] private Text npcCountText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Text buyButtonLabel;

        [Header("Editable Text")]
        [SerializeField] private string moneyFormat = "Tiền: {0}";
        [SerializeField] private string npcCountFormat = "NPC đào quặng: {0}";
        [SerializeField] private string buyButtonFormat = "Mua NPC đào ({0})";
        [SerializeField] private string purchasedMessage = "Đã mua NPC đào quặng!";
        [SerializeField] private string purchaseFailedMessage = "Không đủ tiền hoặc thiếu cấu hình NPC.";

        private void OnEnable()
        {
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
                wallet.MoneyChanged += HandleMoneyChanged;
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

            Refresh();
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

        private void BuyNpc()
        {
            bool purchased = npcShop != null && npcShop.TryBuyNpc();
            if (statusText != null)
            {
                statusText.text = purchased ? purchasedMessage : purchaseFailedMessage;
            }
            Refresh();
        }

        private void HandleMoneyChanged(int money)
        {
            Refresh();
        }

        private void HandleNpcCountChanged(int count)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (moneyText == null || npcCountText == null || buyButton == null || buyButtonLabel == null)
            {
                return;
            }

            int money = wallet != null ? wallet.CurrentMoney : 0;
            int count = npcShop != null ? npcShop.PurchasedCount : 0;
            int cost = npcShop != null ? npcShop.NpcCost : 0;
            moneyText.text = string.Format(moneyFormat, money);
            npcCountText.text = string.Format(npcCountFormat, count);
            buyButtonLabel.text = string.Format(buyButtonFormat, cost);
            buyButton.interactable = npcShop != null && npcShop.CanBuy;
        }
    }
}
