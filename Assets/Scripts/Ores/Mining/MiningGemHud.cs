using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Displays the saved secondary Gem balance without changing the gold HUD.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningGemHud : MonoBehaviour
    {
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private TextMeshProUGUI gemText;
        [SerializeField] private string englishFormat = "Gems: {0}";
        [SerializeField] private string vietnameseFormat = "Ngọc: {0}";

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged -= Refresh;
            MiningLocalization.LanguageChanged += Refresh;

            if (wallet != null)
            {
                wallet.GemsChanged -= HandleGemsChanged;
                wallet.GemsChanged += HandleGemsChanged;
            }

            Refresh();
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
            Refresh();
        }

        private void Refresh()
        {
            if (gemText == null)
            {
                return;
            }

            float amount = wallet != null ? wallet.CurrentGems : 0f;
            string format = MiningLocalization.Text(englishFormat, vietnameseFormat);
            gemText.text = string.Format(format, MiningMoneyFormatter.Format(amount));
        }
    }
}
