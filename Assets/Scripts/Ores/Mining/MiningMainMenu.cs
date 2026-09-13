using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Pauses the simulation behind the authored startup menu until Play is pressed.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningMainMenu : MonoBehaviour
    {
        [SerializeField] private MiningMainMenuData data;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform card;
        [SerializeField] private Button playButton;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI subtitleLabel;
        [SerializeField] private TextMeshProUGUI playLabel;

        private Sequence transition;
        private float timeScaleBeforeMenu = 1f;
        private bool ownsGameplayPause;
        private bool closing;

        private void Awake()
        {
            if (!ResolveReferences())
            {
                Debug.LogError("Main Menu is missing authored UI references. Run " +
                    "Mining Simulator > Setup > Create Or Update Main Menu.", this);
                enabled = false;
                return;
            }

            if (!data.ShowOnStart)
            {
                gameObject.SetActive(false);
                return;
            }

            if (data.PauseGameplay)
            {
                timeScaleBeforeMenu = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                ownsGameplayPause = true;
            }

            ShowImmediately();
        }

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged -= RefreshLocalization;
            MiningLocalization.LanguageChanged += RefreshLocalization;

            if (playButton != null)
            {
                playButton.onClick.RemoveListener(Play);
                playButton.onClick.AddListener(Play);
            }

            RefreshLocalization();
        }

        private void Start()
        {
            if (isActiveAndEnabled && data != null && data.ShowOnStart)
            {
                PlayEntrance();
                EventSystem.current?.SetSelectedGameObject(playButton.gameObject);
            }
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= RefreshLocalization;
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(Play);
            }

            StopTransition();
            ReleaseGameplayPause();
        }

        private void OnDestroy()
        {
            ReleaseGameplayPause();
        }

        public void Play()
        {
            if (closing || data == null || canvasGroup == null || card == null)
            {
                return;
            }

            closing = true;
            playButton.interactable = false;
            StopTransition();
            transition = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Custom(this, canvasGroup.alpha, 0f, data.ExitDuration,
                    static (menu, alpha) => menu.canvasGroup.alpha = alpha, Ease.InCubic))
                .Group(Tween.Scale(card, Vector3.one * data.ExitScale,
                    data.ExitDuration, Ease.InBack))
                .OnComplete(this, static menu => menu.FinishPlay());
        }

        private void FinishPlay()
        {
            ReleaseGameplayPause();
            gameObject.SetActive(false);
        }

        private void PlayEntrance()
        {
            StopTransition();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            card.localScale = Vector3.one * data.EntranceStartScale;
            transition = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Custom(this, 0f, 1f, data.EntranceDuration,
                    static (menu, alpha) => menu.canvasGroup.alpha = alpha, Ease.OutCubic))
                .Group(Tween.Scale(card, Vector3.one, data.EntranceDuration, Ease.OutBack));
        }

        private void ShowImmediately()
        {
            closing = false;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            card.localScale = Vector3.one;
            playButton.interactable = true;
        }

        private void RefreshLocalization()
        {
            if (data == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = MiningLocalization.Text(data.EnglishTitle,
                    data.VietnameseTitle);
            }
            if (subtitleLabel != null)
            {
                subtitleLabel.text = MiningLocalization.Text(data.EnglishSubtitle,
                    data.VietnameseSubtitle);
            }
            if (playLabel != null)
            {
                playLabel.text = MiningLocalization.Text(data.EnglishPlayLabel,
                    data.VietnamesePlayLabel);
            }
        }

        private bool ResolveReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
            return data != null && canvasGroup != null && card != null && playButton != null;
        }

        private void StopTransition()
        {
            if (transition.isAlive)
            {
                transition.Stop();
            }
        }

        private void ReleaseGameplayPause()
        {
            if (!ownsGameplayPause)
            {
                return;
            }

            Time.timeScale = timeScaleBeforeMenu;
            ownsGameplayPause = false;
        }
    }
}
