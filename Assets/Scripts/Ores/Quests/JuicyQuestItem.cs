using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Animation-only presentation for an authored quest row. MiningQuestPanel remains the sole
    /// owner of labels, progress, claim availability and rewards.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JuicyQuestItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Existing quest state")]
        [SerializeField] private string questId;
        [SerializeField] private MiningQuestData data;
        [SerializeField] private MiningQuestSystem questSystem;

        [Header("Authored visual references")]
        [SerializeField] private RectTransform rowBody;
        [SerializeField] private UnityEngine.UI.Image progressFill;
        [SerializeField] private UnityEngine.UI.Button claimButton;
        [SerializeField] private RectTransform claimButtonBody;
        [SerializeField] private UnityEngine.UI.Image claimButtonBackground;
        [SerializeField] private TextMeshProUGUI claimLabel;

        [Header("Animation only")]
        [Min(0.01f), SerializeField] private float progressSpeed = 3.5f;
        [Min(0f), SerializeField] private float pressDepth = 6f;
        [Min(0.01f), SerializeField] private float releaseDuration = 0.08f;
        [Min(0f), SerializeField] private float claimPulseAmount = 0.02f;
        [Min(0f), SerializeField] private float claimPulseSpeed = 4f;

        private static readonly Color InProgressTop = Hex("#4A3B33");
        private static readonly Color InProgressBottom = Hex("#211914");
        private static readonly Color ClaimTop = Hex("#FFB703");
        private static readonly Color ClaimBottom = Hex("#9A2C06");
        private static readonly Color ClaimedTop = Hex("#2F2A27");
        private static readonly Color ClaimedBottom = Hex("#171311");

        private MiningUiGradient claimGradient;
        private Vector2 authoredButtonPosition;
        private Vector3 authoredButtonScale;
        private Vector3 authoredRowScale;
        private float displayedFill;
        private float targetFill;
        private float lastWrittenFill;
        private Coroutine releaseRoutine;
        private Coroutine punchRoutine;

        public string QuestId => questId;

        private void Awake()
        {
            CacheAuthoredTransforms();
            claimGradient = claimButtonBackground != null
                ? claimButtonBackground.GetComponent<MiningUiGradient>()
                : null;
            displayedFill = progressFill != null ? progressFill.fillAmount : 0f;
            targetFill = displayedFill;
            lastWrittenFill = displayedFill;
            RefreshVisualState();
        }

        private void OnEnable()
        {
            if (questSystem != null)
            {
                questSystem.QuestsChanged -= HandleQuestsChanged;
                questSystem.QuestsChanged += HandleQuestsChanged;
                questSystem.RewardClaimed -= HandleRewardClaimed;
                questSystem.RewardClaimed += HandleRewardClaimed;
            }
            RefreshVisualState();
        }

        private void OnDisable()
        {
            if (questSystem != null)
            {
                questSystem.QuestsChanged -= HandleQuestsChanged;
                questSystem.RewardClaimed -= HandleRewardClaimed;
            }
            if (releaseRoutine != null)
            {
                StopCoroutine(releaseRoutine);
                releaseRoutine = null;
            }
            if (punchRoutine != null)
            {
                StopCoroutine(punchRoutine);
                punchRoutine = null;
            }
            RestoreAuthoredTransforms();
        }

        private void LateUpdate()
        {
            AnimateProgress();
            AnimateClaimPulse();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (claimButton == null || !claimButton.interactable || claimButtonBody == null)
            {
                return;
            }
            if (releaseRoutine != null)
            {
                StopCoroutine(releaseRoutine);
                releaseRoutine = null;
            }
            claimButtonBody.localScale = authoredButtonScale;
            claimButtonBody.anchoredPosition = authoredButtonPosition + Vector2.down * pressDepth;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (claimButtonBody == null || !gameObject.activeInHierarchy)
            {
                return;
            }
            if (releaseRoutine != null)
            {
                StopCoroutine(releaseRoutine);
            }
            releaseRoutine = StartCoroutine(ReturnButton());
        }

        private void AnimateProgress()
        {
            if (progressFill == null)
            {
                return;
            }

            // MiningQuestPanel writes the authoritative target directly into this Image.
            // Detect that external write, then animate back toward it without owning quest data.
            if (!Mathf.Approximately(progressFill.fillAmount, lastWrittenFill))
            {
                targetFill = progressFill.fillAmount;
            }
            displayedFill = Mathf.MoveTowards(displayedFill, targetFill,
                progressSpeed * Time.unscaledDeltaTime);
            progressFill.fillAmount = displayedFill;
            lastWrittenFill = displayedFill;
        }

        private void AnimateClaimPulse()
        {
            if (claimButtonBody == null || releaseRoutine != null)
            {
                return;
            }
            bool ready = claimButton != null && claimButton.interactable;
            float pulse = ready
                ? 1f + Mathf.Sin(Time.unscaledTime * claimPulseSpeed) * claimPulseAmount
                : 1f;
            claimButtonBody.localScale = authoredButtonScale * pulse;
        }

        private void HandleQuestsChanged()
        {
            RefreshVisualState();
        }

        private void HandleRewardClaimed(MiningQuestDefinition definition, float amount)
        {
            if (definition == null || !string.Equals(definition.QuestId, questId,
                    System.StringComparison.Ordinal) || rowBody == null)
            {
                return;
            }
            if (punchRoutine != null)
            {
                StopCoroutine(punchRoutine);
            }
            punchRoutine = StartCoroutine(PunchRow());
        }

        private void RefreshVisualState()
        {
            MiningQuestDefinition definition = data != null ? data.GetDefinition(questId) : null;
            bool claimed = questSystem != null && questSystem.IsClaimed(questId);
            bool complete = definition != null && questSystem != null &&
                            questSystem.GetProgress(questId) >= definition.TargetAmount;
            Color top = claimed ? ClaimedTop : complete ? ClaimTop : InProgressTop;
            Color bottom = claimed ? ClaimedBottom : complete ? ClaimBottom : InProgressBottom;
            if (claimGradient != null)
            {
                claimGradient.SetColors(top, bottom);
            }
            else if (claimButtonBackground != null)
            {
                claimButtonBackground.color = Color.Lerp(bottom, top, 0.55f);
            }
            if (claimLabel != null)
            {
                claimLabel.color = claimed
                    ? new Color(0.58f, 0.55f, 0.52f, 1f)
                    : complete ? Color.white : new Color(0.86f, 0.83f, 0.80f, 1f);
            }
        }

        private IEnumerator ReturnButton()
        {
            Vector2 start = claimButtonBody.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < releaseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Sin(Mathf.Clamp01(elapsed / releaseDuration) * Mathf.PI * 0.5f);
                claimButtonBody.anchoredPosition = Vector2.LerpUnclamped(start,
                    authoredButtonPosition, t);
                yield return null;
            }
            claimButtonBody.anchoredPosition = authoredButtonPosition;
            claimButtonBody.localScale = authoredButtonScale;
            releaseRoutine = null;
        }

        private IEnumerator PunchRow()
        {
            float elapsed = 0f;
            const float growDuration = 0.10f;
            const float settleDuration = 0.15f;
            while (elapsed < growDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                rowBody.localScale = Vector3.LerpUnclamped(authoredRowScale,
                    authoredRowScale * 1.035f, Mathf.Clamp01(elapsed / growDuration));
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                rowBody.localScale = Vector3.LerpUnclamped(authoredRowScale * 1.035f,
                    authoredRowScale, Mathf.Clamp01(elapsed / settleDuration));
                yield return null;
            }
            rowBody.localScale = authoredRowScale;
            punchRoutine = null;
        }

        private void CacheAuthoredTransforms()
        {
            if (claimButtonBody != null)
            {
                authoredButtonPosition = claimButtonBody.anchoredPosition;
                authoredButtonScale = claimButtonBody.localScale;
            }
            if (rowBody != null)
            {
                authoredRowScale = rowBody.localScale;
            }
        }

        private void RestoreAuthoredTransforms()
        {
            if (claimButtonBody != null)
            {
                claimButtonBody.anchoredPosition = authoredButtonPosition;
                claimButtonBody.localScale = authoredButtonScale;
            }
            if (rowBody != null)
            {
                rowBody.localScale = authoredRowScale;
            }
        }

        private static Color Hex(string hex) =>
            ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
    }
}
