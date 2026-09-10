using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>
    /// Watches the shared mining power (<see cref="NpcProgressionSystem"/>) and, the moment an
    /// ore or Lucky Block variant crosses its required-power threshold, shows a toast
    /// announcing the unlock and guarantees that exact ore/variant is the next one to appear
    /// (spawned right away, or as soon as a slot frees up if the field/board is full).
    /// Builds its own small screen-space toast UI at runtime, so it does not touch or move any
    /// existing UI elements, icons, or scene structure — just add this component anywhere.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MiningUnlockNotifier : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Auto-found in Awake when left empty.")]
        [SerializeField] private NpcProgressionSystem progressionSystem;
        [Tooltip("Auto-found in Awake when left empty.")]
        [SerializeField] private OreSpawner oreSpawner;
        [Tooltip("Optional. Auto-found in Awake when left empty. Leave the scene without one to skip Lucky Block unlock toasts.")]
        [SerializeField] private LuckyBlockDropSystem luckyBlockDropSystem;
        [Tooltip("Auto-found in Awake when left empty.")]
        [SerializeField] private MiningUiData uiData;

        private readonly Queue<string> pendingMessages = new();
        private readonly List<OreSpawnEntry> oreTableBuffer = new();
        private RectTransform toastRect;
        private Image toastBackground;
        private CanvasGroup toastGroup;
        private TextMeshProUGUI toastLabel;
        private Coroutine toastRoutine;
        private int lastKnownPower = int.MinValue;

        private void Awake()
        {
            if (progressionSystem == null)
            {
                progressionSystem = FindFirstObjectByType<NpcProgressionSystem>(
                    FindObjectsInactive.Include);
            }
            if (oreSpawner == null)
            {
                oreSpawner = FindFirstObjectByType<OreSpawner>(FindObjectsInactive.Include);
            }
            if (luckyBlockDropSystem == null)
            {
                luckyBlockDropSystem = FindFirstObjectByType<LuckyBlockDropSystem>(
                    FindObjectsInactive.Include);
            }
            if (uiData == null)
            {
                MiningUiPanelCoordinator coordinator = FindFirstObjectByType<MiningUiPanelCoordinator>(
                    FindObjectsInactive.Include);
                uiData = coordinator != null ? coordinator.UiData : null;
            }
        }

        private void OnEnable()
        {
            if (progressionSystem == null)
            {
                return;
            }

            // Baseline the current power without announcing anything already unlocked from a
            // previous session/save — only power gained from here on triggers a toast.
            lastKnownPower = progressionSystem.CurrentMiningPower;
            progressionSystem.LevelChanged -= HandleLevelChanged;
            progressionSystem.LevelChanged += HandleLevelChanged;
        }

        private void OnDisable()
        {
            if (progressionSystem != null)
            {
                progressionSystem.LevelChanged -= HandleLevelChanged;
            }
            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
                toastRoutine = null;
            }
            pendingMessages.Clear();
        }

        private void HandleLevelChanged(int newLevel)
        {
            if (progressionSystem == null)
            {
                return;
            }

            int newPower = progressionSystem.CurrentMiningPower;
            int oldPower = lastKnownPower;
            lastKnownPower = newPower;
            if (newPower <= oldPower)
            {
                // Power went down (e.g. a Rebirth reset) — nothing new was unlocked.
                return;
            }

            AnnounceUnlockedOres(oldPower, newPower);
            AnnounceUnlockedLuckyBlocks(oldPower, newPower);
        }

        private void AnnounceUnlockedOres(int oldPower, int newPower)
        {
            if (oreSpawner == null || oreSpawner.SpawnData == null)
            {
                return;
            }

            oreTableBuffer.Clear();
            oreTableBuffer.AddRange(oreSpawner.SpawnData.OreSpawnTable);
            foreach (OreSpawnEntry entry in oreTableBuffer)
            {
                OreData ore = entry.Ore;
                if (ore == null || ore.Prefab == null)
                {
                    continue;
                }

                if (ore.MiningPowerRequired <= oldPower || ore.MiningPowerRequired > newPower)
                {
                    continue;
                }

                string format = uiData != null ? uiData.OreUnlockToastFormat : "Đã mở khóa quặng: {0}!";
                ShowToast(string.Format(format, ore.DisplayName));
                oreSpawner.SpawnGuaranteedOre(ore);
            }
        }

        private void AnnounceUnlockedLuckyBlocks(int oldPower, int newPower)
        {
            if (luckyBlockDropSystem == null || luckyBlockDropSystem.Data == null ||
                luckyBlockDropSystem.Data.Variants == null)
            {
                return;
            }

            foreach (LuckyBlockVariantData variant in luckyBlockDropSystem.Data.Variants)
            {
                if (variant == null || variant.Model == null)
                {
                    continue;
                }

                if (variant.MiningPowerRequired <= oldPower || variant.MiningPowerRequired > newPower)
                {
                    continue;
                }

                string format = uiData != null
                    ? uiData.LuckyBlockUnlockToastFormat
                    : "Đã mở khóa Lucky Block: {0}!";
                ShowToast(string.Format(format, variant.DisplayName));
                luckyBlockDropSystem.TryDropOne(variant);
            }
        }

        private void ShowToast(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            EnsureToastUi();
            pendingMessages.Enqueue(message);
            toastRoutine ??= StartCoroutine(ProcessToastQueue());
        }

        private IEnumerator ProcessToastQueue()
        {
            float fadeDuration = uiData != null ? uiData.UnlockToastFadeDuration : 0.25f;
            float holdDuration = uiData != null ? uiData.UnlockToastHoldDuration : 2.2f;
            while (pendingMessages.Count > 0)
            {
                toastLabel.text = pendingMessages.Dequeue();
                yield return Fade(0f, 1f, fadeDuration);
                yield return new WaitForSecondsRealtime(holdDuration);
                yield return Fade(1f, 0f, fadeDuration);
            }
            toastRoutine = null;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            toastGroup.alpha = from;
            if (duration <= 0f)
            {
                toastGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                toastGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            toastGroup.alpha = to;
        }

        private void EnsureToastUi()
        {
            if (toastGroup != null)
            {
                return;
            }

            GameObject canvasObject = new("Unlock Toast Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = uiData != null ? uiData.CanvasSortingOrder + 50 : 150;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = uiData != null ? uiData.ReferenceResolution : new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = uiData != null ? uiData.MatchWidthOrHeight : 0.5f;

            GameObject panelObject = new("Unlock Toast Panel", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            panelObject.transform.SetParent(canvasObject.transform, false);
            toastRect = panelObject.GetComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 1f);
            toastRect.anchorMax = new Vector2(0.5f, 1f);
            toastRect.pivot = new Vector2(0.5f, 1f);
            toastRect.sizeDelta = uiData != null ? uiData.UnlockToastSize : new Vector2(460f, 64f);
            toastRect.anchoredPosition = uiData != null ? uiData.UnlockToastPosition : new Vector2(0f, -140f);

            toastBackground = panelObject.GetComponent<Image>();
            toastBackground.color = uiData != null
                ? uiData.UnlockToastBackgroundColor
                : new Color(0.08f, 0.09f, 0.11f, 0.95f);
            toastBackground.raycastTarget = false;

            toastGroup = panelObject.GetComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            toastGroup.interactable = false;

            GameObject labelObject = new("Label", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(panelObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 6f);
            labelRect.offsetMax = new Vector2(-16f, -6f);
            toastLabel = labelObject.GetComponent<TextMeshProUGUI>();
            toastLabel.alignment = TextAlignmentOptions.Center;
            toastLabel.fontStyle = FontStyles.Bold;
            toastLabel.fontSize = uiData != null ? uiData.UnlockToastFontSize : 24f;
            toastLabel.color = uiData != null ? uiData.UnlockToastTextColor : new Color(1f, 0.86f, 0.32f, 1f);
            toastLabel.raycastTarget = false;
        }
    }
}