using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Open pop and wallet/title display over the existing panel controller and coordinator.</summary>
    [DisallowMultipleComponent]
    public sealed class JuicyUpgradePanel : AnimatedPanel
    {
        [SerializeField] private RectTransform panelBody;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI gemsText;
        [SerializeField] private TextMeshProUGUI closeText;
        [SerializeField] private float animationDuration = 0.15f;

        protected override RectTransform Body => panelBody;
        protected override float OpenDuration => animationDuration;
        protected override float OpenStartScale => 0.85f;

        protected override void OnEnable()
        {
            MiningLocalization.LanguageChanged += Refresh;
            if (wallet != null)
            {
                wallet.MoneyChanged += HandleMoneyChanged;
                wallet.GemsChanged += HandleGemsChanged;
            }
            Refresh();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            MiningLocalization.LanguageChanged -= Refresh;
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
                wallet.GemsChanged -= HandleGemsChanged;
            }
            base.OnDisable();
        }

        private void HandleMoneyChanged(float amount) => Refresh();
        private void HandleGemsChanged(float amount) => Refresh();

        public void Refresh()
        {
            if (titleText != null)
                titleText.text = MiningLocalization.Text("MINING UPGRADES", "BẢNG NÂNG CẤP HẦM MỎ");
            if (moneyText != null) moneyText.text = wallet != null
                ? MiningMoneyFormatter.Format(wallet.CurrentMoney) : "—";
            if (gemsText != null) gemsText.text = wallet != null
                ? MiningMoneyFormatter.Format(wallet.CurrentGems) : "—";
            if (closeText != null)
                closeText.text = MiningLocalization.Text("CLOSE UPGRADES", "ĐÓNG BẢNG NÂNG CẤP");
        }

    }
}
