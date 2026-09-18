using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>One rare scene-start threat: warns the player, steals one miner, and can jumpscare.</summary>
    [DisallowMultipleComponent]
    public sealed class StalkedEventSystem : MonoBehaviour
    {
        private const string DefaultEyesResourcePath = "MiningEvents/StalkedEyes";

        private DayNightSystem dayNightSystem;
        private DayNightData data;
        private NpcShop npcShop;
        private MiningAudioManager audioManager;
        private MiningUnlockNotifier notifier;
        private MiningMainMenu mainMenu;
        private MiningUiPanelCoordinator uiPanelCoordinator;
        private Coroutine eventRoutine;
        private Canvas eventCanvas;
        private CanvasGroup warningGroup;
        private readonly Image[] cornerImages = new Image[4];
        private CanvasGroup jumpscareGroup;
        private Image jumpscareImage;
        private bool nightHandled;
        private bool hudHiddenForEvent;

        public void Configure(DayNightSystem targetDayNightSystem, DayNightData targetData)
        {
            if (dayNightSystem != targetDayNightSystem && dayNightSystem != null)
            {
                dayNightSystem.PeriodChanged -= HandlePeriodChanged;
            }

            dayNightSystem = targetDayNightSystem;
            if (dayNightSystem != null)
            {
                dayNightSystem.PeriodChanged -= HandlePeriodChanged;
                dayNightSystem.PeriodChanged += HandlePeriodChanged;
            }
            data = targetData;
            if (dayNightSystem != null && dayNightSystem.CurrentPeriod == MiningTimePeriod.Night &&
                !nightHandled)
            {
                BeginNight();
            }
        }

        private void OnDisable()
        {
            if (dayNightSystem != null)
            {
                dayNightSystem.PeriodChanged -= HandlePeriodChanged;
            }
            if (eventRoutine != null)
            {
                StopCoroutine(eventRoutine);
                eventRoutine = null;
            }

            SetWarningVisible(false);
            SetEventHudVisible(true);
        }

        private void HandlePeriodChanged(MiningTimePeriod period)
        {
            if (period == MiningTimePeriod.Day)
            {
                nightHandled = false;
                if (eventRoutine != null)
                {
                    StopCoroutine(eventRoutine);
                    eventRoutine = null;
                }
                SetWarningVisible(false);
                SetEventHudVisible(true);
                return;
            }

            BeginNight();
        }

        private void BeginNight()
        {
            if (nightHandled || data == null)
            {
                return;
            }

            nightHandled = true;
            eventRoutine = StartCoroutine(RunNightEvent());
        }

        private IEnumerator RunNightEvent()
        {
            // A night can begin behind the main menu. Do not run its timer or show an alert
            // until the player has actually entered gameplay.
            while (IsMainMenuOpen())
            {
                yield return null;
            }

            if (dayNightSystem == null || dayNightSystem.CurrentPeriod != MiningTimePeriod.Night)
            {
                yield break;
            }

            // Lets the NPC shop settle after the gameplay screen becomes active.
            yield return new WaitForSecondsRealtime(1f);
            if (data == null || !data.StalkedEventEnabled ||
                Random.value * 100f > data.StalkedEventChanceAtSceneStartPercent)
            {
                yield break;
            }

            MiningNpc target = FindRandomActiveNpc();
            if (target == null)
            {
                yield break;
            }

            SetEventHudVisible(false);
            EnsureVisuals();
            SetWarningVisible(true);
            notifier ??= FindFirstObjectByType<MiningUnlockNotifier>(FindObjectsInactive.Include);
            notifier?.ShowToast("BEING STALKED");

            float elapsed = 0f;
            while (elapsed < data.StalkedWarningSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                UpdateCornerFlicker(elapsed);
                yield return null;
            }

            SetWarningVisible(false);
            if (target != null && target.gameObject.activeInHierarchy)
            {
                yield return SprintAndCapture(target);
            }
            SetEventHudVisible(true);
        }

        private void SetEventHudVisible(bool visible)
        {
            if (visible == !hudHiddenForEvent)
            {
                return;
            }

            uiPanelCoordinator ??= FindFirstObjectByType<MiningUiPanelCoordinator>(
                FindObjectsInactive.Include);
            uiPanelCoordinator?.SetBaseHudVisible(visible);
            hudHiddenForEvent = !visible;
        }

        private bool IsMainMenuOpen()
        {
            mainMenu ??= FindFirstObjectByType<MiningMainMenu>(FindObjectsInactive.Include);
            return mainMenu != null && mainMenu.IsOpen;
        }

        private IEnumerator SprintAndCapture(MiningNpc target)
        {
            GameObject creature = CreateShadowCreature(target.transform.position);
            if (creature == null)
            {
                yield break;
            }

            while (target != null && target.gameObject.activeInHierarchy)
            {
                Vector3 targetPosition = target.transform.position + Vector3.up * 0.75f;
                Vector3 offset = targetPosition - creature.transform.position;
                float distance = offset.magnitude;
                if (distance <= data.StalkedCaptureDistance)
                {
                    break;
                }

                Vector3 direction = offset / Mathf.Max(distance, 0.001f);
                creature.transform.position = Vector3.MoveTowards(creature.transform.position,
                    targetPosition, data.StalkedCreatureSprintSpeed * Time.deltaTime);
                Vector3 flatDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
                if (flatDirection.sqrMagnitude > 0.001f)
                {
                    creature.transform.rotation = Quaternion.LookRotation(flatDirection, Vector3.up);
                }
                yield return null;
            }

            if (target != null && target.gameObject.activeInHierarchy)
            {
                CaptureNpc(target);
            }

            Destroy(creature);
        }

        private void CaptureNpc(MiningNpc target)
        {
            npcShop ??= FindFirstObjectByType<NpcShop>(FindObjectsInactive.Include);
            audioManager ??= FindFirstObjectByType<MiningAudioManager>(FindObjectsInactive.Include);
            notifier ??= FindFirstObjectByType<MiningUnlockNotifier>(FindObjectsInactive.Include);

            bool removed = npcShop != null && npcShop.TryRemoveNpc(target);
            if (!removed)
            {
                return;
            }

            audioManager?.PlaySfx(audioManager.AudioData != null
                ? audioManager.AudioData.StalkedCatchSfx
                : null, 1.2f);
            notifier?.ShowToast("YOUR NPC WAS TAKEN INTO THE VOID!");

            if (Random.value * 100f <= data.StalkedJumpscareChancePercent)
            {
                StartCoroutine(ShowJumpscare());
            }
        }

        private MiningNpc FindRandomActiveNpc()
        {
            MiningNpc[] npcs = FindObjectsByType<MiningNpc>(FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            return npcs.Length == 0 ? null : npcs[Random.Range(0, npcs.Length)];
        }

        private GameObject CreateShadowCreature(Vector3 targetPosition)
        {
            Vector2 ring = Random.insideUnitCircle.normalized;
            if (ring.sqrMagnitude < 0.01f) ring = Vector2.right;
            Vector3 start = targetPosition + new Vector3(ring.x, 0f, ring.y) * 26f;
            GameObject creature = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            creature.name = "Stalked Shadow Creature";
            creature.transform.position = start + Vector3.up;
            creature.transform.localScale = new Vector3(1.25f, 2.6f, 1.25f);
            Collider creatureCollider = creature.GetComponent<Collider>();
            if (creatureCollider != null) creatureCollider.enabled = false;

            Renderer body = creature.GetComponent<Renderer>();
            if (body != null)
            {
                body.material.color = new Color(0.01f, 0f, 0.015f, 1f);
            }

            CreateEye(creature.transform, new Vector3(-0.22f, 0.55f, 0.5f));
            CreateEye(creature.transform, new Vector3(0.22f, 0.55f, 0.5f));
            return creature;
        }

        private static void CreateEye(Transform parent, Vector3 localPosition)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Red Eye";
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = localPosition;
            eye.transform.localScale = Vector3.one * 0.16f;
            Collider eyeCollider = eye.GetComponent<Collider>();
            if (eyeCollider != null) eyeCollider.enabled = false;
            Renderer renderer = eye.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 0.02f, 0.01f, 1f);
            }
        }

        private IEnumerator ShowJumpscare()
        {
            EnsureVisuals();
            Sprite image = data.GetRandomStalkedJumpscareImage() ??
                           Resources.Load<Sprite>(DefaultEyesResourcePath);
            if (jumpscareGroup == null || jumpscareImage == null || image == null)
            {
                yield break;
            }

            audioManager ??= FindFirstObjectByType<MiningAudioManager>(FindObjectsInactive.Include);
            audioManager?.PlaySfx(audioManager.AudioData != null
                ? audioManager.AudioData.StalkedJumpscareSfx
                : null, 2f);
            jumpscareImage.sprite = image;
            jumpscareGroup.alpha = 1f;
            jumpscareGroup.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(data.StalkedJumpscareSeconds);
            jumpscareGroup.alpha = 0f;
            jumpscareGroup.gameObject.SetActive(false);
        }

        private void EnsureVisuals()
        {
            if (eventCanvas != null)
            {
                return;
            }

            GameObject canvasObject = new("Stalked Event Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            eventCanvas = canvasObject.GetComponent<Canvas>();
            eventCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            eventCanvas.sortingOrder = 300;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            CreateWarningToast(canvasObject.transform);
            CreateJumpscareOverlay(canvasObject.transform);
        }

        private void CreateWarningToast(Transform parent)
        {
            GameObject root = new("Being Stalked Effect", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.sizeDelta = data != null ? data.StalkedToastSize : new Vector2(420f, 100f);
            rootRect.anchoredPosition = data != null ? data.StalkedToastPosition : new Vector2(0f, -210f);
            warningGroup = root.GetComponent<CanvasGroup>();

            Image panel = CreateImage(root.transform, "Panel", data != null ? data.StalkedToastColor : new Color(0.16f, 0f, 0.01f, 0.94f));
            Stretch(panel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f));
            Image icon = CreateImage(root.transform, "Eyes", Color.white);
            icon.sprite = Resources.Load<Sprite>(DefaultEyesResourcePath);
            icon.preserveAspect = true;
            icon.rectTransform.anchorMin = new Vector2(0f, 0f);
            icon.rectTransform.anchorMax = new Vector2(0f, 1f);
            icon.rectTransform.sizeDelta = new Vector2(112f, 0f);
            icon.rectTransform.anchoredPosition = new Vector2(56f, 0f);

            TextMeshProUGUI title = CreateText(root.transform, "Title", "BEING STALKED", 28f);
            title.rectTransform.anchorMin = new Vector2(0f, 0.48f);
            title.rectTransform.anchorMax = new Vector2(1f, 0.95f);
            title.rectTransform.offsetMin = new Vector2(112f, 0f);
            title.rectTransform.offsetMax = new Vector2(-16f, 0f);
            title.color = data != null ? data.StalkedTitleColor : new Color(1f, 0.16f, 0.08f, 1f);

            TextMeshProUGUI subtitle = CreateText(root.transform, "Description",
                "A shadow is watching your miners.", 17f);
            subtitle.rectTransform.anchorMin = new Vector2(0f, 0.05f);
            subtitle.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            subtitle.rectTransform.offsetMin = new Vector2(112f, 0f);
            subtitle.rectTransform.offsetMax = new Vector2(-16f, 0f);
            subtitle.color = data != null ? data.StalkedTextColor : new Color(1f, 0.76f, 0.72f, 1f);

            for (int index = 0; index < cornerImages.Length; index++)
            {
                Color cornerColor = data != null ? data.StalkedCornerColor : new Color(0.82f, 0.01f, 0.01f, 1f);
                cornerColor.a = 0f;
                Image corner = CreateImage(parent, $"Red Corner {index + 1}", cornerColor);
                corner.raycastTarget = false;
                corner.rectTransform.sizeDelta = new Vector2(280f, 160f);
                corner.rectTransform.anchorMin = new Vector2(index % 2, index / 2);
                corner.rectTransform.anchorMax = corner.rectTransform.anchorMin;
                corner.rectTransform.pivot = new Vector2(index % 2, index / 2);
                cornerImages[index] = corner;
            }

            SetWarningVisible(false);
        }

        private void CreateJumpscareOverlay(Transform parent)
        {
            GameObject root = new("Stalked Jumpscare", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            jumpscareGroup = root.GetComponent<CanvasGroup>();
            jumpscareGroup.alpha = 0f;
            jumpscareGroup.blocksRaycasts = false;
            jumpscareGroup.interactable = false;

            Image backdrop = CreateImage(root.transform, "Backdrop", new Color(0f, 0f, 0f, 0.96f));
            Stretch(backdrop.rectTransform, Vector2.zero, Vector2.zero);
            jumpscareImage = CreateImage(root.transform, "Image", Color.white);
            jumpscareImage.preserveAspect = true;
            Stretch(jumpscareImage.rectTransform, Vector2.zero, Vector2.zero);
            root.SetActive(false);
        }

        private void SetWarningVisible(bool visible)
        {
            if (warningGroup != null)
            {
                warningGroup.alpha = visible ? 1f : 0f;
            }

            foreach (Image corner in cornerImages)
            {
                if (corner != null)
                {
                    Color color = corner.color;
                    color.a = 0f;
                    corner.color = color;
                }
            }
        }

        private void UpdateCornerFlicker(float elapsed)
        {
            float pulse = 0.12f + Mathf.Abs(Mathf.Sin(elapsed * data.StalkedCornerFlickersPerSecond)) * 0.42f;
            foreach (Image corner in cornerImages)
            {
                if (corner == null) continue;
                Color color = corner.color;
                color.a = pulse;
                corner.color = color;
            }
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string value,
            float fontSize)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.alignment = TextAlignmentOptions.Left;
            text.fontStyle = FontStyles.Bold;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = fontSize;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
