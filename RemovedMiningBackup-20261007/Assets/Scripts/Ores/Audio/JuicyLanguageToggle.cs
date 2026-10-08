using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiningSimulator.Ores
{
    /// <summary>Presentation-only press animation for the existing localized language Button.</summary>
    [DisallowMultipleComponent]
    public sealed class JuicyLanguageToggle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform buttonBody;
        [SerializeField] private TextMeshProUGUI captionText;
        [SerializeField] private TextMeshProUGUI languageCodeText;
        [Min(0f), SerializeField] private float pressDepth = 6f;
        [Min(0.01f), SerializeField] private float returnDuration = 0.08f;

        private Vector2 authoredPosition;
        private Vector3 authoredScale;
        private Coroutine returnRoutine;

        private void Awake()
        {
            if (buttonBody != null)
            {
                authoredPosition = buttonBody.anchoredPosition;
                authoredScale = buttonBody.localScale;
            }
            RefreshText();
        }

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged -= RefreshText;
            MiningLocalization.LanguageChanged += RefreshText;
            RefreshText();
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= RefreshText;
            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
                returnRoutine = null;
            }
            RestoreAuthoredTransform();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (buttonBody == null)
            {
                return;
            }
            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
                returnRoutine = null;
            }
            buttonBody.anchoredPosition = authoredPosition + Vector2.down * pressDepth;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (buttonBody == null || !gameObject.activeInHierarchy)
            {
                return;
            }
            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
            }
            returnRoutine = StartCoroutine(ReturnBody());
        }

        private void RefreshText()
        {
            if (captionText != null)
            {
                captionText.text = MiningLocalization.Text("LANGUAGE", "NGÔN NGỮ");
            }
            if (languageCodeText != null)
            {
                languageCodeText.text = MiningLocalization.CurrentLanguageName switch
                {
                    MiningLocalization.EnglishLanguageName => "EN",
                    MiningLocalization.VietnameseLanguageName => "VI",
                    string languageName => languageName.ToUpperInvariant()
                };
            }
        }

        private IEnumerator ReturnBody()
        {
            Vector2 start = buttonBody.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < returnDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Sin(Mathf.Clamp01(elapsed / returnDuration) * Mathf.PI * 0.5f);
                buttonBody.anchoredPosition = Vector2.LerpUnclamped(start, authoredPosition, t);
                yield return null;
            }
            buttonBody.anchoredPosition = authoredPosition;
            buttonBody.localScale = authoredScale;
            returnRoutine = null;
        }

        private void RestoreAuthoredTransform()
        {
            if (buttonBody == null)
            {
                return;
            }
            buttonBody.anchoredPosition = authoredPosition;
            buttonBody.localScale = authoredScale;
        }
    }
}
