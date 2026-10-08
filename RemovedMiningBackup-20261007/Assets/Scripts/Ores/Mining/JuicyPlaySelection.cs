using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Presents the scene-authored Load Game/New Game choice before the existing menu transition.
    /// Save ownership remains with the project's current gameplay systems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JuicyPlaySelection : MonoBehaviour
    {
        private const string SaveMarkerKey = "MiningSimulator.SaveExists.v1";
        private const string RebirthSaveKey = "MiningSimulator.RebirthCount.v1";
        private const string InventorySaveKey = "MiningSimulator.Inventory.v1";
        private const string QuestSaveKey = "MiningSimulator.Quests.v1";
        private const string AchievementSaveKey = "MiningSimulator.Achievements.v1";

        [Header("Controller")]
        [SerializeField] private MiningMainMenu mainMenu;
        [SerializeField] private MiningRebirthSystem rebirthSystem;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private NpcProgressionSystem progressionSystem;

        [Header("Authored UI")]
        [SerializeField] private RectTransform playButtonRect;
        [SerializeField] private RectTransform overlayRect;
        [SerializeField] private RectTransform selectionPanelRect;
        [SerializeField] private CanvasGroup selectionCanvasGroup;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Image loadGameButtonImage;

        [Header("Localized Labels")]
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI loadGameLabel;
        [SerializeField] private TextMeshProUGUI saveStatsLabel;
        [SerializeField] private TextMeshProUGUI newGameLabel;
        [SerializeField] private TextMeshProUGUI newGameDescriptionLabel;

        [Header("Animation")]
        [Min(0.05f), SerializeField] private float morphDuration = 0.32f;

        private Coroutine animationRoutine;
        private Vector3 playButtonScale = Vector3.one;
        private Vector3 panelScale = Vector3.one;
        private bool hasSaveData;

        private void Awake()
        {
            ResolveReferences();
            if (playButtonRect != null) playButtonScale = playButtonRect.localScale;
            if (selectionPanelRect != null) panelScale = selectionPanelRect.localScale;
            HideImmediately();
        }

        private void OnEnable()
        {
            RemoveListeners();
            loadGameButton?.onClick.AddListener(HandleLoadGame);
            newGameButton?.onClick.AddListener(HandleNewGame);
            backButton?.onClick.AddListener(HandleBack);
            MiningLocalization.LanguageChanged -= Refresh;
            MiningLocalization.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            RemoveListeners();
            MiningLocalization.LanguageChanged -= Refresh;
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }
            HideImmediately();
        }

        /// <summary>Called by the existing Play button. Returns false only if UI wiring is missing.</summary>
        public bool HandlePlayRequest()
        {
            ResolveReferences();
            Refresh();
            if (hasSaveData)
            {
                return TryOpen();
            }

            // A first-time player goes straight into a clean game. Once gameplay starts,
            // the save marker makes future Play clicks open the two-choice panel.
            HandleNewGame();
            return true;
        }

        /// <summary>Records that gameplay has started without altering any progression data.</summary>
        public void MarkGameStarted()
        {
            MarkSaveExists();
            hasSaveData = true;
        }

        /// <summary>Opens the authored choice panel for an existing game.</summary>
        public bool TryOpen()
        {
            ResolveReferences();
            if (overlayRect == null || selectionPanelRect == null ||
                selectionCanvasGroup == null || playButtonRect == null)
            {
                Debug.LogWarning("Play Selection is not fully authored. Run Mining Simulator > UI > Build Play Selection.", this);
                return false;
            }

            Refresh();
            StopAnimation();
            // Keep the authored hierarchy active and control visibility through CanvasGroup.
            // This also repairs scenes where the Overlay was manually disabled in Edit Mode.
            overlayRect.gameObject.SetActive(true);
            selectionCanvasGroup.alpha = 0f;
            selectionCanvasGroup.interactable = false;
            selectionCanvasGroup.blocksRaycasts = true;
            animationRoutine = StartCoroutine(AnimateOpen());
            return true;
        }

        private void HandleLoadGame()
        {
            if (!hasSaveData) return;
            MarkSaveExists();
            BeginGameplay();
        }

        private void HandleNewGame()
        {
            ResolveReferences();
            if (rebirthSystem == null)
            {
                Debug.LogError("New Game requires MiningRebirthSystem so all progression can be reset safely.", this);
                return;
            }

            rebirthSystem.ResetAllProgress();
            MarkSaveExists();
            BeginGameplay();
        }

        private void HandleBack()
        {
            StopAnimation();
            animationRoutine = StartCoroutine(AnimateClose());
        }

        private void BeginGameplay()
        {
            StopAnimation();
            if (selectionCanvasGroup != null)
            {
                selectionCanvasGroup.interactable = false;
                selectionCanvasGroup.blocksRaycasts = true;
            }
            mainMenu?.PlaySelectedGame();
        }

        private IEnumerator AnimateOpen()
        {
            float collapseDuration = morphDuration * 0.35f;
            float elapsed = 0f;
            while (elapsed < collapseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / collapseDuration);
                playButtonRect.localScale = Vector3.LerpUnclamped(playButtonScale,
                    Vector3.zero, EaseInCubic(t));
                yield return null;
            }
            playButtonRect.gameObject.SetActive(false);

            selectionPanelRect.localScale = panelScale * 0.72f;
            selectionCanvasGroup.alpha = 0f;
            selectionCanvasGroup.interactable = false;
            selectionCanvasGroup.blocksRaycasts = true;

            float expandDuration = morphDuration * 0.65f;
            elapsed = 0f;
            while (elapsed < expandDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / expandDuration);
                selectionPanelRect.localScale = Vector3.LerpUnclamped(panelScale * 0.72f,
                    panelScale, EaseOutBack(t));
                selectionCanvasGroup.alpha = EaseOutCubic(t);
                yield return null;
            }

            selectionPanelRect.localScale = panelScale;
            selectionCanvasGroup.alpha = 1f;
            selectionCanvasGroup.interactable = true;
            selectionCanvasGroup.blocksRaycasts = true;
            SelectButton(hasSaveData ? loadGameButton : newGameButton);
            animationRoutine = null;
        }

        private IEnumerator AnimateClose()
        {
            selectionCanvasGroup.interactable = false;
            selectionCanvasGroup.blocksRaycasts = true;
            float closeDuration = morphDuration * 0.4f;
            float elapsed = 0f;
            while (elapsed < closeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / closeDuration);
                selectionPanelRect.localScale = Vector3.LerpUnclamped(panelScale,
                    panelScale * 0.72f, EaseInCubic(t));
                selectionCanvasGroup.alpha = 1f - t;
                yield return null;
            }

            selectionCanvasGroup.alpha = 0f;
            selectionCanvasGroup.interactable = false;
            selectionCanvasGroup.blocksRaycasts = false;
            playButtonRect.gameObject.SetActive(true);
            playButtonRect.localScale = Vector3.zero;
            float expandDuration = morphDuration * 0.6f;
            elapsed = 0f;
            while (elapsed < expandDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / expandDuration);
                playButtonRect.localScale = Vector3.LerpUnclamped(Vector3.zero,
                    playButtonScale, EaseOutBack(t));
                yield return null;
            }

            playButtonRect.localScale = playButtonScale;
            SelectButton(playButtonRect.GetComponent<Button>());
            animationRoutine = null;
        }

        private void Refresh()
        {
            ResolveReferences();
            hasSaveData = DetectSaveData();
            SetText(titleLabel, "SELECT GAME", "CHỌN LỐI CHƠI");
            SetText(loadGameLabel, "LOAD GAME", "TẢI TRÒ CHƠI");
            SetText(newGameLabel, "NEW GAME", "TRÒ CHƠI MỚI");
            SetText(newGameDescriptionLabel, "Start a fresh mining journey",
                "Bắt đầu hành trình khai mỏ mới");

            if (saveStatsLabel != null)
            {
                if (hasSaveData)
                {
                    int level = progressionSystem != null ? progressionSystem.CurrentLevel : 1;
                    float gems = wallet != null ? wallet.CurrentGems : 0f;
                    string english = $"LEVEL {level}  |  {MiningMoneyFormatter.Format(gems)} GEMS";
                    string vietnamese = $"CẤP {level}  |  {MiningMoneyFormatter.Format(gems)} NGỌC";
                    saveStatsLabel.text = MiningLocalization.IsEnglish ? english : vietnamese;
                }
                else
                {
                    saveStatsLabel.text = MiningLocalization.Text("NO SAVE DATA",
                        "CHƯA CÓ DỮ LIỆU LƯU");
                }
            }

            if (loadGameButton != null) loadGameButton.interactable = hasSaveData;
            if (loadGameButtonImage != null)
            {
                loadGameButtonImage.color = hasSaveData
                    ? Color.white
                    : new Color(0.52f, 0.52f, 0.52f, 0.72f);
            }
        }

        private bool DetectSaveData()
        {
            string gemKey = wallet != null && wallet.GameData != null
                ? wallet.GameData.GemSaveKey
                : MiningGameData.DefaultGemSaveKey;
            return PlayerPrefs.GetInt(SaveMarkerKey, 0) == 1 ||
                   PlayerPrefs.HasKey(gemKey) ||
                   PlayerPrefs.HasKey(RebirthSaveKey) ||
                   PlayerPrefs.HasKey(InventorySaveKey) ||
                   PlayerPrefs.HasKey(QuestSaveKey) ||
                   PlayerPrefs.HasKey(AchievementSaveKey) ||
                   (rebirthSystem != null && rebirthSystem.CompletedRebirths > 0) ||
                   (progressionSystem != null && progressionSystem.CurrentLevel > 1) ||
                   (wallet != null && wallet.CurrentGems > 0f);
        }

        private void ResolveReferences()
        {
            mainMenu ??= GetComponent<MiningMainMenu>();
            rebirthSystem ??= FindFirstObjectByType<MiningRebirthSystem>(FindObjectsInactive.Include);
            wallet ??= FindFirstObjectByType<PlayerWallet>(FindObjectsInactive.Include);
            progressionSystem ??= FindFirstObjectByType<NpcProgressionSystem>(
                FindObjectsInactive.Include);
        }

        private void HideImmediately()
        {
            if (selectionCanvasGroup != null)
            {
                selectionCanvasGroup.alpha = 0f;
                selectionCanvasGroup.interactable = false;
                selectionCanvasGroup.blocksRaycasts = false;
            }
            if (selectionPanelRect != null) selectionPanelRect.localScale = panelScale;
            if (overlayRect != null) overlayRect.gameObject.SetActive(true);
            if (playButtonRect != null)
            {
                playButtonRect.gameObject.SetActive(true);
                playButtonRect.localScale = playButtonScale;
            }
        }

        private void StopAnimation()
        {
            if (animationRoutine == null) return;
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        private static void MarkSaveExists()
        {
            PlayerPrefs.SetInt(SaveMarkerKey, 1);
            PlayerPrefs.Save();
        }

        private void RemoveListeners()
        {
            loadGameButton?.onClick.RemoveListener(HandleLoadGame);
            newGameButton?.onClick.RemoveListener(HandleNewGame);
            backButton?.onClick.RemoveListener(HandleBack);
        }

        private static void SetText(TextMeshProUGUI label, string english, string vietnamese)
        {
            if (label != null) label.text = MiningLocalization.Text(english, vietnamese);
        }

        private static void SelectButton(Button button)
        {
            if (button != null) EventSystem.current?.SetSelectedGameObject(button.gameObject);
        }

        private static float EaseInCubic(float t) => t * t * t;
        private static float EaseOutCubic(float t)
        {
            float value = 1f - t;
            return 1f - value * value * value;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
