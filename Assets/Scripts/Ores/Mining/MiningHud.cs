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
        [Tooltip("English NPC count. {0} = miners owned; {1} = miner limit.")]
        [SerializeField] private string npcCountEnglishFormat = "Miner: {0}";
        [Tooltip("Vietnamese NPC count. {0} = miners owned; {1} = miner limit.")]
        [SerializeField] private string npcCountVietnameseFormat = "Thợ mỏ: {0}";
        [SerializeField] private string buyButtonFormat = "Mua NPC đào ({0})";
        [SerializeField] private string purchasedMessage = "Đã mua NPC đào quặng!";
        [SerializeField] private string purchaseFailedMessage =
            "Không đủ tiền hoặc đã đạt giới hạn thợ mỏ.";

        [Header("NPC Count Colors")]
        [Tooltip("Normal color comes from the NPC Count text in the scene. This color is used at the limit.")]
        [SerializeField] private Color fullNpcCountColor = new(1f, 0.26f, 0.22f, 1f);

        private readonly MiningAnimatedCurrencyValue moneyCounter = new();
        private Color authoredNpcCountColor;

        private void Awake()
        {
            if (npcShop == null)
                npcShop = FindFirstObjectByType<NpcShop>(FindObjectsInactive.Include);
            authoredNpcCountColor = npcCountText != null ? npcCountText.color : Color.white;
        }

        private void OnEnable()
        {
            gameData ??= wallet != null ? wallet.GameData : null;
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
            MiningLocalization.LanguageChanged += HandleLanguageChanged;
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
                wallet.MoneyChanged += HandleMoneyChanged;
                moneyCounter.Initialize(wallet.CurrentMoney);
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
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
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
            if (moneyCounter.Tick(Time.unscaledDeltaTime))
            {
                RefreshMoneyText();
            }
        }

        private void BuyNpc()
        {
            bool purchased = npcShop != null && npcShop.TryBuyNpc();
            if (statusText != null)
            {
                statusText.text = purchased
                    ? MiningLocalization.Text("Miner purchased!", purchasedMessage)
                    : MiningLocalization.Text(
                        "Not enough money or the miner limit has been reached.",
                        purchaseFailedMessage);
            }
            RefreshOtherText();
        }

        private void HandleMoneyChanged(float money)
        {
            moneyCounter.SetTarget(money, gameData);

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
                moneyText.text = string.Format(
                    MiningLocalization.Text("Money: {0}", moneyFormat),
                    MiningMoneyFormatter.Format(moneyCounter.Value));
            }
        }

        private void RefreshOtherText()
        {
            if (npcCountText != null)
            {
                int count = npcShop != null ? npcShop.PurchasedCount : 0;
                int maximum = npcShop != null ? npcShop.MaximumMiners : 0;
                npcCountText.color = maximum > 0 && count >= maximum
                    ? fullNpcCountColor : authoredNpcCountColor;
                // The selected Lean language chooses the scene-editable template.
                string format = MiningLocalization.IsEnglish
                    ? npcCountEnglishFormat : npcCountVietnameseFormat;
                npcCountText.text = string.Format(format, count, maximum);
            }

            if (buyButtonLabel != null)
            {
                int cost = npcShop != null ? npcShop.NpcCost : 0;
                buyButtonLabel.text = string.Format(MiningLocalization.Text(
                        "Buy miner ({0})", buyButtonFormat),
                    MiningMoneyFormatter.Format(cost));
            }

            if (buyButton != null)
            {
                buyButton.interactable = npcShop != null && npcShop.CanBuy;
            }
        }

        private void HandleLanguageChanged()
        {
            RefreshMoneyText();
            RefreshOtherText();
        }
    }
}
