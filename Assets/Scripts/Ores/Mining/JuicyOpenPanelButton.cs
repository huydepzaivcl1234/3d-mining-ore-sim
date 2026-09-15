using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiningSimulator.Ores
{
    /// <summary>Animated face for MiningUpgradePanel's existing Open Upgrades Button.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class JuicyOpenPanelButton : MonoBehaviour, IPointerDownHandler,
        IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform buttonBody;
        [SerializeField] private RectTransform shimmerRect;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI expandBadgeText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI actionText;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private float pressDepthY = -8f;
        [SerializeField] private float returnDuration = 0.08f;
        [SerializeField] private float shimmerSpeed = 600f;
        [SerializeField] private float shimmerDelay = 3.5f;

        private UnityEngine.UI.Button button;
        private Vector2 restPosition;
        private Coroutine returnRoutine;
        private Coroutine shimmerRoutine;

        private void Awake()
        {
            button = GetComponent<UnityEngine.UI.Button>();
            if (buttonBody == null) buttonBody = transform.Find("Button_Body") as RectTransform;
            if (buttonBody != null) restPosition = buttonBody.anchoredPosition;
        }

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged += RefreshText;
            RefreshText();
            if (shimmerRect != null) shimmerRoutine = StartCoroutine(Shimmer());
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= RefreshText;
            if (returnRoutine != null) StopCoroutine(returnRoutine);
            if (shimmerRoutine != null) StopCoroutine(shimmerRoutine);
            returnRoutine = shimmerRoutine = null;
            if (buttonBody != null) buttonBody.anchoredPosition = restPosition;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanPress(eventData) || buttonBody == null) return;
            if (returnRoutine != null) StopCoroutine(returnRoutine);
            buttonBody.anchoredPosition = restPosition + new Vector2(0f, pressDepthY);
            if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        private bool CanPress(PointerEventData eventData) =>
            isActiveAndEnabled && button != null && button.IsInteractable() &&
            eventData.button == PointerEventData.InputButton.Left;

        private void Release()
        {
            if (buttonBody == null || !isActiveAndEnabled) return;
            if (returnRoutine != null) StopCoroutine(returnRoutine);
            returnRoutine = StartCoroutine(ReturnBody());
        }

        private IEnumerator ReturnBody()
        {
            Vector2 from = buttonBody.anchoredPosition;
            for (float elapsed = 0f; elapsed < returnDuration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01((elapsed + Time.unscaledDeltaTime) /
                    Mathf.Max(0.001f, returnDuration));
                buttonBody.anchoredPosition = Vector2.Lerp(from, restPosition,
                    Mathf.Sin(t * Mathf.PI * 0.5f));
                yield return null;
            }
            buttonBody.anchoredPosition = restPosition;
            returnRoutine = null;
        }

        private IEnumerator Shimmer()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, shimmerDelay));
                RectTransform mask = shimmerRect.parent as RectTransform;
                float width = mask != null ? mask.rect.width : 294f;
                float start = -width * 0.5f - shimmerRect.rect.width;
                float end = width * 0.5f + shimmerRect.rect.width;
                for (float x = start; x < end; x += Mathf.Max(1f, shimmerSpeed) * Time.unscaledDeltaTime)
                {
                    shimmerRect.anchoredPosition = new Vector2(x, shimmerRect.anchoredPosition.y);
                    yield return null;
                }
                shimmerRect.anchoredPosition = new Vector2(start, shimmerRect.anchoredPosition.y);
            }
        }

        public void RefreshText()
        {
            if (titleText != null) titleText.text = MiningLocalization.Text("UPGRADES", "NÂNG CẤP");
            if (expandBadgeText != null)
                expandBadgeText.text = MiningLocalization.Text("EXPAND", "MỞ RỘNG");
            if (subtitleText != null)
                subtitleText.text = MiningLocalization.Text("Pickaxes & Gear | Backpack",
                    "Cúp Đào & Công Cụ | Balo Khai Mỏ");
            if (actionText != null)
                actionText.text = MiningLocalization.Text("OPEN PANEL", "MỞ BẢNG");
        }

        // Deliberately no IPointerClickHandler: MiningUpgradePanel owns Button.onClick and
        // MiningUiPanelCoordinator already animates panel visibility. Calling SetActive here
        // would double-open the panel and break exclusive modal state.
    }
}
