using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Displays the shared currency wallet without mining or hiring controls.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningHud : MonoBehaviour
    {
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MiningGameData gameData;
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private string moneyFormat = "Tiền: {0}";
        private readonly MiningAnimatedCurrencyValue moneyCounter = new();

        private void OnEnable()
        {
            wallet ??= FindFirstObjectByType<PlayerWallet>();
            gameData ??= wallet != null ? wallet.GameData : null;
            MiningLocalization.LanguageChanged += RefreshMoneyText;
            if (wallet != null)
            {
                wallet.MoneyChanged += HandleMoneyChanged;
                moneyCounter.Initialize(wallet.CurrentMoney);
            }
            RefreshMoneyText();
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= RefreshMoneyText;
            if (wallet != null) wallet.MoneyChanged -= HandleMoneyChanged;
        }

        private void Update()
        {
            if (moneyCounter.Tick(Time.unscaledDeltaTime)) RefreshMoneyText();
        }

        private void HandleMoneyChanged(float money)
        {
            moneyCounter.SetTarget(money, gameData);
            RefreshMoneyText();
        }

        private void RefreshMoneyText()
        {
            if (moneyText != null)
                moneyText.text = string.Format(MiningLocalization.Text("Money: {0}", moneyFormat),
                    MiningMoneyFormatter.Format(moneyCounter.Value));
        }
    }
}
