using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Displays the saved secondary Gem balance without changing the gold HUD.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningGemHud : MonoBehaviour
    {
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private MiningGameData gameData;
        [SerializeField] private TextMeshProUGUI gemText;
        [SerializeField] private RectTransform counterRootRect;
        [SerializeField] private RectTransform gemIconRect;
        [SerializeField] private Button plusButton;
        [SerializeField] private MiningShopPanel shopPanel;
        [SerializeField] private MiningGemSparkleGraphic sparkleFx;
        [SerializeField] private string englishFormat = "Gems: {0}";
        [SerializeField] private string vietnameseFormat = "Ngọc: {0}";

        private readonly MiningAnimatedCurrencyValue gemCounter = new();
        private Vector3 rootAuthoredScale = Vector3.one;
        private Vector3 iconAuthoredScale = Vector3.one;
        private Quaternion iconAuthoredRotation = Quaternion.identity;
        private Color textAuthoredColor = Color.white;
        private float lastWalletAmount;
        private Coroutine punchRoutine;

        private void OnEnable()
        {
            gameData ??= wallet != null ? wallet.GameData : null;
            counterRootRect ??= transform as RectTransform;
            CacheAuthoredPresentation();
            MiningLocalization.LanguageChanged -= Refresh;
            MiningLocalization.LanguageChanged += Refresh;
            plusButton?.onClick.RemoveListener(OpenShop);
            plusButton?.onClick.AddListener(OpenShop);

            if (wallet != null)
            {
                wallet.GemsChanged -= HandleGemsChanged;
                wallet.GemsChanged += HandleGemsChanged;
                gemCounter.Initialize(wallet.CurrentGems);
                lastWalletAmount = wallet.CurrentGems;
            }

            Refresh();
        }

        private void Update()
        {
            if (gemCounter.Tick(Time.unscaledDeltaTime))
            {
                Refresh();
            }

            if (gemIconRect != null)
            {
                float phase = Time.unscaledTime * 2.25f;
                float scale = 1f + Mathf.Sin(phase) * 0.045f;
                gemIconRect.localScale = iconAuthoredScale * scale;
                gemIconRect.localRotation = iconAuthoredRotation *
                                            Quaternion.Euler(0f, 0f,
                                                Mathf.Sin(phase * 0.72f) * 3f);
            }
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= Refresh;
            plusButton?.onClick.RemoveListener(OpenShop);
            if (wallet != null)
            {
                wallet.GemsChanged -= HandleGemsChanged;
            }
            if (punchRoutine != null)
            {
                StopCoroutine(punchRoutine);
                punchRoutine = null;
            }
            RestoreAuthoredPresentation();
        }

        private void HandleGemsChanged(float amount)
        {
            float delta = amount - lastWalletAmount;
            lastWalletAmount = amount;
            gemCounter.SetTarget(amount, gameData);
            if (!Mathf.Approximately(delta, 0f))
            {
                if (punchRoutine != null)
                {
                    StopCoroutine(punchRoutine);
                }
                punchRoutine = StartCoroutine(Punch(delta > 0f));
                if (delta > 0f)
                {
                    sparkleFx?.PlayBurst(delta >= 100f);
                }
            }
            Refresh();
        }

        private void Refresh()
        {
            if (gemText == null)
            {
                return;
            }

            // The sample counter displays only the value; the project icon provides its meaning.
            gemText.text = MiningMoneyFormatter.Format(gemCounter.Value);
        }

        private void OpenShop()
        {
            shopPanel?.Open();
        }

        private void CacheAuthoredPresentation()
        {
            if (counterRootRect != null) rootAuthoredScale = counterRootRect.localScale;
            if (gemIconRect != null)
            {
                iconAuthoredScale = gemIconRect.localScale;
                iconAuthoredRotation = gemIconRect.localRotation;
            }
            if (gemText != null) textAuthoredColor = gemText.color;
        }

        private void RestoreAuthoredPresentation()
        {
            if (counterRootRect != null) counterRootRect.localScale = rootAuthoredScale;
            if (gemIconRect != null)
            {
                gemIconRect.localScale = iconAuthoredScale;
                gemIconRect.localRotation = iconAuthoredRotation;
            }
            if (gemText != null) gemText.color = textAuthoredColor;
        }

        private IEnumerator Punch(bool gained)
        {
            if (counterRootRect == null)
            {
                yield break;
            }

            Vector3 peak = rootAuthoredScale * (gained ? 1.16f : 0.94f);
            Color punchColor = gained
                ? new Color(1f, 0.90f, 0.42f, 1f)
                : new Color(1f, 0.46f, 0.54f, 1f);
            if (gemText != null) gemText.color = punchColor;

            float elapsed = 0f;
            const float popDuration = 0.09f;
            while (elapsed < popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                counterRootRect.localScale = Vector3.LerpUnclamped(rootAuthoredScale, peak,
                    EaseOutBack(Mathf.Clamp01(elapsed / popDuration)));
                yield return null;
            }

            elapsed = 0f;
            const float settleDuration = 0.24f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / settleDuration);
                counterRootRect.localScale = Vector3.LerpUnclamped(peak, rootAuthoredScale,
                    EaseOutCubic(t));
                if (gemText != null)
                    gemText.color = Color.Lerp(punchColor, textAuthoredColor, t);
                yield return null;
            }

            counterRootRect.localScale = rootAuthoredScale;
            if (gemText != null) gemText.color = textAuthoredColor;
            punchRoutine = null;
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            return 1f + (overshoot + 1f) * Mathf.Pow(t - 1f, 3f) +
                   overshoot * Mathf.Pow(t - 1f, 2f);
        }
    }
}
