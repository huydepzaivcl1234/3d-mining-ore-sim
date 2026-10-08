using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Presentation for the existing MiningHud purchase Button; never purchases NPCs itself.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class JuicyBuyMinerButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private RectTransform buttonBody;
        [SerializeField] private RectTransform shimmerRect;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI countBadgeText;
        [SerializeField] private TextMeshProUGUI subStatsText;
        [SerializeField] private TextMeshProUGUI priceLabelText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private NpcShop npcShop;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickSfx;
        [SerializeField] private AudioClip buySuccessSfx;
        [SerializeField] private float pressDepthY = -8f;
        [SerializeField] private float returnDuration = 0.08f;
        [SerializeField] private float shimmerSpeed = 600f;
        [SerializeField] private float shimmerDelay = 3.5f;

        private Button button;
        private Vector2 restingPosition;
        private Vector3 restingScale;
        private Coroutine returnRoutine;
        private Coroutine shimmerRoutine;
        private Coroutine punchRoutine;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (buttonBody == null) buttonBody = transform.Find("Button_Body") as RectTransform;
            if (buttonBody != null)
            {
                restingPosition = buttonBody.anchoredPosition;
                restingScale = buttonBody.localScale;
            }
        }

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged += RefreshUI;
            if (npcShop != null)
            {
                npcShop.NpcCountChanged += OnMinerCountChanged;
                npcShop.NpcPurchased += OnMinerPurchased;
            }
            RefreshUI();
            if (shimmerRect != null) shimmerRoutine = StartCoroutine(Shimmer());
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= RefreshUI;
            if (npcShop != null)
            {
                npcShop.NpcCountChanged -= OnMinerCountChanged;
                npcShop.NpcPurchased -= OnMinerPurchased;
            }
            if (returnRoutine != null) StopCoroutine(returnRoutine);
            if (punchRoutine != null) StopCoroutine(punchRoutine);
            if (shimmerRoutine != null) StopCoroutine(shimmerRoutine);
            returnRoutine = shimmerRoutine = punchRoutine = null;
            if (buttonBody != null)
            {
                buttonBody.anchoredPosition = restingPosition;
                buttonBody.localScale = restingScale;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanPress(eventData) || buttonBody == null) return;
            if (returnRoutine != null) StopCoroutine(returnRoutine);
            buttonBody.anchoredPosition = restingPosition + new Vector2(0f, pressDepthY);
            Play(clickSfx);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        public void OnPointerClick(PointerEventData eventData)
        {
            // Button.onClick is owned by MiningHud; invoking TryBuyNpc here would buy twice.
            if (CanPress(eventData) && buttonBody != null)
            {
                if (punchRoutine != null) StopCoroutine(punchRoutine);
                punchRoutine = StartCoroutine(Punch());
            }
        }

        private bool CanPress(PointerEventData eventData) =>
            isActiveAndEnabled && button != null && button.IsInteractable() &&
            eventData.button == PointerEventData.InputButton.Left;

        private void Release()
        {
            if (buttonBody == null || !isActiveAndEnabled) return;
            if (returnRoutine != null) StopCoroutine(returnRoutine);
            returnRoutine = StartCoroutine(Return());
        }

        private IEnumerator Return()
        {
            Vector2 from = buttonBody.anchoredPosition;
            for (float elapsed = 0f; elapsed < returnDuration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01((elapsed + Time.unscaledDeltaTime) / Mathf.Max(0.001f, returnDuration));
                buttonBody.anchoredPosition = Vector2.Lerp(from, restingPosition,
                    Mathf.Sin(t * Mathf.PI * 0.5f));
                yield return null;
            }
            buttonBody.anchoredPosition = restingPosition;
            returnRoutine = null;
        }

        private IEnumerator Punch()
        {
            Vector3 pulse = new(restingScale.x * 1.04f, restingScale.y * 0.96f, restingScale.z);
            for (float elapsed = 0f; elapsed < 0.08f; elapsed += Time.unscaledDeltaTime)
            {
                buttonBody.localScale = Vector3.Lerp(restingScale, pulse,
                    Mathf.Clamp01(elapsed / 0.08f));
                yield return null;
            }
            for (float elapsed = 0f; elapsed < 0.12f; elapsed += Time.unscaledDeltaTime)
            {
                buttonBody.localScale = Vector3.Lerp(pulse, restingScale,
                    Mathf.Clamp01(elapsed / 0.12f));
                yield return null;
            }
            buttonBody.localScale = restingScale;
            punchRoutine = null;
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

        private void OnMinerCountChanged(int count) => RefreshUI();

        private void OnMinerPurchased(MiningNpc miner)
        {
            Play(buySuccessSfx);
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (titleText != null) titleText.text = MiningLocalization.Text("HIRE MINER", "THUÊ THỢ MỎ");
            if (priceLabelText != null) priceLabelText.text = MiningLocalization.Text("HIRE COST", "GIÁ THUÊ");
            if (countBadgeText != null) countBadgeText.text = $"x{(npcShop != null ? npcShop.PurchasedCount + 1 : 1)}";
            NpcData data = npcShop != null ? npcShop.NpcData : null;
            int oresPerMinute = data != null
                ? Mathf.RoundToInt(data.MiningPower * 60f / Mathf.Max(0.01f, data.SecondsPerHit)) : 0;
            if (subStatsText != null)
                subStatsText.text = string.Format(MiningLocalization.TextKey(
                    "MINING_RATE_FORMAT", "MINING RATE: +{0} ORE/MIN"), oresPerMinute);
            if (costText != null)
                costText.text = npcShop != null ? MiningMoneyFormatter.Format(npcShop.NpcCost) : "—";
        }

        private void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }
    }
}
