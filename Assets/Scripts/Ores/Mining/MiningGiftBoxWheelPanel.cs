using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiningSimulator.Ores
{
    /// <summary>Presents one configured gift-box roll and grants its selected reward.</summary>
    [DisallowMultipleComponent]
    public sealed class MiningGiftBoxWheelPanel : MonoBehaviour
    {
        private readonly List<GameObject> rewardCards = new();

        private MiningItemSystem itemSystem;
        private PlayerWallet wallet;
        private MiningUiPanelCoordinator panelCoordinator;
        private MiningUiData uiData;
        private RectTransform returnPanel;
        private RectTransform wheel;
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI statusLabel;
        private TextMeshProUGUI spinLabel;
        private Button spinButton;
        private Button closeButton;
        private MiningItemData giftBox;
        private MiningGiftReward selectedReward;
        private int sourceSlotIndex = -1;
        private bool spinning;
        private Tween spinTween;
        private RectTransform centerHub;

        private static Sprite cachedWheelSprite;

        public void Configure(MiningItemSystem system, PlayerWallet playerWallet,
            MiningUiPanelCoordinator coordinator, MiningUiData data, RectTransform inventoryPanel)
        {
            itemSystem = system;
            wallet = playerWallet;
            panelCoordinator = coordinator;
            uiData = data;
            returnPanel = inventoryPanel;
            EnsureUi();
        }

        public void Show(int slotIndex, MiningItemData item)
        {
            if (item == null || item.UseType != MiningItemUseType.GiftBox)
            {
                return;
            }

            EnsureUi();
            giftBox = item;
            selectedReward = null;
            sourceSlotIndex = slotIndex;
            spinning = false;
            wheel.localRotation = Quaternion.identity;
            spinButton.interactable = true;
            closeButton.interactable = true;
            RebuildRewardCards();
            RefreshLabels();

            RectTransform panel = (RectTransform)transform;
            if (panelCoordinator != null)
            {
                panelCoordinator.OpenPanel(panel);
            }
            else
            {
                gameObject.SetActive(true);
                returnPanel?.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
            MiningLocalization.LanguageChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            MiningLocalization.LanguageChanged -= HandleLanguageChanged;
            if (spinTween.isAlive)
            {
                spinTween.Stop();
            }
            if (spinning && giftBox != null && itemSystem != null)
            {
                // A modal interruption or Scene shutdown must never silently eat the box.
                itemSystem.TryAddItem(giftBox);
            }
            spinning = false;
        }

        private void StartSpin()
        {
            if (spinning || giftBox == null || itemSystem == null || wallet == null ||
                !giftBox.TryRollGiftReward(out int rewardIndex, out MiningGiftReward reward))
            {
                statusLabel.text = MiningLocalization.Text(
                    "This gift box has no valid rewards.",
                    "Hộp quà chưa có phần thưởng hợp lệ.");
                return;
            }

            if (reward.RewardType == MiningGiftRewardType.Item &&
                !CanFitItemRewardAfterConsumption(reward))
            {
                statusLabel.text = MiningLocalization.Text(
                    "Inventory is full. Free a slot before spinning.",
                    "Túi đồ đã đầy. Hãy dọn một ô trước khi quay.");
                return;
            }
            if (!itemSystem.TryConsumeGiftBox(sourceSlotIndex, giftBox))
            {
                statusLabel.text = MiningLocalization.Text(
                    "The gift box is no longer in this slot.",
                    "Hộp quà không còn trong ô này.");
                return;
            }

            selectedReward = reward;
            spinning = true;
            spinButton.interactable = false;
            closeButton.interactable = false;
            statusLabel.text = MiningLocalization.Text("Spinning...", "Đang quay...");

            int rewardCount = Mathf.Max(1, giftBox.GiftRewards.Count);
            float step = 360f / rewardCount;
            float startAngle = wheel.localEulerAngles.z;
            float selectedAngle = Mathf.Repeat(-rewardIndex * step, 360f);
            float alignment = Mathf.Repeat(selectedAngle - Mathf.Repeat(startAngle, 360f), 360f);
            float endAngle = startAngle + giftBox.GiftSpinRotations * 360f + alignment;
            spinTween = Tween.Custom(this, startAngle, endAngle,
                    giftBox.GiftSpinDurationSeconds,
                    static (panel, angle) => panel.wheel.localRotation =
                        Quaternion.Euler(0f, 0f, angle),
                    Ease.OutQuart, useUnscaledTime: true)
                .OnComplete(this, static panel => panel.CompleteSpin());
        }

        private bool CanFitItemRewardAfterConsumption(MiningGiftReward reward)
        {
            if (itemSystem.CanAddItem(reward.Item, reward.ItemAmount))
            {
                return true;
            }

            MiningItemSystem.InventorySlotView source = itemSystem.GetSlot(sourceSlotIndex);
            return source.Item == giftBox && source.Count == 1 &&
                   reward.ItemAmount <= reward.Item.MaximumStack;
        }

        private void CompleteSpin()
        {
            spinning = false;
            closeButton.interactable = true;
            if (selectedReward == null)
            {
                return;
            }

            bool granted;
            if (selectedReward.RewardType == MiningGiftRewardType.Money)
            {
                wallet.AddMoney(selectedReward.MoneyAmount);
                granted = true;
            }
            else
            {
                granted = itemSystem.TryAddItem(selectedReward.Item, selectedReward.ItemAmount);
            }

            if (granted)
            {
                statusLabel.text = string.Format(MiningLocalization.Text(
                        "YOU WON: {0}", "BẠN NHẬN ĐƯỢC: {0}"),
                    selectedReward.GetDisplayName());
                spinLabel.text = MiningLocalization.Text("REWARD RECEIVED", "ĐÃ NHẬN THƯỞNG");
            }
            else
            {
                itemSystem.TryAddItem(giftBox);
                statusLabel.text = MiningLocalization.Text(
                    "Inventory changed during the spin. The gift box was returned.",
                    "Túi đồ đã thay đổi khi quay. Hộp quà đã được hoàn lại.");
                spinLabel.text = MiningLocalization.Text("SPIN FAILED", "QUAY THẤT BẠI");
            }
        }

        private void ReturnToInventory()
        {
            if (spinning)
            {
                return;
            }

            if (panelCoordinator != null && returnPanel != null)
            {
                panelCoordinator.OpenPanel(returnPanel);
            }
            else
            {
                gameObject.SetActive(false);
                returnPanel?.gameObject.SetActive(true);
            }
        }

        private void HandleLanguageChanged()
        {
            if (!spinning)
            {
                RebuildRewardCards();
                RefreshLabels();
            }
        }

        private void RefreshLabels()
        {
            titleLabel.text = giftBox != null
                ? MiningLocalization.Text("RARE GIFT BOX", "HỘP QUÀ HIẾM")
                : string.Empty;
            if (selectedReward == null)
            {
                statusLabel.text = MiningLocalization.Text(
                    "The pointer decides your reward. Press SPIN.",
                    "Kim chỉ phần thưởng của bạn. Bấm QUAY.");
                spinLabel.text = MiningLocalization.Text("SPIN", "QUAY");
            }
        }

        private void RebuildRewardCards()
        {
            foreach (GameObject card in rewardCards)
            {
                if (card != null)
                {
                    Destroy(card);
                }
            }
            rewardCards.Clear();
            if (giftBox == null || wheel == null)
            {
                return;
            }

            int count = giftBox.GiftRewards.Count;
            if (count <= 0)
            {
                return;
            }

            float step = 360f / count;
            float labelRadius = uiData != null ? uiData.GiftWheelRewardRadius : 142f;
            Vector2 labelSize = uiData != null ? uiData.GiftWheelRewardSize : new Vector2(150f, 58f);
            for (int index = 0; index < count; index++)
            {
                MiningGiftReward reward = giftBox.GiftRewards[index];
                if (reward == null || !reward.IsValid)
                {
                    continue;
                }

                float centerAngle = index * step;
                float startAngle = centerAngle - step * 0.5f;

                // A real pie-slice wedge that fills exactly its share of the circular wheel.
                // Wedges are rotated into place (not translated like the old rectangles), so
                // they always tile edge-to-edge with no overlap or gaps, and the wheel keeps
                // looking like a smooth circle at every spin rotation instead of a square.
                rewardCards.Add(CreateWedge(wheel, $"Wedge {index + 1:00}", reward.WheelColor,
                    step / 360f, startAngle));
                rewardCards.Add(CreateDivider(wheel, startAngle));

                float radians = centerAngle * Mathf.Deg2Rad;
                GameObject labelObject = new("Reward Label", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(wheel, false);
                RectTransform labelRect = (RectTransform)labelObject.transform;
                SetCenteredRect(labelRect,
                    new Vector2(Mathf.Sin(radians) * labelRadius, Mathf.Cos(radians) * labelRadius),
                    labelSize);
                TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
                label.fontSize = 17f;
                label.color = Color.white;
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.raycastTarget = false;
                label.text = $"{reward.GetDisplayName()}\n{giftBox.GetGiftRewardDisplayPercent(reward):0.##}%";
                rewardCards.Add(labelObject);
            }

            // Wedges are appended after the hub, which would otherwise bury the "RARE"
            // center label under the slice tips that converge at the middle of the wheel.
            if (centerHub != null)
            {
                centerHub.SetAsLastSibling();
            }
        }

        private void EnsureUi()
        {
            if (wheel != null)
            {
                return;
            }

            RectTransform panel = (RectTransform)transform;
            SetCenteredRect(panel, Vector2.zero,
                uiData != null ? uiData.GiftWheelPanelSize : new Vector2(720f, 620f));
            Image panelImage = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            panelImage.color = uiData != null ? uiData.GiftWheelPanelColor :
                new Color(0.06f, 0.075f, 0.11f, 0.98f);

            GameObject header = CreateImage(panel, "Header",
                uiData != null ? uiData.GiftWheelHeaderColor : new Color(0.48f, 0.24f, 0.75f));
            RectTransform headerRect = (RectTransform)header.transform;
            Vector2 headerSize = uiData != null ? uiData.GiftWheelHeaderSize : new Vector2(720f, 72f);
            SetTopRect(headerRect, Vector2.zero, headerSize);
            titleLabel = CreateLabel(headerRect, "Title", 30f, Color.white);

            closeButton = CreateButton(panel, "Close", "X", new Vector2(324f, 274f),
                new Vector2(50f, 50f), new Color(0.96f, 0.08f, 0.34f));
            closeButton.onClick.AddListener(ReturnToInventory);

            GameObject wheelObject = CreateImage(panel, "Wheel", new Color(0.11f, 0.14f, 0.20f, 1f));
            wheel = (RectTransform)wheelObject.transform;
            SetCenteredRect(wheel, new Vector2(0f, 32f),
                uiData != null ? uiData.GiftWheelSize : new Vector2(360f, 360f));
            Image wheelImage = wheelObject.GetComponent<Image>();
            wheelImage.sprite = GetWheelSprite();
            wheelImage.type = Image.Type.Simple;

            GameObject hubObject = CreateImage(wheel, "Hub", new Color(0.08f, 0.1f, 0.15f, 0.95f));
            centerHub = (RectTransform)hubObject.transform;
            SetCenteredRect(centerHub, Vector2.zero, new Vector2(112f, 112f));
            Image hubImage = hubObject.GetComponent<Image>();
            hubImage.sprite = GetWheelSprite();
            hubImage.type = Image.Type.Simple;
            TextMeshProUGUI center = CreateLabel(centerHub, "Center", 22f, new Color(1f, 0.78f, 0.16f));
            center.text = "RARE";

            TextMeshProUGUI pointer = CreateLabel(panel, "Pointer", 42f,
                new Color(1f, 0.82f, 0.12f));
            pointer.text = "▼";
            pointer.rectTransform.anchoredPosition = new Vector2(0f, 236f);
            pointer.rectTransform.sizeDelta = new Vector2(60f, 50f);

            statusLabel = CreateLabel(panel, "Status", 21f, Color.white);
            statusLabel.rectTransform.anchoredPosition = new Vector2(0f, -201f);
            statusLabel.rectTransform.sizeDelta = new Vector2(650f, 50f);

            Vector2 spinSize = uiData != null ? uiData.GiftWheelSpinButtonSize : new Vector2(260f, 56f);
            spinButton = CreateButton(panel, "Spin", string.Empty, new Vector2(0f, -265f), spinSize,
                uiData != null ? uiData.GiftWheelSpinButtonColor : new Color(1f, 0.62f, 0.08f));
            spinLabel = spinButton.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            spinButton.onClick.AddListener(StartSpin);
        }

        /// <summary>A soft-edged white circle, generated once and reused for the wheel
        /// background, the center hub, and every pie wedge (via radial fill).</summary>
        private static Sprite GetWheelSprite()
        {
            if (cachedWheelSprite != null)
            {
                return cachedWheelSprite;
            }

            const int size = 256;
            const float edgeSoftness = 1.5f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "GiftWheelCircle"
            };

            Vector2 center = new(size * 0.5f, size * 0.5f);
            float radius = size * 0.5f - 1f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01((radius - distance) / edgeSoftness + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            cachedWheelSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), 100f);
            return cachedWheelSprite;
        }

        /// <summary>One pie slice covering [startAngle, startAngle + fillAmount * 360],
        /// measured clockwise from the top (matching the pointer). Rotated into place
        /// rather than translated, so adjacent wedges always share an exact edge.</summary>
        private static GameObject CreateWedge(Transform parent, string objectName, Color color,
            float fillAmount, float startAngle)
        {
            GameObject child = new(objectName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            child.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)child.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0f, 0f, -startAngle);

            Image image = child.GetComponent<Image>();
            image.color = color;
            image.sprite = GetWheelSprite();
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = true;
            image.fillAmount = Mathf.Clamp01(fillAmount);
            return child;
        }

        /// <summary>A thin radial line marking the boundary between two wedges.</summary>
        private static GameObject CreateDivider(Transform parent, float angle)
        {
            GameObject child = new("Divider", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            child.transform.SetParent(parent, false);
            RectTransform wheelRect = (RectTransform)parent;
            float wheelRadius = Mathf.Min(wheelRect.rect.width, wheelRect.rect.height) * 0.5f;

            RectTransform rect = (RectTransform)child.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(3f, wheelRadius);
            rect.localRotation = Quaternion.Euler(0f, 0f, -angle);

            Image image = child.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.35f);
            image.raycastTarget = false;
            return child;
        }

        private static GameObject CreateImage(Transform parent, string objectName, Color color)
        {
            GameObject child = new(objectName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            child.transform.SetParent(parent, false);
            Image image = child.GetComponent<Image>();
            image.color = color;
            Outline outline = child.AddComponent<Outline>();
            outline.effectColor = new Color(0.025f, 0.055f, 0.09f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);
            return child;
        }

        private static Button CreateButton(Transform parent, string objectName, string text,
            Vector2 position, Vector2 size, Color color)
        {
            GameObject buttonObject = CreateImage(parent, objectName, color);
            RectTransform rect = (RectTransform)buttonObject.transform;
            SetCenteredRect(rect, position, size);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            TextMeshProUGUI label = CreateLabel(rect, "Label", 21f, Color.white);
            label.text = text;
            return button;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string objectName,
            float fontSize, Color color)
        {
            GameObject labelObject = new(objectName, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            SetCenteredRect(label.rectTransform, Vector2.zero, Vector2.zero);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        private static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
