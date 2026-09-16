using System.Collections;
using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Open pop and wallet/title display over the existing panel controller and coordinator.</summary>
    [DisallowMultipleComponent]
    public sealed class JuicyUpgradePanel : MonoBehaviour
    {
        [SerializeField] private RectTransform panelBody;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI gemsText;
        [SerializeField] private TextMeshProUGUI closeText;
        [SerializeField] private float animationDuration = 0.15f;

        private Coroutine openAnimation;
        private Vector3 restingScale;

        private void Awake()
        {
            if (panelBody != null) restingScale = panelBody.localScale;
        }

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged += Refresh;
            if (wallet != null)
            {
                wallet.MoneyChanged += HandleMoneyChanged;
                wallet.GemsChanged += HandleGemsChanged;
            }
            Refresh();
            if (panelBody != null)
            {
                if (openAnimation != null) StopCoroutine(openAnimation);
                openAnimation = StartCoroutine(PopOpen());
            }
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= Refresh;
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
                wallet.GemsChanged -= HandleGemsChanged;
            }
            if (openAnimation != null) StopCoroutine(openAnimation);
            openAnimation = null;
            if (panelBody != null) panelBody.localScale = restingScale;
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

        private IEnumerator PopOpen()
        {
            float duration = Mathf.Max(0.01f, animationDuration);
            Vector3 start = restingScale * 0.85f;
            panelBody.localScale = start;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01((elapsed + Time.unscaledDeltaTime) / duration) - 1f;
                float eased = 1f + 2.70158f * t * t * t + 1.70158f * t * t;
                panelBody.localScale = Vector3.LerpUnclamped(start, restingScale, eased);
                yield return null;
            }
            panelBody.localScale = restingScale;
            openAnimation = null;
        }
    }
}
