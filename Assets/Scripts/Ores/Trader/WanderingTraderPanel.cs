using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Authored buy/sell presentation for the wandering trader.</summary>
    public sealed class WanderingTraderPanel : MonoBehaviour
    {
        public const int CurrentLayoutVersion = 2;
        private const string RuntimePanelName = "Wandering Trader Runtime Panel";
        private const int VisibleOfferCount = 3;

        private static readonly Color Leather = new(0.23f, 0.09f, 0.025f, 0.99f);
        private static readonly Color HeaderLeather = new(0.36f, 0.16f, 0.055f, 1f);
        private static readonly Color DarkWell = new(0.035f, 0.009f, 0.002f, 1f);
        private static readonly Color Gold = new(0.79f, 0.49f, 0.13f, 1f);
        private static readonly Color PaleGold = new(1f, 0.86f, 0.48f, 1f);
        private static readonly Color Green = new(0.08f, 0.48f, 0.16f, 1f);
        private static readonly Color DisabledGreen = new(0.18f, 0.22f, 0.16f, 1f);

        [Header("Authored Scene Layout")]
        [SerializeField] private int layoutVersion;
        [SerializeField] private RectTransform cardRect;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text noteText;
        [SerializeField] private TMP_Text emptyPageText;
        [SerializeField] private Button pageButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button headerCloseButton;

        [Header("Currency Icons")]
        [Tooltip("Optional Coin sprite shown in buy offers. COIN text is used when empty.")]
        [SerializeField] private Sprite coinIcon;
        [Tooltip("Optional Gem sprite shown in buy and sell offers. GEM text is used when empty.")]
        [SerializeField] private Sprite gemIcon;

        [Header("Smooth Animation")]
        [Tooltip("Seconds used by the panel fade-and-pop opening animation.")]
        [Min(0.01f), SerializeField] private float panelOpenDuration = 0.24f;
        [Tooltip("Seconds used by the panel fade-and-shrink closing animation.")]
        [Min(0.01f), SerializeField] private float panelCloseDuration = 0.16f;
        [Tooltip("Starting scale used when the trader panel pops open.")]
        [Range(0.5f, 1f), SerializeField] private float panelStartScale = 0.84f;
        [Tooltip("Seconds used to fade the current Buy or Sell page out.")]
        [Min(0.01f), SerializeField] private float pageFadeOutDuration = 0.1f;
        [Tooltip("Seconds used to fade the next Buy or Sell page in.")]
        [Min(0.01f), SerializeField] private float pageFadeInDuration = 0.18f;
        [Tooltip("Small scale dip used between Buy and Sell pages.")]
        [Range(0.8f, 1f), SerializeField] private float pageSwapScale = 0.96f;

        // Kept only so older serialized scenes do not lose their references during upgrade.
        [SerializeField, HideInInspector] private TMP_Text offerText;
        [SerializeField, HideInInspector] private Button tradeButton;

        private readonly List<OfferRowView> offerRows = new(VisibleOfferCount);
        private IReadOnlyList<WanderingTraderSystem.TraderOffer> buyOffers;
        private IReadOnlyList<WanderingTraderSystem.TraderOffer> sellOffers;
        private PlayerWallet wallet;
        private MiningItemSystem itemSystem;
        private Action closedAction;
        private Func<WanderingTraderSystem.TraderOffer, bool> canAcceptOfferAction;
        private Func<WanderingTraderSystem.TraderOffer, bool> acceptOfferAction;
        private float resetAt;
        private bool isOpen;
        private bool showingSellPage;
        private bool sourcesSubscribed;
        private CanvasGroup rootCanvasGroup;
        private CanvasGroup cardCanvasGroup;
        private Coroutine panelTransitionRoutine;
        private Coroutine pageTransitionRoutine;

        public bool IsOpen => isOpen;
        public bool HasCompleteSceneLayout => layoutVersion >= CurrentLayoutVersion &&
                                              cardRect != null && offerRows.Count == VisibleOfferCount;

        private sealed class OfferRowView
        {
            public GameObject Root;
            public TMP_Text Arrow;
            public Image GiveIcon;
            public TMP_Text GiveFallback;
            public TMP_Text GiveHeading;
            public TMP_Text GiveValue;
            public Image ReceiveIcon;
            public TMP_Text ReceiveFallback;
            public TMP_Text ReceiveHeading;
            public TMP_Text ReceiveValue;
            public Button ActionButton;
            public Image ActionBackground;
            public TMP_Text ActionLabel;
            public WanderingTraderSystem.TraderOffer Offer;
        }

        private void Awake()
        {
            EnsureCompleteLayout();
            BindStaticButtons();
        }

        private void OnEnable()
        {
            EnsureCompleteLayout();
            BindStaticButtons();
            FindRuntimeSources();
            SubscribeSources();
        }

        private void OnDisable()
        {
            UnsubscribeSources();
            panelTransitionRoutine = null;
            pageTransitionRoutine = null;
        }

        public static WanderingTraderPanel EnsureRuntime()
        {
            WanderingTraderPanel existing = FindFirstObjectByType<WanderingTraderPanel>(
                FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.EnsureCompleteLayout();
                return existing;
            }

            Canvas canvas = FindHudCanvas();
            if (canvas == null) return null;
            GameObject root = CreateUiObject(RuntimePanelName, canvas.transform);
            Stretch(root.GetComponent<RectTransform>());
            Image backdrop = root.AddComponent<Image>();
            backdrop.color = new Color(0.015f, 0.005f, 0.02f, 0.78f);
            Button backdropButton = root.AddComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            root.AddComponent<MiningButtonSfx>();
            WanderingTraderPanel panel = root.AddComponent<WanderingTraderPanel>();
            backdropButton.onClick.AddListener(panel.Hide);
            root.SetActive(false);
            return panel;
        }

        public void Show(IReadOnlyList<WanderingTraderSystem.TraderOffer> currentBuyOffers,
            IReadOnlyList<WanderingTraderSystem.TraderOffer> currentSellOffers,
            float secondsRemaining,
            Func<WanderingTraderSystem.TraderOffer, bool> canAccept,
            Func<WanderingTraderSystem.TraderOffer, bool> onAccept,
            Action onClosed)
        {
            EnsureCompleteLayout();
            buyOffers = currentBuyOffers;
            sellOffers = currentSellOffers;
            canAcceptOfferAction = canAccept;
            acceptOfferAction = onAccept;
            closedAction = onClosed;
            resetAt = Time.unscaledTime + Mathf.Max(0f, secondsRemaining);
            showingSellPage = false;
            isOpen = true;
            FindRuntimeSources();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            SubscribeSources();
            RefreshBalance();
            RenderCurrentPage();
            UpdateCountdown();
            StartPanelOpenAnimation();
        }

        public void Hide()
        {
            if (!isOpen) return;
            isOpen = false;
            if (pageTransitionRoutine != null)
            {
                StopCoroutine(pageTransitionRoutine);
                pageTransitionRoutine = null;
            }
            ResetCardVisualState();
            if (!gameObject.activeInHierarchy)
            {
                CompleteHide();
                return;
            }
            if (panelTransitionRoutine != null) StopCoroutine(panelTransitionRoutine);
            panelTransitionRoutine = StartCoroutine(AnimatePanelClosed());
        }

        private void CompleteHide()
        {
            ClearOfferRows();
            Action callback = closedAction;
            closedAction = null;
            canAcceptOfferAction = null;
            acceptOfferAction = null;
            buyOffers = null;
            sellOffers = null;
            gameObject.SetActive(false);
            callback?.Invoke();
        }

        private void Update()
        {
            if (isOpen) UpdateCountdown();
        }

        private void TogglePage()
        {
            if (!isOpen || pageTransitionRoutine != null) return;
            pageTransitionRoutine = StartCoroutine(AnimatePageChange());
        }

        private void StartPanelOpenAnimation()
        {
            rootCanvasGroup ??= GetOrAdd<CanvasGroup>(gameObject);
            cardCanvasGroup ??= cardRect != null ? GetOrAdd<CanvasGroup>(cardRect.gameObject) : null;
            if (pageTransitionRoutine != null)
            {
                StopCoroutine(pageTransitionRoutine);
                pageTransitionRoutine = null;
            }
            ResetCardVisualState();
            if (panelTransitionRoutine != null) StopCoroutine(panelTransitionRoutine);
            panelTransitionRoutine = StartCoroutine(AnimatePanelOpened());
        }

        private IEnumerator AnimatePanelOpened()
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
            if (cardRect != null) cardRect.localScale = Vector3.one * panelStartScale;

            float duration = Mathf.Max(0.01f, panelOpenDuration);
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutBack(progress);
                rootCanvasGroup.alpha = Mathf.Clamp01(progress * 1.35f);
                if (cardRect != null)
                    cardRect.localScale = Vector3.one * Mathf.LerpUnclamped(
                        panelStartScale, 1f, eased);
                yield return null;
            }

            rootCanvasGroup.alpha = 1f;
            rootCanvasGroup.interactable = true;
            rootCanvasGroup.blocksRaycasts = true;
            if (cardRect != null) cardRect.localScale = Vector3.one;
            panelTransitionRoutine = null;
        }

        private IEnumerator AnimatePanelClosed()
        {
            rootCanvasGroup ??= GetOrAdd<CanvasGroup>(gameObject);
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
            float startAlpha = rootCanvasGroup.alpha;
            Vector3 startScale = cardRect != null ? cardRect.localScale : Vector3.one;
            float duration = Mathf.Max(0.01f, panelCloseDuration);

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = progress * progress;
                rootCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
                if (cardRect != null)
                    cardRect.localScale = Vector3.Lerp(startScale,
                        Vector3.one * panelStartScale, eased);
                yield return null;
            }

            panelTransitionRoutine = null;
            CompleteHide();
        }

        private IEnumerator AnimatePageChange()
        {
            if (cardRect == null)
            {
                showingSellPage = !showingSellPage;
                RenderCurrentPage();
                pageTransitionRoutine = null;
                yield break;
            }

            cardCanvasGroup ??= GetOrAdd<CanvasGroup>(cardRect.gameObject);
            cardCanvasGroup.interactable = false;
            float outDuration = Mathf.Max(0.01f, pageFadeOutDuration);
            for (float elapsed = 0f; elapsed < outDuration; elapsed += Time.unscaledDeltaTime)
            {
                float progress = Smooth01(elapsed / outDuration);
                cardCanvasGroup.alpha = Mathf.Lerp(1f, 0.35f, progress);
                cardRect.localScale = Vector3.one * Mathf.Lerp(1f, pageSwapScale, progress);
                yield return null;
            }

            showingSellPage = !showingSellPage;
            RenderCurrentPage();
            cardCanvasGroup.alpha = 0.35f;
            cardRect.localScale = Vector3.one * pageSwapScale;

            float inDuration = Mathf.Max(0.01f, pageFadeInDuration);
            for (float elapsed = 0f; elapsed < inDuration; elapsed += Time.unscaledDeltaTime)
            {
                float progress = Smooth01(elapsed / inDuration);
                cardCanvasGroup.alpha = Mathf.Lerp(0.35f, 1f, progress);
                cardRect.localScale = Vector3.one * Mathf.Lerp(pageSwapScale, 1f, progress);
                yield return null;
            }

            ResetCardVisualState();
            pageTransitionRoutine = null;
        }

        private void ResetCardVisualState()
        {
            if (cardRect != null) cardRect.localScale = Vector3.one;
            if (cardCanvasGroup == null && cardRect != null)
                cardCanvasGroup = GetOrAdd<CanvasGroup>(cardRect.gameObject);
            if (cardCanvasGroup == null) return;
            cardCanvasGroup.alpha = 1f;
            cardCanvasGroup.interactable = true;
            cardCanvasGroup.blocksRaycasts = true;
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static float EaseOutBack(float value)
        {
            value = Mathf.Clamp01(value) - 1f;
            const float overshoot = 1.25f;
            return 1f + value * value * ((overshoot + 1f) * value + overshoot);
        }

        private void RenderCurrentPage()
        {
            IReadOnlyList<WanderingTraderSystem.TraderOffer> offers =
                showingSellPage ? sellOffers : buyOffers;
            string pageTitle = showingSellPage ? "BÁN VẬT PHẨM" : "MUA VẬT PHẨM";
            if (subtitleText != null) subtitleText.text = pageTitle;
            if (noteText != null)
            {
                noteText.text = showingSellPage
                    ? "BÁN VẬT PHẨM TRONG TÚI ĐỂ NHẬN GEM"
                    : "ƯU ĐÃI ĐƯỢC LÀM MỚI NGẪU NHIÊN";
            }
            SetButtonLabel(pageButton, showingSellPage ? "<  TRANG MUA" : "TRANG BÁN  >");

            int count = offers != null ? Mathf.Min(VisibleOfferCount, offers.Count) : 0;
            if (emptyPageText != null)
            {
                emptyPageText.gameObject.SetActive(count == 0);
                emptyPageText.text = showingSellPage
                    ? "TÚI ĐỒ CHƯA CÓ VẬT PHẨM ĐỂ BÁN"
                    : "CHƯA CÓ ƯU ĐÃI";
            }

            for (int index = 0; index < offerRows.Count; index++)
            {
                OfferRowView view = offerRows[index];
                bool visible = index < count;
                view.Root.SetActive(visible);
                view.ActionButton.onClick.RemoveAllListeners();
                if (!visible) continue;
                WanderingTraderSystem.TraderOffer offer = offers[index];
                ConfigureOfferRow(view, offer);
                view.ActionButton.onClick.AddListener(() => TryAccept(offer));
                view.ActionButton.GetComponent<MiningButtonSfx>()?.RefreshBinding();
            }
            RefreshOfferButtons();
        }

        private void ConfigureOfferRow(OfferRowView view,
            WanderingTraderSystem.TraderOffer offer)
        {
            view.Offer = offer;
            bool isBuy = offer.OfferType == WanderingTraderSystem.TraderOfferType.BuyItem;
            if (isBuy)
            {
                string currency = offer.Currency == WanderingTraderSystem.TraderCurrency.Gem
                    ? "GEM"
                    : "COIN";
                Sprite currencyIcon = offer.Currency == WanderingTraderSystem.TraderCurrency.Gem
                    ? gemIcon
                    : coinIcon;
                SetWell(view.GiveIcon, view.GiveFallback, view.GiveHeading, view.GiveValue,
                    currencyIcon, currency, "TRẢ", $"{currency} {FormatAmount(offer.Price)}");
                SetWell(view.ReceiveIcon, view.ReceiveFallback, view.ReceiveHeading,
                    view.ReceiveValue, offer.Item.InventoryIcon, offer.Item.IconFallback,
                    "NHẬN", $"{offer.Item.DisplayName} x{offer.ItemAmount}");
                SetButtonLabel(view.ActionButton, "MUA");
            }
            else
            {
                SetWell(view.GiveIcon, view.GiveFallback, view.GiveHeading, view.GiveValue,
                    offer.Item.InventoryIcon, offer.Item.IconFallback,
                    "BÁN", $"{offer.Item.DisplayName} x{offer.ItemAmount}");
                SetWell(view.ReceiveIcon, view.ReceiveFallback, view.ReceiveHeading,
                    view.ReceiveValue, gemIcon, "GEM", "NHẬN",
                    $"GEM {FormatAmount(offer.Price)}");
                SetButtonLabel(view.ActionButton, "BÁN");
            }
            if (view.Arrow != null) view.Arrow.text = ">";
        }

        private static void SetWell(Image icon, TMP_Text fallback, TMP_Text heading,
            TMP_Text value, Sprite sprite, string fallbackText, string headingText,
            string valueText)
        {
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.color = sprite != null ? Color.white : new Color(1f, 0.72f, 0.2f);
            }
            if (fallback != null)
            {
                fallback.gameObject.SetActive(sprite == null);
                fallback.text = string.IsNullOrWhiteSpace(fallbackText) ? "?" : fallbackText;
                fallback.fontSize = fallback.text.Length > 2 ? 10f : 20f;
            }
            if (heading != null) heading.text = headingText;
            if (value != null) value.text = valueText;
        }

        private void TryAccept(WanderingTraderSystem.TraderOffer offer)
        {
            if (acceptOfferAction != null && acceptOfferAction(offer))
            {
                Hide();
                return;
            }
            if (feedbackText != null)
            {
                feedbackText.text = offer.OfferType == WanderingTraderSystem.TraderOfferType.SellItem
                    ? "KHÔNG ĐỦ VẬT PHẨM"
                    : "KHÔNG ĐỦ TIỀN HOẶC TÚI ĐỒ ĐÃ ĐẦY";
            }
            RefreshBalance();
            RefreshOfferButtons();
        }

        private void UpdateCountdown()
        {
            if (feedbackText == null) return;
            int remaining = Mathf.CeilToInt(Mathf.Max(0f, resetAt - Time.unscaledTime));
            feedbackText.text = $"ĐỔI HÀNG SAU  {remaining / 60:00}:{remaining % 60:00}";
        }

        private void RefreshOfferButtons()
        {
            foreach (OfferRowView view in offerRows)
            {
                if (view?.ActionButton == null || !view.Root.activeSelf) continue;
                bool canTrade = canAcceptOfferAction == null || canAcceptOfferAction(view.Offer);
                view.ActionButton.interactable = canTrade;
                if (view.ActionBackground != null)
                    view.ActionBackground.color = canTrade ? Green : DisabledGreen;
                if (view.ActionLabel != null)
                    view.ActionLabel.color = canTrade ? Color.white :
                        new Color(0.62f, 0.62f, 0.58f, 1f);
            }
        }

        private void RefreshBalance()
        {
            if (balanceText == null) return;
            float money = wallet != null ? wallet.CurrentMoney : 0f;
            float gems = wallet != null ? wallet.CurrentGems : 0f;
            balanceText.text = $"COIN {MiningMoneyFormatter.Format(money)}   |   GEM {MiningMoneyFormatter.Format(gems)}";
        }

        private static string FormatAmount(float value) => MiningMoneyFormatter.Format(value);

        private void FindRuntimeSources()
        {
            wallet ??= FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            itemSystem ??= FindFirstObjectByType<MiningItemSystem>(FindObjectsInactive.Include);
        }

        private void SubscribeSources()
        {
            if (sourcesSubscribed) return;
            if (wallet != null)
            {
                wallet.MoneyChanged += HandleBalanceChanged;
                wallet.GemsChanged += HandleBalanceChanged;
            }
            if (itemSystem != null) itemSystem.InventoryChanged += HandleInventoryChanged;
            sourcesSubscribed = true;
        }

        private void UnsubscribeSources()
        {
            if (!sourcesSubscribed) return;
            if (wallet != null)
            {
                wallet.MoneyChanged -= HandleBalanceChanged;
                wallet.GemsChanged -= HandleBalanceChanged;
            }
            if (itemSystem != null) itemSystem.InventoryChanged -= HandleInventoryChanged;
            sourcesSubscribed = false;
        }

        private void HandleBalanceChanged(float value)
        {
            RefreshBalance();
            RefreshOfferButtons();
        }

        private void HandleInventoryChanged() => RefreshOfferButtons();

        private void ClearOfferRows()
        {
            foreach (OfferRowView view in offerRows)
            {
                if (view?.ActionButton != null) view.ActionButton.onClick.RemoveAllListeners();
                if (view?.Root != null) view.Root.SetActive(false);
            }
        }

        private void BindStaticButtons()
        {
            BindButton(closeButton, Hide);
            BindButton(headerCloseButton, Hide);
            BindButton(pageButton, TogglePage);
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        /// <summary>Used by the editor setup menu to author every card and child in the Scene.</summary>
        public void RebuildCompleteSceneLayout()
        {
            offerRows.Clear();
            for (int index = transform.childCount - 1; index >= 0; index--)
            {
                GameObject child = transform.GetChild(index).gameObject;
                if (Application.isPlaying)
                {
                    child.SetActive(false);
                    Destroy(child);
                }
                else DestroyImmediate(child);
            }

            Image backdrop = GetOrAdd<Image>(gameObject);
            backdrop.color = new Color(0.015f, 0.005f, 0.02f, 0.78f);
            rootCanvasGroup = GetOrAdd<CanvasGroup>(gameObject);
            BuildCompleteLayout();
            layoutVersion = CurrentLayoutVersion;
            BindStaticButtons();
        }

        public void ConfigureSceneUi(TMP_Text title, TMP_Text offer, TMP_Text feedback,
            Button trade, Button close)
        {
            titleText = title;
            offerText = offer;
            feedbackText = feedback;
            tradeButton = trade;
            closeButton = close;
            EnsureCompleteLayout();
        }

        private void EnsureCompleteLayout()
        {
            if (Application.isPlaying && layoutVersion < CurrentLayoutVersion)
            {
                RebuildCompleteSceneLayout();
                return;
            }
            if (cardRect == null)
            {
                Transform existing = transform.Find("Offer Card") ?? transform.Find("Trade Card");
                cardRect = existing != null ? existing.GetComponent<RectTransform>() : null;
            }
            if (cardRect == null)
            {
                BuildCompleteLayout();
                layoutVersion = CurrentLayoutVersion;
                return;
            }

            ResolveStaticReferences();
            ResolveOfferRows();
            if (offerRows.Count != VisibleOfferCount)
            {
                for (int index = offerRows.Count; index < VisibleOfferCount; index++)
                    offerRows.Add(BuildOfferRow(cardRect, index));
            }
            ApplyStaticStyle();
        }

        private void BuildCompleteLayout()
        {
            GameObject card = CreateUiObject("Offer Card", transform);
            cardRect = card.GetComponent<RectTransform>();
            ConfigureCenteredRect(cardRect, Vector2.zero, new Vector2(620f, 540f));
            GetOrAdd<Image>(card).color = Leather;
            cardCanvasGroup = GetOrAdd<CanvasGroup>(card);
            AddOutline(card, Gold, new Vector2(3f, -3f));

            RectTransform innerFrame = EnsureImage("Inner Frame", cardRect, Color.clear,
                Vector2.zero, new Vector2(588f, 508f));
            AddOutline(innerFrame.gameObject,
                new Color(0.92f, 0.69f, 0.45f, 0.35f), new Vector2(1f, -1f));
            RectTransform headerBand = EnsureImage("Header Band", cardRect, HeaderLeather,
                new Vector2(0f, 237f), new Vector2(598f, 66f));
            AddOutline(headerBand.gameObject, new Color(0.55f, 0.29f, 0.08f),
                new Vector2(2f, -2f));
            titleText = EnsureLabel("Title", cardRect, "THƯƠNG NHÂN LANG THANG", 20f,
                new Vector2(0f, 239f), new Vector2(440f, 28f),
                new Color(1f, 0.95f, 0.8f));
            subtitleText = EnsureLabel("Subtitle", cardRect, "MUA VẬT PHẨM", 10f,
                new Vector2(0f, 218f), new Vector2(400f, 20f),
                new Color(0.86f, 0.64f, 0.39f));

            RectTransform badge = EnsureImage("Trader Badge", cardRect,
                new Color(0.08f, 0.022f, 0.005f), new Vector2(-270f, 237f),
                new Vector2(42f, 42f));
            AddOutline(badge.gameObject, PaleGold, new Vector2(2f, -2f));
            EnsureLabel("Badge Label", badge, "W", 19f, Vector2.zero,
                new Vector2(42f, 42f), PaleGold).fontStyle = FontStyles.Bold;

            headerCloseButton = EnsureButton("Header Close Button", cardRect, "X",
                new Vector2(276f, 237f), new Vector2(34f, 34f),
                new Color(0.16f, 0.035f, 0.008f), 15f);
            AddOutline(headerCloseButton.gameObject, Gold, new Vector2(1.5f, -1.5f));
            feedbackText = EnsureLabel("Countdown", cardRect, "ĐỔI HÀNG SAU  00:00", 12f,
                new Vector2(-168f, 164f), new Vector2(220f, 28f),
                new Color(1f, 0.65f, 0.28f));
            RectTransform timerPill = EnsureImage("Timer Pill", cardRect,
                new Color(0.06f, 0.012f, 0.002f, 0.95f),
                new Vector2(-168f, 164f), new Vector2(220f, 28f));
            timerPill.SetAsFirstSibling();
            AddOutline(timerPill.gameObject, new Color(0.55f, 0.28f, 0.08f),
                new Vector2(1.5f, -1.5f));
            balanceText = EnsureLabel("Player Balance", cardRect,
                "COIN 0   |   GEM 0", 12f, new Vector2(162f, 164f),
                new Vector2(256f, 28f), PaleGold);
            RectTransform balancePill = EnsureImage("Balance Pill", cardRect,
                new Color(0.06f, 0.012f, 0.002f, 0.95f),
                new Vector2(162f, 164f), new Vector2(256f, 28f));
            balancePill.SetAsFirstSibling();
            AddOutline(balancePill.gameObject, Gold, new Vector2(1.5f, -1.5f));

            offerRows.Clear();
            for (int index = 0; index < VisibleOfferCount; index++)
                offerRows.Add(BuildOfferRow(cardRect, index));

            emptyPageText = EnsureLabel("Empty Page", cardRect, string.Empty, 13f,
                new Vector2(0f, 4f), new Vector2(500f, 80f), PaleGold);
            emptyPageText.gameObject.SetActive(false);
            noteText = EnsureLabel("Page Note", cardRect,
                "ƯU ĐÃI ĐƯỢC LÀM MỚI NGẪU NHIÊN", 10f,
                new Vector2(0f, -139f), new Vector2(520f, 24f),
                new Color(0.78f, 0.55f, 0.32f));
            pageButton = EnsureButton("Page Button", cardRect, "TRANG BÁN  >",
                new Vector2(174f, -176f), new Vector2(210f, 36f),
                new Color(0.31f, 0.14f, 0.04f), 13f);
            AddOutline(pageButton.gameObject, Gold, new Vector2(1.5f, -1.5f));
            closeButton = EnsureButton("Close Button", cardRect, "ĐÓNG",
                new Vector2(-112f, -220f), new Vector2(300f, 46f),
                new Color(0.49f, 0.09f, 0.025f), 17f);
            AddOutline(closeButton.gameObject, new Color(0.9f, 0.3f, 0.12f),
                new Vector2(2f, -2f));
            layoutVersion = CurrentLayoutVersion;
            ApplyStaticStyle();
        }

        private OfferRowView BuildOfferRow(Transform parent, int index)
        {
            string rowName = $"Offer Card {index + 1}";
            Transform existing = parent.Find(rowName);
            GameObject row = existing != null ? existing.gameObject : CreateUiObject(rowName, parent);
            ConfigureCenteredRect(row.GetComponent<RectTransform>(),
                new Vector2(0f, 96f - index * 92f), new Vector2(556f, 80f));
            GetOrAdd<Image>(row).color = index == 2
                ? new Color(0.09f, 0.015f, 0.12f, 1f)
                : new Color(0.08f, 0.02f, 0.006f, 1f);
            AddOutline(row, index == 2 ? new Color(0.52f, 0.17f, 0.68f) :
                new Color(0.31f, 0.13f, 0.035f), new Vector2(2f, -2f));

            RectTransform give = EnsureImage("Give Well", row.transform, DarkWell,
                new Vector2(-190f, 0f), new Vector2(160f, 52f));
            RectTransform receive = EnsureImage("Receive Well", row.transform, DarkWell,
                new Vector2(34f, 0f), new Vector2(180f, 52f));
            AddOutline(give.gameObject, new Color(0.45f, 0.19f, 0.055f),
                new Vector2(1.5f, -1.5f));
            AddOutline(receive.gameObject, Gold, new Vector2(1.5f, -1.5f));
            OfferRowView view = new()
            {
                Root = row,
                Arrow = EnsureLabel("Exchange Arrow", row.transform, ">", 25f,
                    new Vector2(-80f, 0f), new Vector2(42f, 42f), PaleGold),
                GiveIcon = EnsureIcon(give),
                GiveFallback = EnsureLabel("Fallback", give, "COIN", 10f,
                    new Vector2(-52f, 0f), new Vector2(44f, 40f), Color.white),
                GiveHeading = EnsureLabel("Heading", give, "TRẢ", 9.5f,
                    new Vector2(23f, 12f), new Vector2(100f, 18f),
                    new Color(0.68f, 0.48f, 0.28f)),
                GiveValue = EnsureLabel("Value", give, "COIN 0", 13f,
                    new Vector2(23f, -8f), new Vector2(100f, 24f), Color.white),
                ReceiveIcon = EnsureIcon(receive),
                ReceiveFallback = EnsureLabel("Fallback", receive, "?", 20f,
                    new Vector2(-62f, 0f), new Vector2(44f, 40f), Color.white),
                ReceiveHeading = EnsureLabel("Heading", receive, "NHẬN", 9.5f,
                    new Vector2(23f, 12f), new Vector2(120f, 18f),
                    new Color(0.68f, 0.48f, 0.28f)),
                ReceiveValue = EnsureLabel("Value", receive, "ITEM x1", 13f,
                    new Vector2(23f, -8f), new Vector2(120f, 24f), Color.white)
            };
            view.ActionButton = EnsureButton("Action Button", row.transform, "MUA",
                new Vector2(204f, 0f), new Vector2(120f, 40f), Green, 14f);
            view.ActionBackground = view.ActionButton.GetComponent<Image>();
            view.ActionLabel = view.ActionButton.GetComponentInChildren<TMP_Text>(true);
            AddOutline(view.ActionButton.gameObject, new Color(0.27f, 0.75f, 0.31f),
                new Vector2(1.5f, -1.5f));
            view.GiveHeading.alignment = view.GiveValue.alignment = TextAlignmentOptions.Left;
            view.ReceiveHeading.alignment = view.ReceiveValue.alignment = TextAlignmentOptions.Left;
            view.GiveValue.enableAutoSizing = view.ReceiveValue.enableAutoSizing = true;
            view.GiveValue.fontSizeMin = view.ReceiveValue.fontSizeMin = 8f;
            view.GiveValue.fontSizeMax = view.ReceiveValue.fontSizeMax = 13f;
            row.SetActive(false);
            return view;
        }

        private static Image EnsureIcon(Transform parent)
        {
            RectTransform iconRect = EnsureImage("Icon", parent, Color.white,
                new Vector2(-52f, 0f), new Vector2(40f, 40f));
            Image icon = iconRect.GetComponent<Image>();
            icon.preserveAspect = true;
            return icon;
        }

        private void ResolveStaticReferences()
        {
            Transform card = cardRect;
            titleText = FindText(card, "Title", titleText);
            subtitleText = FindText(card, "Subtitle", subtitleText);
            feedbackText = FindText(card, "Countdown", feedbackText);
            balanceText = FindText(card, "Player Balance", balanceText);
            noteText = FindText(card, "Page Note", noteText);
            emptyPageText = FindText(card, "Empty Page", emptyPageText);
            pageButton = FindButton(card, "Page Button", pageButton);
            closeButton = FindButton(card, "Close Button", closeButton);
            headerCloseButton = FindButton(card, "Header Close Button", headerCloseButton);
        }

        private void ResolveOfferRows()
        {
            offerRows.Clear();
            for (int index = 0; index < VisibleOfferCount; index++)
            {
                Transform row = cardRect.Find($"Offer Card {index + 1}");
                if (row == null) break;
                offerRows.Add(BuildOfferRow(cardRect, index));
            }
        }

        private void ApplyStaticStyle()
        {
            if (cardRect == null) return;
            ConfigureCenteredRect(cardRect, Vector2.zero, new Vector2(620f, 540f));
            GetOrAdd<Image>(cardRect.gameObject).color = Leather;
            AddOutline(cardRect.gameObject, Gold, new Vector2(3f, -3f));
            if (titleText != null) titleText.fontStyle = FontStyles.Bold;
            if (subtitleText != null) subtitleText.fontStyle = FontStyles.Bold;
            if (feedbackText != null) feedbackText.fontStyle = FontStyles.Bold;
            if (balanceText != null) balanceText.fontStyle = FontStyles.Bold;
            if (noteText != null) noteText.fontStyle = FontStyles.Bold;
            BindStaticButtons();
        }

        private static TMP_Text FindText(Transform parent, string name, TMP_Text fallback)
        {
            Transform child = parent != null ? parent.Find(name) : null;
            return child != null ? child.GetComponent<TMP_Text>() : fallback;
        }

        private static Button FindButton(Transform parent, string name, Button fallback)
        {
            Transform child = parent != null ? parent.Find(name) : null;
            return child != null ? child.GetComponent<Button>() : fallback;
        }

        private static Canvas FindHudCanvas()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Canvas candidate in canvases)
                if (candidate.name.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0)
                    return candidate;
            return canvases.Length > 0 ? canvases[0] : null;
        }

        private static GameObject CreateUiObject(string objectName, Transform parent)
        {
            GameObject value = new(objectName, typeof(RectTransform));
            value.transform.SetParent(parent, false);
            return value;
        }

        private static TMP_Text EnsureLabel(string objectName, Transform parent, string text,
            float fontSize, Vector2 position, Vector2 size, Color color)
        {
            Transform existing = parent.Find(objectName);
            GameObject target = existing != null ? existing.gameObject :
                CreateUiObject(objectName, parent);
            ConfigureCenteredRect(target.GetComponent<RectTransform>(), position, size);
            TextMeshProUGUI label = GetOrAdd<TextMeshProUGUI>(target);
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            return label;
        }

        private static Button EnsureButton(string objectName, Transform parent, string label,
            Vector2 position, Vector2 size, Color color, float fontSize)
        {
            Transform existing = parent.Find(objectName);
            GameObject target = existing != null ? existing.gameObject :
                CreateUiObject(objectName, parent);
            ConfigureCenteredRect(target.GetComponent<RectTransform>(), position, size);
            Image image = GetOrAdd<Image>(target);
            image.color = color;
            Button button = GetOrAdd<Button>(target);
            button.targetGraphic = image;
            GetOrAdd<MiningButtonSfx>(target);
            TMP_Text buttonLabel = EnsureLabel("Label", target.transform, label, fontSize,
                Vector2.zero, size, Color.white);
            buttonLabel.fontStyle = FontStyles.Bold;
            return button;
        }

        private static void SetButtonLabel(Button button, string value)
        {
            if (button == null) return;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = value;
        }

        private static RectTransform EnsureImage(string objectName, Transform parent, Color color,
            Vector2 position, Vector2 size)
        {
            Transform existing = parent.Find(objectName);
            GameObject target = existing != null ? existing.gameObject :
                CreateUiObject(objectName, parent);
            RectTransform rect = target.GetComponent<RectTransform>();
            ConfigureCenteredRect(rect, position, size);
            Image image = GetOrAdd<Image>(target);
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = GetOrAdd<Outline>(target);
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T value = target.GetComponent<T>();
            return value != null ? value : target.AddComponent<T>();
        }

        private static void ConfigureCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
