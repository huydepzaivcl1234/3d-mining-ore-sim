using System.Collections;
using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Presentation-only layer for the authored Rebirth confirmation. MiningRebirthPanel remains
    /// the sole owner of opening, closing, validation, reset rewards and the confirmation action.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JuicyRebirthConfirm : MonoBehaviour
    {
        [Header("Authoritative Data")]
        [SerializeField] private MiningRebirthSystem rebirthSystem;
        [SerializeField] private PlayerWallet wallet;

        [Header("Presentation")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform emblemRect;
        [SerializeField] private TextMeshProUGUI subtitleLabel;
        [SerializeField] private TextMeshProUGUI lossTitleLabel;
        [SerializeField] private TextMeshProUGUI lossDetailsLabel;
        [SerializeField] private TextMeshProUGUI gainTitleLabel;
        [SerializeField] private TextMeshProUGUI gainDetailsLabel;
        [SerializeField] private TextMeshProUGUI rewardTitleLabel;

        [Header("Animation Only")]
        [Min(0.05f), SerializeField] private float showDuration = 0.32f;
        [Range(0.5f, 1f), SerializeField] private float startScale = 0.82f;
        [Min(0.2f), SerializeField] private float emblemPulseSpeed = 2.2f;
        [Range(1f, 1.2f), SerializeField] private float emblemPulseScale = 1.08f;

        private Coroutine animationRoutine;
        private Vector3 panelAuthoredScale = Vector3.one;
        private Vector3 emblemAuthoredScale = Vector3.one;
        private bool scalesCached;

        public void Configure(MiningRebirthSystem system, PlayerWallet playerWallet,
            RectTransform panel, CanvasGroup group, RectTransform emblem,
            TextMeshProUGUI subtitle, TextMeshProUGUI lossTitle,
            TextMeshProUGUI lossDetails, TextMeshProUGUI gainTitle,
            TextMeshProUGUI gainDetails, TextMeshProUGUI rewardTitle)
        {
            rebirthSystem = system;
            wallet = playerWallet;
            panelRect = panel;
            canvasGroup = group;
            emblemRect = emblem;
            subtitleLabel = subtitle;
            lossTitleLabel = lossTitle;
            lossDetailsLabel = lossDetails;
            gainTitleLabel = gainTitle;
            gainDetailsLabel = gainDetails;
            rewardTitleLabel = rewardTitle;
        }

        private void Awake()
        {
            ResolveReferences();
            CacheScales();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CacheScales();
            MiningLocalization.LanguageChanged -= Refresh;
            MiningLocalization.LanguageChanged += Refresh;
            if (rebirthSystem != null)
            {
                rebirthSystem.StateChanged -= Refresh;
                rebirthSystem.StateChanged += Refresh;
            }
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
                wallet.MoneyChanged += HandleMoneyChanged;
            }
            Refresh();
            StartPresentation();
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= Refresh;
            if (rebirthSystem != null)
            {
                rebirthSystem.StateChanged -= Refresh;
            }
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleMoneyChanged;
            }
            StopPresentation();
        }

        private void ResolveReferences()
        {
            panelRect ??= transform as RectTransform;
            canvasGroup ??= GetComponent<CanvasGroup>();
        }

        private void CacheScales()
        {
            if (scalesCached)
            {
                return;
            }
            panelAuthoredScale = panelRect != null ? panelRect.localScale : Vector3.one;
            emblemAuthoredScale = emblemRect != null ? emblemRect.localScale : Vector3.one;
            scalesCached = true;
        }

        private void StartPresentation()
        {
            StopPresentation();
            if (isActiveAndEnabled)
            {
                animationRoutine = StartCoroutine(AnimatePresentation());
            }
        }

        private void StopPresentation()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }
            if (panelRect != null)
            {
                panelRect.localScale = panelAuthoredScale;
            }
            if (emblemRect != null)
            {
                emblemRect.localScale = emblemAuthoredScale;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        private IEnumerator AnimatePresentation()
        {
            float elapsed = 0f;
            if (panelRect != null)
            {
                panelRect.localScale = panelAuthoredScale * startScale;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            while (elapsed < showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / showDuration);
                if (panelRect != null)
                {
                    panelRect.localScale = Vector3.LerpUnclamped(panelAuthoredScale * startScale,
                        panelAuthoredScale, EaseOutBack(t));
                }
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = t;
                }
                yield return null;
            }

            if (panelRect != null)
            {
                panelRect.localScale = panelAuthoredScale;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            elapsed = 0f;
            while (true)
            {
                elapsed += Time.unscaledDeltaTime * emblemPulseSpeed;
                if (emblemRect != null)
                {
                    float pulse = Mathf.Lerp(1f, emblemPulseScale,
                        (Mathf.Sin(elapsed) + 1f) * 0.5f);
                    emblemRect.localScale = emblemAuthoredScale * pulse;
                }
                yield return null;
            }
        }

        private void HandleMoneyChanged(float money)
        {
            Refresh();
        }

        private void Refresh()
        {
            int completed = rebirthSystem != null ? rebirthSystem.CompletedRebirths : 0;
            float currentMultiplier = rebirthSystem != null
                ? rebirthSystem.PermanentMoneyMultiplier
                : 1f;
            float nextMultiplier = rebirthSystem != null
                ? rebirthSystem.NextMoneyMultiplier
                : currentMultiplier;
            float currentStrengthMultiplier = rebirthSystem != null
                ? rebirthSystem.PermanentMiningStrengthMultiplier
                : 1f;
            float nextStrengthMultiplier = rebirthSystem != null
                ? rebirthSystem.NextMiningStrengthMultiplier
                : currentStrengthMultiplier;
            string currentMoney = MiningMoneyFormatter.Format(wallet != null
                ? wallet.CurrentMoney
                : 0f);

            Set(subtitleLabel, MiningLocalization.Text("START AGAIN, RETURN STRONGER",
                "BẮT ĐẦU LẠI, TRỞ NÊN MẠNH HƠN"));
            Set(lossTitleLabel, MiningLocalization.Text("RESET", "MẤT ĐI"));
            Set(lossDetailsLabel, MiningLocalization.Text(
                $"Money: {currentMoney}\nAll upgrades\nMiner level and field NPCs",
                $"Tiền: {currentMoney}\nMọi nâng cấp\nCấp thợ mỏ và NPC trên sân"));
            Set(gainTitleLabel, MiningLocalization.Text("PERMANENT REWARD", "NHẬN VĨNH VIỄN"));
            Set(gainDetailsLabel, MiningLocalization.Text(
                $"Money & XP: x{currentMultiplier:0.00}  >  x{nextMultiplier:0.00}\nStrength: x{currentStrengthMultiplier:0.00}  >  x{nextStrengthMultiplier:0.00}\nRebirth {completed}  >  {completed + 1}",
                $"Tiền & XP: x{currentMultiplier:0.00}  >  x{nextMultiplier:0.00}\nSức đào: x{currentStrengthMultiplier:0.00}  >  x{nextStrengthMultiplier:0.00}\nTái sinh {completed}  >  {completed + 1}"));
            Set(rewardTitleLabel, MiningLocalization.Text("NEXT PERMANENT MULTIPLIER",
                "HỆ SỐ VĨNH VIỄN TIẾP THEO"));
        }

        private static void Set(TextMeshProUGUI label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }

        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            return 1f + (overshoot + 1f) * Mathf.Pow(t - 1f, 3f) +
                   overshoot * Mathf.Pow(t - 1f, 2f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            showDuration = Mathf.Max(0.05f, showDuration);
            startScale = Mathf.Clamp(startScale, 0.5f, 1f);
            emblemPulseSpeed = Mathf.Max(0.2f, emblemPulseSpeed);
            emblemPulseScale = Mathf.Clamp(emblemPulseScale, 1f, 1.2f);
        }
#endif
    }
}
