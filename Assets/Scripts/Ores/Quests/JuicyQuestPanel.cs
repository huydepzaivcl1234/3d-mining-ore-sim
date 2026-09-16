using System.Collections;
using TMPro;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Open animation and optional bottom-close forwarding for the authored Quest card.</summary>
    [DisallowMultipleComponent]
    public sealed class JuicyQuestPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform panelBody;
        [SerializeField] private MiningQuestPanel questPanel;
        [SerializeField] private UnityEngine.UI.Button bottomCloseButton;
        [SerializeField] private TextMeshProUGUI bottomCloseLabel;
        [Min(0.01f), SerializeField] private float openDuration = 0.18f;
        [Range(0.5f, 1f), SerializeField] private float openStartScale = 0.88f;

        private Vector3 authoredScale;
        private Coroutine openRoutine;

        private void Awake()
        {
            if (panelBody != null)
            {
                authoredScale = panelBody.localScale;
            }
        }

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged -= RefreshLocalizedText;
            MiningLocalization.LanguageChanged += RefreshLocalizedText;
            bottomCloseButton?.onClick.RemoveListener(CloseThroughExistingPanel);
            bottomCloseButton?.onClick.AddListener(CloseThroughExistingPanel);
            RefreshLocalizedText();
            if (panelBody != null)
            {
                if (openRoutine != null)
                {
                    StopCoroutine(openRoutine);
                }
                openRoutine = StartCoroutine(AnimateOpen());
            }
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= RefreshLocalizedText;
            bottomCloseButton?.onClick.RemoveListener(CloseThroughExistingPanel);
            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
                openRoutine = null;
            }
            if (panelBody != null)
            {
                panelBody.localScale = authoredScale;
            }
        }

        private void CloseThroughExistingPanel()
        {
            questPanel?.Close();
        }

        private void RefreshLocalizedText()
        {
            if (bottomCloseLabel != null)
            {
                bottomCloseLabel.text = MiningLocalization.Text(
                    "CLOSE QUEST PANEL", "ĐÓNG BẢNG NHIỆM VỤ");
            }
        }

        private IEnumerator AnimateOpen()
        {
            Vector3 start = authoredScale * openStartScale;
            panelBody.localScale = start;
            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);
                const float overshoot = 1.70158f;
                float eased = 1f + (overshoot + 1f) * Mathf.Pow(t - 1f, 3f) +
                              overshoot * Mathf.Pow(t - 1f, 2f);
                panelBody.localScale = Vector3.LerpUnclamped(start, authoredScale, eased);
                yield return null;
            }
            panelBody.localScale = authoredScale;
            openRoutine = null;
        }
    }
}
