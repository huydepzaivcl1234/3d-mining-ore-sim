using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Displays the saved secondary Gem balance without changing the gold HUD.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningGemHud : MonoBehaviour
    {
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MiningGameData gameData;
        [SerializeField] private TextMeshProUGUI gemText;
        [SerializeField] private string englishFormat = "Gems: {0}";
        [SerializeField] private string vietnameseFormat = "Ngọc: {0}";

        private readonly MiningAnimatedCurrencyValue gemCounter = new();

        private void OnEnable()
        {
            gameData ??= wallet != null ? wallet.GameData : null;
            MiningLocalization.LanguageChanged -= Refresh;
            MiningLocalization.LanguageChanged += Refresh;

            if (wallet != null)
            {
                wallet.GemsChanged -= HandleGemsChanged;
                wallet.GemsChanged += HandleGemsChanged;
                gemCounter.Initialize(wallet.CurrentGems);
            }

            Refresh();
        }

        private void Update()
        {
            if (gemCounter.Tick(Time.unscaledDeltaTime))
            {
                Refresh();
            }
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= Refresh;
            if (wallet != null)
            {
                wallet.GemsChanged -= HandleGemsChanged;
            }
        }

        private void HandleGemsChanged(float amount)
        {
            gemCounter.SetTarget(amount, gameData);
            Refresh();
        }

        private void Refresh()
        {
            if (gemText == null)
            {
                return;
            }

            string format = MiningLocalization.TextKey("HUD_GEM_FORMAT", "GEMS: {0}");
            gemText.text = string.Format(format, MiningMoneyFormatter.Format(gemCounter.Value));
        }
    }
}
