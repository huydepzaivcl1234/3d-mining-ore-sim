using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Presentation-only animation for the authored Rebirth card. Gameplay and rewards remain
    /// owned by MiningRebirthSystem and the existing MiningRebirthPanel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JuicyRebirth : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Live game data")]
        [SerializeField] private MiningRebirthSystem rebirthSystem;
        [SerializeField] private PlayerWallet wallet;

        [Header("Authored visual references")]
        [SerializeField] private RectTransform buttonBody;
        [SerializeField] private Image buttonBackground;
        [SerializeField] private Image progressFill;
        [SerializeField] private TextMeshProUGUI levelBadgeText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI currentRewardText;
        [SerializeField] private TextMeshProUGUI requirementText;
        [SerializeField] private TextMeshProUGUI percentText;
        [SerializeField] private TextMeshProUGUI buttonTitleText;
        [SerializeField] private TextMeshProUGUI buttonSubText;

        [Header("Animation only")]
        [Min(0f), SerializeField] private float pressDepth = 8f;
        [Min(0.01f), SerializeField] private float returnDuration = 0.08f;
        [Min(0.01f), SerializeField] private float fillSpeed = 3.5f;
        [Min(0f), SerializeField] private float readyPulseAmount = 0.018f;
        [Min(0f), SerializeField] private float readyPulseSpeed = 3f;
        [SerializeField] private Color lockedTop = new(0.27f, 0.24f, 0.23f, 1f);
        [SerializeField] private Color lockedBottom = new(0.12f, 0.10f, 0.09f, 1f);
        [SerializeField] private Color readyTop = new(1f, 0.72f, 0.02f, 1f);
        [SerializeField] private Color readyBottom = new(0.60f, 0.17f, 0.02f, 1f);

        private Vector2 authoredButtonPosition;
        private Vector3 authoredButtonScale;
        private float targetFill;
        private Coroutine returnRoutine;
        private MiningUiGradient buttonGradient;

        private void Awake()
        {
            CacheAuthoredTransform();
            buttonGradient = buttonBackground != null
                ? buttonBackground.GetComponent<MiningUiGradient>()
                : null;
            Refresh(true);
        }

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
            MiningLocalization.LanguageChanged += HandleLanguageChanged;
            if (rebirthSystem != null)
            {
                rebirthSystem.StateChanged -= HandleStateChanged;
                rebirthSystem.StateChanged += HandleStateChanged;
                rebirthSystem.RebirthCompleted -= HandleRebirthCompleted;
                rebirthSystem.RebirthCompleted += HandleRebirthCompleted;
            }
            Refresh(true);
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
            if (rebirthSystem != null)
            {
                rebirthSystem.StateChanged -= HandleStateChanged;
                rebirthSystem.RebirthCompleted -= HandleRebirthCompleted;
            }
            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
                returnRoutine = null;
            }
            RestoreAuthoredTransform();
        }

        private void Update()
        {
            if (progressFill != null)
            {
                progressFill.fillAmount = Mathf.MoveTowards(progressFill.fillAmount, targetFill,
                    fillSpeed * Time.unscaledDeltaTime);
            }

            if (buttonBody != null && rebirthSystem != null && rebirthSystem.CanRebirth &&
                returnRoutine == null)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * readyPulseSpeed) * readyPulseAmount;
                buttonBody.localScale = authoredButtonScale * pulse;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (buttonBody == null || rebirthSystem == null || !rebirthSystem.CanRebirth)
            {
                return;
            }
            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
                returnRoutine = null;
            }
            buttonBody.localScale = authoredButtonScale;
            buttonBody.anchoredPosition = authoredButtonPosition + Vector2.down * pressDepth;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (buttonBody != null && gameObject.activeInHierarchy)
            {
                if (returnRoutine != null)
                {
                    StopCoroutine(returnRoutine);
                }
                returnRoutine = StartCoroutine(ReturnButton());
            }
        }

        private IEnumerator ReturnButton()
        {
            Vector2 start = buttonBody.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < returnDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Sin(Mathf.Clamp01(elapsed / returnDuration) * Mathf.PI * 0.5f);
                buttonBody.anchoredPosition = Vector2.LerpUnclamped(start, authoredButtonPosition, t);
                yield return null;
            }
            buttonBody.anchoredPosition = authoredButtonPosition;
            buttonBody.localScale = authoredButtonScale;
            returnRoutine = null;
        }

        private void HandleStateChanged() => Refresh(false);
        private void HandleRebirthCompleted(int _) => Refresh(false);
        private void HandleLanguageChanged() => Refresh(false);

        private void Refresh(bool instant)
        {
            float money = wallet != null ? wallet.CurrentMoney : 0f;
            int requirement = rebirthSystem != null ? rebirthSystem.CurrentRequirement : 1;
            int count = rebirthSystem != null ? rebirthSystem.CompletedRebirths : 0;
            float currentMultiplier = rebirthSystem != null
                ? rebirthSystem.PermanentMoneyMultiplier
                : 1f;
            float currentStrengthMultiplier = rebirthSystem != null
                ? rebirthSystem.PermanentMiningStrengthMultiplier
                : 1f;
            float nextMultiplier = rebirthSystem != null ? rebirthSystem.NextMoneyMultiplier : 1f;
            float nextStrengthMultiplier = rebirthSystem != null
                ? rebirthSystem.NextMiningStrengthMultiplier
                : 1f;
            bool ready = rebirthSystem != null && rebirthSystem.CanRebirth;
            targetFill = requirement > 0 ? Mathf.Clamp01(money / requirement) : 0f;

            if (instant && progressFill != null)
            {
                progressFill.fillAmount = targetFill;
            }
            if (levelBadgeText != null)
            {
                levelBadgeText.text = string.Format(MiningLocalization.Text("LEVEL {0}", "CẤP {0}"),
                    count);
            }
            if (titleText != null)
            {
                titleText.text = MiningLocalization.Text("REBIRTH", "TRÙNG SINH");
            }
            if (currentRewardText != null)
            {
                currentRewardText.text = string.Format(MiningLocalization.Text(
                    "Permanent reward: x{0:0.00} money & XP • x{1:0.00} strength",
                    "Thưởng vĩnh viễn: x{0:0.00} tiền & XP • x{1:0.00} sức đào"),
                    currentMultiplier, currentStrengthMultiplier);
            }
            if (requirementText != null)
            {
                requirementText.text = string.Format(MiningLocalization.Text(
                        "{0:N0} / {1:N0} MONEY", "{0:N0} / {1:N0} TIỀN"),
                    MiningMoneyFormatter.Format(money), MiningMoneyFormatter.Format(requirement));
            }
            if (percentText != null)
            {
                percentText.text = $"{Mathf.RoundToInt(targetFill * 100f)}%";
            }
            if (buttonTitleText != null)
            {
                buttonTitleText.text = ready
                    ? MiningLocalization.Text("REBIRTH NOW!", "TRÙNG SINH NGAY!")
                    : MiningLocalization.Text("NOT READY YET", "CHƯA ĐỦ ĐIỀU KIỆN");
                buttonTitleText.color = ready ? Color.white : new Color(0.74f, 0.72f, 0.70f, 1f);
            }
            if (buttonSubText != null)
            {
                buttonSubText.text = ready
                    ? string.Format(MiningLocalization.Text(
                        "Next permanent reward: x{0:0.00} money & XP • x{1:0.00} strength",
                        "Thưởng vĩnh viễn kế tiếp: x{0:0.00} tiền & XP • x{1:0.00} sức đào"),
                        nextMultiplier, nextStrengthMultiplier)
                    : string.Format(MiningLocalization.Text(
                            "Need {0:N0} more money to unlock",
                            "Cần thêm {0:N0} tiền để mở khóa"),
                        MiningMoneyFormatter.Format(Mathf.Max(0f, requirement - money)));
                buttonSubText.color = ready
                    ? new Color(1f, 0.93f, 0.55f, 1f)
                    : new Color(0.63f, 0.61f, 0.59f, 1f);
            }

            ApplyButtonColors(ready);
        }

        private void ApplyButtonColors(bool ready)
        {
            Color top = ready ? readyTop : lockedTop;
            Color bottom = ready ? readyBottom : lockedBottom;
            if (buttonGradient != null)
            {
                buttonGradient.SetColors(top, bottom);
            }
            else if (buttonBackground != null)
            {
                buttonBackground.color = Color.Lerp(bottom, top, 0.55f);
            }
        }

        private void CacheAuthoredTransform()
        {
            if (buttonBody == null)
            {
                return;
            }
            authoredButtonPosition = buttonBody.anchoredPosition;
            authoredButtonScale = buttonBody.localScale;
        }

        private void RestoreAuthoredTransform()
        {
            if (buttonBody == null)
            {
                return;
            }
            buttonBody.anchoredPosition = authoredButtonPosition;
            buttonBody.localScale = authoredButtonScale;
        }
    }
}
